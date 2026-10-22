namespace FedUp.Frustration
{
    public enum FrustrationBand { Calm, Uneasy, Agitated, Breaking, FedUp }

    /// <summary>
    /// Pure frustration rules (CLAUDE.md): bands at 30 / 60 / 90 / 100. The bar shows at most 100,
    /// the hidden value keeps counting to 200, so calming down after overshooting takes longer.
    /// </summary>
    public sealed class FrustrationModel
    {
        public const float DisplayMax = 100f;
        public const float HiddenMax = 200f;

        public FrustrationModel(float start = 0f) => Hidden = Clamp(start);

        public float Hidden { get; private set; }
        public float Display => Hidden > DisplayMax ? DisplayMax : Hidden;
        public FrustrationBand Band => BandFor(Hidden);

        public static FrustrationBand BandFor(float value)
        {
            if (value >= 100f) return FrustrationBand.FedUp;
            if (value >= 90f) return FrustrationBand.Breaking;
            if (value >= 60f) return FrustrationBand.Agitated;
            if (value >= 30f) return FrustrationBand.Uneasy;
            return FrustrationBand.Calm;
        }

        /// <summary>Adds <paramref name="delta"/> (negative calms). Returns the change that actually happened.</summary>
        public float Apply(float delta)
        {
            float before = Hidden;
            Hidden = Clamp(Hidden + delta);
            return Hidden - before;
        }

        /// <summary>For ongoing penalties like the smoke alarm (+3 per second).</summary>
        public float ApplyRate(float perSecond, float seconds) => Apply(perSecond * seconds);

        static float Clamp(float v) => v < 0f ? 0f : v > HiddenMax ? HiddenMax : v;
    }
}
