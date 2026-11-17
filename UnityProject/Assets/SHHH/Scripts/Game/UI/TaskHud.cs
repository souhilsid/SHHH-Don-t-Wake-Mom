using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 heads-up task indicator (owner: "HUD as indicator of the task" + pet hunger icons). A small painted strip low in the view:
    /// [my current step icon + label] [dog hunger] [cat hunger] [trash left]. Information only - it never spawns or completes anything.
    /// Everything shown is replicated rules state (recipe steps, PetHunger, Binned), so all players see the same truth.
    /// </summary>
    public sealed class TaskHud : MonoBehaviour
    {
        Renderer _taskIcon, _dogIcon, _catIcon, _trashIcon, _dogBar, _catBar; TextMeshPro _taskText, _trashText, _hintText, _momText, _fpsText; GameObject _momPlate;
        static string _caption = ""; static float _captionUntil, _edgeCool;
        /// <summary>QA: allow the next play-space reminder now.</summary>
        public static void QaResetEdgeCooldown() { _edgeCool = 0f; }
        /// <summary>QA: how many play-space edge reminders were shown.</summary>
        public static int EdgeHints;
        /// <summary>QA: the Mom line currently on the HUD.</summary>
        public string MomLine => _momText != null && _momPlate != null && _momPlate.activeSelf ? _momText.text : "";

        /// <summary>A line Mom says while I cannot see her (MomCharacter.Say) - shown on the HUD's Mom line for a few seconds.</summary>
        public static void Caption(string text, float seconds) { _caption = text ?? ""; _captionUntil = Time.time + seconds; }

        /// <summary>Why Mom picked this sibling, in a few words (evidence cards are "Nell saw Pip make the mug crash; ...").</summary>
        public static string Why(string evidence, EliminationReason reason)
        {
            string e = (evidence ?? "").Trim();
            if (e.Length == 0) e = reason == EliminationReason.CaughtMakingNoise ? "she heard the noise and the others saw who did it" : "someone saw who messed with a job";
            return e.Length > 80 ? e.Substring(0, 79) + "…" : e;
        }

        /// <summary>
        /// V12 (Quest run 2026-11-11: "MOM IS UP! -20 PTS" without ever seeing Mom or why; a hungry pet barking left it unclear who was
        /// blamed). One line above the task strip tells every player what Mom is doing and why, from replicated rules state:
        /// what she heard and where, that she is coming, her question, and the verdict with its reason.
        /// </summary>
        public static string MomNews(RoundController r, out Color col)
        {
            col = Palette.Coral;
            if (r == null) return "";
            var st = r.Mom.State; var v = r.Verdict; var q = r.Interro;
            switch (st)
            {
                case MomState.AlmostAwake: col = Palette.Yellow; return "Mom is almost awake - quiet!";
                case MomState.Waking:
                case MomState.WalkingIn: return $"MOM IS UP! She heard {Heard(r)} - she's coming to look";
                case MomState.Investigating: return $"Mom is looking around {Where(r)}";
                case MomState.Inspecting: return "Mom is checking everyone's jobs";
                case MomState.Interrogating:
                    col = Palette.Yellow;
                    if (q == null) return "Mom is asking...";
                    return $"Mom asks: {q.Question} ({(q.EvidenceOpen ? Mathf.CeilToInt(q.Remaining) + " s - answer on the tray" : q.Elapsed < q.Present ? "listen" : "deciding")})";
                case MomState.Verdict:
                case MomState.ReturningToBed:
                {
                    if (v == null) return "";
                    int me = GameServices.LocalSlot;
                    if (v.Kind == MomVerdictKind.Penalty && ActorIds.IsSibling(v.Culprit))
                    {
                        string who = v.Culprit == me ? "YOU" : r.Roster[v.Culprit].DisplayName;
                        return $"Mom caught {who}: -{r.Config.PenaltyPoints:0} pts{(v.Culprit == me && r.Config.TimeoutSeconds > 0 ? $", {r.Config.TimeoutSeconds:0} s in bed" : "")} - {Why(v.Evidence, v.Reason)}";
                    }
                    if (v.Kind == MomVerdictKind.Eliminate && ActorIds.IsSibling(v.Culprit)) return $"Mom caught {(v.Culprit == me ? "YOU" : r.Roster[v.Culprit].DisplayName)} - {Why(v.Evidence, v.Reason)}";
                    col = Palette.Teal;
                    string ev = (v.Evidence ?? "").Trim();
                    return "Mom: warning, no points lost" + (ev.Length > 0 ? " - " + (ev.Length > 70 ? ev.Substring(0, 69) + "…" : ev) : "");
                }
            }
            return "";
        }

        static string Where(RoundController r)
        {
            var e = r.Verdict != null && r.Verdict.DecisiveEventId >= 0 ? r.Ledger.Get(r.Verdict.DecisiveEventId) : null;
            return e != null ? "the " + HouseLayout.PlaceAt(e.Position) : "the house";
        }

        /// <summary>"the mug crash in the kitchen", "the dog barking in the living room"</summary>
        static string Heard(RoundController r)
        {
            var v = r.Verdict; if (v == null || v.DecisiveEventId < 0) return "a noise";
            var q = r.Interro;
            if (q != null && q.Incident != null && q.Incident.NoiseId == v.DecisiveEventId && !string.IsNullOrEmpty(q.Incident.What))
            {
                string w = q.Incident.What.StartsWith("make ") ? q.Incident.What.Substring(5) : q.Incident.What;
                return $"{w} in the {q.Incident.Where}";
            }
            var e = r.Ledger.Get(v.DecisiveEventId); if (e == null) return "a noise";
            string place = HouseLayout.PlaceAt(e.Position);
            if (e.DirectActor == ActorIds.Dog) return $"the dog barking in the {place}";
            if (e.DirectActor == ActorIds.Cat) return $"the cat yowling in the {place}";
            string kind = r.Room.Prop(e.SourceObjectId)?.Kind;
            return kind != null ? $"the {kind.Replace('_', ' ')} crash in the {place}" : $"a noise in the {place}";
        }
        string _iconName = ""; float _next;

        /// <summary>V12 (Quest run 3:44: my own knees covered the task strip when crouching): the HUD draws after the world with depth test
        /// off - over my own body - but before the black fade (queue 4000), so loading stays black.</summary>
        public static void DrawOnTop(Transform root)
        {
            // text manages its own material: set it there (a renderer-level assignment is overwritten by TextMeshPro)
            foreach (var t in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                var m = t.fontMaterial; m.renderQueue = 3995;
                if (m.HasProperty("unity_GUIZTestMode")) m.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
                if (m.HasProperty("_ZTest")) m.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                t.fontMaterial = m;
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is TMPro.TextMeshPro) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Top(mats[i]);
                r.sharedMaterials = mats;
            }
        }

        static readonly System.Collections.Generic.Dictionary<Material, Material> _top = new System.Collections.Generic.Dictionary<Material, Material>();
        /// <summary>The on-top copy of a (shared, cached) UI material: one copy per source, so swapping icons / bar colours every frame
        /// neither leaks material instances nor falls back to a depth-tested material.</summary>
        public static Material Top(Material src)
        {
            if (src == null || src.renderQueue >= 3990) return src;
            if (_top.TryGetValue(src, out var m) && m != null) { if (m.renderQueue < 3990) m.renderQueue = 3990; return m; }
            int baseQ = src.renderQueue;
            m = new Material(src) { name = src.name + " (on top)" };
            // URP re-derives an alpha-clipped material's queue from its surface type + _QueueOffset when it is first drawn (the board icons
            // fell back to 2450): put the difference in the offset too, so the derived queue is the on-top one
            if (m.HasProperty("_QueueOffset")) m.SetFloat("_QueueOffset", m.GetFloat("_QueueOffset") + (3990 - baseQ));
            if (m.HasProperty("_ZTest")) m.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            if (m.HasProperty("unity_GUIZTestMode")) m.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.renderQueue = 3990;   // last: changing the depth properties re-derived the queue of alpha-tested (cutout) materials
            _top[src] = m; return m;
        }

        public static TaskHud Ensure(Transform head)
        {
            if (head == null) return null;
            var old = head.GetComponentInChildren<TaskHud>(true); if (old != null) return old;
            var g = new GameObject("V12_TaskHud"); g.transform.SetParent(head, false);
            g.transform.localPosition = new Vector3(0f, -0.26f, 0.7f); g.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            var h = g.AddComponent<TaskHud>(); h.Build(); return h;
        }

        Renderer Icon(string name, Vector3 at, float size) => UiKit.Quad(transform, name, UiKit.Cutout(UiKit.Tex(name)), at, new Vector2(size, size)).GetComponent<Renderer>();
        Renderer Bar(Vector3 at) { Prim.Box(transform, "bar_bg", at + new Vector3(0, 0, 0.001f), new Vector3(0.05f, 0.008f, 0.002f), Mats.Unlit(new Color(0.16f, 0.08f, 0.22f)), false); return Prim.Box(transform, "bar", at, new Vector3(0.046f, 0.005f, 0.002f), Mats.Unlit(Palette.Yellow), false).GetComponent<Renderer>(); }

        void Build()
        {
            UiKit.Quad(transform, "plate", Mats.Unlit(new Color(0.12f, 0.07f, 0.18f, 1f)), new Vector3(0, 0, 0.003f), new Vector2(0.34f, 0.075f));
            _taskIcon = Icon("icon_task_carry", new Vector3(-0.145f, 0, 0), 0.055f);
            // V12: was 0.11 m wide at 0.22 - "Lift the frame onto the h..." wrapped to four lines
            _taskText = WorldText.Make(transform, "task", "", new Vector3(-0.052f, 0, 0), 0.18f, Palette.Cream, TextAlignmentOptions.Center, 0.13f);
            _dogIcon = Icon("icon_pet_hungry_dog", new Vector3(0.035f, 0.01f, 0), 0.045f); _dogBar = Bar(new Vector3(0.035f, -0.025f, 0));
            _catIcon = Icon("icon_pet_hungry_cat", new Vector3(0.09f, 0.01f, 0), 0.045f); _catBar = Bar(new Vector3(0.09f, -0.025f, 0));
            _trashIcon = Icon("icon_task_trash", new Vector3(0.135f, 0.008f, 0), 0.04f);
            _trashText = WorldText.Make(transform, "trash", "", new Vector3(0.155f, -0.012f, 0), 0.25f, Palette.Cream, TextAlignmentOptions.Center, 0.04f);
            // V12: where the thing I need was left in the house ("plate: on the coffee table in the living room")
            _hintText = WorldText.Make(transform, "hint", "", new Vector3(0f, -0.058f, 0), 0.24f, Palette.Yellow, TextAlignmentOptions.Center, 0.42f);   // V12: was 0.17 - too small to read on the Quest
            _momPlate = UiKit.Quad(transform, "momPlate", Mats.Unlit(new Color(0.12f, 0.07f, 0.18f, 1f)), new Vector3(0, 0.07f, 0.003f), new Vector2(0.46f, 0.05f)).gameObject;
            _momText = WorldText.Make(transform, "mom", "", new Vector3(0f, 0.07f, 0), 0.2f, Palette.Coral, TextAlignmentOptions.Center, 0.44f);
            _momPlate.SetActive(false);
            _fpsText = WorldText.Make(transform, "fps", "", new Vector3(0.15f, 0.03f, 0), 0.12f, Palette.Coral, TextAlignmentOptions.Right, 0.06f);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DrawOnTop(transform);
        }

        /// <summary>Icon for a recipe step from what it acts on (keywords of the prop / fixture kinds).</summary>
        public static string IconFor(RecipeStepDef s)
        {
            string k = ((s.PropKind ?? "") + " " + (s.Fixture ?? "") + " " + (s.Label ?? "")).ToLowerInvariant();
            if (k.Contains("plate") || k.Contains("dish") || k.Contains("cup") || k.Contains("mug")) return k.Contains("tap") || k.Contains("rinse") ? "task_tap" : k.Contains("scrub") ? "task_sponge" : "task_dishes";
            if (k.Contains("sponge") || k.Contains("soap")) return "task_sponge";
            if (k.Contains("washer") || k.Contains("dryer")) return "task_washer";
            if (k.Contains("lint")) return "task_lint";
            if (k.Contains("towel")) return "task_towel";
            if (k.Contains("sock") || k.Contains("cloth") || k.Contains("shirt") || k.Contains("laundry") || k.Contains("bundle")) return "task_laundry";
            if (k.Contains("toy") || k.Contains("block") || k.Contains("car")) return "task_toys";
            if (k.Contains("bin") || k.Contains("trash") || k.Contains("rubbish")) return "task_trash";
            if (k.Contains("bowl") || k.Contains("pet") || k.Contains("food") || k.Contains("feed")) return "task_feed";
            if (k.Contains("drawer") || k.Contains("cupboard") || k.Contains("shelf")) return "task_drawer";
            if (k.Contains("wipe") || k.Contains("spill") || k.Contains("table top")) return "task_wipe";
            if (k.Contains("pour") || k.Contains("jar") || k.Contains("fill")) return "task_pour";
            if (k.Contains("broom") || k.Contains("sweep") || k.Contains("crumb")) return "task_sweep";
            return "task_carry";
        }

        PlayerRig _rig;
        /// <summary>QA: hidden because the screen is faded to black (loading / teleport blink).</summary>
        public bool HiddenByFade { get; private set; }

        void Update()
        {
            // V12 (owner, Quest loading screen): the task strip drew on top of the black "loading the house" fade - hidden while faded
            if (_rig == null) _rig = GetComponentInParent<PlayerRig>();
            bool hide = _rig != null && _rig.FadeAlpha > 0.4f;
            if (hide != HiddenByFade) { HiddenByFade = hide; transform.localScale = hide ? Vector3.zero : Vector3.one; }
            if (Time.time < _next) return; _next = Time.time + 0.25f;
            var round = GameServices.Round; if (round == null) return;
            // my current step (first unfinished step of my first unfinished job)
            string icon = "task_done", text = "all done!", hint = "";
            if (round.Recipes != null)
                foreach (var inst in round.Recipes.Instances)
                {
                    if (inst.Owner != GameServices.LocalSlot || inst.Complete) continue;
                    for (int i = 0; i < inst.Def.Steps.Count; i++) if (!inst.Satisfied[i]) { var s = inst.Def.Steps[i]; icon = IconFor(s); text = s.Label; hint = WhereIs(round, inst, s.PropKind); break; }
                    break;
                }
            // V12 (Quest run: Practice said "all done!" while its board listed six steps): Practice has no jobs - point at the board
            if (GameServices.PracticeMode && text == "all done!") { icon = "task_carry"; text = "Practice: follow the board"; hint = "book > shelf > cup > pan > walk > wrist menu"; }
            if (icon != _iconName) { _iconName = icon; _taskIcon.sharedMaterial = Top(UiKit.Cutout(UiKit.Tex("icon_" + icon))); }
            _taskText.text = text.Length > 34 ? text.Substring(0, 33) + "…" : text;
            // V12 (Quest run #21: the boundary grid came up walking to the dog bed): near the play-space edge, remind me how to walk
            if (!GameServices.PracticeMode)
            {
                var rig = GetComponentInParent<PlayerRig>();
                float edge = Playspace.EdgeDistance(rig, out var src);
                if (Time.time > _edgeCool && (src == "play area" ? edge < 0.35f : src == "walked" && edge < 0f))
                {
                    Caption("Edge of your play space - walk with your left finger gun: point, hold your thumb down", 4f); _edgeCool = Time.time + 12f; EdgeHints++;
                    if (src == "walked") Playspace.MarkStart(rig);
                }
            }
            if (_hintText != null) _hintText.text = hint;
            Hunger(_dogBar, _dogIcon, round.PetHunger[1]); Hunger(_catBar, _catIcon, round.PetHunger[0]);
            var ts = TrashSystem.I; _trashText.text = ts != null ? ts.Left.ToString() : "-";
            // V12: while the headset misses frames (the world judders on head turns), say so on the strip - evidence for the owner
            if (_fpsText != null) { var pm = PerfMonitor.I; bool low = pm != null && pm.Low; _fpsText.text = low ? $"{pm.AvgFps:0} fps" : ""; }
            // Mom line: what she is doing and why; her spoken lines while she is out of sight (not over the wake / question / verdict)
            Color col = Palette.Coral; string news = GameServices.PracticeMode ? "" : MomNews(round, out col);
            bool visit = round.Mom.State >= MomState.Waking && round.Mom.State <= MomState.Verdict;
            if (!visit && Time.time < _captionUntil) { news = _caption; col = Palette.Cream; }
            bool on = !string.IsNullOrEmpty(news);
            if (_momPlate.activeSelf != on) { _momPlate.SetActive(on); _momText.gameObject.SetActive(on); }
            if (on) { _momText.text = news; _momText.color = col; }
        }

        /// <summary>The first of my job's items of this kind that still lies where the family left it (not picked up yet).</summary>
        public static string WhereIs(RoundController round, RecipeInstance inst, string kind)
        {
            var room = GameServices.Room;
            if (string.IsNullOrEmpty(kind) || room == null || inst.PropsByKind == null || !inst.PropsByKind.TryGetValue(kind, out var ids)) return "";
            foreach (var rid in ids)
            {
                if (!round.RoomPropOf.TryGetValue(rid, out var pid)) continue;
                SHHH.Core.PropSpec spec = null; foreach (var sp in round.Room.Props) if (sp.Id == pid) { spec = sp; break; }
                if (spec == null || spec.StartHint == null) continue;
                var body = room.Prop(pid); if (body == null || body.IsHeld) continue;
                var home = room.transform.TransformPoint(VisualFactory.V(spec.Spawn));
                if (Vector3.Distance(body.transform.position, home) < 0.5f) return kind.Replace('_', ' ') + ": " + spec.StartHint;
            }
            return "";
        }

        static void Hunger(Renderer bar, Renderer icon, float h)
        {
            float k = Mathf.Clamp01(h / 100f); bar.transform.localScale = new Vector3(0.046f * k, 0.005f, 0.002f);
            var p = bar.transform.localPosition; bar.transform.localPosition = new Vector3(icon.transform.localPosition.x - 0.023f + 0.023f * k, p.y, p.z);
            bar.sharedMaterial = Top(Mats.Unlit(h >= RoundController.StarvingAt ? Palette.Danger : h >= RoundController.HungryAt ? Palette.Coral : Palette.Ok));
            float pulse = h >= RoundController.HungryAt ? 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.time * 4f)) : 1f; icon.transform.localScale = new Vector3(0.045f * pulse, 0.045f * pulse, 1f);
        }
    }
}
