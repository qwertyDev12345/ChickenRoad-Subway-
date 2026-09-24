using UnityEngine;

namespace RiverJump
{
    public sealed class WaterSurfaceAnimator2D : MonoBehaviour
    {
        public Material waterMaterial;

        private void Update()
        {
            if (waterMaterial == null) return;
            float horizontal = Mathf.Sin(Time.time * 0.23f) * 0.018f;
            float vertical = Mathf.Repeat(Time.time * 0.012f, 1f);
            waterMaterial.mainTextureOffset = new Vector2(horizontal, vertical);
            float shimmer = Mathf.Sin(Time.time * 0.75f) * 0.025f;
            waterMaterial.color = new Color(1f + shimmer, 1f + shimmer, 1f + shimmer, 1f);
        }

        private void OnDestroy()
        {
            if (waterMaterial != null) Destroy(waterMaterial);
        }
    }
}
