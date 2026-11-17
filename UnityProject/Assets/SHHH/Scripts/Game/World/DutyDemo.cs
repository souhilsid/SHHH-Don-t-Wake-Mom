using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// FUN-G3 / UX-G2 "show, don't tell" round demonstration (~6 s, starts while the countdown runs, never blocks play):
    /// a glowing ghost hand pinches a ghost copy of YOUR first job item, carries it in an arc to its target and drops it
    /// with a sparkle, three times. Labels "YOURS" / "HERE" in your colour. Presentation only - no physics, no rules.
    /// </summary>
    public sealed class DutyDemo : MonoBehaviour
    {
        Transform _hand, _ghost;
        Vector3 _from, _to;
        float _t; int _loops;
        const float Loop = 2.1f; const int Loops = 3;

        public static void Play(RoomRuntime room, RoundController round, int slot)
        {
            if (room == null || round == null || !ActorIds.IsSibling(slot) || !round.Roster[slot].IsActive) return;
            var duty = round.Board.BonusFor(slot); if (duty == null || duty.Steps.Count == 0) return;
            var step = duty.Steps[0];
            var prop = room.Prop(step.PropId); var anchor = room.Anchor(step.AnchorId);
            if (prop == null || anchor == null) return;
            var go = new GameObject("DutyDemo"); go.transform.SetParent(room.transform, false);
            var d = go.AddComponent<DutyDemo>();
            d.Build(prop, anchor, DutyVisuals.ColorFor(round, slot));
        }

        void Build(PropBody prop, AnchorFixture anchor, Color col)
        {
            var glow = Mats.Glow(new Color(col.r, col.g, col.b, 0.38f));
            var b = prop.Col != null ? prop.Col.bounds : new Bounds(prop.transform.position, Vector3.one * 0.1f);
            _from = new Vector3(b.center.x, b.max.y, b.center.z);
            _to = anchor.PlacePoint + Vector3.up * (b.size.y + 0.02f);
            // ghost copy of the item (shared meshes, one glow material)
            _ghost = new GameObject("ghostItem").transform; _ghost.SetParent(transform, false);
            _ghost.SetPositionAndRotation(prop.transform.position, prop.transform.rotation);
            _grabOffset = prop.transform.position - _from;
            foreach (var mf in prop.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || mf.GetComponent<Renderer>() == null || !mf.GetComponent<Renderer>().enabled) continue;
                var g = new GameObject("g"); g.transform.SetParent(_ghost, false);
                g.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation); g.transform.localScale = mf.transform.lossyScale;
                g.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = glow; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // ghost hand: palm + three fingers + thumb (rounded, readable at 2 m)
            _hand = new GameObject("ghostHand").transform; _hand.SetParent(transform, false);
            Prim.Shape(PrimitiveType.Sphere, _hand, "palm", new Vector3(0, 0.05f, -0.04f), new Vector3(0.08f, 0.035f, 0.09f), glow);
            for (int i = -1; i <= 1; i++) Prim.Shape(PrimitiveType.Capsule, _hand, "f", new Vector3(i * 0.022f, 0.045f, 0.02f), new Vector3(0.018f, 0.03f, 0.018f), glow).transform.localRotation = Quaternion.Euler(70, 0, 0);
            Prim.Shape(PrimitiveType.Capsule, _hand, "thumb", new Vector3(-0.045f, 0.03f, -0.01f), new Vector3(0.018f, 0.025f, 0.018f), glow).transform.localRotation = Quaternion.Euler(60, 0, 50);
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            RoomFx.Say(_from + Vector3.up * 0.32f, "YOURS", col, Loop * Loops, 0.6f);
            RoomFx.Say(_to + Vector3.up * 0.3f, "HERE", col, Loop * Loops, 0.6f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / Loop;
            if (k >= 1f) { _t = 0f; _loops++; RoomFx.Burst("toon_impact", _to, 0.12f, 1f); if (_loops >= Loops) { Destroy(gameObject); return; } k = 0f; }
            Vector3 hand; Vector3 item;
            if (k < 0.2f) { hand = Vector3.Lerp(_from + new Vector3(0, 0.25f, -0.15f), _from, k / 0.2f); item = _from; }      // reach in
            else if (k < 0.3f) { hand = _from; item = _from; }                                                                     // pinch
            else if (k < 0.85f) { float u = Mathf.SmoothStep(0, 1, (k - 0.3f) / 0.55f); hand = Vector3.Lerp(_from, _to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.35f; item = hand; } // carry arc
            else { hand = _to + Vector3.up * (0.12f * (k - 0.85f) / 0.15f); item = _to; }                                        // let go
            _hand.position = hand + Vector3.up * 0.01f;
            float pinch = k >= 0.2f && k < 0.85f ? 0.8f : 1f;
            _hand.localScale = Vector3.one * pinch;
            _ghost.position = item + _grabOffset; // pivot follows the pinch point
            // V12 (owner, Quest c15: "a red glove-shaped hint sits inside the book"): the glowing copy sat exactly on the real item while
            // the ghost hand reached in - it only shows once the hand has pinched and lifts it
            bool showCopy = k >= 0.3f && k < 0.95f; if (_ghost.gameObject.activeSelf != showCopy) _ghost.gameObject.SetActive(showCopy);
            var cam = Camera.main; if (cam != null) { var f = hand - cam.transform.position; f.y = 0; if (f.sqrMagnitude > 1e-4f) _hand.rotation = Quaternion.LookRotation(f.normalized); }
        }

        Vector3 _grabOffset;
    }
}
