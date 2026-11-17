using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V5 hero-loop readability layer (presentation only, never decides gameplay):
    ///  - every personal-job item wears a sticker in its owner's colour, so "whose is this?" is answered at a glance;
    ///  - the LOCAL player gets one bobbing 3D chevron: over their next item, or over the target while carrying one;
    ///  - a swiped item gets a tall colour beacon for the foil window ("go get it back!");
    ///  - sabotage / foil / rush moments get short spatial toasts + sounds, saboteurs show a tell above their head.
    /// </summary>
    public sealed class DutyVisuals : MonoBehaviour
    {
        RoomRuntime _room;
        RoundController _round;
        Transform _chevron;
        readonly Dictionary<int, Transform> _beacons = new Dictionary<int, Transform>();   // propId -> beacon
        readonly Dictionary<int, float> _beaconUntil = new Dictionary<int, float>();
        readonly List<int> _tmp = new List<int>();

        public static Color ColorFor(RoundController r, int slot)
        {
            if (r == null || !ActorIds.IsSibling(slot)) return Palette.Cream;
            int cos = r.Roster[slot].CosmeticId;
            return Palette.Siblings[((cos % 4) + 4) % 4];
        }

        public void Bind(RoomRuntime room, RoundController round)
        {
            if (_round != null) Unhook();
            _room = room; _round = round; _announced = false;
            foreach (var c in round.Board.Chores)
            {
                if (!c.IsBonus) continue;
                var col = ColorFor(round, c.BonusOwner);
                foreach (var s in c.Steps) Sticker(room.Prop(s.PropId), col, round.Roster[c.BonusOwner].DisplayName);
            }
            round.Board.Sabotaged += OnSabotaged;
            round.Board.SabotageFoiled += OnFoiled;
            round.RushStarted += OnRush;
            if (_chevron == null) { _chevron = BuildChevron(transform); _chevron.localScale = Vector3.one * 0.55f; }
            DutyDemo.Play(room, round, GameServices.LocalSlot); // 6 s ghost-hand demonstration of YOUR job (during the countdown)
        }

        void OnDestroy() => Unhook();

        void Unhook()
        {
            if (_round == null) return;
            _round.Board.Sabotaged -= OnSabotaged;
            _round.Board.SabotageFoiled -= OnFoiled;
            _round.RushStarted -= OnRush;
        }

        static void Sticker(PropBody p, Color c, string owner)
        {
            if (p == null || p.transform.Find("ownerSticker") != null) return;
            var b = new Bounds(p.transform.position, Vector3.zero);
            foreach (var r in p.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            var root = new GameObject("ownerSticker").transform;
            root.SetParent(p.transform, false);
            root.position = new Vector3(b.center.x, b.max.y + 0.004f, b.center.z);
            // Flat round sticker with a cream rim: readable from any angle above the item, costs 2 draw calls (shared mats).
            Prim.Shape(PrimitiveType.Cylinder, root, "rim", Vector3.zero, new Vector3(0.06f, 0.002f, 0.06f), Mats.Unlit(Palette.Cream));
            Prim.Shape(PrimitiveType.Cylinder, root, "dot", Vector3.up * 0.002f, new Vector3(0.046f, 0.002f, 0.046f), Mats.Unlit(c));
        }

        static Transform BuildChevron(Transform parent)
        {
            var t = new GameObject("myJobChevron").transform; t.SetParent(parent, false);
            var m = Mats.Unlit(Palette.Yellow);
            var l = Prim.Box(t, "l", new Vector3(-0.045f, 0.04f, 0), new Vector3(0.03f, 0.13f, 0.03f), m, false); l.transform.localRotation = Quaternion.Euler(0, 0, 35);
            var r = Prim.Box(t, "r", new Vector3(0.045f, 0.04f, 0), new Vector3(0.03f, 0.13f, 0.03f), m, false); r.transform.localRotation = Quaternion.Euler(0, 0, -35);
            t.gameObject.SetActive(false);
            return t;
        }

        bool _announced;

        /// <summary>The "what do I do?" moment: at GO, a big card in your colour floats in front of you naming YOUR job and its items.</summary>
        void AnnounceMyJob()
        {
            _announced = true;
            int me = GameServices.LocalSlot;
            if (!ActorIds.IsSibling(me) || !_round.Roster[me].IsActive) return;
            var duty = _round.Board.BonusFor(me); if (duty == null) return;
            var cam = Camera.main; if (cam == null) return;
            var fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized; if (fwd.sqrMagnitude < 0.1f) fwd = Vector3.forward;
            var col = ColorFor(_round, me);
            var kind = _round.Room.Prop(duty.Steps[0].PropId)?.Kind ?? "item";
            RoomFx.Say(cam.transform.position + fwd * 1.3f + Vector3.down * 0.05f, $"YOUR JOB\n<size=70%>{duty.Title} ({duty.Steps.Count}x {kind.Replace('_', ' ')})\nRivals may swipe it - grab it back!\nQuiet: Mom is asleep next door</size>", col, 5f, 1.0f);
            AudioBank.I?.Play2D("chore_done", 0.5f);
            foreach (var st in duty.Steps) RoomFx.Burst("toon_impact", (_room.Prop(st.PropId)?.transform.position ?? Vector3.zero) + Vector3.up * 0.15f, 0.12f, 1.2f);
        }

        void Update()
        {
            if (_round == null || _room == null) return;
            if (!_announced && _round.Phase == RoundPhase.Playing) AnnounceMyJob();
            UpdateChevron();
            UpdateBeacons();
        }

        void UpdateChevron()
        {
            int me = GameServices.LocalSlot;
            var duty = ActorIds.IsSibling(me) ? _round.Board.BonusFor(me) : null;
            bool show = duty != null && _round.IsPlayable && _round.Roster[me].IsActive && duty.State != ChoreState.Completed;
            Vector3 target = default;
            if (show)
            {
                int held = _round.HeldBy(me);
                var heldChore = held >= 0 ? _round.Board.ChoreForProp(held) : null;
                if (heldChore == duty)
                {
                    float best = float.MaxValue; show = false;
                    foreach (var s in duty.Steps)
                    {
                        if (s.Done) continue;
                        var a = _room.Anchor(s.AnchorId); if (a == null) continue;
                        float d = (a.transform.position - _room.ActorTransformOrHead(me)).sqrMagnitude;
                        if (d < best) { best = d; target = a.transform.position + Vector3.up * 0.32f; show = true; }
                    }
                }
                else
                {
                    float best = float.MaxValue; show = false;
                    foreach (var s in duty.Steps)
                    {
                        if (s.Done) continue;
                        var p = _room.Prop(s.PropId); if (p == null || p.IsHeld) continue;
                        float d = (p.transform.position - _room.ActorTransformOrHead(me)).sqrMagnitude;
                        if (d < best) { best = d; target = p.transform.position + Vector3.up * 0.3f; show = true; }
                    }
                }
            }
            if (_chevron.gameObject.activeSelf != show) _chevron.gameObject.SetActive(show);
            if (!show) return;
            var camT = Camera.main;
            if (camT != null && (camT.transform.position - target).sqrMagnitude < 0.85f * 0.85f) { _chevron.gameObject.SetActive(false); return; } // you're there: don't cover it
            _chevron.position = target + Vector3.up * (0.18f + Mathf.Sin(Time.time * 4f) * 0.03f);
            var cam = Camera.main;
            if (cam != null) { var f = cam.transform.position - _chevron.position; f.y = 0; if (f.sqrMagnitude > 1e-4f) _chevron.rotation = Quaternion.LookRotation(-f); }
        }

        void UpdateBeacons()
        {
            if (_beacons.Count == 0) return;
            _tmp.Clear();
            foreach (var kv in _beacons)
            {
                var p = _room.Prop(kv.Key);
                if (p == null || kv.Value == null || Time.time > _beaconUntil[kv.Key]) { _tmp.Add(kv.Key); continue; }
                kv.Value.position = p.transform.position;
                kv.Value.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 6f) * 0.08f, 1f);
            }
            foreach (int id in _tmp) ClearBeacon(id);
        }

        void ClearBeacon(int propId)
        {
            if (_beacons.TryGetValue(propId, out var b) && b != null) Destroy(b.gameObject);
            _beacons.Remove(propId); _beaconUntil.Remove(propId);
        }

        void OnSabotaged(SabotageRecord r)
        {
            var p = _room.Prop(r.PropId);
            var pos = p != null ? p.transform.position : Vector3.zero;
            string thief = _round.Roster[r.Saboteur].DisplayName, victim = _round.Roster[r.Victim].DisplayName;
            var col = ColorFor(_round, r.Victim);
            // The owner's item beams a column of its colour until the foil window closes: clear, physical counterplay target.
            if (p != null && !_beacons.ContainsKey(r.PropId))
            {
                var b = new GameObject("swipeBeacon").transform; b.SetParent(transform, false);
                Prim.Shape(PrimitiveType.Cylinder, b, "beam", new Vector3(0, 0.75f, 0), new Vector3(0.025f, 0.7f, 0.025f), Mats.Unlit(col));
                Prim.Shape(PrimitiveType.Sphere, b, "bang", new Vector3(0, 1.5f, 0), Vector3.one * 0.08f, Mats.Unlit(col));
                _beacons[r.PropId] = b; _beaconUntil[r.PropId] = Time.time + ChoreBoard.FoilWindow;
            }
            if (r.Victim == GameServices.LocalSlot)
            {
                RoomFx.Say(pos + Vector3.up * 0.45f, $"{thief} swiped your item!", col, 2.4f, 0.75f);
                AudioBank.I?.Play2D("trap_set", 0.8f);
            }
            else RoomFx.Say(pos + Vector3.up * 0.45f, $"{thief} swiped {victim}'s item", Palette.Purple, 1.6f, 0.5f);
            _room.AvatarFor(r.Saboteur)?.ShowStatus("hehe!", 1.4f);
            RoomFx.Burst("puff", pos + Vector3.up * 0.15f, 0.22f, 1.6f);
            if (!_momCommentedSabotage && _round.Mom.State == MomState.Sleeping) { _momCommentedSabotage = true; _room.Mom?.Say(Random.value < 0.5f ? "mom_sabotage_1" : "mom_sabotage_2", null); }
        }
        bool _momCommentedSabotage;

        void OnFoiled(SabotageRecord r)
        {
            ClearBeacon(r.PropId);
            var p = _room.Prop(r.PropId);
            var pos = p != null ? p.transform.position : Vector3.zero;
            RoomFx.Say(pos + Vector3.up * 0.5f, $"SAVED! +{ScoreRules.SavePoints:0}", Palette.Ok, 2f, 0.8f);
            RoomFx.Burst("stars", pos + Vector3.up * 0.4f, 0.3f, 3f);
            _room.AvatarFor(r.Saboteur)?.ShowStatus($"Caught! -{ScoreRules.CaughtPenalty:0}", 2.2f);
            AudioBank.I?.Play("chore_done", pos, 0.7f, 0.02f, AudioBus.UI);
        }

        void OnRush(int slot)
        {
            string who = _round.Roster[slot].DisplayName;
            var head = _room.ActorTransformOrHead(GameServices.LocalSlot);
            string msg = slot == GameServices.LocalSlot ? "Your job is done! Now guard it!" : $"{who} finished! {Mathf.CeilToInt(_round.TimeRemaining)}s left!";
            var fwd = Camera.main != null ? Camera.main.transform.forward : Vector3.forward; fwd.y = 0;
            RoomFx.Say(head + fwd.normalized * 1.2f, msg, ColorFor(_round, slot), 2.6f, 0.8f);
            AudioBank.I?.Play2D("countdown", 0.7f);
            _room.Mom?.Say(Random.value < 0.5f ? "mom_rush_1" : "mom_rush_2", null);
        }
    }
}
