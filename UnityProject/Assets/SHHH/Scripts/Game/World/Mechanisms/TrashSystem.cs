using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 trash (owner: "trash anywhere - find it, bin it"). Litter is scattered by <see cref="HouseClutter"/> (seeded, same on every peer);
    /// it is ordinary physics props. A piece counts as binned only when the HOST sees it resting inside a real bin (the lidded cans,
    /// lid lifted by hand) - then a journaled <c>TrashBinned</c> credits whoever dropped it there. Clients only see the result.
    /// </summary>
    public sealed class TrashSystem : MonoBehaviour
    {
        public static TrashSystem I { get; private set; }
        RoomRuntime _room;
        readonly List<PropBody> _trash = new List<PropBody>();
        readonly Dictionary<PropBody, float> _rest = new Dictionary<PropBody, float>();
        public int Total => _trash.Count;
        public int Left { get { var r = GameServices.Round; int n = 0; foreach (var t in _trash) if (t != null && (r == null || !r.Binned.Contains(t.Spec.Id))) n++; return n; } }

        public void Bind(RoomRuntime room)
        {
            I = this; _room = room; _trash.Clear();
            foreach (var p in room.Def.Props) if (HouseClutter.IsTrash(p.Kind)) { var b = room.Prop(p.Id); if (b != null) _trash.Add(b); }
        }

        void OnDestroy() { if (I == this) I = null; }

        public static bool InAnyBin(Vector3 world)
        {
            foreach (var f in MechFurniture.All) if (f != null && f.Kind == "bin" && f.InsideBin(world)) return true;
            return false;
        }

        void FixedUpdate()
        {
            var round = GameServices.Round;
            if (!GameServices.IsAuthority || round == null || !round.IsPlayable) return;
            foreach (var t in _trash)
            {
                if (t == null || round.Binned.Contains(t.Spec.Id)) continue;
                if (t.IsHeld || !InAnyBin(t.Col.bounds.center)) { _rest.Remove(t); continue; }
                _rest.TryGetValue(t, out var s); s = t.Speed < 0.3f ? s + Time.fixedDeltaTime : 0f; _rest[t] = s;
                if (s < 0.3f) continue;
                round.TrashBinned(t.LastActor, t.Spec.Id); _rest.Remove(t);
                if (AudioBank.I != null) AudioBank.I.Play("rustle", t.Col.bounds.center, 0.5f);
                RoomFx.Burst("sparkle", t.Col.bounds.center + Vector3.up * 0.15f, 0.15f, 1.2f);
            }
        }
    }
}
