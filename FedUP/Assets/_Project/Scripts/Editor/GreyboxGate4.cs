using System.Collections.Generic;
using System.Linq;
using System.Text;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Appliances;
using FedUp.Tasks.Calm;
using FedUp.Tasks.Snack;
using FedUp.Tasks.Tea;
using FedUp.Tasks.Tv;
using UnityEditor;
using UnityEngine;
using InteractionMode = FedUp.Interaction.InteractionMode;
using G1 = FedUp.EditorTools.GreyboxGate1;
using G3 = FedUp.EditorTools.GreyboxGate3;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 4: the rest of Phase 2 (Grab a Snack with the juice box, the radio) and the start of Phase 3 (Make Tea
    /// with the hunt, the power cut and the dodge; appliance chaos). Every placement has its design reason beside
    /// it; the full list is DESIGN_LOGIC.md #40-52.
    /// </summary>
    public static class GreyboxGate4
    {
        const float G = 4.83f;

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");

            // ---------- data (Appendix 1) ----------
            FrustrationEventSO Fe(string name, float amount, string what) =>
                G1.Once<FrustrationEventSO>("Frustration/" + name, e => { e.amount = amount; e.description = what + " Appendix 1."; });
            var feRescatter = Fe("FE_Tea_Rescatter", 10f, "Make Tea: power-cut reset, the items re-scatter (+10%).");
            var feHit = Fe("FE_Tea_Hit", 7f, "Make Tea: hit during the dodge (+7% per hit).");
            var feFire = Fe("FE_Tea_Fire", 15f, "Make Tea: fire ignites (+15%).");
            var feTea = Fe("FE_Tea_Made", -10f, "Make Tea: tea made (-10%).");
            var feMiss = Fe("FE_Snack_FailedGrab", 5f, "Grab a Snack: failed grab (+5%).");
            var feQte = Fe("FE_Snack_QteFail", 10f, "Grab a Snack: ghost-hand QTE lost (+10%).");
            var feSnack = Fe("FE_Snack_Success", -5f, "Grab a Snack: success (-5%).");
            var feSarcasm = Fe("FE_Snack_Sarcasm", 3f, "Grab a Snack: the fridge's sarcastic line (+3%).");
            var feOverload = Fe("FE_Appliance_NotShutOff", 10f, "Appliance chaos: not shut off in time (+10%).");
            var feBlocked = Fe("FE_Appliance_Blocked", 7f, "Appliance chaos: the ghost covers the control (+7%).");
            var feScare = Fe("FE_Appliance_ScareOff", -5f, "Appliance chaos: torch scare worked (-5%).");
            var feJuice = Fe("FE_Calm_JuiceBox", -5f, "Calm extra: the juice box (-5%).");
            var feRadio = Fe("FE_Calm_Radio", -5f, "Calm extra: the radio, 15 s uninterrupted (-5%).");
            var snackTuning = G1.Once<SnackTuningSO>("Tasks/SnackTuning", t => { t.failedGrab = feMiss; t.qteFail = feQte; t.success = feSnack; t.sarcasm = feSarcasm; t.juiceBox = feJuice; });
            var teaTuning = G1.Once<TeaTuningSO>("Tasks/TeaTuning", t => { t.rescatter = feRescatter; t.hit = feHit; t.fire = feFire; t.made = feTea; });
            var applianceTuning = G1.Once<ApplianceTuningSO>("Tasks/ApplianceTuning", t => { t.notShutOff = feOverload; t.blocked = feBlocked; t.scareOff = feScare; });
            var requests = G1.Once<FrustrationRequestChannelSO>("Channels/FrustrationRequests", null);
            var changed = G1.Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var qte = G1.Once<QteDisplayChannelSO>("Channels/QteDisplay", null);
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            EnsureLines(lines, log);

            var root = new GameObject("GATE4").transform;
            var systems = GameObject.Find("SYSTEMS");
            var phase = systems.GetComponent<NightPhaseController>();
            var director = systems.GetComponent<GhostDirector>();
            var torch = Object.FindAnyObjectByType<PhoneTorch>();
            var tv = Object.FindAnyObjectByType<TvTask>();
            if (tv != null && tv.ScreenLight != null) GreyboxBuilder.Wire(phase, ("alsoOnMains", new Object[] { tv.ScreenLight }));
            var foodMat = GreyboxBuilder.Mat("M_Greybox_Food", new Color(0.7f, 0.55f, 0.35f));
            var fridgeWhite = GreyboxBuilder.Mat("M_Greybox_FridgeDoor", new Color(0.78f, 0.78f, 0.74f));
            var ghostMat = G3.Unlit("M_Greybox_FigureUnlit", new Color(0.62f, 0.63f, 0.66f));

            GameObject Item(string name, Transform parent, Vector3 pos, Vector3 size, Material m, string id, string prompt, float colliderScale = 3f)
            {
                var go = G1.Box(name, parent, pos, size, m, interactable);
                var col = go.GetComponent<BoxCollider>();
                col.size = new Vector3(Mathf.Max(1f, colliderScale * 0.12f / size.x), Mathf.Max(1f, colliderScale * 0.12f / size.y), Mathf.Max(1f, colliderScale * 0.12f / size.z));
                G1.Interactable(go, id, prompt, InteractionMode.Press, 0f);
                return go;
            }

            // ---------- the fridge (#40-42): the kitchen fridge's door faces the kitchen (west) ----------
            var fridgeBox = GameObject.Find("GB_Refrigerator").GetComponent<Renderer>().bounds;
            float faceX = fridgeBox.min.x;
            float doorZ0 = fridgeBox.max.z - 0.05f, doorW = 0.9f;
            var hinge = new GameObject("CTRL_FridgeHinge").transform;
            hinge.SetParent(root, false);
            hinge.position = new Vector3(faceX - 0.03f, G, doorZ0);
            var door = G1.Box("PRT_FridgeDoor", hinge, Vector3.zero, new Vector3(0.05f, 1.85f, doorW), fridgeWhite, interactable);
            door.transform.localPosition = new Vector3(0f, 0.95f, -doorW / 2f);
            var fridgeTI = G1.Interactable(door, "fridge", "Open the fridge", InteractionMode.Press, 0f);
            var inside = new GameObject("GB_FridgeInside").transform;
            inside.SetParent(root, false);
            float shelfX = faceX - 0.12f, zMid = doorZ0 - doorW / 2f;
            var lightGo = new GameObject("LGT_Fridge");
            lightGo.transform.SetParent(inside, false);
            lightGo.transform.position = new Vector3(faceX - 0.5f, G + 1.3f, zMid);
            var fridgeLight = lightGo.AddComponent<Light>();
            fridgeLight.type = LightType.Point; fridgeLight.range = 3f; fridgeLight.intensity = 2.2f; fridgeLight.color = new Color(0.85f, 0.92f, 1f); fridgeLight.enabled = false;
            // a fridge of a man who eats alone: leftovers, milk, one sad snack bar
            var leftovers = G1.Box("GB_Leftovers", inside, new Vector3(shelfX, G + 0.9f, zMid - 0.2f), new Vector3(0.18f, 0.12f, 0.22f), foodMat, 0);
            Object.DestroyImmediate(leftovers.GetComponent<Collider>());
            var shelfA = G1.Marker("CTRL_SnackShelfA", inside, new Vector3(shelfX, G + 1.25f, zMid + 0.28f));
            var shelfB = G1.Marker("CTRL_SnackShelfB", inside, new Vector3(shelfX, G + 1.25f, zMid - 0.3f));
            var snack = Item("PROP_SnackBar (greybox)", root, shelfA.position, new Vector3(0.05f, 0.04f, 0.18f), GreyboxBuilder.Mat("M_Greybox_SnackBar", new Color(0.85f, 0.2f, 0.25f)), "snack", "Grab the snack bar");
            var juice = Item("PROP_JuiceBox (greybox)", root, new Vector3(shelfX, G + 0.92f, zMid + 0.2f), new Vector3(0.07f, 0.11f, 0.05f), GreyboxBuilder.Mat("M_Greybox_Juice", new Color(0.95f, 0.75f, 0.2f)), "juice_box", "Drink the juice box");

            // ---------- the radio (#43): on the dining table, the only table set for more than one ----------
            var table = GameObject.Find("GB_Dining_Table").GetComponent<Renderer>().bounds;
            var radioGo = Item("PROP_Radio (greybox)", root, new Vector3(table.max.x - 0.35f, table.max.y + 0.09f, table.min.z + 0.6f), new Vector3(0.3f, 0.18f, 0.12f), GreyboxBuilder.Mat("M_Greybox_Radio", new Color(0.35f, 0.25f, 0.2f)), "radio", "Turn on the radio");
            var radio = systems.AddComponent<RadioCalm>();
            GreyboxBuilder.Wire(radio, ("radio", radioGo.GetComponent<TaskInteractable>()), ("listener", c.Motor.transform), ("phase", phase), ("calm", feRadio),
                ("frustration", requests), ("subtitles", subtitles), ("lines", lines));

            // ---------- tea (#44-50) ----------
            var counter = GameObject.Find("GB_Kitchen_Set03").GetComponent<Renderer>().bounds;
            float top = counter.max.y, cx = counter.center.x;
            // where anyone keeps them: milk by the fridge (left out after the snack), sugar by the toaster, tea under the cabinet
            Vector3[] start = { new Vector3(cx, top + 0.1f, counter.min.z + 0.2f), new Vector3(cx, top + 0.06f, counter.center.z + 1.45f), new Vector3(cx, top + 0.06f, counter.center.z) };
            string[] ids = { "tea_milk", "tea_sugar", "tea_leaves" };
            Vector3[] sizes = { new Vector3(0.08f, 0.2f, 0.08f), new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.12f, 0.08f, 0.08f) };
            Color[] cols = { new Color(0.92f, 0.92f, 0.9f), new Color(0.85f, 0.8f, 0.7f), new Color(0.25f, 0.45f, 0.25f) };
            var items = new List<TaskInteractable>();
            for (int i = 0; i < 3; i++)
                items.Add(Item("PROP_Tea_" + ids[i].Substring(4) + " (greybox)", root, start[i], sizes[i], GreyboxBuilder.Mat("M_Greybox_" + ids[i], cols[i]), ids[i], "Take it").GetComponent<TaskInteractable>());

            // hiding places: milk in the bathtub (#45), sugar beside the phone (#46), tea on the guest bed (#47)
            var tub = GameObject.Find("PREF_Bathtub (greybox)").GetComponent<Renderer>().bounds;
            var phone = GameObject.Find("PREF_Phone (greybox)").transform.position;
            var guestRoom = GameObject.Find("ROOM_GuestBedroom").transform.position;
            // you come in through the west doorway; the east wall opposite is the only unbroken one (the north wall has an opening)
            if (!Physics.Raycast(new Vector3(guestRoom.x, G + 1.2f, guestRoom.z + 0.6f), Vector3.right, out var gw, 6f))
            {
                Debug.LogWarning("Gate 4: no east wall found in the guest room; the bed is a guess");
                gw.point = guestRoom + Vector3.right * 2.5f;
            }
            // the guest room is empty in the house model: a made double bed, headboard to the wall facing the door, never slept in (#47)
            var bed = G1.Box("GB_GuestBed", root, new Vector3(gw.point.x - 1.02f, G + 0.25f, guestRoom.z + 0.6f), new Vector3(2.0f, 0.5f, 1.6f), GreyboxBuilder.Mat("M_Greybox_Bedding", new Color(0.55f, 0.55f, 0.6f)), 0);
            // added after the walking map is baked, so it cuts itself out of it (the autopilot and any AI walk around it)
            var bedHole = bed.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            bedHole.carving = true; bedHole.shape = UnityEngine.AI.NavMeshObstacleShape.Box; bedHole.size = Vector3.one;
            Physics.SyncTransforms();
            // the tea lies at the foot of the bed, the end nearest the doorway: you see it as you look in
            var guestBed = new Vector3(bed.transform.position.x - 0.6f, G + 0.5f, bed.transform.position.z - 0.3f);
            var hide = new[]
            {
                G1.Marker("CTRL_HideMilk", root, new Vector3(tub.center.x, tub.max.y + 0.1f, tub.center.z)),
                G1.Marker("CTRL_HideSugar", root, phone + new Vector3(-0.35f, 0.01f, 0f)),
                G1.Marker("CTRL_HideTea", root, guestBed + Vector3.up * 0.05f),
            };
            // the stove at the counter's south end, the tea station beside it (#49)
            var stove = G1.Box("GB_Stove", root, new Vector3(cx, top + 0.02f, counter.min.z + 0.75f), new Vector3(0.6f, 0.04f, 0.55f), GreyboxBuilder.Mat("M_Greybox_Stove", new Color(0.15f, 0.15f, 0.16f)), 0);
            var kettleGo = Item("PROP_Kettle (greybox)", root, stove.transform.position + new Vector3(0f, 0.13f, 0f), new Vector3(0.2f, 0.22f, 0.2f), GreyboxBuilder.Mat("M_Greybox_Kettle", new Color(0.6f, 0.62f, 0.65f)), "kettle", "Put the kettle on");
            var station = Item("GB_TeaStation", root, new Vector3(cx, top + 0.05f, counter.min.z + 1.5f), new Vector3(0.5f, 0.08f, 0.6f), c.InteractMat, "tea_station", "Put them down", 1f);
            station.GetComponent<Renderer>().enabled = false;
            var placed = new Transform[3];
            for (int i = 0; i < 3; i++) placed[i] = G1.Marker("CTRL_TeaPlaced" + i, root, new Vector3(cx, top + 0.1f, counter.min.z + 1.3f + i * 0.18f));
            var junk = new GameObject("GB_JunkOnStove");
            junk.transform.SetParent(root, false);
            var junkMat = GreyboxBuilder.Mat("M_Greybox_Junk", new Color(0.5f, 0.42f, 0.3f));
            for (int i = 0; i < 4; i++)
            {
                var j = G1.Box("Junk_" + i, junk.transform, stove.transform.position + new Vector3((i % 2 - 0.5f) * 0.25f, 0.06f + i * 0.03f, (i / 2 - 0.5f) * 0.2f), new Vector3(0.2f, 0.05f, 0.15f), junkMat, 0);
                Object.DestroyImmediate(j.GetComponent<Collider>());
                j.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
            }
            // the fuse box (#48): on the back-hall wall by the locked back door
            var hall = GameObject.Find("ROOM_BackHall").transform.position;
            Vector3 hallEye = new Vector3(hall.x, G + 1.5f, hall.z);
            var wallHit = Physics.Raycast(hallEye, Vector3.forward, out var wh, 4f) ? wh : default;
            Vector3 fusePos = wallHit.collider != null ? wallHit.point - Vector3.forward * 0.06f : hallEye + Vector3.forward * 0.8f;
            var fuse = Item("PREF_FuseBox (greybox)", root, fusePos, new Vector3(0.35f, 0.45f, 0.1f), GreyboxBuilder.Mat("M_Greybox_FuseBox", new Color(0.45f, 0.45f, 0.42f)), "fuse_box", "Flip the breaker", 1.5f);
            // throws come from the upper cabinets over the stove (#50)
            var cab1 = GameObject.Find("GB_Kitchen_Set01").GetComponent<Renderer>().bounds;
            var cab2 = GameObject.Find("GB_Kitchen_Set02").GetComponent<Renderer>().bounds;
            var throwFrom = new[]
            {
                G1.Marker("CTRL_Throw0", root, new Vector3(cab1.min.x - 0.1f, cab1.center.y, cab1.center.z)),
                G1.Marker("CTRL_Throw1", root, new Vector3(cab2.min.x - 0.1f, cab2.center.y - 0.1f, cab2.center.z - 0.6f)),
                G1.Marker("CTRL_Throw2", root, new Vector3(cab2.min.x - 0.1f, cab2.center.y - 0.1f, cab2.center.z + 0.6f)),
            };
            var fireGo = new GameObject("LGT_StoveFire");
            fireGo.transform.SetParent(root, false);
            fireGo.transform.position = stove.transform.position + Vector3.up * 0.4f;
            var fireLight = fireGo.AddComponent<Light>();
            fireLight.type = LightType.Point; fireLight.color = new Color(1f, 0.5f, 0.15f); fireLight.intensity = 4f; fireLight.range = 6f; fireLight.enabled = false;
            var fireFx = Puffs("FX_StoveFire", stove.transform, new Vector3(0f, 0.15f, 0f), 0.5f, new Color(1f, 0.45f, 0.1f, 0.55f), 0.6f, 1.2f, 0.4f, 14f);
            var kitchenZone = G1.Marker("CTRL_KitchenZone", root, new Vector3(-33.15f, G + 1f, -4.05f));

            // ---------- appliances (#51-52) ----------
            var props = GameObject.Find("GB_Kitchen_Set_props").GetComponent<Renderer>().bounds;
            var micro = Item("PREF_Microwave (greybox)", root, new Vector3(cx, top + 0.16f, counter.max.z - 0.35f), new Vector3(0.45f, 0.3f, 0.35f), GreyboxBuilder.Mat("M_Greybox_Microwave", new Color(0.3f, 0.3f, 0.32f)), "microwave", "Switch it off", 1.5f);
            var blender = Item("PREF_Blender (greybox)", root, new Vector3(props.center.x, props.max.y + 0.17f, props.center.z), new Vector3(0.14f, 0.32f, 0.14f), GreyboxBuilder.Mat("M_Greybox_Blender", new Color(0.55f, 0.6f, 0.62f)), "blender", "Switch it off", 1.5f);
            var sink = GameObject.Find("PREF_Sink (greybox)").GetComponent<Renderer>().bounds;
            var tap = Item("PREF_SinkTap (greybox)", root, new Vector3(sink.center.x, sink.max.y + 0.08f, sink.max.z - 0.12f), new Vector3(0.06f, 0.14f, 0.12f), GreyboxBuilder.Mat("M_Greybox_Tap", new Color(0.7f, 0.72f, 0.75f)), "sink_tap", "Switch it off", 2f);
            var units = new[] { micro, blender, tap };
            var hands = new GameObject[3];
            var overload = new ParticleSystem[3];
            Color[] fxCol = { new Color(1f, 0.9f, 0.4f, 0.6f), new Color(0.45f, 0.3f, 0.15f, 0.6f), new Color(0.35f, 0.55f, 0.8f, 0.5f) };
            for (int i = 0; i < 3; i++)
            {
                var h = G1.Box("GB_GhostHand_" + i, units[i].transform, Vector3.zero, new Vector3(0.12f, 0.04f, 0.1f), ghostMat, 0);
                Object.DestroyImmediate(h.GetComponent<Collider>());
                h.transform.position = units[i].transform.position + Vector3.up * 0.12f;
                hands[i] = h;
                overload[i] = Puffs("FX_Overload_" + i, units[i].transform, Vector3.up * 0.1f, 0.3f, fxCol[i], 0.3f, 0.6f, 1.2f, 25f);
            }

            // ---------- tasks ----------
            var snackTask = systems.AddComponent<SnackTask>();
            snackTask.objective = "Grab a snack from the fridge";
            GreyboxBuilder.Wire(snackTask, ("tuning", snackTuning), ("fridge", fridgeTI), ("doorHinge", hinge), ("inside", inside.gameObject), ("fridgeLight", fridgeLight),
                ("snack", snack.GetComponent<TaskInteractable>()), ("shelfA", shelfA), ("shelfB", shelfB), ("juiceBox", juice.GetComponent<TaskInteractable>()),
                ("interactor", c.Interactor), ("frustration", requests), ("frustrationState", changed), ("qte", qte), ("subtitles", subtitles), ("lines", lines));
            var teaTask = systems.AddComponent<TeaTask>();
            teaTask.objective = "Make tea";
            GreyboxBuilder.Wire(teaTask, ("tuning", teaTuning), ("items", items.Cast<Object>().ToArray()), ("hideSpots", hide.Cast<Object>().ToArray()),
                ("placedSpots", placed.Cast<Object>().ToArray()), ("station", station.GetComponent<TaskInteractable>()), ("kettle", kettleGo.GetComponent<TaskInteractable>()),
                ("fuseBox", fuse.GetComponent<TaskInteractable>()), ("kitchen", kitchenZone), ("throwFrom", throwFrom.Cast<Object>().ToArray()), ("junkOnStove", junk),
                ("junkMaterial", junkMat), ("fireLight", fireLight), ("fireFx", fireFx), ("player", c.Motor.transform), ("eye", c.Interactor.transform), ("phase", phase),
                ("frustration", requests), ("qte", qte), ("subtitles", subtitles), ("lines", lines));
            G1.SetStrings(teaTask, "itemNames", new[] { "milk", "sugar", "tea" });
            var so2 = new SerializedObject(teaTask);
            so2.FindProperty("kitchenSize").vector3Value = new Vector3(6.5f, 4f, 7.3f);
            so2.ApplyModifiedPropertiesWithoutUndo();
            var chaos = systems.AddComponent<ApplianceChaosTask>();
            chaos.objective = "Something's on in the house";
            GreyboxBuilder.Wire(chaos, ("tuning", applianceTuning), ("units", units.Select(u => u.GetComponent<TaskInteractable>()).Cast<Object>().ToArray()),
                ("ghostHands", hands.Cast<Object>().ToArray()), ("overloadFx", overload.Cast<Object>().ToArray()), ("director", director), ("torch", torch),
                ("interactor", c.Interactor), ("frustration", requests), ("frustrationState", changed), ("qte", qte), ("lines", lines));
            G1.SetStrings(chaos, "unitNames", new[] { "microwave", "blender", "bathroom sink" });

            var runner = systems.GetComponent<TaskRunner>();
            var so = new SerializedObject(runner);
            var list = so.FindProperty("tasks");
            foreach (var t in new TaskBehaviour[] { snackTask, teaTask, chaos })
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = t;
            }
            so.FindProperty("allDoneLineId").stringValue = "gate4_done";
            so.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"gate 4: fridge face x {faceX:F2}, radio {radioGo.transform.position:F2}, stove {stove.transform.position:F2}, fuse box {fusePos:F2}, " +
                           $"hide milk {hide[0].position:F2} sugar {hide[1].position:F2} tea {hide[2].position:F2}");
        }

        static ParticleSystem Puffs(string name, Transform parent, Vector3 local, float width, Color colour, float sizeMin, float sizeMax, float speed, float rate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // emit upwards
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 2f;
            main.startLifetime = 1.6f; main.startSpeed = speed; main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = colour; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 20f; shape.radius = width * 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Fx_FogPuff.mat");
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        /// <summary>Adds lines the library doesn't have yet. Never changes a line the designer has edited.
        /// Writing guide (HORROR_RESEARCH.md rule 9): flat about the unreal, raw about his life.</summary>
        static void EnsureLines(MonologueLibrarySO lib, StringBuilder log)
        {
            int added = 0;
            void Add(string id, string text, float s)
            {
                if (lib.TryGet(id, out _)) return;
                lib.lines.Add(new MonologueLibrarySO.Entry { id = id, text = text, seconds = s });
                added++;
            }
            Add("fridge_slam", "Fine. Keep it.", 1.8f);
            Add("snack_missed", "Come here. Come HERE.", 2.0f);
            Add("snack_lost", "…Not hungry anyway.", 2.2f);
            Add("snack_got", "Dinner of champions, part two.", 2.4f);
            Add("juice_box", "Apple. Like the ones Mom used to pack. …Huh.", 3.0f);
            Add("radio_song", "…This song. We used to— never mind.", 3.0f);
            Add("tea_start", "Tea. People on TV drink tea when they're sad. It seems to work for them.", 3.4f);
            Add("tea_vanished", "…Where did it all go?", 2.2f);
            Add("tea_hands_full", "Two hands. I have two hands.", 2.0f);
            Add("tea_dark", "Can't see a thing. The fuse box first.", 2.4f);
            Add("tea_power_cut", "No. No, no. Not now.", 2.0f);
            Add("tea_power_back", "Light. Thank god.", 1.8f);
            Add("tea_rescatter", "They're gone again. Of course they are.", 2.6f);
            Add("tea_all_here", "Milk, sugar, tea. Civilisation.", 2.4f);
            Add("tea_dodge", "What— the cabinets—", 1.8f);
            Add("tea_hit", "Ow—", 1.0f);
            Add("tea_survived", "Stove. Off. Now.", 1.8f);
            Add("tea_fire", "Fire. Of course. Fire.", 2.0f);
            Add("tea_no_tea", "No tea. Of course no tea.", 2.4f);
            Add("tea_made", "Tea. I made tea. In this economy.", 2.6f);
            Add("appliance_on_0", "The microwave. Nobody touched the microwave.", 2.6f);
            Add("appliance_on_1", "Now the blender.", 1.8f);
            Add("appliance_on_2", "…Is that water? The bathroom.", 2.4f);
            Add("appliance_need_light", "I can't see it. Torch.", 1.8f);
            Add("appliance_scared", "Get off. GET OFF.", 1.8f);
            Add("appliance_overload", "Great. Just great.", 1.8f);
            Add("gate4_done", "The whole house is awake. So I am too.", 2.8f);
            if (added > 0) { EditorUtility.SetDirty(lib); log.AppendLine($"monologue: added {added} new lines"); }
        }
    }
}
