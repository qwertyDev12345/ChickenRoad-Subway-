using UnityEngine;

namespace RiverJump
{
    public sealed class ChickenAnimation : MonoBehaviour
    {
        private Vector3 baseScale;
        private Transform leftWing;
        private Transform rightWing;
        private Vector3 previousPosition;
        private float verticalSpeed;

        private void Start()
        {
            baseScale = transform.localScale;
            previousPosition = transform.position;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Wing") continue;
                if (leftWing == null) leftWing = child;
                else rightWing = child;
            }
        }

        private void Update()
        {
            verticalSpeed = Mathf.Lerp(verticalSpeed, (transform.position.y - previousPosition.y) / Mathf.Max(Time.deltaTime, 0.001f), Time.deltaTime * 12f);
            previousPosition = transform.position;
            bool airborne = Mathf.Abs(verticalSpeed) > 0.35f;
            float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.018f;
            float squash = airborne ? Mathf.Clamp(verticalSpeed * 0.025f, -0.09f, 0.09f) : 0f;
            transform.localScale = new Vector3(baseScale.x * (1f - squash) / pulse, baseScale.y * (1f + squash) * pulse, baseScale.z * (1f - squash) / pulse);
            float flap = Mathf.Sin(Time.time * (airborne ? 20f : 7f)) * (airborne ? 34f : 8f);
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0, 0, -18f + flap);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0, 0, 18f - flap);
        }
    }
}
