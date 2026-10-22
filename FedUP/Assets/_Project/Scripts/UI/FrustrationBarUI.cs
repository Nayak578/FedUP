using FedUp.Frustration;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.UI
{
    /// <summary>The "FED UP" meter: fills to 100%, changes colour by band, flashes the size of each change.</summary>
    public class FrustrationBarUI : MonoBehaviour
    {
        [SerializeField] FrustrationChannelSO changed;
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Text label;
        [SerializeField] Text deltaText;
        [SerializeField] float smoothing = 6f;

        public float TargetFraction { get; private set; }
        public FrustrationBand Band { get; private set; }
        public string LabelText => label != null ? label.text : null;

        float shown, deltaTimer;

        static readonly Color Calm = new Color(0.45f, 0.75f, 0.45f);
        static readonly Color Uneasy = new Color(0.9f, 0.8f, 0.35f);
        static readonly Color Agitated = new Color(0.95f, 0.5f, 0.2f);
        static readonly Color Breaking = new Color(0.9f, 0.18f, 0.15f);
        static readonly Color FedUpRed = new Color(0.6f, 0.02f, 0.05f);

        void OnEnable()
        {
            if (changed == null) return;
            changed.Raised += Show;
            Show(changed.Last);
        }

        void OnDisable()
        {
            if (changed != null) changed.Raised -= Show;
        }

        void Show(FrustrationState s)
        {
            TargetFraction = s.Display / 100f;
            Band = s.Band;
            if (label != null) label.text = $"FED UP  {Mathf.RoundToInt(s.Display)}%   {BandName(s.Band)}";
            if (deltaText != null && Mathf.Abs(s.Delta) >= 1f)
            {
                deltaText.text = (s.Delta > 0 ? "+" : "") + Mathf.RoundToInt(s.Delta);
                deltaText.color = s.Delta > 0 ? Breaking : Calm;
                deltaTimer = 1.4f;
            }
        }

        void Update()
        {
            shown = Mathf.Lerp(shown, TargetFraction, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(shown), 1f);
            if (fillImage != null)
            {
                var c = ColorFor(Band);
                if (Band == FrustrationBand.FedUp) c = Color.Lerp(c, Color.black, 0.35f * (1f + Mathf.Sin(Time.unscaledTime * 6f)));
                fillImage.color = c;
            }
            if (deltaText != null)
            {
                deltaTimer -= Time.unscaledDeltaTime;
                var col = deltaText.color;
                col.a = Mathf.Clamp01(deltaTimer);
                deltaText.color = col;
            }
        }

        static Color ColorFor(FrustrationBand b) => b switch
        {
            FrustrationBand.Calm => Calm,
            FrustrationBand.Uneasy => Uneasy,
            FrustrationBand.Agitated => Agitated,
            FrustrationBand.Breaking => Breaking,
            _ => FedUpRed
        };

        static string BandName(FrustrationBand b) => b switch
        {
            FrustrationBand.Calm => "calm",
            FrustrationBand.Uneasy => "uneasy",
            FrustrationBand.Agitated => "agitated",
            FrustrationBand.Breaking => "breaking",
            _ => "FED UP"
        };
    }
}
