using UnityEngine;

namespace RiverJump
{
    public sealed class PlatformWake2D : MonoBehaviour
    {
        public RiverPlatform2D target;
        public SpriteRenderer wakeRenderer;
        public float phase;

        private Vector3 baseScale;
        private float trailingDistance;

        private void Start()
        {
            baseScale = transform.localScale;
            var targetRenderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
            trailingDistance = targetRenderer != null ? Mathf.Clamp(targetRenderer.bounds.extents.x * 0.55f, 0.55f, 1.05f) : 0.7f;
        }

        private void Update()
        {
            if (target == null || wakeRenderer == null)
            {
                if (wakeRenderer != null) wakeRenderer.enabled = false;
                return;
            }
            bool visible = target.isActiveAndEnabled;
            wakeRenderer.enabled = visible;
            if (!visible) return;

            float direction = Mathf.Sign(target.speed);
            transform.position = target.transform.position - Vector3.right * direction * trailingDistance + Vector3.down * 0.18f;
            float cycle = Mathf.Repeat(Time.time * 1.55f + phase, 1f);
            transform.localScale = Vector3.Scale(baseScale, new Vector3(0.78f + cycle * 0.58f, 0.82f + cycle * 0.34f, 1f));
            Color color = wakeRenderer.color;
            color.a = (1f - cycle) * 0.28f;
            wakeRenderer.color = color;
        }
    }
}
