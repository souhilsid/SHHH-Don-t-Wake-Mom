using System.Collections.Generic;
using System.Linq;
using System.Text;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V7 Gate 5 confrontation, the non-voice path: while Mom questions the siblings, a small tray floats at chest height in front of
    /// the local player with Mom's question, a countdown, the player's OWN witness memories as poke cards ("show Mom"), one
    /// "point at" button per other sibling (weak, no proof) and "nothing to add". Everyone (spectators too) sees the cards shown so
    /// far. Input goes to the authority (journaled) or to the host via <see cref="GameServices.SendConfrontIntent"/>.
    /// </summary>
    public sealed class ConfrontationPanel : MonoBehaviour
    {
        RoundController _round; Transform _head; Interrogation _for; Transform _tray;
        TextMeshPro _title, _shown;
        readonly List<PokeButton> _buttons = new List<PokeButton>();
        float _next; bool _built; int _sndCards, _sndPoints; Interrogation _sndQ;

        public static ConfrontationPanel Ensure(Transform head)
        {
            var go = GameObject.Find("V7_ConfrontationPanel") ?? new GameObject("V7_ConfrontationPanel");
            var p = go.GetComponent<ConfrontationPanel>() ?? go.AddComponent<ConfrontationPanel>(); p._head = head; return p;
        }

        void Update()
        {
            _round = GameServices.Round;
            var q = _round != null && _round.BeliefMode && _round.Mom.State == MomState.Interrogating ? _round.Interro : null;
            if (q == null) { if (_tray) { Destroy(_tray.gameObject); _tray = null; } _for = null; _built = false; return; }
            if (q != _sndQ) { _sndQ = q; _sndCards = 0; _sndPoints = 0; }
            if (q != _for) { _for = q; Build(q); }
            FollowMe();
            if (Time.time < _next) return; _next = Time.time + 0.2f;
            Refresh(q);
        }

        void Build(Interrogation q)
        {
            if (_tray) Destroy(_tray.gameObject);
            _buttons.Clear(); _built = false;
            _tray = new GameObject("tray").transform; _tray.SetParent(transform, false);
            var f = _head != null ? _head.forward : Vector3.forward; f.y = 0; if (f.sqrMagnitude < 1e-3f) f = Vector3.forward; f.Normalize();
            var hp = _head != null ? _head.position : new Vector3(0, 1.2f, 0);
            _tray.position = hp + f * 0.42f + Vector3.down * 0.32f;
            // face the player (button/text fronts are -Z), tipped back so it reads from above
            _tray.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(30f, 0, 0);
            WorldText.Board(_tray, "bg", new Vector3(0, 0, 0.004f), new Vector2(0.44f, 0.3f), Palette.Navy);
            _title = WorldText.Make(_tray, "q", "", new Vector3(0, 0.118f, -0.002f), 0.2f, Palette.Yellow, TextAlignmentOptions.Center, 0.42f);
            _shown = WorldText.Make(_tray, "shown", "", new Vector3(0, -0.122f, -0.002f), 0.13f, Palette.Cream, TextAlignmentOptions.Center, 0.42f);
            int me = GameServices.LocalSlot;
            bool canAct = ActorIds.IsSibling(me) && _round.Roster[me].IsActive;
            if (canAct)
            {
                // my memories: this incident first, then others; at most 3 cards
                var mine = _round.Belief.MemoriesOf(me).Where(m => !m.Presented).GroupBy(m => (m.SeenActor, _round.Belief.Get(m.Incident)?.What)).Select(g => g.OrderByDescending(m => q.Incident != null && m.Incident == q.Incident.Id).ThenByDescending(m => m.Confidence).First()).OrderByDescending(m => q.Incident != null && m.Incident == q.Incident.Id).ThenByDescending(m => m.Confidence).Take(3).ToList();
                for (int i = 0; i < mine.Count; i++)
                {
                    int id = mine[i].Id; string label = "Show: " + Short(mine[i].Card, 38);
                    _buttons.Add(PokeButton.Create(_tray, label, new Vector3(0, 0.07f - i * 0.05f, -0.012f), new Vector2(0.4f, 0.042f), Palette.Mint, () => Send(0, id), 0.13f));
                }
                if (mine.Count == 0) WorldText.Make(_tray, "nomem", "You didn't see it happen.", new Vector3(0, 0.07f, -0.002f), 0.14f, Palette.Cream, TextAlignmentOptions.Center, 0.4f);
                var others = _round.Roster.ActiveIndices().Where(s => s != me).ToList();
                for (int i = 0; i < others.Count; i++)
                {
                    int s = others[i]; float x = (i - (others.Count - 1) * 0.5f) * 0.135f;
                    _buttons.Add(PokeButton.Create(_tray, "Point: " + _round.Roster[s].DisplayName, new Vector3(x, -0.06f, -0.012f), new Vector2(0.125f, 0.038f), DutyVisuals.ColorFor(_round, s), () => Send(1, s), 0.11f));
                }
                _buttons.Add(PokeButton.Create(_tray, "Nothing to add", new Vector3(0, -0.095f, -0.012f), new Vector2(0.2f, 0.03f), Palette.Cream, () => Send(2, 0), 0.11f));
            }
            TaskHud.DrawOnTop(_tray);   // V12: my arms / hands never hide Mom's question or my cards
            _built = true;
        }

        /// <summary>V12 (Quest run: the tray was placed once where I stood when Mom started asking; I had walked on and never saw it):
        /// it glides back in front of me (0.4 s) after a clear turn (> 50 deg) or step (> 0.5 m).</summary>
        Vector3 _fromP, _toP; Quaternion _fromR, _toR; float _glide = 1f;
        void Pose(out Vector3 p, out Quaternion r)
        {
            var f = _head != null ? _head.forward : Vector3.forward; f.y = 0; if (f.sqrMagnitude < 1e-3f) f = Vector3.forward; f.Normalize();
            var hp = _head != null ? _head.position : new Vector3(0, 1.2f, 0);
            p = hp + f * 0.42f + Vector3.down * 0.32f; r = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(30f, 0, 0);
        }
        void FollowMe()
        {
            if (_tray == null || _head == null) return;
            if (_glide >= 1f)
            {
                Pose(out var p, out var r);
                var toTray = _tray.position - _head.position; toTray.y = 0; var f = _head.forward; f.y = 0;
                bool turned = toTray.sqrMagnitude > 1e-3f && f.sqrMagnitude > 1e-3f && Vector3.Angle(f, toTray) > 50f;
                var dp = p - _tray.position; dp.y = 0;
                if (turned || dp.magnitude > 0.5f) { _fromP = _tray.position; _fromR = _tray.rotation; _toP = p; _toR = r; _glide = 0f; }
            }
            if (_glide < 1f)
            {
                _glide = Mathf.Min(1f, _glide + Time.deltaTime / 0.4f); float e = Mathf.SmoothStep(0f, 1f, _glide);
                _tray.SetPositionAndRotation(Vector3.Lerp(_fromP, _toP, e), Quaternion.Slerp(_fromR, _toR, e));
            }
        }

        static string Short(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        void Refresh(Interrogation q)
        {
            int me = GameServices.LocalSlot;
            bool done = !ActorIds.IsSibling(me) || !_round.Roster[me].IsActive || q.Done.Contains(me);
            string phase = q.EvidenceOpen ? $"{Mathf.CeilToInt(q.Remaining)} s" : q.Elapsed < q.Present ? "listen…" : "deciding…";
            _title.text = $"<b>MOM:</b> {q.Question}  <size=75%>({phase})</size>";
            foreach (var b in _buttons) if (b) { b.Interactable = q.EvidenceOpen && !done; b.gameObject.SetActive(!done); }
            if (q.Cards.Count > _sndCards) AudioBank.I?.Play2D("card_show", 0.8f);
            if (q.Accusations.Count > _sndPoints) AudioBank.I?.Play2D("point_at", 0.6f);
            _sndCards = q.Cards.Count; _sndPoints = q.Accusations.Count;
            var sb = new StringBuilder();
            foreach (var (seat, mid) in q.Cards) sb.Append(_round.Roster[seat].DisplayName).Append(" showed: ").Append(_round.Belief.Memories[mid].Card).Append('\n');
            foreach (var (seat, sus) in q.Accusations) sb.Append(_round.Roster[seat].DisplayName).Append(" points at ").Append(_round.Roster[sus].DisplayName).Append(" (no proof)\n");
            if (done && ActorIds.IsSibling(me) && _round.Roster[me].IsActive) sb.Append("<i>Waiting for the others…</i>");
            _shown.text = sb.ToString();
        }

        void Send(int kind, int arg)
        {
            var r = GameServices.Round; int me = GameServices.LocalSlot; if (r == null) return;
            if (GameServices.IsAuthority)
            {
                if (kind == 0) r.PresentCard(me, arg); else if (kind == 1) r.Accuse(me, arg);
                if (kind != 0) r.PassInterrogation(me); // pointing or "nothing" ends your turn; you may show several cards first
            }
            else GameServices.SendConfrontIntent?.Invoke(kind, arg);
            // a shown card disappears; rebuild so the list stays current
            if (kind == 0 && _for != null) { _for = null; }
        }
    }
}
