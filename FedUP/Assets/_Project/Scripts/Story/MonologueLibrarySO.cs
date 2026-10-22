using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedUp.Story
{
    /// <summary>
    /// Every line the man says to himself, in one editable list (Wolfenstein-style inner voice).
    /// Game code refers to lines by id, so the words can change without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "FedUp/Story/Monologue Library", fileName = "Monologue")]
    public class MonologueLibrarySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            [TextArea(1, 3)] public string text;
            [Min(0.5f)] public float seconds = 3f;
        }

        public SubtitleChannelSO subtitles;
        public List<Entry> lines = new List<Entry>();

        public bool TryGet(string id, out Entry entry)
        {
            entry = lines.Find(l => l.id == id);
            return entry != null;
        }

        /// <summary>Shows the line with this id. Unknown ids are shown as-is, so nothing is ever silently lost.</summary>
        public void Say(string id)
        {
            if (subtitles == null || string.IsNullOrEmpty(id)) return;
            if (TryGet(id, out var e)) subtitles.Say(e.text, e.seconds);
            else subtitles.Say(id);
        }
    }
}
