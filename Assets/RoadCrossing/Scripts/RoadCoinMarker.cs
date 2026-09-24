using System.Collections;
using UnityEngine;

namespace RoadCrossing
{
    public sealed class RoadCoinMarker : MonoBehaviour
    {
        private int row;
        private float phase;
        private float baseY;
        private Transform coin;
        private Transform glow;
        private Vector3 coinScale;
        private Vector3 glowScale;
        private Renderer[] renderers;
        private bool claimed;
        private bool target;

        public void Configure(int markerRow, float animationPhase)
        {
            row = markerRow;
            phase = animationPhase;
        }

        private void Awake()
        {
            baseY = transform.position.y;
            coin = transform.Find("Gold Coin");
            glow = transform.Find("Coin Glow");
            coinScale = coin != null ? coin.localScale : Vector3.one;
            glowScale = glow != null ? glow.localScale : Vector3.one;
            renderers = GetComponentsInChildren<Renderer>(true);
            SetVisible(false);
        }

        private void Update()
        {
            if (!target || claimed) return;
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * 2.4f + phase) * 0.045f;
            transform.position = position;
            if (coin != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4.6f + phase) * 0.045f;
                coin.localScale = coinScale * pulse;
                coin.Rotate(Vector3.forward, 75f * Time.deltaTime, Space.World);
            }
            if (glow != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3.4f + phase) * 0.085f;
                glow.localScale = glowScale * pulse;
                glow.Rotate(Vector3.back, 28f * Time.deltaTime, Space.World);
            }
        }

        public void SetTarget(bool isTarget)
        {
            if (claimed) return;
            target = isTarget && !claimed;
            SetVisible(target);
        }

        public void Claim()
        {
            if (!claimed) StartCoroutine(ClaimRoutine());
        }

        public void ResetMarker()
        {
            StopAllCoroutines();
            claimed = false;
            target = false;
            transform.position = new Vector3(transform.position.x, row * RoadCrossingBootstrap.RowSpacing, transform.position.z);
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (coin != null) coin.localScale = coinScale;
            if (glow != null) glow.localScale = glowScale;
            SetVisible(false);
        }

        private IEnumerator ClaimRoutine()
        {
            claimed = true;
            Vector3 startScale = transform.localScale;
            for (float time = 0f; time < 0.36f; time += Time.deltaTime)
            {
                float t = time / 0.36f;
                float scale = t < 0.34f
                    ? Mathf.Lerp(1f, 1.48f, Mathf.SmoothStep(0f, 1f, t / 0.34f))
                    : Mathf.Lerp(1.48f, 0f, Mathf.SmoothStep(0f, 1f, (t - 0.34f) / 0.66f));
                transform.localScale = startScale * scale;
                transform.Rotate(0f, 0f, 520f * Time.deltaTime);
                yield return null;
            }
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (var item in renderers) item.enabled = visible;
        }
    }
}
