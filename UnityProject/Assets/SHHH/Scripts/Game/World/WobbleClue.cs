using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Readable trap clue: a small coral bead wobbling above a precariously placed noisy object.</summary>
    public sealed class WobbleClue : MonoBehaviour
    {
        Vector3 _base;
        void Start() { _base = transform.localPosition; }
        void Update() { transform.localPosition = _base + new Vector3(Mathf.Sin(Time.time * 9f) * 0.02f, Mathf.Abs(Mathf.Sin(Time.time * 4.5f)) * 0.03f, 0); }
    }
}
