using System.Collections;
using System.IO;
using UnityEngine;

namespace FedUp.DebugTools
{
    /// <summary>
    /// Evidence videos: launched with "-record &lt;folder&gt;", the game saves every frame it draws (HUD included) as a
    /// numbered JPG, at a fixed 30 frames per game-second, and tools/capture_build.ps1 stitches them into the mp4.
    /// Recording from inside the game means other windows on the desktop can never end up in the video, and the
    /// game doesn't need to be in front. Game time steps exactly 1/30 s per frame, so the video plays at true speed
    /// however long each frame takes to save (frame-time numbers from a recorded run are meaningless).
    /// </summary>
    public class FrameRecorder : MonoBehaviour
    {
        public const int Fps = 30;
        string folder;
        int frame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfAsked()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-record");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("FrameRecorder");
            DontDestroyOnLoad(go);
            go.AddComponent<FrameRecorder>().folder = args[i + 1];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            Time.captureFramerate = Fps;
            Debug.Log($"RECORDING frames to {folder} at {Fps} fps");
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, $"f_{frame++:D5}.jpg"), shot.EncodeToJPG(92));
                Destroy(shot);
            }
        }
    }
}
