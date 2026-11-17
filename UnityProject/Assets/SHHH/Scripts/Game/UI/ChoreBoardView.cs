using System.Text;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// The in-room chore board (a physical pin board on the wall): countdown/clock, shared chores with progress, your
    /// private bonus, and everyone's helpfulness bars so the inspection rule is transparent. English only.
    /// </summary>
    public sealed class ChoreBoardView : MonoBehaviour
    {
        TextMeshPro _title, _clock, _list, _bonus, _scores, _big;
        float _next;

        public void Build(string roomTitle)
        {
            var b = WorldText.Board(transform, "board", Vector3.zero, new Vector2(1.25f, 1.0f), Palette.Navy);
            _title = WorldText.Make(b, "title", "YOUR JOB", new Vector3(-0.08f, 0.42f, -0.01f), 0.72f, Palette.Yellow, TextAlignmentOptions.Left, 1.0f);
            _clock = WorldText.Make(b, "clock", "", new Vector3(0.48f, 0.42f, -0.01f), 0.8f, Palette.Cream, TextAlignmentOptions.Right, 0.4f);
            // V6: fixed text boxes that shrink to fit (the recording/screens showed the rivals list overprinting YOUR JOB)
            _list = WorldText.Make(b, "list", "", new Vector3(0f, -0.11f, -0.01f), 0.4f, Palette.Cream, TextAlignmentOptions.TopLeft, 1.15f);
            _list.rectTransform.sizeDelta = new Vector2(1.15f, 0.46f);
            Fit(_list);
            _bonus = WorldText.Make(b, "bonus", "", new Vector3(0f, 0.2f, -0.01f), 0.52f, Palette.Cream, TextAlignmentOptions.Left, 1.15f);
            _bonus.rectTransform.sizeDelta = new Vector2(1.15f, 0.13f);
            Fit(_bonus);
            _scores = WorldText.Make(b, "scores", "", new Vector3(0f, -0.42f, -0.01f), 0.38f, Palette.Teal, TextAlignmentOptions.Left, 1.15f);
            _big = WorldText.Make(transform, "big", "", new Vector3(0, 0, -0.08f), 3.2f, Palette.Yellow, TextAlignmentOptions.Center, 1.2f);
            WorldText.Make(b, "room", roomTitle, new Vector3(-0.08f, 0.33f, -0.01f), 0.36f, Palette.Teal, TextAlignmentOptions.Left, 1.0f);
        }

        static void Fit(TextMeshPro t)
        {
            t.enableAutoSizing = true; t.fontSizeMax = t.fontSize; t.fontSizeMin = t.fontSize * 0.5f;
            t.overflowMode = TextOverflowModes.Truncate; t.enableWordWrapping = true;
        }

        static string Pips(ChoreInstance c)
        {
            // ASCII only: the baked Quicksand SDF atlas has no geometric glyphs.
            return $"{c.DoneSteps}/{c.Steps.Count}";
        }

        void Update()
        {
            if (Time.time < _next) return; _next = Time.time + 0.2f;
            var r = GameServices.Round; if (r == null) return;
            if (r.Phase == RoundPhase.Countdown) { _big.text = Mathf.CeilToInt(r.Countdown).ToString(); }
            else if (r.Phase == RoundPhase.Playing && r.PlayTime < 1.2f) _big.text = "GO!";
            else _big.text = "";
            int t = Mathf.CeilToInt(r.TimeRemaining);
            _clock.text = $"{t / 60}:{t % 60:00}";
            _clock.color = t <= 20 ? Palette.Coral : Palette.Cream;
            // V5 layout: YOUR JOB first (big, your colour, item pips), then rivals' jobs (so sabotage targets are obvious),
            // then the small optional house chores. Few words, lots of symbols.
            int me = GameServices.LocalSlot;
            var mine = ActorIds.IsSibling(me) ? r.Board.BonusFor(me) : null;
            _title.text = "YOUR JOB";
            if (mine != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(DutyVisuals.ColorFor(r, me));
                _bonus.text = $"<size=140%><color=#{hex}>{Pips(mine)}</color></size>  {mine.Title}" + (mine.State == ChoreState.Completed ? "  <color=#5BE38A>DONE - guard it!</color>" : "");
            }
            else _bonus.text = r.Roster[me].IsActive ? "" : "<color=#65C8C5>Spectating - cheer them on!</color>";
            var sb = new StringBuilder();
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                if (i == me || !r.Roster[i].IsActive) continue;
                var d = r.Board.BonusFor(i); if (d == null) continue;
                string hex = ColorUtility.ToHtmlStringRGB(DutyVisuals.ColorFor(r, i));
                sb.Append($"<color=#{hex}>{Pips(d)}</color> {r.Roster[i].DisplayName}{(r.Roster[i].IsBot ? " <size=70%>BOT</size>" : "")}: {d.Title}\n");
            }
            sb.Append("<size=80%><color=#65C8C5>House chores (anyone):</color> ");
            foreach (var c in r.Board.Chores) if (!c.IsBonus) sb.Append(c.State == ChoreState.Completed ? "<color=#5BE38A>v</color> " : "o ").Append(c.Title).Append("  ");
            sb.Append("</size>");
            _list.text = sb.ToString();
            var s2 = new StringBuilder("Score  ");
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                var slot = r.Roster[i];
                if (!slot.IsActive) continue;
                s2.Append(i == me ? "<color=#FFD65A>" : "<color=#65C8C5>").Append(slot.DisplayName).Append(' ').Append(Mathf.RoundToInt(r.Scores[i].Total)).Append("</color>   ");
            }
            _scores.text = s2.ToString();
        }
    }
}
