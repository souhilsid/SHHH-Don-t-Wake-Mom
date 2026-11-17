using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Live lobby board: room code, region/status, the four seats (humans / bots / ready), Ready + Start.</summary>
    public sealed class LobbyBoard : MonoBehaviour
    {
        public TextMeshPro Code, StatusText;
        public readonly TextMeshPro[] Seats = new TextMeshPro[4];
        public PokeButton Ready, Start;
        float _t;

        void Update()
        {
            _t -= Time.unscaledDeltaTime; if (_t > 0f) return; _t = 0.25f;
            var net = GameServices.Net; if (net == null) return;
            Code.text = string.IsNullOrEmpty(net.RoomCode) ? "Connecting..." : $"ROOM  <b>{net.RoomCode}</b>";
            StatusText.text = net.Status + (string.IsNullOrEmpty(net.Region) ? "" : $"  -  region {net.Region}");
            var seats = net.Seats;
            for (int i = 0; i < 4; i++)
            {
                if (Seats[i] == null) continue;
                if (seats == null || i >= seats.Count) { Seats[i].text = $"{i + 1}.  ..."; continue; }
                var s = seats[i];
                var col = "#" + ColorUtility.ToHtmlStringRGB(Palette.Siblings[((s.Cosmetic % 4) + 4) % 4]);
                Seats[i].text = s.Human
                    ? $"{i + 1}.  <color={col}>{s.Name}</color>{(s.Local ? "  <color=#FFD65A>(you)</color>" : "")}   {(s.Connected ? (s.Ready ? "<color=#65C8C5>READY</color>" : "<color=#9FB0D8>not ready</color>") : "<color=#FF7769>reconnecting...</color>")}"
                    : $"{i + 1}.  <color=#9FB0D8>bot sibling (fills the seat)</color>";
            }
            if (Ready != null) Ready.SetLabel(net.LocalReady ? "NOT READY" : "I'M READY");
            if (Start != null)
            {
                Start.gameObject.SetActive(net.IsHost);
                Start.SetColor(net.CanStart ? Palette.Yellow : Palette.Moon);
                Start.SetLabel(net.CanStart ? "START" : "waiting...");
            }
        }
    }
}
