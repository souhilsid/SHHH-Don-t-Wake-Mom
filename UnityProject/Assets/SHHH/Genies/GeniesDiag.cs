#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Genies.Sdk;
using UnityEngine;

namespace SHHH.Genies
{
    /// <summary>
    /// `genies_diag &lt;tag&gt;` (V8 Gate B): exercises the REAL installed Genies Avatar SDK 3.8.6 public API in play mode and records exactly
    /// what works in this project's current account/auth state: init, login state, anonymous login, default/test/user avatar load,
    /// inventories (hair, colours, features, wearables), equip + colour + body edits, local definition save, destroy + reload by
    /// definition, animator/skeleton facts, and a screenshot. Output: QA_Evidence/genies/&lt;tag&gt;/diag.json + PNGs. No secrets logged.
    /// </summary>
    public sealed class GeniesDiag : MonoBehaviour
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static string Flag => Root + "/Automation/geniesdiag.flag";
        readonly List<string> _rows = new List<string>(); string _dir; Camera _cam; bool _done; float _t0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init() { if (!File.Exists(Flag)) return; var go = new GameObject("QA_GeniesDiag"); DontDestroyOnLoad(go); go.AddComponent<GeniesDiag>(); }

        void Row(string step, bool ok, string detail) { _rows.Add($"{{\"step\":\"{Esc(step)}\",\"pass\":{(ok ? "true" : "false")},\"detail\":\"{Esc(detail)}\"}}"); Debug.Log($"[SHHH][genies_diag] {(ok ? "PASS" : "FAIL")} {step}: {detail}"); }
        static string Esc(string s) => (s ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");

        void Start()
        {
            _t0 = Time.realtimeSinceStartup;
            var tag = File.ReadAllText(Flag).Trim(); if (string.IsNullOrEmpty(tag)) tag = "diag";
            _dir = Path.Combine(Root, "QA_Evidence", "genies", tag); Directory.CreateDirectory(_dir);
            RunAsync().Forget();
        }
        void Update() { if (!_done && Time.realtimeSinceStartup - _t0 > 240f) { Row("timeout", false, "240 s budget"); Finish(); } }

        async UniTaskVoid RunAsync()
        {
            ManagedAvatar av = null;
            try
            {
                // Isolated stage: own camera + light, far below the house so nothing else is in frame
                var stage = new GameObject("GeniesStage"); stage.transform.position = new Vector3(0, -50, 0);
                var camGo = new GameObject("GeniesCam"); camGo.transform.SetParent(stage.transform, false); _cam = camGo.AddComponent<Camera>();
                _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0.16f, 0.1f, 0.24f); _cam.fieldOfView = 30f;
                camGo.transform.localPosition = new Vector3(0, 1.0f, 3.6f); camGo.transform.localRotation = Quaternion.Euler(4, 180, 0);
                var lg = new GameObject("GeniesKey").AddComponent<Light>(); lg.type = LightType.Directional; lg.intensity = 1.1f; lg.transform.rotation = Quaternion.Euler(35, 160, 0);

                bool init = await AvatarSdk.InitializeAsync();
                Row("initialize", init, "AvatarSdk.InitializeAsync() -> " + init);
                Row("login_state", true, $"IsLoggedIn={AvatarSdk.IsLoggedIn} IsLoggedInAnonymously={AvatarSdk.IsLoggedInAnonymously}");
                if (!AvatarSdk.IsLoggedIn)
                {
                    var inst = await AvatarSdk.TryInstantLoginAsync();
                    Row("instant_login", inst.isLoggedIn, "TryInstantLoginAsync -> " + inst.isLoggedIn);
                }
                if (!AvatarSdk.IsLoggedIn)
                {
                    var anon = await AvatarSdk.StartLoginAnonymousAsync();
                    Row("anonymous_login", anon.succeeded, "StartLoginAnonymousAsync() -> " + anon.succeeded + (anon.succeeded ? "" : " reason: " + anon.failReason));
                }
                // load: user (falls back to default when not logged in) then test avatar if needed
                av = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.User { AvatarName = "GeniesDiagAvatar", Parent = stage.transform, ShowLoadingSilhouette = false });
                Row("load_user_or_default", av != null, av != null ? "ManagedAvatar ok" : "null");
                if (av == null)
                {
                    av = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Default { AvatarName = "GeniesDiagAvatar", Parent = stage.transform, ShowLoadingSilhouette = false });
                    Row("load_default", av != null, av != null ? "ManagedAvatar ok" : "null");
                }
                if (av == null)
                {
                    av = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Test { AvatarName = "GeniesDiagAvatar", Parent = stage.transform, ShowLoadingSilhouette = false });
                    Row("load_test", av != null, av != null ? "ManagedAvatar ok" : "null");
                }
                if (av == null) { Finish(); return; }
                float w = 0; while (!av.IsLoadingComplete && w < 30f) { await UniTask.Delay(200); w += 0.2f; }
                Row("definition_loaded", av.IsLoadingComplete, $"after {w:0.0}s");
                Facts(av, "initial");
                await UniTask.Delay(500); Shot("01_loaded");

                // inventories
                var hair = await AvatarSdk.GetDefaultHairAssets(HairType.Hair); Row("inv_hair", hair != null && hair.Count > 0, "default hair: " + (hair?.Count ?? -1));
                var skins = await AvatarSdk.GetDefaultColorsAsync(ColorType.Skin); Row("inv_skin_colors", skins != null && skins.Count > 0, "skin colours: " + (skins?.Count ?? -1));
                var hairCols = await AvatarSdk.GetDefaultColorsAsync(ColorType.Hair); Row("inv_hair_colors", hairCols != null && hairCols.Count > 0, "hair colours: " + (hairCols?.Count ?? -1));
                var eyes = await AvatarSdk.GetDefaultColorsAsync(ColorType.Eyes); Row("inv_eye_colors", eyes != null && eyes.Count > 0, "eye colours: " + (eyes?.Count ?? -1));
                var feats = await AvatarSdk.GetDefaultAvatarFeaturesByCategory(AvatarFeatureCategory.All); Row("inv_features", feats != null && feats.Count > 0, "face features: " + (feats?.Count ?? -1));
                var sb = new StringBuilder(); int totalW = 0; WearableAssetInfo firstTop = null, firstPants = null;
                foreach (WearablesCategory c in Enum.GetValues(typeof(WearablesCategory)))
                {
                    if (c == WearablesCategory.All) continue;
                    var l = await AvatarSdk.GetDefaultWearablesByCategoryAsync(c);
                    int n = l?.Count ?? -1; sb.Append(c).Append('=').Append(n).Append(' '); if (n > 0) totalW += n;
                    if (c == WearablesCategory.Hoodie && n > 0) firstTop = l[0]; if (c == WearablesCategory.Pants && n > 0) firstPants = l[0];
                    if (n > 0 && c == WearablesCategory.Shirt && firstTop == null) firstTop = l[0];
                }
                Row("inv_wearables_default", totalW > 0, sb.ToString().Trim());
                var uw = await AvatarSdk.GetUserWearablesByCategoryAsync(UserWearablesCategory.All); Row("inv_wearables_user", uw != null, "user wearables: " + (uw?.Count ?? -1));

                // edits
                string before = av.GetDefinition() ?? "";
                if (hair != null && hair.Count > 1) { try { await AvatarSdk.EquipHairAsync(av, hair[1]); Row("equip_hair", true, hair[1].Name + " / " + hair[1].AssetId); } catch (Exception e) { Row("equip_hair", false, e.Message); } }
                if (firstTop != null) { try { await AvatarSdk.EquipWearableAsync(av, firstTop); Row("equip_top", av.IsAssetEquipped(firstTop.AssetId), firstTop.Name + " / " + firstTop.Category); } catch (Exception e) { Row("equip_top", false, e.Message); } }
                if (firstPants != null) { try { await AvatarSdk.EquipWearableAsync(av, firstPants); Row("equip_pants", av.IsAssetEquipped(firstPants.AssetId), firstPants.Name); } catch (Exception e) { Row("equip_pants", false, e.Message); } }
                if (skins != null && skins.Count > 2) { bool ok = await AvatarSdk.SetColorAsync(av, skins[2]); Row("set_skin_color", ok, "preset #2"); }
                if (hairCols != null && hairCols.Count > 3) { bool ok = await AvatarSdk.SetColorAsync(av, hairCols[3]); Row("set_hair_color", ok, "preset #3"); }
                try { await AvatarSdk.SetAvatarBodyTypeAsync(av, GenderType.Androgynous, BodySize.Medium); Row("body_type", true, "Androgynous/Medium"); } catch (Exception e) { Row("body_type", false, e.Message); }
                bool st = await AvatarSdk.ModifyAvatarFeatureStatAsync(av, AvatarFeatureStat.Body_ShoulderBroadness, 0.3f); Row("feature_stat", st, "Body_ShoulderBroadness=0.3");
                string after = av.GetDefinition() ?? "";
                Row("definition_changed", after.Length > 0 && after != before, $"definition {before.Length} -> {after.Length} chars; equipped {av.GetEquippedAssetIds().Count}");
                await UniTask.Delay(800); Shot("02_edited");

                // persistence: local save -> destroy -> reload (local profile) -> reload (definition string)
                try { await AvatarSdk.SaveAvatarDefinitionLocallyAsync(av, "shhh_diag"); Row("save_local", true, "profile shhh_diag"); } catch (Exception e) { Row("save_local", false, e.Message); }
                av.Dispose(); av = null; await UniTask.Delay(300);
                var re = await AvatarSdk.LoadFromLocalAvatarDefinitionAsync("shhh_diag", false);
                Row("reload_local_profile", re != null, re != null ? "equipped " + re.GetEquippedAssetIds().Count : "null");
                if (re != null) { re.Root.transform.SetParent(stage.transform, false); re.Dispose(); }
                var rd = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.ByDefinition { DefinitionToLoad = after, AvatarName = "GeniesDiagReload", Parent = stage.transform, ShowLoadingSilhouette = false });
                Row("reload_by_definition", rd != null, rd != null ? "ok" : "null");
                if (rd != null) { w = 0; while (!rd.IsLoadingComplete && w < 30f) { await UniTask.Delay(200); w += 0.2f; } Facts(rd, "reloaded"); await UniTask.Delay(600); Shot("03_reloaded"); av = rd; }
            }
            catch (Exception e) { Row("exception", false, e.GetType().Name + ": " + e.Message); }
            Finish();
        }

        void Facts(ManagedAvatar av, string label)
        {
            var root = av.Root; if (root == null) { Row("facts_" + label, false, "no root"); return; }
            var smr = root.GetComponentsInChildren<SkinnedMeshRenderer>(true); int verts = 0, bones = 0, blend = 0, mats = 0;
            foreach (var r in smr) { if (r.sharedMesh) { verts += r.sharedMesh.vertexCount; blend += r.sharedMesh.blendShapeCount; } bones = Mathf.Max(bones, r.bones.Length); mats += r.sharedMaterials.Length; }
            var an = av.Animator; var b = new Bounds(root.transform.position, Vector3.zero); foreach (var r in smr) b.Encapsulate(r.bounds);
            string human = an != null && an.avatar != null ? (an.avatar.isHuman ? "humanoid" : "generic") : "none";
            string head = an != null && an.isHuman ? (an.GetBoneTransform(HumanBodyBones.Head) != null ? "head ok" : "no head") : "-";
            Row("facts_" + label, smr.Length > 0, $"skinned={smr.Length} verts={verts} maxBones={bones} blendshapes={blend} materials={mats} height={b.size.y:0.00}m animator={(an != null)} avatar={human} {head} skeletonRoot={(av.SkeletonRoot != null ? av.SkeletonRoot.name : "null")}");
        }

        void Shot(string name)
        {
            if (_cam == null) return;
            var rt = new RenderTexture(900, 1100, 24) { antiAliasing = 4 }; _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(900, 1100, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 900, 1100), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG()); _cam.targetTexture = null; RenderTexture.active = null; Destroy(tex); rt.Release();
        }

        void Finish()
        {
            if (_done) return; _done = true;
            File.WriteAllText(Path.Combine(_dir, "diag.json"), "{\n \"sdk\": \"Genies Avatar SDK 3.8.6 (Packages/com.genies.avatar-sdk.client)\",\n \"environment\": \"Unity Editor play mode on the Windows PC (no Quest)\",\n \"utc\": \"" + DateTime.UtcNow.ToString("O") + "\",\n \"rows\": [\n  " + string.Join(",\n  ", _rows) + "\n ]\n}\n");
            try { File.Delete(Flag); } catch { }
            Debug.Log("[SHHH][genies_diag] done -> " + _dir);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
