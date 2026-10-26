namespace FedUp.Tasks.Appliances
{
    /// <summary>
    /// One haunted appliance (GDD Task 6). It switches itself on: hold to shut it off within 10 s or it overloads
    /// (+10). Above 50% the ghost may cover the switch (+7): scare it off with the torch first (-5).
    /// </summary>
    public sealed class ApplianceSession
    {
        readonly float timer, blockAbove, blockChance;

        public ApplianceSession(float timerSeconds, float blockAbove, float blockChance)
        {
            timer = timerSeconds;
            this.blockAbove = blockAbove;
            this.blockChance = blockChance;
        }

        public bool Running { get; private set; }
        public bool Blocked { get; private set; }
        public bool ShutOff { get; private set; }
        public bool Failed { get; private set; }
        public bool FailedThisTick { get; private set; }
        public float TimeLeft { get; private set; }

        /// <param name="roll">0..1 from the caller; decides a ghost block when frustration is high.</param>
        public void Start(float frustration, float roll)
        {
            Running = true;
            TimeLeft = timer;
            Blocked = frustration > blockAbove && roll < blockChance;
        }

        public void Tick(float dt)
        {
            FailedThisTick = false;
            if (!Running || dt <= 0f) return;
            TimeLeft -= dt;
            if (TimeLeft > 0f) return;
            Running = false;
            Blocked = false;
            Failed = true;
            FailedThisTick = true;
        }

        /// <summary>The hold completed. False if the ghost is covering the switch.</summary>
        public bool HoldOff()
        {
            if (!Running || Blocked) return false;
            Running = false;
            ShutOff = true;
            return true;
        }

        /// <summary>The torch scare worked: the switch is free.</summary>
        public void ScareOff() => Blocked = false;
    }
}
