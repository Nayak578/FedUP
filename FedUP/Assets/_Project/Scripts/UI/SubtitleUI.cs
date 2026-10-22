using FedUp.Story;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.UI
{
    /// <summary>Bottom-of-screen inner voice. The newest line replaces the old one, then fades.</summary>
    public class SubtitleUI : MonoBehaviour
    {
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] Text text;

        public string CurrentText => text != null && text.color.a > 0.01f ? text.text : "";
        /// <summary>Last line shown, even after it faded (tests).</summary>
        public string LastText { get; private set; } = "";

        float hold;

        void OnEnable() { if (subtitles != null) subtitles.Raised += Show; }
        void OnDisable() { if (subtitles != null) subtitles.Raised -= Show; }

        void Show(SubtitleLine line)
        {
            if (text == null) return;
            text.text = line.Text;
            text.fontStyle = line.Style == SubtitleStyle.Monologue ? FontStyle.Italic : FontStyle.Normal;
            LastText = line.Text;
            hold = line.Seconds;
            SetAlpha(1f);
        }

        void Update()
        {
            if (text == null) return;
            hold -= Time.unscaledDeltaTime;
            if (hold < 0f) SetAlpha(Mathf.Clamp01(1f + hold / 0.4f));
        }

        void SetAlpha(float a)
        {
            var c = text.color; c.a = a; text.color = c;
        }
    }
}
