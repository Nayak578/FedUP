using System.Linq;
using System.Text;
using FedUp.Frustration;
using FedUp.Fx;
using FedUp.Qte;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Cat;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Tv;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using InteractionMode = FedUp.Interaction.InteractionMode;
using G1 = FedUp.EditorTools.GreyboxGate1;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 2b content: Watch TV (living-room couch) and Cat Visit #1. Puts the evening in the GDD / STORY_FLOW
    /// order: bath, clean-up, TV, then the boss call pulls you off the couch, then the cat. Every placement has its
    /// design reason beside it; the full list is DESIGN_LOGIC.md.
    /// </summary>
    public static class GreyboxGate2b
    {
        const float G = 4.83f;

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");

            // ---------- data ----------
            var feBad = G1.Once<FrustrationEventSO>("Frustration/FE_TV_StaticOrHorror", e => { e.amount = 7f; e.description = "Watch TV: static or horror channel (+7%). Appendix 1."; });
            var feAd = G1.Once<FrustrationEventSO>("Frustration/FE_TV_Infomercial", e => { e.amount = 5f; e.description = "Watch TV: infomercial (+5%). Appendix 1."; });
            var feLost = G1.Once<FrustrationEventSO>("Frustration/FE_TV_LostRemote", e => { e.amount = 10f; e.description = "Watch TV: lost remote, batteries under the couch (+10%). Appendix 1."; });
            var feStolen = G1.Once<FrustrationEventSO>("Frustration/FE_TV_RemoteStolen", e => { e.amount = 5f; e.description = "Watch TV: remote stolen, tug of war lost (+5%). Appendix 1."; });
            var feCalm = G1.Once<FrustrationEventSO>("Frustration/FE_TV_Calm", e => { e.amount = -10f; e.description = "Watch TV: calm channel held 10 s (-10%). Appendix 1."; });
            var fePet = G1.Once<FrustrationEventSO>("Frustration/FE_Cat_Petted", e => { e.amount = -20f; e.description = "Cat (any visit): petted (-20%). Appendix 1."; });
            var fePetRetry = G1.Once<FrustrationEventSO>("Frustration/FE_Cat_PettedAfterHiss", e => { e.amount = -10f; e.description = "Cat: petted on retry after a hiss (-10%). Appendix 1."; });
            var tvTuning = G1.Once<TvTuningSO>("Tasks/TvTuning", t => { t.staticOrHorror = feBad; t.infomercial = feAd; t.lostRemote = feLost; t.remoteStolen = feStolen; t.calm = feCalm; });
            var catTuning = G1.Once<CatTuningSO>("Tasks/CatTuning", t => { t.petted = fePet; t.pettedAfterHiss = fePetRetry; });
            var runState = G1.Once<RunStateSO>("Story/RunState", null);
            var requests = G1.Once<FrustrationRequestChannelSO>("Channels/FrustrationRequests", null);
            var changed = G1.Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var qte = G1.Once<QteDisplayChannelSO>("Channels/QteDisplay", null);
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            EnsureLines(lines, log);
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/FedUpControls.inputactions");
            var root = new GameObject("GATE2B").transform;

            // ---------- Watch TV: the living room ----------
            // GDD: "couch facing TV", the living room is the central hub and the TV its main annoyance. The house
            // already has both: the three-seat couch on the south wall faces the media console on the north wall, so
            // you see the TV from the moment you walk in, and its light spills over the room you just cleaned.
            var console = GameObject.Find("GB_MediaConsole").GetComponent<Renderer>().bounds;
            var couchBox = GameObject.Find("GB_3seat4(1)").GetComponent<Renderer>().bounds;

            var screenMat = GreyboxBuilder.Mat("M_Greybox_TVScreen", new Color(0.02f, 0.02f, 0.03f));
            screenMat.EnableKeyword("_EMISSION");
            screenMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            screenMat.SetColor("_EmissionColor", Color.black);
            // screen at seated eye height (sitting eye 1.15 m), on the console's face towards the couch
            var screen = G1.Box("PREF_TVScreen (greybox)", root, new Vector3(console.center.x, G + 1.2f, console.min.z - 0.02f), new Vector3(1.3f, 0.75f, 0.03f), screenMat, 0);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var lightGo = new GameObject("LGT_TVGlow");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.position = screen.transform.position + new Vector3(0f, -0.1f, -0.9f);
            var tvLight = lightGo.AddComponent<Light>();
            tvLight.type = LightType.Point; tvLight.range = 7f; tvLight.intensity = 0f; tvLight.shadows = LightShadows.None; tvLight.enabled = false;

            // the couch backs onto the south wall, so its front (max z) faces the TV
            // the seat: sitting = the camera low at the front of the middle cushion, facing the screen
            var seatTop = G1.Box("GB_CouchSeat", root, new Vector3(couchBox.center.x, couchBox.max.y + 0.03f, couchBox.center.z), new Vector3(couchBox.size.x - 0.2f, 0.06f, couchBox.size.z - 0.1f), c.InteractMat, interactable);
            seatTop.GetComponent<Renderer>().enabled = false; // the couch itself is the visible thing
            var couchTI = G1.Interactable(seatTop, "couch", "Sit down", InteractionMode.Hold, 1f);
            var seat = G1.Marker("CTRL_CouchSeat", root, new Vector3(couchBox.center.x, G, couchBox.max.z + 0.05f));
            seat.rotation = Quaternion.LookRotation(Flat(screen.transform.position - seat.position));
            var stand = G1.Marker("CTRL_CouchStand", root, new Vector3(couchBox.center.x, G, couchBox.max.z + 0.65f));
            stand.rotation = seat.rotation;

            // the remote lives on the right armrest, where his hand falls when he sits
            var remoteMat = GreyboxBuilder.Mat("M_Greybox_Remote", new Color(0.1f, 0.1f, 0.12f));
            var remote = G1.Box("PROP_Remote (greybox)", root, new Vector3(couchBox.max.x - 0.25f, couchBox.max.y + 0.02f, couchBox.center.z), new Vector3(0.05f, 0.025f, 0.17f), remoteMat, 0);
            Object.DestroyImmediate(remote.GetComponent<Collider>());

            // GDD: spamming ejects the batteries "under couch". They stick out from under the front edge, so once he
            // stands up he's looking right at them.
            // the whole front skirt of the couch counts as "under the couch", so you can reach it standing
            var under = G1.Box("GB_UnderCouch", root, new Vector3(couchBox.center.x, G + 0.25f, couchBox.max.z + 0.1f), new Vector3(1.6f, 0.5f, 0.22f), c.InteractMat, interactable);
            under.GetComponent<Renderer>().enabled = false;
            var underTI = G1.Interactable(under, "under_couch", "Feel under the couch", InteractionMode.Hold, 1.2f);
            var batteries = new GameObject("PROP_Batteries (greybox)");
            batteries.transform.SetParent(root, false);
            var battMat = GreyboxBuilder.Mat("M_Greybox_Battery", new Color(0.75f, 0.6f, 0.15f));
            for (int i = 0; i < 2; i++)
            {
                var b = G1.Box("Battery_" + i, batteries.transform, new Vector3(couchBox.center.x - 0.15f + i * 0.32f, G + 0.012f, couchBox.max.z + 0.04f), new Vector3(0.05f, 0.024f, 0.024f), battMat, 0);
                Object.DestroyImmediate(b.GetComponent<Collider>());
            }

            // After a lost tug of war the ghost leaves the remote at the foot of the TV, so getting it back means
            // walking up to the screen of static.
            var floorRemote = G1.Box("PROP_RemoteOnFloor (greybox)", root, new Vector3(console.center.x + 0.35f, G + 0.015f, console.min.z - 0.35f), new Vector3(0.05f, 0.025f, 0.17f), remoteMat, interactable);
            floorRemote.GetComponent<BoxCollider>().size = new Vector3(8f, 12f, 3f);
            var floorRemoteTI = G1.Interactable(floorRemote, "remote_floor", "Pick up the remote", InteractionMode.Press, 0f);

            // ---------- Cat visit #1 ----------
            // GDD: it walks in while you're still, and if ignored "walks away into a shadowy hallway". It comes from
            // the back hall: the darkest end of the ground floor, by the back door that's locked. Nobody can come in
            // that way, which is the first hint this isn't an ordinary cat. After the boss call you're standing at
            // the hallway shelf, looking straight down that corridor as it comes.
            var entry = G1.Marker("CTRL_CatEntry", root, GameObject.Find("ROOM_BackHall").transform.position);
            // GDD Task 8 waypoints include the couch: after a hiss it reappears on the armrest. His couch, where
            // he'll fall asleep tonight; the cat has claimed his spot.
            var perch = G1.Marker("CTRL_CatPerch", root, new Vector3(couchBox.min.x + 0.2f, couchBox.max.y, couchBox.center.z));
            var catMat = GreyboxBuilder.Mat("M_Greybox_Cat", new Color(0.06f, 0.06f, 0.07f)); // a black cat: the old omen, read instantly
            var cat = new GameObject("PREF_Cat (greybox)");
            cat.transform.SetParent(root, false);
            cat.transform.position = entry.position;
            cat.layer = interactable;
            var bodyRoot = new GameObject("CTRL_Body").transform;
            bodyRoot.SetParent(cat.transform, false);
            void Part(string n, Vector3 local, Vector3 size, float pitch = 0f)
            {
                var p = G1.Box(n, bodyRoot, Vector3.zero, size, catMat, 0);
                Object.DestroyImmediate(p.GetComponent<Collider>());
                p.transform.localPosition = local;
                p.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Part("PRT_Torso", new Vector3(0f, 0.17f, 0f), new Vector3(0.2f, 0.18f, 0.42f));
            Part("PRT_Head", new Vector3(0f, 0.3f, 0.24f), new Vector3(0.15f, 0.14f, 0.14f));
            Part("PRT_EarL", new Vector3(-0.05f, 0.39f, 0.24f), new Vector3(0.04f, 0.05f, 0.03f));
            Part("PRT_EarR", new Vector3(0.05f, 0.39f, 0.24f), new Vector3(0.04f, 0.05f, 0.03f));
            Part("PRT_Tail", new Vector3(0f, 0.3f, -0.3f), new Vector3(0.035f, 0.035f, 0.3f), -50f);
            // eyes that catch the light: you see it coming down the dark corridor before you see the cat
            var eyeMat = GreyboxBuilder.Mat("M_Greybox_CatEyes", new Color(0.75f, 0.85f, 0.3f));
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(0.75f, 0.85f, 0.3f) * 1.2f);
            foreach (var x in new[] { -0.035f, 0.035f })
            {
                var eye = G1.Box(x < 0 ? "PRT_EyeL" : "PRT_EyeR", bodyRoot, Vector3.zero, new Vector3(0.03f, 0.02f, 0.01f), eyeMat, 0);
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.localPosition = new Vector3(x, 0.32f, 0.315f);
            }
            var col = cat.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f); col.size = new Vector3(0.5f, 0.5f, 0.8f);
            var catTI = cat.AddComponent<TaskInteractable>();
            catTI.id = "cat"; catTI.prompt = "Pet the cat"; catTI.mode = InteractionMode.Hold; catTI.holdSeconds = 1.5f;
            var agent = cat.AddComponent<NavMeshAgent>();
            agent.radius = 0.2f; agent.height = 0.4f; agent.speed = 1.1f; agent.angularSpeed = 300f; agent.acceleration = 4f;
            agent.stoppingDistance = 0.05f; agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            var surface = Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null) agent.agentTypeID = surface.agentTypeID;

            // GDD: when petted "the screen briefly softens": a post-processing look blended in and out, not a UI
            // overlay (the brightness boost amplifies overlays into a wash)
            const string softPath = "Assets/_Project/Settings/CatSoft.asset";
            var soft = AssetDatabase.LoadAssetAtPath<VolumeProfile>(softPath);
            if (soft == null)
            {
                soft = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(soft, softPath);
                var ca = soft.Add<ColorAdjustments>(true);
                ca.colorFilter.value = new Color(1f, 0.9f, 0.78f); // warm
                ca.postExposure.value = 0.35f;
                ca.saturation.value = 12f;
                var vg = soft.Add<Vignette>(true);
                vg.intensity.value = 0.1f; // the dark edges ease off
                foreach (var comp in soft.components) AssetDatabase.AddObjectToAsset(comp, soft);
                EditorUtility.SetDirty(soft);
                AssetDatabase.SaveAssets();
            }
            var softGo = new GameObject("FX_CatSoften");
            softGo.transform.SetParent(root, false);
            var vol = softGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10f; vol.weight = 0f; vol.sharedProfile = soft;
            var glow = softGo.AddComponent<ScreenSoften>();
            GreyboxBuilder.Wire(glow, ("volume", vol));

            // ---------- systems ----------
            var systems = GameObject.Find("SYSTEMS");
            var tv = systems.AddComponent<TvTask>();
            tv.objective = "Watch TV — find something calm";
            GreyboxBuilder.Wire(tv, ("tuning", tvTuning), ("couch", couchTI), ("underCouch", underTI), ("remoteOnFloor", floorRemoteTI),
                ("seat", seat), ("standSpot", stand), ("screenLookAt", screen.transform), ("screen", screen.GetComponent<Renderer>()),
                ("screenLight", tvLight), ("remoteOnCouch", remote), ("batteries", batteries), ("motor", c.Motor), ("interactor", c.Interactor),
                ("controls", controls), ("frustration", requests), ("frustrationState", changed), ("qte", qte), ("subtitles", subtitles), ("lines", lines));
            var visit = systems.AddComponent<CatVisitTask>();
            visit.objective = "";
            GreyboxBuilder.Wire(visit, ("tuning", catTuning), ("cat", agent), ("petTarget", catTI), ("body", bodyRoot), ("entry", entry),
                ("hissPerch", perch), ("motor", c.Motor), ("frustration", requests), ("frustrationState", changed), ("runState", runState),
                ("lines", lines), ("glow", glow));

            // GDD / STORY_FLOW order: ... clean-up, TV, phone #1, cat #1 (then he falls asleep on the couch: gate 3)
            var runner = systems.GetComponent<TaskRunner>();
            var so = new SerializedObject(runner);
            var list = so.FindProperty("tasks");
            int phoneAt = -1;
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue is PhoneCallTask) { phoneAt = i; break; }
            if (phoneAt < 0) phoneAt = list.arraySize;
            list.InsertArrayElementAtIndex(phoneAt);
            list.GetArrayElementAtIndex(phoneAt).objectReferenceValue = tv;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = visit;
            so.FindProperty("allDoneLineId").stringValue = "gate2b_done";
            so.ApplyModifiedPropertiesWithoutUndo();

            var order = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue?.GetType().Name);
            log.AppendLine($"gate 2b: TV screen at {screen.transform.position:F2}, seat {seat.position:F2}, cat enters at {entry.position:F2}; task order: {string.Join(" > ", order)}");
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Adds lines the library doesn't have yet. Never changes a line the designer has edited.</summary>
        static void EnsureLines(MonologueLibrarySO lib, StringBuilder log)
        {
            int added = 0;
            void Add(string id, string text, float s)
            {
                if (lib.TryGet(id, out _)) return;
                lib.lines.Add(new MonologueLibrarySO.Entry { id = id, text = text, seconds = s });
                added++;
            }
            Add("tv_sit", "Couch. The only relationship that never asked anything of me.", 3.0f);
            Add("tv_batteries", "The batteries. Under the couch. Of course they are.", 2.8f);
            Add("tv_found_batteries", "Two batteries, a coin, and a fry from last year.", 2.8f);
            Add("tv_tug_won", "Mine. Get your own remote.", 2.2f);
            Add("tv_tug_lost", "…It took the remote. Something in this house took the remote.", 3.0f);
            Add("tv_calm_win", "That kid had plans. …Sorry, buddy.", 2.8f);
            Add("cat_arrive", "…I don't have a cat.", 2.0f);
            Add("cat_pet", "Finally, someone chill.", 2.2f);
            Add("cat_hiss", "Okay. Okay. Even the cat.", 2.0f);
            Add("cat_left", "Cats are smart. They don't stay where they're not loved.", 3.2f);
            Add("gate2b_done", "Eyes… heavy. Just five minutes on the couch.", 2.8f);
            if (added > 0) { EditorUtility.SetDirty(lib); log.AppendLine($"monologue: added {added} new lines"); }
        }
    }
}
