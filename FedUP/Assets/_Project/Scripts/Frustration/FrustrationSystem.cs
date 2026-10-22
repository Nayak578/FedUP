using System.Globalization;
using System.IO;
using UnityEngine;

namespace FedUp.Frustration
{
    /// <summary>
    /// The one owner of the frustration value. Applies requests, broadcasts the new state and writes the
    /// session log (GDD "Frustration Analytics Tracker Hooks") to persistentDataPath/sessions/*.csv.
    /// </summary>
    public class FrustrationSystem : MonoBehaviour
    {
        [SerializeField] FrustrationRequestChannelSO requests;
        [SerializeField] FrustrationChannelSO changed;
        [SerializeField] float startValue;
        [SerializeField] bool writeSessionLog = true;

        public FrustrationModel Model { get; private set; } = new FrustrationModel();
        public float Value => Model.Hidden;
        public string LogPath { get; private set; }

        float rateAccumulated, rateTimer;
        string rateSource;

        void Awake()
        {
            Model = new FrustrationModel(startValue);
            if (!writeSessionLog) return;
            string dir = Path.Combine(Application.persistentDataPath, "sessions");
            Directory.CreateDirectory(dir);
            LogPath = Path.Combine(dir, $"session_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(LogPath, "time_s,source,delta,hidden,display,band\n");
        }

        void OnEnable() { if (requests != null) requests.Raised += OnRequest; }
        void OnDisable() { if (requests != null) requests.Raised -= OnRequest; }

        void Start()
        {
            Publish(0f, "start");
            Log("start", 0f);
        }

        void OnRequest(FrustrationRequest r)
        {
            if (r.Event == null) return;
            float d = Model.Apply(r.Event.amount * r.Scale);
            if (r.Event.perSecond)
            {
                rateAccumulated += d;
                rateSource = r.Event.name;
            }
            else Log(r.Event.name, d);
            Publish(d, r.Event.name);
        }

        void Update()
        {
            if (Mathf.Approximately(rateAccumulated, 0f)) { rateTimer = 0f; return; }
            rateTimer += Time.deltaTime;
            if (rateTimer < 1f) return;
            Log(rateSource, rateAccumulated);
            rateAccumulated = 0f;
            rateTimer = 0f;
        }

        void OnDestroy()
        {
            if (!Mathf.Approximately(rateAccumulated, 0f)) Log(rateSource, rateAccumulated);
        }

        /// <summary>Tests and debug only: jump straight to a value.</summary>
        public void SetValue(float value)
        {
            Model = new FrustrationModel(value);
            Publish(0f, "set");
        }

        void Publish(float delta, string source)
        {
            if (changed != null) changed.Raise(new FrustrationState(Model.Hidden, Model.Display, Model.Band, delta, source));
        }

        void Log(string source, float delta)
        {
            if (LogPath == null) return;
            var c = CultureInfo.InvariantCulture;
            File.AppendAllText(LogPath, string.Format(c, "{0:F2},{1},{2:F2},{3:F2},{4:F2},{5}\n",
                Time.timeSinceLevelLoad, source, delta, Model.Hidden, Model.Display, Model.Band));
        }
    }
}
