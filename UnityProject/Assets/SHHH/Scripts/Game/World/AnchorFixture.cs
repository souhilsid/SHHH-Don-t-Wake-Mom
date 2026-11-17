using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Chore target (shelf slot, rack hook, tray...). Shows a ghost of the expected item, gently snaps a released item
    /// into place (hands-friendly precision), and reports validated placement/removal to the host RoundController.
    /// </summary>
    public sealed class AnchorFixture : MonoBehaviour
    {
        public AnchorSpec Spec;
        public PropBody Occupant { get; private set; }
        public bool Active;          // part of a chore this round
        public bool IsBonusFor = false;
        public int BonusOwner = ActorIds.None;
        /// <summary>V5: personal-duty targets are tinted in their owner's colour for everyone (rivals can see what to sabotage).</summary>
        public Color OwnerColor = Palette.Yellow;
        Renderer _ghost, _ring;
        float _snapT = -1f; PropBody _snapping; Vector3 _snapFrom; Quaternion _snapFromRot; int _snapActor;
        float _settleTimer;
        PropBody _candidate;

        public void Init(AnchorSpec spec)
        {
            Spec = spec;
            transform.position = VisualFactory.V(spec.Position);
            if (spec.Kind == "shelf_book") transform.rotation = Quaternion.Euler(0, -90, 0);
            // Physical support pad so the target is always reachable and stable, whatever furniture model sits here.
            var pad = Prim.Box(transform, "pad", new Vector3(0, -0.008f, 0), new Vector3(spec.Radius * 1.8f, 0.016f, spec.Radius * 1.8f), Mats.Lit(Palette.Cream));
            pad.layer = Layers.Furniture;
            var ring = Prim.Shape(PrimitiveType.Cylinder, transform, "ring", new Vector3(0, 0.002f, 0), new Vector3(spec.Radius * 2f, 0.002f, spec.Radius * 2f), Mats.Unlit(Palette.Yellow));
            _ring = ring.GetComponent<Renderer>();
            var ghostRoot = new GameObject("ghost"); ghostRoot.transform.SetParent(transform, false);
            var g = VisualFactory.BuildProp(spec.AcceptsPropKind, ghostRoot.transform);
            var glow = Mats.Glow(new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.16f));
            foreach (var r in g.GetComponentsInChildren<Renderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = glow; r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            _ghost = g.GetComponentInChildren<Renderer>();
            SetActive(false);
        }

        public void SetActive(bool on)
        {
            Active = on;
            if (_ring) _ring.gameObject.SetActive(on);
            var ghostRoot = transform.Find("ghost"); if (ghostRoot) ghostRoot.gameObject.SetActive(on && Occupant == null);
        }

        void Update()
        {
            if (!Active) return;
            var ghostRoot = transform.Find("ghost");
            if (ghostRoot) { bool show = Occupant == null && (!IsBonusFor || BonusOwner == GameServices.LocalSlot); if (ghostRoot.gameObject.activeSelf != show) ghostRoot.gameObject.SetActive(show); if (show) ghostRoot.localPosition = new Vector3(0, 0.01f + Mathf.Sin(Time.time * 2.5f) * 0.008f, 0); }
            if (_ring) _ring.sharedMaterial = Occupant != null ? Mats.Glow(new Color(Palette.Ok.r, Palette.Ok.g, Palette.Ok.b, 0.45f)) : Mats.Glow(new Color(OwnerColor.r, OwnerColor.g, OwnerColor.b, 0.35f));
        }

        static readonly System.Collections.Generic.List<AnchorFixture> _all = new System.Collections.Generic.List<AnchorFixture>();
        void OnEnable() { if (!_all.Contains(this)) _all.Add(this); }
        void OnDisable() { _all.Remove(this); }
        bool IsOccupantElsewhere(PropBody p) { foreach (var a in _all) if (a != this && (a.Occupant == p || a._snapping == p)) return true; return false; }

        void FixedUpdate()
        {
            if (!Active || !GameServices.IsAuthority) return;
            var round = GameServices.Round; if (round == null) return;

            if (_snapping != null) { StepSnap(round); return; }

            // Occupant left? -> removal (credit suspended in the board).
            if (Occupant != null)
            {
                float d = Vector3.Distance(Occupant.transform.position, transform.position);
                if (Occupant.IsHeld || d > Spec.Radius * 1.8f + 0.06f)
                {
                    if (!Occupant.IsHeld) round.Removed(Occupant.Spec.Id, Occupant.LastActor);
                    Occupant = null;
                }
                return;
            }

            // Mom is in the room (play frozen): wait; the item is validated when play resumes (no rejection spam).
            if (!round.IsPlayable) return;
            // Look for a released item of the accepted kind nearby.
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.08f, Spec.Radius * 1.7f + 0.08f, Layers.PropsMask, QueryTriggerInteraction.Ignore);
            PropBody best = null; float bestD = float.MaxValue;
            foreach (var h in hits)
            {
                var p = h.GetComponentInParent<PropBody>();
                if (p == null || p.IsHeld || p.Spec.Kind != Spec.AcceptsPropKind) continue;
                // net_multi run 2: two neighbouring slots both snapped the same released item (and kept re-snapping it), which
                // spammed rejections and could drag a placed item off its own slot. One snap per item; never steal an occupant.
                if (p.ReservedUntil > Time.time || (p.Body != null && p.Body.isKinematic) || IsOccupantElsewhere(p)) continue;
                var c = round.Board.ChoreForProp(p.Spec.Id); if (c == null) continue;
                var st = c.StepForProp(p.Spec.Id); if (st != null && st.Done) continue;
                if (c.IsBonus && p.LastActor != c.BonusOwner) continue; // only the owner can complete a personal job (no snap loops)
                float d = Vector3.Distance(p.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            if (best == null) { _candidate = null; _settleTimer = 0f; return; }
            if (best != _candidate) { _candidate = best; _settleTimer = 0f; }
            if (best.Speed < 0.35f) _settleTimer += Time.fixedDeltaTime; else _settleTimer = 0f;
            if (_settleTimer > 0.12f && Time.time - best.LastReleaseTime < 30f)
            {
                best.ReservedUntil = Time.time + 1.0f;
                // Begin a short kinematic snap into the slot (never during a fast throw).
                _snapping = best; _snapT = 0f; _snapFrom = best.transform.position; _snapFromRot = best.transform.rotation; _snapActor = best.LastActor;
                best.Body.isKinematic = true;
            }
        }

        void StepSnap(RoundController round)
        {
            _snapT += Time.fixedDeltaTime / 0.22f;
            var p = _snapping;
            if (p.IsHeld) { _snapping = null; _candidate = null; _settleTimer = 0f; return; } // someone grabbed it mid-snap: hands win
            var targetRot = Quaternion.Euler(0, transform.eulerAngles.y + p.Spec.SpawnYaw * 0f, 0) * Quaternion.Euler(0, Mathf.Round(p.transform.eulerAngles.y / 90f) * 90f, 0);
            if (Spec.Kind == "shelf_book") targetRot = transform.rotation;
            var target = transform.position + Vector3.up * 0.004f;
            p.Body.MovePosition(Vector3.Lerp(_snapFrom, target, Mathf.SmoothStep(0, 1, _snapT)));
            p.Body.MoveRotation(Quaternion.Slerp(_snapFromRot, targetRot, Mathf.SmoothStep(0, 1, _snapT)));
            if (_snapT < 1f) return;
            p.Body.isKinematic = false; p.Body.linearVelocity = Vector3.zero; p.Body.angularVelocity = Vector3.zero;
            p.IgnoreImpactsUntil = Time.time + 0.4f;
            _snapping = null; _candidate = null; _settleTimer = 0f;
            if (p.IsHeld) return;
            if (round.Placed(p.Spec.Id, Spec.Id, _snapActor))
            {
                Occupant = p;
                RoomFx.StepDone(this, round.Board.ChoreForProp(p.Spec.Id), _snapActor);
            }
            else Debug.Log($"[SHHH][anchor] {Spec.Kind}#{Spec.Id} rejected {p.Spec.Kind}#{p.Spec.Id} by actor {_snapActor}: {(round.IsPlayable ? round.Board.LastRejectReason : "round not playable")}");
        }

        /// <summary>Used by bots: an exact, gentle placement (still physically validated by FixedUpdate).</summary>
        public Vector3 PlacePoint => transform.position + Vector3.up * 0.01f;
    }
}
