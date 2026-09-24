using UnityEngine;

namespace RiverJump
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            rectTransform.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rectTransform.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
