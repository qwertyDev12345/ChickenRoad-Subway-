using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RiverJump
{
    public sealed class ChickenController : MonoBehaviour
    {
        public const float RowSpacing = 1.55f;
        public const float StartRowY = -1.2f;
        public bool IsRoundOver { get; set; }
        public bool IsJumping => jumping;
        public int CurrentRow => currentRow;
        public float EstimatedForwardJumpTime
        {
            get
            {
                int nextRow = Mathf.Clamp(currentRow + 1, 0, 20);
                float targetY = nextRow == 0 ? StartRowY : nextRow * RowSpacing;
                float extraTravel = Mathf.Max(0f, Mathf.Abs(targetY - transform.position.y) - RowSpacing);
                return 0.07f + 0.28f + extraTravel * 0.06f + 0.1f;
            }
        }
        private bool jumping;
        private RiverPlatform2D platform;
        private Vector3 platformPosition;
        private Vector3 restingScale;
        private Transform artwork;
        private SpriteRenderer artworkRenderer;
        private Vector3 artworkRestingPosition;
        private Vector3 artworkRestingScale;
        private Quaternion artworkRestingRotation;
        private LandingIndicator2D landingIndicator;
        private int currentRow;

        private void Start()
        {
            restingScale = transform.localScale;
            currentRow = Mathf.Max(0, Mathf.RoundToInt(transform.position.y / RowSpacing));
            landingIndicator = FindFirstObjectByType<LandingIndicator2D>();
            artwork = transform.Find("Chicken Artwork");
            if (artwork != null)
            {
                artworkRenderer = artwork.GetComponent<SpriteRenderer>();
                artworkRestingPosition = artwork.localPosition;
                artworkRestingScale = artwork.localScale;
                artworkRestingRotation = artwork.localRotation;
            }
        }

        private void Update()
        {
            if (IsRoundOver) return;
            FollowPlatform();
            if (jumping) return;

            ReadKeyboard();
            ReadMouse();
            ReadTouchscreen();

            if (transform.position.x < -4.2f || transform.position.x > 4.2f) RiverJumpBootstrap.Instance.Lose();
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                Jump(Vector3.forward);
        }

        private void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.wasReleasedThisFrame && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                Jump(Vector3.forward);
        }

        private void ReadTouchscreen()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;
            var touch = touchscreen.primaryTouch;
            int touchId = touch.touchId.ReadValue();
            if (touch.press.wasReleasedThisFrame && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touchId)))
                Jump(Vector3.forward);
        }

        public void RequestJump(Vector3 direction) => Jump(Vector3.forward);

        private void Jump(Vector3 direction)
        {
            if (!jumping)
            {
                bool dangerous = landingIndicator != null
                    && landingIndicator.IsVisible
                    && landingIndicator.CurrentSafety < 0.45f;
                RiverJumpBootstrap.Instance.PlayJumpSound(dangerous);
                StartCoroutine(JumpRoutine(direction));
            }
        }

        private IEnumerator JumpRoutine(Vector3 direction)
        {
            jumping = true;
            platform = null;
            Vector3 start = transform.position;
            int rowDelta = Mathf.RoundToInt(direction.z);
            int nextRow = Mathf.Clamp(currentRow + rowDelta, 0, 20);
            Vector3 target = start;
            target.x = Mathf.Clamp(Mathf.Round(start.x + direction.x), -3f, 3f);
            target.y = nextRow == 0 ? StartRowY : nextRow * RowSpacing;
            target.z = start.z;

            const float anticipationDuration = 0.07f;
            for (float elapsed = 0; elapsed < anticipationDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / anticipationDuration);
                SetArtworkPose(
                    new Vector2(1f + t * 0.12f, 1f - t * 0.16f),
                    -0.065f * t,
                    direction.x * 7f * t);
                yield return null;
            }

            RiverJumpBootstrap.Instance.SpawnJumpEffect(start + Vector3.up * 0.34f);
            float extraTravel = Mathf.Max(0f, Mathf.Abs(target.y - start.y) - RowSpacing);
            float duration = 0.28f + extraTravel * 0.06f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float arc = Mathf.Sin(t * Mathf.PI);
                float flutter = Mathf.Sin(t * Mathf.PI * 4f);
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * arc * 0.42f;
                SetArtworkPose(
                    new Vector2(1f - arc * 0.07f + flutter * 0.025f, 1f + arc * 0.17f),
                    arc * 0.035f,
                    -direction.x * (9f + arc * 8f) + flutter * 3.5f);
                yield return null;
            }
            transform.position = target;

            const float landingDuration = 0.1f;
            for (float elapsed = 0; elapsed < landingDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / landingDuration;
                float impact = Mathf.Sin(t * Mathf.PI);
                SetArtworkPose(
                    new Vector2(1f + impact * 0.15f, 1f - impact * 0.13f),
                    -impact * 0.055f,
                    direction.x * impact * 4f);
                yield return null;
            }

            ResetArtworkPose();
            currentRow = nextRow;
            jumping = false;
            CheckLanding();
        }

        public IEnumerator PlayDefeatAnimation()
        {
            jumping = true;
            platform = null;
            Vector3 start = transform.position;
            Color startColor = artworkRenderer != null ? artworkRenderer.color : Color.white;
            RiverJumpBootstrap.Instance.SpawnDefeatEffect(start + Vector3.up * 1.28f);

            const float shockDuration = 0.24f;
            for (float elapsed = 0; elapsed < shockDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / shockDuration;
                float pulse = Mathf.Sin(t * Mathf.PI);
                float panic = Mathf.Sin(t * Mathf.PI * 6f);
                transform.position = start + Vector3.up * pulse * 0.14f;
                SetArtworkPose(
                    new Vector2(1f + pulse * 0.13f, 1f - pulse * 0.08f),
                    pulse * 0.035f,
                    panic * 9f);
                yield return null;
            }

            const float sinkDuration = 0.78f;
            for (float elapsed = 0; elapsed < sinkDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / sinkDuration;
                float eased = t * t;
                float wobble = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t);
                transform.position = start
                    + Vector3.right * wobble * 0.09f
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.18f - eased * 1.55f);
                SetArtworkPose(
                    new Vector2(1f - t * 0.2f + Mathf.Abs(wobble) * 0.04f, 1f - t * 0.42f),
                    -eased * 0.08f,
                    wobble * 16f + t * 18f);
                if (artworkRenderer != null)
                {
                    Color color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0.12f, Mathf.SmoothStep(0.35f, 1f, t));
                    artworkRenderer.color = color;
                }
                yield return null;
            }

            if (artworkRenderer != null)
            {
                Color color = startColor;
                color.a = 0.12f;
                artworkRenderer.color = color;
            }
        }

        private void SetArtworkPose(Vector2 scale, float verticalOffset, float angle)
        {
            if (artwork != null)
            {
                artwork.localPosition = artworkRestingPosition + Vector3.up * verticalOffset;
                artwork.localScale = Vector3.Scale(artworkRestingScale, new Vector3(scale.x, scale.y, 1f));
                artwork.localRotation = artworkRestingRotation * Quaternion.Euler(0f, 0f, angle);
                return;
            }

            transform.localScale = Vector3.Scale(restingScale, new Vector3(scale.x, scale.y, 1f));
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ResetArtworkPose()
        {
            transform.localScale = restingScale;
            transform.rotation = Quaternion.identity;
            if (artwork == null) return;
            artwork.localPosition = artworkRestingPosition;
            artwork.localScale = artworkRestingScale;
            artwork.localRotation = artworkRestingRotation;
            if (artworkRenderer != null)
            {
                Color color = artworkRenderer.color;
                color.a = 1f;
                artworkRenderer.color = color;
            }
        }

        private void CheckLanding()
        {
            int row = currentRow;
            if (row == 0 || row == 7 || row == 14 || row >= 20)
            {
                PlaySuccessfulLandingFeedback(false);
                RiverJumpBootstrap.Instance.ReachedRow(Mathf.Min(row, 20), false, false);
                return;
            }

            var hits = Physics2D.OverlapPointAll(transform.position);
            foreach (var hit in hits)
            {
                platform = hit.GetComponentInParent<RiverPlatform2D>();
                if (platform != null && platform.isActiveAndEnabled)
                {
                    platformPosition = platform.transform.position;
                    float halfWidth = Mathf.Max(0.01f, hit.bounds.extents.x);
                    float centerDistance = Mathf.Abs(transform.position.x - platform.transform.position.x);
                    float perfectThreshold = Mathf.Clamp(halfWidth * 0.22f, 0.14f, 0.28f);
                    bool perfect = centerDistance <= perfectThreshold;
                    bool nearMiss = centerDistance >= halfWidth * 0.72f;
                    PlaySuccessfulLandingFeedback(nearMiss);
                    RiverJumpBootstrap.Instance.ReachedRow(row, perfect, true);
                    return;
                }
            }
            RiverJumpBootstrap.Instance.SpawnSplash(transform.position);
            RiverJumpBootstrap.Instance.Lose();
        }

        private void PlaySuccessfulLandingFeedback(bool nearMiss)
        {
            RiverJumpBootstrap.Instance.PlayLandingSound(nearMiss);
            RiverJumpBootstrap.Instance.SpawnLandingEffect(transform.position + Vector3.up * 0.32f);
        }

        private void FollowPlatform()
        {
            if (platform == null || jumping) return;
            if (!platform.isActiveAndEnabled)
            {
                platform = null;
                RiverJumpBootstrap.Instance.SpawnSplash(transform.position);
                RiverJumpBootstrap.Instance.Lose();
                return;
            }
            Vector3 delta = platform.transform.position - platformPosition;
            transform.position += delta;
            platformPosition = platform.transform.position;
        }
    }
}
