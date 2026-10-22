using System.Collections.Generic;
using System.Text;
using FedUp.Frustration;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Qte;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Toast;
using FedUp.UI;
using FedUp.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using InteractionMode = FedUp.Interaction.InteractionMode;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 1 content for the greybox: front door + entry zone, frustration system and bar, inner-voice subtitles,
    /// placeholder cutscenes (prologue, file discovery), Drop Your Things, Make Toast, smoke alarm, QTE bar.
    /// Data assets are created once with the designer's values and never overwritten, so tuning survives rebuilds.
    /// </summary>
    public static class GreyboxGate1
    {
        const string Root = "Assets/_Project";
        const float G = 4.83f;

        public readonly struct Context
        {
            public readonly FirstPersonMotor Motor;
            public readonly Interactor Interactor;
            public readonly Transform Hud;
            public readonly Material InteractMat, PropMat;
            public readonly StringBuilder Log;
            public Context(FirstPersonMotor motor, Interactor interactor, Transform hud, Material interactMat, Material propMat, StringBuilder log)
            {
                Motor = motor; Interactor = interactor; Hud = hud; InteractMat = interactMat; PropMat = propMat; Log = log;
            }
        }

        public static void Build(Context c)
        {
            var log = c.Log;
            // ---------- data ----------
            var feFailed = Once<FrustrationEventSO>("Frustration/FE_Toast_FailedOrBurnt", e => { e.amount = 10f; e.description = "Make Toast: burnt toast or failed catch (+10%). Appendix 1."; });
            var feSuccess = Once<FrustrationEventSO>("Frustration/FE_Toast_Success", e => { e.amount = -5f; e.description = "Make Toast: golden toast taken (-5%). Appendix 1."; });
            var feEaten = Once<FrustrationEventSO>("Frustration/FE_Toast_EatenByGhost", e => { e.amount = 10f; e.description = "Make Toast: toast not picked up within 15 s, eaten (+10%). Appendix 1 (from task page)."; });
            var feAlarm = Once<FrustrationEventSO>("Frustration/FE_SmokeAlarm_PerSecond", e => { e.amount = 3f; e.perSecond = true; e.description = "Smoke alarm left ringing (+3% per second). Appendix 1."; });
            var requests = Once<FrustrationRequestChannelSO>("Channels/FrustrationRequests", null);
            var changed = Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var qte = Once<QteDisplayChannelSO>("Channels/QteDisplay", null);
            var toastTuning = Once<ToastTuningSO>("Tasks/ToastTuning", t => { t.failed = feFailed; t.success = feSuccess; t.eatenByGhost = feEaten; });
            var lines = Once<MonologueLibrarySO>("Story/Monologue", m => { m.subtitles = subtitles; m.lines = DefaultLines(); });
            var prologue = Once<CutsceneSO>("Story/CS_Prologue", cs => cs.steps = new List<CutsceneSO.Step>
            {
                new CutsceneSO.Step(CutsceneSO.StepKind.BlackCaption, "Late evening.", 2.0f),
                new CutsceneSO.Step(CutsceneSO.StepKind.BlackCaption, "The factories never sleep. Neither does the smog.", 2.6f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Line, "prologue_chatbot", 3.4f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Tone, "buzz", 0.9f),
                new CutsceneSO.Step(CutsceneSO.StepKind.BlackCaption, "212 unread messages.", 1.8f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Line, "prologue_later", 1.4f),
                new CutsceneSO.Step(CutsceneSO.StepKind.FadeIn, "", 1.5f),
            });
            var fileScene = Once<CutsceneSO>("Story/CS_FileDiscovery", cs => cs.steps = new List<CutsceneSO.Step>
            {
                new CutsceneSO.Step(CutsceneSO.StepKind.LookAt, "GB_BossFile", 0.8f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Tone, "choir", 0.2f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Caption, "[ a choir hits one note ]", 1.8f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Line, "file_what", 2.4f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Caption, "CONFIDENTIAL  —  Q4 Restructuring Plan", 2.2f),
                new CutsceneSO.Step(CutsceneSO.StepKind.Line, "file_not_mine", 2.6f),
            });
            if (toastTuning.failed == null) { toastTuning.failed = feFailed; toastTuning.success = feSuccess; toastTuning.eatenByGhost = feEaten; EditorUtility.SetDirty(toastTuning); }
            if (lines.subtitles == null) { lines.subtitles = subtitles; EditorUtility.SetDirty(lines); }

            var storyMat = GreyboxBuilder.Mat("M_Greybox_Story", new Color(0.55f, 0.08f, 0.08f));
            var burntMat = GreyboxBuilder.Mat("M_Greybox_Burnt", new Color(0.05f, 0.04f, 0.03f));
            var ghostMat = GreyboxBuilder.Mat("M_Greybox_Ghost", new Color(0.01f, 0.01f, 0.015f));
            int interactable = LayerMask.NameToLayer("Interactable");

            var root = new GameObject("GATE1").transform;

            // ---------- front door (west porch) ----------
            var hinge = new GameObject("CTRL_FrontDoorHinge").transform;
            hinge.SetParent(root);
            hinge.position = new Vector3(-47.92f, G, -3.88f);
            var panel = Box("PREF_Door_Front (greybox)", hinge, new Vector3(0f, 1.09f, 0.56f), new Vector3(0.06f, 2.18f, 1.1f), c.PropMat, interactable, local: true);
            var doorHandle = panel.AddComponent<TaskInteractable>();
            doorHandle.id = "front_door";
            var door = panel.AddComponent<FrontDoor>();
            GreyboxBuilder.Wire(door, ("handle", doorHandle), ("hinge", hinge), ("lines", lines));
            SetFloat(door, "openAngle", 100f);
            var zone = new GameObject("GB_DoorEntryZone");
            zone.transform.SetParent(root);
            zone.transform.position = new Vector3(-46.3f, G + 1f, -3.3f);
            zone.layer = LayerMask.NameToLayer("Ignore Raycast");
            var zc = zone.AddComponent<BoxCollider>(); zc.isTrigger = true; zc.size = new Vector3(1.2f, 2f, 2.4f);
            var dez = zone.AddComponent<DoorEntryZone>();
            GreyboxBuilder.Wire(dez, ("door", door));

            // ---------- drop your things ----------
            var hook = Box("PREF_CoatHook (greybox)", root, new Vector3(-47.78f, G + 1.65f, -1.5f), new Vector3(0.08f, 0.08f, 0.3f), c.InteractMat, interactable);
            hook.GetComponent<BoxCollider>().size = new Vector3(4f, 6f, 2f);
            var hookTI = Interactable(hook, "coat_hook", "Hang your jacket", InteractionMode.Press, 0f);
            var jacket = Box("GB_Jacket", root, new Vector3(-47.70f, G + 1.2f, -1.5f), new Vector3(0.14f, 0.85f, 0.5f), c.PropMat, 0);
            Object.DestroyImmediate(jacket.GetComponent<Collider>());
            var haunt = jacket.AddComponent<JacketHaunt>();
            GreyboxBuilder.Wire(haunt, ("viewer", c.Interactor.transform));

            var rack = Box("PREF_ShoeRack (greybox)", root, new Vector3(-47.55f, G + 0.2f, -0.75f), new Vector3(0.45f, 0.4f, 0.8f), c.InteractMat, interactable);
            var rackTI = Interactable(rack, "shoe_rack", "Kick off your shoes", InteractionMode.Press, 0f);
            var shoes = Box("GB_Shoes", rack.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.8f, 0.3f, 0.6f), c.PropMat, 0, local: true);
            Object.DestroyImmediate(shoes.GetComponent<Collider>());

            var table = Box("PREF_SideTable (greybox)", root, new Vector3(-47.45f, G + 0.375f, -5.6f), new Vector3(0.6f, 0.75f, 0.6f), c.InteractMat, interactable);
            var tableTI = Interactable(table, "bag_table", "Empty your bag", InteractionMode.Hold, 1.2f);
            var contents = new GameObject("GB_BagContents"); contents.transform.SetParent(root);
            var bag = Box("GB_Bag", contents.transform, new Vector3(-47.5f, G + 0.88f, -5.75f), new Vector3(0.38f, 0.26f, 0.2f), c.PropMat, 0);
            Object.DestroyImmediate(bag.GetComponent<Collider>());
            var file = Box("GB_BossFile", root, new Vector3(-47.35f, G + 0.765f, -5.45f), new Vector3(0.24f, 0.03f, 0.31f), storyMat, 0);
            Object.DestroyImmediate(file.GetComponent<Collider>());

            // ---------- kitchen: toaster, smoke alarm ----------
            Vector3 counter = Surface(new Vector3(-30.8f, 0f, -2.0f), G + 0.5f, G + 1.3f, log, "kitchen counter");
            var toasterGo = Box("PREF_Toaster (greybox)", root, counter + new Vector3(0f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 0.28f), c.InteractMat, interactable);
            toasterGo.GetComponent<BoxCollider>().size = new Vector3(2.2f, 1.8f, 1.6f);
            var toasterTI = Interactable(toasterGo, "toaster", "Make toast", InteractionMode.Press, 0f);
            var spawn = new GameObject("CTRL_ToastSpawn").transform;
            spawn.SetParent(root); spawn.position = counter + new Vector3(0f, 0.24f, 0f);
            var golden = Box("PREF_Toast (greybox)", root, counter + new Vector3(0f, 0.26f, 0f), new Vector3(0.12f, 0.02f, 0.12f), GreyboxBuilder.Mat("M_Greybox_Toast", new Color(0.8f, 0.55f, 0.2f)), interactable);
            golden.GetComponent<BoxCollider>().size = new Vector3(3f, 12f, 3f);
            var goldenTI = Interactable(golden, "toast_pickup", "Take the toast", InteractionMode.Press, 0f);

            var targets = new Transform[3];
            targets[0] = Marker("CTRL_ToastLands_Floor", root, new Vector3(-32.6f, G + 0.02f, -1.2f));
            targets[1] = Marker("CTRL_ToastLands_Counter", root, Surface(new Vector3(-30.8f, 0f, -3.4f), G + 0.5f, G + 1.3f, log, "far counter") + Vector3.up * 0.02f);
            Vector3 ceiling = Ceiling(new Vector3(-32.4f, G + 1.5f, -2.4f), log);
            targets[2] = Marker("CTRL_ToastLands_Fan", root, ceiling - Vector3.up * 0.35f);

            var cat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cat.name = "GB_GhostCat"; cat.transform.SetParent(root);
            cat.transform.position = counter + new Vector3(0f, 0.15f, 0.55f);
            cat.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cat.transform.localScale = new Vector3(0.18f, 0.2f, 0.18f);
            Object.DestroyImmediate(cat.GetComponent<Collider>());
            cat.GetComponent<MeshRenderer>().sharedMaterial = ghostMat;

            var alarmGo = Box("PREF_SmokeAlarm (greybox)", root, ceiling - Vector3.up * 0.03f, new Vector3(0.16f, 0.04f, 0.16f), c.InteractMat, interactable);
            alarmGo.GetComponent<BoxCollider>().size = new Vector3(3f, 8f, 3f);
            var alarmTI = Interactable(alarmGo, "smoke_alarm", "Silence the smoke alarm", InteractionMode.Press, 0f);
            var blinkGo = new GameObject("LGT_AlarmBlink"); blinkGo.transform.SetParent(alarmGo.transform, false);
            blinkGo.transform.localPosition = new Vector3(0f, -3f, 0f);
            var blink = blinkGo.AddComponent<Light>(); blink.type = LightType.Point; blink.color = Color.red; blink.range = 3f; blink.intensity = 2f;
            var alarm = alarmGo.AddComponent<SmokeAlarm>();
            GreyboxBuilder.Wire(alarm, ("button", alarmTI), ("frustration", requests), ("perSecond", feAlarm), ("blinkLight", blink), ("lines", lines));

            // ---------- systems ----------
            var systems = new GameObject("SYSTEMS");
            var fs = systems.AddComponent<FrustrationSystem>();
            GreyboxBuilder.Wire(fs, ("requests", requests), ("changed", changed));
            var cutscenes = systems.AddComponent<CutscenePlayer>();
            var drop = systems.AddComponent<DropThingsTask>();
            drop.objective = "Drop your things";
            GreyboxBuilder.Wire(drop, ("coatHook", hookTI), ("shoeRack", rackTI), ("bagTable", tableTI), ("jacketOnHook", jacket),
                ("shoesOnRack", shoes), ("bagContents", contents), ("bossFile", file), ("jacket", haunt), ("cutscenes", cutscenes),
                ("fileDiscovery", fileScene), ("lines", lines));
            var toast = systems.AddComponent<MakeToastTask>();
            toast.objective = "Make toast";
            GreyboxBuilder.Wire(toast, ("tuning", toastTuning), ("toaster", toasterTI), ("toastPickup", goldenTI), ("toastSpawn", spawn),
                ("launchTargets", new Object[] { targets[0], targets[1], targets[2] }), ("ghostCat", cat), ("alarm", alarm),
                ("frustration", requests), ("frustrationState", changed), ("qte", qte), ("lines", lines), ("burntMaterial", burntMat));
            var runner = systems.AddComponent<TaskRunner>();

            // ---------- HUD ----------
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // frustration bar (top-left)
            var barRoot = Rect("FrustrationBar", c.Hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -64), new Vector2(420, 22), new Vector2(0, 1));
            var barBg = barRoot.gameObject.AddComponent<Image>(); barBg.color = new Color(0f, 0f, 0f, 0.55f);
            var fill = Rect("Fill", barRoot, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Vector2(0, 0.5f));
            var fillImg = fill.gameObject.AddComponent<Image>();
            var barLabel = TextAt("Label", barRoot, font, 26, TextAnchor.LowerLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 32), new Vector2(520, 34), new Vector2(0, 0.5f));
            var barDelta = TextAt("Delta", barRoot, font, 30, TextAnchor.MiddleLeft, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(12, 0), new Vector2(80, 30), new Vector2(0, 0.5f));
            var bar = barRoot.gameObject.AddComponent<FrustrationBarUI>();
            GreyboxBuilder.Wire(bar, ("changed", changed), ("fill", fill), ("fillImage", fillImg), ("label", barLabel), ("deltaText", barDelta));
            // objective (top-right)
            var objective = TextAt("Objective", c.Hud, font, 32, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -36), new Vector2(800, 40), new Vector2(1, 1));
            GreyboxBuilder.Wire(runner, ("tasks", new Object[] { drop, toast }), ("objectiveText", objective), ("lines", lines));
            // subtitles (bottom)
            var sub = TextAt("Subtitle", c.Hud, font, 36, TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(1500, 140), new Vector2(0.5f, 0));
            sub.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var subUI = sub.gameObject.AddComponent<SubtitleUI>();
            GreyboxBuilder.Wire(subUI, ("subtitles", subtitles), ("text", sub));
            // QTE bar (centre, below crosshair)
            var qteRoot = Rect("QteBar", c.Hud, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(520, 24), new Vector2(0.5f, 0.5f));
            qteRoot.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var qZone = Rect("GoldenZone", qteRoot, new Vector2(0.6f, 0), new Vector2(0.75f, 1), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            qZone.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.75f, 0.25f, 0.85f);
            var qMarker = Rect("Marker", qteRoot, new Vector2(0, -0.3f), new Vector2(0, 1.3f), Vector2.zero, new Vector2(5, 0), new Vector2(0.5f, 0.5f));
            qMarker.gameObject.AddComponent<Image>().color = Color.white;
            var qMash = Rect("MashFill", qteRoot, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, new Vector2(0, 0.5f));
            qMash.gameObject.AddComponent<Image>().color = new Color(0.9f, 0.35f, 0.2f, 0.9f);
            var qLabel = TextAt("Label", qteRoot, font, 28, TextAnchor.LowerCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 30), new Vector2(600, 30), new Vector2(0.5f, 0.5f));
            var qteUI = c.Hud.gameObject.AddComponent<QteBarUI>();
            GreyboxBuilder.Wire(qteUI, ("channel", qte), ("root", qteRoot.gameObject), ("zone", qZone), ("marker", qMarker), ("mashFill", qMash), ("label", qLabel));
            // cutscene overlay (on top of everything)
            var black = Rect("CutsceneBlack", c.Hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var blackImg = black.gameObject.AddComponent<Image>(); blackImg.color = new Color(0, 0, 0, 0); blackImg.raycastTarget = false;
            var caption = TextAt("Caption", c.Hud, font, 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1500, 120), new Vector2(0.5f, 0.5f));
            caption.color = new Color(0.92f, 0.9f, 0.85f);
            GreyboxBuilder.Wire(cutscenes, ("prologue", prologue), ("motor", c.Motor), ("interactor", c.Interactor), ("lines", lines),
                ("subtitles", subtitles), ("blackPanel", blackImg), ("captionText", caption));

            EditorUtility.SetDirty(toastTuning);
            log.AppendLine($"gate 1: door, drop-things props, toaster at {counter:F2}, smoke alarm at {ceiling:F2}, systems and HUD");
        }

        // ------------------------------------------------------------------ data

        static List<MonologueLibrarySO.Entry> DefaultLines()
        {
            var l = new List<MonologueLibrarySO.Entry>();
            void Add(string id, string text, float s) => l.Add(new MonologueLibrarySO.Entry { id = id, text = text, seconds = s });
            Add("prologue_chatbot", "Nine hours of making a chatbot sound human. It's better at it than me.", 3.4f);
            Add("prologue_later", "…Later.", 1.6f);
            Add("door_home", "Home. Technically.", 2.2f);
            Add("hang_jacket", "Hang up the costume.", 2.2f);
            Add("shoes_off", "Shoes off. Best part of the day. Every day.", 2.6f);
            Add("file_what", "Huh… what the hell is this?", 2.4f);
            Add("file_not_mine", "That's not mine. That is very much not mine.", 2.8f);
            Add("toaster_start", "Toast. A man can still make toast.", 2.6f);
            Add("toaster_jammed", "Jammed. Naturally.", 2.0f);
            Add("toaster_unjammed", "There we go. Violence works.", 2.2f);
            Add("toast_burnt", "Of course it burns.", 2.2f);
            Add("toast_launched", "Great. Even my toaster hates me.", 2.4f);
            Add("toast_golden", "Golden. Don't touch it. Don't breathe on it.", 2.6f);
            Add("toast_taken", "Finally. Something in this house works.", 2.6f);
            Add("toast_eaten", "…Did something just eat my toast?", 2.6f);
            Add("alarm", "No no no — not the alarm.", 2.2f);
            Add("alarm_silenced", "…Silence. Beautiful.", 2.2f);
            Add("gate1_done", "You're still here? …Huh. Nobody stays.", 3.2f);
            return l;
        }

        /// <summary>Loads Data/&lt;path&gt;.asset, or creates it with <paramref name="init"/> the first time. Never overwrites.</summary>
        static T Once<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            string full = $"{Root}/Data/{path}.asset";
            var a = AssetDatabase.LoadAssetAtPath<T>(full);
            if (a != null) return a;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
            a = ScriptableObject.CreateInstance<T>();
            init?.Invoke(a);
            AssetDatabase.CreateAsset(a, full);
            return a;
        }

        // ------------------------------------------------------------------ scene helpers

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, int layer, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.layer = layer;
            return go;
        }

        static TaskInteractable Interactable(GameObject go, string id, string prompt, InteractionMode mode, float hold)
        {
            var t = go.AddComponent<TaskInteractable>();
            t.id = id; t.prompt = prompt; t.mode = mode; t.holdSeconds = hold;
            return t;
        }

        static Transform Marker(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent); t.position = pos;
            return t;
        }

        static Vector3 Surface(Vector3 xz, float minY, float maxY, StringBuilder log, string what)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(xz.x, maxY + 0.5f, xz.z), Vector3.down, 3f))
                if (h.point.y >= minY && h.point.y <= maxY) return h.point;
            log.AppendLine($"WARNING no {what} surface found at {xz}; using {minY + 0.45f}");
            return new Vector3(xz.x, minY + 0.45f, xz.z);
        }

        static Vector3 Ceiling(Vector3 from, StringBuilder log)
        {
            if (Physics.Raycast(from, Vector3.up, out var h, 4f)) return h.point;
            log.AppendLine("WARNING no kitchen ceiling found for the smoke alarm");
            return from + Vector3.up * 1.4f;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static Text TextAt(string name, Transform parent, Font font, int size, TextAnchor align, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta, Vector2 pivot)
        {
            var rt = Rect(name, parent, aMin, aMax, pos, sizeDelta, pivot);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = align; t.color = new Color(0.95f, 0.92f, 0.85f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.text = "";
            return t;
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
