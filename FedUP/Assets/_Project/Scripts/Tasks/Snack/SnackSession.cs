namespace FedUp.Tasks.Snack
{
    public enum GrabResult { Grabbed, FailedGrab, GhostHand, NotOpen }

    /// <summary>
    /// Grab a Snack rules (GDD Phase 3 continuing tasks + Task 3). Open the fridge and grab the snack. Above 30% the
    /// snack slides away if you look at it too long; grabbing while it slides is a failed grab (+5). Hover 3 s
    /// without grabbing and the fridge mocks you (+3, once). Grab after 10 s open and a ghost hand yanks it (QTE).
    /// Leave it open 15 s and the door slams shut.
    /// </summary>
    public sealed class SnackSession
    {
        readonly float slideAbove, grabWindow, slideSeconds, sarcasmAfter, handAfter, slamAfter;
        float lookTime, hoverTotal, slideLeft;
        bool mocked;

        public SnackSession(float slideAbove, float grabWindow, float slideSeconds, float sarcasmAfter, float handAfterOpen, float slamAfter)
        {
            this.slideAbove = slideAbove;
            this.grabWindow = grabWindow;
            this.slideSeconds = slideSeconds;
            this.sarcasmAfter = sarcasmAfter;
            handAfter = handAfterOpen;
            this.slamAfter = slamAfter;
        }

        public bool IsOpen { get; private set; }
        public float OpenFor { get; private set; }
        public int Slides { get; private set; }
        public bool Sliding => slideLeft > 0f;
        public bool SlidAwayThisTick { get; private set; }
        public bool SarcasmThisTick { get; private set; }
        public bool SlammedThisTick { get; private set; }

        public void Open()
        {
            IsOpen = true;
            OpenFor = 0f;
            lookTime = 0f;
            hoverTotal = 0f;
            slideLeft = 0f;
            mocked = false;
        }

        /// <param name="lookingAtSnack">True while the crosshair is on the snack.</param>
        public void Tick(float dt, bool lookingAtSnack, float frustration)
        {
            SlidAwayThisTick = SarcasmThisTick = SlammedThisTick = false;
            if (!IsOpen || dt <= 0f) return;
            OpenFor += dt;
            if (slideLeft > 0f) slideLeft -= dt;

            if (lookingAtSnack)
            {
                lookTime += dt;
                hoverTotal += dt;
                if (frustration > slideAbove && lookTime > grabWindow && !Sliding)
                {
                    Slides++;
                    SlidAwayThisTick = true;
                    slideLeft = slideSeconds;
                    lookTime = 0f;
                }
                if (!mocked && hoverTotal >= sarcasmAfter) { mocked = true; SarcasmThisTick = true; }
            }
            else lookTime = 0f;

            if (OpenFor >= slamAfter) { IsOpen = false; SlammedThisTick = true; }
        }

        public GrabResult Grab()
        {
            if (!IsOpen) return GrabResult.NotOpen;
            if (Sliding) return GrabResult.FailedGrab;
            return OpenFor >= handAfter ? GrabResult.GhostHand : GrabResult.Grabbed;
        }
    }
}
