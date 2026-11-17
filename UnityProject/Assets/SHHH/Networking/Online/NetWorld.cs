using Fusion;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// Host physics -> clients: every prop pose + holder and every host-driven body (bots, cat, dog) for the current room.
    /// Clients show props kinematically (smoothed), except an item the local hand is holding (client-side prediction).
    /// </summary>
    public sealed class NetWorld : NetworkBehaviour
    {
        public static NetWorld I { get; private set; }
        public const int MaxProps = 96, MaxAgents = 6;   // V12: + scattered trash, pet bowls / food (was 48)
        [Networked] public int RoomKey { get; set; }  // (round index + 1) * 16 + room id; 0 = no room
        [Networked, Capacity(MaxProps)] public NetworkArray<NetPropPose> Props => default;
        [Networked, Capacity(MaxAgents)] public NetworkArray<NetAgentPose> Agents => default;
        public const int MaxPuddles = 16, MaxFixtureLiquids = 4;
        /// <summary>V12 liquids: puddles and appliance trays (washer detergent compartment) - host-simulated, clients only show them.</summary>
        [Networked, Capacity(MaxPuddles)] public NetworkArray<NetPuddle> Puddles => default;
        [Networked, Capacity(MaxFixtureLiquids)] public NetworkArray<short> FixtureMl => default;

        public override void Spawned() { I = this; Runner.MakeDontDestroyOnLoad(gameObject); GameServices.HostStreamsLiquids = !HasStateAuthority; }
        public override void Despawned(NetworkRunner runner, bool hasState) { if (I == this) I = null; GameServices.HostStreamsLiquids = false; }

        public static int KeyFor(RoundController r) => r == null ? 0 : (r.Config.Index + 1) * 16 + (int)r.Config.Room;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            var room = GameServices.Room; var round = GameServices.Round;
            if (room == null || round == null || GameServices.Match == null) { RoomKey = 0; return; }
            RoomKey = KeyFor(round);
            var def = room.Def;
            for (int i = 0; i < def.Props.Count && i < MaxProps; i++)
            {
                var p = room.Prop(def.Props[i].Id); if (p == null) continue;
                var lc = Vessels.Of(p);
                Props.Set(i, new NetPropPose { P = p.transform.position, R = p.transform.rotation, Holder = (sbyte)Mathf.Clamp(round.HolderOf(def.Props[i].Id), -1, 120), Ml = (short)(lc != null ? Mathf.Clamp(Mathf.RoundToInt(lc.Amount * 1000f), 0, 32000) : 0) });
            }
            for (int s = 0; s < 4; s++)
            {
                var b = room.BodyFor(s);
                if (b == null) { Agents.Set(s, default); continue; }
                Agents.Set(s, new NetAgentPose { P = b.transform.position, Yaw = b.transform.eulerAngles.y, Speed = b.CurrentSpeed, Carry = b.HeldProp >= 0, Visible = b.gameObject.activeInHierarchy, Plot = Plotting(room, s) });
            }
            for (int i = 0; i < MaxPuddles; i++)
            {
                var pd = i < Puddle.All.Count ? Puddle.All[i] : null;
                Puddles.Set(i, pd != null ? new NetPuddle { P = pd.transform.position, N = pd.Normal, Litres = pd.Litres, MaxR = pd.MaxRadius, Granular = pd.Granular } : default);
            }
            for (int i = 0; i < MaxFixtureLiquids; i++) { var wa = i < WasherAppliance.All.Count ? WasherAppliance.All[i] : null; FixtureMl.Set(i, (short)(wa != null && wa.Detergent != null ? Mathf.RoundToInt(wa.Detergent.Amount * 1000f) : 0)); }
            var cat = room.PetFor(PetKind.Cat); var dog = room.PetFor(PetKind.Dog);
            if (cat) Agents.Set(4, new NetAgentPose { P = cat.transform.position, Yaw = cat.transform.eulerAngles.y, Visible = true });
            if (dog) Agents.Set(5, new NetAgentPose { P = dog.transform.position, Yaw = dog.transform.eulerAngles.y, Visible = true });
        }

        /// <summary>Holder slot of a prop as last replicated (-1 none).</summary>
        public int HolderOf(int propId)
        {
            var room = GameServices.Room; if (room == null) return -1;
            var def = room.Def;
            for (int i = 0; i < def.Props.Count && i < MaxProps; i++) if (def.Props[i].Id == propId) return Props[i].Holder;
            return -1;
        }

        public override void Render()
        {
            if (HasStateAuthority) return;
            var room = GameServices.Room; var round = GameServices.Round;
            if (room == null || round == null || RoomKey != KeyFor(round)) return; // host is still in another room
            var def = room.Def; float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
            for (int i = 0; i < def.Props.Count && i < MaxProps; i++)
            {
                var p = room.Prop(def.Props[i].Id); if (p == null || p.Body == null) continue;
                if (!p.Body.isKinematic) { p.Body.isKinematic = true; p.Body.interpolation = RigidbodyInterpolation.None; }
                if (p.IsHeld && p.HeldBy == GameServices.LocalSlot) continue; // our hand drives it (prediction)
                var s = Props[i];
                if (s.R.x == 0 && s.R.y == 0 && s.R.z == 0 && s.R.w == 0) continue; // not written yet
                var pos = (p.transform.position - s.P).sqrMagnitude > 1f ? s.P : Vector3.Lerp(p.transform.position, s.P, k);
                var rot = Quaternion.Slerp(p.transform.rotation, s.R, k);
                p.transform.SetPositionAndRotation(pos, rot); p.Body.position = pos; p.Body.rotation = rot;
                var lc = Vessels.Of(p); if (lc != null) { lc.Replica = true; lc.Amount = Mathf.Min(lc.Capacity, s.Ml / 1000f); }
            }
            // held by our own hand: still the host's volume
            for (int i = 0; i < def.Props.Count && i < MaxProps; i++) { var p = room.Prop(def.Props[i].Id); var lc = p != null ? Vessels.Of(p) : null; if (lc != null && p.IsHeld && p.HeldBy == GameServices.LocalSlot) { lc.Replica = true; lc.Amount = Mathf.Min(lc.Capacity, Props[i].Ml / 1000f); } }
            for (int i = 0; i < MaxPuddles; i++) { var s = Puddles[i]; Puddle.SyncFromHost(i, s.P, s.N, s.Litres, s.MaxR, s.Granular); }
            for (int i = 0; i < MaxFixtureLiquids && i < WasherAppliance.All.Count; i++) { var wa = WasherAppliance.All[i]; if (wa != null && wa.Detergent != null) { wa.Detergent.Replica = true; wa.Detergent.Amount = FixtureMl[i] / 1000f; } }
            for (int a = 0; a < MaxAgents; a++)
            {
                var s = Agents[a]; if (!s.Visible) continue;
                var pup = room.PuppetFor(a < 4 ? a : (a == 4 ? ActorIds.Cat : ActorIds.Dog));
                if (pup != null) { pup.SetTarget(s.P, s.Yaw, s.Speed, s.Carry); if (a < 4 && pup.Avatar != null) pup.Avatar.SetPlotting(s.Plot); }
            }
        }

        static bool Plotting(RoomRuntime room, int slot)
        {
            foreach (var br in room.Brains) if (br.Slot == slot) return br.Plotting;
            return false;
        }
    }
}
