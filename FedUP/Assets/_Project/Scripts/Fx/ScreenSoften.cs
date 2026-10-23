using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FedUp.Fx
{
    /// <summary>
    /// GDD: petting the cat makes "the screen briefly soften". Fades a post-processing volume in and out
    /// (warmer colour, lighter vignette, a touch brighter; see Settings/CatSoft.asset).
    /// </summary>
    public class ScreenSoften : MonoBehaviour
    {
        [SerializeField] Volume volume;
        [SerializeField, Min(0.1f)] float seconds = 3.5f;

        public bool Glowing { get; private set; }

        void Awake()
        {
            if (volume != null) volume.weight = 0f;
        }

        public void Pulse()
        {
            if (volume == null) return;
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Glowing = true;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                volume.weight = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI); // in, then gently back out
                yield return null;
            }
            volume.weight = 0f;
            Glowing = false;
        }
    }
}
