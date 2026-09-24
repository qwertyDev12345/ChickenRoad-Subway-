using UnityEngine;

namespace RiverJump
{
    public sealed class PortraitCamera2D : MonoBehaviour
    {
        public Transform target;
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            float targetY = Mathf.Max(3.45f, target.position.y + 3.45f);
            Vector3 desired = new(0f, targetY, -10f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.2f);
        }
    }
}
