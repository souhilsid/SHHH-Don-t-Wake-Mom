using System.Text;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Object-local job card floating just above the back edge of a recipe's table (replaces the big "YOUR JOB" wall panel for
    /// recipe rounds). Owner colour + name, recipe name, the 2–5 steps (done = struck through, next = highlighted), the quiet
    /// tip, and a "messed with!" line while a sabotage is unrepaired. Public on purpose: rivals can read it too.
    /// </summary>
    public sealed class RecipeClipboard : MonoBehaviour
    {
        RecipeRuntime _rt; RecipeInstance _inst; TextMeshPro _title, _list; float _next; string _last;
        public RecipeInstance Instance => _inst;
        public string Text => _list != null ? _list.text : "";

        public void Build(RecipeRuntime rt, RecipeInstance inst, RecipeStation st)
        {
            _rt = rt; _inst = inst;
            float yaw = RecipeLayout.PropYaw(st);
            transform.localPosition = VisualFactory.V(st.Local(0f, st.Front * (-st.Depth * 0.5f + 0.01f), 0.3f));
            transform.localRotation = Quaternion.Euler(0, yaw + 180f, 0) * Quaternion.Euler(20f, 0, 0);
            _baseRot = transform.localRotation; _baseYaw = transform.eulerAngles.y;
            var owner = DutyVisuals.ColorFor(rt.Round, inst.Owner);
            var board = WorldText.Board(transform, "card", Vector3.zero, new Vector2(0.34f, 0.19f), Color.Lerp(Palette.Navy, owner, 0.12f));
            Prim.Box(board, "owner_band", new Vector3(0, 0.083f, -0.004f), new Vector3(0.34f, 0.022f, 0.004f), Mats.Unlit(owner), false);
            string who = rt.Round.Roster[inst.Owner].DisplayName;
            _title = WorldText.Make(board, "title", $"<b>{who.ToUpperInvariant()}</b>  ·  {inst.Def.DisplayName}", new Vector3(0, 0.083f, -0.008f), 0.2f, Palette.Navy, TextAlignmentOptions.Center, 0.33f);
            _title.outlineWidth = 0f;
            _list = WorldText.Make(board, "steps", "", new Vector3(0, -0.012f, -0.008f), 0.16f, Palette.Cream, TextAlignmentOptions.TopLeft, 0.32f);
            _list.rectTransform.sizeDelta = new Vector2(0.32f, 0.15f);
            _list.outlineWidth = 0.1f;
        }

        // presentation sounds from replicated recipe state: steps, joins, fold/fill stages, done, "messed with"
        int _sndSteps = -1, _sndJoined, _sndStage; bool _sndDone, _sndMessed;

        void Sounds(bool messed)
        {
            int steps = 0; foreach (var b in _inst.Satisfied) if (b) steps++;
            int joined = 0, stage = 0; Vector3 joinAt = transform.position, stageAt = transform.position; var w = _rt.World;
            foreach (var kv in _inst.PropsByKind) foreach (var id in kv.Value)
                {
                    var p = w.Prop(id); if (p == null) continue; var body = _rt.Body(id);
                    if (p.Partner >= 0 || p.AttachedTo >= 0) { joined++; if (body) joinAt = body.transform.position; }
                    if (p.Stage > 0) { stage += p.Stage; if (body) stageAt = body.transform.position; }
                }
            var ab = AudioBank.I;
            if (_sndSteps >= 0 && ab != null)
            {
                if (joined > _sndJoined) { ab.Play("join_pop", joinAt, 0.7f); RoomFx.Burst("sparkle", joinAt, 0.12f, 1f); }
                if (stage > _sndStage) { Verb v = Verb.Fold; foreach (var s in _inst.Def.Steps) if (s.Kind == StepKind.Stage) v = s.Verb; ab.Play(v == Verb.Fold ? "rustle" : v == Verb.Fill ? "pour" : "wipe", stageAt, 0.7f); RoomFx.Burst("puff", stageAt, 0.12f, 1f); }
                if (_inst.Complete && !_sndDone) ab.Play("chore_done", transform.position, 0.8f);
                else if (steps > _sndSteps && !_inst.Complete) ab.Play("step_done", transform.position, 0.45f);
                if (messed && !_sndMessed) ab.Play("recipe_undone", transform.position, _inst.Owner == GameServices.LocalSlot ? 0.9f : 0.4f);
            }
            _sndSteps = steps; _sndJoined = joined; _sndStage = stage; _sndDone = _inst.Complete; _sndMessed = messed;
        }

        Quaternion _baseRot; float _baseYaw;
        /// <summary>QA: the last turn decision.</summary>
        public string QaInfo = "";
        /// <summary>The card's own (table-front) yaw; its readable side faces -forward.</summary>
        public float BaseYaw => _baseYaw;
        /// <summary>V12 (Quest run 2026-11-11 #18: "Collect stray hallway toys" was unreadable at an angle): the card turns toward me (up to
        /// 70 deg off its table's front) and grows up to 1.5x with distance, so it reads from wherever I stand.</summary>
        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null || _inst == null) return;
            var to = cam.transform.position - transform.position; to.y = 0f; float dist = to.magnitude;
            float want = dist > 0.05f ? Quaternion.LookRotation(-to).eulerAngles.y : _baseYaw;
            float raw = Mathf.DeltaAngle(_baseYaw, want);
            // at the table (< 1 m) or behind it the card keeps its place: turned toward a player that close it stood in front of the job
            float delta = dist < 1.0f || Mathf.Abs(raw) > 110f ? 0f : Mathf.Clamp(raw, -70f, 70f); QaInfo = $"base {_baseYaw:0} want {want:0} delta {delta:0} now {transform.eulerAngles.y:0}";
            var target = Quaternion.Euler(0f, _baseYaw + delta, 0f) * Quaternion.Euler(20f, 0f, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 120f * Time.deltaTime);
            float s = Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(1.2f, 3.5f, dist));
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, s, Time.deltaTime);
        }

        void Update()
        {
            if (_inst == null || Time.time < _next) return; _next = Time.time + 0.25f;
            var sb = new StringBuilder();
            int nextStep = -1;
            for (int i = 0; i < _inst.Def.Steps.Count; i++) if (!_inst.Satisfied[i] && nextStep < 0) nextStep = i;
            for (int i = 0; i < _inst.Def.Steps.Count; i++)
            {
                var s = _inst.Def.Steps[i];
                if (_inst.Satisfied[i]) sb.Append("<color=#5BE38A><s>").Append(i + 1).Append(". ").Append(s.Label).Append("</s></color>\n");
                else if (i == nextStep) sb.Append("<color=#FFD65A>> ").Append(i + 1).Append(". ").Append(s.Label).Append(Hint(s)).Append("</color>\n");
                else sb.Append(i + 1).Append(". ").Append(s.Label).Append('\n');
            }
            bool messed = false; foreach (var r in _rt.Board.Sabotage) if (r.Instance == _inst.Index && !r.Foiled) messed = true;
            Sounds(messed);
            if (_inst.Complete) sb.Append("<color=#5BE38A><b>DONE!</b></color>");
            else if (messed) sb.Append("<color=#FF7769><b>Someone messed with this!</b></color>");
            else if (!string.IsNullOrEmpty(_inst.Def.QuietPath)) sb.Append("<size=80%><i>Quiet: ").Append(_inst.Def.QuietPath).Append("</i></size>");
            var t = sb.ToString();
            if (t != _last) { _list.text = t; _last = t; }
        }

        /// <summary>A short how-to for the hand gesture each step needs (shown on the highlighted step only).</summary>
        static string Hint(RecipeStepDef s)
        {
            switch (s.Kind)
            {
                case StepKind.Strokes: return " <size=80%>(rub it)</size>";
                case StepKind.Stage: return s.Verb == Verb.Fold ? " <size=80%>(hold + rub with other hand)</size>" : " <size=80%>(hold + rub)</size>";
                case StepKind.Paired: case StepKind.Attached: return " <size=80%>(hold them together)</size>";
                case StepKind.OpenState: case StepKind.CloseState: case StepKind.Flag: return " <size=80%>(press)</size>";
                default: return "";
            }
        }
    }
}
