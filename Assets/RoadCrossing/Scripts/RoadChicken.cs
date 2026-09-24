using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RoadCrossing
{
    public sealed class RoadChicken : MonoBehaviour
    {
        public bool InputEnabled { get; set; }
        private bool moving;
        private int row;
        private Vector3 restingScale;
        private SpriteRenderer spriteRenderer;
        private Color restingColor;

        private void Awake()
        {
            restingScale = transform.localScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            restingColor = spriteRenderer.color;
        }

        private void Update()
        {
            if (!InputEnabled || moving) return;
            bool pressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame);
            pressed |= Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                int id = Touchscreen.current.primaryTouch.touchId.ReadValue();
                pressed |= EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(id);
            }
            if (pressed) StartCoroutine(RunForward());
        }

        private IEnumerator RunForward()
        {
            moving = true;
            int nextRow = row + 1;
            Vector3 start = transform.position;
            Vector3 target = new Vector3(0f, nextRow * RoadCrossingBootstrap.RowSpacing, 0f);
            const float duration = 0.42f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                float bounce = Mathf.Sin(t * Mathf.PI * 4f);
                transform.localScale = Vector3.Scale(restingScale, new Vector3(1f - bounce * 0.04f, 1f + bounce * 0.07f, 1f));
                if (CheckForCar())
                {
                    moving = false;
                    RoadCrossingBootstrap.Instance.HitByCar();
                    yield break;
                }
                yield return null;
            }
            transform.position = target;
            transform.localScale = restingScale;
            row = nextRow;
            moving = false;
            RoadCrossingBootstrap.Instance.CompletedRow(row);
        }

        private bool CheckForCar()
        {
            foreach (var car in FindObjectsByType<RoadCar>(FindObjectsSortMode.None))
            {
                Vector2 delta = car.transform.position - transform.position;
                if (Mathf.Abs(delta.x) < 0.92f && Mathf.Abs(delta.y) < 0.5f) return true;
            }
            return false;
        }

        public IEnumerator PlayHit()
        {
            Vector3 start = transform.position;
            Color startColor = spriteRenderer.color;
            for (float elapsed = 0f; elapsed < 0.7f; elapsed += Time.deltaTime)
            {
                float t = elapsed / 0.7f;
                transform.position = start + Vector3.right * Mathf.Sin(t * Mathf.PI) * 1.15f + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                transform.rotation = Quaternion.Euler(0f, 0f, t * 540f);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }
        }

        public void ResetChicken()
        {
            StopAllCoroutines();
            row = 0;
            moving = false;
            transform.position = new Vector3(0f, -0.35f, 0f);
            transform.rotation = Quaternion.identity;
            transform.localScale = restingScale;
            spriteRenderer.color = restingColor;
        }
    }
}
