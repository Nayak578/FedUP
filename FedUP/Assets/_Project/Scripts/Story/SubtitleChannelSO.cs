using FedUp.Core;
using UnityEngine;

namespace FedUp.Story
{
    public enum SubtitleStyle { Monologue, Caption }

    public readonly struct SubtitleLine
    {
        public readonly string Text;
        public readonly float Seconds;
        public readonly SubtitleStyle Style;
        public SubtitleLine(string text, float seconds, SubtitleStyle style) { Text = text; Seconds = seconds; Style = style; }
    }

    [CreateAssetMenu(menuName = "FedUp/Channels/Subtitles", fileName = "Subtitles")]
    public class SubtitleChannelSO : EventChannelSO<SubtitleLine>
    {
        public void Say(string text, float seconds = 3f, SubtitleStyle style = SubtitleStyle.Monologue)
            => Raise(new SubtitleLine(text, seconds, style));
    }
}
