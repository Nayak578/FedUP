using UnityEngine;
using UnityEngine.InputSystem;

namespace FedUp.Player
{
    /// <summary>
    /// Walk, sprint and look. No jumping. Every number comes from <see cref="PlayerTuningSO"/>.
    /// Autopilot and Play Mode tests drive it through the Virtual* properties instead of the keyboard.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMotor : MonoBehaviour
    {
        [SerializeField] PlayerTuningSO tuning;
        [SerializeField] Transform cameraPivot;
        [SerializeField] InputActionAsset controls;

        /// <summary>Cutscenes take control away: no walking or looking while locked.</summary>
        public bool Locked { get; set; }
        /// <summary>Lower the camera (e.g. lying in the bath). Null = normal eye height.</summary>
        public float? EyeHeightOverride { get; set; }
        public bool UseVirtualInput { get; set; }
        public Vector2 VirtualMove { get; set; }
        public bool VirtualSprint { get; set; }

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float CurrentSpeed => horizontalVelocity.magnitude;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public PlayerTuningSO Tuning => tuning;

        CharacterController controller;
        InputAction move, look, sprint;
        Vector3 horizontalVelocity;
        float verticalVelocity, bobPhase;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            ApplyBody();
            if (controls != null)
            {
                move = controls.FindAction("Player/Move", true);
                look = controls.FindAction("Player/Look", true);
                sprint = controls.FindAction("Player/Sprint", true);
            }
        }

        void OnEnable() => controls?.FindActionMap("Player", true).Enable();
        void OnDisable() => controls?.FindActionMap("Player", true).Disable();

        void Start()
        {
            var spawn = FindAnyObjectByType<SpawnPoint>();
            if (spawn != null) Teleport(spawn.transform.position, spawn.transform.eulerAngles.y);
            if (!UseVirtualInput) Cursor.lockState = CursorLockMode.Locked;
        }

        public void ApplyBody()
        {
            if (tuning == null || controller == null) return;
            controller.height = tuning.bodyHeight;
            controller.radius = tuning.bodyRadius;
            controller.center = new Vector3(0, tuning.bodyHeight * 0.5f, 0);
            controller.stepOffset = tuning.stepHeight;
            controller.slopeLimit = 45f;
            if (cameraPivot != null) cameraPivot.localPosition = new Vector3(0, tuning.eyeHeight, 0);
        }

        /// <summary>Place the player's feet at <paramref name="feet"/>, snapped down onto the floor below.</summary>
        public void Teleport(Vector3 feet, float yaw)
        {
            if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore))
                feet = hit.point + Vector3.up * 0.02f;
            controller.enabled = false;
            transform.position = feet;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            SetLook(yaw, 0f);
        }

        public void SetLook(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, -tuning.pitchLimit, tuning.pitchLimit);
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }

        void Update()
        {
            Vector2 moveInput = UseVirtualInput ? VirtualMove : move?.ReadValue<Vector2>() ?? Vector2.zero;
            bool sprinting = UseVirtualInput ? VirtualSprint : sprint != null && sprint.IsPressed();
            if (Locked) { moveInput = Vector2.zero; sprinting = false; }

            if (!Locked && !UseVirtualInput && look != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = look.ReadValue<Vector2>() * tuning.lookSensitivity;
                SetLook(Yaw + delta.x, Pitch - delta.y);
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            float speed = sprinting ? tuning.sprintSpeed : tuning.walkSpeed;
            Vector3 wish = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wish, tuning.acceleration * speed * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += tuning.gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            HeadBob();
        }

        void HeadBob()
        {
            if (cameraPivot == null) return;
            float t = tuning.walkSpeed > 0 ? CurrentSpeed / tuning.walkSpeed : 0f;
            if (IsGrounded && t > 0.05f) bobPhase += Time.deltaTime * tuning.headBobFrequency * Mathf.PI * 2f * Mathf.Min(t, 1.6f);
            float bob = Mathf.Sin(bobPhase) * tuning.headBobAmplitude * Mathf.Clamp01(t);
            cameraPivot.localPosition = new Vector3(0, (EyeHeightOverride ?? tuning.eyeHeight) + bob, 0);
        }
    }
}
