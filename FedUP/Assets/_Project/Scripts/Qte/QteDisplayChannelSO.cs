using FedUp.Core;
using UnityEngine;

namespace FedUp.Qte
{
    public enum QteKind { Timing, Mash }

    public readonly struct QteDisplay
    {
        public readonly bool Visible;
        public readonly QteKind Kind;
        public readonly float Position, ZoneStart, ZoneEnd, Progress;
        public readonly string Label;

        public QteDisplay(bool visible, QteKind kind, float position, float zoneStart, float zoneEnd, float progress, string label)
        {
            Visible = visible; Kind = kind; Position = position; ZoneStart = zoneStart; ZoneEnd = zoneEnd; Progress = progress; Label = label;
        }

        public static readonly QteDisplay Hidden = new QteDisplay(false, QteKind.Timing, 0, 0, 0, 0, "");
    }

    /// <summary>Whatever quick-time challenge is on screen. One shared look for every QTE in the game.</summary>
    [CreateAssetMenu(menuName = "FedUp/Channels/QTE Display", fileName = "QteDisplay")]
    public class QteDisplayChannelSO : EventChannelSO<QteDisplay> { }
}
