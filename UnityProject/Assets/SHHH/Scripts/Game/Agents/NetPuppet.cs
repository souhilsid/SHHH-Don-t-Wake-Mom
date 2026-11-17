using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Online client: a bot sibling or pet whose brain runs on the host. The networked pose (position, yaw, speed,
    /// carrying) is applied smoothly here; the avatar animates from the replicated speed.
    /// </summary>
    public sealed class NetPuppet : MonoBehaviour
    {
        public int ActorId;
        public SiblingAvatar Avatar;
        Vector3 _target; float _yaw, _speed; bool _carry, _has;

        public void SetTarget(Vector3 pos, float yaw, float speed, bool carrying)
        {
            if (!_has) { transform.position = pos; transform.rotation = Quaternion.Euler(0, yaw, 0); _has = true; }
            _target = pos; _yaw = yaw; _speed = speed; _carry = carrying;
        }

        void Update()
        {
            if (!_has) return;
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            var d = _target - transform.position;
            transform.position = d.sqrMagnitude > 4f ? _target : Vector3.Lerp(transform.position, _target, k); // teleport if far (bed / scene start)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, _yaw, 0), k);
            Avatar?.SetMotion(_speed, _carry);
        }
    }
}
