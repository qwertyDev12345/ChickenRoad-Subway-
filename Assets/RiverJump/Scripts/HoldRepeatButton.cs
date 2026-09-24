using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiverJump
{
    [RequireComponent(typeof(Button))]
    public sealed class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        private const float InitialDelay = 0.38f;
        private const float RepeatInterval = 0.085f;

        private Button button;
        private UnityAction action;
        private Coroutine repeatRoutine;
        private bool holding;

        public void Configure(UnityAction callback)
        {
            button = GetComponent<Button>();
            action = callback;
            button.onClick.RemoveAllListeners();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null) button = GetComponent<Button>();
            if (!button.interactable || action == null) return;
            holding = true;
            action.Invoke();
            if (repeatRoutine != null) StopCoroutine(repeatRoutine);
            repeatRoutine = StartCoroutine(RepeatWhileHeld());
        }

        public void OnPointerUp(PointerEventData eventData) => StopRepeating();

        public void OnPointerExit(PointerEventData eventData) => StopRepeating();

        public void OnSubmit(BaseEventData eventData)
        {
            if (button == null) button = GetComponent<Button>();
            if (button.interactable) action?.Invoke();
        }

        private IEnumerator RepeatWhileHeld()
        {
            yield return new WaitForSecondsRealtime(InitialDelay);
            while (holding && button != null && button.interactable)
            {
                action?.Invoke();
                yield return new WaitForSecondsRealtime(RepeatInterval);
            }
            repeatRoutine = null;
        }

        private void StopRepeating()
        {
            holding = false;
            if (repeatRoutine == null) return;
            StopCoroutine(repeatRoutine);
            repeatRoutine = null;
        }

        private void OnDisable() => StopRepeating();
    }
}
