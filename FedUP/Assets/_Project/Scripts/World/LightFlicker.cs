using UnityEngine;

namespace FedUp.World
{
    /// <summary>A failing bulb: mostly steady, with short irregular dips. Respects the light switch (only flickers while on).</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float minGap = 1.5f;
        [SerializeField, Min(0.1f)] float maxGap = 6f;
        [SerializeField, Range(0f, 1f)] float dipTo = 0.15f;

        Light lamp;
        float baseIntensity, nextDip, dipEnds;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            nextDip = Time.time + Random.Range(minGap, maxGap);
        }

        /// <summary>Phase changes set a new steady brightness.</summary>
        public void SetBase(float intensity) { baseIntensity = intensity; lamp.intensity = intensity; }

        void Update()
        {
            if (Time.time >= nextDip)
            {
                dipEnds = Time.time + Random.Range(0.05f, 0.25f);
                nextDip = dipEnds + Random.Range(minGap, maxGap);
            }
            lamp.intensity = Time.time < dipEnds ? baseIntensity * dipTo : baseIntensity;
        }
    }
}
