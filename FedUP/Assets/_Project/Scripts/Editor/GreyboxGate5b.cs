using System.Collections.Generic;
using System.Linq;
using System.Text;
using FedUp.Ghost;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Bath;
using FedUp.Tasks.Tea;
using FedUp.Tasks.Tv;
using FedUp.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using InteractionMode = FedUp.Interaction.InteractionMode;
using LineRule = FedUp.Story.MonologueLibrarySO.LineRule;
using G1 = FedUp.EditorTools.GreyboxGate1;
using G3 = FedUp.EditorTools.GreyboxGate3;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 5b: the Storeroom blackout (and the tea moving upstairs), Mom's voicemail, the scenes for the loop,
    /// unemployed, dying in your sleep and the Burial in the house, and the ending card pass. Every placement has its
    /// design reason beside it; the full list is DESIGN_LOGIC.md #53-60 and #77-86.
    /// </summary>
    public static class GreyboxGate5b
    {
        const float G = 4.83f, Up = 8.28f;

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            EnsureLines(lines, log);

            var root = new GameObject("GATE5B").transform;
            var systems = GameObject.Find("SYSTEMS");
            var phase = systems.GetComponent<NightPhaseController>();
            var torch = Object.FindAnyObjectByType<PhoneTorch>();
            var runner = systems.GetComponent<TaskRunner>();
            var tea = systems.GetComponent<TeaTask>();
            var ghostMat = G3.Unlit("M_Greybox_FigureUnlit", new Color(0.62f, 0.63f, 0.66f));
            var boxMat = GreyboxBuilder.Mat("M_Greybox_Boxes", new Color(0.42f, 0.36f, 0.28f));
            var woodMat = GreyboxBuilder.Mat("M_Greybox_DrawerWood", new Color(0.36f, 0.27f, 0.2f));
            var steel = GreyboxBuilder.Mat("M_Greybox_AxeHead", new Color(0.55f, 0.56f, 0.58f));
            var darkMat = G3.Unlit("M_Greybox_Void", new Color(0.01f, 0.01f, 0.012f));

            GameObject Solid(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool carve = true)
            {
                var go = G1.Box(name, parent, pos, size, m, 0);
                if (carve) { var o = go.AddComponent<NavMeshObstacle>(); o.carving = true; o.shape = NavMeshObstacleShape.Box; o.size = Vector3.one; }
                return go;
            }

            // ---------- the full-screen black the blackout and the endings fade through ----------
            var hudBlack = G1.Rect("SceneBlack", c.Hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var black = hudBlack.gameObject.AddComponent<Image>();
            black.color = new Color(0f, 0f, 0f, 0f);
            black.raycastTarget = false;
            var cardGo = c.Hud.Find("EndingCard");
            hudBlack.SetSiblingIndex(cardGo != null ? cardGo.GetSiblingIndex() : c.Hud.childCount - 1); // under the ending card

            // ---------- the Storeroom (#54-57): a walk-through room full of a life ----------
            var store = new GameObject("ROOMFX_Storeroom").transform;
            store.SetParent(root, false);
            Vector3 storeMid = new Vector3(-33.4f, Up, -3.4f);
            float eastFace = Physics.Raycast(new Vector3(-31.0f, Up + 1.5f, -4.3f), Vector3.right, out var eh, 3f) ? eh.point.x : -29.8f;
            float northFace = Physics.Raycast(new Vector3(-33.4f, Up + 1.5f, -2.0f), Vector3.forward, out var nh, 3f) ? nh.point.z : -0.6f;
            // the axe on the east wall, opposite the landing doorway: the first thing in your torch beam as you come in (#55)
            var axeGo = G1.Box("PROP_Axe (greybox)", store, new Vector3(eastFace - 0.04f, Up + 1.45f, -4.3f), new Vector3(0.04f, 0.8f, 0.06f), woodMat, interactable);
            var head = G1.Box("PRT_AxeHead", axeGo.transform, Vector3.zero, new Vector3(0.05f, 0.16f, 0.24f), steel, 0);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.localScale = new Vector3(1.2f, 0.2f, 4f);
            head.transform.localPosition = new Vector3(0f, 0.42f, 0.1f);
            axeGo.GetComponent<BoxCollider>().size = new Vector3(6f, 1.2f, 6f);
            var axeTI = G1.Interactable(axeGo, "axe", "Take the axe", InteractionMode.Hold, 0.8f);
            axeTI.available = false;
            // the shelf right under it, the tea tin on top (#56)
            var shelf = Solid("GB_ShelfUnderAxe", store, new Vector3(eastFace - 0.2f, Up + 0.45f, -4.3f), new Vector3(0.38f, 0.9f, 1.0f), woodMat);
            var hideTea = GameObject.Find("CTRL_HideTea").transform;
            hideTea.position = new Vector3(eastFace - 0.22f, Up + 0.95f, -4.3f);
            // the tall shelf against the north wall, and the top of a door frame showing above it (#57)
            Solid("GB_TallShelf", store, new Vector3(-33.2f, Up + 1.0f, northFace - 0.22f), new Vector3(1.7f, 2.0f, 0.42f), woodMat);
            var frameMat = GreyboxBuilder.Mat("M_Greybox_DoorFrame", new Color(0.5f, 0.45f, 0.38f));
            G1.Box("GB_HiddenDoorFrame_Top", store, new Vector3(-33.2f, Up + 2.12f, northFace - 0.03f), new Vector3(1.0f, 0.1f, 0.05f), frameMat, 0);
            G1.Box("GB_HiddenDoorFrame_L", store, new Vector3(-33.65f, Up + 2.04f, northFace - 0.03f), new Vector3(0.08f, 0.16f, 0.05f), frameMat, 0);
            G1.Box("GB_HiddenDoorFrame_R", store, new Vector3(-32.75f, Up + 2.04f, northFace - 0.03f), new Vector3(0.08f, 0.16f, 0.05f), frameMat, 0);
            var dust = Puffs("FX_DustToTheFrame", store, new Vector3(-33.2f, Up + 1.6f, northFace - 0.8f), new Color(0.6f, 0.6f, 0.58f, 0.15f), 0.08f, 0.2f, 0.12f, 3f, false);
            dust.transform.rotation = Quaternion.LookRotation(Vector3.forward + Vector3.up * 0.8f); // drifting towards the frame: a draft
            dust.Play();
            // a lifetime of boxes, along the walls, out of the way between the doors
            var rnd = new System.Random(5);
            Vector3[] boxSpots = { new Vector3(-35.9f, 0f, -4.7f), new Vector3(-35.2f, 0f, -4.8f), new Vector3(-34.3f, 0f, -4.75f), new Vector3(-31.6f, 0f, -4.8f),
                                   new Vector3(-35.9f, 0f, -1.4f), new Vector3(-35.1f, 0f, -1.3f), new Vector3(-31.4f, 0f, -1.3f), new Vector3(-30.7f, 0f, -1.6f) };
            for (int i = 0; i < boxSpots.Length; i++)
            {
                float h = 0.35f + (float)rnd.NextDouble() * 0.4f;
                var b = Solid("GB_StoreBox_" + i, store, new Vector3(boxSpots[i].x, Up + h / 2f, boxSpots[i].z), new Vector3(0.6f, h, 0.5f), boxMat);
                b.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 30f - 15f, 0f);
                if (i % 3 == 0) Solid("GB_StoreBox_" + i + "_top", store, b.transform.position + new Vector3(0.05f, h / 2f + 0.17f, 0f), new Vector3(0.45f, 0.34f, 0.4f), boxMat, false);
            }
            var trophy = Solid("GB_TrophyBox_Taped", store, new Vector3(-34.2f, Up + 0.2f, -1.35f), new Vector3(0.5f, 0.4f, 0.4f), GreyboxBuilder.Mat("M_Greybox_Tape", new Color(0.55f, 0.48f, 0.3f)));
            var brokenChair = Solid("GB_BrokenChair", store, new Vector3(-32.4f, Up + 0.25f, -4.85f), new Vector3(0.45f, 0.5f, 0.45f), woodMat);
            brokenChair.transform.rotation = Quaternion.Euler(0f, 30f, 72f);
            var heard = G1.Marker("CTRL_TinHeardFrom", root, new Vector3(-33.0f, Up + 0.1f, -3.8f));

            // ---------- the Locked Room, dreamt (#58, #80) ----------
            var dream = new GameObject("ROOMFX_LockedRoomDream").transform;
            dream.SetParent(root, false);
            float lockedNorth = Physics.Raycast(new Vector3(-32.2f, Up + 1.4f, 4.0f), Vector3.forward, out var lh, 8f) ? lh.point.z : 8.8f;
            var dreamSpot = G1.Marker("CTRL_DreamSpot", dream, new Vector3(-32.2f, Up, lockedNorth - 4.6f));
            var window = G1.Box("GB_DreamWindow", dream, new Vector3(-32.2f, Up + 1.6f, lockedNorth - 0.02f), new Vector3(1.1f, 1.0f, 0.02f), G3.Unlit("M_Greybox_WindowNight", new Color(0.16f, 0.12f, 0.1f)), 0);
            Object.DestroyImmediate(window.GetComponent<Collider>());
            var dreamLook = G1.Marker("CTRL_DreamLook", dream, window.transform.position);
            var factoryGlow = new GameObject("LGT_FactoryGlow");
            factoryGlow.transform.SetParent(dream, false);
            factoryGlow.transform.position = window.transform.position + new Vector3(0f, -0.3f, -0.6f);
            var dl = factoryGlow.AddComponent<Light>();
            dl.type = LightType.Point; dl.color = new Color(0.95f, 0.55f, 0.3f); dl.intensity = 1.6f; dl.range = 6f;
            // a desk and an upright chair facing the shut window; nothing knocked over, nothing hanging (research rule 10)
            Solid("GB_DreamDesk", dream, new Vector3(-32.2f, Up + 0.38f, lockedNorth - 0.5f), new Vector3(1.2f, 0.76f, 0.6f), woodMat, false);
            Solid("GB_DreamChair_Seat", dream, new Vector3(-32.2f, Up + 0.23f, lockedNorth - 1.15f), new Vector3(0.45f, 0.46f, 0.45f), woodMat, false);
            Solid("GB_DreamChair_Back", dream, new Vector3(-32.2f, Up + 0.7f, lockedNorth - 1.35f), new Vector3(0.45f, 0.5f, 0.05f), woodMat, false);
            var stain = G1.Box("GB_DreamStain", dream, new Vector3(-31.3f, Up + 0.006f, lockedNorth - 3.3f), new Vector3(1.1f, 0.01f, 0.8f), GreyboxBuilder.Mat("M_Greybox_WaterStain", new Color(0.18f, 0.16f, 0.13f)), 0);
            Object.DestroyImmediate(stain.GetComponent<Collider>());
            Solid("GB_DreamMattress", dream, new Vector3(-33.7f, Up + 0.06f, lockedNorth - 1.2f), new Vector3(0.9f, 0.12f, 1.9f), GreyboxBuilder.Mat("M_Greybox_Bedding", new Color(0.55f, 0.55f, 0.6f)), false);
            Solid("GB_DreamBoxes", dream, new Vector3(-30.6f, Up + 0.25f, lockedNorth - 1.0f), new Vector3(0.6f, 0.5f, 0.5f), boxMat, false);
            // the shape that passes the window, outside, upstairs: a glimpse
            var glimpse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            glimpse.name = "GB_DreamGlimpse";
            Object.DestroyImmediate(glimpse.GetComponent<Collider>());
            glimpse.transform.SetParent(dream, false);
            glimpse.transform.position = window.transform.position + new Vector3(-0.8f, -0.1f, 0.35f);
            glimpse.transform.localScale = new Vector3(0.45f, 0.9f, 0.35f);
            glimpse.GetComponent<Renderer>().sharedMaterial = ghostMat;

            // ---------- waking in the bathtub (#59) ----------
            var bath = Object.FindAnyObjectByType<BathTask>();
            var bathSpot = (Transform)new SerializedObject(bath).FindProperty("bathSpot").objectReferenceValue;
            var tub = GameObject.Find("PREF_Bathtub (greybox)").GetComponent<Renderer>().bounds;
            var tubStand = G1.Marker("CTRL_TubStand", root, NavMesh.SamplePosition(new Vector3(tub.min.x - 0.6f, G, tub.center.z), out var ts, 1.2f, NavMesh.AllAreas) ? ts.position : new Vector3(tub.min.x - 0.6f, G, tub.center.z));
            tubStand.rotation = Quaternion.Euler(0f, 270f, 0f);

            var blackout = systems.AddComponent<StoreroomBlackout>();
            GreyboxBuilder.Wire(blackout, ("tea", tea), ("motor", c.Motor), ("interactor", c.Interactor), ("torch", torch), ("phase", phase), ("black", black),
                ("dreamSpot", dreamSpot), ("dreamLookAt", dreamLook), ("dreamLight", dl), ("glimpse", glimpse), ("tubLie", bathSpot), ("tubStand", tubStand), ("lines", lines));
            var tso = new SerializedObject(blackout);
            tso.FindProperty("glimpseTravel").vector3Value = new Vector3(1.6f, 0f, 0f);
            tso.ApplyModifiedPropertiesWithoutUndo();
            GreyboxBuilder.Wire(tea, ("heardFrom", heard));

            // ---------- Mom's voicemail (#53, #77) ----------
            var card = GameObject.Find("PROP_MomsCard (greybox)").transform.position;
            var hisPhone = G1.Box("PROP_HisPhone (greybox)", root, card + new Vector3(-0.12f, 0.005f, 0.24f), new Vector3(0.08f, 0.012f, 0.15f), GreyboxBuilder.Mat("M_Greybox_Gun", new Color(0.08f, 0.08f, 0.09f)), interactable);
            hisPhone.GetComponent<BoxCollider>().size = new Vector3(3f, 12f, 2f);
            var phoneTI = G1.Interactable(hisPhone, "his_phone", "Listen to the voicemail (Mom)", InteractionMode.Press, 0f);
            var screenGo = new GameObject("LGT_PhoneScreen");
            screenGo.transform.SetParent(hisPhone.transform, false);
            screenGo.transform.position = hisPhone.transform.position + Vector3.up * 0.12f;
            var screen = screenGo.AddComponent<Light>();
            screen.type = LightType.Point; screen.color = new Color(0.7f, 0.8f, 1f); screen.intensity = 0.6f; screen.range = 1.1f;
            var vm = systems.AddComponent<MomsVoicemail>();
            GreyboxBuilder.Wire(vm, ("phone", phoneTI), ("screen", screen), ("runner", runner), ("phase", phase), ("lines", lines), ("subtitles", subtitles));

            // ---------- the ending scenes (#81-83) ----------
            var spawn = Object.FindAnyObjectByType<SpawnPoint>().transform;
            var porch = G1.Marker("CTRL_LoopPorch", root, spawn.position);
            porch.rotation = spawn.rotation;
            var slams = new[] { "ROOM_Bedroom", "ROOM_WorkRoom", "ROOM_BathroomUpstairs", "ROOM_Wardrobe", "ROOM_BathroomMiddle", "ROOM_Storeroom" }
                .Select(n => GameObject.Find(n)).Where(g => g != null).Select(g => g.transform).Cast<Object>().ToArray();
            var scenes = systems.AddComponent<EndingScenes>();
            GreyboxBuilder.Wire(scenes, ("phase", phase), ("motor", c.Motor), ("interactor", c.Interactor), ("black", black), ("porch", porch),
                ("frontDoor", Object.FindAnyObjectByType<FrontDoor>()), ("tv", Object.FindAnyObjectByType<TvTask>()), ("slams", slams),
                ("bedsidePhone", GameObject.Find("PROP_Charger (greybox)").transform), ("lines", lines), ("subtitles", subtitles));

            // ---------- the Burial in the house (#84-85) ----------
            var bur = new GameObject("ROOMFX_BurialRoute").transform;
            bur.SetParent(root, false);
            const float wallX = -25.59f, cavityX = -25.13f, wallZ = 8.0f;
            var wallSpot = G1.Marker("CTRL_BurialWallSpot", bur, NavMesh.SamplePosition(new Vector3(wallX - 1.0f, Up, wallZ), out var ws, 1f, NavMesh.AllAreas) ? ws.position : new Vector3(wallX - 1.0f, Up, wallZ));
            var wallFace = G1.Marker("CTRL_BurialWallFace", bur, new Vector3(wallX, Up + 0.9f, wallZ));
            var crack = G1.Box("GB_WallCrack", bur, new Vector3(wallX - 0.012f, Up + 1.1f, wallZ), new Vector3(0.02f, 1.5f, 0.12f), darkMat, 0);
            Object.DestroyImmediate(crack.GetComponent<Collider>());
            var hole = G1.Box("GB_WallHole", bur, new Vector3(wallX - 0.012f, Up + 0.95f, wallZ), new Vector3(0.025f, 1.9f, 0.55f), darkMat, 0);
            Object.DestroyImmediate(hole.GetComponent<Collider>());
            var wallHit = G1.Box("GB_FakeWall", bur, new Vector3(wallX - 0.06f, Up + 1.1f, wallZ), new Vector3(0.1f, 2.0f, 0.9f), c.InteractMat, interactable);
            wallHit.GetComponent<Renderer>().enabled = false;
            var wallTI = G1.Interactable(wallHit, "burial_wall", "Break the wall", InteractionMode.Press, 0f);
            var squeeze = G1.Marker("CTRL_SqueezeIn", bur, new Vector3(cavityX, Up, wallZ - 0.3f));
            squeeze.rotation = Quaternion.Euler(0f, 180f, 0f);
            var triangle = G1.Marker("CTRL_Triangle", bur, new Vector3(cavityX, Up, -6.6f));
            var ventGo = G1.Box("GB_Vent", bur, new Vector3(-25.65f, Up + 0.01f, -7.85f), new Vector3(0.7f, 0.02f, 0.7f), darkMat, interactable);
            ventGo.GetComponent<BoxCollider>().size = new Vector3(1f, 40f, 1f);
            var ventTI = G1.Interactable(ventGo, "vent", "Crawl in", InteractionMode.Press, 0f);
            var darkness = Puffs("FX_DarknessRising", bur, ventGo.transform.position + Vector3.up * 0.05f, new Color(0.01f, 0.01f, 0.01f, 0.85f), 0.5f, 1.1f, 0.35f, 18f, true);
            var heldAxe = G1.Box("PROP_HeldAxe (greybox)", c.Interactor.transform, new Vector3(0.28f, -0.3f, 0.5f), new Vector3(0.04f, 0.5f, 0.04f), woodMat, 0, true);
            Object.DestroyImmediate(heldAxe.GetComponent<Collider>());
            heldAxe.transform.localRotation = Quaternion.Euler(25f, 0f, -20f);
            var burialFigure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            burialFigure.name = "GB_BurialGhost";
            Object.DestroyImmediate(burialFigure.GetComponent<Collider>());
            burialFigure.transform.SetParent(bur, false);
            burialFigure.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
            burialFigure.GetComponent<Renderer>().sharedMaterial = ghostMat;
            var via = G1.Marker("CTRL_BurialVia_Storeroom", bur, storeMid);
            var burial = systems.AddComponent<BurialSequence>();
            GreyboxBuilder.Wire(burial, ("motor", c.Motor), ("interactor", c.Interactor), ("torch", torch), ("black", black), ("start", GameObject.Find("CTRL_ShowdownStart").transform),
                ("wallSpot", wallSpot), ("wallFace", wallFace), ("via", via), ("figure", burialFigure), ("crack", crack), ("hole", hole), ("wall", wallTI), ("axe", axeTI),
                ("heldAxe", heldAxe), ("squeezeIn", squeeze), ("triangle", triangle), ("vent", ventTI), ("darkness", darkness), ("lines", lines), ("subtitles", subtitles));

            // ---------- the ending cards (#86): the small outcome line ----------
            var endings = systems.GetComponent<EndingPlayer>();
            var cardRt = c.Hud.Find("EndingCard");
            var outcome = G1.TextAt("Outcome", cardRt, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 24, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(1300, 40), new Vector2(0.5f, 1f));
            outcome.color = new Color(0.75f, 0.72f, 0.66f);
            GreyboxBuilder.Wire(endings, ("burial", burial), ("scenes", scenes), ("outcome", outcome));

            // does the chase really pass the axe? (#85)
            var chasePath = new NavMeshPath();
            bool throughStore = NavMesh.CalculatePath(GameObject.Find("CTRL_ShowdownStart").transform.position, via.position, NavMesh.AllAreas, chasePath)
                && NavMesh.CalculatePath(via.position, wallSpot.position, NavMesh.AllAreas, chasePath);
            log.AppendLine($"gate 5b: storeroom east face x {eastFace:F2} north face z {northFace:F2}, tea now at {hideTea.position:F2}, dream spot {dreamSpot.position:F2} " +
                           $"(window z {lockedNorth:F2}), tub stand {tubStand.position:F2}, his phone {hisPhone.transform.position:F2}, burial wall spot {wallSpot.position:F2}, " +
                           $"chase path {chasePath.status} through the Storeroom={throughStore}");
        }

        static ParticleSystem Puffs(string name, Transform parent, Vector3 pos, Color colour, float sizeMin, float sizeMax, float speed, float rate, bool upwards)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (upwards) go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 2f;
            main.startLifetime = 2.5f; main.startSpeed = speed; main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = colour; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 150;
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 15f; shape.radius = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Fx_FogPuff.mat");
            return ps;
        }

        /// <summary>
        /// The 5b lines, tagged with their writing rule, and the final pass on the ending cards. A card is only rewritten if
        /// it still reads as 5a wrote it: a line the designer has edited is never touched.
        /// </summary>
        static void EnsureLines(MonologueLibrarySO lib, StringBuilder log)
        {
            int added = 0, rewritten = 0;
            const LineRule F = LineRule.FlatAboutTheUnreal, R = LineRule.RawAboutHisLife, O = LineRule.OtherVoice;
            void Add(string id, string text, float s, LineRule rule)
            {
                if (lib.TryGet(id, out _)) return;
                lib.lines.Add(new MonologueLibrarySO.Entry { id = id, text = text, seconds = s, rule = rule });
                added++;
            }
            void Rewrite(string id, string was, string now)
            {
                if (lib.TryGet(id, out var e) && e.text == was) { e.text = now; rewritten++; }
            }
            // Mom's voicemail: a butt-dial, mid-prayer (the mantra is P3, waiting for the designer's yes)
            Add("mom_vm_1", "MOM:  [ rustling ]  …I see you… I'm here…", 3.2f, O);
            Add("mom_vm_2", "MOM:  …stay with— stay with me, stay—", 3.2f, O);
            Add("mom_vm_3", "MOM:  …oh— did I— is this on? How do you—  [ click ]", 3.2f, O);
            Add("mom_vm_after", "…Later, Mom.", 2.2f, R);
            // the tea hunt: heard from upstairs; the blackout
            Add("tea_upstairs", "…That came from upstairs.", 2.4f, F);
            Add("blackout_cold", "…Why is it so cold in here.", 2.4f, F);
            Add("blackout_tub", "…The bath. Why am I in the bath.", 2.6f, F);
            Add("blackout_tub_milk", "…The bath. And the milk. Sure. Why not.", 2.8f, F);
            // the endings
            Add("loop_again", "…Again.", 2.2f, R);
            Add("unemployed_vm", "BOSS (voicemail):  '…don't bother coming in.'", 3.2f, O);
            Add("unemployed_after", "…Yeah. Me too.", 2.4f, R);
            Add("burial_mantra", "I see you. I'm here. Stay with me.", 3.4f, R);
            Add("burial_mantra_2", "…That's what she used to say. When I was sick.", 2.6f, R);
            Add("burial_crack", "It went in. Into the wall.", 2.6f, F);
            Add("burial_axe", "…The axe. In the Storeroom.", 2.6f, F);
            Add("burial_hole", "There's a space in there. Between the walls.", 2.6f, F);
            Add("burial_squeeze", "Sideways. Breathe out. Don't think about it.", 2.8f, F);
            Add("burial_triangle", "…Darkness. Coming up out of the floor. Of course.", 3.0f, F);
            // the card pass (#86): the title, one line about him, and the outcome
            Rewrite("ending_die_line", "In the morning his phone rings, and rings. Nobody picks up.", "In the morning his phone rings, and rings. It's his mother. Nobody picks up.");
            Rewrite("ending_loop_line", "7:00 AM. The alarm. The bag. The toaster. Again.", "7:00 AM. The alarm. The office. The bag. The toaster. He tells himself tonight will be different.");
            Rewrite("ending_unemployed_line", "Voicemail, 7:02 AM: '…don't bother coming in.' Upstairs, something is furious.", "'…don't bother coming in.' He doesn't. The house won't let him forget it.");
            Rewrite("ending_showdown_line", "The boss came for the file. The house is wrecked. The ghost is still here.", "The boss came for the file. He shot him at the gate. The house is wrecked, and the ghost is still here.");
            Rewrite("ending_burial_answered_line", "The ghost runs, and he follows it. (The Burial itself is built in art pass A4.) Voicemail: '…take a few days off.'",
                    "He followed it into the walls, and further down than walls go. Voicemail, 9:40 AM: '…take a few days off.'");
            Rewrite("ending_burial_ignored_line", "The ghost runs, and he follows it. (The Burial itself is built in art pass A4.) Voicemail: '…don't bother coming in.'",
                    "He followed it into the walls, and further down than walls go. Voicemail, 9:40 AM: '…don't bother coming in.'");
            Rewrite("ending_secret_loop_line", "He sleeps through all of it. 7:00 AM. Again.", "He slept through all of it. 7:00 AM. Maybe ignoring it all was the real answer. Maybe not.");
            Rewrite("ending_secret_ghost_line", "He sleeps through all of it. The house keeps him.", "He slept through all of it. The house kept him.");
            Add("ending_die_outcome", "Job: —   ·   Home: —   ·   The ghost: still here", 3f, O);
            Add("ending_loop_outcome", "Job: safe   ·   Home: safe   ·   The ghost: still here", 3f, O);
            Add("ending_unemployed_outcome", "Job: gone   ·   Home: safe   ·   The ghost: furious", 3f, O);
            Add("ending_showdown_outcome", "Job: —   ·   Home: destroyed   ·   The ghost: still here", 3f, O);
            Add("ending_burial_answered_outcome", "Job: safe   ·   Home: safe   ·   The ghost: at peace", 3f, O);
            Add("ending_burial_ignored_outcome", "Job: gone   ·   Home: safe   ·   The ghost: at peace", 3f, O);
            Add("ending_secret_loop_outcome", "Job: safe   ·   Home: safe   ·   The ghost: still here", 3f, O);
            Add("ending_secret_ghost_outcome", "Job: —   ·   Home: —   ·   The ghost: wins", 3f, O);
            if (added + rewritten > 0)
            {
                EditorUtility.SetDirty(lib);
                log.AppendLine($"monologue: added {added} lines, rewrote {rewritten} ending cards");
            }
        }
    }
}
