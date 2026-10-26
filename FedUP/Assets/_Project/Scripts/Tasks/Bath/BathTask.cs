using FedUp.Audio;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FedUp.Tasks.Bath
{
    /// <summary>
    /// GDD Phase 1, Take a Bath. Hold E on the tub to get in; keep holding E to relax. For as long as you lie
    /// there, the ghost keeps snatching at the clothes on the hooks: press the shown direction (W A S D) in time
    /// to snatch each back. Each lost item +6 and "...Not again."; a perfect bath -10.
    /// </summary>
    public class BathTask : TaskBehaviour
    {
        [SerializeField] BathTuningSO tuning;
        [SerializeField] TaskInteractable tub;
        [SerializeField] Transform bathSpot;
        [SerializeField] Transform lookAt;
        [SerializeField] Transform[] clothes = new Transform[0];
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] InputActionAsset controls;
        [SerializeField] FrustrationRequestChannelSO frustration;
        [SerializeField] QteDisplayChannelSO qte;
        [SerializeField] MonologueLibrarySO lines;

        public bool Bathing { get; private set; }
        public BathSession Session { get; private set; }
        /// <summary>Autopilot/tests: hold the relax button.</summary>
        public bool VirtualRelaxHeld { get; set; }

        InputAction move, interact;
        BathDirection? virtualPress;
        bool directionWasDown;
        Vector3 exitFeet;
        float exitYaw;
        Vector3[] clothesHome;
        AudioSource sfx;

        void Awake()
        {
            if (tub != null) tub.available = false;
            if (controls != null)
            {
                move = controls.FindAction("Player/Move", true);
                interact = controls.FindAction("Player/Interact", true);
            }
            clothesHome = new Vector3[clothes.Length];
            for (int i = 0; i < clothes.Length; i++) clothesHome[i] = clothes[i].position;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
        }

        protected override void OnBegin()
        {
            tub.available = true;
            tub.Used += _ => GetIn();
        }

        /// <summary>Autopilot/tests: press a direction once.</summary>
        public void VirtualPress(BathDirection d) => virtualPress = d;

        void GetIn()
        {
            if (Bathing || State != TaskState.Active) return;
            tub.available = false;
            Session = new BathSession(tuning.relaxSeconds, clothes.Length, tuning.firstSnatchAt, tuning.snatchEvery, tuning.directions, tuning.snatchWindow);
            Bathing = true;
            exitFeet = motor.transform.position;
            exitYaw = motor.Yaw;
            motor.Locked = true;
            interactor.enabled = false;
            motor.Teleport(bathSpot.position, bathSpot.eulerAngles.y);
            motor.EyeHeightOverride = tuning.bathEyeHeight;
            Vector3 d = lookAt.position - (bathSpot.position + Vector3.up * tuning.bathEyeHeight);
            motor.SetLook(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
            lines?.Say("bath_start");
        }

        void Update()
        {
            if (!Bathing) return;
            bool held = VirtualRelaxHeld || (interact != null && interact.IsPressed());
            int sliding = Session.Prompt.HasValue ? Session.Prompt.Value.Item : -1;
            Session.Tick(Time.deltaTime, held);

            if (Session.LostThisTick > 0 && sliding >= 0)
            {
                clothes[sliding].gameObject.SetActive(false);
                frustration?.Request(tuning.itemLost);
                lines?.Say("cloth_lost");
            }

            var dir = ReadDirection();
            if (dir.HasValue && Session.Prompt.HasValue)
            {
                int item = Session.Prompt.Value.Item;
                if (Session.Press(dir.Value))
                {
                    clothes[item].position = clothesHome[item];
                    sfx.PlayOneShot(PlaceholderTones.Get("pop"));
                }
            }

            if (Session.Prompt.HasValue)
            {
                var p = Session.Prompt.Value;
                // the sliding item creeps towards the edge of the hook
                float k = 1f - p.TimeLeft / p.Window;
                clothes[p.Item].position = clothesHome[p.Item] + Vector3.down * 0.35f * k + clothes[p.Item].right * 0.25f * k;
                qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, p.TimeLeft / p.Window, $"SNATCH IT!   PRESS  {KeyFor(p.Direction)}"));
            }
            else qte?.Raise(new QteDisplay(true, QteKind.Mash, 0, 0, 0, Session.Relax, "HOLD  E  TO RELAX"));

            if (Session.Finished) GetOut();
        }

        BathDirection? ReadDirection()
        {
            if (virtualPress.HasValue) { var v = virtualPress; virtualPress = null; return v; }
            if (move == null) return null;
            Vector2 m = move.ReadValue<Vector2>();
            if (m.magnitude <= 0.6f) { directionWasDown = false; return null; }
            if (directionWasDown) return null;
            directionWasDown = true;
            return Mathf.Abs(m.x) > Mathf.Abs(m.y) ? (m.x > 0 ? BathDirection.Right : BathDirection.Left)
                                                   : (m.y > 0 ? BathDirection.Up : BathDirection.Down);
        }

        void GetOut()
        {
            Bathing = false;
            qte?.Raise(QteDisplay.Hidden);
            if (Session.Perfect) { frustration?.Request(tuning.success); lines?.Say("bath_perfect"); }
            else lines?.Say("bath_lost");
            motor.EyeHeightOverride = null;
            motor.Teleport(exitFeet, exitYaw);
            motor.Locked = false;
            interactor.enabled = true;
            Complete();
        }

        public override void DebugComplete()
        {
            if (Bathing) { Bathing = false; qte?.Raise(QteDisplay.Hidden); motor.EyeHeightOverride = null; motor.Locked = false; interactor.enabled = true; }
            tub.available = false;
            base.DebugComplete();
        }

        static string KeyFor(BathDirection d) => d switch
        {
            BathDirection.Up => "W  ↑",
            BathDirection.Down => "S  ↓",
            BathDirection.Left => "A  ←",
            _ => "D  →",
        };
    }
}
