using UnityEngine;

namespace RiverJump
{
    public sealed class DivingCrocodile : MonoBehaviour
    {
        public float phase;
        private RiverPlatform platform;
        private Renderer[] renderers;
        private Collider platformCollider;
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            platform = GetComponent<RiverPlatform>();
            renderers = GetComponentsInChildren<Renderer>();
            // Decorative eyes, scales and tail are created as primitives and their
            // colliders are destroyed at the end of the frame. Cache only the
            // crocodile's gameplay collider so we never retain destroyed children.
            platformCollider = GetComponent<Collider>();
            properties = new MaterialPropertyBlock();
        }

        private void Update()
        {
            float cycle = Mathf.Repeat(Time.time + phase, 6f);
            bool warning = cycle > 3.5f && cycle < 4.5f;
            bool submerged = cycle >= 4.5f;
            platform.enabled = !submerged;
            if (platformCollider != null) platformCollider.enabled = !submerged;
            foreach (var renderer in renderers)
            {
                renderer.enabled = !submerged;
                if (!submerged)
                {
                    properties.SetColor("_BaseColor", warning && Mathf.FloorToInt(Time.time * 8) % 2 == 0 ? new Color(1f, 0.25f, 0.12f) : new Color(0.18f, 0.52f, 0.16f));
                    renderer.SetPropertyBlock(properties);
                }
            }
        }
    }
}
