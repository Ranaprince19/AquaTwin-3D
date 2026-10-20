using System.Collections;
using System.IO;
using UnityEngine;

namespace LeakWatch
{
    /// F9: steps through all six scenes, waits 2 seconds on each and saves a PNG
    /// into LeakWatch3D/Screenshots/ (scene 6 gets one extra shot once the alert is up).
    public class ScreenshotTour : MonoBehaviour
    {
        bool running;
        static readonly string[] Names = { "1_supply", "2_measure", "3_collect", "4_send", "5_leak", "6_analyze" };

        void Update()
        {
            if (!running && InputHelper.KeyDown(LWKey.F9)) StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            running = true;
            var story = Ctx.Story;
            bool prevAuto = story.Autoplay;
            story.Autoplay = false;
            story.CaptureMode = true;

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            Directory.CreateDirectory(dir);

            for (int i = 0; i < StoryController.SceneCount; i++)
            {
                story.GoTo(i, true);
                yield return new WaitForSeconds(2f);
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(dir, "scene" + Names[i] + ".png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("LeakWatch screenshot: " + path);

                if (i == StoryController.SceneCount - 1)
                {
                    yield return new WaitForSeconds(5f);
                    yield return new WaitForEndOfFrame();
                    string path2 = Path.Combine(dir, "scene" + Names[i] + "_alert.png");
                    ScreenCapture.CaptureScreenshot(path2);
                    Debug.Log("LeakWatch screenshot: " + path2);
                }
                yield return null;
            }

            story.CaptureMode = false;
            story.Autoplay = prevAuto;
            running = false;
        }
    }
}
