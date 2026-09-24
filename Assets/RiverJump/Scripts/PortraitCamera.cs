using UnityEngine;

namespace RiverJump
{
    public sealed class PortraitCamera : MonoBehaviour
    {
        public Transform target;
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            float forward = Mathf.Max(2.5f, target.position.z + 4.2f);
            Vector3 desired = new Vector3(0, 10.5f, forward - 11.2f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.22f);
        }
    }
}
