using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// A placeholder cutscene: a list of simple steps (black caption cards, subtitles, camera look, tones).
    /// The art passes replace these with real cinematics, comic panels and absurd mini-scenes.
    /// </summary>
    [CreateAssetMenu(menuName = "FedUp/Story/Cutscene", fileName = "CS_New")]
    public class CutsceneSO : ScriptableObject
    {
        public enum StepKind { BlackCaption, Caption, Line, FadeIn, LookAt, Tone, Wait }

        [Serializable]
        public class Step
        {
            public StepKind kind;
            [Tooltip("Caption text, monologue line id, look-at object name, or tone name (choir / buzz / beep / click).")]
            [TextArea(1, 3)] public string text;
            [Min(0f)] public float seconds = 2f;

            public Step() { }
            public Step(StepKind kind, string text, float seconds) { this.kind = kind; this.text = text; this.seconds = seconds; }
        }

        public List<Step> steps = new List<Step>();
    }
}
