using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks.Appliances
{
    /// <summary>
    /// GDD Appliance Chaos (+ Task 6). One at a time, when the ghost director allows, an appliance switches itself
    /// on. Hold E on it within 10 s to shut it off, or it overloads (+10): the microwave screams, the blender
    /// sprays, the sink floods. Above 50% the ghost may cover the switch (+7): shine the torch on it and press E
    /// to scare it off (-5), then shut it off.
    /// </summary>
    public class ApplianceChaosTask : TaskBehaviour
    {
        [SerializeField] ApplianceTuningSO tuning;
        [SerializeField] TaskInteractable[] units = new TaskInteractable[0];
        [SerializeField] string[] unitNames = { "microwave", "blender", "bathroom sink" };
        [SerializeField] GameObject[] ghostHands = new GameObject[0];
        [SerializeField] ParticleSystem[] overloadFx = new ParticleSystem[0];
        [SerializeField] GhostDirector director;
        [SerializeField] PhoneTorch torch;
        [SerializeField] Interactor interactor;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] FrustrationChannelSO frustrationState;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] MonologueLibrarySO lines;

        public int Index { get; private set; }
        public ApplianceSession Session { get; private set; }
        public int ShutOffCount { get; private set; }
        public int Overloads { get; private set; }
        public int Blocks { get; private set; }
        public int Scares { get; private set; }
        /// <summary>Tests: the next activation's dice (ghost block).</summary>
        public float? ForcedRoll { get; set; }

        AudioSource[] hums;
        int scarePresses;
        bool saidNeedLight;

        public override string Objective =>
            Session != null && Session.Running ? $"The {unitNames[Index]} switched itself on: shut it off ({Mathf.CeilToInt(Session.TimeLeft)} s)" : objective;

        void Awake()
        {
            hums = new AudioSource[units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                units[i].available = false;
                hums[i] = units[i].gameObject.AddComponent<AudioSource>();
                hums[i].playOnAwake = false; hums[i].loop = true; hums[i].spatialBlend = 1f; hums[i].maxDistance = 20f; hums[i].volume = 0.8f;
                int k = i;
                units[i].Used += _ => OnUnit(k);
            }
            foreach (var h in ghostHands) if (h != null) h.SetActive(false);
            foreach (var fx in overloadFx) if (fx != null) fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void OnBegin() => Index = 0;

        void Update()
        {
            if (State != TaskState.Active) return;
            if (Session == null)
            {
                // the next one waits for the director, like any scare
                if (Index < units.Length && director.TryScare("appliance_" + unitNames[Index], 3, ScareSize.Small)) Activate();
                return;
            }
            Session.Tick(Time.deltaTime);
            if (Session.Running)
                qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, Session.TimeLeft / tuning.timerSeconds, $"{unitNames[Index].ToUpperInvariant()}!"));
            if (Session.FailedThisTick) Overload();
        }

        void Activate()
        {
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            Session = new ApplianceSession(tuning.timerSeconds, tuning.blockAbove, tuning.blockChance);
            Session.Start(f, ForcedRoll ?? Random.value);
            ForcedRoll = null;
            var unit = units[Index];
            hums[Index].clip = PlaceholderTones.Get("whine");
            hums[Index].Play();
            unit.available = true;
            scarePresses = 0;
            if (Session.Blocked)
            {
                Blocks++;
                frustration?.Request(tuning.blocked);
                if (Index < ghostHands.Length && ghostHands[Index] != null) ghostHands[Index].SetActive(true);
                unit.mode = InteractionMode.Press;
                unit.prompt = "Scare it off (torch on it, press E)";
            }
            else SwitchPrompt(unit);
            lines?.Say("appliance_on_" + Index);
        }

        void SwitchPrompt(TaskInteractable unit)
        {
            unit.mode = InteractionMode.Hold;
            unit.holdSeconds = tuning.holdSeconds;
            unit.prompt = "Switch it off";
        }

        void OnUnit(int i)
        {
            if (Session == null || i != Index || !Session.Running) return;
            if (Session.Blocked)
            {
                if (torch == null || !torch.Lit)
                {
                    if (!saidNeedLight) { saidNeedLight = true; lines?.Say("appliance_need_light"); }
                    return;
                }
                if (++scarePresses < tuning.scarePresses) return;
                Session.ScareOff();
                Scares++;
                frustration?.Request(tuning.scareOff);
                if (i < ghostHands.Length && ghostHands[i] != null) ghostHands[i].SetActive(false);
                SwitchPrompt(units[i]);
                lines?.Say("appliance_scared");
                return;
            }
            if (!Session.HoldOff()) return;
            ShutOffCount++;
            hums[i].PlayOneShot(PlaceholderTones.Get("click"));
            Next();
        }

        void Overload()
        {
            Overloads++;
            frustration?.Request(tuning.notShutOff);
            if (Index < overloadFx.Length && overloadFx[Index] != null) overloadFx[Index].Play();
            hums[Index].PlayOneShot(PlaceholderTones.Get("sting"));
            lines?.Say("appliance_overload");
            Next();
        }

        void Next()
        {
            hums[Index].Stop();
            units[Index].available = false;
            if (Index < ghostHands.Length && ghostHands[Index] != null) ghostHands[Index].SetActive(false);
            qte?.Raise(QteDisplay.Hidden);
            Session = null;
            Index++;
            if (Index >= units.Length) Complete();
        }

        public override void DebugComplete()
        {
            for (int i = 0; i < units.Length; i++) { units[i].available = false; if (hums != null && hums[i] != null) hums[i].Stop(); }
            foreach (var h in ghostHands) if (h != null) h.SetActive(false);
            qte?.Raise(QteDisplay.Hidden);
            Session = null;
            base.DebugComplete();
        }
    }
}
