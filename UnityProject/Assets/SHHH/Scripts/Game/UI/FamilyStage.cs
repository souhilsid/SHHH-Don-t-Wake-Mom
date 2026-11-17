using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V7 Gate 8 physical Family Stage in the front hall: the four siblings stand on a little rug stage next to the main board,
    /// wearing the player's saved wardrobe. Poke "Play as …" under a sibling to pick them (sets your colour / character); the picked
    /// one cheers and gets a spotlight ring. The wardrobe board rebuilds the stage so changes show immediately.
    /// </summary>
    public sealed class FamilyStage : MonoBehaviour
    {
        public static FamilyStage I { get; private set; }
        readonly List<SiblingAvatar> _sibs = new List<SiblingAvatar>();
        readonly List<GameObject> _rings = new List<GameObject>();
        readonly List<PokeButton> _buttons = new List<PokeButton>();
        static readonly string[] Names = { "Pip", "Nell", "Rory", "Tess" };

        public static FamilyStage Build(Transform parent, Vector3 localPos, float yaw)
        {
            var g = new GameObject("FamilyStage"); g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var s = g.AddComponent<FamilyStage>(); I = s; AvatarService.LocalLookChanged += s.Redress; s.Rebuild(); return s;
        }

        void OnDestroy() { if (I == this) I = null; AvatarService.LocalLookChanged -= Redress; }

        public void Rebuild()
        {
            foreach (Transform c in transform) Destroy(c.gameObject);
            _sibs.Clear(); _rings.Clear(); _buttons.Clear();
            Prim.Box(transform, "stage", new Vector3(0, 0.04f, 0), new Vector3(2.0f, 0.08f, 0.7f), Mats.Lit(Palette.WoodLight));
            Prim.Box(transform, "stageRug", new Vector3(0, 0.081f, 0), new Vector3(1.9f, 0.003f, 0.6f), Mats.Lit(Palette.Purple), false);
            var prog = ProgressService.Current; int picked = ((Settings.Current.Cosmetic % 4) + 4) % 4;
            for (int i = 0; i < 4; i++)
            {
                int k = i; float x = (i - 1.5f) * 0.48f;
                var holder = new GameObject("sib_" + Names[i]).transform; holder.SetParent(transform, false); holder.localPosition = new Vector3(x, 0.08f, 0);
                var av = holder.gameObject.AddComponent<SiblingAvatar>();
                // the picked sibling wears the player's saved outfit; the others their own saved look (per character)
                av.Build(i, Names[i], false, Outfit.ForLocal(prog, i, CharacterCatalog.SiblingKeys[i]));
                // V8: the picked sibling wears the player's saved Genies look, the others their starter Genies look
                av.SetGeniesLook(i == picked && !string.IsNullOrEmpty(Settings.Current.GeniesDefinition) ? Settings.Current.GeniesDefinition : null);
                var plate = av.transform.Find("nameplate"); if (plate) plate.gameObject.SetActive(false); // the button names them
                av.transform.localScale = Vector3.one * 0.85f;
                _sibs.Add(av);
                var ring = Prim.Shape(PrimitiveType.Cylinder, transform, "spot" + i, new Vector3(x, 0.083f, 0), new Vector3(0.42f, 0.002f, 0.42f), Mats.Unlit(Palette.Yellow));
                ring.SetActive(i == picked); _rings.Add(ring);
                var b = PokeButton.Create(transform, i == picked ? "You: " + Names[i] : "Play as " + Names[i], new Vector3(x, 0.32f, 0.42f), new Vector2(0.4f, 0.07f), Palette.Siblings[i], () => Pick(k), 0.24f);
                b.transform.localRotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(30, 0, 0); // faces the player in front of the stage
                _buttons.Add(b);
            }
        }

        /// <summary>A wardrobe piece changed: re-dress the stand-in bodies in place. Genies bodies are kept (they do not wear these pieces) -
        /// destroying them while the SDK builds them crashed the Quest (2026-11-12).</summary>
        public void RefreshOutfits()
        {
            var prog = ProgressService.Current;
            for (int i = 0; i < _sibs.Count; i++)
            {
                var av = _sibs[i]; if (av == null || av.HasGeniesBody || AvatarService.Backend != null) continue;
                Rebuild(); return;   // no Genies at all: the old stand-ins show the pieces, rebuild them (cheap, no SDK)
            }
        }

        /// <summary>V12: the picked kid wears my saved Genies look, the others their starter looks - re-dressed in place (only a kid whose
        /// look really changes reloads; every rebuild used to throw all four Genies bodies away and show stand-ins again).</summary>
        public void Redress()
        {
            if (_sibs.Count != 4 || AvatarService.Backend == null) { Rebuild(); return; }
            int picked = ((Settings.Current.Cosmetic % 4) + 4) % 4;
            for (int i = 0; i < 4; i++)
            {
                if (_sibs[i] == null) continue;
                _sibs[i].SetGeniesLook(i == picked && !string.IsNullOrEmpty(Settings.Current.GeniesDefinition) ? Settings.Current.GeniesDefinition : null);
                if (i < _rings.Count) _rings[i].SetActive(i == picked);
                if (i < _buttons.Count && _buttons[i] != null) _buttons[i].SetLabel(i == picked ? "You: " + Names[i] : "Play as " + Names[i]);
            }
        }

        public void Pick(int i)
        {
            Settings.Current.Cosmetic = i; Settings.Save();
            AudioBank.I?.Play2D("step_done");
            Redress();
            if (i >= 0 && i < _sibs.Count) _sibs[i].Trigger("Cheer");
        }
    }
}
