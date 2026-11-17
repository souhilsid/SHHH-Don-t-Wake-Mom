using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V8 single end-of-match podium (one results screen per match): four toy blocks by final rank (1st in the middle, highest),
    /// each sibling standing on theirs in their saved outfit, a crown over the winner (who cheers), a name/points plate per block,
    /// and confetti. Built in front of the player in the house where the match ended; the results board sits in front of it.
    /// </summary>
    public static class ResultsPodium
    {
        static GameObject _root;

        public static void Build(MatchController m, Vector3 floorPos, float yaw)
        {
            Clear();
            var sum = m.Summary; if (sum == null) return;
            _root = new GameObject("ResultsPodium");
            if (GameServices.Room != null) _root.transform.SetParent(GameServices.Room.transform, true);
            _root.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0, yaw, 0));
            var order = sum.Ranking != null && sum.Ranking.Count > 0 ? sum.Ranking : new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            float[] x = { 0f, -0.62f, 0.62f, 1.18f }; float[] h = { 0.42f, 0.3f, 0.2f, 0.1f };
            Color[] block = { UiKit.Hush, UiKit.MoonCream, UiKit.Coral, UiKit.Purple };
            string[] place = { "1ST", "2ND", "3RD", "4TH" };
            for (int k = 0; k < order.Count && k < 4; k++)
            {
                int slot = order[k]; var seat = m.Roster[slot];
                var b = new GameObject("place" + (k + 1)).transform; b.SetParent(_root.transform, false); b.localPosition = new Vector3(x[k], 0, 0);
                // toy block with a bevel (front faces the player = local -Z)
                var blk = UiKit.SlabObject(b, "block", new Vector3(0, h[k] / 2f, 0), 0.52f, h[k], 0.06f, 0.5f, 0.03f, Mats.Lit(block[k], 0.35f));
                blk.transform.localRotation = Quaternion.Euler(0, 0, 0);
                var plate = UiKit.Text(b, place[k], new Vector3(0, h[k] * 0.55f, -0.255f), 0.9f, UiKit.Deep, UiKit.Display, 0.5f, TextAlignmentOptions.Center, 0.18f);
                plate.outlineColor = new Color32(255, 248, 232, 255);
                float pts = sum.FinalScore != null && slot < sum.FinalScore.Length ? sum.FinalScore[slot] : 0f;
                UiKit.Text(b, $"{seat.DisplayName}{(seat.IsBot ? " <size=60%>BOT</size>" : "")}\n<size=75%>{pts:0} pts</size>", new Vector3(0, h[k] + 1.42f, 0), 0.62f, k == 0 ? UiKit.Hush : UiKit.SoftWhite, UiKit.Display, 0.7f);
                var holder = new GameObject("sib").transform; holder.SetParent(b, false); holder.localPosition = new Vector3(0, h[k], 0); holder.localRotation = Quaternion.Euler(0, 180, 0);
                var av = holder.gameObject.AddComponent<SiblingAvatar>();
                av.Build(slot, seat.DisplayName, seat.IsBot, seat.CosmeticId);
                var np = av.transform.Find("nameplate"); if (np) np.gameObject.SetActive(false);
                if (k == 0) { av.Trigger("Cheer"); UiKit.Icon(b, "crown", new Vector3(0, h[k] + 1.25f, -0.02f), 0.26f).transform.localRotation = Quaternion.Euler(0, 0, 0); }
                else if (k == 3) av.Trigger("Sad");
            }
            RoomFx.Burst("stars", floorPos + Vector3.up * 1.6f, 0.6f, 4f);
            RoomFx.Burst("sparks", floorPos + Vector3.up * 1.2f, 0.5f, 3f);
        }

        public static void Clear() { if (_root != null) Object.Destroy(_root); _root = null; }
    }
}
