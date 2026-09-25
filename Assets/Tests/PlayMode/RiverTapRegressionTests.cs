using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RiverTapRegressionTests : InputTestFixture
{
    private Touchscreen screen;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator TouchStartsJumpAndHudKeepsOnlyButtonInteractive()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        yield return null;
        var bootstrapType = Type.GetType("RiverJump.RiverJumpBootstrap, Assembly-CSharp", true);
        var bootstrap = UnityEngine.Object.FindFirstObjectByType(bootstrapType);
        var chicken = (MonoBehaviour)bootstrapType.GetField("chicken", PrivateInstance).GetValue(bootstrap);
        var type = chicken.GetType();
        // Dismiss first-run tutorial so this check also works with fresh PlayerPrefs.
        ((GameObject)bootstrapType.GetField("tutorialOverlay", PrivateInstance).GetValue(bootstrap)).SetActive(false);
        bootstrapType.GetMethod("StartRound", PrivateInstance).Invoke(bootstrap, null);
        type.GetMethod("RequestJump").Invoke(chicken, new object[] { Vector3.forward });
        Assert.IsFalse((bool)type.GetProperty("IsJumping").GetValue(chicken), "Start button must not also jump");
        yield return null;
        Canvas.ForceUpdateCanvases();

        var button = (Button)bootstrapType.GetField("cashOutButton", PrivateInstance).GetValue(bootstrap);
        button.gameObject.SetActive(true);
        yield return null;
        Canvas.ForceUpdateCanvases();
        var buttonRect = (RectTransform)button.transform;
        var buttonPoint = RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center));
        // Batch Mode does not render the Game View (Graphic.depth stays -1),
        // so verify the hit region and target configuration without relying on drawing.
        Assert.IsTrue(button.image.raycastTarget, "END RUN must remain interactive");
        Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(buttonRect, buttonPoint));
        Assert.IsTrue(button.image.Raycast(buttonPoint, null));
        button.gameObject.SetActive(false);

        Vector2 playPoint = new Vector2(Screen.width * .5f, Screen.height * .55f);
        var hud = (GameObject)bootstrapType.GetField("gameplayHud", PrivateInstance).GetValue(bootstrap);
        foreach (var graphic in hud.GetComponentsInChildren<Graphic>(true))
            if (graphic.gameObject != button.gameObject) Assert.IsFalse(graphic.raycastTarget, graphic.name);

        screen = InputSystem.AddDevice<Touchscreen>();
        BeginTouch(71, playPoint, queueEventOnly: true, screen: screen);
        // Test-runner coroutines can resume before MonoBehaviour.Update. Allow
        // the player loop to process the event and then run the controller.
        for (int frame = 0; frame < 3 && !(bool)type.GetProperty("IsJumping").GetValue(chicken); frame++)
            yield return null;
        Assert.IsTrue((bool)type.GetProperty("IsJumping").GetValue(chicken),
            $"Touch must jump: phase={screen.primaryTouch.phase.ReadValue()}, press={screen.primaryTouch.press.ReadValue()}, pressed={screen.primaryTouch.press.wasPressedThisFrame}, frame={Time.frameCount}, started={type.GetField("roundStartedFrame", PrivateInstance).GetValue(chicken)}, over={type.GetProperty("IsRoundOver").GetValue(chicken)}, blocked={type.GetMethod("IsOverUI", PrivateInstance).Invoke(chicken, new object[] { playPoint })}");
        chicken.StopAllCoroutines();
        type.GetField("jumping", PrivateInstance).SetValue(chicken, false);
        type.GetProperty("IsRoundOver").SetValue(chicken, true);
        type.GetMethod("RequestJump").Invoke(chicken, new object[] { Vector3.forward });
        Assert.IsFalse((bool)type.GetProperty("IsJumping").GetValue(chicken), "Finished rounds must reject jumps");
    }

}
