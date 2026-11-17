using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// Mom's in-world presentation, slaved to the authoritative MomDirector. She lives behind her bedroom door, walks in
    /// through it (NavMesh, never teleporting through walls), investigates, delivers the verdict with an evidence card,
    /// then walks back to bed. The player's camera is never moved.
    /// </summary>
    public sealed class MomCharacter : MonoBehaviour
    {
        public Transform Door;            // hinge pivot
        public Light DoorLight;
        public Vector3 OffstagePoint, DoorwayPoint;
        NavMeshAgent _agent;
        Animator _anim;
        Transform _visual, _cardRoot;
        TextMeshPro _subtitle, _cardText;
        TextMeshPro _zzz;
        MomState _state = MomState.Sleeping;
        float _doorOpen, _doorTarget;
        AudioSource _snore;
        string _pendingLineName;
        float _subtitleUntil;
        static readonly int SpeedId = Animator.StringToHash("Speed");

        public void Init(Vector3 offstage, Vector3 doorway, Transform door, Light doorLight, int agentTypeId = 0)
        {
            OffstagePoint = offstage; DoorwayPoint = doorway; Door = door; DoorLight = doorLight;
            transform.position = offstage;
            _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.agentTypeID = agentTypeId;
            if (NavMesh.SamplePosition(offstage, out var spawnHit, 1f, new NavMeshQueryFilter { agentTypeID = agentTypeId, areaMask = NavMesh.AllAreas })) _agent.Warp(spawnHit.position);
            _agent.speed = 1.3f; _agent.radius = 0.25f; _agent.height = 1.75f; _agent.angularSpeed = 360f; _agent.acceleration = 5f;
            // V12 (Quest run: she walked right into the player - the camera ended up inside her): steer around players (PersonalSpace)
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; _agent.avoidancePriority = 10;
            var tag = gameObject.AddComponent<ActorTag>(); tag.ActorId = ActorIds.Mom;
            BuildVisual(); TryGeniesMom();
            _subtitle = WorldText.Make(transform, "subtitle", "", new Vector3(0, 2.15f, 0), 0.75f, Palette.Cream, TextAlignmentOptions.Center, 1.8f);
            _cardRoot = WorldText.Board(transform, "evidenceCard", new Vector3(0, 2.75f, 0), new Vector2(1.5f, 0.5f), Palette.Navy);
            _cardText = WorldText.Make(_cardRoot, "text", "", new Vector3(0, 0, -0.01f), 0.62f, Palette.Yellow, TextAlignmentOptions.Center, 1.4f);
            _cardRoot.gameObject.SetActive(false);
            _zzz = WorldText.Make(door != null ? door.parent : transform, "zzz", "z z Z", new Vector3(0.3f, 2.25f, -0.15f), 1.0f, Palette.Moon, TextAlignmentOptions.Center, 0.8f);
            _visual.gameObject.SetActive(false);
            if (AudioBank.I != null) { _snore = AudioBank.I.Play("mom_snore", doorway + Vector3.up * 1.2f, 0.25f, 0f, AudioBus.Voice); if (_snore) _snore.loop = true; }
        }

        /// <summary>QA: Mom's body is out (not behind her door) / her NavMesh agent.</summary>
        public bool BodyOut => _visual != null && _visual.gameObject.activeSelf;
        public NavMeshAgent Agent => _agent;
        public Vector3 LastTarget { get; private set; }

        AstraRig _rig;
        AvatarHandle _genies; Animator _gAnim; static readonly int GSpeed = Animator.StringToHash("Speed");
        /// <summary>V12 (owner): Mom is one of the five saved Genies looks (always clothed: robe / pyjamas + slippers).</summary>
        void TryGeniesMom()
        {
            if (AvatarService.Backend == null) return;
            AvatarService.Backend.Load(null, 4, transform, h =>
            {
                if (this == null || h == null || h.Root == null) { h?.Release?.Invoke(); return; }
                _genies = h; _gAnim = h.Animator; h.Root.transform.SetParent(_visual, false);
                h.Root.transform.localPosition = Vector3.zero; h.Root.transform.localRotation = Quaternion.identity; h.Root.transform.localScale = Vector3.one * (1.72f / 1.54f);
                foreach (var r in _visual.GetComponentsInChildren<Renderer>(true)) if (!r.transform.IsChildOf(h.Root.transform)) r.enabled = false;   // the authored body stays for its rig, hidden
                Debug.Log("[SHHH][mom] wearing the saved Genies Mom look");
            });
        }

        void BuildVisual()
        {
            // V6: Astra's original Mom (robe, slippers, expressive face) replaces the CC0 stand-in the user rejected.
            var me = CharacterCatalog.I != null ? CharacterCatalog.I.Get("mom") : null;
            if (me != null) { _rig = AstraRig.Create(transform, me, 1.72f); if (_rig != null) { _rig.WalkSpeedForClip = 1.1f; _rig.RunSpeedForClip = 9f; _visual = _rig.transform; return; } }
            var model = ArtCatalog.I.Model("qw_Female_Dress");
            if (model != null)
            {
                var v = VisualFactory.Normalise(model, 1.72f, transform, "momBody", byHeight: true, keepAnimators: true);
                _visual = v.transform;
                foreach (var r in v.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        var n = mats[i].name.ToLowerInvariant();
                        if (n.Contains("dress") || n.Contains("cloth") || n.Contains("shirt")) { var m = new Material(mats[i]); m.SetColor("_BaseColor", Palette.Purple); mats[i] = m; }
                    }
                    r.sharedMaterials = mats;
                }
                _anim = v.GetComponentInChildren<Animator>();
                if (_anim == null) _anim = v.transform.GetChild(0).gameObject.AddComponent<Animator>();
                if (ArtCatalog.I.momAnimator != null) _anim.runtimeAnimatorController = ArtCatalog.I.momAnimator;
                _anim.applyRootMotion = false;
                Transform head = _anim.isHuman ? _anim.GetBoneTransform(HumanBodyBones.Head) : null;
                if (head == null) foreach (var t in v.GetComponentsInChildren<Transform>()) if (t.name == "Head") { head = t; break; }
                if (head != null) AddNightwear(head, true);
            }
            else
            {
                _visual = new GameObject("momProc").transform; _visual.SetParent(transform, false);
                Prim.Shape(PrimitiveType.Capsule, _visual, "robe", new Vector3(0, 0.75f, 0), new Vector3(0.5f, 0.75f, 0.4f), Mats.Lit(Palette.Purple));
                var h = Prim.Shape(PrimitiveType.Sphere, _visual, "head", new Vector3(0, 1.58f, 0), Vector3.one * 0.3f, Mats.Lit(Palette.Cream));
                AddNightwear(h.transform, false);
            }
        }

        void AddNightwear(Transform head, bool boneSpace)
        {
            float k = boneSpace ? 1f / Mathf.Max(0.001f, head.lossyScale.y) : 1f / Mathf.Max(0.001f, head.lossyScale.y);
            var mask = Prim.Box(head, "sleepMask", new Vector3(0, 0.16f * k * 0.5f, 0.05f * k), new Vector3(0.2f, 0.05f, 0.04f) * k, Mats.Lit(Palette.Coral), false);
            for (int i = -1; i <= 1; i++)
            {
                var c = Prim.Shape(PrimitiveType.Cylinder, head, "curler", new Vector3(0.07f * i * k, 0.2f * k, -0.03f * k), new Vector3(0.05f, 0.04f, 0.05f) * k, Mats.Lit(Palette.Teal));
                c.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        }

        // ---- V13 voice director (owner: "a lot of repetition in ... voice overs"). The rules still decide WHAT happens (Core Mom); this
        // only decides which of her reaction lines are voiced: one line at a time, cool-downs per kind of line, and never the same line
        // again soon (another variant is used, or the line is skipped). Lines that tell the player something new always play.
        static readonly string[] AlwaysSay = { "mom_caught", "mom_lazy", "mom_inspect", "mom_question", "mom_penalty", "mom_tiebreak", "mom_win", "mom_intro", "mom_lastminute", "mom_warning" };
        static readonly (string kind, float gap)[] LineGaps = { ("mom_stir", 25f), ("mom_suspicious", 25f), ("mom_almost", 20f), ("mom_investigate", 40f), ("mom_bed", 45f), ("mom_wake", 15f), ("mom_sabotage", 30f), ("mom_rush", 20f) };
        readonly Dictionary<string, float> _kindAt = new Dictionary<string, float>();
        readonly List<string> _recentLines = new List<string>();
        float _speakingUntil; AudioSource _voiceSrc; Coroutine _pendingVoice;
        /// <summary>QA: lines skipped by the director.</summary>
        public static int LinesSkipped;

        static string KindOf(string lineId) { int u = lineId.LastIndexOf('_'); return u > 0 && int.TryParse(lineId.Substring(u + 1), out _) ? lineId.Substring(0, u) : lineId; }

        /// <summary>Returns the line to voice (maybe another variant of the same kind), or null to skip it.</summary>
        string Direct(string lineId)
        {
            string kind = KindOf(lineId); bool always = false;
            foreach (var a in AlwaysSay) if (kind.StartsWith(a)) always = true;
            float now = Time.time;
            if (!always)
            {
                if (now < _speakingUntil) return null;                                     // she is still talking
                foreach (var g in LineGaps) if (kind == g.kind && _kindAt.TryGetValue(kind, out var at) && now - at < g.gap) return null;
            }
            if (_recentLines.Contains(lineId))
            {
                // another variant of the same kind that was not used lately
                for (int n = 1; n <= 6; n++) { string alt = kind + "_" + n; if (alt != lineId && !_recentLines.Contains(alt) && AudioBank.I != null && AudioBank.I.Has("vo_" + alt) && !string.IsNullOrEmpty(MomLines.Text(alt, null))) { lineId = alt; break; } }
                if (_recentLines.Contains(lineId) && !always) return null;
            }
            _kindAt[kind] = now; _recentLines.Add(lineId); if (_recentLines.Count > 8) _recentLines.RemoveAt(0);
            if (always && _voiceSrc != null && _voiceSrc.isPlaying && now < _speakingUntil) _voiceSrc.Stop();   // an important line interrupts a reaction
            if (_pendingVoice != null) { StopCoroutine(_pendingVoice); _pendingVoice = null; }
            return lineId;
        }

        public void Say(string lineId, string name)
        {
            string chosen = Direct(lineId);
            if (chosen == null) { LinesSkipped++; AudioBank.Trace?.Add($"{Time.time:0.00},SKIPSAY:{lineId},,Voice,,,,"); return; }
            lineId = chosen;
            string txt = MomLines.Text(lineId, name);
            if (string.IsNullOrEmpty(txt)) return;
            AudioBank.Trace?.Add($"{Time.time:0.00},SAY:{lineId},,Voice,,,,");
            _subtitle.text = txt; _subtitleUntil = Time.time + Mathf.Clamp(txt.Length * 0.07f, 2f, 6f);
            var pos = _visual.gameObject.activeSelf ? transform.position + Vector3.up * 1.6f : DoorwayPoint + Vector3.up * 1.6f;
            if (AudioBank.I != null)
            {
                // English VO (Piper TTS, public-domain-trained voice). Lines with {name}: a name clip first, then the line.
                string nameKey = name != null ? "vo_name_" + name.ToLowerInvariant() : null;
                bool hasName = nameKey != null && MomLines.HasName(lineId) && AudioBank.I.Has(nameKey);
                // V13: her wake-up line waits for the "dun dun DUNNN" sting instead of talking over it
                float delay = KindOf(lineId) == "mom_wake" ? 0.9f : 0f;
                if (AudioBank.I.Has("vo_" + lineId))
                {
                    if (hasName) { _pendingVoice = StartCoroutine(PlayAfter(nameKey, pos, delay, "vo_" + lineId)); }
                    else if (delay > 0f) _pendingVoice = StartCoroutine(PlayAfter("vo_" + lineId, pos, delay, null));
                    else _voiceSrc = AudioBank.I.Play("vo_" + lineId, pos, 1f, 0f, AudioBus.Voice);
                }
                else _voiceSrc = AudioBank.I.Play("mom_babble", pos, 0.9f, 0.05f, AudioBus.Voice);
                _speakingUntil = Time.time + delay + (_voiceSrc != null && _voiceSrc.clip != null ? _voiceSrc.clip.length : Mathf.Clamp(txt.Length * 0.06f, 1.5f, 5f)) + 0.4f;
            }
            if (!_visual.gameObject.activeSelf) { _subtitle.transform.position = DoorwayPoint + new Vector3(0, 2.3f, -0.4f); }
            // V12 (Quest run: her lines only showed as text upstairs, far away): when I cannot see her, her line also goes on my HUD
            var cam = Camera.main;
            if (!_visual.gameObject.activeSelf || cam == null || Vector3.Distance(cam.transform.position, transform.position) > 3.5f) TaskHud.Caption("Mom: " + txt, Mathf.Clamp(txt.Length * 0.07f, 2.5f, 6f));
        }

        /// <summary>Plays key after delay; then (optionally) the follow-up line right after it (name clip, then the line).</summary>
        System.Collections.IEnumerator PlayAfter(string key, Vector3 pos, float delay, string then)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            _voiceSrc = AudioBank.I?.Play(key, pos, 1f, 0f, AudioBus.Voice);
            if (then != null)
            {
                yield return new WaitForSeconds(_voiceSrc != null && _voiceSrc.clip != null ? _voiceSrc.clip.length + 0.08f : 0.5f);
                _voiceSrc = AudioBank.I?.Play(then, pos, 1f, 0f, AudioBus.Voice);
            }
            if (_voiceSrc != null && _voiceSrc.clip != null) _speakingUntil = Mathf.Max(_speakingUntil, Time.time + _voiceSrc.clip.length + 0.4f);
            _pendingVoice = null;
        }

        public void OnStateChanged(MomState from, MomState to, RoundController round)
        {
            _state = to;
            switch (to)
            {
                case MomState.Waking:
                    RoomFx.Burst("wham", DoorwayPoint + new Vector3(0, 2.0f, -0.5f), 0.4f, 2.5f);
                    _doorTarget = 1f; AudioBank.I?.Play("mom_wake_stinger", DoorwayPoint, 1f, 0f, AudioBus.Sfx);
                    AudioBank.I?.Play("door_creak", DoorwayPoint, 0.8f);
                    _visual.gameObject.SetActive(true);
                    _rig?.Play("wake");
                    if (_agent.isOnNavMesh || _agent.Warp(OffstagePoint)) _agent.SetDestination(DoorwayPoint);
                    break;
                case MomState.WalkingIn:
                {
                    Vector3 target = InvestigationPoint(round); LastTarget = target;
                    _agent.speed = Mathf.Clamp(Vector3.Distance(transform.position, target) / Mathf.Max(0.8f, round.Mom.WalkInDuration * round.Mom.Pace), 0.9f, 3.0f);
                    if (_agent.isOnNavMesh && NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) _agent.SetDestination(hit.position);
                    break;
                }
                case MomState.Investigating: _rig?.Play("scan"); break;
                case MomState.Inspecting: _rig?.Play("inspect"); break;
                case MomState.Interrogating:
                    // V7: she stands, arms crossed, and asks one question; the evidence tray appears in front of each sibling
                    _rig?.Play("admonish");
                    if (round.Interro != null) { _cardText.text = "<color=#FFD65A>Well?</color>\n<size=70%><color=#FFF3DD>" + round.Interro.Question + "</color></size>"; _cardRoot.gameObject.SetActive(true); }
                    break;
                case MomState.Verdict:
                    ShowVerdict(round);
                    _anim?.SetTrigger("Point");
                    if (_rig != null) _rig.Play(round.Verdict != null && round.Verdict.Kind == MomVerdictKind.Eliminate ? "point" : "admonish");
                    break;
                case MomState.ReturningToBed:
                    _cardRoot.gameObject.SetActive(false);
                    _agent.speed = 1.6f;
                    if (_agent.isOnNavMesh) _agent.SetDestination(OffstagePoint);
                    break;
                case MomState.Sleeping:
                    if (from == MomState.ReturningToBed) { _doorTarget = 0f; _visual.gameObject.SetActive(false); AudioBank.I?.Play("door_close_soft", DoorwayPoint, 0.5f); }
                    break;
            }
        }

        Vector3 InvestigationPoint(RoundController round)
        {
            var v = round.Verdict;
            if (v != null && v.DecisiveEventId >= 0)
            {
                var e = round.Ledger.Get(v.DecisiveEventId);
                if (e != null) { var n = VisualFactory.V(e.Position); return StandNear(n, n + (transform.position - n).normalized * 0.8f); }
            }
            var board = GameServices.Room != null ? GameServices.Room.ChoreBoardPos : Vector3.zero;
            return Vector3.Lerp(new Vector3(0, 0, 0.3f), board, 0.5f);
        }

        /// <summary>
        /// V12 (Quest run 2026-11-11: Mom stopped 0.3 m from the player - inside the camera - so she was never seen). Mom stands on the
        /// noise's floor 1.0-1.6 m from it, at least 1.1 m from every player, with a clear line to the spot, preferring a place in front of
        /// my view. Local presentation only (Mom witnesses nothing during the visit); falls back to the old spot.
        /// </summary>
        Vector3 StandNear(Vector3 noise, Vector3 fallback)
        {
            float floorY = noise.y;
            if (NavMesh.SamplePosition(noise, out var nh, 1.6f, NavMesh.AllAreas)) floorY = nh.position.y;
            var cam = Camera.main; Vector3 head = cam != null ? cam.transform.position : noise;
            var fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 best = fallback; float bestScore = float.MinValue;
            foreach (float r in new[] { 1.3f, 1.6f, 1.0f })
                for (int a = 0; a < 16; a++)
                {
                    var dir = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward;
                    var c = new Vector3(noise.x, floorY, noise.z) + dir * r;
                    if (!NavMesh.SamplePosition(c, out var h, 0.3f, NavMesh.AllAreas) || Mathf.Abs(h.position.y - floorY) > 0.3f) continue;
                    var p = h.position;
                    if (PersonalSpace.Nearest(p) < 1.1f) continue;
                    if (Physics.Linecast(p + Vector3.up * 1.5f, noise + Vector3.up * 0.1f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) continue;
                    var toP = p - head; toP.y = 0f; float dist = toP.magnitude;
                    float score = (dist > 0.01f ? Vector3.Dot(fwd, toP / dist) : 0f) * 2f - Mathf.Abs(dist - 1.8f) * 0.6f - Mathf.Abs(r - 1.3f);
                    if (score > bestScore) { bestScore = score; best = p; }
                }
            return best;
        }

        void ShowVerdict(RoundController round)
        {
            var v = round.Verdict; if (v == null) return;
            string name = ActorIds.IsSibling(v.Culprit) ? round.Roster[v.Culprit].DisplayName : "";
            string who = ActorIds.IsSibling(v.Culprit) ? $"{name} {(round.Roster[v.Culprit].IsBot ? "(BOT)" : "")}" : "";
            switch (v.Kind)
            {
                case MomVerdictKind.Eliminate:
                    _cardText.text = (v.Reason == EliminationReason.CaughtMakingNoise ? "<color=#FF7769>CAUGHT!</color>  " : v.Reason == EliminationReason.CaughtSabotaging ? "<color=#FF7769>CAUGHT MESSING!</color>  " : "<color=#FF7769>LEAST HELPFUL</color>  ") + who + "\n<size=70%><color=#FFF3DD>" + v.Evidence + "</color></size>";
                    var target = GameServices.Room != null ? GameServices.Room.ActorTransform(v.Culprit) : null;
                    if (target != null) { var d = target.position - transform.position; d.y = 0; if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); }
                    break;
                case MomVerdictKind.TieBreak:
                    _cardText.text = "<color=#FFD65A>TIE!</color>\n<size=70%>Tie-break challenge!</size>"; break;
                default:
                    _cardText.text = "<color=#65C8C5>Warning</color>\n<size=70%><color=#FFF3DD>" + v.Evidence + "</color></size>"; break;
            }
            _cardRoot.gameObject.SetActive(true);
        }

        void Update()
        {
            if (_gAnim != null && _agent != null && _gAnim.runtimeAnimatorController != null) _gAnim.SetFloat(GSpeed, _agent.velocity.magnitude);
            _doorOpen = Mathf.MoveTowards(_doorOpen, _doorTarget, Time.deltaTime * 1.8f);
            var round = GameServices.Round;
            float shake = 0f;
            if (round != null && _doorTarget == 0f)
            {
                var st = round.Mom.State;
                if (st == MomState.AlmostAwake) shake = Mathf.Sin(Time.time * 30f) * 1.5f;
                else if (st == MomState.Suspicious) shake = Mathf.Sin(Time.time * 18f) * 0.5f;
                if (_zzz) { _zzz.gameObject.SetActive(st <= MomState.Stirring); _zzz.transform.localPosition = new Vector3(0.3f + Mathf.Sin(Time.time) * 0.05f, 2.25f + Mathf.Repeat(Time.time * 0.1f, 0.2f), -0.15f); }
                if (_snore) _snore.volume = (st == MomState.Sleeping ? 0.25f : st == MomState.Stirring ? 0.15f : 0.05f) * AudioBank.BusVolume[(int)AudioBus.Voice];
            }
            if (Door != null) Door.localRotation = Quaternion.Euler(0, -95f * _doorOpen + shake, 0);
            if (DoorLight != null)
            {
                float stageGlow = round != null ? Mathf.InverseLerp(30f, 100f, round.Meter.Value) : 0f;
                DoorLight.intensity = Mathf.Lerp(0.25f + stageGlow * 1.2f, 2.5f, _doorOpen) * (round != null && round.Mom.State == MomState.AlmostAwake ? (0.8f + Mathf.PerlinNoise(Time.time * 8f, 0) * 0.4f) : 1f);
            }
            // V12: once she has stopped (looking, asking, verdict) she turns to face me
            if (_agent != null && _visual != null && _visual.gameObject.activeSelf && _agent.velocity.sqrMagnitude < 0.04f
                && (_state == MomState.Investigating || _state == MomState.Inspecting || _state == MomState.Interrogating || _state == MomState.Verdict) && Camera.main != null)
            {
                var d = Camera.main.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 240f * Time.deltaTime);
            }
            if (_anim != null && _agent != null) _anim.SetFloat(SpeedId, _agent.velocity.magnitude);
            if (_rig != null && _agent != null) _rig.SetSpeed(_agent.velocity.magnitude);
            if (_subtitle != null)
            {
                if (Time.time > _subtitleUntil) _subtitle.text = "";
                WorldText.Billboard(_subtitle.transform);
            }
            if (_cardRoot != null && _cardRoot.gameObject.activeSelf) WorldText.Billboard(_cardRoot);
        }
    }
}
