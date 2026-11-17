#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// `recipe_diag [recipeId|all]` (V7 Gate 4): plays recipe rounds in the production room build with simulated hands on the real
    /// Boot rig. For each recipe it asks the solver for the next step on a COPY of the board, then performs that step only through
    /// hand poses (move / pinch / release / poke / rub) — no direct rule calls — and checks the authoritative board after every
    /// action. Then a rival seat physically sabotages it and the owner physically repairs it (foil). Desktop-simulated evidence;
    /// headset UNVERIFIED.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class RecipeDiagnostic : MonoBehaviour
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static string Flag => Root + "/Automation/recipediag.flag";
        static string Dir => Root + "/QA_Evidence/recipe_v7";
        [Serializable] class Check { public string recipe, name, detail; public bool pass; }
        [Serializable] class Report { public string harness = "Boot rig / SimHandSource / HandInteractor / PropBody / RecipeFixtureView / RecipeRuntime / RoundController.RecipeAction"; public string hardware = "UNVERIFIED: desktop-simulated hands, no Quest"; public string capturedUtc; public int pass, fail; public List<string> recipes = new List<string>(); public List<Check> checks = new List<Check>(); public List<string> events = new List<string>(); }
        readonly Report _rep = new Report();
        PlayerRig _rig; RoomRuntime _room; RoundController _round; RecipeRuntime _rt; SimHandSource _l, _r; Camera _cam;
        Vector3 _headPos; Quaternion _headRot; bool _done; float _start; string _cur = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() { if (!File.Exists(Flag)) return; var go = new GameObject("V7_RecipeDiagnostic"); DontDestroyOnLoad(go); go.AddComponent<RecipeDiagnostic>(); }

        void Start() { _start = Time.realtimeSinceStartup; Directory.CreateDirectory(Dir); StartCoroutine(Safe(Run())); }
        void Update() { if (_round != null && !_done) _round.Tick(Time.deltaTime); if (!_done && Time.realtimeSinceStartup - _start > _budget) { Ck("timeout", false, $"{_budget:0} s"); Finish(); } }
        void LateUpdate() { if (_rig && _rig.Head) _rig.Head.SetPositionAndRotation(_headPos, _headRot); }

        IEnumerator Safe(IEnumerator e)
        {
            while (true)
            {
                object cur;
                try { if (!e.MoveNext()) yield break; cur = e.Current; }
                catch (Exception ex) { Ck("exception", false, ex.ToString()); Finish(); yield break; }
                yield return cur;
            }
        }

        void Ck(string n, bool ok, string d) { _rep.checks.Add(new Check { recipe = _cur, name = n, pass = ok, detail = d }); Debug.Log($"[SHHH][recipe_diag] {(ok ? "PASS" : "FAIL")} {_cur} {n} {d}"); }

        IEnumerator Run()
        {
            float wait = 0; while ((GameRoot.I == null || GameRoot.I.State != AppState.FrontEnd) && wait < 60) { wait += Time.deltaTime; yield return null; }
            if (File.ReadAllText(Flag).Trim() == "stage") { yield return Safe(Stage()); Finish(); yield break; }
            GameRoot.I.StartPractice(); wait = 0; while (GameRoot.I.State != AppState.Practice && wait < 40) { wait += Time.deltaTime; yield return null; }
            var root = GameRoot.I; _rig = root.Rig; root.enabled = false; if (root.Room) Destroy(root.Room.gameObject); yield return null;
            var ovr = _rig.GetComponent<OVRCameraRig>(); if (ovr) ovr.enabled = false;
            _l = new GameObject("QA_Sim_Left").AddComponent<SimHandSource>(); _l.Hand = Handedness.Left;
            _r = new GameObject("QA_Sim_Right").AddComponent<SimHandSource>(); _r.Hand = Handedness.Right;
            _rig.Left = _l; _rig.Right = _r; _rig.LeftHand.Source = _l; _rig.RightHand.Source = _r;
            if (UiPointer.I) { UiPointer.I.Rebind(0, _l, _rig.LeftHand); UiPointer.I.Rebind(1, _r, _rig.RightHand); }
            PokeButton.Pokers.Clear(); PokeButton.Pokers.Add(_l); PokeButton.Pokers.Add(_r);
            _cam = _rig.Head.GetComponent<Camera>();
            string arg = File.ReadAllText(Flag).Trim();
            if (arg.EndsWith("!")) { _opShots = true; RecipeRuntime.DebugJoins = true; arg = arg.TrimEnd('!'); } // "LD07!" = a screenshot after every hand action
            if (arg == "stations") { yield return Safe(AuditStations()); Finish(); yield break; }
            if (arg == "surfaces12") { yield return Safe(SurfaceProbe()); Finish(); yield break; }
            if (arg == "ui12") { _budget = 200f; yield return Safe(UiLab()); Finish(); yield break; }
            if (arg == "grabs") { _budget = 300f; yield return Safe(GrabsLab()); Finish(); yield break; }
            if (arg == "hands") { _budget = 200f; yield return Safe(HandsLab()); Finish(); yield break; }
            if (arg == "wardrobe_spam") { _budget = 400f; yield return Safe(WardrobeSpamLab()); Finish(); yield break; }
            if (arg == "scatter") { _budget = 300f; yield return Safe(ScatterLab()); Finish(); yield break; }
            if (arg.StartsWith("probe:")) { yield return Safe(Probe(arg.Substring(6))); Finish(); yield break; }
            if (arg.StartsWith("look:")) { yield return Safe(Look(arg.Substring(5))); Finish(); yield break; }
            if (arg == "water") { yield return Safe(WaterShots()); Finish(); yield break; }
            if (arg == "hints13") { _budget = 300f; yield return Safe(HintsLab()); Finish(); yield break; }
            if (arg == "pets") { _budget = 600f; yield return Safe(PetLab()); Finish(); yield break; }
            if (arg == "avatars") { _budget = 420f; yield return Safe(AvatarLab()); Finish(); yield break; }
            if (arg == "laundry") { _budget = 900f; foreach (var id in new[] { "LD08", "LD09", "LD05" }) yield return Safe(OneRecipe(RoomId.Laundry, id)); yield return Safe(FurnitureLab()); Finish(); yield break; }
            if (arg == "washer") { _budget = 600f; yield return Safe(OneRecipe(RoomId.Laundry, "LD04")); yield return Safe(WasherLab()); Finish(); yield break; }
            if (arg == "surfaces") { yield return Safe(FindSurfaces()); Finish(); yield break; }
            if (arg == "practice12") { _budget = 120f; yield return Safe(PracticeLab()); Finish(); yield break; }
            if (arg == "grip12") { _budget = 60f; yield return Safe(GripLab()); Finish(); yield break; }
            if (arg == "stable12") { _budget = 200f; yield return Safe(StableLab()); Finish(); yield break; }
            if (arg == "stage12") { _budget = 260f; yield return Safe(StageLab()); Finish(); yield break; }
            if (arg == "teleport12") { _budget = 200f; yield return Safe(TeleportLab()); Finish(); yield break; }
            if (arg == "walls12") { _budget = 200f; yield return Safe(WallsLab()); Finish(); yield break; }
            if (arg == "props12") { _budget = 300f; yield return Safe(PropsLab()); Finish(); yield break; }
            if (arg == "mom12") { _budget = 400f; yield return Safe(MomLab()); Finish(); yield break; }
            if (arg == "confront") { yield return Safe(Confront()); Finish(); yield break; }
            if (arg == "siblings") { yield return Safe(Siblings()); Finish(); yield break; }
            var targets = new List<(RoomId room, string id)>();
            var rooms = new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry };
            foreach (var rm in rooms)
                foreach (var d in RecipeCatalog.All.Where(d => RecipeLayout.AreasHostedBy(rm).Contains(d.Area) && RecipeLayout.Stations(rm).Any(s => RecipeLayout.Fits(d, s))))
                {
                    var toks = arg.Split(',');
                    // tokens: "all", a recipe id (KT10), an id prefix (LD, BT), or a room name (Laundry = everything staged there)
                    if (arg == "all" || toks.Contains(d.Id) || toks.Any(t => t.Length == 2 && d.Id.StartsWith(t)) || (arg == "1" && targets.Count == 0) || toks.Contains(rm.ToString())) targets.Add((rm, d.Id));
                }
            _budget = Mathf.Max(900f, targets.Count * 75f);
            foreach (var t in targets) { yield return Safe(OneRecipe(t.room, t.id)); }
            Finish();
        }

        RoundController MakeRound(RoomId rm, string id, out ulong seed)
        {
            for (seed = 1; seed < 3000; seed++)
            {
                var roster = new CompetitorRoster();
                roster.SeatHuman(0, "Pip", 0); roster.SeatHuman(1, "Rory", 2); roster.SeatHuman(2, "Nell", 1); roster.SeatHuman(3, "Tess", 3); roster.LockForMatch();
                // single-mechanism labs test one job at its station: the house-wide scatter is off here (the house runs test it)
                var cfg = RoundConfig.PracticeConfig(); cfg.Room = rm; cfg.RecipeMode = true; cfg.CountdownSeconds = 0.2f; cfg.ScatterChoreProps = false;
                var r = new RoundController(cfg, roster, seed);
                if (r.Recipes != null && r.Recipes.Instances.Any(i => i.Owner == 0 && i.Def.Id == id)) return r;
            }
            return null;
        }

        IEnumerator OneRecipe(RoomId rm, string id)
        {
            _cur = id; _rep.recipes.Add(id); ImpactGate.ResetStats();
            if (_room) { Destroy(_room.gameObject); yield return null; yield return Resources.UnloadUnusedAssets(); GC.Collect(); } // each recipe rebuilds the house: free the last one (QAReporter aborts above 4 GB)
            // partial result after every recipe so an aborted session still leaves evidence
            try { File.WriteAllText(Path.Combine(Root, "Automation", "recipe_diag_partial.json"), JsonUtility.ToJson(_rep, true)); } catch { }
            _round = MakeRound(rm, id, out var seed);
            if (_round == null) { Ck("seed", false, "no seed gives slot 0 this recipe"); yield break; }
            _round.Begin();
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = true;
            _room = new GameObject("V7_Recipe_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, seed); _room.enabled = false; _round.Pets.Clear();
            _round.Log.Sink = line => _rep.events.Add(id + " " + line);
            _rt = _room.GetComponent<RecipeRuntime>();
            float w = 0; while (!_round.IsPlayable && w < 5) { w += Time.deltaTime; yield return null; }
            var inst = _round.Recipes.Instances.First(i => i.Owner == 0);
            Ck("runtime_bound", _rt != null && _rt.Fixtures.Count == _round.RecipeFixtures.Count && _round.IsPlayable, $"seed {seed}; fixtures {_rt?.Fixtures.Count}; playable {_round.IsPlayable}");
            // stand in front of the station, child eye height
            var pl0 = _round.RecipeFixtures.First(f => f.Instance == inst.Index);
            var st = RecipeLayout.Stations(rm)[pl0.Station];
            var toPlayer = VisualFactory.V(st.Local(0, st.Front * 1f) - st.Center).normalized;
            var centre = _room.transform.TransformPoint(VisualFactory.V(st.Center));
            _rig.Spawn(centre + toPlayer * 0.55f - Vector3.up * centre.y, Quaternion.LookRotation(-toPlayer).eulerAngles.y);
            _headPos = centre + toPlayer * 0.5f + Vector3.up * 0.62f; _headRot = Quaternion.LookRotation(centre + Vector3.up * 0.05f - _headPos);
            Park();
            yield return new WaitForSeconds(0.6f);
            Shot(id + "_0_start");
            // ---- owner completes it
            float before = _round.Scores[0].Total;
            var sfx0 = new Dictionary<string, int>(AudioBank.Played);
            yield return Safe(Complete(inst, 0, 40));
            Ck("complete_by_hands", inst.Complete, $"{inst.StepsDone}/{inst.Def.Steps.Count} steps; {Snap(inst)}; walked {Walks} times / {WalkedMetres:0.0} m");
            Ck("scored", _round.Scores[0].Total > before, $"{before:0.0} -> {_round.Scores[0].Total:0.0}");
            // careful hands should not produce physics-artefact crashes (> 30 loudness) while doing the job
            var loud = _rep.events.Where(e => e.StartsWith(id + " ") && e.Contains(" noise N")).Select(e => { var i = e.IndexOf(" v=", StringComparison.Ordinal); float v = 0; if (i > 0) float.TryParse(e.Substring(i + 3).Split(' ')[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v); return (v, e); }).Where(x => x.v > 30f).ToList();
            Ck("no_artefact_crashes", loud.Count == 0, loud.Count == 0 ? "max loudness ok" : string.Join(" | ", loud.Take(3).Select(x => x.e.Length > 140 ? x.e.Substring(0, 140) : x.e)));
            yield return new WaitForSeconds(0.3f); // clipboard sounds poll at 4 Hz
            var heard = AudioBank.Played.Where(kv => kv.Value > (sfx0.TryGetValue(kv.Key, out var v0) ? v0 : 0)).Select(kv => $"{kv.Key}x{kv.Value - (sfx0.TryGetValue(kv.Key, out var v1) ? v1 : 0)}").ToList();
            Ck("feedback_sfx", heard.Any(h => h.StartsWith("chore_done")) && heard.Any(h => h.StartsWith("slot_click") || h.StartsWith("join_pop") || h.StartsWith("wipe") || h.StartsWith("lid_") || h.StartsWith("ui_click") || h.StartsWith("rustle") || h.StartsWith("pour")), string.Join(", ", heard));
            var card = _room.GetComponentsInChildren<RecipeClipboard>().FirstOrDefault(c => c.Instance == inst);
            Ck("clipboard_done", card != null && card.Text.Contains("DONE"), card != null ? card.Text.Replace("\n", " | ") : "no card");
            Shot(id + "_1_done");
            if (!inst.Complete) { Ck("impact_policy", true, ImpactGate.Report()); yield break; }
            // ---- rival (seat 1) physically disturbs it: take out a placed item / open the lid / pull a pair apart / press a control
            yield return new WaitForSeconds(0.5f);
            int sabBefore = _round.Recipes.Sabotage.Count;
            GameServices.LocalSlot = 1;
            string how = null; yield return Safe(Disturb(inst, s => how = s));
            yield return new WaitForSeconds(0.6f);
            var rec = _round.Recipes.Sabotage.Skip(sabBefore).FirstOrDefault(r => r.Instance == inst.Index && r.Saboteur == 1);
            Ck("rival_sabotage", rec != null && !inst.Complete, $"{how}; record: {rec}");
            Shot(id + "_2_sabotaged");
            // ---- owner repairs (foil)
            GameServices.LocalSlot = 0;
            yield return new WaitForSeconds(RecipeBoard.RepairProtection * 0f + 0.3f);
            yield return Safe(Complete(inst, 0, 30));
            Ck("owner_repair_foil", inst.Complete && rec != null && rec.Foiled, $"complete {inst.Complete}; foiled {rec?.Foiled}");
            Shot(id + "_3_repaired");
            Ck("impact_policy", true, ImpactGate.Report() + $"; impact_ceramic sounds {(AudioBank.Played.TryGetValue("impact_ceramic", out var ic) ? ic : 0) - (sfx0.TryGetValue("impact_ceramic", out var ic0) ? ic0 : 0)}");
            if (_rt.Fixtures.Any(f => f.Instance == inst && f.Seating != null)) yield return Safe(RackLab(inst));
        }

        // ------------------------------------------------------------------ V10 rack lab (KT01)
        float MaxNoiseSince(int evIndex) { float m = 0f; for (int i = evIndex; i < _rep.events.Count; i++) { var e = _rep.events[i]; if (!e.Contains(" noise N")) continue; int k = e.IndexOf(" v=", StringComparison.Ordinal); if (k > 0 && float.TryParse(e.Substring(k + 3).Split(' ')[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) m = Mathf.Max(m, v); } return m; }
        int NoiseEventsSince(int evIndex) { int n = 0; for (int i = evIndex; i < _rep.events.Count; i++) if (_rep.events[i].Contains(" noise N")) n++; return n; }

        /// <summary>Hold a plate on edge over a chosen gap with its rim <paramref name="above"/> over the tray floor, release, wait.
        /// <paramref name="flat"/> = release it lying flat (wrong angle: must NOT count as racked).</summary>
        IEnumerator RackDrop(PropBody b, RecipeFixtureView fv, int gap, float above, bool flat)
        {
            var h = HandHolding(b); if (h == null) { yield return Safe(WalkTo(b.Col.bounds.center)); yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            yield return Safe(WalkTo(fv.transform.position));   // V10: carry it to the rack like a player, no 2 m arm
            var right = fv.transform.right;
            for (int k = 0; k < 3; k++)
            {
                var tgt = flat ? Vector3.up : (Vector3.Dot(b.transform.up, right) >= 0 ? right : -right);
                if (Vector3.Angle(b.transform.up, tgt) < 15f) break;
                var r0 = h.transform.rotation; var r1 = Quaternion.FromToRotation(b.transform.up, tgt) * r0;
                for (float t = 0; t < 1; t += Time.deltaTime / 0.4f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; }
                h.transform.rotation = r1; yield return new WaitForSeconds(0.15f);
            }
            var floor = fv.transform.TransformPoint(new Vector3(fv.Seating.Gaps[gap], fv.transform.InverseTransformPoint(fv.Seating.FloorWorld(0f)).y, fv.Seating.SupportZ));
            yield return Safe(Drop(h, b, new Vector3(floor.x, floor.y + b.Col.bounds.extents.y + above - 0.012f, floor.z)));
            var rp = _rt.RProp(b); float tw = 0f;
            while (tw < 2.5f && rp != null && !rp.IsPlaced) { tw += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator RackLab(RecipeInstance inst)
        {
            string saved = _cur; _cur = inst.Def.Id + "_rack";
            var fv = _rt.Fixtures.First(f => f.Instance == inst && f.Seating != null);
            var plates = _rt.Bodies.Where(x => { var r = _rt.RProp(x); return r != null && r.RecipeInstance == inst.Index && r.Kind == "plate"; }).ToList();
            Ck("rack_geometry", fv.Seating.Gaps.Count >= 3 && fv.Seating.GapClearWidth > 0.035f, $"{fv.Seating.Gaps.Count} gaps, clear {fv.Seating.GapClearWidth * 1000f:0.0} mm, plate collider {plates.FirstOrDefault()?.Col.GetType().Name} {plates.FirstOrDefault()?.Col.bounds.size:F3}");
            int mid = fv.Seating.Gaps.Count / 2;
            int[] gaps = { mid - 2, mid + 2, mid, mid - 1, mid + 1 };
            int inserted = 0;
            // 1. take both out, then 5 sequential insertions into different gaps, alternating plates, removing the older one each time
            // (V10 rev: no trips back to the table - a player lifts a plate out and sets it into another gap)
            yield return Safe(WalkTo(fv.transform.position));
            for (int i = 0; i < gaps.Length; i++)
            {
                var p = plates[i % plates.Count]; var rp = _rt.RProp(p);
                if (rp.IsPlaced) { yield return Safe(Grab(p)); if (HandHolding(p) != null) yield return Safe(Move(HandHolding(p), HandHolding(p).transform.position + Vector3.up * 0.15f, 0.4f)); }
                Ck($"rack_removed_{i}", !rp.IsPlaced, $"plate#{p.Spec.Id} out of the rack before insertion {i}");
                int ev = _rep.events.Count; var s0 = ImpactGate.Stats; int snd0 = AudioBank.Played.TryGetValue("impact_ceramic", out var a0) ? a0 : 0; float t0 = Time.time;
                yield return Safe(RackDrop(p, fv, Mathf.Clamp(gaps[i], 0, fv.Seating.Gaps.Count - 1), 0.015f, false));
                bool seated = rp.IsPlaced && fv.Seating.StandsInGap(p, out int g) && g == Mathf.Clamp(gaps[i], 0, fv.Seating.Gaps.Count - 1);
                int snd = (AudioBank.Played.TryGetValue("impact_ceramic", out var a1) ? a1 : 0) - snd0;
                if (seated) inserted++;
                Ck($"rack_insert_{i}_gap{gaps[i]}", seated && snd <= 3, $"seated {seated} in {Time.time - t0:0.00} s; {fv.Seating.Describe(p)}; ceramic sounds {snd}; noise events {NoiseEventsSince(ev)} (max {MaxNoiseSince(ev):0.0}); contacts {ImpactGate.Stats.Contacts - s0.Contacts}, accepted {ImpactGate.Stats.Accepted - s0.Accepted}, rattle {ImpactGate.Stats.Rattle - s0.Rattle}");
                if (i == 1) Shot(saved + "_rack_two_plates");
            }
            Ck("rack_three_plus_sequential", inserted >= 3, $"{inserted}/{gaps.Length} physical insertions seated");
            // 2. quiet vs loud: the same plate set in gently vs dropped from 30 cm
            var pq = plates[0]; var rq = _rt.RProp(pq);
            if (rq.IsPlaced) { yield return Safe(Grab(pq)); yield return Safe(Move(HandHolding(pq), HandHolding(pq).transform.position + Vector3.up * 0.15f, 0.4f)); }
            int evq = _rep.events.Count; yield return Safe(RackDrop(pq, fv, mid - 2, 0.01f, false)); float quiet = MaxNoiseSince(evq); bool quietSeated = rq.IsPlaced;
            yield return Safe(Grab(pq)); yield return Safe(Move(HandHolding(pq), HandHolding(pq).transform.position + Vector3.up * 0.15f, 0.4f));
            int evl = _rep.events.Count; yield return Safe(RackDrop(pq, fv, mid - 2, 0.30f, false)); float loud = MaxNoiseSince(evl); int loudEvents = NoiseEventsSince(evl);
            Shot(saved + "_rack_after_loud_drop");
            Ck("rack_quiet_vs_loud", loud > quiet + 3f && loudEvents <= 3, $"gentle set-in: max noise {quiet:0.0} (seated {quietSeated}); 30 cm drop: max noise {loud:0.0} in {loudEvents} event(s), seated {rq.IsPlaced}");
            // 3. wrong angle: released lying flat over the wires -> not racked; then recovered by re-inserting on edge
            if (rq.IsPlaced) { yield return Safe(Grab(pq)); yield return Safe(Move(HandHolding(pq), HandHolding(pq).transform.position + Vector3.up * 0.15f, 0.4f)); }
            yield return Safe(RackDrop(pq, fv, mid, 0.12f, true));
            Shot(saved + "_rack_wrong_angle");
            Ck("rack_wrong_angle_not_counted", !rq.IsPlaced, $"flat release: placed {rq.IsPlaced}, lean from vertical {Vector3.Angle(pq.transform.up, Vector3.up):0}° (90 = on edge)");
            yield return Safe(RackDrop(pq, fv, mid, 0.015f, false));
            Ck("rack_recovered_after_wrong_angle", rq.IsPlaced, $"re-inserted on edge: placed {rq.IsPlaced}");
            Ck("rack_impact_policy_total", true, ImpactGate.Report());
            _cur = saved;
        }

        /// <summary>Plan-relevant physical state (placement, holding, joins, stages, lids, smudges, controls) - ignores evidence spot
        /// names and orientation flags, which the hands legitimately produce differently from the planner's guesses.</summary>
        static string Key(RecipeWorld w)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in w.Props) sb.Append(p.Fixture).Append(',').Append(p.Slot).Append(',').Append(p.HeldBy).Append(',').Append(p.Stage).Append(',').Append(p.Partner).Append(',').Append(p.AttachedTo).Append('|');
            foreach (var f in w.Fixtures) sb.Append(f.Open ? 1 : 0).Append(',').Append(f.Decals).Append(',').Append(string.Join("/", f.Occupant)).Append(',').Append(f.Flags.Count).Append('|');
            return sb.ToString();
        }

        /// <summary>
        /// `recipe_diag stations`: checks every recipe station rectangle against the REAL built room (connected house): a grid of
        /// downward rays must land on a flat top at the station height, and the space 0.45 m above it must be free. Reports coverage
        /// per station + the measured top extents so Core station data can be corrected from evidence.
        /// </summary>
        /// <summary>`probe:Kitchen;x0;x1;z0;z1` - height map (room-local, 2 cm grid) of what a hand would touch from above in that box:
        /// used to anchor fixtures to real furniture (counter tops, sink basins) instead of guessing.</summary>
        /// <summary>V12: where are the real table tops / seats of the baked house, compared with the Core furniture list (used to place
        /// chore items "on the coffee table")? Raycasts the centre and four inner points of each listed surface.</summary>
        /// <summary>Wardrobe crash investigation (Quest 2 run, 1:16.7): the family stage + six fast "›" taps in the old wardrobe, as in the
        /// video. Counts Genies bodies requested / built / thrown away / alive and managed memory.</summary>
        // ------------------------------------------------------------------ hands lab (Quest run 2026-11-11: tracked gloves looked corrupted)
        static readonly float[][] FingerSeg = { new[] { 0.035f, 0.03f }, new[] { 0.045f, 0.025f, 0.022f }, new[] { 0.05f, 0.03f, 0.024f }, new[] { 0.045f, 0.028f, 0.022f }, new[] { 0.035f, 0.02f, 0.02f } };
        static readonly Vector3[] FingerBase = { new Vector3(0.025f, -0.012f, 0.025f), new Vector3(0.022f, 0f, 0.09f), new Vector3(0.0f, 0f, 0.095f), new Vector3(-0.02f, 0f, 0.09f), new Vector3(-0.038f, -0.003f, 0.08f) };

        /// <summary>A synthetic adult tracked hand (OpenXR joint layout): 0 wrist, 1 palm, 2-3 thumb, 4-6 index ... 13-15 little, 16-20 tips.
        /// <paramref name="curl"/>[finger][joint] in degrees toward the palm; the thumb swings across the palm by <paramref name="thumbIn"/>.</summary>
        static Vector3[] SynthHand(Vector3 wrist, Quaternion rot, bool left, float[][] curl, float thumbIn)
        {
            var j = new Vector3[21]; float s = left ? -1f : 1f;
            Vector3 W(Vector3 l) => wrist + rot * new Vector3(l.x * s, l.y, l.z);
            j[0] = wrist; j[1] = W(new Vector3(0f, 0f, 0.045f));
            for (int f = 0; f < 5; f++)
            {
                var p = FingerBase[f]; var dir = f == 0 ? Quaternion.Euler(0f, -thumbIn, 0f) * new Vector3(0.55f, -0.25f, 0.8f).normalized : Vector3.forward;
                float bend = 0f; int first = f == 0 ? 2 : 4 + (f - 1) * 3;
                for (int k = 0; k < FingerSeg[f].Length; k++)
                {
                    j[first + k] = W(p);
                    bend += curl[f][k];
                    var d = Quaternion.AngleAxis(bend, Vector3.right) * dir;   // +X rotation bends +Z toward -Y = the palm side
                    p += d * FingerSeg[f][k];
                }
                j[16 + f] = W(p);
            }
            return j;
        }

        /// <summary>Quest run 2026-11-11: pinch grabs failed (mug, coasters, practice book). Desktop checks of the grab rules with a simulated
        /// hand: pinch-then-reach grabs once the fingers are on the item and stopped; a palm-up pinch-and-hold (walk gesture) never grabs;
        /// a normal pinch still grabs; sweeping past with a closed pinch does not; tracked bone ids come from the skeleton's own id set.</summary>
        /// <summary>V12 UI fixes from the Quest run (desktop, simulated head + hands): wrist watch opens only on a deliberate look, menu boards
        /// keep one spot, loading card on a fully black fade, nameplates scale / hide up close, HUD over my body, Customize -> Genies look,
        /// Practice HUD points at the board.</summary>
        IEnumerator UiLab()
        {
            _cur = "UI12"; _rep.recipes.Add("UI12");
            var wrist = FindFirstObjectByType<WristMenu>(FindObjectsInactive.Include);
            if (wrist == null) { Ck("wrist_menu", false, "no WristMenu in the scene"); }
            else
            {
                wrist.Hand = _l;   // the lab's simulated left hand (the watch was bound to the headset hand source at boot)
                _headPos = new Vector3(0f, 1.55f, 0f); _r.transform.position = new Vector3(0.3f, 1.0f, 0.3f);
                var palmAt = _headPos + new Vector3(-0.12f, -0.28f, 0.32f);
                void Palm(Vector3 at) { _l.transform.position = at + _l.transform.forward * 0.04f; _l.palmNormal = (_headPos - at).normalized; }
                // a) palm toward me but I am looking away (to the right): never opens
                _headRot = Quaternion.LookRotation(Vector3.right); Palm(palmAt); yield return new WaitForSeconds(1.0f);
                bool a = !wrist.IsOpen;
                // b) a quick glance (0.2 s) at the palm: does not open
                _headRot = Quaternion.LookRotation(palmAt - _headPos); yield return new WaitForSeconds(0.2f);
                bool b = !wrist.IsOpen;
                // c) a deliberate look: opens
                yield return new WaitForSeconds(0.5f);
                bool c = wrist.IsOpen;
                // d) tapping it with the other hand, looking 30 deg off: stays open
                _headRot = Quaternion.LookRotation(Quaternion.Euler(0f, 30f, 0f) * (palmAt - _headPos)); _r.transform.position = palmAt + new Vector3(0.05f, 0.03f, -0.02f);
                yield return new WaitForSeconds(0.6f);
                bool d = wrist.IsOpen;
                _l.palmNormal = Vector3.down; _r.transform.position = new Vector3(0.3f, 1.0f, 0.3f); yield return new WaitForSeconds(0.5f);
                Ck("wrist_watch_deliberate_look", a && b && c && d, $"palm up but looking away: {(a ? "closed" : "OPEN")}; 0.2 s glance: {(b ? "closed" : "OPEN")}; 0.7 s look: {(c ? "open" : "CLOSED")}; tapping it 30 deg off-centre: {(d ? "stays open" : "CLOSED")}");
            }
            // menu boards keep one spot
            _headPos = new Vector3(0f, 1.55f, 0f); _headRot = Quaternion.LookRotation(Vector3.forward); yield return null;
            MenuBuilder.ShowCustomize(GameRoot.I); yield return null;
            var pop1 = GameObject.Find("Customize"); var p1 = pop1 != null ? pop1.transform.position : Vector3.zero;
            bool noSkill = pop1 != null && !pop1.GetComponentsInChildren<TMPro.TextMeshPro>(true).Any(t => t.text.Contains("Bot skill"));
            bool genies = pop1 != null && pop1.GetComponentsInChildren<TMPro.TextMeshPro>(true).Any(t => t.text.Contains("Genies look") || t.text.Contains("Wardrobe"));
            Ck("customize_is_about_me", noSkill && genies, $"Customize: bot skill row {(noSkill ? "gone (it is in the Solo panel)" : "STILL THERE")}; look button {(genies ? (AvatarService.Available ? "'My Genies look' -> Genies wardrobe" : "'Wardrobe' (Genies unavailable here)") : "MISSING")}");
            _headRot = Quaternion.LookRotation(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward); yield return new WaitForSeconds(0.3f);
            MenuBuilder.ShowRules(GameRoot.I); yield return null;
            var pop2 = GameObject.Find("Rules"); var p2 = pop2 != null ? pop2.transform.position : Vector3.zero;
            _headRot = Quaternion.LookRotation(Quaternion.Euler(0f, 90f, 0f) * Vector3.forward); yield return new WaitForSeconds(1.2f);
            var p3 = pop2 != null ? pop2.transform.position : Vector3.zero; var toB = p3 - _headPos; toB.y = 0f;
            Ck("menu_boards_keep_one_spot", pop2 != null && Vector3.Distance(p1, p2) < 0.01f && Vector3.Angle(Quaternion.Euler(0f, 90f, 0f) * Vector3.forward, toB) < 15f,
               $"second popup after a 30 deg head turn opened {Vector3.Distance(p1, p2) * 100f:0.0} cm from the first (same spot); after a 90 deg turn it glided in front ({Vector3.Angle(Quaternion.Euler(0f, 90f, 0f) * Vector3.forward, toB):0} deg off my view direction)");
            MenuBuilder.ClosePopup();
            // loading: black fade fully covers, the card shows
            _headRot = Quaternion.LookRotation(Vector3.forward); _rig.FadeTo(1f); _rig.Loading = true; yield return new WaitForSeconds(0.8f);
            var card = _rig.Head.Find("loadingCard");
            float lum = 0f; yield return Late(() => { Shot("ui12_loading"); lum = MeanLuminance(); });
            var hudL = TaskHud.Ensure(_rig.Head);
            Ck("hud_hidden_while_loading", hudL != null && hudL.HiddenByFade, $"task strip while the screen is black: {(hudL != null && hudL.HiddenByFade ? "hidden" : "DRAWN over the loading card")}");
            Ck("loading_card_on_black", card != null && card.gameObject.activeSelf && lum < 0.08f, $"loading card {(card != null && card.gameObject.activeSelf ? "shown" : "MISSING")} on the fade; mean brightness {lum:0.000} (black with text)");
            _rig.FadeTo(0f); yield return new WaitForSeconds(0.6f);
            bool cardGone = card == null || !card.gameObject.activeSelf;
            Ck("loading_card_leaves_with_the_fade", cardGone, cardGone ? "card hidden once the fade lifts" : "card still visible");
            // nameplates up close
            var g = new GameObject("QA_Bot"); var av = new GameObject("avatar").AddComponent<SiblingAvatar>(); av.transform.SetParent(g.transform, false); av.Build(2, "Nell", true, 2);
            var plate = av.transform.Find("nameplate");
            g.transform.position = _headPos + Vector3.forward * 0.35f + Vector3.down * 1.5f; yield return new WaitForSeconds(0.3f);
            bool hiddenNear = plate != null && plate.GetComponentsInChildren<Renderer>().All(r => !r.enabled);
            g.transform.position = _headPos + Vector3.forward * 2.0f + Vector3.down * 1.5f; yield return new WaitForSeconds(0.3f);
            bool shownFar = plate != null && plate.GetComponentsInChildren<Renderer>().Any(r => r.enabled); float sc = plate != null ? plate.localScale.x : 0f;
            Ck("nameplates_up_close", hiddenNear && shownFar && sc < 0.9f, $"bot right in front of me: plate {(hiddenNear ? "hidden" : "SHOWN")}; at 2 m: {(shownFar ? "shown" : "HIDDEN")} at {sc:0.00}x");
            Destroy(g);
            // HUD on top of my body; Practice HUD text
            var hud = TaskHud.Ensure(_rig.Head);
            var under = hud != null ? hud.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterials.Any(m => m != null && m.renderQueue < 3990)).Select(r => r.name + "(" + string.Join(",", r.sharedMaterials.Where(m => m != null).Select(m => m.renderQueue)) + ")").ToList() : new List<string> { "no hud" };
            bool onTop = under.Count == 0;
            var prevPractice = GameServices.PracticeMode; GameServices.PracticeMode = true; var prevRound = GameServices.PracticeRound;
            yield return new WaitForSeconds(0.4f);
            var txt = hud != null ? string.Join(" | ", hud.GetComponentsInChildren<TMPro.TextMeshPro>(true).Select(t => t.text).Where(t => !string.IsNullOrEmpty(t))) : "";
            Ck("hud_over_my_body", onTop, onTop ? "every HUD material draws after the world (queue >= 3990, depth test off): my knees / arms cannot cover it" : "still depth-tested: " + string.Join(" ", under));
            Ck("practice_hud_points_at_board", txt.Contains("Practice") && !txt.Contains("all done"), "HUD: " + txt);
            GameServices.PracticeMode = prevPractice;
        }

        float MeanLuminance()
        {
            const int W = 160, H = 90; var rt = new RenderTexture(W, H, 24); var prev = _cam.targetTexture; var act = RenderTexture.active;
            _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply(); var px = t.GetPixels32();
            _cam.targetTexture = prev; RenderTexture.active = act; Destroy(t); rt.Release(); Destroy(rt);
            double sum = 0; foreach (var c in px) sum += (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0; return (float)(sum / px.Length);
        }

        IEnumerator GrabsLab()
        {
            _cur = "GRABS"; _rep.recipes.Add("GRABS");
            Ck("bone_ids_by_skeleton_type",
               OvrHandSource.Id(false, OVRSkeleton.BoneId.XRHand_IndexTip, OVRSkeleton.BoneId.Hand_IndexTip) == OVRSkeleton.BoneId.Hand_IndexTip
               && OvrHandSource.Id(true, OVRSkeleton.BoneId.XRHand_IndexTip, OVRSkeleton.BoneId.Hand_IndexTip) == OVRSkeleton.BoneId.XRHand_IndexTip
               && (int)OVRSkeleton.BoneId.XRHand_IndexTip == (int)OVRSkeleton.BoneId.Hand_Middle2,
               $"XRHand_IndexTip = {(int)OVRSkeleton.BoneId.XRHand_IndexTip} = Hand_Middle2 on a legacy skeleton (the old lookup's wrong bone); ids now follow the skeleton type");
            _round = MakeRound(RoomId.Kitchen, "KT01", out var seed); if (_round == null) { Ck("seed", false, "no seed"); yield break; }
            _round.Begin();
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = true;
            RoomRuntime.NoSelfBody = true;
            _room = new GameObject("V12_Grabs").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room);
            RoomRuntime.NoSelfBody = false;
            float w0 = 0; while (!_round.IsPlayable && w0 < 5) { w0 += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(1.5f);
            // isolated small items resting on furniture (nothing else grabbable within 20 cm)
            var all = _round.Room.Props.Select(sp => _room.Prop(sp.Id)).Where(b => b != null && b.Spec.Grabbable && b.Col != null).ToList();
            var items = all.Where(b => b.Col.bounds.size.magnitude < 0.3f && b.transform.position.y - _room.transform.position.y > 0.3f
                                       && all.All(o => o == b || Vector3.Distance(o.Col.ClosestPoint(b.Col.bounds.center), b.Col.bounds.center) > 0.2f)).Take(4).ToList();
            if (items.Count < 2) { Ck("grab_items", false, $"only {items.Count} isolated items"); yield break; }
            _rep.events.Add("GRABS items: " + string.Join(", ", items.Select(b => b.Spec.Kind + "#" + b.Spec.Id)));
            var h = _r; var far = new Vector3(0f, -5f, 0f); _l.simPinch = 0; _l.transform.position = far;
            Vector3 Touch(PropBody b) { var c = b.Col.bounds.center; var top = b.Col.ClosestPoint(c + Vector3.up * 0.5f); return top + Vector3.up * 0.005f; }
            IEnumerator Reset() { h.simPinch = 0; h.palmNormal = Vector3.down; yield return new WaitForSeconds(0.5f); h.transform.position = _headPos + Vector3.down * 0.5f + Vector3.forward * 0.1f; yield return new WaitForSeconds(0.4f); }

            // A: pinch first (well away), keep pinching past the late-grab window, then reach the item and stop
            var a = items[0]; yield return Safe(WalkTo(a.Col.bounds.center)); yield return Reset();
            var t = Touch(a); h.transform.SetPositionAndRotation(t + new Vector3(0f, 0.3f, -0.2f), Quaternion.LookRotation(Vector3.forward)); yield return new WaitForSeconds(0.2f);
            h.simPinch = 1f; yield return new WaitForSeconds(1.2f);
            yield return Safe(Move(h, t, 0.3f)); yield return new WaitForSeconds(0.8f);
            Ck("pinch_first_then_reach_grabs", HandHolding(a) != null, $"{a.Spec.Kind}: pinched 36 cm away, held 1.2 s, then reached it and stopped -> {(HandHolding(a) != null ? "grabbed" : "not grabbed: " + Int(h).LastGrabFailure)}");
            yield return Reset();

            // B: palm-up pinch-and-hold (the walk gesture) ending on an item: must NOT grab
            var b2 = items[1]; yield return Safe(WalkTo(b2.Col.bounds.center)); yield return Reset();
            t = Touch(b2); h.palmNormal = Vector3.up; h.transform.SetPositionAndRotation(t + new Vector3(0f, 0.3f, -0.2f), Quaternion.LookRotation(Vector3.forward));
            yield return new WaitForSeconds(0.2f); h.simPinch = 1f; yield return new WaitForSeconds(1.2f);
            yield return Safe(Move(h, t, 0.3f)); yield return new WaitForSeconds(0.8f);
            int g0 = Int(h).GrabSuccesses; bool heldB = HandHolding(b2) != null;
            yield return Reset();   // opening the walk pinch must not grab either
            Ck("walk_gesture_never_grabs", !heldB && Int(h).GrabSuccesses == g0, $"{b2.Spec.Kind}: palm-up pinch held onto it, then opened -> {(!heldB && Int(h).GrabSuccesses == g0 ? "never grabbed (correct)" : "GRABBED")}");

            // C: the normal way - open hand onto the item, then pinch
            var c3 = items.Count > 2 ? items[2] : items[0]; yield return Safe(WalkTo(c3.Col.bounds.center)); yield return Reset();
            t = Touch(c3); h.transform.SetPositionAndRotation(t + new Vector3(0f, 0.15f, -0.1f), Quaternion.LookRotation(Vector3.forward));
            yield return Safe(Move(h, t, 0.3f)); yield return new WaitForSeconds(0.3f); h.simPinch = 1f; yield return new WaitForSeconds(0.5f);
            Ck("normal_pinch_grabs", HandHolding(c3) != null, $"{c3.Spec.Kind}: open hand onto it, then pinch -> {(HandHolding(c3) != null ? "grabbed" : "not grabbed: " + Int(h).LastGrabFailure)}");
            yield return Reset();

            // D: sweeping past an item with a closed pinch (no stop): must NOT grab
            var d4 = items.Count > 3 ? items[3] : items[1]; yield return Safe(WalkTo(d4.Col.bounds.center)); yield return Reset();
            t = Touch(d4); h.transform.SetPositionAndRotation(t + new Vector3(-0.35f, 0.01f, 0f), Quaternion.LookRotation(Vector3.forward));
            yield return new WaitForSeconds(0.2f); h.simPinch = 1f; yield return new WaitForSeconds(1.2f);
            yield return Safe(Move(h, t + new Vector3(0.35f, 0.01f, 0f), 1.4f)); yield return new WaitForSeconds(0.3f);
            Ck("sweep_past_does_not_grab", HandHolding(d4) == null, $"{d4.Spec.Kind}: swept across it at ~1.4 m/s with a closed pinch -> {(HandHolding(d4) == null ? "not grabbed (correct)" : "GRABBED")}");
            yield return Reset();
            GrabLog.Flush();
            var logPath = System.IO.Path.Combine(Application.persistentDataPath, "shhh_grablog.txt");
            Ck("grab_log_written", File.Exists(logPath) && File.ReadAllLines(logPath).Length > 0, $"{logPath}: {(File.Exists(logPath) ? File.ReadAllLines(logPath).Length : 0)} lines (on the Quest: /sdcard/Android/data/<package>/files/shhh_grablog.txt)");
        }

        IEnumerator HandsLab()
        {
            _cur = "HANDS"; _rep.recipes.Add("HANDS");
            var vis = FindObjectsByType<SimpleHandVisual>(FindObjectsSortMode.None);
            if (vis.Length < 2) { Ck("glove_visuals", false, $"{vis.Length} SimpleHandVisual found"); yield break; }
            _headPos = new Vector3(0f, 1.5f, 0f); _headRot = Quaternion.LookRotation(new Vector3(0f, -0.35f, 1f));
            var poses = new (string name, float[][] curl, float thumbIn)[]
            {
                ("open",  new[] { new[] { 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f } }, 0f),
                ("fist",  new[] { new[] { 25f, 35f }, new[] { 75f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f } }, 35f),
                ("pinch", new[] { new[] { 15f, 20f }, new[] { 35f, 45f, 30f }, new[] { 25f, 30f, 20f }, new[] { 25f, 30f, 20f }, new[] { 25f, 30f, 20f } }, 25f),
                ("point", new[] { new[] { 25f, 35f }, new[] { 0f, 0f, 0f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f } }, 35f),
            };
            var joints = new Dictionary<SimpleHandVisual, Vector3[]>();
            SimpleHandVisual.QaJoint = (v, i) => joints.TryGetValue(v, out var a) ? a[i] : (Vector3?)null;
            foreach (var mode in new[] { true, false })
            {
                SimpleHandVisual.LegacyRetarget = mode;
                foreach (var (pn, curl, thumbIn) in poses)
                {
                    foreach (var v in vis)
                    {
                        bool left = v.IsLeft; var wrist = _headPos + _headRot * new Vector3(left ? -0.1f : 0.1f, -0.18f, 0.32f);
                        var rot = _headRot * Quaternion.Euler(-10f, left ? 15f : -15f, left ? 25f : -25f);   // fingers forward, back of hand up, a little turned in
                        joints[v] = SynthHand(wrist, rot, left, curl, thumbIn);
                        // the sim hand source must count as tracked for the glove to show
                        var src = v.Interactor != null ? v.Interactor.Source as SimHandSource : v.Source as SimHandSource; if (src != null) { src.transform.SetPositionAndRotation(wrist + rot * Vector3.forward * 0.09f, rot); src.simPinch = 0f; src.simGrip = 0f; }
                    }
                    yield return new WaitForSeconds(0.3f);
                    float maxStretch = 0f, maxTip = 0f, maxPinch = 0f; int bent = 0;
                    yield return Late(() =>
                    {
                        foreach (var v in vis)
                        {
                            var j = joints[v];
                            maxStretch = Mathf.Max(maxStretch, v.QaMaxBoneStretch());
                            var errs = new System.Text.StringBuilder();
                            for (int f = 0; f < 5; f++) { var tipV = v.QaTip(f); float d = Vector3.Distance(tipV, j[16 + f]); if (f < 2) maxPinch = Mathf.Max(maxPinch, d); else maxTip = Mathf.Max(maxTip, d); errs.Append($"{d * 100f:0.0} "); }
                            if (!mode) _rep.events.Add($"HANDS new {pn} {(v.IsLeft ? "L" : "R")}: tip errors cm (thumb..little) {errs}; {v.QaFrameReport(j)}");
                        }
                        var cen = Vector3.zero; foreach (var v in vis) cen += joints[v][1]; cen /= vis.Length;
                        var eye = cen - _headRot * Vector3.forward * 0.2f + Vector3.up * 0.12f;
                        ShotFrom($"hands_{(mode ? "old" : "new")}_{pn}", eye, cen + _headRot * Vector3.forward * 0.04f, false);
                    });
                    if (!mode) Ck("glove_" + pn, maxStretch < 0.03f && maxPinch < 0.015f && maxTip < 0.04f, $"{pn}: glove bones stretched at most {maxStretch * 100f:0.0} % (no stretching = 0); thumb + index tips at most {maxPinch * 100f:0.0} cm from the real ones (target < 1.5); other fingertips at most {maxTip * 100f:0.0} cm (cartoon vs adult proportions, target < 4)");
                    else _rep.events.Add($"HANDS old retarget, {pn}: bones stretched up to {maxStretch * 100f:0} %, fingertips up to {maxTip * 100f:0.0} cm off");
                }
            }
            SimpleHandVisual.QaJoint = null; SimpleHandVisual.LegacyRetarget = false;
        }

        IEnumerator WardrobeSpamLab()
        {
            _cur = "WARDROBE_SPAM"; _rep.recipes.Add("WARDROBE_SPAM");
            if (AvatarService.Backend == null) { Ck("genies_backend", false, "no Genies backend in this Editor session"); yield break; }
            var holder = new GameObject("QA_StageHolder").transform; holder.position = new Vector3(40f, 0f, 40f);
            var stage = FamilyStage.Build(holder, Vector3.zero, 0f);
            _headPos = holder.position + new Vector3(0, 1.3f, -2.2f); _headRot = Quaternion.LookRotation(holder.position + Vector3.up * 0.8f - _headPos);
            float w = 0; while (w < 120f && AvatarService.LiveBodies < 4) { w += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(2f);
            int r0 = AvatarService.LoadsRequested, b0 = AvatarService.LoadsBuilt, d0 = AvatarService.LoadsDiscarded, l0 = AvatarService.LiveBodies;
            long m0 = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            Shot("wardrobe_spam_before");
            // six taps of "›" on a row whose only unlocked item is the current one (Glasses), ~0.35 s apart
            var prog = ProgressService.Current; string character = CharacterCatalog.SiblingKeys[((Settings.Current.Cosmetic % 4) + 4) % 4];
            for (int t = 0; t < 6; t++) { MenuBuilder.WardrobeStep(prog, character, "glasses", 1); yield return new WaitForSeconds(0.35f); }
            int peakLive = 0; long peakMem = m0; w = 0;
            while (w < 90f) { w += Time.deltaTime; peakLive = Mathf.Max(peakLive, AvatarService.LiveBodies); peakMem = System.Math.Max(peakMem, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()); if (w > 10f && AvatarService.LoadsRequested - AvatarService.LoadsBuilt - AvatarService.LoadsDiscarded <= 0) break; yield return null; }
            yield return new WaitForSeconds(2f);
            Shot("wardrobe_spam_after");
            int req = AvatarService.LoadsRequested - r0, built = AvatarService.LoadsBuilt - b0, disc = AvatarService.LoadsDiscarded - d0;
            Ck("wardrobe_taps_do_not_rebuild_bodies", built == 0 && AvatarService.LiveBodies == l0,
               $"6 taps on a locked row: {req} Genies loads requested, {built} built by the SDK, {disc} thrown away; live bodies {l0} -> {AvatarService.LiveBodies} (peak {peakLive}); managed memory {m0 / 1048576} -> peak {peakMem / 1048576} MB; waited {w:0} s");
            // the Editor-crash scenario: tear the stage down repeatedly while its Genies bodies are being built (5 rebuilds, 0.3 s apart)
            int rq = AvatarService.LoadsRequested, bl = AvatarService.LoadsBuilt, sk = AvatarService.LoadsSkipped, ds = AvatarService.LoadsDiscarded;
            for (int t = 0; t < 5; t++) { FamilyStage.I?.Rebuild(); yield return new WaitForSeconds(0.3f); }
            w = 0; while (w < 150f && (AvatarService.LoadsRequested - AvatarService.LoadsBuilt - AvatarService.LoadsSkipped > 0 || AvatarService.LiveBodies != 4)) { w += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(2f);
            Shot("wardrobe_spam_rebuild_storm");
            Ck("stage_rebuild_storm_survives", AvatarService.LiveBodies == 4,
               $"5 stage rebuilds mid-build: {AvatarService.LoadsRequested - rq} requested, {AvatarService.LoadsBuilt - bl} built, {AvatarService.LoadsSkipped - sk} skipped before the SDK started, {AvatarService.LoadsDiscarded - ds} disposed after their build; live bodies {AvatarService.LiveBodies}; settled after {w:0} s; Editor still running");
            Destroy(holder.gameObject);
        }

        IEnumerator ScatterLab()
        {
            _cur = "SCATTER"; _rep.recipes.Add("SCATTER");
            // a real 10-minute house match (me + 3 bots), built like the game builds it
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 7, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.NoSelfBody = true;
            _room = new GameObject("V12_Scatter").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 7);
            RoomRuntime.NoSelfBody = false;
            yield return new WaitForSeconds(3f);   // let everything settle under real physics
            var sc = _round.Room.Props.Where(sp => sp.StartHint != null).ToList();
            int ok = 0; var bad = new List<string>(); var lines = new List<string>();
            foreach (var sp in sc)
            {
                var b = _room.Prop(sp.Id); if (b == null) { bad.Add(sp.Kind + " missing"); continue; }
                var local = _room.transform.InverseTransformPoint(b.transform.position);
                float flat = new Vector2(local.x - sp.Spawn.x, local.z - sp.Spawn.z).magnitude, dy = local.y - sp.Spawn.y;
                bool rest = flat < 0.3f && dy > -0.2f && dy < 0.15f;
                if (rest) ok++; else bad.Add($"{sp.Kind} {sp.StartHint}: moved {flat:0.00} m, dy {dy:0.00}");
                lines.Add($"{sp.Kind} {sp.StartHint} (rest y {local.y:0.00})");
            }
            Ck("scattered_items_rest_where_left", sc.Count >= 6 && ok == sc.Count, $"{ok}/{sc.Count} at rest where they were left; {string.Join(" | ", bad)}");
            _rep.events.Add("SCATTER " + string.Join(" | ", lines));
            // what my HUD tells me
            string hint = "";
            foreach (var inst in _round.Recipes.Instances) { if (inst.Owner != 0) continue; foreach (var st in inst.Def.Steps) { hint = TaskHud.WhereIs(_round, inst, st.PropKind); if (hint != "") break; } if (hint != "") break; }
            Ck("hud_says_where", hint != "", hint == "" ? "no hint for my jobs (none of my items scattered?)" : "HUD: " + hint);
            // pictures of a few of them, as a player would come across them
            int n = 0; var kindsShot = new HashSet<string>();
            foreach (var sp in sc)
            {
                if (n >= 5) break; if (!kindsShot.Add(sp.Kind)) continue; var b = _room.Prop(sp.Id); if (b == null) continue;
                var at = b.transform.position; var from = at + new Vector3(0.9f, 1.1f, -0.9f);
                if (Physics.Linecast(from, at + Vector3.up * 0.05f, out var hh, ~0, QueryTriggerInteraction.Ignore) && hh.collider.GetComponentInParent<PropBody>() != b) from = at + new Vector3(-0.9f, 1.1f, 0.9f);
                _headPos = from; _headRot = Quaternion.LookRotation(at - from); yield return new WaitForSeconds(0.2f);
                Shot($"scatter_{n}_{sp.Kind}"); n++;
            }
        }

        IEnumerator SurfaceProbe()
        {
            _cur = "SURFACES"; _rep.recipes.Add("SURFACES");
            foreach (var rm in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                if (_room) { Destroy(_room.gameObject); yield return null; }
                _room = new GameObject("V12_Surfaces_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(RecipeRoomOnly(rm));
                yield return new WaitForSeconds(0.4f);
                foreach (var f in RoomCatalog.Get(rm).Furniture)
                {
                    if (!(f.Kind == "coffee_table" || f.Kind == "side_table" || f.Kind == "dining_table" || f.Kind == "tv_stand" || f.Kind == "sofa" || f.Kind == "armchair")) continue;
                    var sb = new System.Text.StringBuilder();
                    float c = Mathf.Cos(f.Yaw * Mathf.Deg2Rad), sn = Mathf.Sin(f.Yaw * Mathf.Deg2Rad);
                    foreach (var (u, v) in new[] { (0f, 0f), (-0.3f, -0.3f), (0.3f, -0.3f), (-0.3f, 0.3f), (0.3f, 0.3f) })
                    {
                        float lx = u * f.Size.x, lz = v * f.Size.z;
                        var local = new Vector3(f.Position.x + lx * c + lz * sn, 2.2f, f.Position.z - lx * sn + lz * c);
                        var o = _room.transform.TransformPoint(local);
                        if (Physics.Raycast(o, Vector3.down, out var hit, 2.2f, ~0, QueryTriggerInteraction.Ignore)) sb.Append($" {_room.transform.InverseTransformPoint(hit.point).y:0.00}({hit.collider.name.Substring(0, Mathf.Min(18, hit.collider.name.Length))})");
                        else sb.Append(" none");
                    }
                    Ck("surface_" + rm + "_" + f.Kind, true, $"core top {f.Size.y:0.00} at ({f.Position.x:0.00},{f.Position.z:0.00}); real:{sb}");
                }
            }
        }

        IEnumerator Probe(string a)
        {
            var p = a.Split(';'); var rm = (RoomId)System.Enum.Parse(typeof(RoomId), p[0]);
            float x0 = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), x1 = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), z0 = float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture), z1 = float.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture);
            if (_room) { Destroy(_room.gameObject); yield return null; }
            _room = new GameObject("V9_Probe_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(RecipeRoomOnly(rm));
            yield return new WaitForSeconds(0.4f);
            var sb = new System.Text.StringBuilder(); const float st = 0.04f;
            for (float z = z0; z <= z1 + 1e-4f; z += st)
            {
                sb.Append($"z{z:0.00}:");
                for (float x = x0; x <= x1 + 1e-4f; x += st)
                {
                    var o = _room.transform.TransformPoint(new Vector3(x, 2.2f, z));
                    float y = Physics.Raycast(o, Vector3.down, out var hit, 2.2f, ~0, QueryTriggerInteraction.Ignore) ? _room.transform.InverseTransformPoint(hit.point).y : -1f;
                    sb.Append($" {y:0.00}");
                }
                sb.Append('\n');
            }
            foreach (var t in _room.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("F_counter") || t.name.StartsWith("F_stove")) { var b = new Bounds(t.position, Vector3.zero); foreach (var r in t.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds); var lc = _room.transform.InverseTransformPoint(b.center); sb.Append($"{t.name}: local centre {lc:F3} size {b.size:F3}\n"); }
            Ck("probe", true, sb.ToString());
        }

        /// <summary>V10 `look:Kitchen|name;px;py;pz;tx;ty;tz|name2;...` - builds the room (no recipe round) and saves a desktop capture per
        /// view (room-local eye position + target). Also lists every collider whose bounds intersect a 0.1 m sphere at the target.</summary>
        IEnumerator Look(string a)
        {
            var parts = a.Split('|'); var rm = (RoomId)System.Enum.Parse(typeof(RoomId), parts[0]);
            if (_room) { Destroy(_room.gameObject); yield return null; }
            _room = new GameObject("V10_Look_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(RecipeRoomOnly(rm));
            yield return new WaitForSeconds(0.6f);
            _cur = "look";
            // V10: model collider audit - build each articulated model alone and compare authored COL boxes with the visible mesh
            foreach (var key in new[] { "dc_kit018", "dc_kit015", "dc_kit026" })
            {
                var holder = new GameObject("audit_" + key).transform; holder.SetParent(_room.transform, false); holder.localPosition = new Vector3(0, 3.5f, -2f);
                var am = ArticulatedModel.Build(key, holder, key, 0.45f, Layers.Furniture);
                if (!am.Ok) { Ck("model_audit_" + key, false, "model missing"); continue; }
                Physics.SyncTransforms();
                Bounds vis = default; bool any = false;
                foreach (var r in am.Root.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!any) { vis = r.bounds; any = true; } else vis.Encapsulate(r.bounds); }
                Bounds col = default; any = false; string floor = "";
                foreach (var c in am.Colliders) { if (!any) { col = c.bounds; any = true; } else col.Encapsulate(c.bounds); if (c.name.StartsWith("COL_TrayFloor")) { var mr = c.GetComponent<Renderer>(); floor += $"{c.name}: collider y [{c.bounds.min.y - holder.position.y:0.000},{c.bounds.max.y - holder.position.y:0.000}] mesh y [{(mr ? mr.bounds.min.y - holder.position.y : -1):0.000},{(mr ? mr.bounds.max.y - holder.position.y : -1):0.000}] lossy {c.transform.lossyScale:F3} rot {c.transform.rotation.eulerAngles:F0}; "; } }
                string partList = string.Join(" ", am.Parts.Where(kv => kv.Key != "FRONT").Select(kv => $"{kv.Key}@y{kv.Value.position.y - holder.position.y:0.000}"));
                floor += " parts: " + partList;
                Ck("model_audit_" + key, key != "dc_kit018" || (Mathf.Abs(col.min.y - vis.min.y) < 0.02f && Mathf.Abs(col.max.y - vis.max.y) < 0.03f),
                    $"visible y [{vis.min.y - holder.position.y:0.000},{vis.max.y - holder.position.y:0.000}] size {vis.size:F3}; colliders y [{col.min.y - holder.position.y:0.000},{col.max.y - holder.position.y:0.000}] size {col.size:F3}; {floor}");
                Destroy(holder.gameObject);
            }
            // V12: mechanism models split in Blender (washer / fridge / bin / drawers). Report each PART pivot (holder-local) and its mesh bounds,
            // and in the Laundry room keep them standing on the floor in a row so the look views can capture them.
            int mi = 0;
            foreach (var key in new[] { "ld_laundry_washer_mech", "uh_uh_fridge_mech", "uh_uh_bin_mech", "uh_uh_drawers3_mech" })
            {
                var holder = new GameObject("mech_" + key).transform; holder.SetParent(_room.transform, false);
                holder.localPosition = rm == RoomId.Laundry ? new Vector3(-1.9f + mi * 0.95f, 0f, 1.2f) : new Vector3(0, 3.5f, -2f); mi++;
                var am = ArticulatedModel.Build(key, holder, key, 0f, Layers.Furniture);
                if (!am.Ok) { Ck("mech_audit_" + key, false, "model missing"); continue; }
                Physics.SyncTransforms();
                Bounds vis = default; bool any = false;
                foreach (var r in am.Root.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!any) { vis = r.bounds; any = true; } else vis.Encapsulate(r.bounds); }
                var sbm = new System.Text.StringBuilder();
                sbm.Append($"visible size {vis.size:F3} min y {vis.min.y - holder.position.y:0.000}; ");
                foreach (var kv in am.Parts)
                {
                    var lp = holder.InverseTransformPoint(kv.Value.position);
                    Bounds pb = default; bool pany = false;
                    foreach (var r in kv.Value.GetComponentsInChildren<Renderer>()) { if (!pany) { pb = r.bounds; pany = true; } else pb.Encapsulate(r.bounds); }
                    sbm.Append($"{kv.Key} pivot {lp:F3}" + (pany ? $" mesh c {holder.InverseTransformPoint(pb.center):F3} s {pb.size:F3}" : "") + "; ");
                }
                Ck("mech_audit_" + key, am.Parts.Count > 1 && vis.min.y - holder.position.y > -0.01f && vis.size.y > 0.2f, sbm.ToString());
                if (rm != RoomId.Laundry) Destroy(holder.gameObject);
            }
            for (int i = 1; i < parts.Length; i++)
            {
                var f = parts[i].Split(';'); if (f.Length < 7) continue;
                float F(int k) => float.Parse(f[k], System.Globalization.CultureInfo.InvariantCulture);
                var eye = _room.transform.TransformPoint(new Vector3(F(1), F(2), F(3))); var tgt = _room.transform.TransformPoint(new Vector3(F(4), F(5), F(6)));
                _headPos = eye; _headRot = Quaternion.LookRotation(tgt - eye, Vector3.up);
                yield return new WaitForSeconds(0.35f);
                Shot("look_" + f[0]);
                var hits = Physics.OverlapSphere(tgt, 0.1f, ~0, QueryTriggerInteraction.Ignore).Select(c => $"{c.name}@{_room.transform.InverseTransformPoint(c.bounds.center):F2} size {c.bounds.size:F2}").Take(12);
                Ck("look_" + f[0], true, "colliders near target: " + string.Join(" | ", hits));
            }
        }

        /// <summary>V10 `recipe_diag water`: KT01 in the real kitchen; close captures of the tap at 0 / 50 / 100 % valve (dragged by a hand),
        /// a plate held in the stream, and the shut-off drips. Desktop render only (Quest stereo UNTESTED).</summary>
        IEnumerator WaterShots()
        {
            _cur = "KT01"; _rep.recipes.Add("KT01_water");
            _round = MakeRound(RoomId.Kitchen, "KT01", out var seed); if (_round == null) { Ck("seed", false, "no seed"); yield break; }
            _round.Begin();
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = true;
            _room = new GameObject("V10_Water_Kitchen").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, seed); _room.enabled = false; _round.Pets.Clear();
            _rt = _room.GetComponent<RecipeRuntime>();
            float w = 0; while (!_round.IsPlayable && w < 5) { w += Time.deltaTime; yield return null; }
            var inst = _round.Recipes.Instances.First(i => i.Owner == 0 && i.Def.Id == "KT01");
            var fv = _rt.Fixtures.First(f => f.Instance == inst && f.Tap != null); var tap = fv.Tap;
            var spout = tap.SpoutWorld; var front = fv.transform.forward;
            _headPos = spout + front * 0.55f + Vector3.up * 0.12f; _headRot = Quaternion.LookRotation(tap.BasinBottomWorld + Vector3.up * 0.12f - _headPos);
            Park(); yield return new WaitForSeconds(0.5f);
            // V10 measurement (owner: "the water is a lot up"): horizontal rays from the front of the sink toward the back, every 1 cm of
            // height, at the tap's x: which height is faucet (hit well in front of the backsplash) and where is its most forward tip?
            {
                var rt = _room.transform; var sb = new System.Text.StringBuilder(); float backZ = float.MinValue; var prof = new List<(float y, float z)>();
                var lf = rt.InverseTransformPoint(spout); float x = lf.x;
                for (float y = 0.45f; y <= 1.6f; y += 0.01f)
                {
                    var o = rt.TransformPoint(new Vector3(x, y, 1.9f)); var dir = rt.TransformDirection(Vector3.forward);
                    if (Physics.Raycast(o, dir, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PropBody>() == null)
                    { float hz = rt.InverseTransformPoint(hit.point).z; prof.Add((y, hz)); backZ = Mathf.Max(backZ, hz); }
                }
                foreach (var pr in prof) sb.Append($"{pr.y:0.00}:{pr.z:0.00} ");
                // vertical rays down along the tap's centre line: basin floor
                var down = Physics.Raycast(rt.TransformPoint(new Vector3(x, 1.2f, rt.InverseTransformPoint(tap.BasinBottomWorld).z)), Vector3.down, out var fh, 1.2f, ~0, QueryTriggerInteraction.Ignore) ? rt.InverseTransformPoint(fh.point).y : -1f;
                Ck("faucet_profile", true, $"tap x {x:0.00}; spout used {lf:F3}; basin floor ray {down:0.000}; fixture y {rt.InverseTransformPoint(fv.transform.position).y:0.000}; profile y:z {sb}");
            }
            _headPos = tap.BasinBottomWorld + fv.transform.right * 0.75f + Vector3.up * 0.35f + front * 0.15f; _headRot = Quaternion.LookRotation(tap.BasinBottomWorld + Vector3.up * 0.25f - _headPos);
            yield return new WaitForSeconds(0.3f); Shot("water_side_closed");
            _headPos = spout + front * 0.55f + Vector3.up * 0.12f; _headRot = Quaternion.LookRotation(tap.BasinBottomWorld + Vector3.up * 0.12f - _headPos);
            foreach (var lvl in new[] { 0f, 0.5f, 1f })
            {
                if (lvl > 0f) yield return Safe(TurnTap(tap, lvl));
                Park(); yield return new WaitForSeconds(2.5f);
                Shot($"water_valve_{lvl * 100:000}");
                if (lvl >= 1f) { var hp = _headPos; var hr = _headRot; _headPos = tap.BasinBottomWorld + fv.transform.right * 0.75f + Vector3.up * 0.35f + front * 0.15f; _headRot = Quaternion.LookRotation(tap.BasinBottomWorld + Vector3.up * 0.25f - _headPos); yield return new WaitForSeconds(0.3f); Shot("water_side_open"); _headPos = hp; _headRot = hr; }
                Ck($"water_valve_{lvl * 100:000}", Mathf.Abs(tap.Level - lvl) < 0.11f, $"level {tap.Level:0.00} flow {tap.Flow:0.00}; stream hits {(tap.Stream.Hitting ? tap.Stream.HitCollider.name : "nothing")} after {tap.Stream.FallLength:0.00} m; basin {tap.BasinLitres:0.00} L");
            }
            // a plate held in the falling water (the water lands on it, not through it)
            var plate = _rt.Bodies.First(x => { var r = _rt.RProp(x); return r != null && r.RecipeInstance == inst.Index && r.Kind == "plate"; });
            {
                var wv = plate.GetComponent<WashableView>(); var rpp = _rt.RProp(plate);
                Ck("stain_render_state", wv != null, $"dirt cells {RecipeWorld.CountBits(rpp.Dirt)}; {(wv != null ? wv.Debug_State() : "no WashableView")}");
                var hp = _headPos; var hr = _headRot; var pc = plate.Col.bounds.center;
                _headPos = pc + Vector3.up * 0.45f + Vector3.forward * 0.05f; _headRot = Quaternion.LookRotation(pc - _headPos, Vector3.forward);
                yield return new WaitForSeconds(0.3f); Shot("stain_plate_topdown"); _headPos = hp; _headRot = hr;
            }
            yield return Safe(WalkTo(plate.Col.bounds.center)); yield return Safe(Grab(plate));
            yield return Safe(WalkTo(fv.transform.position));
            var h = HandHolding(plate);
            if (h != null) { yield return Safe(FaceUp(plate, h)); h = HandHolding(plate) ?? h; var off = h.transform.position - plate.Col.bounds.center; yield return Safe(Move(h, tap.RinsePoint + off, 0.4f)); }
            _headPos = spout + front * 0.5f + Vector3.up * 0.05f; _headRot = Quaternion.LookRotation(tap.RinsePoint - _headPos);
            yield return new WaitForSeconds(0.6f);
            Shot("water_on_plate");
            Ck("water_lands_on_plate", tap.Stream.HitProp == plate, $"first hit {(tap.Stream.HitCollider ? tap.Stream.HitCollider.name : "none")}, prop {(tap.Stream.HitProp ? tap.Stream.HitProp.Spec.Kind : "none")}, fall {tap.Stream.FallLength:0.00} m");
            yield return Safe(TurnTap(tap, 0f)); yield return new WaitForSeconds(0.25f);
            _headPos = spout + front * 0.55f + Vector3.up * 0.12f; _headRot = Quaternion.LookRotation(tap.BasinBottomWorld + Vector3.up * 0.12f - _headPos);
            Shot("water_shutoff_drips");
            yield return Safe(LiquidLab(tap, fv));
        }

        /// <summary>V11 liquid reservoirs: a mug fills under the tap (finite volume), is tilted over a bowl (pour arc, transfer), then over the
        /// counter (spill -> puddle). Vessels are moved by a SCRIPTED TEST JIG (kinematic), not by a hand: this tests the liquid model, not grasping.</summary>
        IEnumerator LiquidLab(SinkTapView tap, RecipeFixtureView fv)
        {
            _cur = "LIQUID"; var right = fv.transform.right; var front = fv.transform.forward;
            GameObject Vessel(string kind, Vector3 at, out LiquidContainer lc, float startL)
            {
                var g = new GameObject("V11_jig_" + kind); g.transform.position = at;
                RecipeVisuals.BuildProp(kind, null, g.transform, false);
                var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true;
                var b = Prim.RendererBounds(g); var lb = new Bounds(g.transform.InverseTransformPoint(b.center), b.size);
                var bc = g.AddComponent<BoxCollider>(); bc.center = lb.center; bc.size = new Vector3(lb.size.x, lb.size.y * 0.6f, lb.size.z); bc.center -= new Vector3(0, lb.size.y * 0.2f, 0); // open top: the box is the lower body
                float r = Mathf.Min(lb.extents.x, lb.extents.z) * 0.78f, floor = lb.min.y + 0.008f, rim = lb.max.y - 0.004f;
                float cap = Mathf.PI * r * r * (rim - floor) * 1000f;
                lc = LiquidContainer.Add(g, cap, r, floor, rim, Mathf.Min(startL, cap));
                return g;
            }
            // mug under the tap
            var under = tap.SpoutWorld + Vector3.down * 0.35f;
            var mug = Vessel("mug", under, out var mugL, 0f);
            yield return null;
            _headPos = tap.SpoutWorld + front * 0.5f - Vector3.up * 0.05f; _headRot = Quaternion.LookRotation(under - _headPos);
            yield return Safe(TurnTap(tap, 0.6f)); Park();
            float t = 0; while (t < 6f && mugL.LevelFraction < 0.75f) { t += Time.deltaTime; yield return null; }
            Shot("liquid_mug_filling");
            yield return Safe(TurnTap(tap, 0f)); Park(); yield return new WaitForSeconds(0.4f);
            float filled = mugL.Amount;
            Ck("liquid_fill_from_tap", filled > 0.05f && mugL.Amount <= mugL.Capacity + 1e-4f, $"mug {filled * 1000f:0} ml of {mugL.Capacity * 1000f:0} ml after {t:0.0} s at 60 % valve; overflow went to the basin; ledger {string.Join(" | ", mugL.Ledger)}");
            // a bowl on the left counter, the mug lifted over it and tilted (jig)
            var bowlAt = fv.transform.position + right * 0.62f + front * 0.12f; bowlAt.y = fv.transform.position.y + 0.235f + 0.004f;   // left counter worktop (0.80 m)
            RaycastHit ground; if (Physics.Raycast(bowlAt + Vector3.up * 0.3f, Vector3.down, out ground, 1f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) bowlAt = ground.point;
            var bowl = Vessel("fruit_bowl", bowlAt, out var bowlL, 0f);
            yield return null;
            var hold = bowlAt + Vector3.up * 0.16f - right * 0.11f;   // lip just short of the bowl centre: the arc carries it in
            for (float u = 0; u < 1f; u += Time.deltaTime / 0.8f) { mug.transform.position = Vector3.Lerp(under, hold, Mathf.SmoothStep(0, 1, u)); yield return null; }
            _headPos = bowlAt + front * 0.55f + Vector3.up * 0.4f; _headRot = Quaternion.LookRotation(bowlAt + Vector3.up * 0.1f - _headPos);
            float m0 = mugL.Amount, b0 = bowlL.Amount, p0 = Puddle.All.Sum(x => x.Litres);
            var axis = Vector3.Cross(Vector3.up, right).normalized;   // tip toward +right (over the bowl)
            for (float u = 0; u < 1f; u += Time.deltaTime / 2.2f) { mug.transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(0f, 120f, Mathf.SmoothStep(0, 1, u)), axis); if (u > 0.55f && u < 0.57f) Shot("liquid_pour_arc"); yield return null; }
            yield return new WaitForSeconds(1.2f);
            Shot("liquid_bowl_after");
            float poured = m0 - mugL.Amount, got = bowlL.Amount - b0, spilled = Puddle.All.Sum(x => x.Litres) - p0;
            Ck("liquid_pour_transfer", poured > 0.02f && got > poured * 0.6f && Mathf.Abs(poured - got - spilled) < 0.01f + poured * 0.05f,
               $"mug {m0 * 1000f:0} -> {mugL.Amount * 1000f:0} ml (poured {poured * 1000f:0}); bowl +{got * 1000f:0} ml; spilled {spilled * 1000f:0} ml; conserved within {(poured - got - spilled) * 1000f:0.0} ml");
            // refill a little from the bowl? no - tilt the bowl over the counter edge to make a puddle
            var spillAt = bowlAt - right * 0.3f + Vector3.up * 0.12f;
            for (float u = 0; u < 1f; u += Time.deltaTime / 0.8f) { bowl.transform.position = Vector3.Lerp(bowlAt, spillAt, u); yield return null; }
            float bb = bowlL.Amount; var axis2 = -front;
            for (float u = 0; u < 1f; u += Time.deltaTime / 1.8f) { bowl.transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(0f, 130f, u), axis2); yield return null; }
            yield return new WaitForSeconds(1.5f);
            _headPos = spillAt + front * 0.6f + Vector3.up * 0.5f; _headRot = Quaternion.LookRotation(spillAt - Vector3.up * 0.15f - _headPos);
            yield return new WaitForSeconds(0.2f); Shot("liquid_puddle");
            Ck("liquid_spill_puddle", Puddle.All.Count > 0 && Puddle.All.Sum(x => x.Litres) > 0.01f, $"bowl {bb * 1000f:0} -> {bowlL.Amount * 1000f:0} ml; puddles {Puddle.All.Count}: {string.Join(", ", Puddle.All.Select(x => $"{x.Litres * 1000f:0} ml r {x.Radius * 100f:0.0} cm"))}");
            _cur = "KT01";
        }

        IEnumerator AuditStations()
        {
            foreach (var rm in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                _cur = rm.ToString();
                if (_room) { Destroy(_room.gameObject); yield return null; }
                _room = new GameObject("V7_Stations_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(RecipeRoomOnly(rm)); // legacy props removed, as in a recipe round
                yield return new WaitForSeconds(0.3f);
                int cleared = RecipeRuntime.ClearStations(_room, rm); Ck("dressing_cleared", true, $"{rm}: {cleared} dressing objects hidden from station tops");
                yield return new WaitForFixedUpdate(); yield return new WaitForSeconds(0.2f);
                var sts = RecipeLayout.Stations(rm);
                for (int i = 0; i < sts.Length; i++)
                {
                    var st = sts[i]; int ok = 0, n = 0, blocked = 0; var misses = new List<string>(); float minU = 9, maxU = -9, minT = 9, maxT = -9; var heights = new List<float>();
                    for (float u = -st.Length * 0.5f + 0.02f; u <= st.Length * 0.5f - 0.02f + 1e-3f; u += st.Length / 10f)
                        for (float t = -st.Depth * 0.5f + 0.02f; t <= st.Depth * 0.5f - 0.02f + 1e-3f; t += st.Depth / 6f)
                        {
                            n++;
                            var w = _room.transform.TransformPoint(VisualFactory.V(st.Local(u, t, 0f)));
                            if (Physics.Raycast(w + Vector3.up * 0.45f, Vector3.down, out var hit, 1.0f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
                            {
                                heights.Add(hit.point.y);
                                bool flat = Mathf.Abs(hit.point.y - w.y) < 0.03f && hit.normal.y > 0.95f;
                                if (!flat && misses.Count < 6) misses.Add($"(u{u:0.00},t{t:0.00}: y{hit.point.y:0.000} n{hit.normal.y:0.00} {hit.collider.name})");
                                if (flat) { ok++; minU = Mathf.Min(minU, u); maxU = Mathf.Max(maxU, u); minT = Mathf.Min(minT, t); maxT = Mathf.Max(maxT, t); }
                                if (Physics.CheckSphere(w + Vector3.up * 0.25f, 0.06f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) blocked++;
                            }
                        }
                    float medH = heights.Count > 0 ? heights.OrderBy(x => x).ElementAt(heights.Count / 2) : -1;
                    Ck($"station{i}", ok >= n * 0.9f && blocked <= n * 0.05f, $"{rm} station {i} centre {st.Center} {st.Length}x{st.Depth} alongZ={st.AlongZ} front={st.Front}: flat {ok}/{n}, blocked-above {blocked}/{n}, median top y {medH:0.000}, flat u[{minU:0.00},{maxU:0.00}] t[{minT:0.00},{maxT:0.00}] misses {string.Join(" ", misses)}");
                    // top view of the station
                    var c = _room.transform.TransformPoint(VisualFactory.V(st.Center));
                    _headPos = c + Vector3.up * 1.3f; _headRot = Quaternion.LookRotation(Vector3.down, st.AlongZ ? Vector3.right * st.Front : Vector3.forward * st.Front);
                    yield return null; yield return null;
                    Shot($"station_{rm}_{i}");
                }
            }
        }

        /// <summary>`recipe_diag surfaces`: maps every flat, reachable work surface (0.38–1.0 m high, free 0.3 m above) in each built room
        /// at 4 cm resolution and reports connected surfaces as rectangles (room coordinates) - the evidence for station data.</summary>
        IEnumerator FindSurfaces()
        {
            foreach (var rm in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                _cur = rm.ToString();
                if (_room) { Destroy(_room.gameObject); yield return null; }
                var def = RecipeRoomOnly(rm);
                _room = new GameObject("V7_Surfaces_" + rm).AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(def);
                yield return new WaitForSeconds(0.5f);
                const float step = 0.04f; int nx = (int)(def.Width / step), nz = (int)(def.Depth / step);
                var h = new float[nx, nz];
                for (int i = 0; i < nx; i++) for (int k = 0; k < nz; k++)
                {
                    h[i, k] = -1;
                    var p = _room.transform.TransformPoint(new Vector3(-def.Width * 0.5f + (i + 0.5f) * step, 1.6f, -def.Depth * 0.5f + (k + 0.5f) * step));
                    if (Physics.Raycast(p, Vector3.down, out var hit, 1.6f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.97f)
                    {
                        float y = _room.transform.InverseTransformPoint(hit.point).y;
                        if (y >= 0.38f && y <= 1.0f && !Physics.CheckSphere(hit.point + Vector3.up * 0.2f, 0.08f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) h[i, k] = y;
                    }
                }
                var seen = new bool[nx, nz]; int found = 0;
                for (int i = 0; i < nx; i++) for (int k = 0; k < nz; k++)
                {
                    if (seen[i, k] || h[i, k] < 0) continue;
                    var q = new Queue<(int, int)>(); q.Enqueue((i, k)); seen[i, k] = true; int cnt = 0, x0 = i, x1 = i, z0 = k, z1 = k; float y0 = h[i, k];
                    while (q.Count > 0)
                    {
                        var (a, c) = q.Dequeue(); cnt++; x0 = Mathf.Min(x0, a); x1 = Mathf.Max(x1, a); z0 = Mathf.Min(z0, c); z1 = Mathf.Max(z1, c);
                        foreach (var (da, dc) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        { int na = a + da, nc = c + dc; if (na < 0 || nc < 0 || na >= nx || nc >= nz || seen[na, nc] || h[na, nc] < 0 || Mathf.Abs(h[na, nc] - y0) > 0.015f) continue; seen[na, nc] = true; q.Enqueue((na, nc)); }
                    }
                    if (cnt < 40) continue; // < ~0.06 m2
                    float fill = cnt / (float)((x1 - x0 + 1) * (z1 - z0 + 1));
                    float cx = -def.Width * 0.5f + (x0 + x1 + 1) * 0.5f * step, cz = -def.Depth * 0.5f + (z0 + z1 + 1) * 0.5f * step;
                    found++;
                    Ck($"surface{found}", true, $"{rm} surface centre ({cx:0.00},{y0:0.000},{cz:0.00}) size x {(x1 - x0 + 1) * step:0.00} z {(z1 - z0 + 1) * step:0.00} fill {fill:0.00} area {cnt * step * step:0.00} m2");
                }
            }
        }

        /// <summary>
        /// `recipe_diag confront` (Gate 5 in the real runtime): seat 3 (Rory) makes crashes right in front of the local player; the host
        /// PerceptionService must record what the local player saw (line of sight + view cone), Mom must wake WITHOUT knowing who,
        /// the confrontation tray must appear, the simulated finger pokes "Show: Saw Rory…" and "Nothing to add", and the verdict must
        /// follow the card. A second pass hides the crash from the player's view: no memory, no proof.
        /// </summary>
        IEnumerator Confront()
        {
            foreach (bool visible in new[] { true, false })
            {
                _cur = visible ? "CONFRONT-SEEN" : "CONFRONT-UNSEEN";
                if (_room) { Destroy(_room.gameObject); yield return null; yield return Resources.UnloadUnusedAssets(); GC.Collect(); }
                var roster = new CompetitorRoster();
                roster.SeatHuman(0, "Pip", 0); roster.SeatHuman(1, "Nell", 1); roster.SeatHuman(2, "Rory", 2); roster.SeatHuman(3, "Tess", 3); roster.LockForMatch();
                var cfg = RoundConfig.DefaultSequence()[0]; cfg.RecipeMode = true; cfg.CountdownSeconds = 0.2f;
                _round = new RoundController(cfg, roster, 7); _round.Begin();
                GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
                _room = new GameObject("V7_Confront").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 7); _room.enabled = false; _round.Pets.Clear();
                // V12 pets get hungry and whine (PetCare, 2026-11-11): in this lab the only noise must be Rory's crash
                foreach (var pc in PetCare.All.ToList()) if (pc != null) pc.enabled = false;
                _round.PetHunger[0] = _round.PetHunger[1] = 0f;
                _round.Log.Sink = line => _rep.events.Add(_cur + " " + line);
                _rt = _room.GetComponent<RecipeRuntime>();
                float w = 0; while (!_round.IsPlayable && w < 5) { w += Time.deltaTime; yield return null; }
                var inst = _round.Recipes.Instances.First(i => i.Owner == 0);
                var pl0 = _round.RecipeFixtures.First(f => f.Instance == inst.Index);
                var st = RecipeLayout.Stations(RoomId.LivingRoom)[pl0.Station];
                var toPlayer = VisualFactory.V(st.Local(0, st.Front * 1f) - st.Center).normalized;
                var centre = _room.transform.TransformPoint(VisualFactory.V(st.Center));
                _rig.Spawn(centre + toPlayer * 0.55f - Vector3.up * centre.y, Quaternion.LookRotation(-toPlayer).eulerAngles.y);
                _headPos = centre + toPlayer * 0.5f + Vector3.up * 0.62f;
                _headRot = visible ? Quaternion.LookRotation(centre - _headPos) : Quaternion.LookRotation(toPlayer + Vector3.up * 0.2f); // unseen: looking away
                Park(); yield return new WaitForSeconds(0.5f);
                // Rory's crashes with one of Pip's items at Pip's table (API: the crash is not what is under test here)
                // a hard item of Pip's (a soft one - towel, cloth - never adds up to a wake: 2.1 per drop)
                var cands = inst.PropsByKind.Values.SelectMany(x => x).Where(id => !_round.Recipes.World.Prop(id).IsPlaced).Select(id => _round.RoomPropOf[id]).ToList();
                int prop = cands.OrderBy(id => _round.Room.Prop(id)?.Material == NoiseMaterial.Soft ? 1 : 0).First();
                var at = _room.Prop(prop).transform.position;
                for (int k = 0; k < 120 && _round.Phase == RoundPhase.Playing; k++) { _round.Grab(2, prop); _round.Release(2, prop, 3f, true); _round.SubmitImpact(prop, 9f, VisualFactory.C(at)); yield return new WaitForSeconds(0.12f); }
                Ck("mom_woke_without_knowing", _round.Phase == RoundPhase.MomSequence && _round.Verdict != null && _round.Verdict.Kind == MomVerdictKind.None, $"phase {_round.Phase} verdict {_round.Verdict?.Kind}");
                yield return new WaitForSeconds(0.3f);
                var mem = _round.Belief.MemoriesOf(0).ToList();
                Ck(visible ? "local_player_remembers" : "local_player_saw_nothing", visible ? mem.Count > 0 && mem.All(m => m.SeenActor == 2) : mem.Count == 0, string.Join(" | ", mem.Select(m => m.Card + $" ({m.Confidence:0.00})")));
                _headRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(-toPlayer, Vector3.up).normalized * 0.9f + Vector3.down * 0.6f); // look ahead before Mom asks
                w = 0; while (!(_round.Mom.State == MomState.Interrogating && _round.Interro.EvidenceOpen) && w < 40) { w += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.6f);
                var panel = FindFirstObjectByType<ConfrontationPanel>();
                var buttons = panel != null ? panel.GetComponentsInChildren<PokeButton>().ToList() : new List<PokeButton>();
                Ck("tray_shown", buttons.Count >= 4, $"{buttons.Count} buttons: {string.Join(", ", buttons.Select(b => b.Label))}");
                Shot(_cur + "_tray");
                var show = buttons.FirstOrDefault(b => b.Label.StartsWith("Show"));
                if (visible) { if (show != null) yield return Safe(PokeButtonWithFinger(show)); Ck("card_presented", _round.Interro != null && _round.Interro.Cards.Count == 1, $"cards {_round.Interro?.Cards.Count}"); }
                yield return new WaitForSeconds(0.5f);
                panel = FindFirstObjectByType<ConfrontationPanel>();
                var nothing = panel != null ? panel.GetComponentsInChildren<PokeButton>().FirstOrDefault(b => b.Label == "Nothing to add") : null;
                if (nothing != null) yield return Safe(PokeButtonWithFinger(nothing));
                Shot(_cur + "_after_input");
                w = 0; while (_round.Mom.State < MomState.Verdict && w < 45) { w += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.8f);
                Shot(_cur + "_verdict");
                if (visible) Ck("verdict_follows_card", _round.Verdict.Kind == MomVerdictKind.Eliminate && _round.Verdict.Culprit == 2 && _round.Verdict.Reason == EliminationReason.CaughtMakingNoise, $"{_round.Verdict.Kind} {_round.Verdict.Culprit} {_round.Verdict.Reason} :: {_round.Verdict.Evidence}");
                else Ck("no_proof_no_catch", _round.Verdict.Reason != EliminationReason.CaughtMakingNoise && !_round.LastResolution.Proven, $"{_round.Verdict.Kind} {_round.Verdict.Culprit} {_round.Verdict.Reason}");
            }
        }

        /// <summary>V12 `recipe_diag mom12` (Quest run 2026-11-11: "MOM IS UP! -20 PTS" without ever seeing Mom or why; bots spawned on me
        /// and walked through me). A real solo house match: I make a crash at a table near my spawn; Mom's walk is sampled (state,
        /// position, path status, in my view or not) through the whole visit; bots' spawn spacing and clearance around me are measured.</summary>
        IEnumerator MomLab()
        {
            _cur = "MOM12"; _rep.recipes.Add("MOM12");
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 11, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            _round.Log.Sink = line => { if (line.Contains("MOM") || line.Contains("PENALTY") || line.Contains("CONFRONT") || line.Contains("Mom ")) _rep.events.Add(_cur + " " + line); };
            RoomRuntime.NoSelfBody = true;
            _room = new GameObject("V12_Mom").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 11);
            RoomRuntime.NoSelfBody = false;
            var spawn = _room.SpawnFor(0); _headPos = spawn + Vector3.up * 1.55f; _headRot = Quaternion.LookRotation(Vector3.forward);
            _l.transform.position = _headPos + new Vector3(-0.2f, -0.5f, 0.2f); _r.transform.position = _headPos + new Vector3(0.2f, -0.5f, 0.2f);
            yield return null;
            var botStart = new List<string>(); float nearestBot = 99f;
            foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb == null) continue; float d = Flat(bb.transform.position - spawn).magnitude; nearestBot = Mathf.Min(nearestBot, d); botStart.Add($"{_round.Roster[br.Slot].DisplayName} {d:0.00} m"); }
            Ck("bots_spawn_clear_of_me", nearestBot >= 1.2f, $"bots start {string.Join(", ", botStart)} from me");
            yield return Late(() => Shot("mom12_start_view"));
            { var cp = _cam.transform.position; var near = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.SqrDistance(cp) < 0.04f).Select(r => $"{r.name}<{(r.transform.parent ? r.transform.parent.name : "-")}> {(r.sharedMaterial ? r.sharedMaterial.name + " q" + r.sharedMaterial.renderQueue : "nomat")}").Take(15);
              _rep.events.Add($"MOM12 camera {_cam.name} enabled {_cam.enabled} mask {_cam.cullingMask} clear {_cam.clearFlags} near {_cam.nearClipPlane} far {_cam.farClipPlane} rt {(_cam.targetTexture != null)}; renderers at the camera: {string.Join(" | ", near)}"); }
            // bots go about their jobs for a while: the closest any bot comes to my head while I stand still
            float minClear = 99f; string who = "";
            for (float t = 0; t < 20f; t += 0.1f)
            {
                foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb == null) continue; float d = Flat(bb.transform.position - _headPos).magnitude; if (d < minClear) { minClear = d; who = _round.Roster[br.Slot].DisplayName; } }
                yield return new WaitForSeconds(0.1f);
            }
            // and a bot sent straight through where I stand (the brains stay paused meanwhile)
            var crosser = _room.Brains.Select(b => _room.BodyFor(b.Slot)).FirstOrDefault(b => b != null);
            float crossClear = 99f;
            if (crosser != null)
            {
                RoomRuntime.QaPauseBrains = true;
                foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb != null) bb.Stop(); }
                var from = crosser.transform.position; var through = Flat(_headPos) + (Flat(_headPos) - Flat(from)).normalized * 2.0f;
                crosser.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = VisualFactory.C(through) });
                for (float t = 0; t < 12f && crosser.Busy; t += 0.05f) { crossClear = Mathf.Min(crossClear, Flat(crosser.transform.position - _headPos).magnitude); yield return new WaitForSeconds(0.05f); }
                RoomRuntime.QaPauseBrains = false;
            }
            Ck("bots_keep_out_of_my_head", minClear >= 0.45f && crossClear >= 0.45f, $"20 s of bots working: closest {minClear:0.00} m ({who}); a bot sent straight through my spot passed {crossClear:0.00} m from my head");

            // a crash right in front of me, made by me
            // an item I can see from my spawn, with a free spot 0.6 m in front of it to stand (not inside a wall / furniture)
            Vector3 StandFor(PropBody p) => p.transform.position + Flat(spawn - p.transform.position).normalized * 0.6f;
            bool Clear(PropBody p)
            {
                var st0 = StandFor(p); var eye = new Vector3(st0.x, _room.transform.position.y + 1.55f, st0.z);
                return !Physics.Linecast(spawn + Vector3.up * 1.5f, p.transform.position, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)
                    && !Physics.CheckSphere(eye, 0.2f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)
                    && UnityEngine.AI.NavMesh.SamplePosition(new Vector3(st0.x, _room.transform.position.y, st0.z), out _, 0.25f, UnityEngine.AI.NavMesh.AllAreas);
            }
            var prop = _room.Props.Where(p => p.Spec.Grabbable && p.Col != null && p.transform.position.y - _room.transform.position.y > 0.3f && Clear(p))
                                  .OrderBy(p => Flat(p.transform.position - spawn).magnitude).FirstOrDefault();
            if (prop == null) { Ck("noise_prop", false, "no prop near my spawn"); yield break; }
            var at = prop.transform.position; var stand = StandFor(prop);
            _headPos = new Vector3(stand.x, _room.transform.position.y + 1.55f, stand.z); _headRot = Quaternion.LookRotation(Flat(at - _headPos).normalized * 0.9f + Vector3.down * 0.4f);
            yield return new WaitForSeconds(0.3f);
            float t0 = Time.time;
            for (int k = 0; k < 60 && _round.Phase == RoundPhase.Playing; k++) { _round.Grab(0, prop.Spec.Id); _round.Release(0, prop.Spec.Id, 3f, true); _round.SubmitImpact(prop.Spec.Id, 9f, VisualFactory.C(at), directActor: 0); yield return new WaitForSeconds(0.12f); }
            Ck("mom_wakes", _round.Phase == RoundPhase.MomSequence, $"{prop.Spec.Kind} crashed in front of me for {Time.time - t0:0.0} s -> phase {_round.Phase}, Mom {_round.Mom.State}");
            if (_round.Phase != RoundPhase.MomSequence) yield break;
            float wake = Time.time; var mom = _room.Mom; float minMom = 99f, minStopped = 99f, inView = 0f, firstSeen = -1f, firstNear = -1f; string lastSt = ""; var tl = new List<string>();
            var hud = TaskHud.Ensure(_rig.Head); string hudWake = "", hudAsk = "", hudVerdict = ""; float trayOff = -1f, trayAng = -1f; bool moved = false;
            var look = _headRot; var eyes = _headPos;
            while ((_round.Mom.RoundFrozen || Time.time - wake < 1f) && Time.time - wake < 120f)
            {
                var mp = mom.transform.position + Vector3.up * 1.4f; float d = Flat(mp - _headPos).magnitude; minMom = Mathf.Min(minMom, d);
                var stE = _round.Mom.State;
                bool stopped = stE == MomState.Investigating || stE == MomState.Interrogating || stE == MomState.Verdict;
                if (stopped && mom.Agent != null && mom.Agent.velocity.magnitude < 0.2f) minStopped = Mathf.Min(minStopped, d);
                if (firstNear < 0f && d < 2.5f) firstNear = Time.time - wake;
                var toM = mp - _headPos; bool cone = Vector3.Angle(_rig.Head.forward, toM) < 50f;
                bool clear = !Physics.Linecast(_headPos, mp, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore);
                if (mom.BodyOut && cone && clear && d > 0.6f) { inView += 0.25f; if (firstSeen < 0f) firstSeen = Time.time - wake; }
                if (hud != null && (stE == MomState.Waking || stE == MomState.WalkingIn) && !hudWake.Contains("MOM IS UP")) hudWake = hud.MomLine;   // the HUD refreshes every 0.25 s
                if (hud != null && stE == MomState.Interrogating && _round.Interro != null && _round.Interro.EvidenceOpen && hudAsk == "") hudAsk = hud.MomLine;
                if (hud != null && (stE == MomState.Verdict || stE == MomState.ReturningToBed) && !(hudVerdict.StartsWith("Mom caught") || hudVerdict.StartsWith("Mom: warning"))) hudVerdict = hud.MomLine;
                // halfway through the questions I step 1 m aside and turn 90 deg: the tray must come back in front of me
                if (!moved && stE == MomState.Interrogating && _round.Interro != null && _round.Interro.EvidenceOpen && _round.Interro.Remaining < _round.Interro.Evidence - 3f)
                {
                    moved = true; var side = Quaternion.Euler(0f, 90f, 0f) * Flat(_rig.Head.forward).normalized;
                    _headPos = eyes + side * 1.0f; _headRot = Quaternion.LookRotation(side * 0.9f + Vector3.down * 0.3f);
                    yield return new WaitForSeconds(1.0f);
                    var tray = GameObject.Find("V7_ConfrontationPanel/tray");
                    if (tray != null) { trayOff = Flat(tray.transform.position - _headPos).magnitude; trayAng = Vector3.Angle(Flat(_rig.Head.forward), Flat(tray.transform.position - _headPos)); }
                    yield return Late(() => Shot("mom12_tray_follows"));
                    _headPos = eyes; _headRot = look; yield return new WaitForSeconds(0.5f);
                }
                string st = stE.ToString();
                if (st != lastSt)
                {
                    lastSt = st; var ag = mom.Agent;
                    tl.Add($"{Time.time - wake:0.0}s {st} {d:0.0} m from me, floor y {mom.transform.position.y - _room.transform.position.y:0.00}, path {(ag != null && ag.isOnNavMesh ? ag.pathStatus.ToString() : "OFF-NAVMESH")}, target {Flat(mom.LastTarget - _headPos).magnitude:0.0} m from me");
                    if (st == "Investigating") { yield return new WaitForSeconds(1.5f); yield return Late(() => Shot("mom12_mom_arrives")); }
                    if (st == "Verdict") yield return Late(() => Shot("mom12_verdict"));
                }
                yield return new WaitForSeconds(0.25f);
            }
            _rep.events.Add("MOM12 timeline: " + string.Join(" | ", tl));
            float upFor = Time.time - wake;
            Ck("mom_comes_to_the_noise", firstNear >= 0f, $"Mom within 2.5 m of me after {firstNear:0.0} s; up for {upFor:0.0} s");
            Ck("mom_stands_where_i_see_her", minStopped >= 1.0f && minStopped < 3.5f && inView >= 3f, $"while looking / asking / judging she stood {minStopped:0.0} m from me at the closest (walking past: {minMom:0.0} m); in my view {inView:0.0} s (first at {firstSeen:0.0} s)");
            Ck("hud_says_what_mom_heard", hudWake.Contains("MOM IS UP") && hudWake.Contains("heard"), "HUD at wake: " + hudWake);
            Ck("hud_shows_her_question", hudAsk.Contains("Mom asks"), "HUD while asking: " + hudAsk);
            Ck("tray_follows_me", trayOff >= 0f && trayOff < 0.7f && trayAng < 35f, $"after I stepped 1 m aside and turned 90 deg the tray was {trayOff:0.00} m from me, {trayAng:0} deg off my view");
            var pen = _round.Penalties.LastOrDefault();
            Ck("hud_verdict_says_why", hudVerdict.Length > 12 && (hudVerdict.Contains(" - ") || hudVerdict.Contains("warning")), "HUD verdict: " + hudVerdict);
            _rep.events.Add($"MOM12 verdict proven={_round.LastResolution?.Proven} penalty {(pen != null ? $"slot {pen.Slot} -{pen.Points} '{pen.Evidence}'" : "none")}");
        }

        /// <summary>V12 `recipe_diag props12` (Quest run 2026-11-11 #18-#20: the tray read as an open book, coaster spots said "stack 2" in tiny
        /// text, the dog bowl read as a flat red disc, a yellow smiley ball sat at the very edge of the side table). A real house match with
        /// LV09 (cups and coasters): each thing photographed from where a standing player sees it; how the dog toy rests is measured.</summary>
        IEnumerator PropsLab()
        {
            _cur = "PROPS12"; _rep.recipes.Add("PROPS12");
            MatchController m = null; int seed = 0;
            for (seed = 1; seed < 400; seed++)
            {
                var mm = new MatchController(); mm.Roster.SeatHuman(900, "Pip"); mm.Roster.SetReady(900, true); mm.SetMatchLength(MatchRules.TenMinutes);
                if (!mm.Start(MatchMode.Solo, (ulong)seed, out _)) continue;
                mm.SkipIntro(); mm.Tick(0.01f);
                if (mm.Round.Recipes != null && mm.Round.Recipes.Instances.Any(i => i.Def.Id == "LV09")) { m = mm; break; }
            }
            if (m == null) { Ck("seed", false, "no seed with LV09"); yield break; }
            for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.NoSelfBody = true; RoomRuntime.QaPauseBrains = true;
            _room = new GameObject("V12_Props").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, (ulong)seed);
            RoomRuntime.NoSelfBody = false;
            foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb != null) { bb.Stop(); bb.transform.position += Vector3.down * 50f; } }
            yield return new WaitForSeconds(3f);
            float floorY = _room.transform.position.y;
            IEnumerator View(string name, Vector3 at, float dist, float eye)
            {
                // a standing player's view: 'dist' m away horizontally, eyes at 'eye' m, from the most open side
                Vector3 bestFrom = at; float bestScore = -1f;
                for (int a = 0; a < 12; a++)
                {
                    var dir = Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward; var from = new Vector3(at.x, floorY + eye, at.z) + dir * dist;
                    if (Physics.Linecast(from, at + Vector3.up * 0.03f, out var hh, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore) && hh.distance < dist * 0.9f) continue;
                    if (Physics.CheckSphere(from, 0.15f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) continue;
                    float score = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(from.x, floorY, from.z), out _, 0.3f, UnityEngine.AI.NavMesh.AllAreas) ? 2f : 1f;
                    if (score > bestScore) { bestScore = score; bestFrom = from; }
                }
                _headPos = bestFrom; _headRot = Quaternion.LookRotation(at - bestFrom); yield return new WaitForSeconds(0.3f);
                yield return Late(() => Shot(name));
            }
            var lv = _round.Recipes.Instances.First(i => i.Def.Id == "LV09");
            var rt = _room.GetComponent<RecipeRuntime>();
            var fx = rt != null ? rt.Fixtures.Where(f => f != null && f.Instance == lv).ToList() : new List<RecipeFixtureView>();
            foreach (var f in fx) { yield return View("props12_" + f.Decl.Kind + "_standing", f.transform.position, 0.9f, 1.55f); yield return View("props12_" + f.Decl.Kind + "_close", f.transform.position, 0.45f, 1.25f); }
            _rep.events.Add("PROPS12 LV09 fixtures: " + string.Join(", ", fx.Select(f => f.Decl.Kind + (f.Placement.Anchored ? " (on furniture)" : "") + (f.Placement.Stack ? " stack" : ""))));
            var bowl = _room.Props.FirstOrDefault(p => p.Spec.Kind == "pet_bowl_dog");
            if (bowl != null) { yield return View("props12_dog_bowl_standing", bowl.transform.position, 1.0f, 1.55f); _rep.events.Add($"PROPS12 dog bowl bounds {bowl.Col.bounds.size:F3}"); }
            var toy = _room.Props.FirstOrDefault(p => p.Spec.Kind == "dog_toy");
            if (toy != null)
            {
                var c = toy.Col.bounds.center; float r = toy.Col.bounds.extents.y;
                bool under = Physics.Raycast(c, Vector3.down, out var dh, 1.5f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore);
                float gap = under ? dh.distance - r : 99f; var rb = toy.GetComponent<Rigidbody>();
                string what = under ? dh.collider.name : "nothing";
                _rep.events.Add($"PROPS12 dog toy at {_room.transform.InverseTransformPoint(c):F2} (spec {toy.Spec.Spawn.x:0.00},{toy.Spec.Spawn.y:0.00},{toy.Spec.Spawn.z:0.00} hint '{toy.Spec.StartHint}'); under its centre: {what} gap {gap * 100f:0.0} cm; sleeping {(rb != null && rb.IsSleeping())}; kinematic {(rb != null && rb.isKinematic)}");
                yield return View("props12_dog_toy_standing", c, 0.9f, 1.55f);
            }
            // ---- checks
            var tray = fx.FirstOrDefault(f => f.Decl.Kind == "tray"); var spots = fx.FirstOrDefault(f => f.Decl.Kind == "coaster_spots");
            int rims = tray != null ? tray.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("tray_rim") || t.name == "tray_end") : 0;
            int rings = tray != null ? tray.GetComponentsInChildren<MeshFilter>().Count(mf => mf.name.StartsWith("slot") && !mf.name.StartsWith("slot_in") && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cylinder")) : 0;
            Ck("tray_reads_as_a_tray", rims >= 4 && rings == 2, $"tray: {rims} rims/ends with handle grips, {rings} round mug rings (was two flat cream pads)");
            var tag = spots != null ? spots.GetComponentsInChildren<TMPro.TextMeshPro>(true).Select(t => t.text).FirstOrDefault(t => t.Contains("here")) : null;
            Ck("coaster_spot_says_what", tag != null && tag.Contains("coaster"), $"coaster spot: round mark + standing tag '{tag}' (was 'stack 2' flat in tiny text)");
            if (toy != null)
            {
                var c = toy.Col.bounds.center; var lc = _room.transform.InverseTransformPoint(c); var bed = VisualFactory.V(_round.Room.DogBed);
                bool under = Physics.Raycast(c, Vector3.down, out var dh, 1.5f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore); float gap = under ? dh.distance - toy.Col.bounds.extents.y : 99f;
                float toBed = new Vector2(lc.x - bed.x, lc.z - bed.z).magnitude;
                Ck("dog_toy_by_the_dog_bed", lc.y < 0.2f && toBed < 0.9f && gap < 0.01f, $"dog toy at height {lc.y:0.00} m, {toBed:0.00} m from the dog bed, {gap * 100f:0.0} cm above what is under it (was on the side table edge)");
            }
            if (bowl != null)
            {
                var mat = GameObject.Find("feeding_mat");
                Ck("dog_bowl_readable", bowl.Col.bounds.size.x >= 0.25f && mat != null, $"dog bowl {bowl.Col.bounds.size.x * 100f:0} x {bowl.Col.bounds.size.y * 100f:0} cm (was 22 x 5) on a feeding mat with the dog's picture: {mat != null}");
            }
            // the job card from 60 deg off its front
            var card = FindObjectsByType<RecipeClipboard>(FindObjectsSortMode.None).FirstOrDefault(k => k.Instance == lv);
            if (card != null)
            {
                Vector3 front = Quaternion.Euler(0f, card.BaseYaw, 0f) * Vector3.back;   // the readable side faces -forward
                float Off() { var to = Flat(_headPos - card.transform.position).normalized; return Vector3.Angle(-Flat(card.transform.forward).normalized, to); }
                var side = Quaternion.Euler(0f, 60f, 0f) * front; _headPos = card.transform.position + side * 1.6f + Vector3.up * 0.2f; _headRot = Quaternion.LookRotation(card.transform.position - _headPos);
                yield return null; float before = Vector3.Angle(front, side); yield return new WaitForSeconds(1.2f); float after = Off();
                yield return Late(() => Shot("props12_job_card_from_the_side"));
                _rep.events.Add($"PROPS12 card {card.QaInfo} yaw {card.transform.eulerAngles.y:0} main cam {(Camera.main != null ? Camera.main.name + " " + Camera.main.transform.position.ToString("F2") : "none")} head {_headPos:F2} rig cam {_cam.name} tag {_cam.tag}");
                Ck("job_card_turns_to_me", after < 20f, $"from 60 deg off the card's front: {before:0} deg -> {after:0} deg after 1.2 s; scale {card.transform.localScale.x:0.00}x at 1.6 m");
            }
            // HUD: a long step label stays on two lines; walking off physically gives the walk-gesture reminder
            var hud = TaskHud.Ensure(_rig.Head); yield return new WaitForSeconds(0.5f);
            var task = hud != null ? hud.GetComponentsInChildren<TMPro.TextMeshPro>(true).FirstOrDefault(t => t.name == "task") : null;
            if (task != null) { task.text = "Lift the frame onto the hook"; task.ForceMeshUpdate(); }
            Ck("hud_task_label_fits", task != null && task.textInfo.lineCount <= 2, $"'Lift the frame onto the hook' on {(task != null ? task.textInfo.lineCount : -1)} lines (was 4)");
            // the lab's camera jumps between views count as walking (and start the reminder's 12 s cooldown): start clean
            TaskHud.QaResetEdgeCooldown(); Playspace.MarkStart(_rig); yield return new WaitForSeconds(0.3f);
            Playspace.MarkStart(_rig); int hints0 = TaskHud.EdgeHints; var st0 = _headPos;
            _headPos = st0 + Vector3.right * 1.3f; yield return new WaitForSeconds(0.8f);
            string line = hud != null ? hud.MomLine : "";
            { var rg = hud != null ? hud.GetComponentInParent<PlayerRig>() : null; float ed = Playspace.EdgeDistance(rg, out var srcE); _rep.events.Add($"PROPS12 playspace rig {(rg != null)} edge {ed:0.00} source {srcE} hints {TaskHud.EdgeHints}"); }
            Ck("playspace_edge_reminder", TaskHud.EdgeHints > hints0 && line.Contains("finger gun"), $"walked 1.3 m with my feet -> reminders {hints0} -> {TaskHud.EdgeHints}; HUD: '{line}' (no play-area size in the Editor: the 1.1 m walked-away ring is used)");
            RoomRuntime.QaPauseBrains = false;
        }

        Vector3 spawnOf(Component c) { var r = _room != null ? _room.SpawnFor(0) : c.transform.position; return r; }

        /// <summary>V12 `recipe_diag walls12` (owner: "the house is not showing the walls, showing directly the outside" - Quest run 2:21 / 3:50:
        /// the living-room window wall was missing on the headset, present in every Editor capture). The house from my spawn in four directions
        /// at the Editor's quality level and at the Quest's (Android default), with a Quest-like field of view; LOD groups that cull a piece
        /// of the house at the Quest's LOD bias are listed.</summary>
        IEnumerator WallsLab()
        {
            _cur = "WALLS12"; _rep.recipes.Add("WALLS12");
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 7, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.NoSelfBody = true; RoomRuntime.QaPauseBrains = true;
            _room = new GameObject("V12_Walls").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 7);
            RoomRuntime.NoSelfBody = false;
            yield return new WaitForSeconds(1.5f);
            var spawn = _room.SpawnFor(0); _headPos = spawn + Vector3.up * 1.55f;
            int q0 = QualitySettings.GetQualityLevel(); float fov0 = _cam.fieldOfView;
            int android = QaAndroidQuality();
            _rep.events.Add($"WALLS12 quality editor {QualitySettings.names[q0]} (lod bias {QualitySettings.lodBias}), android default {(android >= 0 ? QualitySettings.names[android] : "?")}");
            var groups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            _rep.events.Add($"WALLS12 LOD groups in the scene: {groups.Length}");
            string[] dirs = { "N", "E", "S", "W" };
            var culledAtQuest = new HashSet<string>(); var culledAtEditor = new HashSet<string>();
            foreach (var (lvl, tag) in new[] { (q0, "editor"), (android, "quest") })
            {
                if (lvl < 0) continue;
                QualitySettings.SetQualityLevel(lvl, true); _cam.fieldOfView = 96f; yield return null;
                for (int i = 0; i < 4; i++)
                {
                    _headRot = Quaternion.Euler(5f, i * 90f, 0f); yield return new WaitForSeconds(0.25f);
                    yield return Late(() => Shot($"walls12_{tag}_{dirs[i]}"));
                    foreach (var g in groups)
                    {
                        if (g == null || !g.enabled) continue; var lods = g.GetLODs(); if (lods.Length == 0) continue;
                        float size = g.size * Mathf.Max(g.transform.lossyScale.x, Mathf.Max(g.transform.lossyScale.y, g.transform.lossyScale.z));
                        var c = g.transform.TransformPoint(g.localReferencePoint); float d = Mathf.Max(0.01f, Vector3.Distance(_headPos, c));
                        float rel = size / (2f * d * Mathf.Tan(0.5f * _cam.fieldOfView * Mathf.Deg2Rad)) * QualitySettings.lodBias;
                        if (rel < lods[lods.Length - 1].screenRelativeTransitionHeight && d < 12f)
                            (tag == "quest" ? culledAtQuest : culledAtEditor).Add($"{g.name} ({d:0.0} m, rel {rel:0.000} < {lods[lods.Length - 1].screenRelativeTransitionHeight:0.000})");
                    }
                }
            }
            QualitySettings.SetQualityLevel(q0, true); _cam.fieldOfView = fov0;
            // which renderers carry the window view / were cut open, and how the room looks if the window-view panes draw nothing
            var house = FindFirstObjectByType<HouseAssembly>();
            var wv = house != null ? house.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterials.Any(mm => mm != null && mm.shader != null && mm.shader.name == "SHHH/WindowView")).ToList() : new List<Renderer>();
            var cut = house != null ? house.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.sharedMesh.name.Contains("_openWindows")).ToList() : new List<MeshFilter>();
            _rep.events.Add("WALLS12 window-view renderers: " + string.Join(" | ", wv.Select(r => $"{r.name} enabled {r.enabled} bounds {r.bounds.center:F1} size {r.bounds.size:F1} verts {(r.GetComponent<MeshFilter>()?.sharedMesh != null ? r.GetComponent<MeshFilter>().sharedMesh.vertexCount : -1)}")));
            _rep.events.Add("WALLS12 cut-open meshes: " + string.Join(" | ", cut.Select(f => $"{f.name} {f.sharedMesh.name} subs {f.sharedMesh.subMeshCount}")));
            _cam.fieldOfView = 96f;
            foreach (var r in wv) r.enabled = false;
            for (int i = 0; i < 4; i++) { _headRot = Quaternion.Euler(5f, i * 90f, 0f); yield return new WaitForSeconds(0.2f); yield return Late(() => Shot($"walls12_noview_{dirs[i]}")); }
            foreach (var r in wv) r.enabled = true; _cam.fieldOfView = fov0;
            var arch = house != null ? house.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.Contains("V5_LivingArchitecture")) : null;
            int glass = house != null ? house.GetComponentsInChildren<Renderer>(true).Count(r => r.sharedMaterials.Any(mm => mm != null && mm.name == "SHHH_WindowGlass")) : 0;
            Ck("no_runtime_cut_of_unreadable_meshes", cut.Count == 0 && arch != null && arch.GetComponent<Renderer>().enabled && arch.sharedMesh.vertexCount > 0,
               $"living-room walls: {(arch != null ? $"{arch.sharedMesh.name} (Read/Write {(arch.sharedMesh.isReadable ? "on" : "OFF - an APK cannot read it")}) drawn as authored, {arch.sharedMesh.vertexCount} vertices" : "MISSING")}; meshes cut at runtime: {cut.Count} (the cut copy came out empty on the Quest)");
            Ck("windows_are_glass", glass > 0, $"{glass} renderers show the windows as faint glass (no mesh edit); {wv.Count} painted-on-wall windows use the window view");
            _rep.events.Add($"WALLS12 culled within 12 m at the Quest level: {culledAtQuest.Count}: " + string.Join(" | ", culledAtQuest.Take(40)));
            _rep.events.Add($"WALLS12 culled within 12 m at the Editor level: {culledAtEditor.Count}: " + string.Join(" | ", culledAtEditor.Take(20)));
            Ck("house_pieces_drawn_at_quest_quality", culledAtQuest.Count == 0, $"{culledAtQuest.Count} house LOD groups within 12 m of my spawn are culled at the Quest's quality level ({culledAtEditor.Count} at the Editor's)");
            RoomRuntime.QaPauseBrains = false;
        }

        static int QaAndroidQuality()
        {
#if UNITY_EDITOR
            var names = QualitySettings.names;
            var so = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var per = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; per != null && i < per.arraySize; i++) { var e = per.GetArrayElementAtIndex(i); if (e.FindPropertyRelative("first").stringValue == "Android") return e.FindPropertyRelative("second").intValue; }
#endif
            return -1;
        }


        /// <summary>V12 `recipe_diag teleport12` (owner: "add teleportation in the house"); V13 finger gun: right-hand finger gun (thumb up)
        /// aims an arc from the index finger, dropping the thumb hops; the arc never passes a wall, never lands on furniture; the left hand still walks; the wrist button cycles the styles.</summary>
        IEnumerator TeleportLab()
        {
            _cur = "TELEPORT12"; _rep.recipes.Add("TELEPORT12");
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 7, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.NoSelfBody = true; RoomRuntime.QaPauseBrains = true;
            _room = new GameObject("V12_Teleport").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 7);
            RoomRuntime.NoSelfBody = false;
            foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb != null) bb.Stop(); }
            yield return new WaitForSeconds(1.0f);
            var st = new Settings(); _rig.ApplyLocomotion(st); _rig.LocomotionEnabled = true;
            var spawn = _room.SpawnFor(0); _rig.Spawn(spawn, 0f); _headPos = spawn + Vector3.up * 1.55f; _headRot = Quaternion.LookRotation(Vector3.forward);
            _l.simPinch = 0; _r.simPinch = 0; _l.palmNormal = Vector3.down; _r.palmNormal = Vector3.down;
            _l.simGun = _r.simGun = _l.simThumbDown = _r.simThumbDown = false; _l.simGrip = _r.simGrip = 0f;
            _l.transform.position = _headPos + new Vector3(-0.25f, -0.5f, 0.2f); yield return new WaitForSeconds(0.3f);
            StartCoroutine(Film("teleport12")); _filmCap = "Finger-gun test (simulated hands, Unity Editor)"; yield return new WaitForSeconds(1.2f);

            // aim the right hand along a direction (palm up, fingers pointing) and report what the arc says
            IEnumerator Aim(Vector3 dir, float pitchDown)
            {
                var flat = new Vector3(dir.x, 0f, dir.z).normalized;
                _r.palmNormal = Vector3.left; _r.transform.SetPositionAndRotation(_headPos + flat * 0.3f + Vector3.down * 0.35f + Vector3.Cross(Vector3.up, flat) * 0.15f, Quaternion.LookRotation(Quaternion.AngleAxis(pitchDown, Vector3.Cross(Vector3.up, flat)) * flat));
                yield return new WaitForSeconds(0.1f); _r.simGrip = 0.9f; _r.simThumbDown = false; _r.simGun = true; yield return new WaitForSeconds(0.8f);
            }
            // drop the thumb (fire), then put the hand down
            IEnumerator Release() { _filmCap = "...DROP the thumb = hop (if the spot is teal)"; yield return new WaitForSeconds(0.5f); _r.simThumbDown = true; yield return new WaitForSeconds(0.6f); _r.simGun = false; _r.simThumbDown = false; _r.simGrip = 0f; _r.palmNormal = Vector3.down; yield return new WaitForSeconds(0.1f); }
            Vector3 Moved(Vector3 before) => Flat(_rig.transform.position - before);

            // 1) a floor spot ~3 m into the room: the arc is valid and I land there
            // sweep the pinched hand around (as a player looks for a spot) until the arc shows a valid landing 2-5 m away
            _filmCap = "1  RIGHT hand: finger gun, thumb UP -> an arc comes out of the index finger";
            yield return Aim(Vector3.forward, 22f);
            for (int a = 0; a < 32 && !(_rig.HopAimValid && Flat(_rig.HopAimTarget - _headPos).magnitude > 2f); a++)
            {
                var dd = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward; float pitch = a < 16 ? 22f : 12f;
                _r.transform.SetPositionAndRotation(_headPos + dd * 0.3f + Vector3.down * 0.35f, Quaternion.LookRotation(Quaternion.AngleAxis(pitch, Vector3.Cross(Vector3.up, dd)) * dd));
                yield return null; yield return null;
            }
            _filmCap = "1  Pointing at the floor: teal spot + 'drop your thumb to hop'"; yield return new WaitForSeconds(1.5f);
            bool valid1 = _rig.HopAimValid; var tgt = _rig.HopAimTarget; string why1 = _rig.LastHopRefusal; yield return Late(() => Shot("teleport12_aim_floor"));
            {
                var arcR = _rig.GetComponentInChildren<LineRenderer>(true);
                string arcSh = arcR != null && arcR.sharedMaterial != null ? arcR.sharedMaterial.shader.name : "-";
                var spot = GameObject.Find("teleportMarker"); var spotR = spot != null ? spot.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader.name == "SHHH/TeleportTarget") : null;
                var aimHint = GameObject.Find("hopHint");
                string hintTxt = aimHint != null && aimHint.activeSelf ? aimHint.GetComponentInChildren<TMPro.TextMeshPro>().text : "(none)";
                Ck("hop_visuals", arcSh == "SHHH/TeleportArc" && spotR != null && hintTxt.Contains("drop your thumb"), $"arc shader {arcSh}; landing spot {(spotR != null ? "glowing disc + rim + ripple + facing arrow" : "MISSING")}; label at the spot: '{hintTxt}'");
            }
            var r0 = _rig.transform.position; int hops0 = _rig.Hops; yield return Release();
            var d1 = Moved(r0); _headPos += d1;
            float land = Flat(_headPos - tgt).magnitude;
            Ck("hop_to_the_floor", valid1 && _rig.Hops == hops0 + 1 && land < 0.25f, $"right-hand finger gun, thumb dropped, aimed {Flat(tgt - (r0 + Vector3.zero)).magnitude:0.0} m ahead: arc {(valid1 ? "valid" : "INVALID: " + why1)}; hopped {d1.magnitude:0.00} m, head {land * 100f:0} cm from the marker");
            _filmCap = "1  Hopped - landed on the spot"; yield return new WaitForSeconds(1.2f);
            yield return Late(() => Shot("teleport12_after_hop"));

            // palm-up right hand (not pinching yet): the hand says how to hop
            _filmCap = "2  Palm up = a help label above the hand";
            { _r.simPinch = 0f; _r.palmNormal = Vector3.up; _r.transform.position = _headPos + new Vector3(0.15f, -0.35f, 0.3f); yield return new WaitForSeconds(0.6f);
              var hh = GameObject.Find("moveHint1"); string ht = hh != null && hh.activeSelf ? hh.GetComponentInChildren<TMPro.TextMeshPro>().text : "(none)";
              _l.simPinch = 0f; _l.palmNormal = Vector3.up; _l.transform.position = _headPos + new Vector3(-0.2f, -0.12f, 0.4f); yield return new WaitForSeconds(0.6f);
              var hl = GameObject.Find("moveHint0"); string lt = hl != null && hl.activeSelf ? hl.GetComponentInChildren<TMPro.TextMeshPro>().text : "(none)";
              yield return Late(() => Shot("teleport12_palm_up_hints"));
              _l.palmNormal = Vector3.down; _r.palmNormal = Vector3.down;
              Ck("gesture_explained_at_the_hands", ht.Contains("HOP") && lt.Contains("WALK"), $"right palm up: '{ht}'; left palm up: '{lt}' (left hovering {_rig.LeftHand.IsHovering}, grabbing {_rig.LeftHand.IsGrabbing}, walks {_rig.Walks})"); }
            // 2) straight at a wall: refused, I stay
            var wallDir = Vector3.zero; float best = 99f;
            for (int a = 0; a < 16; a++) { var d = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward; if (Physics.Raycast(_headPos + Vector3.down * 0.4f, d, out var wh, 6f, 1 << 0, QueryTriggerInteraction.Ignore) && Mathf.Abs(wh.normal.y) < 0.2f && wh.distance > 1.0f && wh.distance < best) { best = wh.distance; wallDir = d; } }
            if (wallDir != Vector3.zero)
            {
                _filmCap = "3  Pointing at a WALL -> red cross, 'can't land here'"; yield return Aim(wallDir, -12f); yield return new WaitForSeconds(1.2f);
                bool valid2 = _rig.HopAimValid; string why2 = _rig.LastHopRefusal; yield return Late(() => Shot("teleport12_aim_wall"));
                { var nb = GameObject.Find("hopNoSpot"); var ah = GameObject.Find("hopHint"); string at = ah != null && ah.activeSelf ? ah.GetComponentInChildren<TMPro.TextMeshPro>().text : "(none)";
                  Ck("cannot_land_is_shown", nb != null && nb.activeSelf && at.Contains("can't land"), $"crossed-out spot {(nb != null && nb.activeSelf ? "shown on the wall" : "MISSING")}; label: '{at}'"); }
                r0 = _rig.transform.position; hops0 = _rig.Hops; yield return Release();
                Ck("arc_stops_at_walls", !valid2 && _rig.Hops == hops0 && Moved(r0).magnitude < 0.01f, $"aimed up at the wall {best:0.0} m away: {(valid2 ? "VALID (would pass the wall)" : "refused: " + why2)}; moved {Moved(r0).magnitude:0.00} m");
            }
            else Ck("arc_stops_at_walls", false, "no wall found around me");

            // 2b) V13 (owner, Quest c21: "the view goes inside walls"): head 12 cm from a wall = clear view; head 4 cm INTO it = dark view
            // with a step-back hint; back out = clear again
            if (wallDir != Vector3.zero && Physics.Raycast(_headPos, wallDir, out var wHit, 6f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                var keep = _headPos; var nrm = wHit.normal;
                _headPos = wHit.point + nrm * 0.12f; yield return new WaitForSeconds(0.6f); float near = _rig.WallFade, nearD = _rig.HeadDepth;
                _headPos = wHit.point - nrm * 0.04f; yield return new WaitForSeconds(0.6f); float inside = _rig.WallFade, inD = _rig.HeadDepth;
                yield return Late(() => Shot("teleport12_head_in_wall"));
                _headPos = keep; yield return new WaitForSeconds(0.6f); float back = _rig.WallFade;
                Ck("head_in_wall_darkens", near < 0.05f && inside > 0.8f && back < 0.05f,
                   $"12 cm from the wall ({wHit.collider.name}): fade {near:0.00} (depth {nearD:0.000}); 4 cm inside: fade {inside:0.00} (depth {inD:0.000}); back out: {back:0.00}");
            }

            // 3) at a table top: refused
            var table = _room.Props.Select(pb => pb.transform.position).Where(pp => pp.y - _room.transform.position.y > 0.4f && pp.y - _room.transform.position.y < 0.9f && Flat(pp - _headPos).magnitude > 1.2f && Flat(pp - _headPos).magnitude < 3.5f)
                             .OrderBy(pp => Flat(pp - _headPos).magnitude).FirstOrDefault();
            if (table != Vector3.zero)
            {
                // drive the arc from just above the table so it comes down on its top
                var dir = Flat(table - _headPos).normalized; _filmCap = "4  Arc coming down on a TABLE TOP -> refused";
                _r.palmNormal = Vector3.left; _r.transform.SetPositionAndRotation(table + Vector3.up * 0.5f - dir * 0.3f, Quaternion.LookRotation(Quaternion.AngleAxis(60f, Vector3.Cross(Vector3.up, dir)) * dir));
                yield return new WaitForSeconds(0.1f); _r.simGrip = 0.9f; _r.simGun = true; yield return new WaitForSeconds(0.8f);
                bool valid3 = _rig.HopAimValid; string why3 = _rig.LastHopRefusal; r0 = _rig.transform.position; hops0 = _rig.Hops; yield return Release();
                Ck("never_onto_furniture", !valid3 && _rig.Hops == hops0, $"arc coming down on a table top: {(valid3 ? "VALID" : "refused: " + why3)}");
            }

            // 4) the left hand still walks
            // face the most open direction first (walking into a wall stops the character controller)
            { Vector3 open = Vector3.forward; float far = 0f; for (int a = 0; a < 16; a++) { var dd = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward; float dist = Physics.SphereCast(_headPos + Vector3.down * 0.9f, 0.25f, dd, out var oh, 6f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore) ? oh.distance : 6f; if (dist > far) { far = dist; open = dd; } } _headRot = Quaternion.LookRotation(open); yield return null; }
            r0 = _rig.transform.position; var anchor = _headPos + new Vector3(-0.2f, -0.4f, 0.25f); var head0 = _headPos;
            var fwd = Flat(_rig.Head.forward).normalized;
            _filmCap = "5  LEFT hand: finger gun, thumb UP = ready (nothing moves yet)";
            // finger gun pointing forward, thumb up (cocked): nothing moves yet
            _l.palmNormal = Vector3.right; _l.transform.SetPositionAndRotation(anchor, Quaternion.LookRotation(fwd + Vector3.down * 0.3f)); _l.simGrip = 0.9f; _l.simGun = true; _l.simThumbDown = false;
            yield return new WaitForSeconds(0.5f);
            float cocked = Moved(r0).magnitude; bool ready = _rig.WalkReadyHand == 0;
            // hold the thumb down for 1 s: walk where the finger points (my head and hand ride along with the rig)
            yield return new WaitForSeconds(0.8f);
            var handLocal = _rig.transform.InverseTransformPoint(anchor); _l.simThumbDown = true; _filmCap = "5  HOLD the thumb down = walk where the finger points";
            for (float t = 0; t < 1.0f; t += Time.deltaTime) { _l.transform.position = _rig.transform.TransformPoint(handLocal); _headPos = head0 + Moved(r0); yield return null; }
            var walked = Moved(r0); _l.simThumbDown = false; _filmCap = "5  Lift the thumb = stop"; yield return new WaitForSeconds(0.3f);
            float afterStop = (Moved(r0) - walked).magnitude;
            _l.simGun = false; _l.simGrip = 0f; _l.palmNormal = Vector3.down; yield return new WaitForSeconds(0.2f);
            Ck("left_finger_gun_walks", cocked < 0.01f && ready && walked.magnitude > 0.5f && Vector3.Dot(walked.normalized, fwd) > 0.8f && afterStop < 0.1f && _rig.Hops == hops0,
               $"cocked (thumb up) 0.5 s: moved {cocked:0.00} m, ready {ready}; thumb held down 1 s: walked {walked.magnitude:0.00} m, {Vector3.Dot(walked.normalized, fwd):0.00} along the finger; after lifting the thumb {afterStop:0.00} m; hops {_rig.Hops - hops0}");

            // 4b) a poke (index out, thumb tucked from the start) never cocks the gun; a finger gun on an item never grabs it
            yield return new WaitForSeconds(0.8f); _filmCap = "6  A button poke (thumb tucked from the start) never hops";
            {
                _r.palmNormal = Vector3.left; _r.transform.SetPositionAndRotation(_headPos + new Vector3(0.15f, -0.3f, 0.35f), Quaternion.LookRotation(Vector3.forward + Vector3.down * 0.5f));
                _r.simGrip = 0.9f; _r.simThumbDown = true; _r.simGun = true; yield return new WaitForSeconds(0.6f);
                bool armed = _rig.AimingHand == 1 || _rig.Hops != hops0; _r.simGun = false; _r.simThumbDown = false; _r.simGrip = 0f; yield return new WaitForSeconds(0.2f);
                var item = _room.Props.Where(pb => pb != null && pb.Col != null && !pb.IsHeld).OrderBy(pb => Flat(pb.transform.position - _headPos).magnitude).FirstOrDefault();
                bool grabbed = false; string what = "-";
                if (item != null)
                {
                    _filmCap = "6  A finger gun on an item never grabs it"; what = item.Spec.Kind; var top = item.Col.ClosestPoint(item.Col.bounds.center + Vector3.up * 0.5f);
                    _r.transform.SetPositionAndRotation(top + Vector3.up * 0.01f, Quaternion.LookRotation(Vector3.forward + Vector3.down)); yield return new WaitForSeconds(0.3f);
                    _r.simGrip = 0.9f; _r.simGun = true; yield return new WaitForSeconds(0.8f);
                    grabbed = _rig.RightHand.IsGrabbing; _r.simGun = false; _r.simGrip = 0f; yield return new WaitForSeconds(0.3f);
                }
                Ck("finger_gun_is_not_a_grab_or_poke", !armed && !grabbed && item != null, $"poke pose (thumb tucked from the start): {(armed ? "ARMED a hop" : "never armed")}; finger gun on {what}: {(grabbed ? "GRABBED" : "not grabbed")}");
            }

            // 4c) V13 (owner, Quest c18: "both hands walk"): an older save with "Walk only" is reset once to right hand hops / left hand walks;
            // a style chosen after that reset is kept. The Editor's own saved settings are restored afterwards.
            {
                const string key = "shhh_settings_v1"; string orig = PlayerPrefs.GetString(key, "");
                var old = new Settings { HopRightHand = false, TeleportMode = false, LocoVersion = 0 };
                PlayerPrefs.SetString(key, JsonUtility.ToJson(old)); Settings.Load();
                bool reset = Settings.Current.HopRightHand && !Settings.Current.TeleportMode && Settings.Current.LocoVersion == Settings.CurrentLocoVersion;
                Settings.Current.HopRightHand = false; Settings.Save(); Settings.Load(); bool kept = !Settings.Current.HopRightHand;
                if (string.IsNullOrEmpty(orig)) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetString(key, orig);
                Settings.Load(); _rig.ApplyLocomotion(new Settings());
                Ck("old_walk_only_save_reset_once", reset && kept, $"old save 'Walk only' -> after loading: {(reset ? "right hand hops, left walks" : "STILL " + PlayerRig.LocomotionName(Settings.Current))}; 'Walk only' chosen again afterwards: {(kept ? "kept" : "RESET AGAIN")}");
            }

            // 5) the wrist button cycles walk + hop -> walk only -> hop only -> walk + hop
            var names = new List<string>(); var s2 = new Settings();
            for (int i = 0; i < 3; i++) names.Add(PlayerRig.CycleLocomotion(s2));
            Ck("wrist_cycles_styles", names[0] == "Walk only" && names[1] == "Hop only" && names[2].StartsWith("Walk + hop"), "default 'Walk + hop (right hand)' -> " + string.Join(" -> ", names));
            _filmCap = "Done: all checks recorded"; yield return new WaitForSeconds(1.0f); _filming = false; yield return null;
            RoomRuntime.QaPauseBrains = false;
        }


        /// <summary>V12 `recipe_diag stage12` (owner, Quest frame 2026-11-12: the front-hall Family Stage showed the old big-head stand-ins for
        /// Tess / Rory / Nell next to a Genies Pip - "from the beginning they should have Genies avatars, not fallback characters"). The stage
        /// is built in front of me; what each kid shows is sampled every 0.5 s until all four wear Genies (or 120 s).</summary>
        IEnumerator StageLab()
        {
            _cur = "STAGE12"; _rep.recipes.Add("STAGE12");
            var old = FindObjectsByType<FamilyStage>(FindObjectsSortMode.None); foreach (var o in old) Destroy(o.gameObject);
            yield return null;
            _headPos = new Vector3(0f, 1.55f, -2.2f); _headRot = Quaternion.LookRotation(new Vector3(0f, -0.25f, 1f));
            var root = new GameObject("QA_StageRoot").transform; root.position = new Vector3(0f, 0f, 0f);
            int req0 = AvatarService.LoadsRequested, built0 = AvatarService.LoadsBuilt;
            float t0 = Time.time; var stage = FamilyStage.Build(root, Vector3.zero, 180f);
            var sibs = stage.GetComponentsInChildren<SiblingAvatar>(true);
            string State(SiblingAvatar a) => a == null ? "gone" : a.HasGeniesBody ? "genies" : a.ShowsStandIn ? "STAND-IN" : "waiting";
            var tl = new List<string>(); string last = ""; float allAt = -1f; int standInFrames = 0, samples = 0;
            yield return Late(() => Shot("stage12_t0"));
            while (Time.time - t0 < 120f)
            {
                sibs = stage.GetComponentsInChildren<SiblingAvatar>(true);
                var now = string.Join(",", sibs.Select(State)); samples++;
                if (sibs.Any(a => a != null && a.ShowsStandIn)) standInFrames++;
                if (now != last) { last = now; tl.Add($"{Time.time - t0:0.0}s {now}"); }
                if (sibs.Length == 4 && sibs.All(a => a != null && a.HasGeniesBody)) { allAt = Time.time - t0; break; }
                if (Mathf.Abs(Time.time - t0 - 4f) < 0.26f) yield return Late(() => Shot("stage12_t4"));
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(1f); yield return Late(() => Shot("stage12_loaded"));
            _rep.events.Add("STAGE12 timeline: " + string.Join(" | ", tl));
            _rep.events.Add($"STAGE12 backend {AvatarService.Status}; loads requested {AvatarService.LoadsRequested - req0}, built {AvatarService.LoadsBuilt - built0}, skipped {AvatarService.LoadsSkipped}");
            Ck("all_four_wear_genies", allAt >= 0f, allAt >= 0f ? $"all four kids wear Genies after {allAt:0.0} s" : "not all four loaded in 120 s: " + last);
            Ck("no_stand_in_kids_shown", standInFrames == 0, $"old big-head stand-ins visible in {standInFrames}/{samples} samples while the Genies bodies loaded");
            // picking another kid must not throw the loaded bodies away (each rebuild restarted all four loads)
            int built1 = AvatarService.LoadsBuilt; stage.Pick(2); yield return new WaitForSeconds(1.0f);
            sibs = stage.GetComponentsInChildren<SiblingAvatar>(true);
            Ck("pick_keeps_bodies", sibs.Count(a => a != null && a.HasGeniesBody) >= 3, $"after picking Rory: {string.Join(",", sibs.Select(State))}; new builds {AvatarService.LoadsBuilt - built1}");
            stage.Pick(0); Destroy(root.gameObject);
            // the starter looks ship with the game (a fresh Quest install has no locally built looks)
            var ta = Resources.Load<TextAsset>("Genies/shhh_genies_presets_v3");
            var pc = ta != null ? JsonUtility.FromJson<QaPresetCache>(ta.text) : null;
            int clothed = pc?.defs != null && AvatarService.Backend != null ? pc.defs.Count(d => !string.IsNullOrEmpty(d) && AvatarService.Backend.IsClothed(d)) : 0;
            Ck("starter_looks_ship_with_the_game", clothed == 5, $"Resources/Genies/shhh_genies_presets_v3: {(ta != null ? $"{pc?.defs?.Length ?? 0} looks, {clothed} clothed" : "MISSING")} (Pip, Nell, Rory, Tess, Mom)");
        }

        [Serializable] class QaPresetCache { public string[] defs; }


        /// <summary>V12 `recipe_diag stable12` (owner, Quest c15: "a lot of vibration when moving or turning my head - nothing is stable").
        /// Real head motion (no locomotion) must never move the rig: the head leans over a table, sways and turns in place, standing in the
        /// open and with its capsule against furniture; the rig's own movement is measured every frame.</summary>
        IEnumerator StableLab()
        {
            _cur = "STABLE12"; _rep.recipes.Add("STABLE12");
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 7, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.QaPauseBrains = true;
            _room = new GameObject("V12_Stable").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 7);
            foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb != null) bb.Stop(); }
            _rig.LocomotionEnabled = true; _l.simPinch = 0; _r.simPinch = 0; _l.palmNormal = Vector3.down; _r.palmNormal = Vector3.down;
            var spawn = _room.SpawnFor(0); _rig.Spawn(spawn, 0f); yield return new WaitForSeconds(1.5f);
            { float w0 = 0f; while (w0 < 30f && (_room.SelfBody == null || _room.SelfBody.Body == null || !_room.SelfBody.Body.Calibrated)) { w0 += Time.deltaTime; yield return null; } }
            // a table near the spawn to lean over
            var table = _room.Props.Where(pb => pb.Spec.Grabbable && pb.transform.position.y - _room.transform.position.y > 0.5f && pb.transform.position.y - _room.transform.position.y < 0.9f)
                                   .OrderBy(pb => Flat(pb.transform.position - spawn).magnitude).Select(pb => pb.transform.position).FirstOrDefault();
            var scenes = new List<(string name, Vector3 at, Vector3 look)> { ("open floor", spawn + Vector3.up * 1.55f, Vector3.forward) };
            if (table != Vector3.zero) { var toT = Flat(table - spawn).normalized; scenes.Add(("leaning over a table", table - toT * 0.15f + Vector3.up * 0.55f, toT + Vector3.down * 1.2f)); scenes.Add(("standing at a table", table - toT * 0.45f + Vector3.up * 1.0f, toT + Vector3.down * 0.6f)); }
            float worst = 0f; string worstWhere = ""; var rows = new List<string>(); var bodyRows = new List<string>(); float worstBody = 0f;
            foreach (var (nm, at, look) in scenes)
            {
                _headPos = at; _headRot = Quaternion.LookRotation(look); yield return new WaitForSeconds(0.5f);
                var r0 = _rig.transform.position; var rr0 = _rig.transform.rotation; float maxMove = 0f, path = 0f; var last = r0;
                var body = _room.SelfBody != null ? _room.SelfBody.Body : null;
                var chest = body != null ? body.A.GetBoneTransform(HumanBodyBones.Chest) : null; var hand = body != null ? body.A.GetBoneTransform(HumanBodyBones.RightHand) : null;
                var hist = new List<(Vector3 h, Vector3 c, Vector3 r)>();
                for (float t = 0; t < 4f; t += Time.deltaTime)
                {
                    // small real-life head motion: sway 4 cm, bob 2 cm, turn +-35 deg, nod +-15 deg
                    _headPos = at + new Vector3(Mathf.Sin(t * 2.1f) * 0.04f, Mathf.Sin(t * 3.3f) * 0.02f, Mathf.Cos(t * 1.7f) * 0.03f);
                    _headRot = Quaternion.LookRotation(look) * Quaternion.Euler(Mathf.Sin(t * 1.9f) * 15f, Mathf.Sin(t * 1.3f) * 35f, 0f);
                    yield return null;
                    var p = _rig.transform.position; maxMove = Mathf.Max(maxMove, Vector3.Distance(p, r0)); path += Vector3.Distance(p, last); last = p;
                    if (chest != null && hand != null) hist.Add((_rig.Head.position, chest.position - _rig.Head.position, hand.position));
                }
                // jitter = mean |second difference| per frame (smooth motion ~0; shaking = large), body relative to the head
                float Jerk(System.Func<(Vector3 h, Vector3 c, Vector3 r), Vector3> f) { if (hist.Count < 3) return 0f; float sum = 0f; for (int i = 2; i < hist.Count; i++) sum += (f(hist[i]) - 2f * f(hist[i - 1]) + f(hist[i - 2])).magnitude; return sum / (hist.Count - 2); }
                if (hist.Count > 3) { float jh = Jerk(x => x.h), jc = Jerk(x => x.c), jr = Jerk(x => x.r); bodyRows.Add($"{nm}: head {jh * 1000f:0.00} mm, chest-vs-head {jc * 1000f:0.00} mm, right hand {jr * 1000f:0.00} mm per frame"); worstBody = Mathf.Max(worstBody, jc); }
                float turn = Quaternion.Angle(rr0, _rig.transform.rotation);
                rows.Add($"{nm}: rig moved up to {maxMove * 1000f:0.0} mm (path {path * 1000f:0} mm), turned {turn:0.0} deg");
                if (maxMove > worst) { worst = maxMove; worstWhere = nm; }
            }
            _rep.events.Add("STABLE12 " + string.Join(" | ", rows));
            Ck("head_motion_never_moves_the_rig", worst < 0.002f, $"{string.Join("; ", rows)} (worst: {worstWhere})");
            Ck("my_body_does_not_shake", bodyRows.Count > 0 && worstBody < 0.0015f, bodyRows.Count > 0 ? string.Join("; ", bodyRows) : "no tracked self body");
            RoomRuntime.QaPauseBrains = false;
        }


        /// <summary>V12 `recipe_diag grip12` (owner, Quest c15: "grab works, ungrab does not - everything gets stuck"). The grip value that
        /// decides release, computed the c15 way (tip-to-palm / wrist-to-knuckle) and the new way (each finger's own straightness) on
        /// synthetic tracked hands from wide open to a fist, for both hand sizes the Quest reports.</summary>
        IEnumerator GripLab()
        {
            _cur = "GRIP12"; _rep.recipes.Add("GRIP12"); yield return null;
            var poses = new (string name, float[][] c, float thumb)[] {
                ("open",    new[] { new[] { 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f } }, 0f),
                ("relaxed", new[] { new[] { 5f, 10f }, new[] { 8f, 15f, 10f }, new[] { 10f, 18f, 12f }, new[] { 12f, 20f, 12f }, new[] { 15f, 22f, 15f } }, 10f),
                ("half",    new[] { new[] { 15f, 20f }, new[] { 35f, 45f, 30f }, new[] { 35f, 45f, 30f }, new[] { 35f, 45f, 30f }, new[] { 35f, 45f, 30f } }, 20f),
                ("pinch",   new[] { new[] { 30f, 30f }, new[] { 45f, 45f, 30f }, new[] { 20f, 25f, 15f }, new[] { 20f, 25f, 15f }, new[] { 20f, 25f, 15f } }, 30f),
                ("fist",    new[] { new[] { 25f, 35f }, new[] { 75f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f } }, 35f) };
            var rows = new List<string>(); var newG = new Dictionary<string, float>(); var oldG = new Dictionary<string, float>();
            foreach (var (nm, c, th) in poses)
            {
                var j = SynthHand(Vector3.zero, Quaternion.identity, false, c, th);
                float size = Mathf.Max(0.05f, Vector3.Distance(j[0], j[7]));
                float og = (OvrHandSource.Curl(j[18], j[1], size) + OvrHandSource.Curl(j[19], j[1], size) + OvrHandSource.Curl(j[20], j[1], size)) / 3f;
                float ng = (OvrHandSource.FingerCurl(j[7], j[8], j[9], j[18]) + OvrHandSource.FingerCurl(j[10], j[11], j[12], j[19]) + OvrHandSource.FingerCurl(j[13], j[14], j[15], j[20])) / 3f;
                oldG[nm] = og; newG[nm] = ng; rows.Add($"{nm}: c15 grip {og:0.00} -> new {ng:0.00}");
            }
            _rep.events.Add("GRIP12 " + string.Join(" | ", rows));
            Ck("open_hand_reads_open", newG["open"] < 0.1f && newG["relaxed"] < 0.4f, $"new grip open {newG["open"]:0.00}, relaxed {newG["relaxed"]:0.00} (release below 0.4); c15: open {oldG["open"]:0.00}, relaxed {oldG["relaxed"]:0.00}");
            Ck("fist_reads_closed", newG["fist"] > 0.7f, $"new grip fist {newG["fist"]:0.00} (grab above 0.7); c15 {oldG["fist"]:0.00}; half {newG["half"]:0.00}");
        }


        /// <summary>V12 `recipe_diag practice12` (owner, Quest c15 practice: mug / plate gone after a crash, instruction board far and tiny,
        /// a red glove-shaped hint inside the book). The practice room as the game builds it.</summary>
        IEnumerator PracticeLab()
        {
            _cur = "PRACTICE12"; _rep.recipes.Add("PRACTICE12");
            var roster = new CompetitorRoster(); roster.SeatHuman(0, "Pip"); roster.LockForMatch();
            for (int i = 1; i < 4; i++) roster.Eliminate(i, EliminationReason.None, -1);
            _round = new RoundController(RoundConfig.PracticeConfig(), roster, 7, new GameLog());
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.PracticeMode = true; GameServices.LocalSlot = 0; GameServices.IsAuthority = true;
            RoomRuntime.NoSelfBody = true;
            _room = new GameObject("V12_Practice").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _round.Begin(); _room.Bind(_round, 7);
            RoomRuntime.NoSelfBody = false;
            var tidy = _room.gameObject.AddComponent<PracticeTidy>(); tidy.Room = _room;
            MenuBuilder.ShowTutorial(GameRoot.I, _room);
            var spawn = _room.SpawnFor(0); _headPos = spawn + Vector3.up * 1.55f; _headRot = Quaternion.LookRotation(Vector3.forward);
            yield return new WaitForSeconds(1.5f);
            // the board: near the bench, turned to me
            var board = GameObject.Find("TutorialBoard");
            if (board != null)
            {
                float d = Vector3.Distance(board.transform.position, _headPos); var to = board.transform.position - _headPos; to.y = 0f; var f = board.transform.forward; f.y = 0f;
                float ang = Vector3.Angle(f, to);
                _headRot = Quaternion.LookRotation(board.transform.position - _headPos); yield return new WaitForSeconds(0.2f); yield return Late(() => Shot("practice12_board"));
                Ck("practice_board_readable", d < 2.0f && ang < 35f, $"board {d:0.00} m from my eyes at the start (was ~4 m on the far wall), turned {ang:0} deg off my line of sight, scale {board.transform.localScale.x:0.00}x");
                _headRot = Quaternion.LookRotation(Vector3.forward);
            }
            else Ck("practice_board_readable", false, "no TutorialBoard");
            // a knocked-off cup comes back
            var cup = _room.Props.FirstOrDefault(pb => pb.Spec.Kind == "cup");
            if (cup != null)
            {
                var home = _room.transform.TransformPoint(VisualFactory.V(cup.Spec.Spawn));
                var floorAt = new Vector3(home.x + 0.6f, _room.transform.position.y + 0.06f, home.z - 0.4f);
                cup.Body.position = floorAt; cup.transform.position = floorAt; cup.Body.linearVelocity = Vector3.zero;
                yield return new WaitForSeconds(2.0f); float mid = Vector3.Distance(cup.transform.position, home);
                yield return new WaitForSeconds(4.0f); float after = Vector3.Distance(cup.transform.position, home);
                Ck("practice_items_come_back", mid > 0.3f && after < 0.15f, $"cup knocked to the floor: after 2 s still there ({mid:0.00} m from its spot), after 6 s back ({after:0.00} m); returned {tidy.Returned}");
            }
            // the demo: the glowing copy is not on the real item while the ghost hand reaches in
            DutyDemo.Play(_room, _round, 0); yield return null;
            var demo = FindFirstObjectByType<DutyDemo>();
            if (demo != null)
            {
                var copy = demo.transform.Find("ghostItem");
                yield return new WaitForSeconds(0.2f); bool earlyHidden = copy != null && !copy.gameObject.activeSelf;
                yield return new WaitForSeconds(0.9f); bool laterShown = copy != null && copy.gameObject.activeSelf;
                Ck("demo_copy_not_inside_the_item", earlyHidden && laterShown, $"glowing copy while the ghost hand reaches in: {(earlyHidden ? "hidden" : "SHOWN on the real item")}; while it carries: {(laterShown ? "shown" : "hidden")}");
            }
            else _rep.events.Add("PRACTICE12 no duty demo in practice (no bonus job) - nothing to check");
        }

        /// <summary>`recipe_diag siblings`: the four siblings (detached pieces) in idle and walking, captured front / side / back.</summary>
        IEnumerator Siblings()
        {
            _cur = "SIBLINGS";
            if (_room) { Destroy(_room.gameObject); yield return null; }
            // neutral studio far from the house: floor, key + fill light, the four siblings in a row
            var studio = new GameObject("studio").transform; studio.position = new Vector3(60f, 0f, 60f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.SetParent(studio, false); floor.transform.localScale = Vector3.one * 2f; floor.GetComponent<Renderer>().sharedMaterial = Mats.Lit(new Color(0.55f, 0.5f, 0.62f));
            var key = new GameObject("key").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.3f; key.transform.SetParent(studio, false); key.transform.rotation = Quaternion.Euler(40, 150, 0);
            var fill = new GameObject("fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.6f; fill.transform.SetParent(studio, false); fill.transform.rotation = Quaternion.Euler(20, -30, 0);
            var avs = new List<SiblingAvatar>(); string[] names = { "Pip", "Nell", "Rory", "Tess" };
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject("sib_" + names[i]); g.transform.SetParent(studio, false); g.transform.localPosition = new Vector3((i - 1.5f) * 0.8f, 0, 0);
                // wardrobe check: Pip wears one of everything, the others a deterministic bot outfit
                int look = i == 0 ? Outfit.Encode(0, new Dictionary<string, string> { ["hat"] = "hat_crown", ["glasses"] = "glasses_round", ["accessory"] = "accessory_teddy", ["pyjama"] = "pyjama_stars" })
                    : i == 1 ? Outfit.Encode(1, new Dictionary<string, string> { ["hat"] = "hat_beanie", ["glasses"] = "glasses_star", ["accessory"] = "accessory_magnifier" })
                    : i == 2 ? Outfit.Encode(2, new Dictionary<string, string> { ["hat"] = "hat_nightcap", ["accessory"] = "accessory_toolbelt", ["glasses"] = "glasses_sleepmask" }) : i;
                var a = g.AddComponent<SiblingAvatar>(); a.Build(i, names[i], true, look); avs.Add(a);
                Ck("outfit_" + names[i], i == 3 ? a.Outfit == 0 : a.Outfit >= 3, $"{a.Outfit} wardrobe pieces");
                // which way does the face point? (mouth/face renderer centre relative to the head, in avatar space)
                var mouth = a.Rig != null ? a.Rig.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.Contains("Mouth")) : null;
                if (mouth != null && a.Head != null) { var lz = a.transform.InverseTransformPoint(mouth.bounds.center).z - a.transform.InverseTransformPoint(a.Head.position).z; Ck("face_forward_" + names[i], lz > 0, $"mouth is {lz:+0.000;-0.000} m along the avatar's forward from the head bone"); }
                var bones = a.Rig != null ? string.Join(",", a.Rig.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r => r.bones).Where(b => b != null).Select(b => b.name).Distinct()) : "no rig";
                var rends = a.Rig != null ? string.Join(",", a.Rig.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).Select(r => r.name)) : "";
                Ck("rig_" + names[i], a.Rig != null, $"renderers [{rends}] bones [{bones}] detach: {a.LastDetach.Meshes} meshes changed");
            }
            yield return new WaitForSeconds(0.5f);
            var c = studio.position + Vector3.up * 0.7f;
            foreach (var (label, dir) in new[] { ("front", Vector3.forward), ("side", Vector3.right), ("back", Vector3.back), ("three_quarter", new Vector3(0.6f, 0, 0.8f).normalized) })
            {
                for (int i = 0; i < 4; i++) avs[i].transform.rotation = Quaternion.identity; // characters face +Z; the camera orbits
                _headPos = c + dir * 4.2f + Vector3.up * 0.3f; _headRot = Quaternion.LookRotation(c - _headPos);
                yield return new WaitForSeconds(0.4f);
                Shot("siblings_" + label);
            }
            for (int i = 0; i < 4; i++) avs[i].transform.rotation = Quaternion.LookRotation(Vector3.right);
            _headPos = c + new Vector3(0.5f, 0.3f, 4.0f); _headRot = Quaternion.LookRotation(c - _headPos);
            for (int k = 0; k < 24; k++) { for (int i = 0; i < 4; i++) avs[i].SetMotion(1.0f, false); yield return new WaitForSeconds(0.05f); }
            Shot("siblings_walking");
            // close-up of one sibling, three angles
            for (int i = 1; i < 4; i++) avs[i].gameObject.SetActive(false);
            foreach (var (label, dir) in new[] { ("pip_front", Vector3.forward), ("pip_side", Vector3.right), ("pip_back", Vector3.back) })
            {
                avs[0].transform.rotation = Quaternion.identity; avs[0].SetMotion(0f, false);
                var pc = avs[0].transform.position + Vector3.up * 0.7f;
                _headPos = pc + dir * 1.9f + Vector3.up * 0.15f; _headRot = Quaternion.LookRotation(pc - _headPos);
                yield return new WaitForSeconds(0.5f);
                Shot("siblings_" + label);
            }
            Destroy(studio.gameObject);
        }

        /// <summary>`recipe_diag stage`: the front-hall Family Stage - siblings visible in their saved wardrobe, pick one with a finger poke.</summary>
        IEnumerator Stage()
        {
            _cur = "STAGE"; _rig = GameRoot.I.Rig;
            var ovr = _rig.GetComponent<OVRCameraRig>(); if (ovr) ovr.enabled = false;
            _l = new GameObject("QA_Sim_Left").AddComponent<SimHandSource>(); _l.Hand = Handedness.Left;
            _r = new GameObject("QA_Sim_Right").AddComponent<SimHandSource>(); _r.Hand = Handedness.Right;
            _rig.Left = _l; _rig.Right = _r; _rig.LeftHand.Source = _l; _rig.RightHand.Source = _r;
            PokeButton.Pokers.Clear(); PokeButton.Pokers.Add(_l); PokeButton.Pokers.Add(_r);
            _cam = _rig.Head.GetComponent<Camera>();
            var st = FamilyStage.I;
            Ck("stage_built", st != null, st != null ? "family stage present" : "missing");
            if (st == null) yield break;
            var sibs = st.GetComponentsInChildren<SiblingAvatar>().ToList();
            Ck("four_siblings", sibs.Count == 4 && sibs.All(a => a.Rig != null), $"{sibs.Count} siblings");
            var c = st.transform.position + Vector3.up * 0.7f;
            _headPos = st.transform.parent.TransformPoint(new Vector3(0, 1.15f, -0.6f)); _headRot = Quaternion.LookRotation(c - _headPos); // GameRoot spawns the rig at (0,0,-0.6), seated eye height
            yield return new WaitForSeconds(0.6f); Shot("stage_view_from_spawn");
            _headPos = c + st.transform.forward * 1.6f + Vector3.up * 0.35f; _headRot = Quaternion.LookRotation(c - _headPos);
            yield return new WaitForSeconds(0.3f); Shot("stage_view");
            int before = Settings.Current.Cosmetic, target = (before + 1) % 4;
            var btn = st.GetComponentsInChildren<PokeButton>().FirstOrDefault(b => b.Label.EndsWith(new[] { "Pip", "Nell", "Rory", "Tess" }[target]));
            if (btn != null) yield return Safe(PokeButtonWithFinger(btn));
            yield return new WaitForSeconds(0.5f);
            Ck("picked_by_poke", Settings.Current.Cosmetic == target, $"cosmetic {before} -> {Settings.Current.Cosmetic}");
            Shot("stage_after_pick");
            Settings.Current.Cosmetic = before; Settings.Save();
        }

        IEnumerator PokeButtonWithFinger(PokeButton btn)
        {
            var h = _r; var fwd = btn.transform.forward;
            h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            Vector3 front = btn.transform.TransformPoint(new Vector3(0, 0, -0.05f)) - fwd * 0.03f, press = btn.transform.TransformPoint(new Vector3(0, 0, 0.006f)) - fwd * 0.03f;
            yield return Safe(Move(h, front, 0.8f));
            int c0 = btn.PressCount; yield return Safe(Move(h, press, 0.2f)); yield return new WaitForSeconds(0.1f);
            yield return Safe(Move(h, front, 0.3f));
            Ck("poke_" + btn.Label.Split(':')[0], btn.PressCount > c0, btn.Label);
        }

        static RoomDefinition RecipeRoomOnly(RoomId rm)
        {
            var src = RoomCatalog.Get(rm); // same filter as RoundController.BuildRecipeRoom: only pet toys stay
            var d = new RoomDefinition { Id = src.Id, Title = src.Title, Width = src.Width, Depth = src.Depth, Height = src.Height, MomDoor = src.MomDoor,
                SiblingSpawns = (Vec3[])src.SiblingSpawns.Clone(), ChoreBoard = src.ChoreBoard, CatBed = src.CatBed, DogBed = src.DogBed };
            d.Furniture.AddRange(src.Furniture); d.SpectatorViews.AddRange(src.SpectatorViews);
            foreach (var p in src.Props) if (p.PetToy) d.Props.Add(p);
            return d;
        }

        string Snap(RecipeInstance inst) => string.Join(",", inst.Satisfied.Select(x => x ? "1" : "0"));

        void Park()
        {
            var b = _headPos + _headRot * new Vector3(0, -0.45f, 0.2f);
            _l.simPinch = _r.simPinch = 0; _l.transform.position = b + _headRot * Vector3.left * 0.2f; _r.transform.position = b + _headRot * Vector3.right * 0.2f;
            _l.transform.rotation = _r.transform.rotation = Quaternion.LookRotation(_headRot * Vector3.forward, Vector3.up);
        }

        RecipeBoard Clone()
        {
            var c = new RecipeBoard();
            foreach (var i in _round.Recipes.Instances) c.Add(i.Def, i.Owner);
            c.World.Time = _round.Recipes.World.Time; c.Restore(_round.Recipes.Snapshot());
            return c;
        }

        /// <summary>Next op the solver would do for this actor, computed on a copy (the real board only changes through hands).</summary>
        ROp? NextOp(RecipeInstance inst, int actor)
        {
            var c = Clone(); var ci = c.Instances[inst.Index]; ROp? first = null;
            RecipeSolver.Solve(c, ci, actor, 120, op => { var e = c.Request(op); if (e == null && first == null) first = op; return e; });
            return first;
        }

        bool _opShots;
        float _budget = 900f; // seconds; "all" gets 75 s per recipe

        IEnumerator Complete(RecipeInstance inst, int actor, int maxOps)
        {
            // follow the solver's whole plan (like a player who knows what to do), re-planning only when an action did not take
            var plan = new List<ROp>(); int replans = 0;
            for (int n = 0; n < maxOps && !inst.Complete; n++)
            {
                if (plan.Count == 0) { plan = RecipeSolver.Plan(_round.Recipes, inst, actor); replans++; if (plan.Count == 0) { Ck("plan", false, "solver has no plan: " + Snap(inst)); yield break; } }
                var op = plan[0]; plan.RemoveAt(0);
                // the slot squares an item that arrives square: a planned straighten is then already done
                if (op.Verb == Verb.Align && op.Arg != "tilt" && _round.Recipes.World.Prop(op.Prop).Oriented) { n--; continue; }
                string snap = _round.Recipes.World.Snapshot(), snapBoard = _round.Recipes.Snapshot();
                yield return Safe(Do(op));
                yield return new WaitForSeconds(0.35f);
                bool changed = _round.Recipes.World.Snapshot() != snap;
                // did the hands produce what the plan expected? (e.g. the slot snapped the item into a different slot) -> re-plan
                var expect = _round.Recipes.Clone(); expect.Restore(snapBoard); var eop = op; eop.EventId = 0; expect.Request(eop);
                bool asPlanned = Key(expect.World) == Key(_round.Recipes.World);
                Ck($"op{n}_{op.Verb}", changed, op + " -> " + Snap(inst) + (asPlanned ? "" : " (differs from plan: re-plan)"));
                if (_opShots) Shot($"{_cur}_op{n:00}_{op.Verb}");
                if (!changed || !asPlanned) { plan.Clear(); if (replans > 12) { Ck("stuck", false, op.ToString()); yield break; } }
            }
        }

        // ------------------------------------------------------------------ physical actions
        SimHandSource HandHolding(PropBody b) { if (_rig.RightHand.Held == b) return _r; if (_rig.LeftHand.Held == b) return _l; return null; }
        HandInteractor Int(SimHandSource s) => s == _r ? _rig.RightHand : _rig.LeftHand;
        SimHandSource Free() => _rig.RightHand.Held == null ? _r : _rig.LeftHand.Held == null ? _l : null;

        IEnumerator Do(ROp op)
        {
            var w = _round.Recipes.World; var p = op.Prop >= 0 ? w.Prop(op.Prop) : null; var b = p != null ? _rt.Body(p.Id) : null;
            var fv = op.Fixture >= 0 ? _rt.FixtureView(op.Fixture) : null;
            // V10: the simulated player WALKS to what it works on (table -> sink -> rack), carrying what it holds; no reaching across the room
            Vector3? focus = fv != null ? fv.transform.position : b != null && HandHolding(b) == null ? b.Col.bounds.center : (Vector3?)null;
            if (op.Verb == Verb.Stroke && fv == null && b != null) focus = null;   // scrubbing a held plate happens wherever we stand
            if (focus.HasValue) yield return Safe(WalkTo(focus.Value));
            switch (op.Verb)
            {
                case Verb.PickUp: yield return Safe(Grab(b)); break;
                case Verb.PutDown: case Verb.Release: yield return Safe(PutOnTable(b)); break;
                case Verb.PlaceInSocket when fv != null && fv.Appliance != null: yield return Safe(LoadDrum(b, fv.Appliance)); break;
                case Verb.PlaceInSocket when fv != null && fv.Host != null: yield return Safe(LoadInto(b, fv.Host, fv.Decl.Kind)); break;   // V12: into a drawer / bin from above   // V12: through the open door into the drum
                case Verb.PlaceInSocket: yield return Safe(PlaceIn(b, fv, op.Slot)); break;
                case Verb.Align:
                {
                    // straighten = lift it out and set it back square (the slot re-checks orientation on the new snap)
                    int fx = p.Fixture >= 0 ? p.Fixture : op.Fixture, sl = p.Slot >= 0 ? p.Slot : op.Slot;
                    yield return Safe(Grab(b)); if (fx >= 0) yield return Safe(PlaceIn(b, _rt.FixtureView(fx), Mathf.Max(0, sl))); break;
                }
                case Verb.Open: case Verb.Close: yield return Safe(Poke(fv, "lid")); break;
                case Verb.Press: yield return Safe(Poke(fv, op.Arg)); break;
                case Verb.Stroke:
                    if (fv != null) yield return Safe(Rub(fv)); else if (p != null && p.StrokeCap > 0) yield return Safe(SpongeScrub(b)); else yield return Safe(Work(b)); break;
                case Verb.Scrub: yield return Safe(SpongeScrub(b)); break;   // V10: stains come off by sponge contact, cell by cell
                case Verb.Fill when fv != null && fv.Tap != null: yield return Safe(RinseUnderTap(b, fv.Tap)); break;
                case Verb.Fill when fv != null && fv.Pump != null: yield return Safe(PumpSoap(b, fv.Pump)); break;
                case Verb.Fold: case Verb.Fill: case Verb.Unfold: yield return Safe(Work(b)); break;
                case Verb.Pair: case Verb.Attach: yield return Safe(Join(b, _rt.Body(op.Prop2))); break;
                case Verb.Unpair: case Verb.Detach: yield return Safe(Split(b)); break;
                default: Ck("unmapped_verb", false, op.ToString()); break;
            }
        }

        public int Walks; public float WalkedMetres;
        /// <summary>Walk (1.3 m/s, head + both hands + whatever they hold) until the focus is ~0.5 m in front of the eyes.</summary>
        IEnumerator WalkTo(Vector3 focus)
        {
            var flat = focus - _headPos; flat.y = 0f;
            if (flat.magnitude < 0.75f) yield break;
            var dir = flat.normalized;
            // V10: carry like a person - anything in hand comes up to chest height and close to the body before walking, so it clears chair
            // backs (0.9 m) and table edges (a plate carried at 0.9 m snagged on a chair and the hand let go: crash)
            foreach (var hh in new[] { _l, _r })
            {
                if (Int(hh).Held == null) continue;
                var chest = _headPos + Vector3.down * 0.32f + Vector3.ProjectOnPlane(_headRot * Vector3.forward, Vector3.up).normalized * 0.22f + (_headRot * Vector3.right) * (hh == _l ? -0.12f : 0.12f);
                var lift = new Vector3(hh.transform.position.x, Mathf.Max(hh.transform.position.y, chest.y), hh.transform.position.z);
                yield return Safe(Move(hh, lift, 0.6f)); yield return Safe(Move(hh, chest, 0.6f));
            }
            var start = _headPos;
            var stand = new Vector3(focus.x, Mathf.Clamp(focus.y + 0.62f, 1.05f, 1.45f), focus.z) - dir * 0.48f;
            var r0 = _headRot; var r1 = Quaternion.LookRotation(focus + Vector3.down * 0.02f - stand, Vector3.up);
            // V12: a person walks AROUND walls and furniture - when the house NavMesh says the straight line is blocked, follow its path
            // (corners at walking speed, turning the body and whatever the hands carry with it). Clear lines walk straight as before.
            var route = RouteAround(start, stand);
            if (route != null)
            {
                float routed = 0f; var at = start;
                foreach (var c in route)
                {
                    var leg = c - at; leg.y = 0f; if (leg.sqrMagnitude < 0.01f) { at = c; continue; }
                    var ry = Quaternion.LookRotation(leg.normalized, Vector3.up);
                    float ld = Mathf.Max(0.25f, leg.magnitude / 1.1f), lt = 0f; var la = _headPos; var lr = _headRot;
                    while (lt < ld)
                    {
                        lt += Time.deltaTime; float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(lt / ld));
                        var np = Vector3.Lerp(la, new Vector3(c.x, Mathf.Lerp(la.y, stand.y, e), c.z), e);
                        var nr = Quaternion.Slerp(lr, ry, Mathf.Clamp01(e * 3f));
                        var yaw = Quaternion.Euler(0f, nr.eulerAngles.y - _headRot.eulerAngles.y, 0f);
                        foreach (var hh in new[] { _l, _r }) { hh.transform.position = np + yaw * (hh.transform.position - _headPos); hh.transform.rotation = yaw * hh.transform.rotation; }
                        _headPos = np; _headRot = nr;
                        _rig.Spawn(new Vector3(np.x, _room.transform.position.y, np.z), _headRot.eulerAngles.y);
                        yield return null;
                    }
                    routed += leg.magnitude; at = _headPos;
                }
                _rep.events.Add($"{_cur} harness: routed around an obstacle ({route.Count} corners, {routed:0.00} m)");
                start = _headPos; r0 = _headRot; WalkedMetres += routed;
            }
            float dur = Mathf.Max(0.3f, Vector3.Distance(start, stand) / 1.3f), t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime; float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / dur));
                var np = Vector3.Lerp(start, stand, e); var d = np - _headPos;
                _headPos = np; _headRot = Quaternion.Slerp(r0, r1, e);
                _l.transform.position += d; _r.transform.position += d;
                _rig.Spawn(new Vector3(np.x, _room.transform.position.y, np.z), _headRot.eulerAngles.y);
                yield return null;
            }
            Walks++; WalkedMetres += Vector3.Distance(start, stand);
            _rep.events.Add($"{_cur} harness: walked {Vector3.Distance(start, stand):0.00} m to {_room.transform.InverseTransformPoint(focus):F2}");
        }

        /// <summary>NavMesh corners (at head height) between two standing points, or null when the straight walk is clear / no path.</summary>
        List<Vector3> RouteAround(Vector3 from, Vector3 to)
        {
            const int all = UnityEngine.AI.NavMesh.AllAreas;
            if (!UnityEngine.AI.NavMesh.SamplePosition(Flat(from), out var ha, 1.5f, all) || !UnityEngine.AI.NavMesh.SamplePosition(Flat(to), out var hb, 1.5f, all)) return null;
            if (!UnityEngine.AI.NavMesh.Raycast(ha.position, hb.position, out _, all)) return null;
            var path = new UnityEngine.AI.NavMeshPath();
            if (!UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, all, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete || path.corners.Length < 3) return null;
            var list = new List<Vector3>();
            for (int i = 1; i < path.corners.Length - 1; i++) list.Add(new Vector3(path.corners[i].x, from.y, path.corners[i].z));
            return list;
        }

        IEnumerator Move(SimHandSource h, Vector3 to, float speed = 0.5f)
        {
            var from = h.transform.position; float dur = Mathf.Max(0.08f, Vector3.Distance(from, to) / speed), t = 0;
            while (t < dur) { t += Time.deltaTime; h.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur)); yield return null; }
            h.transform.position = to;
        }

        IEnumerator Grab(PropBody b)
        {
            if (b == null) yield break;
            if (HandHolding(b) != null) yield break;
            var h = Free(); if (h == null) { var other = _rig.RightHand.Held; yield return Safe(PutOnTable(other)); h = Free(); }
            // V12: an item inside a washer / dryer drum is reached through the open door and pulled out frontwards
            var inDrum = WasherAppliance.All.FirstOrDefault(w => w != null && w.InDrum(b.Col.bounds.center));
            if (inDrum != null)
            {
                if (inDrum.Door != null && inDrum.Door.Opening < 0.5f) yield return Safe(SwingDoor(inDrum, 1f, 0.45f));
                h = Free() ?? h;
                yield return Safe(WalkTo(inDrum.DrumCentre)); FaceAppliance(inDrum, 0.65f, 0.4f);
                h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(-inDrum.Front, Vector3.up);
                var mouth = inDrum.DrumCentre + inDrum.Front * 0.45f;
                yield return Safe(Move(h, mouth, 0.6f)); yield return Safe(Move(h, b.Col.bounds.center, 0.25f));
                h.simPinch = 1; yield return new WaitForSeconds(0.3f);
                if (Int(h).Held == b) { yield return Safe(Move(h, inDrum.DrumCentre + inDrum.Front * 0.5f, 0.25f)); yield return Safe(Move(h, h.transform.position + Vector3.up * 0.35f, 0.4f)); }
                else Ck("grab_from_drum", false, $"{b.Spec.Kind}#{b.Spec.Id}: {Int(h).LastGrabCandidates}");
                yield break;
            }
            h.simPinch = 0; yield return new WaitForSeconds(0.1f);
            var top = b.Col.bounds.center + Vector3.up * Mathf.Max(0.002f, b.Col.bounds.extents.y - 0.012f); // thin items: pinch at the top face
            if (b.Racked || _rt.Bodies.Any(o => o != b && o != null && Vector3.Distance(o.Col.bounds.center, b.Col.bounds.center) < 0.12f))
            {
                // V11: close neighbours (plates 5 cm apart in the rack lean into each other's bounding box): pinch a point INSIDE this item,
                // high on it and on the side away from the others (the run-9 pinch was inside the neighbour, 11 mm outside the target)
                var away = Vector3.zero; foreach (var o in _rt.Bodies) if (o != b && o != null) { var dv = b.Col.bounds.center - o.Col.bounds.center; dv.y = 0; if (dv.magnitude < 0.15f && dv.sqrMagnitude > 1e-6f) away += dv.normalized; }
                var want = b.Col.bounds.center + Vector3.up * b.Col.bounds.extents.y * 0.7f + (away.sqrMagnitude > 1e-6f ? away.normalized * 0.01f : Vector3.zero);
                top = b.Col.ClosestPoint(want);
                top = Vector3.Lerp(top, b.Col.bounds.center, 0.08f);   // a few mm inside the surface, never on it
            }
            h.transform.rotation = Quaternion.LookRotation(_headRot * Vector3.forward, Vector3.up);
            yield return Safe(Move(h, top + Vector3.up * 0.12f, 0.8f)); yield return Safe(Move(h, top, 0.35f));
            float t0 = Vector3.Angle(b.transform.up, Vector3.up);
            h.simPinch = 1; yield return new WaitForSeconds(0.3f);
            if (Int(h).Held != null && Int(h).Held != b)
            {
                // V11: took the neighbour - let it go where it is and re-grip INSIDE the intended item, on the side away from other props
                var wrong = Int(h).Held; _rep.events.Add($"{_cur} harness: grab {b.Spec.Kind}#{b.Spec.Id} took {wrong.Spec.Kind}#{wrong.Spec.Id} ({Int(h).LastGrabCandidates}) - re-gripping");
                h.simPinch = 0; yield return new WaitForSeconds(0.25f);
                var away = Vector3.zero; foreach (var o in _rt.Bodies) if (o != b && o != null) { var dv = b.Col.bounds.center - o.Col.bounds.center; dv.y = 0; if (dv.magnitude < 0.15f && dv.sqrMagnitude > 1e-6f) away += dv.normalized; }
                var aim = b.Col.ClosestPoint(b.Col.bounds.center + Vector3.up * b.Col.bounds.extents.y * 0.8f + (away.sqrMagnitude > 1e-6f ? away.normalized * 0.03f : Vector3.zero));
                yield return Safe(Move(h, aim + Vector3.up * 0.1f, 0.5f)); yield return Safe(Move(h, aim, 0.3f));
                h.simPinch = 1; yield return new WaitForSeconds(0.3f);
            }
            float t1 = Vector3.Angle(b.transform.up, Vector3.up);
            if (Int(h).Held != b) Ck("grab", false, $"{b.Spec.Kind}#{b.Spec.Id}: {Int(h).LastGrabCandidates}; {Int(h).LastGrabFailure} (holding {(Int(h).Held ? Int(h).Held.Spec.Kind + "#" + Int(h).Held.Spec.Id : "nothing")}; item kinematic {b.Body.isKinematic}, reserved {b.Reserved}, held by {b.HeldBy}, pos {b.Col.bounds.center:F3}, hand {h.transform.position:F3})");
            else yield return Safe(Move(h, h.transform.position + Vector3.up * 0.12f, 0.4f));
            float t2 = Vector3.Angle(b.transform.up, Vector3.up);
            if (t2 > 20f || t1 > 20f) _rep.events.Add($"{_cur} harness: grab {b.Spec.Kind}#{b.Spec.Id} tilt before {t0:0}° after pinch {t1:0}° after lift {t2:0}°; kinematic {b.Body.isKinematic}; follower {_rt.IsFollower(_rt.RProp(b))}");
        }

        IEnumerator Drop(SimHandSource h, PropBody b, Vector3 restCentre)
        {
            // move so the item's collider centre ends just above restCentre, then open the hand
            var off = h.transform.position - b.Col.bounds.center;
            // careful path: lift clear first, travel over, then lower (never sweep the item through other things)
            float safeY = Mathf.Max(h.transform.position.y, restCentre.y + off.y + 0.22f);
            yield return Safe(Move(h, new Vector3(h.transform.position.x, safeY, h.transform.position.z), 0.4f));
            yield return Safe(Move(h, new Vector3(restCentre.x + off.x, safeY, restCentre.z + off.z), 0.5f));
            yield return Safe(Move(h, restCentre + off + Vector3.up * 0.1f, 0.3f));
            yield return Safe(Move(h, restCentre + off + Vector3.up * 0.012f, 0.2f));
            yield return new WaitForSeconds(0.15f);
            h.simPinch = 0; yield return new WaitForSeconds(0.25f);
            yield return Safe(Move(h, h.transform.position + Vector3.up * 0.12f, 0.5f));
        }

        IEnumerator PutOnTable(PropBody b)
        {
            if (b == null) yield break;
            // an item that is now joined to its partner is simply let go where it is (it rides on the partner)
            var jp = _rt.RProp(b); var held0 = HandHolding(b);
            if (jp != null && held0 != null && _rt.IsFollower(jp))
            { held0.simPinch = 0; yield return new WaitForSeconds(0.3f); yield return Safe(Move(held0, held0.transform.position + Vector3.up * 0.1f, 0.4f)); yield break; }
            if (jp != null && held0 != null && _round.Recipes.World.Props.Any(o => o.AttachedTo == jp.Id || o.Partner == jp.Id))
            {   // carrying the base of an assembly (riders follow it): carry it level back over its spawn spot, then set it down
                // (a gesture such as rolling may have brought it off the table, toward the player)
                var sp = _room.transform.TransformPoint(VisualFactory.V(b.Spec.Spawn));
                var hpos = held0.transform.position; float liftY = Mathf.Max(hpos.y, sp.y + b.Col.bounds.size.y + 0.15f);
                yield return Safe(Move(held0, new Vector3(hpos.x, liftY, hpos.z), 0.4f));
                var off = held0.transform.position - b.Col.bounds.center;
                yield return Safe(Move(held0, new Vector3(sp.x + off.x, liftY, sp.z + off.z), 0.5f));
                var c = b.Col.bounds.center; yield return Safe(Drop(held0, b, new Vector3(c.x, sp.y + b.Col.bounds.extents.y + 0.005f, c.z))); yield break;
            }
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            var spawn = _room.transform.TransformPoint(VisualFactory.V(b.Spec.Spawn));
            // thin sheets (drawings, cards, envelopes) go beside a pile, never back on top of the one we want next
            var sz = b.Col.bounds.size; if (Mathf.Max(sz.x, sz.z) < 0.35f) spawn = ClearSpot(spawn, b); // big soft items (towels) stay on their own pile
            yield return Safe(Drop(h, b, spawn + Vector3.up * (b.Col.bounds.extents.y + 0.005f)));
        }

        /// <summary>Its spawn spot unless another item already lies there (a loose pile: putting the top sheet back on the pile would
        /// bury the one we want) - then beside it, toward the player.</summary>
        Vector3 ClearSpot(Vector3 sp, PropBody b)
        {
            var toMe = _headPos - sp; toMe.y = 0; toMe = toMe.sqrMagnitude > 1e-4f ? toMe.normalized : Vector3.back;
            var orig = sp;
            for (int k = 0; k < 3; k++)
            {
                var at = sp; bool busy = _rt.Bodies.Any(o => o != null && o != b && !o.IsHeld && Vector2.Distance(new Vector2(o.Col.bounds.center.x, o.Col.bounds.center.z), new Vector2(at.x, at.z)) < 0.15f);
                if (!busy) break;
                var next = sp + toMe * 0.18f;
                // never step off the top it is on (static furniture under the new centre)
                bool under = false;
                foreach (var h in Physics.RaycastAll(next + Vector3.up * 0.3f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore))
                    if ((h.collider.attachedRigidbody == null || h.collider.attachedRigidbody.isKinematic) && h.collider.GetComponentInParent<PropBody>() == null && Mathf.Abs(h.point.y - orig.y) < 0.05f) under = true;
                if (!under) break;
                sp = next;
            }
            return sp;
        }

        IEnumerator PlaceIn(PropBody b, RecipeFixtureView fv, int slot)
        {
            if (b == null || fv == null) yield break;
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            var pt = fv.SlotPoint(slot);
            float floor = pt.y; if (fv.Placement.Stack) foreach (var id in fv.State.Occupant) if (id >= 0 && _rt.Body(id) != null) floor = Mathf.Max(floor, _rt.Body(id).Col.bounds.max.y);
            // right it (if it came up tilted) and square it to the fixture by turning the hand (never by writing the prop transform)
            if (fv.Upright)
            {
                // turn the hand until the held plate stands on edge (re-measured: the grip offset can differ per grab)
                for (int k = 0; k < 3; k++)
                {
                    float flat = Vector3.Angle(b.transform.up, Vector3.up); flat = Mathf.Min(flat, 180f - flat);
                    if (flat >= 70f) break;
                    var tgt = Vector3.Dot(b.transform.up, fv.transform.right) >= 0 ? fv.transform.right : -fv.transform.right;
                    var r0v = h.transform.rotation; var r1v = Quaternion.FromToRotation(b.transform.up, tgt) * r0v;
                    for (float t = 0; t < 1; t += Time.deltaTime / 0.4f) { h.transform.rotation = Quaternion.Slerp(r0v, r1v, t); yield return null; }
                    h.transform.rotation = r1v; yield return new WaitForSeconds(0.15f);
                }
                { float flat = Vector3.Angle(b.transform.up, Vector3.up); flat = Mathf.Min(flat, 180f - flat); if (flat < 35f) _rep.events.Add($"{_cur} harness: {b.Spec.Kind}#{b.Spec.Id} still {flat:0}° from flat after turning the hand (grip follows hand? held {Int(h).Held != null})"); }
                // V10: lower it on edge into the real gap until its rim is ~1.5 cm above the tray floor, open the hand, let it settle
                float rad = b.Col.bounds.extents.y;
                yield return Safe(Drop(h, b, new Vector3(pt.x, pt.y + rad + 0.004f, pt.z)));
                var rpu = _rt.RProp(b); float tw = 0f;
                while (tw < 2f && rpu != null && !rpu.IsPlaced) { tw += Time.deltaTime; yield return null; }
                if (rpu != null && !rpu.IsPlaced && fv.Seating != null) _rep.events.Add($"{_cur} harness: plate#{b.Spec.Id} not seated after release into slot {slot}: {fv.Seating.Describe(b)}; aimed gap {fv.Seating.GapOfSlot(slot)}; held {Int(h).Held != null}");
                yield break;
            }
            float tilt0 = Vector3.Angle(b.transform.up, Vector3.up);
            if (tilt0 > 20f)
            {
                _rep.events.Add($"{_cur} harness: {b.Spec.Kind}#{b.Spec.Id} held tilted {tilt0:0}° (hand {h.transform.eulerAngles:F0}, item {b.transform.eulerAngles:F0}) - turning the hand to right it");
                var r0u = h.transform.rotation; var r1u = Quaternion.FromToRotation(b.transform.up, Vector3.up) * r0u;
                for (float t = 0; t < 1; t += Time.deltaTime / 0.35f) { h.transform.rotation = Quaternion.Slerp(r0u, r1u, t); yield return null; }
                h.transform.rotation = r1u; yield return new WaitForSeconds(0.15f);
            }
            float rel = Mathf.DeltaAngle(0, RecipeFixtureView.YawOf(b.transform) - RecipeFixtureView.YawOf(fv.transform)), q = Mathf.Round(rel / 90f) * 90f;
            if (Mathf.Abs(rel - q) > 5f) { var r0 = h.transform.rotation; var r1 = Quaternion.Euler(0, q - rel, 0) * r0; for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; } h.transform.rotation = r1; }
            if (fv.WallHeight > 0) { var off = h.transform.position - b.Col.bounds.center; yield return Safe(Move(h, new Vector3(pt.x, fv.Top + fv.WallHeight + b.Col.bounds.extents.y + 0.06f, pt.z) + off, 0.6f)); }
            yield return Safe(Drop(h, b, new Vector3(pt.x, floor + b.Col.bounds.extents.y + 0.004f, pt.z)));
            yield return new WaitForSeconds(0.45f);
        }

        /// <summary>V10: operate the mixer lever like a hand would - touch its tip, drag it along its arc to the target opening, slide off sideways.</summary>
        IEnumerator TurnTap(SinkTapView tap, float target)
        {
            var h = Free() ?? _l; var off = h.transform.position - h.PalmCenter;
            float from = tap.Level; var start = tap.LeverTipAt(from);
            Vector3 t0 = tap.LeverTipAt(0f), t1 = tap.LeverTipAt(1f); var chord = (t1 - t0).normalized;
            var away = Vector3.Cross(chord, Vector3.up); if (away.sqrMagnitude < 1e-4f) away = tap.transform.right; away.Normalize();
            if (Vector3.Dot(away, start - tap.SpoutWorld) < 0f) away = -away;
            yield return Safe(Move(h, start + away * 0.10f + Vector3.up * 0.05f + off, 0.7f));
            yield return Safe(Move(h, start + off, 0.3f));
            for (float t = 0; t <= 1.001f; t += 0.1f) { yield return Safe(Move(h, tap.LeverTipAt(Mathf.Lerp(from, target, t)) + off, 0.25f)); }
            yield return new WaitForSeconds(0.35f);
            yield return Safe(Move(h, tap.LeverTipAt(target) + away * 0.14f + off, 0.4f));
            Ck(target > 0.01f ? "tap_lever_open" : "tap_lever_close", target > 0.01f ? tap.Running && Mathf.Abs(tap.Level - target) < 0.11f : !tap.Running, $"lever dragged {from:0.00} -> {target:0.00}: level {tap.Level:0.00}, running {tap.Running}, flow {tap.Flow:0.00}");
        }

        IEnumerator Poke(RecipeFixtureView fv, string which)
        {
            if (fv == null) yield break;
            if (fv.Host != null && fv.Appliance == null)
            {
                // V12: house furniture: its door is a real drawer / hinged door / lid moved by the hand
                if (which == "lid")
                {
                    var part = fv.Host.DoorFor(fv.Decl.Kind);
                    if (part is SlidingPart sp) yield return Safe(SlideRail(sp, fv.Host, fv.State.Open ? 0f : 1f, fv.State.Open ? 0.15f : 0.25f));
                    else if (part is HingedPart hp) yield return Safe(SwingHinge(hp, fv.Host, fv.State.Open ? 0f : 1f, fv.State.Open ? 0.3f : 0.45f));
                    else Ck("poke", false, "no door part on " + fv.Decl.Kind);
                }
                yield break;
            }
            if (fv.Appliance != null)
            {
                // V12: the house washer has no poke keys - its door is swung by the handle and START is a real button pressed by a fingertip
                if (which == "lid")
                {
                    if (!fv.State.Open && fv.Appliance.Running) { yield return Safe(PressStart(fv.Appliance)); yield return new WaitForSeconds(1.5f); }   // locked while running: stop it first
                    yield return Safe(SwingDoor(fv.Appliance, fv.State.Open ? 0f : 1f, fv.State.Open ? 0.3f : 0.45f)); yield break;
                }
                bool wantOn = !which.StartsWith("!"); if (fv.State.Flags.Contains(which.TrimStart('!')) != wantOn) yield return Safe(PressStart(fv.Appliance));
                yield break;
            }
            if (fv.Tap != null && which == "lid") { yield return Safe(TurnTap(fv.Tap, fv.State.Open ? 0f : 1f)); yield break; }
            PokeButton btn = null;
            foreach (var pb in fv.GetComponentsInChildren<PokeButton>())
                if (which == "lid" ? (pb.Label == "Open" || pb.Label == "Close" || pb.Label == "Tap on" || pb.Label == "Tap off") : pb.name.Contains("btn_")) { if (which == "lid" || Matches(fv, pb, which)) { btn = pb; break; } }
            if (btn == null) { Ck("poke", false, "no button for " + which + " on " + fv.Placement.Kind); yield break; }
            var h = Free() ?? _l;
            var fwd = btn.transform.forward;
            h.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            Vector3 front = btn.transform.TransformPoint(new Vector3(0, 0, -0.05f)) - fwd * 0.03f, press = btn.transform.TransformPoint(new Vector3(0, 0, 0.006f)) - fwd * 0.03f;
            yield return Safe(Move(h, front + Vector3.up * 0.05f, 0.7f)); yield return Safe(Move(h, front, 0.3f));
            int c0 = btn.PressCount; yield return Safe(Move(h, press, 0.2f)); yield return new WaitForSeconds(0.1f);
            yield return Safe(Move(h, front, 0.3f));
            if (btn.PressCount == c0) Ck("poke_press", false, btn.Label);
        }

        // ------------------------------------------------------------------ V12 appliance hands (desktop simulated hands, NOT Quest)
        void FaceHost(MechHost h, float back = 0.62f, float up = 0.5f)
        {
            if (h is WasherAppliance w) { FaceAppliance(w, back, up); return; }
            var c = h.Focus; _headPos = c + h.Front * back + Vector3.up * up; _headRot = Quaternion.LookRotation(c - Vector3.up * 0.1f - _headPos, Vector3.up);
        }

        /// <summary>V12: put an item INTO a drawer / bin from above (V13: a fridge shelf from the front), after opening it by hand; the host
        /// reports it placed when it rests inside. Square to the furniture (folded towels "folded edge out").</summary>
        IEnumerator LoadInto(PropBody b, MechHost host, string kind)
        {
            if (b == null) yield break;
            if (host.DoorFor(kind) is SlidingPart sd && sd.Opening < 0.6f) yield return Safe(SlideRail(sd, host, 1f));
            if (host is MechFurniture mf && mf.Lid != null && mf.Lid.Opening < 0.6f) yield return Safe(SwingHinge(mf.Lid, host, 1f, 0.45f));
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            yield return Safe(WalkTo(host.Focus)); FaceHost(host, 0.6f, 0.45f);
            var fv = _rt.Fixtures.FirstOrDefault(f => f != null && f.Host == host && f.Decl.Kind == kind); var st = fv?.State; var rp = _rt.RProp(b);
            int slot = 0; if (st != null) { slot = -1; for (int k = 0; k < st.Occupant.Length && slot < 0; k++) if (st.Occupant[k] < 0 && (string.IsNullOrEmpty(st.SlotTags[k]) || st.SlotTags[k] == rp?.Tag)) slot = k; if (slot < 0) slot = 0; }
            var pt = host.SlotPoint(kind, slot);
            // square it to the furniture by turning the hand (never by writing the item transform)
            float rel = Mathf.DeltaAngle(0, RecipeFixtureView.YawOf(b.transform) - RecipeFixtureView.YawOf(host.transform)), q = Mathf.Round(rel / 90f) * 90f;
            if (Mathf.Abs(rel - q) > 5f) { var r0 = h.transform.rotation; var r1 = Quaternion.Euler(0, q - rel, 0) * r0; for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; } h.transform.rotation = r1; }
            var off = h.transform.position - b.Col.bounds.center;
            var loadDir = host.LoadFrom(kind);
            if (Mathf.Abs(loadDir.y) < 0.5f)
            {
                // V13 fridge: through the FRONT, like a person - line up in front of the shelf spot just above the shelf, slide in, set down
                float shelfY = pt.y + b.Col.bounds.extents.y + 0.03f; var outside = pt + loadDir.normalized * 0.45f;
                // first well out in front of the fridge (clear of the open door's arc), then in front of the spot, then in
                var stage = host.Focus + loadDir.normalized * 0.95f;
                yield return Safe(Move(h, new Vector3(stage.x, Mathf.Max(shelfY, 0.9f), stage.z) + off, 0.5f));
                yield return Safe(Move(h, new Vector3(outside.x, shelfY, outside.z) + off, 0.5f));
            }
            else
            {
            // come in ABOVE the rim / drawer top (the slot point is the bin floor): lift first, then travel over, then lower
            float overY = Mathf.Max(pt.y, host.Focus.y) + 0.18f + b.Col.bounds.extents.y;
            yield return Safe(Move(h, new Vector3(h.transform.position.x, Mathf.Max(h.transform.position.y, overY + off.y), h.transform.position.z), 0.4f));
            yield return Safe(Move(h, new Vector3(pt.x, overY, pt.z) + off, 0.4f));
            }
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < 90 && HandHolding(b) != null; k++)
            {
                // lower it straight down into the opening, logging anything it touches on the way in
                // steer by where the ITEM is (a soft item hangs off the fingers): close the horizontal gap first, then lower
                var err = pt + Vector3.up * (b.Col.bounds.extents.y + 0.01f) - b.Col.bounds.center; var flat = new Vector3(err.x, 0f, err.z);
                h.transform.position += Vector3.ClampMagnitude(flat, 0.02f);
                if (flat.magnitude < 0.012f) h.transform.position += Vector3.up * Mathf.Clamp(err.y, -0.015f, 0f);
                var target = h.transform.position;
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                foreach (var x in Physics.OverlapBox(b.Col.bounds.center, b.Col.bounds.extents + Vector3.one * 0.004f, b.transform.rotation, ~Layers.PropsMask, QueryTriggerInteraction.Ignore)) if (sb.Length < 300 && x.gameObject.layer != Layers.Hands) sb.Append(x.name).Append('@').Append(host.transform.InverseTransformPoint(b.Col.bounds.center).ToString("F2")).Append(' ');
                if (flat.magnitude < 0.012f && err.y > -0.006f) break;
            }
            yield return new WaitForSeconds(0.15f); h.simPinch = 0; yield return new WaitForSeconds(0.3f);
            yield return Safe(Move(h, h.transform.position + Vector3.up * 0.25f, 0.4f));
            if (sb.Length > 0) _rep.events.Add($"{_cur} harness: lowering into {kind} touched {sb}; slot point local {host.transform.InverseTransformPoint(pt):F2}");
            float w = 0; while (w < 2f && rp != null && !rp.IsPlaced) { w += Time.deltaTime; yield return null; }
            Park();
            if (_opShots) Shot(_cur + "_load_" + kind);
            Ck("loaded_into_" + kind, rp != null && rp.IsPlaced && host.Inside(kind, b.Col.bounds.center), $"{b.Spec.Kind}#{b.Spec.Id} inside {host.Inside(kind, b.Col.bounds.center)} at {host.transform.InverseTransformPoint(b.Col.bounds.center):F2}; placed {rp?.IsPlaced} after {w:0.0} s; oriented {rp?.Oriented}");
        }

        void FaceAppliance(WasherAppliance wa, float back = 0.62f, float up = 0.5f)
        {
            var c = wa.DrumCentre; _headPos = c + wa.Front * back + Vector3.up * up; _headRot = Quaternion.LookRotation(c + Vector3.up * 0.05f - _headPos, Vector3.up);
        }

        /// <summary>Grasp the door at its handle and swing it along the handle's real arc to <paramref name="target"/> (0 = closed: the hand
        /// lets go a few degrees short and the soft-close spring latches it). <paramref name="speed"/> = hand speed (m/s): fast = slam.</summary>
        IEnumerator SwingDoor(WasherAppliance wa, float target, float speed) { yield return Safe(SwingHinge(wa.Door, wa, target, speed)); }

        /// <summary>V12 generic: any hinged door / lid of a house host, swung by its handle along its real arc about its own axis.</summary>
        static bool BotNear(Vector3 p, float r) => FindObjectsByType<BotBody>(FindObjectsSortMode.None).Any(bb => bb != null && Flat(bb.transform.position - p).magnitude < r);

        /// <summary>Open a lid by its handle with the FREE hand only (no walking, no parking - the other hand keeps what it holds).</summary>
        IEnumerator LiftLidOneHand(HingedPart lid, MechHost host)
        {
            var h = Free(); if (h == null) yield break;
            h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(-host.Front, Vector3.up);
            var hp = lid.HandleWorld;
            yield return Safe(Move(h, hp + host.Front * 0.12f + Vector3.up * 0.03f, 0.8f)); yield return Safe(Move(h, hp, 0.3f));
            h.simPinch = 1; yield return new WaitForSeconds(0.15f);
            float a0 = lid.Angle, a1 = lid.OpenLimit; var pivot = lid.transform.position; var r0 = lid.HandleWorld - pivot; int n = Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 8f));
            for (int k = 1; k <= n; k++) { float a = Mathf.Lerp(a0, a1, k / (float)n); yield return Safe(Move(h, pivot + Quaternion.AngleAxis(a - a0, lid.AxisW) * r0, 0.45f)); }
            yield return new WaitForSeconds(0.15f); h.simPinch = 0; yield return new WaitForSeconds(0.1f);
            yield return Safe(Move(h, h.transform.position + host.Front * 0.15f + Vector3.up * 0.1f, 0.5f));
            _rep.events.Add($"{_cur} harness: lid lifted one-handed {a0:0} -> {lid.Angle:0} deg");
        }

        IEnumerator SwingHinge(HingedPart door, MechHost host, float target, float speed, Vector3? grabAt = null)
        {
            if (door == null) { Ck("door_part", false, "no hinged part"); yield break; }
            var wa = host;
            yield return Safe(WalkTo(host.Focus)); FaceHost(host);
            // reach for the handle once the part is at rest and nobody holds it (someone else may be using it right now)
            { float wr = 0f, la = door.Angle; yield return new WaitForSeconds(0.1f); while (wr < 15f && (door.IsHeld || Mathf.Abs(door.Angle - la) > 0.3f || BotNear(host.transform.position, 2.2f) && door.Opening > 0.05f)) { la = door.Angle; wr += 0.25f; yield return new WaitForSeconds(0.25f); }
              if (wr > 0f) _rep.events.Add($"{_cur} harness: waited {wr:0.00} s for {door.Label} to come to rest ({door.Angle:0} deg)"); }
            var h = Free() ?? _l; h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(-wa.Front, Vector3.up);
            var hp = grabAt ?? door.HandleWorld;   // V13: a test may grab the handle line away from its centre (a lid's rim corner)
            yield return Safe(Move(h, hp + wa.Front * 0.12f + Vector3.up * 0.03f, 0.8f)); yield return Safe(Move(h, hp, 0.3f));
            h.simPinch = 1; yield return new WaitForSeconds(0.15f);
            bool held = door.IsHeld; float a0 = door.Angle, a1 = target > 0f ? target * door.OpenLimit : door.OpenLimit * 0.04f;
            var pivot = door.transform.position; var r0 = hp - pivot; int n = Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 8f));
            if (speed > 1f && target <= 0f)
            {
                // flick: shove it shut and let go halfway - the door keeps its swing and hits the frame
                float dur = 0.32f, t = 0f;
                float aEnd = -Mathf.Sign(door.OpenLimit) * 12f;
                while (t < dur) { t += Time.deltaTime; float a = Mathf.Lerp(a0, aEnd, t / dur); h.transform.position = pivot + Quaternion.AngleAxis(a - a0, door.AxisW) * r0; if (t > dur * 0.45f) h.simPinch = 0; yield return null; }
                h.simPinch = 0; yield return new WaitForSeconds(0.6f);
            }
            else
            {
                for (int k = 1; k <= n; k++) { float a = Mathf.Lerp(a0, a1, k / (float)n); yield return Safe(Move(h, pivot + Quaternion.AngleAxis(a - a0, door.AxisW) * r0, speed)); }
                yield return new WaitForSeconds(0.15f);
                h.simPinch = 0; yield return new WaitForSeconds(0.1f);
            }
            yield return Safe(Move(h, h.transform.position + wa.Front * 0.15f, 0.5f));
            yield return new WaitForSeconds(target > 0f ? 0.5f : 1.2f);   // soft-close latch time
            Park();
            var st = _rt.Fixtures.FirstOrDefault(f => f != null && f.Host == host && host.DoorFor(f.Decl.Kind) == door)?.State;
            string nm = host is WasherAppliance ? "washer_door" : door.Label.Replace(' ', '_');
            Ck(nm + (target > 0f ? "_open_by_handle" : "_close_by_handle"), held && (target > 0f ? door.Opening > target * 0.6f && (st == null || st.Open) : door.IsClosed && (st == null || !st.Open)),
               $"grasped {held}; angle {a0:0} -> {door.Angle:0} deg (limit {door.OpenLimit:0}); hand {speed:0.00} m/s; slams {door.Slams}; locked {door.Locked}; rules {(st == null ? "unbound" : (st.Open ? "open " + st.Level : "closed"))}");
        }

        /// <summary>Push START with the index fingertip (the hand points into the panel; the cap travels, fires at 70 %).</summary>
        IEnumerator PressStart(WasherAppliance wa)
        {
            var btn = wa.StartButton; if (btn == null) { Ck("start_button", false, "no PART_StartButton"); yield break; }
            yield return Safe(WalkTo(wa.DrumCentre)); FaceAppliance(wa, 0.55f, 0.75f);
            var h = Free() ?? _l; h.simPinch = 0; var fwd = -btn.NormalWorld; h.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var face = btn.FaceWorld; int c0 = btn.Presses; bool before = wa.Running;
            yield return Safe(Move(h, face - fwd * 0.07f - fwd * 0.03f, 0.7f));
            yield return Safe(Move(h, face + fwd * 0.004f - fwd * 0.03f, 0.12f)); yield return new WaitForSeconds(0.25f);
            yield return Safe(Move(h, face - fwd * 0.08f - fwd * 0.03f, 0.3f)); yield return new WaitForSeconds(0.4f);
            Park();
            Ck("start_button_fingertip", btn.Presses > c0, $"presses {c0} -> {btn.Presses}; running {before} -> {wa.Running}; rejected starts {wa.RejectedStarts} ({wa.LastStartError})");
        }

        /// <summary>Carry a bundle to the open door and push it into the drum, release inside; the drum reports it loaded (rules).</summary>
        IEnumerator LoadDrum(PropBody b, WasherAppliance wa)
        {
            if (b == null) yield break;
            if (wa.Door != null && wa.Door.Opening < 0.5f) yield return Safe(SwingDoor(wa, 1f, 0.45f));
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            yield return Safe(WalkTo(wa.DrumCentre)); FaceAppliance(wa, 0.7f, 0.45f);
            var off = h.transform.position - b.Col.bounds.center; var c = wa.DrumCentre;
            string Lost(string at) { var hits = Physics.OverlapBox(b.Col.bounds.center, b.Col.bounds.extents + Vector3.one * 0.01f, b.transform.rotation, ~0, QueryTriggerInteraction.Ignore).Where(x => x != b.Col).Select(x => x.name).Take(6); return $"lost the {b.Spec.Kind} {at}: item at {wa.transform.InverseTransformPoint(b.Col.bounds.center):F2}, hand at {wa.transform.InverseTransformPoint(h.transform.position):F2}; touching {string.Join(", ", hits)}"; }
            // lift clear of open doors / machine tops before travelling sideways (a door open at 105 deg sticks out between the machines)
            yield return Safe(Move(h, new Vector3(h.transform.position.x, Mathf.Max(h.transform.position.y, c.y + 0.6f), h.transform.position.z), 0.4f));
            foreach (var (to, sp, label) in new[] { (c + wa.Front * 0.6f + Vector3.up * 0.5f, 0.4f, "above-front"), (c + wa.Front * 0.42f, 0.3f, "at the door"), (c + wa.Front * 0.05f + Vector3.up * 0.07f, 0.2f, "in the mouth"), (c - wa.Front * 0.13f + Vector3.up * 0.06f, 0.12f, "inside") })
            {
                yield return Safe(Move(h, to + off, sp));
                if (HandHolding(b) == null) { Ck("drum_load_carry", false, Lost(label)); break; }
            }
            if (_opShots) Shot(_cur + "_drum_load");
            h.simPinch = 0; yield return new WaitForSeconds(0.2f);
            yield return Safe(Move(h, c + wa.Front * 0.45f, 0.4f));
            float t = 0; var rp = _rt.RProp(b); while (t < 2f && rp != null && !rp.IsPlaced) { t += Time.deltaTime; yield return null; }
            Park();
            Ck("drum_loaded_by_hand", rp != null && rp.IsPlaced && wa.InDrum(b.Col.bounds.center), $"{b.Spec.Kind}#{b.Spec.Id} in drum {wa.InDrum(b.Col.bounds.center)} at {wa.transform.InverseTransformPoint(b.Col.bounds.center):F2}; rules placed {rp?.IsPlaced} after {t:0.0} s");
        }

        /// <summary>Pinch the dial and twist the hand about the dial axis to a detent (the knob clicks through each detent).</summary>
        IEnumerator TurnDial(WasherAppliance wa, int detent)
        {
            var dial = wa.Dial; if (dial == null) { Ck("dial", false, "no PART_Dial"); yield break; }
            FaceAppliance(wa, 0.55f, 0.75f);
            var h = Free() ?? _l; h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(-wa.Front, Vector3.up);
            yield return Safe(Move(h, dial.HandleWorld + wa.Front * 0.08f, 0.6f)); yield return Safe(Move(h, dial.HandleWorld, 0.2f));
            h.simPinch = 1; yield return new WaitForSeconds(0.15f);
            bool held = dial.IsHeld; int c0 = dial.Clicks;
            float step = dial.MaxAngle / Mathf.Max(1, dial.Detents - 1), want = detent * step - dial.Angle; var r0 = h.transform.rotation;
            int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(want) / 10f));
            for (int k = 1; k <= n; k++) { h.transform.rotation = Quaternion.AngleAxis(-want * k / n, wa.Front) * r0; yield return new WaitForSeconds(0.05f); }
            yield return new WaitForSeconds(0.2f); h.simPinch = 0; yield return new WaitForSeconds(0.4f);
            yield return Safe(Move(h, h.transform.position + wa.Front * 0.1f, 0.4f)); Park();
            Ck("dial_twisted_by_hand", held && dial.Detent == detent, $"grasped {held}; detent {dial.Detent} ({WasherAppliance.Programmes[Mathf.Clamp(dial.Detent, 0, WasherAppliance.Programmes.Length - 1)]}), angle {dial.Angle:0}, clicks +{dial.Clicks - c0}");
        }

        /// <summary>Grasp the drawer front and pull / push it along its rail to an opening fraction.</summary>
        IEnumerator SlideDrawer(WasherAppliance wa, float target, float speed = 0.25f) { yield return Safe(SlideRail(wa.Drawer, wa, target, speed)); }

        /// <summary>V12 generic: grasp any drawer front and pull / push it along its own rail.</summary>
        IEnumerator SlideRail(SlidingPart dr, MechHost host, float target, float speed = 0.25f)
        {
            if (dr == null) { Ck("drawer", false, "no sliding part"); yield break; }
            var wa = host; yield return Safe(WalkTo(host.Focus)); FaceHost(host, 0.6f, 0.45f);
            var h = Free() ?? _l; h.simPinch = 0; h.transform.rotation = Quaternion.LookRotation(-wa.Front, Vector3.up);
            yield return Safe(Move(h, dr.HandleWorld + wa.Front * 0.08f, 0.6f)); yield return Safe(Move(h, dr.HandleWorld, 0.2f));
            h.simPinch = 1; yield return new WaitForSeconds(0.15f);
            bool held = dr.IsHeld; float p0 = dr.Position, p1 = target * dr.Travel;
            yield return Safe(Move(h, h.transform.position + dr.AxisW * (p1 - p0 + (target <= 0f ? -0.004f : 0f)), speed));
            yield return new WaitForSeconds(0.2f); h.simPinch = 0; yield return new WaitForSeconds(0.5f);
            yield return Safe(Move(h, h.transform.position + wa.Front * 0.1f + Vector3.up * 0.05f, 0.4f)); Park();
            Ck((host is WasherAppliance ? "drawer" : dr.Label.Replace(' ', '_')) + (target > 0f ? "_pulled_by_hand" : "_pushed_by_hand"), held && (target > 0f ? dr.Opening > target * 0.7f : dr.IsClosed), $"grasped {held}; travel {p0 * 100f:0.0} -> {dr.Position * 100f:0.0} cm of {dr.Travel * 100f:0.0}; slams {dr.Slams}");
        }

        /// <summary>V12 pets + trash lab (desktop, simulated hands; Quest UNTESTED). Kitchen round: clutter placed, a hungry dog goes to its
        /// bowl and whines (host noise), the food bag is carried over and tipped into the bowl (finite kibble), the dog eats the REAL kibble
        /// (journaled meal + feeder bonus), a piece of trash goes past a lifted lid into a bin (journaled), the HUD shows it all.</summary>
        /// <summary>V13 `recipe_diag hints13` (owner: "for each interaction it should ghost or highlight"): a hand 25 cm from the bin lid
        /// shows a soft outline (not ready), 1 cm from the rim a yellow "lift"; my job's next step has an arrow + its words; standing near
        /// that target without doing it brings a ghost hand after ~2.5 s.</summary>
        IEnumerator HintsLab()
        {
            _cur = "HINTS13"; _rep.recipes.Add("HINTS13");
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(MatchRules.TenMinutes);
            if (!m.Start(MatchMode.Solo, 11, out var err)) { Ck("match", false, err); yield break; }
            m.SkipIntro(); m.Tick(0.01f); for (int i = 0; i < 40; i++) m.Tick(0.1f);
            _round = m.Round; GameServices.Match = m; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            RoomRuntime.NoSelfBody = true; RoomRuntime.QaPauseBrains = true;
            _room = new GameObject("V13_Hints").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, 11);
            RoomRuntime.NoSelfBody = false;
            foreach (var br in _room.Brains) { var bb = _room.BodyFor(br.Slot); if (bb != null) bb.Stop(); }
            yield return new WaitForSeconds(1.5f); _rig.LocomotionEnabled = true;
            _l.simPinch = 0; _r.simPinch = 0; _l.transform.position = new Vector3(0, -5, 0);
            var bin = MechFurniture.All.Where(f => f != null && f.Kind == "bin" && f.Lid != null).OrderBy(f => f.transform.position.sqrMagnitude).FirstOrDefault();
            if (bin == null) { Ck("bin", false, "no bin"); yield break; }
            var rim = bin.Lid.HandleWorld; var fr = bin.Front;
            _headPos = rim + fr * 0.55f + Vector3.up * 0.45f; _headRot = Quaternion.LookRotation(rim - _headPos);
            _r.transform.SetPositionAndRotation(rim + fr * 0.25f, Quaternion.LookRotation(-fr)); yield return new WaitForSeconds(0.3f);
            int farShown = InteractionHints.Shown, farReady = InteractionHints.ReadyShown; yield return Late(() => Shot("hints13_near_lid"));
            _r.transform.position = rim + fr * 0.01f + Vector3.up * 0.01f; yield return new WaitForSeconds(0.3f);
            int nearReady = InteractionHints.ReadyShown; var lab = FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled && t.transform.parent != null && t.transform.parent.name.StartsWith("interactHint")).Select(t => t.text).FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? "";
            yield return Late(() => Shot("hints13_ready_lid"));
            Ck("highlight_near_then_ready", farShown > 0 && farReady == 0 && nearReady > 0 && lab == "lift", $"hand 25 cm from the lid: outlines {farShown}, ready {farReady}; hand on the rim: ready {nearReady}, word '{lab}'");
            // next step of my job: an arrow with its words
            _r.transform.position = new Vector3(0, -5, 0); yield return new WaitForSeconds(0.3f);
            string next = InteractionHints.NextLabel; var at = InteractionHints.NextAt;
            Ck("next_step_arrow", !string.IsNullOrEmpty(next), $"next step '{next}' at {at:F2}");
            // stand near that target without doing it: the ghost hand shows how
            if (!string.IsNullOrEmpty(next))
            {
                var away = Vector3.ProjectOnPlane(_headPos - at, Vector3.up); away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
                _headPos = at + away * 0.7f + Vector3.up * 0.35f; _headRot = Quaternion.LookRotation(at - _headPos);
                float w = 0f; while (w < 5f && !InteractionHints.GhostOn) { w += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.5f); yield return Late(() => Shot("hints13_ghost"));
                Ck("ghost_hand_when_hesitating", InteractionHints.GhostOn && w > 2f, $"ghost shown after {w:0.0} s standing 0.7 m from '{next}'; {InteractionHints.GhostWhy}");
            }
            RoomRuntime.QaPauseBrains = false;
        }

        IEnumerator PetLab()
        {
            _cur = "PETS"; _rep.recipes.Add("PETS");
            _round = MakeRound(RoomId.Kitchen, "KT01", out var seed); if (_round == null) { Ck("seed", false, "no seed"); yield break; }
            _round.Begin();
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = true;
            _room = new GameObject("V12_Pets").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, seed);   // room loop ON: pet + bot brains really run
            _rt = _room.GetComponent<RecipeRuntime>();
            float w0 = 0; while (!_round.IsPlayable && w0 < 5) { w0 += Time.deltaTime; yield return null; }
            var props = _round.Room.Props; int trashN = props.Count(p => HouseClutter.IsTrash(p.Kind));
            var foodSpec = props.FirstOrDefault(p => p.Kind == "pet_food"); var dogBowlSpec = props.FirstOrDefault(p => p.Kind == "pet_bowl_dog");
            Ck("clutter_placed", trashN >= 4 && foodSpec != null && dogBowlSpec != null, $"trash {trashN} ({string.Join(", ", props.Where(p => HouseClutter.IsTrash(p.Kind)).Select(p => p.Kind.Substring(6) + "@" + p.Spawn.x.ToString("0.0") + "," + p.Spawn.z.ToString("0.0")))}); food bag at {foodSpec?.Spawn.x:0.0},{foodSpec?.Spawn.z:0.0}; dog bowl at {dogBowlSpec?.Spawn.x:0.0},{dogBowlSpec?.Spawn.z:0.0}; bins {MechFurniture.All.Count(f => f != null && f.Kind == "bin")}; meals per match dog {_round.PetMealsTarget[1]} cat {_round.PetMealsTarget[0]}");
            var dogCare = PetCare.All.FirstOrDefault(p => p != null && p.Kind == PetKind.Dog); var bowl = _room.Prop(dogBowlSpec.Id); var bag = _room.Prop(foodSpec.Id);
            if (dogCare == null || bowl == null || bag == null) { Ck("pet_parts", false, $"care {dogCare != null} bowl {bowl != null} bag {bag != null}"); yield break; }
            // harness setup (stated): the dog has been waiting - jump its hunger so the test does not wait minutes of match clock
            // start the dog well away from its bowl, so walking there is the brain's doing (not where it happened to spawn)
            { var ag = dogCare.Body.GetComponent<UnityEngine.AI.NavMeshAgent>(); var far = bowl.transform.position + (_room.transform.position - bowl.transform.position).normalized * 2.5f;
              if (ag != null && UnityEngine.AI.NavMesh.SamplePosition(far, out var nh, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) ag.Warp(nh.position); }
            float startDist = Vector3.Distance(Flat(dogCare.Body.transform.position), Flat(bowl.transform.position));
            _round.PetHunger[1] = 78f; int whines0 = dogCare.Whines;
            float t = 0; while (t < 25f && (dogCare.Whines == whines0 || Vector3.Distance(Flat(dogCare.Body.transform.position), Flat(bowl.transform.position)) > 0.9f)) { t += Time.deltaTime; yield return null; }
            _headPos = bowl.transform.position + new Vector3(0.9f, 1.0f, 0.4f); _headRot = Quaternion.LookRotation(bowl.transform.position - _headPos); yield return new WaitForSeconds(0.3f); Shot("pets_dog_hungry_at_bowl");
            var noise = _round.RecentNoise.Where(e => e.Cause == NoiseCause.PetAction && e.DirectActor == ActorIds.Dog).ToList();
            Ck("hungry_dog_walks_to_bowl_and_whines", startDist > 1.2f && Vector3.Distance(Flat(dogCare.Body.transform.position), Flat(bowl.transform.position)) < 0.9f && dogCare.Whines > whines0 && noise.Count > 0, $"dog started {startDist:0.0} m from the bowl, now {Vector3.Distance(Flat(dogCare.Body.transform.position), Flat(bowl.transform.position)):0.00} m after {t:0.0} s; whines {dogCare.Whines - whines0}; noise events {noise.Count} (max {(noise.Count > 0 ? noise.Max(e => e.Value) : 0):0.0}); meter {_round.Meter.Value:0.0}");
            // bot parity: an AI sibling notices the hungry dog, fetches the bag, tips it over the bowl, the dog eats
            {
                int dm0 = _round.PetMeals[1]; float bb0 = 0; for (int sb = 1; sb < 4; sb++) bb0 += _round.Scores[sb].Care; float bt = 0; string who = "";
                while (bt < 75f && _round.PetMeals[1] == dm0) { bt += Time.deltaTime; if (_round.FeedingBot >= 0) who = "S" + (_round.FeedingBot + 1); if (_round.Interro != null && !_round.Interro.Done.Contains(0)) _round.PassInterrogation(0); yield return null; }
                float bb1 = 0; for (int sb = 1; sb < 4; sb++) bb1 += _round.Scores[sb].Care;
                _headPos = bowl.transform.position + new Vector3(0.8f, 0.9f, 0.5f); _headRot = Quaternion.LookRotation(bowl.transform.position - _headPos); yield return new WaitForSeconds(0.3f); Shot("pets_bot_fed_dog");
                Ck("bot_feeds_hungry_pet", _round.PetMeals[1] > dm0 && bb1 > bb0, $"feeder {who}; dog meals {dm0} -> {_round.PetMeals[1]} after {bt:0} s; dog ate {dogCare.EatenGrams:0} g; bot care bonus +{bb1 - bb0:0.0}; bots: {string.Join(" | ", _room.Brains.Select(b => "S" + (b.Slot + 1) + " " + b.DebugGoal))}; last bot pour: {string.Join(" / ", _room.GetComponentsInChildren<BotBody>().Where(x => x.LastPour.Length > 0).Select(x => x.LastPour))}; kibble on the floor {Puddle.All.Where(x => x.Granular).Sum(x => x.Litres) * 1000f:0} g in {Puddle.All.Count(x => x.Granular)} heaps at {string.Join(" ", Puddle.All.Where(x => x.Granular).Take(6).Select(x => (x.Litres * 1000f).ToString("0") + "g@" + x.transform.position.ToString("F1")))}; bag now {Vessels.Of(bag).Amount * 1000f:0} g; dog bowl liquid: holder {Vessels.Of(bowl).transform.position:F3} floorY {Vessels.Of(bowl).FloorY:0.000} rimY {Vessels.Of(bowl).RimY:0.000} r {Vessels.Of(bowl).InnerRadius:0.000} catchR {Vessels.Of(bowl).CatchRadius:0.000} lossy {Vessels.Of(bowl).transform.lossyScale.x:0.00}; collider {bowl.Col.bounds.min:F3}..{bowl.Col.bounds.max:F3}; sample hit local {Vessels.Of(bowl).transform.InverseTransformPoint(new Vector3(-2.227f, 0.048f, -2.359f) + (_room.transform.position)):F3}");
                for (float w2 = 0; w2 < 10f && bag.IsHeld; w2 += Time.deltaTime) yield return null;   // the bot puts the bag down
                { var bl = Vessels.Of(bag); yield return new WaitForSeconds(1.5f);
                  _rep.events.Add($"{_cur} harness: after the bot: bag held {bag.IsHeld} at {_room.transform.InverseTransformPoint(bag.transform.position):F2} tilt {Vector3.Angle(bag.transform.up, Vector3.up):0} kibble {(bl != null ? bl.Amount * 1000f : -1f):0} g; on the floor {Puddle.All.Sum(x => x.Litres) * 1000f:0} g; bots: {string.Join(" | ", _room.Brains.Select(b => "S" + (b.Slot + 1) + " " + b.DebugGoal))}"); }
            }
            // the player feeds the CAT (the job is claimed so the bots leave the bag alone)
            var catCare0 = PetCare.All.FirstOrDefault(p => p != null && p.Kind == PetKind.Cat); dogCare = catCare0; bowl = _room.Prop(props.First(p => p.Kind == "pet_bowl_cat").Id);
            _round.PetHunger[0] = 80f; _round.FeedingBot = 0;
            var blc = Vessels.Of(bag); var bowlLc = Vessels.Of(bowl);
            yield return Safe(WalkTo(bag.Col.bounds.center)); yield return Safe(GrabSide(bag)); var h = HandHolding(bag);
            if (h == null || blc == null || bowlLc == null) { Ck("feed_carry", false, $"held {h != null} bag liquid {blc != null} bowl liquid {bowlLc != null}"); yield break; }
            // carry it UPRIGHT like a person (an open bag held tipped pours out on the way)
            for (int k = 0; k < 3 && Vector3.Angle(bag.transform.up, Vector3.up) > 8f; k++)
            {
                var u0 = h.transform.rotation; var u1 = Quaternion.FromToRotation(bag.transform.up, Vector3.up) * u0;
                for (float u = 0; u < 1f; u += Time.deltaTime / 0.25f) { h.transform.rotation = Quaternion.Slerp(u0, u1, u); yield return null; }
                h.transform.rotation = u1; yield return new WaitForSeconds(0.1f);
            }
            float bag0 = blc.Amount, bowl0 = bowlLc.Amount, eaten0 = dogCare.EatenGrams, pud0 = Puddle.All.Sum(x => x.Litres);
            string stages = $"picked up {blc.Amount * 1000f:0} g tilt {Vector3.Angle(bag.transform.up, Vector3.up):0}"; int meals0 = _round.PetMeals[0]; float score0 = _round.Scores[0].Care;
            yield return Safe(WalkTo(bowl.Col.bounds.center));
            stages += $"; after walk {blc.Amount * 1000f:0} g tilt {Vector3.Angle(bag.transform.up, Vector3.up):0}";
            var bc = bowlLc.transform.TransformPoint(new Vector3(0, bowlLc.RimY, 0));
            yield return Safe(Move(h, h.transform.position + (bc + Vector3.up * 0.3f - bag.Col.bounds.center), 0.4f));
            stages += $"; over bowl {blc.Amount * 1000f:0} g tilt {Vector3.Angle(bag.transform.up, Vector3.up):0}";
            var toMe = _headPos - bc; toMe.y = 0; var axis = Vector3.Cross(Vector3.up, toMe.normalized); var r0 = h.transform.rotation;
            // a careful pour: spout over the bowl, tip until it flows, then a little more; stop with a meal in the bowl
            { float a = 0f, pt0 = blc.PouredOut, rc0 = bowlLc.Received, tt = 0f; bool shot = false;
              while (tt < 12f && blc.Amount > 0.002f && bowlLc.Received - rc0 < 0.1f)
              {
                  tt += Time.deltaTime;
                  var lip = blc.transform.TransformPoint(new Vector3(0, blc.RimY, 0)); var dl = bc - lip; dl.y = 0f;
                  h.transform.position += Vector3.ClampMagnitude(dl, 0.01f);
                  if (dl.magnitude < 0.04f) a = Mathf.Min(blc.Pouring ? 95f : 120f, a + (blc.Pouring ? 12f : 60f) * Time.deltaTime);
                  h.transform.rotation = Quaternion.AngleAxis(a, axis) * r0;
                  if (!shot && blc.Pouring) { shot = true; Shot("pets_kibble_pour"); }
                  float pouredNow = blc.PouredOut - pt0, gotNow = bowlLc.Received - rc0; if (pouredNow > 0.03f && gotNow < pouredNow * 0.3f) break;   // missing: stop
                  yield return null;
              }
              float wt = 0; while (wt < 1.5f && blc.Pouring) { wt += Time.deltaTime; h.transform.rotation = Quaternion.Slerp(h.transform.rotation, r0, 0.2f); yield return null; }
            }
            stages += $"; after pour {blc.Amount * 1000f:0} g; stream {blc.StreamLanding}";
            h.transform.rotation = r0; yield return new WaitForSeconds(0.4f);
            float poured = bag0 - blc.Amount, got = bowlLc.Amount - bowl0 + (dogCare.EatenGrams - eaten0) / 1000f, spilled = Puddle.All.Sum(x => x.Litres) - pud0;
            Ck("kibble_poured_into_bowl_by_hand", got > 0.03f, $"{stages}; bag {bag0 * 1000f:0} -> {blc.Amount * 1000f:0} g; bowl received {got * 1000f:0} g; spilled {spilled * 1000f:0} g; conserved within {(poured - got - spilled) * 1000f:0.0} g; last feeder S{bowlLc.LastFeeder + 1}; stream {blc.StreamLanding}; caught by {blc.LastReceiver}; lost {Puddle.Lost * 1000f:0} g; receivers: {string.Join(", ", LiquidContainer.All.Where(x => x.Received > 0.001f).Select(x => (x.GetComponentInParent<PropBody>() != null ? x.GetComponentInParent<PropBody>().Spec.Kind : x.name) + " " + (x.Received * 1000f).ToString("0")))}");
            yield return Safe(PutOnTable(bag)); Park();
            t = 0; while (t < 30f && _round.PetMeals[0] == meals0) { t += Time.deltaTime; yield return null; }
            _round.FeedingBot = -1;
            _headPos = bowl.transform.position + new Vector3(0.8f, 0.9f, 0.5f); _headRot = Quaternion.LookRotation(bowl.transform.position - _headPos); yield return new WaitForSeconds(0.3f); Shot("pets_cat_eating");
            Ck("player_fed_cat_real_food", _round.PetMeals[0] > meals0 && _round.PetHunger[0] < 35f && _round.Scores[0].Care > score0, $"cat ate {dogCare.EatenGrams - eaten0:0} g in {t:0.0} s; hunger {_round.PetHunger[0]:0}; meals {meals0} -> {_round.PetMeals[0]}; feeder bonus +{_round.Scores[0].Care - score0:0.0} (care points, separate from noise penalties); bowl left {bowlLc.Amount * 1000f:0} g");
            // trash: nearest piece into the nearest bin (lid lifted by hand)
            var trash = _rt != null ? null : (PropBody)null;
            foreach (var p in props.Where(p => HouseClutter.IsTrash(p.Kind) && !_round.Binned.Contains(p.Id) && _round.HolderOf(p.Id) < 0 && !_round.TrashClaimedByOther(p.Id, 0))) { var b = _room.Prop(p.Id); if (b != null && (trash == null || Vector3.Distance(b.transform.position, _headPos) < Vector3.Distance(trash.transform.position, _headPos))) trash = b; }
            // like a person: use a bin nobody (bot) is standing at, and reach for its lid only once it is at rest and not held
            bool BotAt(MechFurniture f) => FindObjectsByType<BotBody>(FindObjectsSortMode.None).Any(bb => bb != null && Flat(bb.transform.position - f.transform.position).magnitude < 1.6f);
            // a free bin close to me, then the loose trash closest to THAT bin (short carry, like a person tidying one corner at a time)
            var bin = MechFurniture.All.Where(f => f != null && f.Kind == "bin").OrderBy(f => Vector3.Distance(f.transform.position, _headPos)).FirstOrDefault();   // claimed below: bots use another bin meanwhile
            if (bin != null)
            {
                trash = null;
                foreach (var p in props.Where(p => HouseClutter.IsTrash(p.Kind) && !_round.Binned.Contains(p.Id) && _round.HolderOf(p.Id) < 0 && !_round.TrashClaimedByOther(p.Id, 0)))
                { var b = _room.Prop(p.Id); if (b != null && !TrashSystem.InAnyBin(b.Col.bounds.center) && (trash == null || Vector3.Distance(b.transform.position, bin.transform.position) < Vector3.Distance(trash.transform.position, bin.transform.position))) trash = b; }
            }
            if (trash == null || bin == null) Ck("trash_parts", false, $"trash {trash != null} bin {bin != null}");
            else
            {
                _round.ClaimTrash(0, trash.Spec.Id);   // "I'm getting that one" - bots leave it to me
                bin.ClaimBinFor(0);                    // ... and I'm using this bin
                if (bin.Lid != null)
                {
                    float wl = 0f, la = bin.Lid.Angle;
                    while (wl < 6f && (bin.Lid.IsHeld || BotAt(bin) || Mathf.Abs(bin.Lid.Angle - la) > 0.5f)) { la = bin.Lid.Angle; wl += 0.25f; yield return new WaitForSeconds(0.25f); }
                    _rep.events.Add($"{_cur} harness: bin {bin.name} lid at rest {bin.Lid.Angle:0} deg after {wl:0.0} s (bot there {BotAt(bin)})");
                }
                // V13 (owner, Quest c21: "opening the bin using grab is not working"): grab the lid near the CORNER of its front rim (75 % of
                // the way to the side, 2 cm off the edge) - where a real hand lands - not the exact centre point
                if (bin.Lid != null && bin.Lid.Opening < 0.6f)
                {
                    var corner = bin.Lid.HandleWorld + bin.Lid.transform.TransformVector(bin.Lid.HandleSpanLocal) * 0.75f + Vector3.up * 0.02f;
                    _rep.events.Add($"{_cur} bin lid grabbed {Vector3.Distance(corner, bin.Lid.HandleWorld) * 100f:0} cm from the old single handle point");
                    yield return Safe(SwingHinge(bin.Lid, bin, 1f, 0.45f, corner));
                }
                yield return Safe(WalkTo(trash.Col.bounds.center));   // go to the trash before reaching for it
                _round.ClaimTrash(0, trash.Spec.Id); bin.ClaimBinFor(0); yield return Safe(Grab(trash)); var th = HandHolding(trash);
                if (th != null)
                {
                    string Where() => $"{_room.transform.InverseTransformPoint(trash.transform.position):F2} held {(Int(th).Held == trash)} holder S{_round.HolderOf(trash.Spec.Id) + 1}";
                    _rep.events.Add($"{_cur} harness: trash grabbed at {Where()}");
                    yield return Safe(WalkTo(bin.Focus)); FaceHost(bin, 0.6f, 0.45f);
                    _rep.events.Add($"{_cur} harness: at the bin, trash {Where()}");
                    // someone closed it while I fetched the trash: lift the lid again with the free hand (the trash stays in the other)
                    if (bin.Lid != null && bin.Lid.Opening < 0.6f) { _rep.events.Add($"{_cur} harness: lid closed again ({bin.Lid.Angle:0} deg) - lifting it with the free hand"); yield return Safe(LiftLidOneHand(bin.Lid, bin)); }
                    var pt = bin.SlotPoint("lint_bin", 0); var off = th.transform.position - trash.Col.bounds.center; float overY = Mathf.Max(pt.y, bin.Focus.y) + 0.2f + trash.Col.bounds.extents.y;
                    yield return Safe(Move(th, new Vector3(th.transform.position.x, Mathf.Max(th.transform.position.y, overY + off.y), th.transform.position.z), 0.4f));
                    // come in from the FRONT of the opening (a bin in a niche: a sideways sweep hits the furniture beside it)
                    yield return Safe(Move(th, new Vector3(pt.x, overY, pt.z) - bin.Front * 0.3f + off, 0.4f));
                    yield return Safe(Move(th, new Vector3(pt.x, overY, pt.z) + off, 0.4f));
                    _rep.events.Add($"{_cur} harness: over the bin, trash {Where()}");
                    th.simPinch = 0; yield return new WaitForSeconds(1.2f); Park();
                    // counted once it rests 0.3 s in the bin while play runs (not during Mom's wake-up / confrontation)
                    { float wb = 0f; while (wb < 6f && !_round.Binned.Contains(trash.Spec.Id)) { wb += Time.deltaTime; yield return null; } }
                }
                Shot("pets_trash_binned");
                Ck("trash_binned_by_hand", _round.Binned.Contains(trash.Spec.Id), $"{trash.Spec.Kind}#{trash.Spec.Id} in bin {TrashSystem.InAnyBin(trash.Col.bounds.center)} at {_room.transform.InverseTransformPoint(trash.transform.position):F2} (bin {_room.transform.InverseTransformPoint(bin.transform.position):F2}); binned {_round.Binned.Count}; left {(TrashSystem.I != null ? TrashSystem.I.Left : -1)}");
            }
            // HUD capture (head-locked strip: my step, dog / cat hunger, trash left)
            _headPos = _room.transform.TransformPoint(new Vector3(0.6f, 1.2f, -0.3f)); _headRot = Quaternion.LookRotation(_room.transform.forward); yield return new WaitForSeconds(0.4f); Shot("pets_hud");
            var hud = _rig.Head.GetComponentInChildren<TaskHud>(true); Ck("task_hud_present", hud != null, hud != null ? "head-locked HUD built" : "no HUD");
        }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        /// <summary>Grip a bulky item (bottle, bag) by its side at mid height, the way a person picks it up (a bag lying down too).</summary>
        IEnumerator GrabSide(PropBody b)
        {
            var hb = Free() ?? _r; hb.simPinch = 0; yield return new WaitForSeconds(0.1f);
            var toMe = _headPos - b.Col.bounds.center; toMe.y = 0; if (toMe.sqrMagnitude < 1e-4f) toMe = Vector3.back; toMe.Normalize();
            var side = b.Col.ClosestPoint(b.Col.bounds.center + toMe * 0.3f); side = Vector3.Lerp(side, b.Col.bounds.center, 0.15f);
            hb.transform.rotation = Quaternion.LookRotation(-toMe, Vector3.up);
            yield return Safe(Move(hb, side + toMe * 0.1f, 0.6f)); yield return Safe(Move(hb, side, 0.25f));
            hb.simPinch = 1; yield return new WaitForSeconds(0.3f);
            if (Int(hb).Held == b) yield return Safe(Move(hb, hb.transform.position + Vector3.up * 0.15f, 0.4f));
            else
            {
                // took something lying next to it: put that back down where it was and grip the intended item from above its middle
                if (Int(hb).Held != null) { var wrong = Int(hb).Held; hb.simPinch = 0; yield return new WaitForSeconds(0.4f); _rep.events.Add($"{_cur} harness: side grip took {wrong.Spec.Kind} - let go"); }
                hb.simPinch = 0; yield return Safe(Move(hb, b.Col.bounds.center + Vector3.up * 0.2f, 0.5f)); yield return Safe(Move(hb, b.Col.bounds.center, 0.25f));
                hb.simPinch = 1; yield return new WaitForSeconds(0.3f);
                if (Int(hb).Held == b) yield return Safe(Move(hb, hb.transform.position + Vector3.up * 0.15f, 0.4f)); else { hb.simPinch = 0; yield return Safe(Grab(b)); }
            }
        }

        /// <summary>V12 furniture lab (desktop, simulated hands): each chest drawer pulled / pushed on its rail, a shoved drawer slams,
        /// the bin lid lifted and dropped, and the house mechanism state the round journals for other players.</summary>
        /// <summary>Render my view with and without my own body: (share of the view my body covers, share of my glove pixels still visible).</summary>
        int TipsCovered;
        (float cover, float gloves) OwnBodyCover(string name)
        {
            var mine = _room != null && _room.SelfBody != null ? _room.SelfBody.GetComponentsInChildren<Renderer>() : new Renderer[0];
            var glove = FindObjectsByType<SimpleHandVisual>(FindObjectsSortMode.None).SelectMany(v => v.GetComponentsInChildren<Renderer>()).ToArray();
            Color32[] Grab(bool body, bool hands)
            {
                foreach (var r in mine) r.enabled = body; foreach (var r in glove) r.enabled = hands;
                const int W = 320, H = 180; var rt = new RenderTexture(W, H, 24); var prev = _cam.targetTexture; var act = RenderTexture.active;
                _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
                var t = new Texture2D(W, H, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply(); var px = t.GetPixels32();
                _cam.targetTexture = prev; RenderTexture.active = act; Destroy(t); rt.Release(); Destroy(rt); return px;
            }
            bool Diff(Color32 a, Color32 b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 24;
            bool TipCovered(Vector3 tip, Color32[] withBody, Color32[] withoutBody)
            {
                // can I see what I am pinching? the 9x9 px around each pinch point must not be covered by my own body
                var sp = _cam.WorldToViewportPoint(tip); if (sp.z <= 0f) return false;
                int cx = Mathf.RoundToInt(sp.x * 320), cy = Mathf.RoundToInt(sp.y * 180), cov = 0, n = 0;
                for (int dy = -4; dy <= 4; dy++) for (int dx = -4; dx <= 4; dx++) { int x = cx + dx, y = cy + dy; if (x < 0 || y < 0 || x >= 320 || y >= 180) continue; n++; if (Diff(withBody[y * 320 + x], withoutBody[y * 320 + x])) cov++; }
                return n > 0 && cov > n / 4;
            }
            var mineOn = mine.Select(r => r.enabled).ToArray(); var gloveOn = glove.Select(r => r.enabled).ToArray();
            var all = Grab(true, gloveOn.Any(x => x)); var noBody = Grab(false, gloveOn.Any(x => x)); var none = Grab(false, false);
            for (int i = 0; i < mine.Length; i++) mine[i].enabled = mineOn[i]; for (int i = 0; i < glove.Length; i++) glove[i].enabled = gloveOn[i];
            int bodyPx = 0, glovePx = 0, gloveSeen = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (Diff(all[i], noBody[i])) bodyPx++;
                if (Diff(noBody[i], none[i])) { glovePx++; if (!Diff(all[i], noBody[i])) gloveSeen++; }
            }
            Shot(name);
            TipsCovered = (TipCovered(_l.PinchPoint, all, noBody) ? 1 : 0) + (TipCovered(_r.PinchPoint, all, noBody) ? 1 : 0);
            return (bodyPx / (float)all.Length, glovePx > 0 ? gloveSeen / (float)glovePx : 0f);
        }

        IEnumerator Late(Action a) { QaLateShooter.Ensure(); QaLateShooter.Pending = a; float w = 0; while (QaLateShooter.Pending != null && w < 2f) { w += Time.unscaledDeltaTime; yield return null; } }

        /// <summary>A capture from any viewpoint (third person), without moving the player's head.</summary>
        void ShotFrom(string name, Vector3 eye, Vector3 at, bool hideMe = true)
        {
            var go = new GameObject("QA_ThirdPerson"); var c = go.AddComponent<Camera>(); c.CopyFrom(_cam); c.enabled = false;
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at - eye));
            // third person: my own body is not in this picture (it stands at my eyes, which may be right at this camera)
            var mine = hideMe && _room != null && _room.SelfBody != null ? _room.SelfBody.GetComponentsInChildren<Renderer>() : new Renderer[0];
            foreach (var r in mine) r.enabled = false;
            var keep = _cam; _cam = c; Shot(name); _cam = keep; Destroy(go);
            foreach (var r in mine) if (r != null) r.enabled = true;
        }

        /// <summary>V12 avatar lab (desktop, simulated head + hands): my own tracked Genies body under the headset, and a friend's body
        /// driven by scripted head / hand poses exactly as the replicated poses drive it online - stand, reach, crouch to the floor,
        /// turn, walk. Measures how well the body follows (head, hands, feet) and captures each pose.</summary>
        IEnumerator AvatarLab()
        {
            _cur = "AVATARS"; _rep.recipes.Add("AVATARS");
            _round = MakeRound(RoomId.Kitchen, "KT01", out var seed); if (_round == null) { Ck("seed", false, "no seed"); yield break; }
            _round.Begin();
            GameServices.Match = null; GameServices.PracticeRound = _round; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = true;
            _room = new GameObject("V12_Avatars").AddComponent<RoomRuntime>(); _room.LocalHead = _rig.Head; _room.Build(_round.Room); _room.Bind(_round, seed);
            float floor = _room.transform.position.y; var c0 = _room.transform.position;
            // me: standing 1.55 m eyes, hands in front of the chest
            Vector3 me = c0 + new Vector3(0.6f, 0f, -0.4f); _headPos = me + Vector3.up * 1.55f; _headRot = Quaternion.LookRotation(Vector3.forward);
            _l.transform.SetPositionAndRotation(_headPos + new Vector3(-0.18f, -0.42f, 0.32f), Quaternion.LookRotation(Vector3.forward)); _l.simPinch = 0;
            _r.transform.SetPositionAndRotation(_headPos + new Vector3(0.18f, -0.42f, 0.32f), Quaternion.LookRotation(Vector3.forward)); _r.simPinch = 0;
            float w = 0; while (w < 90f && (_room.SelfBody == null || _room.SelfBody.Body == null || !_room.SelfBody.Body.Calibrated)) { w += Time.deltaTime; yield return null; }
            var self = _room.SelfBody != null ? _room.SelfBody.Body : null;
            Ck("self_body_loaded", self != null && self.Calibrated, self == null ? $"no tracked self body after {w:0} s (Genies backend {(AvatarService.Backend != null)})" : $"my Genies body tracked after {w:0.0} s; scale {self.Scale:0.00} for eyes at {self.EyeHeight:0.00} m");
            if (self != null)
            {
                yield return new WaitForSeconds(1.0f);
                var mv = self.Mover.position;
                Ck("self_body_under_me", Flat(mv - _headPos).magnitude < 0.15f && Mathf.Abs(self.FootLift) < 0.08f && self.HandErrorL < 0.06f && self.HandErrorR < 0.06f,
                   $"body {Flat(mv - _headPos).magnitude * 100f:0} cm from under my eyes; feet {self.FootLift * 100f:0} cm off the floor; wrists {self.HandErrorL * 100f:0.0} / {self.HandErrorR * 100f:0.0} cm from my gloves' wrists (arm stretch {self.StretchL:0.00} / {self.StretchR:0.00})");
                // look down at myself (first person) + see me from the side
                _headRot = Quaternion.LookRotation(Vector3.forward * 0.35f + Vector3.down); yield return new WaitForSeconds(0.6f); yield return Late(() => Shot("avatar_self_look_down"));
                _headRot = Quaternion.LookRotation(Vector3.forward); yield return new WaitForSeconds(0.4f);
                yield return Late(() => ShotFrom("avatar_self_side", _headPos + new Vector3(1.6f, -0.35f, 0.9f), mv + Vector3.up * 0.9f, false));
                // Quest run 2026-11-11: reaching for something on a table, my own sleeve hid my hand and the object. Measure it: the share of
                // my view my own body covers, and how much of my gloves stays visible (rendered with / without my body)
                var eyes0 = _headPos;
                foreach (var (nm, hl, hr, pitch) in new[] { ("reach_table", new Vector3(-0.13f, -0.48f, 0.42f), new Vector3(0.13f, -0.48f, 0.42f), 40f),
                                                            ("reach_far", new Vector3(-0.15f, -0.3f, 0.58f), new Vector3(0.15f, -0.3f, 0.58f), 20f),
                                                            ("watch", new Vector3(-0.05f, -0.28f, 0.3f), new Vector3(0.2f, -0.45f, 0.25f), 35f) })
                {
                    _headPos = eyes0; _headRot = Quaternion.Euler(pitch, 0f, 0f);
                    _l.transform.SetPositionAndRotation(eyes0 + hl, Quaternion.LookRotation(Vector3.forward + Vector3.down * 0.3f)); _r.transform.SetPositionAndRotation(eyes0 + hr, Quaternion.LookRotation(Vector3.forward + Vector3.down * 0.3f));
                    yield return new WaitForSeconds(0.8f);
                    float cover0 = 0f, gloves0 = 0f, cover = 0f, gloves = 0f; string shot = "avatar_self_" + nm;
                    // as on the Quest (full arms), then with my forearms only
                    int tips0 = 0, tips = 0;
                    self.ArmTrim?.Set(false); yield return null;
                    yield return Late(() => { (cover0, gloves0) = OwnBodyCover(shot + "_fullarms"); tips0 = TipsCovered; });
                    self.ArmTrim?.Set(true); yield return null;
                    yield return Late(() => { (cover, gloves) = OwnBodyCover(shot); tips = TipsCovered; });
                    Ck("own_body_view_" + nm, cover < 0.12f, $"my own body covers {cover * 100f:0.0} % of my view (full arms: {cover0 * 100f:0.0} %); pinch points hidden by my body: {tips}/2 (full arms: {tips0}/2); glove pixels visible {gloves * 100f:0} % (full arms {gloves0 * 100f:0} %, the rest is the cuff over the wrist) - target < 5 % and both pinch points visible; {self.ArmTrim?.Report}");
                }
                _headPos = eyes0; _headRot = Quaternion.LookRotation(Vector3.forward);
                // V12 owner 2026-11-12: complete outfit, my real Genies hands (fingers from tracking), gloves hidden, UI over my body
                Ck("own_outfit_complete", self.ArmTrim != null && self.ArmTrim.Report.Contains(" 0 upper-arm triangles removed"), "my arms: " + (self.ArmTrim != null ? self.ArmTrim.Report : "no trim"));
                var gl = FindObjectsByType<SimpleHandVisual>(FindObjectsSortMode.None);
                var hR = self.A.GetBoneTransform(HumanBodyBones.RightHand); var hL = self.A.GetBoneTransform(HumanBodyBones.LeftHand);
                bool handsShown = hR != null && hL != null && hR.lossyScale.x > 0.2f && hL.lossyScale.x > 0.2f;
                bool glovesHidden = gl.Length > 0 && gl.SelectMany(v => v.GetComponentsInChildren<Renderer>(true)).All(r => !r.enabled);
                Ck("own_genies_hands_shown", handsShown && glovesHidden, $"Genies hands {(handsShown ? "shown" : "HIDDEN")} (scale {(hR != null ? hR.lossyScale.x : 0f):0.00}); gloves {(glovesHidden ? "hidden" : "STILL SHOWN")}");
                // fingers from (synthetic) tracked joints: open hand vs fist vs pinch
                var openC = new[] { new[] { 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f }, new[] { 0f, 0f, 0f } };
                var fistC = new[] { new[] { 25f, 35f }, new[] { 75f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f }, new[] { 80f, 90f, 55f } };
                var pinchC = new[] { new[] { 30f, 30f }, new[] { 45f, 45f, 30f }, new[] { 25f, 30f, 20f }, new[] { 25f, 30f, 20f }, new[] { 25f, 30f, 20f } };
                var jts = new Dictionary<SimpleHandVisual, Vector3[]>();
                SimpleHandVisual.QaJoint = (v, i) => jts.TryGetValue(v, out var arr) ? arr[i] : (Vector3?)null;
                float Tip(HumanBodyBones distal, HumanBodyBones inter)
                {
                    var d = self.A.GetBoneTransform(distal); var m = self.A.GetBoneTransform(inter); var w = self.A.GetBoneTransform(HumanBodyBones.RightHand);
                    if (d == null || m == null || w == null) return -1f; var tip = d.position + (d.position - m.position) * 0.8f; return Vector3.Distance(tip, w.position);
                }
                Vector3 TipPos(HumanBodyBones distal, HumanBodyBones inter) { var d = self.A.GetBoneTransform(distal); var m = self.A.GetBoneTransform(inter); return d.position + (d.position - m.position) * 0.8f; }
                var res = new Dictionary<string, (float idx, float mid, float pinchGap, bool tracked)>();
                foreach (var (pn, cc, thumbIn) in new[] { ("open", openC, 0f), ("fist", fistC, 35f), ("pinch", pinchC, 30f) })
                {
                    foreach (var v in gl)
                    {
                        bool left = v.IsLeft; var wrist = eyes0 + new Vector3(left ? -0.12f : 0.12f, -0.3f, 0.32f);
                        var rot = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(-10f, left ? 15f : -15f, left ? 25f : -25f);
                        jts[v] = SynthHand(wrist, rot, left, cc, thumbIn);
                        var src = left ? _l : _r; src.transform.SetPositionAndRotation(wrist + rot * Vector3.forward * 0.09f, rot); src.simPinch = 0f; src.palmNormal = rot * Vector3.down;
                    }
                    yield return new WaitForSeconds(0.5f);
                    float gap = Vector3.Distance(TipPos(HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate), TipPos(HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate));
                    res[pn] = (Tip(HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate), Tip(HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate), gap, self.FingersTracked[1]);
                    _headRot = Quaternion.LookRotation(new Vector3(0f, -0.55f, 1f)); yield return new WaitForSeconds(0.1f);
                    yield return Late(() => Shot("avatar_self_hand_" + pn));
                    _headRot = Quaternion.LookRotation(Vector3.forward);
                }
                // ground truth (owner, Quest c15: my Genies hands did not turn like my real hands): synthetic tracked hands in real poses;
                // the Genies hand's own finger direction and palm direction are compared with the tracked joints - not with the rotation
                // the body was given
                {
                    var poses = new (string name, Quaternion rel)[] {
                        ("palm down", Quaternion.identity), ("palm up", Quaternion.Euler(0f, 0f, 180f)), ("palm in (karate)", Quaternion.Euler(0f, 0f, 90f)),
                        ("fingers up (wave)", Quaternion.Euler(-80f, 0f, 0f)), ("pointing left", Quaternion.Euler(0f, -70f, 0f)), ("tilted 45", Quaternion.Euler(-30f, 25f, 45f)) };
                    float oldF = 0f, oldP = 0f;
                    for (int pass = 0; pass < 2; pass++) {
                    TrackedGeniesBody.QaOldHandFrame = pass == 0;
                    float worstF = 0f, worstP = 0f; string worstWhat = ""; var rows = new List<string>();
                    Vector3 Palm(Vector3 w, Vector3 ip, Vector3 lp, bool left) { var n = Vector3.Cross(ip - w, lp - w).normalized; return left ? n : -n; }
                    foreach (var (pn, rel) in poses)
                    {
                        foreach (var v in gl)
                        {
                            bool left = v.IsLeft; var wrist = eyes0 + new Vector3(left ? -0.15f : 0.15f, -0.3f, 0.35f);
                            var rot = Quaternion.LookRotation(Vector3.forward) * rel;   // SynthHand mirrors the left hand itself
                            jts[v] = SynthHand(wrist, rot, left, openC, 0f);
                            var src = left ? _l : _r; var j = jts[v];
                            var tf = j[7] - j[0]; var tp = Palm(j[0], j[4], j[13], left);
                            src.transform.SetPositionAndRotation(wrist + tf.normalized * 0.09f, Quaternion.LookRotation(tf, -tp)); src.simPinch = 0f; src.palmNormal = tp;
                        }
                        yield return new WaitForSeconds(0.6f);
                        foreach (var v in gl)
                        {
                            bool left = v.IsLeft; var j = jts[v];
                            var tf = (j[7] - j[0]).normalized; var tp = Palm(j[0], j[4], j[13], left);
                            var hb = self.A.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                            var mp = self.A.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
                            var ip = self.A.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
                            var lp = self.A.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
                            float af = Vector3.Angle(mp.position - hb.position, tf), ap = Vector3.Angle(Palm(hb.position, ip.position, lp.position, left), tp);
                            rows.Add($"{(left ? "L" : "R")} {pn}: fingers {af:0} deg, palm {ap:0} deg");
                            if (af > worstF) worstF = af; if (ap > worstP) { worstP = ap; worstWhat = (left ? "left " : "right ") + pn; }
                        }
                        _headRot = Quaternion.LookRotation(new Vector3(0f, -0.5f, 1f)); yield return null;
                        yield return Late(() => Shot("avatar_self_handmatch_" + pn.Split(' ')[0] + "_" + pn.Split(' ')[1].Trim('(', ')')));
                        _headRot = Quaternion.LookRotation(Vector3.forward);
                    }
                    _rep.events.Add($"AVATARS hand match ({(pass == 0 ? "OLD c15 conversion" : "fixed")}): " + string.Join(" | ", rows));
                    if (pass == 0) { oldF = worstF; oldP = worstP; continue; }
                    Ck("my_hands_turn_like_my_real_hands", worstF < 20f && worstP < 25f, $"Genies hand vs tracked hand over 6 poses x 2 hands: worst finger direction {worstF:0} deg, worst palm direction {worstP:0} deg ({worstWhat}); with the c15 conversion: {oldF:0} / {oldP:0} deg");
                    }
                    TrackedGeniesBody.QaOldHandFrame = false;
                }
                SimpleHandVisual.QaJoint = null;
                var o = res["open"]; var fi = res["fist"]; var pi = res["pinch"];
                Ck("own_fingers_follow_tracking", o.tracked && fi.tracked && fi.mid < o.mid * 0.75f && fi.idx < o.idx * 0.75f,
                   $"my Genies fingers from tracked joints ({(o.tracked ? "tracked" : "NOT TRACKED - grip curl")}): fingertip to wrist open {o.idx * 100f:0.0}/{o.mid * 100f:0.0} cm -> fist {fi.idx * 100f:0.0}/{fi.mid * 100f:0.0} cm (index/middle); pinch: thumb-index tips {pi.pinchGap * 100f:0.0} cm apart (open {o.pinchGap * 100f:0.0})");
                // UI near me is drawn over my body / hands
                var wm = FindFirstObjectByType<WristMenu>(FindObjectsInactive.Include);
                bool wristTop = wm != null && wm.GetComponentsInChildren<Renderer>(true).Where(r => !(r is TMPro.TextMeshPro)).All(r => r.sharedMaterials.All(mm => mm == null || mm.renderQueue >= 3990));
                { var src0 = UiKit.Cutout(UiKit.Tex("icon_check")); var top0 = TaskHud.Top(src0); int q0 = top0.renderQueue; yield return null; yield return null;
                  _rep.events.Add($"AVATARS queue probe: src {src0.name} q{src0.renderQueue} -> copy {top0.name} q{q0}, two frames later q{top0.renderQueue}; same object as src {ReferenceEquals(src0, top0)}"); }
                MenuBuilder.ShowRules(GameRoot.I); yield return new WaitForSeconds(0.6f);
                var board = MenuBuilder.CurrentPopup != null ? MenuBuilder.CurrentPopup.gameObject : null;
                bool boardTop = board != null && board.GetComponentsInChildren<Renderer>(true).Where(r => !(r is TMPro.TextMeshPro)).All(r => r.sharedMaterials.All(mm => mm == null || mm.renderQueue >= 3990));
                string lowB = board != null ? string.Join(", ", board.GetComponentsInChildren<Renderer>(true).Where(r => !(r is TMPro.TextMeshPro) && r.sharedMaterials.Any(mm => mm != null && mm.renderQueue < 3990)).Take(4).Select(r => $"{r.name}<{r.transform.parent?.name}> q{r.sharedMaterial?.renderQueue} {r.GetType().Name}")) : "";
                if (board != null)
                {
                    var lf = board.GetComponent<LecternFollow>();
                    var ic0 = board.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("icon_"));
                    _rep.events.Add($"AVATARS board before: passes {(lf != null ? lf.QaOnTopPasses : -1)}; icon mat {(ic0 != null ? ic0.sharedMaterial.name + " q" + ic0.sharedMaterial.renderQueue + " id " + ic0.sharedMaterial.GetInstanceID() : "-")}; Top() of it now -> {(ic0 != null ? TaskHud.Top(ic0.sharedMaterial).name + " id " + TaskHud.Top(ic0.sharedMaterial).GetInstanceID() : "-")}");
                    TaskHud.DrawOnTop(board.transform);
                    int still = board.GetComponentsInChildren<Renderer>(true).Count(r => !(r is TMPro.TextMeshPro) && r.sharedMaterials.Any(mm => mm != null && mm.renderQueue < 3990));
                    var ic = board.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("icon_"));
                    _rep.events.Add($"AVATARS board: LecternFollow {(lf != null ? (lf.enabled ? "present" : "disabled") : "MISSING")}; after a manual DrawOnTop {still} renderers still below 3990; icon mat {(ic != null ? ic.sharedMaterial.name + " q" + ic.sharedMaterial.renderQueue + " shader " + ic.sharedMaterial.shader.name : "-")}");
                }
                MenuBuilder.ClosePopup();
                Ck("ui_over_my_body", wristTop && boardTop, $"wrist watch {(wristTop ? "on top" : "DEPTH-TESTED")}; menu board {(board == null ? "not opened" : boardTop ? "on top" : "DEPTH-TESTED: " + lowB)} - my arms / hands cannot hide them");
                _headPos = eyes0; _headRot = Quaternion.LookRotation(Vector3.forward);
            }
            // a friend, driven like the network drives it (head + two hand poses)
            // the friend stands on open floor (nothing within 1.1 m), the camera in front of them
            Vector3 spot = c0; float best = 1e9f;
            for (float gx = -3.5f; gx <= 3.5f; gx += 0.25f) for (float gz = -3.5f; gz <= 3.5f; gz += 0.25f)
            {
                var p = c0 + new Vector3(gx, 0f, gz); if (Physics.CheckCapsule(p + Vector3.up * 0.8f, p + Vector3.up * 1.7f, 0.7f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var cp = p + Vector3.back * 2.1f + Vector3.right * 0.5f; if (Physics.CheckSphere(cp + Vector3.up * 1.2f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                float d = Flat(p - c0).magnitude; if (d < best) { best = d; spot = p; }
            }
            Vector3 fh = new Vector3(spot.x, floor + 1.62f, spot.z); Quaternion fr = Quaternion.LookRotation(Vector3.back);
            Pose fl = new Pose(fh + fr * new Vector3(-0.2f, -0.45f, 0.3f), fr), frt = new Pose(fh + fr * new Vector3(0.2f, -0.45f, 0.3f), fr); float fgrip = 0.15f;
            var fg = new GameObject("QA_Friend"); fg.transform.SetParent(_room.transform, false); fg.transform.position = new Vector3(fh.x, floor, fh.z);
            var fav = new GameObject("avatar").AddComponent<SiblingAvatar>(); fav.transform.SetParent(fg.transform, false); fav.Build(1, "Rory", false, 1);
            w = 0; while (w < 90f && !fav.HasGeniesBody) { w += Time.deltaTime; yield return null; }
            var fb = fav.HasGeniesBody ? TrackedGeniesBody.Attach(fav, fg.transform, false, () => new Pose(fh, fr), () => fl, () => frt, () => fgrip, () => fgrip) : null;
            w = 0; while (fb != null && w < 10f && !fb.Calibrated) { w += Time.deltaTime; yield return null; }
            Ck("friend_body_loaded", fb != null && fb.Calibrated, fb == null ? $"friend Genies body not loaded after {w:0} s" : "friend Genies body tracked");
            if (fb == null) yield break;
            Vector3 cam = fh + new Vector3(0.5f, -0.4f, -2.1f);
            _rep.events.Add($"{_cur} harness: friend on open floor at {_room.transform.InverseTransformPoint(spot):F2} ({(best < 1e8f ? "found" : "none found, room centre")})");
            _headPos = cam; _headRot = Quaternion.LookRotation(fh + Vector3.down * 0.7f - cam);
            // 1 standing, hands in front
            yield return new WaitForSeconds(1.2f);
            Ck("friend_stands_at_height", fb.HeadError < 0.10f && Mathf.Abs(fb.FootLift) < 0.08f && Mathf.Abs(fb.EyeHeight - 1.62f) < 0.05f,
               $"eyes {fb.EyeHeight:0.00} m -> scale {fb.Scale:0.00}; head {fb.HeadError * 100f:0} cm from the headset; feet {fb.FootLift * 100f:0} cm off the floor");
            yield return Late(() => ShotFrom("avatar_friend_stand", cam, fh + Vector3.down * 0.7f));
            // 2 reaching out, one hand turned, holding something (fingers close)
            fl = new Pose(fh + fr * new Vector3(-0.25f, -0.25f, 0.5f), fr * Quaternion.Euler(0, 0, -70f)); frt = new Pose(fh + fr * new Vector3(0.3f, -0.1f, 0.48f), fr * Quaternion.Euler(-30f, 0, 0)); fgrip = 0.95f;
            yield return new WaitForSeconds(1.0f);
            Ck("friend_hands_follow", fb.HandErrorL < 0.05f && fb.HandErrorR < 0.05f && fb.HandAngleErrL < 30f && fb.HandAngleErrR < 30f,
               $"wrists {fb.HandErrorL * 100f:0.0} / {fb.HandErrorR * 100f:0.0} cm from the tracked hands; hand turn error {fb.HandAngleErrL:0} / {fb.HandAngleErrR:0} deg; fingers closed (grip {fgrip:0.0})");
            yield return Late(() => ShotFrom("avatar_friend_reach", cam, fh + Vector3.down * 0.5f));
            // 3 crouch to pick something off the floor
            fgrip = 0.15f; fh = new Vector3(fh.x, floor + 0.95f, fh.z); var down = Quaternion.LookRotation(fr * Vector3.forward + Vector3.down * 1.2f);
            fl = new Pose(new Vector3(fh.x, floor + 0.2f, fh.z) + fr * new Vector3(-0.15f, 0, 0.4f), down); frt = new Pose(new Vector3(fh.x, floor + 0.2f, fh.z) + fr * new Vector3(0.15f, 0, 0.4f), down); fr = down;
            yield return new WaitForSeconds(1.5f);
            Ck("friend_crouches", fb.CrouchDepth > 0.5f && Mathf.Abs(fb.FootLift) < 0.08f && fb.HeadError < 0.18f && Mathf.Abs(fb.EyeHeight - 1.62f) < 0.05f && fb.HandErrorL < 0.08f && fb.HandErrorR < 0.08f,
               $"crouched {fb.CrouchDepth * 100f:0} cm (eyes stay calibrated at {fb.EyeHeight:0.00} m); feet {fb.FootLift * 100f:0} cm off the floor; head {fb.HeadError * 100f:0} cm from the headset; wrists {fb.HandErrorL * 100f:0} / {fb.HandErrorR * 100f:0} cm (arm stretch {fb.StretchL:0.00} / {fb.StretchR:0.00})");
            yield return Late(() => ShotFrom("avatar_friend_crouch", cam, new Vector3(fh.x, floor + 0.6f, fh.z)));
            // 4 stand up and turn the head far right: the body turns after it
            fh = new Vector3(fh.x, floor + 1.62f, fh.z); fr = Quaternion.LookRotation(Quaternion.Euler(0, 110f, 0) * Vector3.back);
            fl = new Pose(fh + fr * new Vector3(-0.2f, -0.5f, 0.25f), fr); frt = new Pose(fh + fr * new Vector3(0.2f, -0.5f, 0.25f), fr);
            float turned = 0; yield return null; float y0 = fg.transform.eulerAngles.y;
            while (turned < 2f && Mathf.Abs(Mathf.DeltaAngle(fg.transform.eulerAngles.y, fr.eulerAngles.y)) > 6f) { turned += Time.deltaTime; yield return null; }
            Ck("friend_body_turns_with_head", Mathf.Abs(Mathf.DeltaAngle(fg.transform.eulerAngles.y, fr.eulerAngles.y)) <= 6f, $"head turned 110 deg; body {y0:0} -> {fg.transform.eulerAngles.y:0} deg after {turned:0.0} s");
            yield return Late(() => ShotFrom("avatar_friend_turned", cam, fh + Vector3.down * 0.7f));
            // 5 walk 2 m
            var start = fg.transform.position; var dir = fr * Vector3.forward; dir.y = 0; dir.Normalize(); float wt = 0, maxSpeed = 0; bool walkShot = false;
            while (wt < 1.6f)
            {
                wt += Time.deltaTime; fh += dir * 1.25f * Time.deltaTime;
                fl = new Pose(fh + fr * new Vector3(-0.2f, -0.55f, 0.15f), fr); frt = new Pose(fh + fr * new Vector3(0.2f, -0.55f, 0.15f), fr);
                var an = fb.A; if (an != null && an.runtimeAnimatorController != null) foreach (var pr in an.parameters) if (pr.name == "Speed") maxSpeed = Mathf.Max(maxSpeed, an.GetFloat("Speed"));
                if (!walkShot && wt > 0.8f) { walkShot = true; yield return Late(() => ShotFrom("avatar_friend_walk", cam, fg.transform.position + Vector3.up * 0.8f)); }
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);
            float moved = Flat(fg.transform.position - start).magnitude;
            Ck("friend_walks", moved > 1.6f && maxSpeed > 0.6f && Mathf.Abs(fb.FootLift) < 0.15f, $"walked {moved:0.00} m; locomotion Speed {maxSpeed:0.00}; feet {fb.FootLift * 100f:0} cm off the floor");
        }


        IEnumerator FurnitureLab()
        {
            _cur = "FURNITURE";
            var chest = MechFurniture.All.FirstOrDefault(f => f != null && f.Kind == "chest"); var bin = MechFurniture.All.FirstOrDefault(f => f != null && f.Kind == "bin");
            Ck("furniture_built", chest != null && bin != null && chest.Drawers.Count == 3 && bin.Lid != null, $"chest drawers {(chest != null ? chest.Drawers.Count : 0)}, bin lid {(bin != null && bin.Lid != null)}");
            if (chest != null)
            {
                yield return Safe(WalkTo(chest.Focus)); FaceHost(chest, 0.9f, 0.3f); Park(); yield return new WaitForSeconds(0.3f); Shot("chest_front");
                for (int i = 0; i < chest.Drawers.Count - 1; i++)
                {
                    var d = chest.Drawers[i]; yield return Safe(SlideRail(d, chest, 1f)); if (i == 0) Shot("chest_drawer_open");
                    int s0 = d.Slams; yield return Safe(SlideRail(d, chest, 0f, i == 0 ? 1.4f : 0.15f));
                    if (i == 0) Ck("chest_drawer_shove_slams", d.Slams > s0, $"shoved shut at 1.4 m/s: slams +{d.Slams - s0}");
                }
                var r = _round; int id = chest.Drawers[0].HouseId;
                Ck("house_mech_replicated", r.HouseMech.ContainsKey(id) && r.HouseMech[id] == 0 && chest.Drawers[0].GetComponent<HouseMechSync>().Reports >= 2, $"drawer id {id}: round level {(r.HouseMech.TryGetValue(id, out var lv) ? lv : -1)}; reports {chest.Drawers[0].GetComponent<HouseMechSync>().Reports}; all house mechs [{string.Join(", ", r.HouseMech.Select(kv => kv.Key + ":" + kv.Value))}]");
            }
            if (bin != null && bin.Lid != null)
            {
                yield return Safe(SwingHinge(bin.Lid, bin, 1f, 0.45f)); FaceHost(bin, 0.7f, 0.5f); yield return new WaitForSeconds(0.2f); Shot("bin_lid_open");
                yield return Safe(SwingHinge(bin.Lid, bin, 0f, 0.3f));
            }
            _cur = "LD09";
        }

        /// <summary>V12 washer lab after LD04 (desktop PlayMode, simulated hands; Quest UNTESTED): locked door while running, laundry tumbling in
        /// the turning drum, dial to Spin, stop, slam vs soft close, detergent drawer on its rail, hand-held detergent pour into the tray.</summary>
        IEnumerator WasherLab()
        {
            _cur = "WASHER"; var wa = WasherAppliance.All.FirstOrDefault(a => a != null && a.Kind == "washer");
            if (wa == null) { Ck("washer_built", false, "no WasherAppliance in the house"); yield break; }
            Ck("washer_built", true, $"door {wa.Door != null}, drawer {wa.Drawer != null}, dial {wa.Dial != null}, start {wa.StartButton != null}; drum r {wa.DrumRadius:0.000} m depth {wa.DrumDepth:0.000} m; dryer present {WasherAppliance.All.Any(a => a != null && a.Kind == "dryer")}");
            var fv = _rt.Fixtures.FirstOrDefault(f => f != null && f.Appliance == wa);
            yield return Safe(WalkTo(wa.DrumCentre)); FaceAppliance(wa, 0.9f, 0.6f); Park();
            yield return new WaitForSeconds(0.3f); Shot("washer_front");
            if (!wa.Running && fv != null) yield return Safe(PressStart(wa));
            // 1. locked door: a hand pulls the handle while the drum turns
            if (wa.Running)
            {
                int r0 = wa.Door.Rattles; float before = wa.Door.Angle;
                var h = Free() ?? _l; h.transform.rotation = Quaternion.LookRotation(-wa.Front, Vector3.up);
                yield return Safe(Move(h, wa.Door.HandleWorld + wa.Front * 0.1f, 0.6f)); yield return Safe(Move(h, wa.Door.HandleWorld, 0.2f));
                h.simPinch = 1; yield return new WaitForSeconds(0.1f);
                yield return Safe(Move(h, wa.Door.HandleWorld + wa.Front * 0.15f, 0.3f)); yield return new WaitForSeconds(0.2f);
                h.simPinch = 0; Park(); yield return new WaitForSeconds(0.3f);
                Ck("washer_door_locked_while_running", wa.Door.Angle < 2f && (fv == null || !fv.State.Open), $"pulled the handle: angle {before:0.0} -> {wa.Door.Angle:0.0} deg, rattles +{wa.Door.Rattles - r0}; rules {(fv != null && fv.State.Open ? "open" : "closed")}");
                // 2. tumbling: the loaded bundles move with the turning drum and stay inside
                var inside = _rt.Bodies.Where(b => b != null && wa.InDrum(b.Col.bounds.center)).ToList();
                var p0 = inside.Select(b => b.Col.bounds.center).ToList(); float d0 = wa.DrumAngle;
                FaceAppliance(wa, 0.55f, 0.15f); yield return new WaitForSeconds(3.5f); Shot("washer_running_tumble");
                float moved = 0f; for (int i = 0; i < inside.Count; i++) moved = Mathf.Max(moved, Vector3.Distance(p0[i], inside[i].Col.bounds.center));
                Ck("washer_drum_tumbles_laundry", inside.Count > 0 && moved > 0.03f && inside.All(b => wa.InDrum(b.Col.bounds.center)) && Mathf.Abs(wa.DrumAngle - d0) > 90f,
                   $"{inside.Count} bundles in the drum; drum turned {wa.DrumAngle - d0:0} deg; max bundle travel {moved * 100f:0.0} cm; all still inside {inside.All(b => wa.InDrum(b.Col.bounds.center))}; machine noise reports {wa.NoiseReports}");
                // 3. programme dial to Spin (louder), then stop with START
                yield return Safe(TurnDial(wa, 4)); yield return new WaitForSeconds(2.5f);
                int nr = wa.NoiseReports; yield return new WaitForSeconds(3.2f);
                Ck("washer_spin_is_audible", wa.NoiseReports > nr, $"noise reports while spinning +{wa.NoiseReports - nr} (owner = whoever pressed START)");
                yield return Safe(TurnDial(wa, 1));
                yield return Safe(PressStart(wa)); yield return new WaitForSeconds(2.5f);
                Ck("washer_stopped", !wa.Running && (fv == null || !fv.State.Flags.Contains("start")), $"running {wa.Running}");
            }
            // 4. soft close vs slam (noise event, Mom can hear it)
            yield return Safe(SwingDoor(wa, 1f, 0.45f));
            Shot("washer_door_open");
            int slams0 = wa.Door.Slams; int ev0 = _round.RecentNoise.Count(e => e.Cause == NoiseCause.Slam);
            yield return Safe(SwingDoor(wa, 0f, 2.4f));
            int slamEvents = _round.RecentNoise.Count(e => e.Cause == NoiseCause.Slam) - ev0;
            Ck("washer_door_slam_is_noise", wa.Door.Slams > slams0, $"fast close (2.4 m/s hand): slams +{wa.Door.Slams - slams0}; slam noise events +{slamEvents}");
            yield return Safe(SwingDoor(wa, 1f, 0.45f)); int s1 = wa.Door.Slams;
            yield return Safe(SwingDoor(wa, 0f, 0.25f));
            Ck("washer_door_soft_close_quiet", wa.Door.Slams == s1 && wa.Door.IsClosed, $"slow close (0.25 m/s): slams +{wa.Door.Slams - s1}, latched {wa.Door.IsClosed}");
            // 5. detergent drawer + hand-held pour from the house detergent bottle
            yield return Safe(SlideDrawer(wa, 1f));
            var bottle = UnityEngine.Object.FindObjectsByType<PropBody>(FindObjectsSortMode.None).FirstOrDefault(b => b.Spec.Kind == "detergent");
            var blc = bottle != null ? bottle.GetComponentInChildren<LiquidContainer>() : null;
            if (bottle == null || blc == null || wa.Detergent == null) Ck("detergent_pour", false, $"bottle {(bottle != null)}, bottle liquid {(blc != null)}, tray {(wa.Detergent != null)}");
            else
            {
                yield return Safe(WalkTo(bottle.Col.bounds.center));
                {
                    var hb = Free() ?? _r; hb.simPinch = 0; yield return new WaitForSeconds(0.1f);
                    var toMe = _headPos - bottle.Col.bounds.center; toMe.y = 0; toMe.Normalize();
                    var side = bottle.Col.ClosestPoint(bottle.Col.bounds.center + toMe * 0.3f); side = Vector3.Lerp(side, bottle.Col.bounds.center, 0.15f);
                    hb.transform.rotation = Quaternion.LookRotation(-toMe, Vector3.up);
                    yield return Safe(Move(hb, side + toMe * 0.1f, 0.6f)); yield return Safe(Move(hb, side, 0.25f));
                    hb.simPinch = 1; yield return new WaitForSeconds(0.3f);
                    if (Int(hb).Held != bottle) _rep.events.Add($"WASHER bottle side grip took {(Int(hb).Held ? Int(hb).Held.Spec.Kind : "nothing")}: {Int(hb).LastGrabCandidates}; bundles at {string.Join(" ", _rt.Bodies.Where(x => x != null && x.Spec.Kind.Contains("bundle")).Select(x => x.Col.bounds.center.ToString("F2")))}");
                    else yield return Safe(Move(hb, hb.transform.position + Vector3.up * 0.12f, 0.4f));
                }
                var h = HandHolding(bottle);
                if (h != null)
                {
                    // the cap: the other hand pinches it and unscrews it about the bottle's own axis (1.5 turns), then lets the cap drop
                    yield return Safe(WalkTo(wa.DrumCentre)); FaceAppliance(wa, 0.6f, 0.8f);   // carry it closed; open it where it is used
                    // hold it upright like a person would before opening it (turn the wrist until the bottle stands)
                    for (int k = 0; k < 3 && Vector3.Angle(bottle.transform.up, Vector3.up) > 8f; k++)
                    {
                        var hr0 = h.transform.rotation; var hr1 = Quaternion.FromToRotation(bottle.transform.up, Vector3.up) * hr0;
                        for (float u = 0; u < 1f; u += Time.deltaTime / 0.4f) { h.transform.rotation = Quaternion.Slerp(hr0, hr1, u); yield return null; }
                        h.transform.rotation = hr1; yield return new WaitForSeconds(0.2f);
                    }
                    _rep.events.Add($"WASHER bottle held at tilt {Vector3.Angle(bottle.transform.up, Vector3.up):0} deg before uncapping");
                    var cap = bottle.GetComponentInChildren<ScrewCap>();
                    if (cap != null)
                    {
                        float spilledCarry = Puddle.All.Sum(x => x.Litres);
                        var h2 = Free(); if (h2 != null)
                        {
                            h2.simPinch = 0; h2.transform.rotation = Quaternion.LookRotation(-bottle.transform.up, _headRot * Vector3.forward);
                            yield return Safe(Move(h2, cap.HandleWorld + bottle.transform.up * 0.08f, 0.5f)); yield return Safe(Move(h2, cap.HandleWorld, 0.2f));
                            h2.simPinch = 1; yield return new WaitForSeconds(0.15f); bool capHeld = cap.IsHeld; var rr = h2.transform.rotation;
                            for (int k = 1; k <= 60 && !cap.Off; k++) { h2.transform.rotation = Quaternion.AngleAxis(-10f * k, bottle.transform.up) * rr; /* counter-clockwise from above = loosen */ h2.transform.position = cap.HandleWorld; yield return new WaitForSeconds(0.03f); }
                            yield return Safe(Move(h2, h2.transform.position + bottle.transform.up * 0.06f + Vector3.right * 0.05f, 0.3f));
                            h2.simPinch = 0; yield return new WaitForSeconds(0.3f); Park2(h2);
                            Ck("bottle_cap_unscrewed_by_hand", capHeld && cap.Off && !blc.Sealed, $"cap grasped {capHeld}; unscrewed {cap.Unscrewed:0} deg of {cap.Turns * 360f:0}; off {cap.Off}; nothing spilled while capped {(spilledCarry < 0.001f)} ({spilledCarry * 1000f:0} ml puddles before)");
                        }
                    }
                    float bottle0 = blc.Amount, tray0 = wa.Detergent.Amount, pud0 = Puddle.All.Sum(x => x.Litres);
                    yield return Safe(WalkTo(wa.DrumCentre)); FaceAppliance(wa, 0.6f, 0.8f);
                    _rep.events.Add($"WASHER before pour: drawer travel {wa.Drawer.Position * 100f:0.0} cm, grabs {wa.Drawer.Grabs}");
                    var trayC = wa.Detergent.transform.TransformPoint(new Vector3(0, wa.Detergent.RimY, 0));
                    yield return Safe(Move(h, h.transform.position + (trayC + Vector3.up * 0.34f + wa.Front * 0.06f - bottle.Col.bounds.center), 0.4f));   // well clear of the machine top
                    for (int k = 0; k < 3 && Vector3.Angle(bottle.transform.up, Vector3.up) > 8f; k++)
                    {
                        var u0 = h.transform.rotation; var u1 = Quaternion.FromToRotation(bottle.transform.up, Vector3.up) * u0;
                        for (float u = 0; u < 1f; u += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(u0, u1, u); yield return null; }
                        h.transform.rotation = u1; yield return new WaitForSeconds(0.15f);
                    }
                    var right = Vector3.Cross(Vector3.up, wa.Front).normalized; var r0 = h.transform.rotation; float t = 0f;
                    for (float a = 0; a <= 125f; a += 5f)
                    {
                        h.transform.rotation = Quaternion.AngleAxis(a, right) * r0; yield return new WaitForFixedUpdate();
                        if (Mathf.Abs(a % 25f) < 0.1f) _rep.events.Add($"WASHER tilt {a:0}: bottle tilt {Vector3.Angle(bottle.transform.up, Vector3.up):0} deg, liquid up tilt {Vector3.Angle(blc.transform.up, Vector3.up):0}, held {HandHolding(bottle) != null}, sealed {blc.Sealed}, amount {blc.Amount * 1000f:0} ml cap {blc.Capacity * 1000f:0} r {blc.InnerRadius:0.000} floor {blc.FloorY:0.000} rim {blc.RimY:0.000}, pouring {blc.Pouring}, lands {blc.StreamLanding}");
                        var lip = blc.transform.TransformPoint(new Vector3(0, blc.RimY, 0)); var dlt = trayC - wa.Front * 0.06f + Vector3.up * 0.06f - lip;   // the stream leaves the lip moving forward: aim a little behind the centre dlt.y = Mathf.Min(0f, dlt.y);
                        h.transform.position += new Vector3(dlt.x, 0f, dlt.z) * 0.5f; yield return new WaitForSeconds(0.05f);
                        if (a > 60f && blc.Pouring && t == 0f) { Shot("detergent_pour"); t = Time.time; _rep.events.Add($"WASHER pour at {a:0} deg: stream lands on {blc.StreamLanding}; tray opening centre {trayC:F3}, tray in opening {wa.Detergent.InOpening(trayC)}; drawer travel {wa.Drawer.Position * 100f:0.0} cm held {wa.Drawer.IsHeld} grabs {wa.Drawer.Grabs}, handle {wa.Drawer.HandleWorld:F3}, tray r {wa.Detergent.InnerRadius:0.000}"); }
                    }
                    float w0 = 0; while (w0 < 2.5f && wa.Detergent.Amount < 0.06f) { w0 += Time.deltaTime; yield return null; }
                    h.transform.rotation = r0; yield return new WaitForSeconds(0.5f);
                    float poured = bottle0 - blc.Amount, got = wa.Detergent.Amount - tray0, spilled = Puddle.All.Sum(x => x.Litres) - pud0;
                    Ck("detergent_poured_by_hand_into_drawer", got > 0.01f && Mathf.Abs(poured - got - spilled) < 0.01f + poured * 0.05f,
                       $"bottle {bottle0 * 1000f:0} -> {blc.Amount * 1000f:0} ml; tray +{got * 1000f:0} ml; spilled {spilled * 1000f:0} ml; conserved within {(poured - got - spilled) * 1000f:0.0} ml");
                    yield return Safe(PutOnTable(bottle));
                }
                else Ck("detergent_bottle_grab", false, "could not hold the bottle");
            }
            yield return Safe(SlideDrawer(wa, 0f, 0.15f));
            Shot("washer_after");
            Ck("mech_haptics_requested", MechHaptics.Requested > 0, $"{MechHaptics.Requested} haptic pulses requested (controllers only; desktop has none)");
            _cur = "LD04";
        }

        static bool Matches(RecipeFixtureView fv, PokeButton pb, string flagArg)
        {
            string flag = flagArg.TrimStart('!'); string label = flag;
            foreach (var s in fv.Instance.Def.Steps) if (s.Kind == StepKind.Flag && s.Fixture == fv.Decl.Kind && s.FlagName == flag) label = s.Label;
            string shortL = label.Length <= 14 ? label : label.Substring(0, 13) + "…";
            return pb.Label == shortL;
        }

        IEnumerator Rub(RecipeFixtureView fv)
        {
            var h = Free(); if (h == null) { yield return Safe(PutOnTable(_rig.RightHand.Held)); h = Free(); }
            var fwd = fv.transform.forward; h.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up); // palm = pos - forward*0.04
            // V8: scrub ON the active dirt patch (patch-local cleaning), small back-and-forth strokes
            var pw = fv.PatchWorld(fv.ActivePatch);
            var c = new Vector3(pw.x, fv.transform.position.y + 0.04f, pw.z) + h.transform.forward * 0.04f;
            var side = fv.transform.right * Mathf.Min(0.05f, fv.PatchRadius(fv.ActivePatch));
            yield return Safe(Move(h, c + Vector3.up * 0.1f, 0.6f));
            for (int i = 0; i < 5; i++) { yield return Safe(Move(h, c + side, 0.4f)); yield return Safe(Move(h, c - side, 0.4f)); }
            yield return Safe(Move(h, c + Vector3.up * 0.12f, 0.5f));
        }

        IEnumerator Work(PropBody b)
        {
            if (b == null) yield break;
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            var other = h == _r ? _l : _r;
            yield return Safe(Move(h, _headPos + _headRot * new Vector3(0, -0.35f, 0.32f), 0.5f));
            var c = b.Col.bounds.center; other.transform.rotation = Quaternion.LookRotation(_headRot * Vector3.forward, Vector3.up);
            yield return Safe(Move(other, c + _headRot * Vector3.left * 0.05f + Vector3.up * 0.03f, 0.6f));
            for (int i = 0; i < 5; i++) { c = b.Col.bounds.center; yield return Safe(Move(other, c + _headRot * Vector3.right * 0.06f + Vector3.up * 0.03f, 0.45f)); yield return Safe(Move(other, c + _headRot * Vector3.left * 0.06f + Vector3.up * 0.03f, 0.45f)); }
            Park2(other);
        }

        PropBody FindBody(string kind, int recipeInstance)
        {
            foreach (var bb in _rt.Bodies) { var r = _rt.RProp(bb); if (bb != null && bb.Spec.Kind == kind && r != null && r.RecipeInstance == recipeInstance) return bb; }
            return null;
        }

        /// <summary>KT01 pumping: hold the sponge under the nozzle, push the pump pad down with the other hand, lift off (one dose per push).</summary>
        IEnumerator PumpSoap(PropBody sponge, SoapPumpView pump)
        {
            if (sponge == null || pump == null) yield break;
            var h = HandHolding(sponge); if (h == null) { yield return Safe(Grab(sponge)); h = HandHolding(sponge); if (h == null) yield break; }
            var off = h.transform.position - sponge.Col.bounds.center;
            var cp = pump.CatchPoint; int d0 = pump.Doses;
            yield return Safe(Move(h, new Vector3(cp.x, cp.y + 0.15f, cp.z) + off + Vector3.forward * 0.0f, 0.5f));
            yield return Safe(Move(h, cp + Vector3.down * (sponge.Col.bounds.extents.y + 0.01f) + off, 0.25f));
            var other = h == _r ? _l : _r; var top = pump.PadTop;
            // V13 (owner, Quest c22: "the soap is not working ... any kind of gesture to press"): press like a person - 1) an index FINGERTIP
            // pushes the pad top (hand pointing down: the palm stays ~5 cm above), 2) a FLAT PALM pushes it; each must give one dose
            var r0 = other.transform.rotation;
            other.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);   // fingers point down
            var tipOff = other.transform.position - other.IndexTip;
            yield return Safe(Move(other, top + Vector3.up * 0.10f + tipOff, 0.6f));
            yield return Safe(Move(other, top + Vector3.down * 0.004f + tipOff, 0.2f)); yield return new WaitForSeconds(0.15f);
            yield return Safe(Move(other, top + Vector3.up * 0.12f + tipOff, 0.3f)); yield return new WaitForSeconds(0.45f);
            int afterFinger = pump.Doses;
            other.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up); var pn = other.palmNormal; other.palmNormal = Vector3.down;   // flat hand
            var palmOff = other.transform.position - other.PalmCenter;
            yield return Safe(Move(other, top + Vector3.down * 0.004f + Vector3.up * 0.012f + palmOff, 0.3f)); yield return new WaitForSeconds(0.15f);
            yield return Safe(Move(other, top + Vector3.up * 0.14f + palmOff, 0.3f));
            other.palmNormal = pn; other.transform.rotation = r0;
            Ck("pump_press_any_gesture", afterFinger > d0 && pump.Doses > afterFinger, $"fingertip press: {afterFinger - d0} dose(s); flat-palm press: {pump.Doses - afterFinger} dose(s) (pad top {top:F3}, pivot {pump.PadWorld:F3})");
            if (pump.Doses == d0) Ck("pump_press", false, "the hand push on the pad did not dose");
            Park2(other);
        }

        /// <summary>KT01 scrubbing: the plate is held in one hand; the other hand takes the (soapy) sponge and scrubs across the plate face.</summary>
        IEnumerator SpongeScrub(PropBody plate)
        {
            if (plate == null) yield break;
            var rp = _rt.RProp(plate); var sponge = rp != null ? FindBody("sponge", rp.RecipeInstance) : null;
            var wash = plate.GetComponent<WashableView>();
            if (sponge == null || wash == null) { yield return Safe(Work(plate)); yield break; }
            var h = HandHolding(plate); if (h == null) { yield return Safe(Grab(plate)); h = HandHolding(plate); if (h == null) yield break; }
            // V11: hold it out at ~0.5 m (turned flat at 0.34 m a 22 cm plate reached into the player's own body collider and stuck on edge)
            yield return Safe(Move(h, _headPos + _headRot * new Vector3(0.05f, -0.30f, 0.50f), 0.5f));
            yield return Safe(FaceUp(plate, h));   // V10: eating face up (stains are on it), like a person tilting the plate toward themselves
            yield return Safe(FaceShot(plate, $"{_cur}_scrub_{plate.Spec.Id}_before"));
            var hs = HandHolding(sponge);
            if (hs == null) { yield return Safe(Grab(sponge)); hs = HandHolding(sponge); if (hs == null) { Ck("sponge_grab", false, "could not take the sponge"); yield break; } }
            var off = hs.transform.position - sponge.Col.bounds.center;
            int s0 = rp.Stage, cells0 = RecipeWorld.CountBits(rp.Dirt); float t0 = Time.time; int strokes = 0;
            // work stain by stain: press the sponge on the face over a stained cell and scrub back and forth across it
            var tries = new Dictionary<int, int>();
            for (int i = 0; i < 40 && rp.Stage == s0 && rp.Dirt != 0; i++)
            {
                // least-tried remaining stain first (round-robin), so one awkward cell near the rim cannot eat the whole budget
                int cell = -1, best = int.MaxValue;
                for (int ci = 0; ci < 64; ci++) if ((rp.Dirt & (1UL << ci)) != 0) { tries.TryGetValue(ci, out var tn); if (tn < best) { best = tn; cell = ci; } }
                tries[cell] = best + 1;
                var n = plate.transform.up; var at = wash.CellWorld(cell) + n * (sponge.Col.bounds.extents.y * 0.55f);
                var side = Vector3.ProjectOnPlane(_headRot * Vector3.right, n).normalized * 0.03f;
                yield return Safe(Move(hs, at + n * 0.03f + off, 0.5f));
                for (int k = 0; k < 3; k++) { yield return Safe(Move(hs, at + side + off, 0.35f)); yield return Safe(Move(hs, at - side + off, 0.35f)); strokes++; }
                yield return new WaitForSeconds(0.2f);
                if (i == 2) yield return Safe(FaceShot(plate, $"{_cur}_scrub_{plate.Spec.Id}_half"));
            }
            Ck("scrub_stains_" + plate.Spec.Id, rp.Dirt == 0 && rp.Stage > s0, $"{cells0} stained cells -> {RecipeWorld.CountBits(rp.Dirt)} after {strokes} sponge strokes in {Time.time - t0:0.0} s; stage {s0} -> {rp.Stage}");
            if (cells0 > 0) yield return Safe(FaceShot(plate, $"{_cur}_scrub_{plate.Spec.Id}_after"));
            yield return Safe(PutOnTable(sponge));
        }

        /// <summary>V11 evidence: a FACE-ON capture of a plate - the camera on the face normal 0.3 m out, looking straight at the eating face;
        /// the stain grid count is written into the event log with the shot name.</summary>
        IEnumerator FaceShot(PropBody plate, string name)
        {
            var hp = _headPos; var hr = _headRot; var n = plate.transform.up; var c = plate.Col.bounds.center;
            var upHint = Vector3.ProjectOnPlane(_headRot * Vector3.forward, n); if (upHint.sqrMagnitude < 1e-4f) upHint = Vector3.ProjectOnPlane(Vector3.forward, n);
            _headPos = c + n * 0.3f; _headRot = Quaternion.LookRotation(-n, upHint.normalized);
            yield return null; yield return null;
            Shot(name); var rp = _rt.RProp(plate);
            _rep.events.Add($"{_cur} face-on shot {name}: {(rp != null ? RecipeWorld.CountBits(rp.Dirt) : -1)} stained cells, plate tilt {Vector3.Angle(n, Vector3.up):0} deg");
            _headPos = hp; _headRot = hr; yield return null;
        }

        /// <summary>Turn the hand until the held plate's eating face points up (a person looks at the face they wash); logged if it fails.</summary>
        IEnumerator FaceUp(PropBody plate, SimHandSource h)
        {
            float a0 = Vector3.Angle(plate.transform.up, Vector3.up); var trace = new System.Text.StringBuilder();
            bool both = _rig.LeftHand.Held == plate && _rig.RightHand.Held == plate;
            for (int k = 0; k < 6 && Vector3.Angle(plate.transform.up, Vector3.up) > 10f; k++)
            {
                trace.Append($"{Vector3.Angle(plate.transform.up, Vector3.up):0}/hand{Quaternion.Angle(h.transform.rotation, Quaternion.identity):0} ");
                var r0 = h.transform.rotation; var r1 = Quaternion.FromToRotation(plate.transform.up, Vector3.up) * r0;
                for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; }
                h.transform.rotation = r1; yield return new WaitForSeconds(0.12f);
            }
            float a1 = Vector3.Angle(plate.transform.up, Vector3.up);
            if (a1 > 20f)
            {
                // V11: blocked (the plate presses on the counter / backsplash in front of us) - step back like a person and turn it again
                var back = -Vector3.ProjectOnPlane(_headRot * Vector3.forward, Vector3.up).normalized * 0.4f; var hp0 = _headPos;
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f) { var d = back * (Time.deltaTime / 0.5f); _headPos += d; _l.transform.position += d; _r.transform.position += d; yield return null; }
                for (int k = 0; k < 6 && Vector3.Angle(plate.transform.up, Vector3.up) > 10f; k++)
                {
                    var r0 = h.transform.rotation; var r1 = Quaternion.FromToRotation(plate.transform.up, Vector3.up) * r0;
                    for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; }
                    h.transform.rotation = r1; yield return new WaitForSeconds(0.12f);
                }
                trace.Append($"| stepped back 0.4 m -> {Vector3.Angle(plate.transform.up, Vector3.up):0}");
                a1 = Vector3.Angle(plate.transform.up, Vector3.up);
            }
            if (a1 > 20f && Physics.Raycast(plate.Col.bounds.center, Vector3.down, out var below, 1.5f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                // V11 grip transition: set it down on the surface below and pick it up again from above (it lies flat), like a person would
                var off = h.transform.position - plate.Col.bounds.center;
                yield return Safe(Move(h, below.point + Vector3.up * (plate.Col.bounds.extents.y + 0.03f) + off, 0.4f));
                h.simPinch = 0; yield return new WaitForSeconds(0.6f);
                yield return Safe(Move(h, h.transform.position + Vector3.up * 0.1f, 0.4f));
                yield return Safe(Grab(plate));
                h = HandHolding(plate) ?? h;
                for (int k = 0; k < 6 && HandHolding(plate) != null && Vector3.Angle(plate.transform.up, Vector3.up) > 10f; k++)
                {
                    var r0 = h.transform.rotation; var r1 = Quaternion.FromToRotation(plate.transform.up, Vector3.up) * r0;
                    for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { h.transform.rotation = Quaternion.Slerp(r0, r1, t); yield return null; }
                    h.transform.rotation = r1; yield return new WaitForSeconds(0.12f);
                }
                trace.Append($"| re-gripped from the surface -> {Vector3.Angle(plate.transform.up, Vector3.up):0}");
                a1 = Vector3.Angle(plate.transform.up, Vector3.up);
            }
            if (a1 > 10f || a0 > 30f) _rep.events.Add($"{_cur} harness: face-up plate#{plate.Spec.Id}: {a0:0} deg -> {a1:0} deg; held by {(both ? "BOTH hands" : h == _l ? "left" : "right")}; kinematic {plate.Body.isKinematic}; follower {_rt.IsFollower(_rt.RProp(plate))}; steps {trace}");
        }

        /// <summary>KT01: carry the scrubbed plate into the running water and hold it there (the runtime rinses it after ~1 s in the stream).</summary>
        IEnumerator RinseUnderTap(PropBody b, SinkTapView tap)
        {
            if (b == null || tap == null) yield break;
            var h = HandHolding(b); if (h == null) { yield return Safe(Grab(b)); h = HandHolding(b); if (h == null) yield break; }
            yield return Safe(FaceUp(b, h));   // V10: the falling water has to land ON the face (an edge-on plate lets it fall past)
            h = HandHolding(b) ?? h;            // a re-grip may have changed the hand
            var off = h.transform.position - b.Col.bounds.center;
            var at = tap.RinsePoint;
            yield return Safe(Move(h, new Vector3(at.x, at.y + 0.12f, at.z) + off, 0.6f));
            yield return Safe(Move(h, at + off, 0.25f));
            float t = 0; var rp = _rt.RProp(b); int s0 = rp != null ? rp.Stage : 0; int frames = 0, wet = 0; var hitNames = new Dictionary<string, int>();
            while (t < RecipeRuntime.RinseSeconds + 1.2f && (rp == null || rp.Stage == s0))
            {
                t += Time.deltaTime; h.transform.position = at + off + new Vector3(Mathf.Sin(t * 9f) * 0.012f, 0, 0); frames++;
                if (tap.Wets(b)) wet++;
                var hn = tap.Stream != null && tap.Stream.HitCollider != null ? tap.Stream.HitCollider.name : "none"; hitNames.TryGetValue(hn, out var hc); hitNames[hn] = hc + 1;
                yield return null;
            }
            _rep.events.Add($"{_cur} harness: rinse plate#{b.Spec.Id}: stream on the plate {wet}/{frames} frames, stage {s0}->{(rp != null ? rp.Stage : -1)}, running {tap.Running}, first hits: {string.Join(", ", hitNames.Select(kv => kv.Key + "x" + kv.Value))}");
            yield return Safe(Move(h, new Vector3(at.x, at.y + 0.15f, at.z) + off, 0.4f));
        }

        void Park2(SimHandSource h) { var bse = _headPos + _headRot * new Vector3(h == _l ? -0.2f : 0.2f, -0.45f, 0.2f); h.transform.position = bse; }

        IEnumerator Join(PropBody item, PropBody partner)
        {
            if (item == null || partner == null) yield break;
            // the solver may name either one as held; bring the held one (or the item) onto the other
            var held = HandHolding(item) != null ? item : HandHolding(partner) != null ? partner : item;
            var other = held == item ? partner : item;
            var h = HandHolding(held); if (h == null) { yield return Safe(Grab(held)); h = HandHolding(held); if (h == null) yield break; }
            var off = h.transform.position - held.Col.bounds.center;
            float top = other.Col.bounds.max.y; var orp = _rt.RProp(other); if (orp != null) foreach (var f in _rt.FollowersOf(orp.Id)) if (f != held) top = Mathf.Max(top, f.Col.bounds.max.y);
            var target = new Vector3(other.Col.bounds.center.x, top + held.Col.bounds.extents.y + 0.004f, other.Col.bounds.center.z);
            // lift clear first, travel above everything, then lower onto the partner (a straight low path sweeps the table)
            float clearY = Mathf.Max(h.transform.position.y, target.y + off.y + 0.12f);
            yield return Safe(Move(h, new Vector3(h.transform.position.x, clearY, h.transform.position.z), 0.4f));
            yield return Safe(Move(h, new Vector3(target.x + off.x, clearY, target.z + off.z), 0.6f));
            yield return Safe(Move(h, target + off + Vector3.up * 0.03f, 0.3f)); yield return Safe(Move(h, target + off, 0.15f));
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Split(PropBody b)
        {
            if (b == null) yield break;
            // a pair comes apart with both halves in hand (one hand alone just carries the pair); a lid/layer comes off with one hand
            var rp = _rt.RProp(b); PropBody other = rp != null && rp.Partner >= 0 ? _rt.Body(rp.Partner) : null;
            if (other != null) { yield return Safe(Grab(other)); yield return new WaitForSeconds(0.15f); }
            yield return Safe(Grab(b)); var h = HandHolding(b); if (h == null) yield break;
            yield return Safe(Move(h, h.transform.position + _headRot * Vector3.left * 0.3f, 0.5f));
            yield return new WaitForSeconds(0.2f);
            if (other != null) { var ho = HandHolding(other); if (ho != null) { ho.simPinch = 0; yield return new WaitForSeconds(0.3f); } }
        }

        IEnumerator Disturb(RecipeInstance inst, Action<string> how)
        {
            var w = _round.Recipes.World;
            var placed = inst.PropsByKind.Values.SelectMany(x => x).Select(w.Prop).FirstOrDefault(p => p.IsPlaced && !(w.Fixture(p.Fixture).HasDoor && !w.Fixture(p.Fixture).Open));
            var joined = inst.PropsByKind.Values.SelectMany(x => x).Select(w.Prop).FirstOrDefault(p => p.Partner >= 0 || p.AttachedTo >= 0);
            var door = inst.Def.Fixtures.FirstOrDefault(f => f.HasDoor && !w.Fixture(inst.Fixture(f.Kind)).Open);
            var flag = inst.Def.Steps.FirstOrDefault(s => s.Kind == StepKind.Flag);
            var staged = inst.PropsByKind.Values.SelectMany(x => x).Select(w.Prop).FirstOrDefault(p => p.Stage > 0);
            if (placed != null)
            {
                how("took " + placed.Kind + " out of its slot"); var b = _rt.Body(placed.Id); yield return Safe(Grab(b));
                // carry it clear of its fixture first (an assembly base is set straight down where it is, which would re-seat it)
                var hh = HandHolding(b);
                if (hh != null && b != null)
                {
                    // drop spot: its spawn, nudged toward the player while any fixture would catch it there (else the slot re-seats it)
                    var sp = _room.transform.TransformPoint(VisualFactory.V(b.Spec.Spawn));
                    var toMe = _headPos - sp; toMe.y = 0; toMe = toMe.sqrMagnitude > 1e-4f ? toMe.normalized : Vector3.back;
                    for (int k = 0; k < 4 && _rt.Fixtures.Any(f => f != null && f.Contains(sp + Vector3.up * 0.03f, 0.2f)); k++) sp += toMe * 0.1f;
                    sp = ClearSpot(sp, b);
                    yield return Safe(Move(hh, hh.transform.position + Vector3.up * 0.15f, 0.4f));
                    yield return Safe(Move(hh, sp + Vector3.up * (0.2f + b.Col.bounds.extents.y) + (hh.transform.position - b.Col.bounds.center), 0.4f));
                    yield return Safe(Drop(hh, b, sp + Vector3.up * (b.Col.bounds.extents.y + 0.005f)));
                }
                else yield return Safe(PutOnTable(b));
            }
            else if (door != null) { how("opened the " + door.Kind); yield return Safe(Poke(_rt.FixtureView(inst.Fixture(door.Kind)), "lid")); }
            else if (joined != null) { how("pulled " + joined.Kind + " apart"); yield return Safe(Split(_rt.Body(joined.Id))); var hh = HandHolding(_rt.Body(joined.Id)); if (hh != null) { hh.simPinch = 0; yield return new WaitForSeconds(0.3f); } }
            else if (flag != null) { how("switched off " + flag.FlagName); yield return Safe(Poke(_rt.FixtureView(inst.Fixture(flag.Fixture)), "!" + flag.FlagName)); }
            else if (staged != null) { how("no physical unfold gesture for " + staged.Kind + " (rules-only sabotage)"); }
            else how("nothing to disturb");
        }

        // ------------------------------------------------------------------ evidence
        // ---- V13 QA film (owner: "I want you to do test in unity to see"): ~10 fps frames with a caption saying what the step is and
        // what each simulated hand is doing, assembled into a video afterwards (QA_Evidence/recipe_v7/film_<name>/frame_NNNN.jpg)
        bool _filming; string _filmCap = ""; int _filmN;
        static string HandState(SimHandSource h)
        {
            if (h == null || !h.simTracked) return "not tracked";
            if (h.simGun) return h.simThumbDown ? "finger gun, THUMB DOWN" : "finger gun, thumb up";
            if (h.palmNormal.y > 0.6f) return "palm up";
            return h.simPinch > 0.5f ? "pinching" : "relaxed";
        }
        IEnumerator Film(string name)
        {
            if (_cam == null) yield break;
            string dir = Path.Combine(Dir, "film_" + name); Directory.CreateDirectory(dir); foreach (var old in Directory.GetFiles(dir, "frame_*.jpg")) File.Delete(old);
            _filming = true; _filmN = 0;
            var cg = new GameObject("qaFilmCaption"); cg.transform.SetParent(_cam.transform, false); cg.transform.localPosition = new Vector3(0f, -0.25f, 0.6f);
            var cap = WorldText.Make(cg.transform, "t", "", new Vector3(0f, 0.025f, 0f), 0.42f, Palette.Yellow, TMPro.TextAlignmentOptions.Center, 1.1f);
            var hands = WorldText.Make(cg.transform, "h", "", new Vector3(0f, -0.035f, 0f), 0.3f, Palette.Cream, TMPro.TextAlignmentOptions.Center, 1.1f);
            WorldText.Board(cg.transform, "bg", new Vector3(0f, 0f, 0.004f), new Vector2(1.16f, 0.13f), new Color(0.05f, 0.04f, 0.12f));
            TaskHud.DrawOnTop(cg.transform);
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 }; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            float next = 0f;
            while (_filming)
            {
                cap.text = _filmCap; hands.text = $"LEFT hand: {HandState(_l)}     RIGHT hand: {HandState(_r)}     hops {_rig.Hops}";
                yield return null;   // not WaitForEndOfFrame: it stops firing while the Editor window is unfocused (the camera is rendered by hand anyway)
                if (Time.unscaledTime < next) continue;
                next = Time.unscaledTime + 0.1f;
                var prev = _cam.targetTexture; var act = RenderTexture.active;
                _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"frame_{_filmN++:0000}.jpg"), tex.EncodeToJPG(85));
                _cam.targetTexture = prev; RenderTexture.active = act;
            }
            Destroy(cg); Destroy(tex); rt.Release(); Destroy(rt);
            _rep.events.Add($"film {name}: {_filmN} frames");
        }

        void Shot(string name)
        {
            if (_cam == null) return;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 }; var prev = _cam.targetTexture; var act = RenderTexture.active;
            _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            _cam.targetTexture = prev; RenderTexture.active = act; Destroy(tex); rt.Release(); Destroy(rt);
        }

        void Finish()
        {
            if (_done) return; _done = true;
            _rep.capturedUtc = DateTime.UtcNow.ToString("O"); _rep.pass = _rep.checks.Count(c => c.pass); _rep.fail = _rep.checks.Count - _rep.pass;
            var json = JsonUtility.ToJson(_rep, true);
            File.WriteAllText(Path.Combine(Dir, "RECIPE_DIAG_RESULT.json"), json);
            File.WriteAllText(Path.Combine(Root, "Automation", "recipe_diag_result.json"), json);
            File.WriteAllText(Path.Combine(Root, "QA_Evidence", "recipe_diag_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmm") + ".json"), json);
            if (File.Exists(Flag)) File.Delete(Flag);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
