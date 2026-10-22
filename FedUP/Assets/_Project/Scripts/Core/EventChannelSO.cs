using System;
using UnityEngine;

namespace FedUp.Core
{
    /// <summary>
    /// A shared "radio channel" asset. Systems raise values on it; listeners subscribe without knowing who sent them.
    /// Remembers the last value so late listeners (e.g. a HUD enabled after the fact) can catch up.
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> Raised;
        public T Last { get; private set; }

        public void Raise(T value)
        {
            Last = value;
            Raised?.Invoke(value);
        }

        void OnDisable() => Last = default;
    }
}
