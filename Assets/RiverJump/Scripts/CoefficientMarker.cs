using System.Collections;
using UnityEngine;

namespace RiverJump
{
    public sealed class CoefficientMarker : MonoBehaviour
    {
        public float phase;
        public bool twoDimensional;
        private float startY;
        private float startX;
        private Transform label;
        private Transform coin;
        private Transform glow;
        private Transform symbol;
        private Transform chicken;
        private Vector3 coinStartScale;
        private Vector3 glowStartScale;
        private Renderer[] markerRenderers;
        private bool lastVisible;
        private bool claimed;

        private void Start()
        {
            startY = transform.position.y;
            startX = transform.position.x;
            label = transform.Find("Coefficient");
            coin = transform.Find("Gold Coin");
            glow = transform.Find("Coin Glow");
            symbol = transform.Find("Coin Symbol");
            if (coin != null) coinStartScale = coin.localScale;
            if (glow != null) glowStartScale = glow.localScale;
            var controller = FindFirstObjectByType<ChickenController>();
            if (controller != null) chicken = controller.transform;
            markerRenderers = GetComponentsInChildren<Renderer>(true);
            lastVisible = true;
            SetVisible(false);
        }

        private void Update()
        {
            if (claimed) return;
            if (chicken != null)
            {
                float rowsAhead = twoDimensional
                    ? (transform.position.y - chicken.position.y) / ChickenController.RowSpacing
                    : transform.position.z - chicken.position.z;
                // Only the next row is shown, centered ahead of the chicken.
                // This keeps the upcoming multiplier unambiguous.
                SetVisible(rowsAhead > 0.2f && rowsAhead <= 1.25f);
            }
            var position = transform.position;
            if (twoDimensional)
            {
                position.x = startX;
                position.y = startY + Mathf.Sin(Time.time * 2.4f + phase) * 0.045f;
            }
            else position.y = startY + Mathf.Sin(Time.time * 2.2f + phase) * 0.1f;
            transform.position = position;
            if (coin != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4.6f + phase) * 0.045f;
                coin.localScale = coinStartScale * pulse;
                coin.Rotate(twoDimensional ? Vector3.forward : Vector3.up, 75f * Time.deltaTime, Space.World);
            }
            if (glow != null)
            {
                float glowPulse = 1f + Mathf.Sin(Time.time * 3.4f + phase) * 0.085f;
                glow.localScale = glowStartScale * glowPulse;
                glow.Rotate(twoDimensional ? Vector3.back : Vector3.down, 28f * Time.deltaTime, Space.World);
            }
        }

        private void LateUpdate()
        {
            if (twoDimensional) return;
            if (!lastVisible) return;
            if (label == null || Camera.main == null) return;
            label.rotation = Quaternion.LookRotation(label.position - Camera.main.transform.position, Camera.main.transform.up);
            if (symbol != null) symbol.rotation = Quaternion.LookRotation(symbol.position - Camera.main.transform.position, Camera.main.transform.up);
        }

        private void SetVisible(bool visible)
        {
            if (lastVisible == visible) return;
            lastVisible = visible;
            if (markerRenderers == null) return;
            foreach (var markerRenderer in markerRenderers)
            {
                if (markerRenderer != null) markerRenderer.enabled = visible;
            }
        }

        public void Claim()
        {
            if (!claimed) StartCoroutine(ClaimRoutine());
        }

        private IEnumerator ClaimRoutine()
        {
            claimed = true;
            Vector3 startScale = transform.localScale;
            const float growDuration = 0.12f;
            const float vanishDuration = 0.24f;
            for (float time = 0; time < growDuration + vanishDuration; time += Time.deltaTime)
            {
                float scale;
                if (time < growDuration)
                {
                    float t = Mathf.SmoothStep(0f, 1f, time / growDuration);
                    scale = Mathf.Lerp(1f, 1.48f, t);
                }
                else
                {
                    float t = Mathf.SmoothStep(0f, 1f, (time - growDuration) / vanishDuration);
                    scale = Mathf.Lerp(1.48f, 0f, t);
                }
                transform.localScale = startScale * scale;
                if (twoDimensional) transform.Rotate(0, 0, 520f * Time.deltaTime);
                else transform.Rotate(0, 520f * Time.deltaTime, 0);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
