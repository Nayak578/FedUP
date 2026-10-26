using System.Collections;
using System.Linq;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Ghost.Scares;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Bath;
using FedUp.Tasks.Cat;
using FedUp.Tasks.Cleanup;
using FedUp.Tasks.Appliances;
using FedUp.Tasks.Calm;
using FedUp.Tasks.Night;
using FedUp.Tasks.Snack;
using FedUp.Tasks.Tea;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Toast;
using FedUp.Tasks.Tv;
using FedUp.World;
using UnityEngine;
using UnityEngine.AI;

namespace FedUp.DebugTools
{
    /// <summary>
    /// Hands-free play for evidence videos and build checks. Runs only when the game is launched with
    /// "-autopilot" (or when a test sets <see cref="forceRun"/>).
    /// "-autopilot" plays the newest gate (4: snack, juice box, radio, Make Tea, appliance chaos). "-autopilot-gate3"
    /// plays sleep and the Phase 2 wake-up with its scares. "-autopilot-gate2b" plays
    /// TV, the boss call and the cat; "-autopilot-gate2a" bath, clean-up and the call; "-autopilot-gate1" the porch,
    /// drop-things and toast; "-autopilot-tour" walks every room.
    /// </summary>
    public class Autopilot : MonoBehaviour
    {
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] LightSwitch firstSwitch;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float legTimeout = 25f;
        public bool forceRun;
        public bool forceTour;
        public bool quitWhenDone = true;

        public bool Finished { get; private set; }
        public int RoomsVisited { get; private set; }
        public int RoomsTotal { get; private set; }
        public int SwitchToggles { get; private set; }

        float frameTimeSum; int frameCount; float worstFrame;

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool tour = forceTour || args.Contains("-autopilot-tour");
            bool gate1 = args.Contains("-autopilot-gate1");
            bool gate2a = args.Contains("-autopilot-gate2a");
            bool gate2b = args.Contains("-autopilot-gate2b");
            bool gate3 = args.Contains("-autopilot-gate3");
            bool demo = forceRun || args.Contains("-autopilot");
            if (!tour && !gate1 && !gate2a && !gate2b && !gate3 && !demo) { enabled = false; return; }
            motor.UseVirtualInput = true;
            if (firstSwitch != null) firstSwitch.Toggled += _ => SwitchToggles++;
            StartCoroutine(tour ? Tour() : gate1 ? Gate1Demo() : gate2a ? Gate2aDemo() : gate2b ? Gate2bDemo() : gate3 ? Gate3Demo() : Gate4Demo());
        }

        void Update()
        {
            if (Time.timeSinceLevelLoad < 2f || Finished) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            frameTimeSum += ms; frameCount++; worstFrame = Mathf.Max(worstFrame, ms);
        }

        // ------------------------------------------------------------------ Gate 1 demo

        IEnumerator Gate1Demo()
        {
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            var frustration = FindAnyObjectByType<FrustrationSystem>();
            var toast = FindAnyObjectByType<MakeToastTask>();
            var alarm = FindAnyObjectByType<SmokeAlarm>();
            var drop = FindAnyObjectByType<DropThingsTask>();
            yield return new WaitForSeconds(0.3f);
            yield return WaitForCutscene(cutscenes);
            Debug.Log("AUTOPILOT START gate1");

            yield return Use("front_door");
            yield return WalkTo(RoomPos("ROOM_Entrance"), "inside");
            yield return Use("coat_hook");
            yield return Use("shoe_rack");
            yield return Use("bag_table", hold: 1.4f);
            yield return WaitForCutscene(cutscenes);
            Debug.Log($"AUTOPILOT DROPTHINGS done={drop != null && drop.State == TaskState.Done} fileFound={drop != null && drop.FileFound}");

            if (firstSwitch != null) yield return UseObject(firstSwitch.transform, 0f, () => interactor.VirtualPress());

            // 1st toast: too late, burns
            yield return Use("toaster");
            yield return PressWhenMarker(toast, 0.93f);
            yield return new WaitUntil(() => toast.Toaster == ToasterState.Idle);
            // 2nd toast: too early, launches, and two failures set off the alarm
            yield return Use("toaster");
            yield return PressWhenMarker(toast, 0.28f);
            yield return new WaitForSeconds(1.0f);
            if (alarm != null && alarm.Ringing)
            {
                Debug.Log("AUTOPILOT ALARM ringing");
                yield return new WaitForSeconds(2.5f); // let it cost a little: frustration passes 30%, so the toaster jams next
                yield return Use("smoke_alarm");
            }
            // 3rd toast: probably jammed by now (frustration above 30), mash, then golden
            yield return new WaitUntil(() => toast.Toaster == ToasterState.Idle);
            yield return Use("toaster");
            if (toast.Toaster == ToasterState.Jammed)
            {
                Debug.Log("AUTOPILOT JAM mashing");
                for (float t = 0; t < 4f && toast.Toaster == ToasterState.Jammed; t += 0.13f)
                {
                    interactor.VirtualPress();
                    yield return new WaitForSeconds(0.13f);
                }
            }
            float golden = (toast.Tuning.goldenZoneStart + toast.Tuning.goldenZoneEnd) / 2f;
            yield return PressWhenMarker(toast, golden);
            yield return new WaitForSeconds(0.6f);
            yield return Use("toast_pickup");
            yield return new WaitForSeconds(3.5f);

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE gate1 dropThings={drop?.State} toast={toast.State} attempts={toast.Attempts} last={toast.LastOutcome} " +
                      $"frustration={frustration?.Value:F1} switchToggles={SwitchToggles} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2} log={frustration?.LogPath}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        // ------------------------------------------------------------------ Gate 2a demo

        IEnumerator Gate2aDemo()
        {
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            var runner = FindAnyObjectByType<TaskRunner>();
            var frustration = FindAnyObjectByType<FrustrationSystem>();
            var bath = FindAnyObjectByType<BathTask>();
            var clean = FindAnyObjectByType<CleanupTask>();
            var call = FindAnyObjectByType<PhoneCallTask>();
            yield return new WaitForSeconds(0.3f);
            if (cutscenes != null) cutscenes.SkipAll();
            // Gate 1 chores are shown in the Gate 1 video; skip them here
            FindAnyObjectByType<FrontDoor>()?.Open();
            motor.Teleport(RoomPos("ROOM_Entrance"), 90f);
            yield return new WaitForSeconds(0.3f);
            while (runner.Current is DropThingsTask || runner.Current is MakeToastTask) { runner.DebugSkipCurrent(); yield return null; }
            // a player who did Gate 1 already has the living-room light on
            if (firstSwitch != null && !firstSwitch.IsOn) yield return UseObject(firstSwitch.transform, 0f, () => interactor.VirtualPress());
            Debug.Log("AUTOPILOT START gate2a");

            // bath: save the first and last item, lose the middle one
            yield return Use("bathtub", hold: 1.4f);
            bath.VirtualRelaxHeld = true;
            int lastPrompt = -1;
            while (bath.Bathing)
            {
                var p = bath.Session.Prompt;
                if (p.HasValue && p.Value.Item != lastPrompt)
                {
                    lastPrompt = p.Value.Item;
                    if (p.Value.Item != 1) { yield return new WaitForSeconds(0.5f); bath.VirtualPress(p.Value.Direction); }
                }
                yield return null;
            }
            bath.VirtualRelaxHeld = false;
            Debug.Log($"AUTOPILOT BATH saved={bath.Session.Saved} lost={bath.Session.Lost}");
            yield return new WaitForSeconds(1.0f);

            // clean up: nearest item first, right container; flown-back items get picked up again
            for (int guard = 0; guard < 12 && clean.State == TaskState.Active; guard++)
            {
                ClutterItem next = null; float best = float.MaxValue;
                foreach (var i in FindObjectsByType<ClutterItem>(FindObjectsSortMode.None))
                {
                    if (i.InBin || !i.gameObject.activeInHierarchy) continue;
                    float d = (i.transform.position - motor.transform.position).sqrMagnitude;
                    if (d < best) { best = d; next = i; }
                }
                if (next == null) break;
                yield return new WaitForSeconds(0.7f); // let a flown-back item land
                yield return Use(next.Pickup.id);
                yield return Use(next.kind == ClutterKind.Laundry ? "bin_laundry" : "bin_trash");
            }
            Debug.Log($"AUTOPILOT CLEANUP done={clean.State == TaskState.Done} flybacks={clean.ComedyFlybacks} rejections={clean.Rejections}");

            // the boss calls
            float waited = 0f;
            while (!call.Ringing && waited < 6f) { waited += Time.deltaTime; yield return null; }
            yield return Use("phone");
            yield return new WaitUntil(() => call.State == TaskState.Done);
            yield return new WaitForSeconds(3.0f);

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE gate2a bath={bath.State} cleanup={clean.State} phone={(call.Answered ? "answered" : call.Ignored ? "ignored" : "none")} " +
                      $"frustration={frustration?.Value:F1} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2} log={frustration?.LogPath}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        // ------------------------------------------------------------------ Gate 2b demo

        IEnumerator Gate2bDemo()
        {
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            var runner = FindAnyObjectByType<TaskRunner>();
            var frustration = FindAnyObjectByType<FrustrationSystem>();
            var tv = FindAnyObjectByType<TvTask>();
            var call = FindAnyObjectByType<PhoneCallTask>();
            var cat = FindAnyObjectByType<CatVisitTask>();
            yield return new WaitForSeconds(0.3f);
            if (cutscenes != null) cutscenes.SkipAll();
            // Gate 1 and 2a are in their own videos; skip to the TV
            FindAnyObjectByType<FrontDoor>()?.Open();
            motor.Teleport(RoomPos("ROOM_LivingRoom"), 180f);
            yield return new WaitForSeconds(0.3f);
            while (!(runner.Current is TvTask)) { runner.DebugSkipCurrent(); yield return null; }
            if (firstSwitch != null && !firstSwitch.IsOn) firstSwitch.Interact(interactor);
            Debug.Log("AUTOPILOT START gate2b");

            // the demo fixes the dice so every outcome shows up in a minute; the odds themselves are tested
            yield return Use("couch", hold: 1.3f);
            yield return new WaitForSeconds(1.5f);
            foreach (var roll in new[] { 0.1f, 0.6f, 0.85f })
            {
                tv.ForcedChannelRoll = roll; tv.ForcedStealRoll = 0.9f; tv.VirtualPress();
                yield return new WaitForSeconds(2.6f);
            }
            for (int i = 0; i < 10 && tv.Seated; i++)
            {
                tv.ForcedChannelRoll = 0.1f; tv.ForcedStealRoll = 0.9f; tv.VirtualPress();
                yield return new WaitForSeconds(0.15f);
            }
            Debug.Log($"AUTOPILOT TV batteries={tv.BatteryLosses}");
            yield return new WaitForSeconds(1.5f);
            yield return Use("under_couch", hold: 1.5f);
            yield return new WaitForSeconds(1.2f);
            yield return Use("couch", hold: 1.3f);
            tv.ForcedChannelRoll = 0.97f; tv.ForcedStealRoll = 0.9f; tv.VirtualPress();
            float t0 = Time.time;
            while (tv.State != TaskState.Done && Time.time - t0 < 15f) yield return null;
            Debug.Log($"AUTOPILOT TV state={tv.State} channel={tv.Session.Channel} changes={tv.ChannelChanges}");

            // the boss calls from the hallway shelf; get up and answer
            t0 = Time.time;
            while (!call.Ringing && Time.time - t0 < 6f) yield return null;
            yield return new WaitForSeconds(1.0f);
            tv.VirtualStand();
            yield return null;
            yield return Use("phone");
            yield return new WaitUntil(() => call.State == TaskState.Done);

            // stand still at the shelf: the cat comes down the corridor
            motor.VirtualMove = Vector2.zero;
            t0 = Time.time;
            while (!cat.Arrived && Time.time - t0 < 30f) yield return null;
            yield return FaceTowards(cat.Cat.transform.position);
            yield return new WaitForSeconds(1.5f);
            yield return Use("cat", hold: 1.8f);
            t0 = Time.time;
            while (cat.State != TaskState.Done && Time.time - t0 < 20f) yield return null;
            yield return new WaitForSeconds(2.5f);

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE gate2b tv={tv.State} batteries={tv.BatteryLosses} phone={(call.Answered ? "answered" : call.Ignored ? "ignored" : "none")} " +
                      $"cat={(cat.Session.Petted ? "petted" : cat.Session.Left ? "left" : "none")} frustration={frustration?.Value:F1} " +
                      $"avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2} log={frustration?.LogPath}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        // ------------------------------------------------------------------ Gate 3 demo

        IEnumerator Gate3Demo()
        {
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            var runner = FindAnyObjectByType<TaskRunner>();
            var frustration = FindAnyObjectByType<FrustrationSystem>();
            var sleep = FindAnyObjectByType<SleepTask>();
            var torch = FindAnyObjectByType<PhoneTorch>();
            var steps = FindAnyObjectByType<FootstepStalker>();
            var mirror = FindAnyObjectByType<MirrorScare>();
            var door = FindAnyObjectByType<DoorSlamScare>();
            yield return new WaitForSeconds(0.3f);
            if (cutscenes != null) cutscenes.SkipAll();
            // the evening is in the earlier videos; start at the end of it, lights on, by the couch
            FindAnyObjectByType<FrontDoor>()?.Open();
            while (!(runner.Current is SleepTask)) { runner.DebugSkipCurrent(); yield return null; }
            if (firstSwitch != null && !firstSwitch.IsOn) firstSwitch.Interact(interactor);
            motor.Teleport(GameObject.Find("CTRL_CouchStand").transform.position + Vector3.up * 0.2f, 0f);
            yield return new WaitForSeconds(0.5f);
            Debug.Log("AUTOPILOT START gate3");

            yield return Use("couch_sleep", hold: 1.4f);
            yield return new WaitUntil(() => sleep.State == TaskState.Done);
            Debug.Log($"AUTOPILOT WOKE phase={FindAnyObjectByType<NightPhaseController>().Phase}");
            yield return new WaitForSeconds(1.2f);
            torch.VirtualToggle();
            yield return LookAround();
            yield return new WaitForSeconds(1.5f);

            // the hallway: walk towards the bathroom; when the steps start, stop, then turn around
            yield return WalkTo(new Vector3(-36.0f, motor.transform.position.y, 1.0f), "hallway");
            yield return FaceTowards(new Vector3(-28f, motor.transform.position.y + 1.5f, 1.0f));
            float t0 = Time.time;
            while (!steps.Fired && Time.time - t0 < 12f) { motor.VirtualMove = new Vector2(0f, 0.6f); yield return null; }
            float walked = Time.time;
            while (steps.Running && steps.StepsPlayed < 5 && Time.time - walked < 5f) { motor.VirtualMove = new Vector2(0f, 0.6f); yield return null; }
            motor.VirtualMove = Vector2.zero;
            yield return new WaitForSeconds(1.4f); // it takes one more step
            yield return FaceTowards(motor.transform.position + Vector3.left * 3f + Vector3.up * 1.4f);
            Debug.Log($"AUTOPILOT STEPS played={steps.StepsPlayed} stoppedByTurning={steps.StoppedByTurning}");
            yield return new WaitForSeconds(1.8f);

            // the bathroom: splash, then look up into the mirror and sway a little (the reflection lags)
            yield return Use("sink");
            var glass = mirror.transform.position;
            yield return FaceTowards(glass);
            t0 = Time.time;
            while (!mirror.Finished && Time.time - t0 < 25f)
            {
                float yaw = 180f + Mathf.Sin((Time.time - t0) * 1.3f) * 9f;
                motor.SetLook(yaw, motor.Pitch);
                yield return null;
            }
            Debug.Log($"AUTOPILOT MIRROR figure={mirror.FigureWasShown} cracked={mirror.Cracked}");
            yield return new WaitForSeconds(1.0f);

            // leave: the door slams behind you in the hallway; go back to it, it opens with fog; then the couch
            yield return WalkTo(new Vector3(-31.2f, motor.transform.position.y, 1.2f), "out of the bathroom");
            yield return FaceTowards(new Vector3(-36f, motor.transform.position.y + 1.5f, 1.2f));
            t0 = Time.time;
            while (door.State == DoorSlamScare.DoorState.Open && Time.time - t0 < 15f) yield return null;
            yield return new WaitForSeconds(2.5f);
            yield return FaceTowards(door.transform.position + Vector3.up * 1.2f);
            yield return new WaitForSeconds(1.5f);
            t0 = Time.time;
            while (door.State == DoorSlamScare.DoorState.Slammed && Time.time - t0 < 10f)
            {
                Vector3 to = door.transform.position - motor.transform.position; to.y = 0f;
                motor.VirtualMove = to.magnitude > 2.0f ? new Vector2(0f, 0.5f) : Vector2.zero;
                yield return null;
            }
            motor.VirtualMove = Vector2.zero;
            yield return new WaitForSeconds(5.5f); // it opens, fog seeping out
            Debug.Log($"AUTOPILOT DOOR state={door.State} slams={door.Slams}");
            yield return WalkTo(GameObject.Find("ROOM_LivingRoom").transform.position, "living room");
            t0 = Time.time;
            while (!runner.AllDone && Time.time - t0 < 5f) yield return null;
            yield return new WaitForSeconds(3f);

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE gate3 phase={FindAnyObjectByType<NightPhaseController>().Phase} steps={steps.Fired} mirror={mirror.Finished} " +
                      $"door={door.State} allDone={runner.AllDone} frustration={frustration?.Value:F1} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        // ------------------------------------------------------------------ Gate 4 demo

        /// <summary>The demo skips the long walks across the house (they're in the Gate 3 video): it steps in near.</summary>
        IEnumerator StepNear(string id, float metres = 1.6f)
        {
            var t = TaskInteractable.Find(id);
            if (t == null) yield break;
            Vector3 best = t.transform.position; bool found = false;
            for (int i = 0; i < 16 && !found; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                Vector3 p = t.transform.position + dir * metres;
                if (!NavMesh.SamplePosition(new Vector3(p.x, 4.85f, p.z), out var hit, 0.2f, NavMesh.AllAreas)) continue;
                if (Physics.Linecast(hit.position + Vector3.up * 1.6f, t.transform.position, out var b) && !b.collider.transform.IsChildOf(t.transform)
                    && b.collider.GetComponentInParent<IInteractable>() == null) continue;
                best = hit.position; found = true;
            }
            motor.Teleport(best + Vector3.up * 0.2f, motor.Yaw);
            yield return null;
            yield return FaceTowards(t.transform.position);
        }

        IEnumerator Gate4Demo()
        {
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            var runner = FindAnyObjectByType<TaskRunner>();
            var frustration = FindAnyObjectByType<FrustrationSystem>();
            var torch = FindAnyObjectByType<PhoneTorch>();
            var snack = FindAnyObjectByType<SnackTask>();
            var tea = FindAnyObjectByType<TeaTask>();
            var chaos = FindAnyObjectByType<ApplianceChaosTask>();
            yield return new WaitForSeconds(0.3f);
            if (cutscenes != null) cutscenes.SkipAll();
            FindAnyObjectByType<FrontDoor>()?.Open();
            // the evening and the wake-up are in the earlier videos: start at 3 AM, in the living room
            while (!(runner.Current is SnackTask)) { runner.DebugSkipCurrent(); yield return null; }
            motor.Teleport(GameObject.Find("ROOM_LivingRoom").transform.position + Vector3.up * 0.2f, 90f);
            yield return new WaitForSeconds(0.5f);
            torch.VirtualToggle();
            Debug.Log("AUTOPILOT START gate4");

            // the fridge: "Back so soon?", a juice box today, then the snack bar
            snack.ForcedJuice = true;
            yield return Use("fridge");
            yield return new WaitForSeconds(1.8f);
            yield return Use("juice_box");
            yield return new WaitForSeconds(2.2f);
            yield return Use("snack");
            yield return new WaitForSeconds(2.5f);
            Debug.Log($"AUTOPILOT SNACK got={snack.Got} juice={snack.JuiceDrunk}");

            // the radio on the dining table: switch it on, let it play a moment
            yield return Use("radio");
            yield return new WaitForSeconds(4f);
            yield return Use("radio");

            // tea: gather the three things, they vanish
            while (!(runner.Current is TeaTask)) yield return null;
            foreach (var id in new[] { "tea_milk", "tea_sugar", "tea_leaves" }) yield return Use(id);
            yield return new WaitForSeconds(2.2f);
            // the hunt: milk in the bathtub, sugar by the phone, to the station; then the tea on the guest bed
            yield return StepNear("tea_milk"); yield return Use("tea_milk");
            yield return StepNear("tea_sugar"); yield return Use("tea_sugar");
            yield return StepNear("tea_station"); yield return Use("tea_station");
            yield return StepNear("tea_leaves"); yield return Use("tea_leaves");
            Debug.Log($"AUTOPILOT HUNT placed={tea.Session.Placed} carrying={tea.Session.Carrying}");
            // walk back into the kitchen with the last one: the power cuts
            motor.Teleport(GameObject.Find("ROOM_LivingRoom").transform.position + Vector3.up * 0.2f, 90f);
            yield return WalkTo(GameObject.Find("ROOM_Kitchen").transform.position, "kitchen");
            yield return new WaitForSeconds(2f);
            Debug.Log($"AUTOPILOT POWER cut={tea.Session.Stage == TeaStage.PowerCut}");
            yield return StepNear("fuse_box"); yield return Use("fuse_box", hold: 1.3f);
            yield return new WaitForSeconds(1f);
            yield return StepNear("tea_station"); yield return Use("tea_station");
            yield return Use("kettle");
            // the kettle screams; the cabinets throw: step aside every throw
            float t0 = Time.time;
            while (tea.Current != TeaTask.Step.Dodge && Time.time - t0 < 15f) yield return null;
            var k = GameObject.Find("ROOM_Kitchen").transform.position;
            Vector3 a = new Vector3(k.x, k.y + 0.2f, k.z - 1f), b = new Vector3(k.x, k.y + 0.2f, k.z + 1f);
            motor.Teleport(a, 90f);
            yield return FaceTowards(TaskInteractable.Find("kettle").transform.position + Vector3.up * 0.8f);
            int seen = tea.Launched; bool atA = true;
            t0 = Time.time;
            while (tea.Current == TeaTask.Step.Dodge && Time.time - t0 < 30f)
            {
                if (tea.Launched != seen)
                {
                    seen = tea.Launched;
                    atA = !atA;
                    for (float s = 0f; s < 0.3f; s += Time.deltaTime) { motor.VirtualMove = new Vector2(atA ? -1f : 1f, 0f); yield return null; } // sidestep
                    motor.VirtualMove = Vector2.zero;
                    motor.Teleport(atA ? a : b, motor.Yaw);
                }
                yield return null;
            }
            Debug.Log($"AUTOPILOT DODGE hits={tea.Dodge?.Hits} step={tea.Current}");
            if (tea.Current == TeaTask.Step.StoveOff) yield return Use("kettle", hold: 1.3f);
            else if (tea.Current == TeaTask.Step.Fire) yield return Use("kettle", hold: 2.3f);
            yield return new WaitForSeconds(2.5f);

            // the appliances: each one when the director lets it
            foreach (var id in new[] { "microwave", "blender", "sink_tap" })
            {
                t0 = Time.time;
                while (chaos.Session == null && Time.time - t0 < 30f) yield return null;
                yield return new WaitForSeconds(1.5f);
                if (id == "sink_tap") yield return StepNear(id);
                yield return Use(id, hold: 1.3f);
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(3f);

            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE gate4 snack={snack.Got} juice={snack.JuiceDrunk} tea={tea.Current} dodgeHits={tea.Dodge?.Hits} " +
                      $"appliances={chaos.ShutOffCount}/3 allDone={runner.AllDone} frustration={frustration?.Value:F1} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        IEnumerator PressWhenMarker(MakeToastTask toast, float at)
        {
            float started = Time.time;
            while (toast.Toaster == ToasterState.Toasting && toast.MarkerPosition < at && Time.time - started < 10f) yield return null;
            if (toast.Toaster == ToasterState.Toasting) interactor.VirtualPress();
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator Use(string id, float hold = 0f)
        {
            var target = TaskInteractable.Find(id);
            if (target == null) { Debug.LogWarning($"AUTOPILOT missing {id}"); yield break; }
            yield return UseObject(target.transform, hold, null);
        }

        IEnumerator UseObject(Transform target, float hold, System.Action press)
        {
            Vector3 away = motor.transform.position - target.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -target.forward;
            Vector3 stand = StandSpot(target, away.normalized, Mathf.Min(1.0f, away.magnitude));
            stand.y = motor.transform.position.y; // stand on the floor below wall- and ceiling-mounted things
            if (away.magnitude > 1.2f) yield return WalkTo(stand, target.name);
            yield return FaceTowards(target.position);
            yield return new WaitForSeconds(0.25f);
            var focused = interactor.Current as Component;
            if (press == null && (focused == null || focused.transform != target))
            {
                Debug.LogWarning($"AUTOPILOT MISSED {target.name} at {target.position} from {motor.transform.position}, looking at {(focused != null ? focused.name : "nothing")}" +
                                 $" (interactor on={interactor.isActiveAndEnabled}, usable={target.GetComponent<IInteractable>()?.CanInteract(null)}, ray: {RayHits()})");
                // aim again for a moment before giving up (the camera can still be settling after a teleport)
                yield return FaceTowards(target.position);
                yield return new WaitForSeconds(0.5f);
                if (ReferenceEquals(interactor.Current as Component, target.GetComponent<IInteractable>() as Component)) Debug.Log($"AUTOPILOT RECOVERED {target.name}");
            }
            if (hold > 0f)
            {
                interactor.VirtualHeld = true;
                yield return new WaitForSeconds(hold);
                interactor.VirtualHeld = false;
            }
            else if (press != null) press();
            else interactor.VirtualPress();
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Where to stand to use something: the way we came if it has a clear view, otherwise the nearest direction
        /// around it that does (things you can't use right now don't block the view, as for the interactor).
        /// </summary>
        Vector3 StandSpot(Transform target, Vector3 preferred, float distance)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 22.5f;
                Vector3 feet = target.position + Quaternion.Euler(0f, a, 0f) * preferred * Mathf.Max(0.8f, distance);
                if (!NavMesh.SamplePosition(new Vector3(feet.x, motor.transform.position.y, feet.z), out var hit, 0.6f, NavMesh.AllAreas)) continue;
                if (ClearView(hit.position + Vector3.up * 1.6f, target)) return hit.position;
            }
            return target.position + preferred * distance;
        }

        static bool ClearView(Vector3 eye, Transform target)
        {
            Vector3 to = target.position - eye;
            var hits = Physics.RaycastAll(eye, to.normalized, to.magnitude + 0.05f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(target)) return true;
                var other = h.collider.GetComponentInParent<IInteractable>();
                if (other != null && !other.CanInteract(null)) continue;
                return false;
            }
            return true;
        }

        IEnumerator WaitForCutscene(CutscenePlayer cutscenes)
        {
            while (cutscenes != null && cutscenes.Playing) yield return null;
        }

        Vector3 RoomPos(string markerName)
        {
            var m = GameObject.Find(markerName);
            return m != null ? m.transform.position : motor.transform.position;
        }

        // ------------------------------------------------------------------ Slice 0 room tour

        IEnumerator Tour()
        {
            yield return new WaitForSeconds(1.5f);
            var cutscenes = FindAnyObjectByType<CutscenePlayer>();
            if (cutscenes != null) cutscenes.SkipAll();
            var door = FindAnyObjectByType<FrontDoor>();
            if (door != null) door.Open();
            var rooms = FindObjectsByType<RoomMarker>(FindObjectsSortMode.None).Where(r => r.tourOrder > 0).OrderBy(r => r.tourOrder).ToList();
            RoomsTotal = rooms.Count;
            Debug.Log($"AUTOPILOT START tour rooms={RoomsTotal}");
            foreach (var room in rooms)
            {
                bool ok = false;
                yield return WalkTo(room.transform.position, room.roomName, r => ok = r);
                if (ok) { RoomsVisited++; Debug.Log($"AUTOPILOT VISITED {room.roomName}"); }
                yield return LookAround();
            }
            Finished = true;
            motor.VirtualMove = Vector2.zero;
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Debug.Log($"AUTOPILOT DONE tour visited={RoomsVisited}/{RoomsTotal} avgFrameMs={avg:F2} worstFrameMs={worstFrame:F2}");
            if (quitWhenDone && !Application.isEditor) { yield return new WaitForSeconds(1f); Application.Quit(); }
        }

        // ------------------------------------------------------------------ movement helpers

        IEnumerator WalkTo(Vector3 target, string label, System.Action<bool> done = null)
        {
            var path = new NavMeshPath();
            Vector3 from = NavMesh.SamplePosition(motor.transform.position, out var here, 1.0f, NavMesh.AllAreas) ? here.position : motor.transform.position;
            if (!NavMesh.SamplePosition(target, out var hit, 1.5f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"AUTOPILOT NO PATH to {label}");
                done?.Invoke(false);
                yield break;
            }

            float started = Time.time;
            foreach (var corner in path.corners.Skip(1))
            {
                while (Flat(corner - motor.transform.position).magnitude > 0.35f)
                {
                    if (Time.time - started > legTimeout)
                    {
                        Debug.LogWarning($"AUTOPILOT STUCK on the way to {label} at {motor.transform.position}");
                        motor.VirtualMove = Vector2.zero;
                        done?.Invoke(false);
                        yield break;
                    }
                    Vector3 to = Flat(corner - motor.transform.position);
                    float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    float yaw = Mathf.MoveTowardsAngle(motor.Yaw, targetYaw, turnSpeed * Time.deltaTime);
                    motor.SetLook(yaw, Mathf.MoveTowards(motor.Pitch, 0f, 60f * Time.deltaTime));
                    float facing = Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw));
                    motor.VirtualMove = new Vector2(0f, facing < 50f ? 1f : 0.2f);
                    yield return null;
                }
            }
            motor.VirtualMove = Vector2.zero;
            done?.Invoke(true);
        }

        /// <summary>What the crosshair ray passes through, nearest first (for MISSED logs).</summary>
        string RayHits()
        {
            var t = interactor.transform;
            var hits = Physics.RaycastAll(t.position, t.forward, 4f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            var names = new System.Collections.Generic.List<string>();
            foreach (var h in hits) if (!h.collider.transform.IsChildOf(motor.transform)) names.Add($"{h.collider.name}@{h.distance:F2}");
            return names.Count > 0 ? string.Join(", ", names) : "nothing";
        }

        IEnumerator FaceTowards(Vector3 point)
        {
            Vector3 eye = interactor.transform.position;
            Vector3 d = point - eye;
            float targetYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Atan2(d.y, Flat(d).magnitude) * Mathf.Rad2Deg;
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                motor.SetLook(Mathf.MoveTowardsAngle(motor.Yaw, targetYaw, turnSpeed * Time.deltaTime),
                              Mathf.MoveTowards(motor.Pitch, targetPitch, 120f * Time.deltaTime));
                yield return null;
            }
            motor.SetLook(targetYaw, targetPitch);
        }

        IEnumerator LookAround()
        {
            float start = motor.Yaw;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                motor.SetLook(start + Mathf.Sin(t / 0.8f * Mathf.PI * 2f) * 35f, 0f);
                yield return null;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
