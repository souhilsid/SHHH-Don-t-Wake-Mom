using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// V8 personal house map (opened with MAP on the wrist; floats above the wrist, never head-locked). Built from the same data the
    /// rules use (HouseLayout offsets + RoomCatalog sizes), drawn as raised room tiles on map paper with room stickers. Shows ONLY what
    /// is fair for you: where you are and which way you face, your own jobs (pins: to do / done / messed with), the walking route to
    /// your next unfinished job (real NavMesh path), Mom's door and her state, and a ripple at the latest noise Mom heard.
    /// Rivals' positions are not shown (fairness).
    /// </summary>
    public sealed class HouseMapView : MonoBehaviour
    {
        const float MapW = 0.34f, MapH = 0.27f;
        float _scale; Vector2 _min; Transform _content, _me, _route, _pins, _noise; Renderer _momPin;
        TextMeshPro _title;
        readonly List<GameObject> _routeDots = new List<GameObject>();
        float _nextRoute;
        int _lastPinHash = -1;

        public static HouseMapView Build(Transform parent, Vector3 localPos)
        {
            var g = new GameObject("HouseMap"); g.transform.SetParent(parent, false); g.transform.localPosition = localPos;
            var v = g.AddComponent<HouseMapView>(); v.Make(); return v;
        }

        void Make()
        {
            UiKit.Panel(transform, "bg", Vector3.zero, new Vector2(MapW + 0.03f, MapH + 0.05f), "HOUSE MAP");
            UiKit.Quad(transform, "paper", UiKit.Opaque(UiKit.Tex("map_paper")), new Vector3(0, -0.01f, -0.001f), new Vector2(MapW, MapH));
            _content = new GameObject("content").transform; _content.SetParent(transform, false); _content.localPosition = new Vector3(0, -0.01f, -0.002f);
            // fit all built rooms
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var r in HouseLayout.Rooms)
            {
                var d = RoomCatalog.Get(r); var o = HouseLayout.Offset(r);
                x0 = Mathf.Min(x0, o.x - d.Width / 2f); x1 = Mathf.Max(x1, o.x + d.Width / 2f); z0 = Mathf.Min(z0, o.z - d.Depth / 2f); z1 = Mathf.Max(z1, o.z + d.Depth / 2f);
            }
            float pad = 0.5f;
            _scale = Mathf.Min((MapW - 0.02f) / (x1 - x0 + 2 * pad), (MapH - 0.02f) / (z1 - z0 + 2 * pad));
            _min = new Vector2((x0 + x1) / 2f, (z0 + z1) / 2f);
            var tileCols = new Dictionary<RoomId, Color> { { RoomId.LivingRoom, Palette.Hex(0xC9A7E8) }, { RoomId.Kitchen, Palette.Hex(0x9ED9D6) }, { RoomId.Laundry, Palette.Hex(0xF5C08A) } };
            foreach (var r in HouseLayout.Rooms)
            {
                var d = RoomCatalog.Get(r); var o = HouseLayout.Offset(r);
                var c = P(new Vec3(o.x, 0, o.z));
                UiKit.SlabObject(_content, "room_" + r, new Vector3(c.x, c.y, 0f), d.Width * _scale - 0.004f, d.Depth * _scale - 0.004f, 0.006f, 0.004f, 0.0015f, Mats.Lit(tileCols.TryGetValue(r, out var col) ? col : UiKit.MoonCream, 0.2f));
                UiKit.Icon(_content, WristMenu.RoomIcon(r).Substring(5), new Vector3(c.x, c.y + 0.012f, -0.004f), 0.03f);
                UiKit.Text(_content, HouseLayout.PlaceName(r).ToUpperInvariant(), new Vector3(c.x, c.y - 0.016f, -0.004f), 0.11f, UiKit.Deep, UiKit.Display, d.Width * _scale, TextAlignmentOptions.Center, 0f);
            }
            // Mom's door (living room north wall)
            var momGo = UiKit.Icon(_content, "mom", (Vector3)P(new Vec3(0, 0, 3.05f)) + new Vector3(0, 0, -0.006f), 0.026f);
            _momPin = momGo.GetComponent<Renderer>();
            _pins = new GameObject("pins").transform; _pins.SetParent(_content, false);
            _route = new GameObject("route").transform; _route.SetParent(_content, false);
            _noise = UiKit.SlabObject(_content, "noise", new Vector3(0, 0, -0.003f), 0.03f, 0.03f, 0.015f, 0.002f, 0.001f, Mats.Unlit(UiKit.Coral)).transform; _noise.gameObject.SetActive(false);
            // me: an arrow (triangle) in my sibling colour
            _me = new GameObject("me").transform; _me.SetParent(_content, false);
            var tri = new GameObject("arrow"); tri.transform.SetParent(_me, false); tri.transform.localPosition = new Vector3(0, 0, -0.008f); tri.layer = Layers.UI;
            var m = new Mesh { name = "map_arrow" };
            m.vertices = new[] { new Vector3(0, 0.012f, 0), new Vector3(0.008f, -0.008f, 0), new Vector3(0, -0.004f, 0), new Vector3(-0.008f, -0.008f, 0) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 }; m.RecalculateNormals(); m.RecalculateBounds();
            tri.AddComponent<MeshFilter>().sharedMesh = m;
            tri.AddComponent<MeshRenderer>().sharedMaterial = Mats.Unlit(Palette.Siblings[((Settings.Current.Cosmetic % 4) + 4) % 4]);
            UiKit.SlabObject(_me, "ring", new Vector3(0, 0, -0.006f), 0.022f, 0.022f, 0.011f, 0.002f, 0.0008f, Mats.Unlit(UiKit.Deep));
            _title = UiKit.Text(transform, "", new Vector3(0, -MapH / 2f - 0.012f, -0.012f), 0.13f, UiKit.MoonCream, UiKit.Body, MapW, TextAlignmentOptions.Center, 0.1f);
        }

        /// <summary>House point (metres) -> map-local point (metres on the board). North (+z) is up.</summary>
        Vector2 P(Vec3 w) => new Vector2((w.x - _min.x) * _scale, (w.z - _min.y) * _scale);
        Vector2 P(Vector3 w) => new Vector2((w.x - _min.x) * _scale, (w.z - _min.y) * _scale);

        void LateUpdate()
        {
            var game = GameRoot.I; var head = game != null && game.Rig != null ? game.Rig.Head : null;
            if (head != null)
            {
                var p = P(head.position); _me.localPosition = new Vector3(p.x, p.y, 0);
                var f = head.forward; f.y = 0; float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                _me.localRotation = Quaternion.Euler(0, 0, -yaw);
            }
            var r = GameServices.Round; if (r == null) { _title.text = "Jobs appear here when the match starts"; return; }
            int me = GameServices.LocalSlot;
            // pins for my jobs (rebuilt when their state changes)
            int hash = 17; RecipeInstance next = null;
            if (r.Recipes != null) foreach (var inst in r.Recipes.Instances) if (inst.Owner == me) { hash = hash * 31 + inst.Index * 7 + (inst.Complete ? 1 : 0) + Messed(r, inst) * 3; if (!inst.Complete && next == null) next = inst; }
            if (hash != _lastPinHash)
            {
                _lastPinHash = hash;
                foreach (Transform c in _pins) Destroy(c.gameObject);
                if (r.Recipes != null)
                    foreach (var inst in r.Recipes.Instances)
                    {
                        if (inst.Owner != me) continue;
                        var q = P(WristMenu.FixturePos(r, inst.Index));
                        string icon = inst.Complete ? "check" : Messed(r, inst) > 0 ? "sabotage" : "star";
                        UiKit.Icon(_pins, icon, new Vector3(q.x, q.y, -0.007f), inst.Complete ? 0.022f : 0.028f);
                    }
            }
            // route to my next unfinished job (NavMesh), refreshed twice a second
            if (Time.time >= _nextRoute && head != null)
            {
                _nextRoute = Time.time + 0.5f;
                foreach (var d in _routeDots) d.SetActive(false);
                if (next != null)
                {
                    var goal = WristMenu.FixturePos(r, next.Index); var start = new Vector3(head.position.x, 0, head.position.z);
                    var path = new NavMeshPath();
                    if (NavMesh.SamplePosition(start, out var a, 1.5f, NavMesh.AllAreas) && NavMesh.SamplePosition(new Vector3(goal.x, 0, goal.z), out var b, 1.5f, NavMesh.AllAreas) && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.corners.Length > 1)
                    {
                        int k = 0;
                        for (int i = 0; i + 1 < path.corners.Length; i++)
                        {
                            float len = Vector3.Distance(path.corners[i], path.corners[i + 1]);
                            for (float s = 0; s < len; s += 0.45f)
                            {
                                var w = Vector3.Lerp(path.corners[i], path.corners[i + 1], s / len); var q = P(w);
                                if (k >= _routeDots.Count) _routeDots.Add(UiKit.SlabObject(_route, "dot", Vector3.zero, 0.006f, 0.006f, 0.003f, 0.0015f, 0.0006f, Mats.Unlit(UiKit.Pink)));
                                var dot = _routeDots[k++]; dot.SetActive(true); dot.transform.localPosition = new Vector3(q.x, q.y, -0.005f);
                            }
                        }
                    }
                    _title.text = $"Next: {next.Def.DisplayName} ({HouseLayout.PlaceAt(WristMenu.FixturePos(r, next.Index))})";
                }
                else _title.text = "All your jobs are done - guard them!";
            }
            // Mom state + last heard noise
            bool awake = r.Mom.State != MomState.Sleeping;
            _momPin.sharedMaterial = UiKit.Cutout(UiKit.Tex(awake ? "icon_alert" : "icon_mom"));
            Incident last = null; var incs = r.Belief.Incidents; if (incs.Count > 0) last = incs[incs.Count - 1];
            bool fresh = last != null && r.Time - last.Time < 6f;
            if (_noise.gameObject.activeSelf != fresh) _noise.gameObject.SetActive(fresh);
            if (fresh) { var q = P(last.Position); float age = (r.Time - last.Time) / 6f; _noise.localPosition = new Vector3(q.x, q.y, -0.003f); _noise.localScale = Vector3.one * (0.6f + age * 1.6f); }
        }

        static int Messed(RoundController r, RecipeInstance inst) { foreach (var s in r.Recipes.Sabotage) if (s.Instance == inst.Index && !s.Foiled && !inst.Complete) return 1; return 0; }
    }
}
