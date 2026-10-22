using FedUp.Qte;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.UI
{
    /// <summary>The shared QTE bar: a golden zone and a sliding marker (timing) or a filling bar (mash).</summary>
    public class QteBarUI : MonoBehaviour
    {
        [SerializeField] QteDisplayChannelSO channel;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform zone;
        [SerializeField] RectTransform marker;
        [SerializeField] RectTransform mashFill;
        [SerializeField] Text label;

        public bool Visible => root != null && root.activeSelf;

        void OnEnable()
        {
            if (channel == null) return;
            channel.Raised += Show;
            Show(QteDisplay.Hidden);
        }

        void OnDisable() { if (channel != null) channel.Raised -= Show; }

        void Show(QteDisplay d)
        {
            if (root == null) return;
            root.SetActive(d.Visible);
            if (!d.Visible) return;
            bool timing = d.Kind == QteKind.Timing;
            zone.gameObject.SetActive(timing);
            marker.gameObject.SetActive(timing);
            mashFill.gameObject.SetActive(!timing);
            if (timing)
            {
                zone.anchorMin = new Vector2(d.ZoneStart, 0f);
                zone.anchorMax = new Vector2(d.ZoneEnd, 1f);
                marker.anchorMin = new Vector2(d.Position, -0.3f);
                marker.anchorMax = new Vector2(d.Position, 1.3f);
            }
            else mashFill.anchorMax = new Vector2(Mathf.Clamp01(d.Progress), 1f);
            if (label != null) label.text = d.Label;
        }
    }
}
