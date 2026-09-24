using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotComponent : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(TakeScreenShot());
        }
    }
    IEnumerator TakeScreenShot()
    {
        yield return new WaitForEndOfFrame();
        string currentTime = System.DateTime.Now.ToString("MM-dd-yy (HH-mm-ss)");
        ScreenCapture.CaptureScreenshot("screenshot " + currentTime + ".png");
        Debug.Log("A screenshot was taken!");
    }
}
