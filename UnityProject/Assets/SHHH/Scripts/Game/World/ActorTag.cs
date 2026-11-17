using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Marks a collider as belonging to an actor (sibling slot 0..3, pet or Mom) so host physics can attribute bumps.
    /// Speed is tracked so "careless" contacts (fast swipes) can be distinguished from gentle ones.
    /// </summary>
    public sealed class ActorTag : MonoBehaviour
    {
        public int ActorId = SHHH.Core.ActorIds.None;
        public float Speed { get; private set; }
        Vector3 _last; bool _init;

        void FixedUpdate()
        {
            var p = transform.position;
            if (_init) Speed = Mathf.Lerp(Speed, (p - _last).magnitude / Time.fixedDeltaTime, 0.5f);
            _last = p; _init = true;
        }
    }

    public static class Layers
    {
        public static int Props => L("Props", 8);
        public static int Hands => L("Hands", 9);
        public static int Agents => L("Agents", 10);
        public static int UI => L("WorldUI", 11);
        public static int Furniture => L("Furniture", 12);
        static int L(string name, int fallback) { int l = LayerMask.NameToLayer(name); return l >= 0 ? l : fallback; }
        public static int PropsMask => 1 << Props;
        public static int EnvironmentMask => (1 << 0) | (1 << Furniture);
    }
}
