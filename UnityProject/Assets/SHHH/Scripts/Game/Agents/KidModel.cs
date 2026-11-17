using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Original stylised kid sibling (built from primitives, fully clothed in pyjamas + slippers, big friendly head).
    /// Animated procedurally: walk swing, bob, carry pose, cheer, shush. Cheap: ~20 primitives, shared materials.
    /// </summary>
    public sealed class KidModel : MonoBehaviour
    {
        public Transform Head { get; private set; }
        Transform _body, _armL, _armR, _legL, _legR, _torso;
        float _phase, _speed;
        public bool Carrying;
        public float CheerUntil, ShushUntil;

        static readonly Color[] Skins = { Palette.Hex(0xF2C7A5), Palette.Hex(0xC98E62), Palette.Hex(0x8D5B3E), Palette.Hex(0xE8B48C) };
        static readonly Color[] Hairs = { Palette.Hex(0x3A2418), Palette.Hex(0xD9822B), Palette.Hex(0x15121F), Palette.Hex(0xE8C15A) };

        public void Build(int slot, int cosmetic, Color pyjama)
        {
            var skin = Mats.Lit(Skins[(slot + cosmetic) % Skins.Length]);
            var hair = Mats.Lit(Hairs[(slot * 3 + cosmetic) % Hairs.Length]);
            var pj = Mats.Lit(pyjama); var pjDark = Mats.Lit(pyjama * 0.75f + new Color(0, 0, 0, 0.25f));
            var cream = Mats.Lit(Palette.Cream); var ink = Mats.Unlit(Palette.Ink);
            _body = new GameObject("kid").transform; _body.SetParent(transform, false);

            // Legs (pyjama trousers) + slippers, pivoted at the hips for swinging.
            _legL = Pivot("legL", new Vector3(-0.08f, 0.5f, 0)); _legR = Pivot("legR", new Vector3(0.08f, 0.5f, 0));
            foreach (var leg in new[] { _legL, _legR })
            {
                Prim.Shape(PrimitiveType.Capsule, leg, "trouser", new Vector3(0, -0.24f, 0), new Vector3(0.12f, 0.24f, 0.12f), pjDark);
                var slipper = Prim.Shape(PrimitiveType.Sphere, leg, "slipper", new Vector3(0, -0.47f, 0.04f), new Vector3(0.11f, 0.06f, 0.17f), cream);
                Prim.Shape(PrimitiveType.Sphere, slipper.transform, "pom", new Vector3(0, 0.6f, 0.45f), Vector3.one * 0.45f, Mats.Lit(pyjama));
            }
            // Torso (pyjama top with buttons and a cream collar)
            _torso = Pivot("torso", new Vector3(0, 0.5f, 0));
            Prim.Shape(PrimitiveType.Capsule, _torso, "top", new Vector3(0, 0.23f, 0), new Vector3(0.3f, 0.24f, 0.22f), pj);
            Prim.Shape(PrimitiveType.Cylinder, _torso, "collar", new Vector3(0, 0.43f, 0), new Vector3(0.18f, 0.015f, 0.15f), cream);
            for (int i = 0; i < 3; i++) Prim.Shape(PrimitiveType.Sphere, _torso, "button", new Vector3(0, 0.33f - i * 0.08f, 0.108f), Vector3.one * 0.025f, cream);
            Prim.Box(_torso, "pocket", new Vector3(0.07f, 0.3f, 0.105f), new Vector3(0.06f, 0.05f, 0.01f), cream, false);
            // Arms pivoted at the shoulders, hands in skin tone
            _armL = Pivot("armL", new Vector3(-0.17f, 0.88f, 0)); _armR = Pivot("armR", new Vector3(0.17f, 0.88f, 0));
            foreach (var arm in new[] { _armL, _armR })
            {
                Prim.Shape(PrimitiveType.Capsule, arm, "sleeve", new Vector3(0, -0.17f, 0), new Vector3(0.085f, 0.17f, 0.085f), pj);
                Prim.Shape(PrimitiveType.Sphere, arm, "hand", new Vector3(0, -0.36f, 0), Vector3.one * 0.075f, skin);
            }
            // Big friendly head
            Head = Pivot("head", new Vector3(0, 1.1f, 0));
            Prim.Shape(PrimitiveType.Sphere, Head, "face", new Vector3(0, 0.05f, 0), new Vector3(0.3f, 0.29f, 0.28f), skin);
            var style = slot % 4;
            Prim.Shape(PrimitiveType.Sphere, Head, "hairCap", new Vector3(0, 0.1f, -0.02f), new Vector3(0.315f, 0.25f, 0.3f), hair);
            if (style == 1) { Prim.Shape(PrimitiveType.Sphere, Head, "bunL", new Vector3(-0.14f, 0.15f, -0.03f), Vector3.one * 0.1f, hair); Prim.Shape(PrimitiveType.Sphere, Head, "bunR", new Vector3(0.14f, 0.15f, -0.03f), Vector3.one * 0.1f, hair); }
            else if (style == 2) { var t = Prim.Shape(PrimitiveType.Capsule, Head, "tuft", new Vector3(0, 0.23f, 0.04f), new Vector3(0.05f, 0.06f, 0.05f), hair); t.transform.localRotation = Quaternion.Euler(30, 0, 0); }
            else if (style == 3) Prim.Shape(PrimitiveType.Capsule, Head, "ponytail", new Vector3(0, 0.05f, -0.15f), new Vector3(0.08f, 0.1f, 0.08f), hair);
            // Face: eyes with highlights, cheeks, smile
            foreach (var x in new[] { -0.065f, 0.065f })
            {
                var eye = Prim.Shape(PrimitiveType.Sphere, Head, "eye", new Vector3(x, 0.06f, 0.125f), new Vector3(0.05f, 0.06f, 0.03f), ink);
                Prim.Shape(PrimitiveType.Sphere, eye.transform, "glint", new Vector3(0.2f, 0.25f, 0.4f), Vector3.one * 0.35f, Mats.Unlit(Color.white));
                Prim.Shape(PrimitiveType.Sphere, Head, "cheek", new Vector3(x * 1.45f, -0.01f, 0.115f), new Vector3(0.05f, 0.03f, 0.02f), Mats.Lit(Palette.Coral));
            }
            var mouth = Prim.Shape(PrimitiveType.Capsule, Head, "smile", new Vector3(0, -0.035f, 0.135f), new Vector3(0.012f, 0.03f, 0.012f), ink);
            mouth.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }

        Transform Pivot(string n, Vector3 p) { var t = new GameObject(n).transform; t.SetParent(_body, false); t.localPosition = p; return t; }

        public void SetSpeed(float s) => _speed = Mathf.Lerp(_speed, s, 0.2f);

        void LateUpdate()
        {
            if (_body == null) return;
            float k = Mathf.Clamp01(_speed / 1.1f);
            _phase += Time.deltaTime * (2.5f + 7f * k);
            float s = Mathf.Sin(_phase);
            _legL.localRotation = Quaternion.Euler(s * 30f * k, 0, 0);
            _legR.localRotation = Quaternion.Euler(-s * 30f * k, 0, 0);
            float breath = Mathf.Sin(Time.time * 1.7f) * 2f;
            bool cheer = Time.time < CheerUntil, shush = Time.time < ShushUntil;
            if (cheer) { float w = Mathf.Sin(Time.time * 14f) * 15f; _armL.localRotation = Quaternion.Euler(0, 0, -150 + w); _armR.localRotation = Quaternion.Euler(0, 0, 150 - w); }
            else if (Carrying) { _armL.localRotation = Quaternion.Euler(-75, 0, -8); _armR.localRotation = Quaternion.Euler(-75, 0, 8); }
            else
            {
                _armL.localRotation = Quaternion.Euler(-s * 28f * k, 0, -6f + breath);
                _armR.localRotation = shush ? Quaternion.Euler(-135, 0, 25) : Quaternion.Euler(s * 28f * k, 0, 6f - breath);
            }
            _body.localPosition = new Vector3(0, Mathf.Abs(s) * 0.03f * k + (cheer ? Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.06f : 0f), 0);
            _torso.localRotation = Quaternion.Euler(4f * k + breath * 0.5f, s * 5f * k, 0);
            Head.localRotation = Quaternion.Euler(shush ? 8f : 0f, Mathf.Sin(Time.time * 0.6f) * 8f, Mathf.Sin(_phase) * 3f * k);
        }
    }
}
