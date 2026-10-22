using FedUp.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace FedUp.UI
{
    /// <summary>Crosshair dot plus "E — Light switch" under it. Listens to the focus channel only.</summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] InteractionFocusChannelSO focusChannel;
        [SerializeField] InteractionSettingsSO settings;
        [SerializeField] Text promptText;
        [SerializeField] Graphic crosshair;
        [SerializeField] Color idleCrosshair = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color activeCrosshair = new Color(1f, 0.9f, 0.6f, 0.95f);

        public string CurrentText => promptText != null ? promptText.text : null;

        void OnEnable()
        {
            if (focusChannel == null) return;
            focusChannel.Raised += Show;
            Show(focusChannel.Last);
        }

        void OnDisable()
        {
            if (focusChannel != null) focusChannel.Raised -= Show;
        }

        void Show(InteractionFocus f)
        {
            string key = settings != null ? settings.keyLabel : "E";
            if (promptText != null)
            {
                if (!f.HasTarget) promptText.text = "";
                else if (f.Mode == InteractionMode.Hold)
                    promptText.text = $"Hold {key} — {f.Prompt}  {Mathf.RoundToInt(f.Progress * 100)}%";
                else promptText.text = $"{key} — {f.Prompt}";
            }
            if (crosshair != null) crosshair.color = f.HasTarget ? activeCrosshair : idleCrosshair;
        }
    }
}
