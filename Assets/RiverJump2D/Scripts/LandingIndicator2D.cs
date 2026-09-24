using UnityEngine;

namespace RiverJump
{
    public sealed class LandingIndicator2D : MonoBehaviour
    {
        public ChickenController chicken;
        public SpriteRenderer indicator;
        public float CurrentSafety { get; private set; } = 1f;
        public bool IsVisible => indicator != null && indicator.enabled;

        private Vector3 baseScale;
        private bool safetyInitialized;
        private bool wasSafe;

        private void Start()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (chicken == null || indicator == null) return;
            int nextRow = chicken.CurrentRow + 1;
            bool visible = !chicken.IsRoundOver && !chicken.IsJumping && nextRow <= 20;
            indicator.enabled = visible;
            if (!visible)
            {
                CurrentSafety = 1f;
                safetyInitialized = false;
                return;
            }

            float targetX = Mathf.Clamp(Mathf.Round(chicken.transform.position.x), -3f, 3f);
            transform.position = new Vector3(targetX, nextRow * ChickenController.RowSpacing, -0.5f);
            float safety = EvaluateSafety(nextRow, targetX, chicken.EstimatedForwardJumpTime);
            CurrentSafety = safety;
            bool safe = safety >= 0.99f;
            if (safetyInitialized && safe && !wasSafe) RiverJumpBootstrap.Instance?.PlaySafeWindowSound();
            safetyInitialized = true;
            wasSafe = safe;
            Color color = safety >= 0.99f
                ? new Color(0.3f, 1f, 0.34f, 0.88f)
                : safety >= 0.45f
                    ? new Color(1f, 0.78f, 0.12f, 0.82f)
                    : new Color(1f, 0.24f, 0.14f, 0.74f);
            indicator.color = color;
            float pulse = 1f + Mathf.Sin(Time.time * 5.2f) * (safety >= 0.99f ? 0.08f : 0.045f);
            transform.localScale = baseScale * pulse;
        }

        private static float EvaluateSafety(int row, float targetX, float travelTime)
        {
            if (row == 7 || row == 14 || row >= 20) return 1f;
            float nearestEdgeDistance = float.MaxValue;
            foreach (var platform in FindObjectsByType<RiverPlatform2D>(FindObjectsSortMode.None))
            {
                if (platform.difficulty != row || !platform.isActiveAndEnabled) continue;
                float predictedX = platform.transform.position.x + platform.speed * travelTime;
                while (predictedX > platform.wrapEdge) predictedX -= platform.wrapDistance;
                while (predictedX < -platform.wrapEdge) predictedX += platform.wrapDistance;
                var collider = platform.GetComponent<Collider2D>();
                float halfWidth = collider != null ? collider.bounds.extents.x * 0.88f : 0.8f;
                float edgeDistance = Mathf.Abs(targetX - predictedX) - halfWidth;
                nearestEdgeDistance = Mathf.Min(nearestEdgeDistance, edgeDistance);
            }

            if (nearestEdgeDistance <= 0f) return 1f;
            return Mathf.Clamp01(1f - nearestEdgeDistance / 0.75f) * 0.8f;
        }
    }
}
