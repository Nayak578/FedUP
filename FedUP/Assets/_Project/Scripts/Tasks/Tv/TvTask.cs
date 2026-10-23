using System.Collections;
using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FedUp.Tasks.Tv
{
    /// <summary>
    /// GDD Phase 1 + Task 2, Watch TV. Hold E on the couch to sit; while sitting, E is the remote (next channel)
    /// and moving stands you up. Bad channels cost (+7 static/horror, +5 ads; the TV adds +10 at most). Ten
    /// presses in five seconds and the batteries roll under the couch (+10): stand, hold E at the couch front to
    /// find them. Above 50% a ghost hand may grab the remote: mash E to win the tug of war, or it ends up at the
    /// foot of the TV (+5). Ten seconds on the calm channel (old home video of him as a kid) wins (-10).
    /// </summary>
    public class TvTask : TaskBehaviour
    {
        [SerializeField] TvTuningSO tuning;
        [SerializeField] TaskInteractable couch;
        [SerializeField] TaskInteractable underCouch;
        [SerializeField] TaskInteractable remoteOnFloor;
        [SerializeField] Transform seat;
        [SerializeField] Transform standSpot;
        [SerializeField] Transform screenLookAt;
        [SerializeField] Renderer screen;
        [SerializeField] Light screenLight;
        [SerializeField] GameObject remoteOnCouch;
        [SerializeField] GameObject batteries;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] InputActionAsset controls;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] SubtitleChannelSO subtitles;
        [SerializeField] MonologueLibrarySO lines;

        public TvSession Session { get; private set; }
        public bool Seated { get; private set; }
        public bool Tugging => tug != null;
        public int BatteryLosses { get; private set; }
        public int RemoteThefts { get; private set; }
        public int ChannelChanges { get; private set; }

        /// <summary>Tests/autopilot: fixed dice for the next presses (channel roll, then ghost roll).</summary>
        public float? ForcedChannelRoll { get; set; }
        public float? ForcedStealRoll { get; set; }

        InputAction interact, move;
        bool virtualPress, virtualStand;
        MashChallenge tug;
        AudioSource tvAudio, sfx;
        MaterialPropertyBlock block;
        bool firstSit = true;

        static readonly string[] AdCaptions =
        {
            "[ AD:  'FocusMax! Work twenty hours. Feel nothing. Ask your manager!' ]",
            "[ AD:  'Lonely? Our AI friend never logs off. Never sleeps. Never leaves.' ]",
        };
        static readonly string[] HorrorCaptions =
        {
            "[ NEWS:  '…another forty thousand jobs handed to AI this week. Experts say: adapt.' ]",
            "[ …whispering…   it's saying your name… ]",
        };
        int adIndex, horrorIndex;

        void Awake()
        {
            if (controls != null)
            {
                interact = controls.FindAction("Player/Interact", true);
                move = controls.FindAction("Player/Move", true);
            }
            if (couch != null) { couch.available = false; couch.mode = InteractionMode.Hold; }
            if (underCouch != null) underCouch.available = false;
            if (remoteOnFloor != null) { remoteOnFloor.available = false; remoteOnFloor.gameObject.SetActive(false); }
            if (batteries != null) batteries.SetActive(false);
            tvAudio = (screen != null ? screen.gameObject : gameObject).AddComponent<AudioSource>(); // the sound comes from the set
            tvAudio.playOnAwake = false;
            tvAudio.loop = true;
            tvAudio.spatialBlend = 0.7f;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            block = new MaterialPropertyBlock();
            ShowChannel(TvChannel.Off);
        }

        protected override void OnBegin()
        {
            Session = new TvSession(tuning.channelOdds, tuning.spamPresses, tuning.spamWindowSeconds, tuning.stealAbove,
                tuning.stealChance, tuning.calmSecondsToWin, tuning.positiveCap);
            couch.holdSeconds = tuning.sitHoldSeconds;
            couch.available = true;
            couch.Used += _ => SitDown();
            underCouch.holdSeconds = tuning.searchHoldSeconds;
            underCouch.Used += _ => FindBatteries();
            remoteOnFloor.Used += _ => PickUpRemote();
        }

        Transform ScreenPoint => screenLookAt != null ? screenLookAt : screen.transform;

        /// <summary>Autopilot/tests: press the remote button (or mash during the tug of war) once.</summary>
        public void VirtualPress() => virtualPress = true;
        /// <summary>Autopilot/tests: stand up from the couch.</summary>
        public void VirtualStand() => virtualStand = true;

        void SitDown()
        {
            if (Seated || State != TaskState.Active) return;
            Seated = true;
            couch.available = false;
            motor.Locked = true;
            interactor.enabled = false;
            motor.Teleport(seat.position, seat.eulerAngles.y);
            motor.EyeHeightOverride = tuning.sitEyeHeight;
            Vector3 d = ScreenPoint.position - (seat.position + Vector3.up * tuning.sitEyeHeight);
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
            if (Session.HasRemote && remoteOnCouch != null) remoteOnCouch.SetActive(false);
            if (firstSit) { firstSit = false; lines?.Say("tv_sit"); }
            virtualPress = false;
        }

        void StandUp()
        {
            if (!Seated) return;
            Seated = false;
            qte?.Raise(QteDisplay.Hidden);
            motor.EyeHeightOverride = null;
            motor.Teleport(standSpot.position, standSpot.eulerAngles.y);
            motor.Locked = false;
            interactor.enabled = true;
            if (State == TaskState.Active) couch.available = true;
        }

        void Update()
        {
            if (Session == null) return;
            bool pressed = virtualPress || (interact != null && interact.WasPressedThisFrame());
            virtualPress = false;
            bool standing = virtualStand || (!motor.UseVirtualInput && move != null && move.ReadValue<Vector2>().magnitude > 0.6f);
            virtualStand = false;

            if (tug != null) { TickTug(pressed); return; }
            if (!Seated) return;

            if (standing) { StandUp(); return; }

            if (State == TaskState.Active && pressed) OnRemotePress();

            Session.Tick(Time.deltaTime, Seated);
            if (Session.WonThisTick) Win();
            if (Seated && State == TaskState.Active)
            {
                float calm = Session.Channel == TvChannel.Calm ? Session.CalmHeld / tuning.calmSecondsToWin : 0f;
                qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, calm, "E  NEXT CHANNEL      ·      MOVE TO STAND UP"));
            }
        }

        void OnRemotePress()
        {
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            float cRoll = ForcedChannelRoll ?? Random.value;
            float sRoll = ForcedStealRoll ?? Random.value;
            ForcedChannelRoll = null;
            ForcedStealRoll = null;
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
            switch (Session.Press(Time.time, cRoll, f, sRoll))
            {
                case TvPressResult.Changed:
                    ChannelChanges++;
                    ShowChannel(Session.Channel);
                    if (Session.Channel == TvChannel.Static || Session.Channel == TvChannel.Horror) Charge(tuning.staticOrHorror);
                    else if (Session.Channel == TvChannel.Ad) Charge(tuning.infomercial);
                    break;
                case TvPressResult.BatteriesOut:
                    StartCoroutine(BatteriesOut());
                    break;
                case TvPressResult.Stolen:
                    StartTug();
                    break;
            }
        }

        void Charge(FrustrationEventSO ev)
        {
            if (ev == null || frustration == null || Mathf.Approximately(ev.amount, 0f)) return;
            float applied = Session.Charge(ev.amount);
            if (Mathf.Abs(applied) > 1e-4f) frustration.Request(ev, applied / ev.amount);
        }

        IEnumerator BatteriesOut()
        {
            BatteryLosses++;
            Charge(tuning.lostRemote);
            sfx.PlayOneShot(PlaceholderTones.Get("pop"));
            subtitles?.Say("You lost the remote again…", 2.4f, SubtitleStyle.Caption); // GDD on-screen prompt
            if (batteries != null) batteries.SetActive(true);
            underCouch.available = true;
            yield return new WaitForSeconds(0.25f);
            sfx.PlayOneShot(PlaceholderTones.Get("pop"));
            lines?.Say("tv_batteries");
            StandUp();
        }

        void FindBatteries()
        {
            if (Session.HasRemote) return;
            underCouch.available = false;
            if (batteries != null) batteries.SetActive(false);
            Session.RemoteRecovered();
            lines?.Say("tv_found_batteries");
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
        }

        void StartTug()
        {
            tug = new MashChallenge(tuning.tugPresses, tuning.tugSeconds);
            sfx.PlayOneShot(PlaceholderTones.Get("thunk"));
            subtitles?.Say("[ something under the couch grabs the remote ]", 2.2f, SubtitleStyle.Caption);
        }

        void TickTug(bool pressed)
        {
            if (pressed) tug.Press();
            tug.Tick(Time.deltaTime);
            qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, tug.Progress, "TUG OF WAR!   MASH  E"));
            if (!tug.Finished) return;
            bool won = tug.Succeeded;
            tug = null;
            qte?.Raise(QteDisplay.Hidden);
            if (won) { lines?.Say("tv_tug_won"); return; }
            RemoteThefts++;
            Charge(tuning.remoteStolen);
            Session.RemoteLost();
            sfx.PlayOneShot(PlaceholderTones.Get("giggle"));
            remoteOnFloor.gameObject.SetActive(true);
            remoteOnFloor.available = true;
            lines?.Say("tv_tug_lost");
            StandUp();
        }

        void PickUpRemote()
        {
            if (Session.HasRemote) return;
            remoteOnFloor.available = false;
            remoteOnFloor.gameObject.SetActive(false);
            Session.RemoteRecovered();
            sfx.PlayOneShot(PlaceholderTones.Get("click"));
        }

        void Win()
        {
            frustration?.Request(tuning.calm, Session.Charge(tuning.calm.amount) / tuning.calm.amount);
            lines?.Say("tv_calm_win");
            qte?.Raise(QteDisplay.Hidden);
            couch.available = false;
            Complete();
        }

        void ShowChannel(TvChannel ch)
        {
            Color c; float light; string tone; string caption = null;
            switch (ch)
            {
                case TvChannel.Static: c = new Color(0.75f, 0.75f, 0.78f); light = 1.4f; tone = "static"; caption = "[ static ]"; break;
                case TvChannel.Ad: c = new Color(1f, 0.55f, 0.15f); light = 1.6f; tone = "jingle"; caption = AdCaptions[adIndex++ % AdCaptions.Length]; break;
                case TvChannel.Horror: c = new Color(0.55f, 0.08f, 0.1f); light = 0.9f; tone = "whisper"; caption = HorrorCaptions[horrorIndex++ % HorrorCaptions.Length]; break;
                case TvChannel.Calm: c = new Color(0.95f, 0.82f, 0.55f); light = 1.1f; tone = "lullaby"; caption = "[ HOME VIDEO:  a kid on a red bike, laughing at whoever's holding the camera ]"; break;
                default: c = new Color(0.02f, 0.02f, 0.03f); light = 0f; tone = null; break;
            }
            if (screen != null)
            {
                screen.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                block.SetColor("_EmissionColor", c * 0.45f); // enough to glow, not so much it blows out to white
                screen.SetPropertyBlock(block);
            }
            if (screenLight != null) { screenLight.color = c; screenLight.intensity = light; screenLight.enabled = light > 0f; }
            if (tvAudio != null)
            {
                if (tone == null) tvAudio.Stop();
                else { tvAudio.clip = PlaceholderTones.Get(tone); tvAudio.Play(); }
            }
            if (caption != null) subtitles?.Say(caption, 3f, SubtitleStyle.Caption);
        }

        /// <summary>The TV changes channel by itself (the sleep scene: it turns to static while he dozes).</summary>
        public void ForceChannel(TvChannel ch)
        {
            if (Session != null && Session.Channel == ch) return;
            ShowChannel(ch);
            ForcedChannel = ch;
        }

        public TvChannel? ForcedChannel { get; private set; }
        public Light ScreenLight => screenLight;

        /// <summary>The sound warps as he falls asleep (1 = normal).</summary>
        public void SetSoundPitch(float pitch) { if (tvAudio != null) tvAudio.pitch = pitch; }

        /// <summary>The screen colour right now (tests check the channel is visible).</summary>
        public Color ScreenColor
        {
            get { if (screen == null) return Color.black; screen.GetPropertyBlock(block); return block.GetColor("_BaseColor"); }
        }

        public override void DebugComplete()
        {
            tug = null;
            StandUp();
            if (couch != null) couch.available = false;
            if (underCouch != null) underCouch.available = false;
            if (remoteOnFloor != null) { remoteOnFloor.available = false; remoteOnFloor.gameObject.SetActive(false); }
            base.DebugComplete();
        }
    }
}
