using System.Linq;
using System.Text;
using FedUp.Frustration;
using FedUp.Ghost;
using FedUp.Ghost.Scares;
using FedUp.Player;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Night;
using FedUp.Tasks.Tv;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using InteractionMode = FedUp.Interaction.InteractionMode;
using G1 = FedUp.EditorTools.GreyboxGate1;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 3: falling asleep on the couch, the Phase 2 wake-up (the "sick" house), the ghost director, and three
    /// scares on the wake-up route (hallway footsteps, the bathroom mirror, the bathroom door). Every placement
    /// has its design reason beside it; the full list is DESIGN_LOGIC.md #30-39.
    /// </summary>
    public static class GreyboxGate3
    {
        const float G = 4.83f;
        const string MirrorLayer = "MirrorOnly";

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");
            int mirrorOnly = EnsureLayer(MirrorLayer, log);

            // ---------- data ----------
            var directorTuning = G1.Once<GhostDirectorTuningSO>("Ghost/GhostDirectorTuning", null);
            var wakeUp = G1.Once<CutsceneSO>("Story/CS_WakeUp", cs =>
            {
                cs.steps.Add(new CutsceneSO.Step(CutsceneSO.StepKind.BlackCaption, "3:07 AM", 2.6f));
                cs.steps.Add(new CutsceneSO.Step(CutsceneSO.StepKind.FadeIn, "", 2.4f));
                cs.steps.Add(new CutsceneSO.Step(CutsceneSO.StepKind.Line, "wake_cold", 2.6f));
            });
            var changed = G1.Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            EnsureLines(lines, log);
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/FedUpControls.inputactions");
            var root = new GameObject("GATE3").transform;
            var systems = GameObject.Find("SYSTEMS");
            var eye = c.Interactor.transform;
            var mainCam = eye.GetComponent<Camera>();
            mainCam.cullingMask &= ~(1 << mirrorOnly); // the figure exists only in the mirror

            // ---------- the director ----------
            var director = systems.AddComponent<GhostDirector>();
            GreyboxBuilder.Wire(director, ("tuning", directorTuning), ("frustrationState", changed));

            // ---------- the phone torch (#33): a narrow cone from his hand, so the dark stays dark around it ----------
            var torchGo = new GameObject("LGT_PhoneTorch");
            torchGo.transform.SetParent(eye, false);
            torchGo.transform.localPosition = new Vector3(0.18f, -0.15f, 0.1f);
            var beam = torchGo.AddComponent<Light>();
            beam.type = LightType.Spot; beam.spotAngle = 62f; beam.innerSpotAngle = 26f; beam.range = 15f; beam.intensity = 20f;
            beam.color = new Color(0.86f, 0.9f, 1f); beam.shadows = LightShadows.Soft;
            var torch = torchGo.AddComponent<PhoneTorch>();
            GreyboxBuilder.Wire(torch, ("controls", controls));

            // ---------- Phase 2 look (#32): colder, greyer, heavier edges ----------
            var look = Profile("Assets/_Project/Settings/Phase2Look.asset", p =>
            {
                var wb = p.Add<WhiteBalance>(true); wb.temperature.value = -30f;
                var ca = p.Add<ColorAdjustments>(true); ca.saturation.value = -30f; ca.postExposure.value = -0.1f; ca.contrast.value = 12f;
                var vg = p.Add<Vignette>(true); vg.intensity.value = 0.45f;
            });
            var lookGo = new GameObject("FX_Phase2Look");
            lookGo.transform.SetParent(root, false);
            var lookVol = lookGo.AddComponent<Volume>();
            lookVol.isGlobal = true; lookVol.priority = 5f; lookVol.weight = 0f; lookVol.sharedProfile = look;

            // drone + whispers (GDD Phase 2: "audio shifts to include whispers, droning")
            var drone = NewSource("SFX_Drone", root, 0.32f);
            var whisper = NewSource("SFX_Whispers", root, 0.09f);

            // the lights that stay on draw the path: back-hall bulb at the end of the hallway, the bathroom you're
            // heading for (the TV static is the third, handled by the TV)
            var roomLights = GameObject.Find("Lights").GetComponentsInChildren<Light>(true);
            var keep = roomLights.Where(l => l.name == "LGT_BathroomDownstairs" || l.name == "LGT_BackHall").ToArray();
            var phase = systems.AddComponent<NightPhaseController>();
            GreyboxBuilder.Wire(phase, ("roomLights", roomLights.Cast<Object>().ToArray()), ("keepLit", keep.Cast<Object>().ToArray()),
                ("phase2Look", lookVol), ("drone", drone), ("whispers", whisper), ("torch", torch), ("director", director));

            // ---------- falling asleep (#30, #31): on the couch, facing the TV ----------
            var couchBox = GameObject.Find("GB_3seat4(1)").GetComponent<Renderer>().bounds;
            var lie = G1.Box("GB_CouchLie", root, new Vector3(couchBox.center.x, couchBox.max.y + 0.09f, couchBox.center.z), new Vector3(couchBox.size.x - 0.2f, 0.06f, couchBox.size.z - 0.1f), c.InteractMat, interactable);
            lie.GetComponent<Renderer>().enabled = false;
            var lieTI = G1.Interactable(lie, "couch_sleep", "Lie down", InteractionMode.Hold, 1.2f);

            // ---------- the downstairs bathroom: sink + mirror on the south wall, facing the doorway (#35) ----------
            var room = GameObject.Find("ROOM_BathroomDownstairs").transform.position;
            Vector3 probe = new Vector3(room.x, G + 1.2f, room.z);
            float zSouth = Hit(probe, Vector3.back, 9f).z, zNorth = Hit(probe, Vector3.forward, 9f).z, xWest = Hit(probe, Vector3.left, 9f).x;
            // the doorway: the gap in the north wall
            float doorMin = float.MaxValue, doorMax = float.MinValue;
            for (float x = xWest + 0.05f; x < room.x + 2f; x += 0.05f)
            {
                if (Physics.Raycast(new Vector3(x, G + 1.2f, zNorth - 0.6f), Vector3.forward, 1.4f)) continue;
                doorMin = Mathf.Min(doorMin, x); doorMax = Mathf.Max(doorMax, x);
            }
            float doorMid = (doorMin + doorMax) / 2f, doorWidth = doorMax - doorMin + 0.05f;
            float sinkX = doorMid + 0.35f; // in line with the doorway, so the reflection is centred on it
            var sink = G1.Box("PREF_Sink (greybox)", root, new Vector3(sinkX, G + 0.42f, zSouth + 0.24f), new Vector3(0.6f, 0.84f, 0.45f), c.InteractMat, interactable);
            var sinkTI = G1.Interactable(sink, "sink", "Splash water on your face", InteractionMode.Press, 0f);

            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/_Project/Art/RT_Mirror.renderTexture");
            if (rt == null) { rt = new RenderTexture(512, 640, 24) { name = "RT_Mirror" }; AssetDatabase.CreateAsset(rt, "Assets/_Project/Art/RT_Mirror.renderTexture"); }
            // brighter than a real mirror: the reflection of a dim room would vanish under the night grade
            var mirrorMat = Unlit("M_Greybox_Mirror", new Color(2.4f, 2.4f, 2.4f, 1f));
            mirrorMat.SetTexture("_BaseMap", rt);
            mirrorMat.SetTextureScale("_BaseMap", new Vector2(-1f, 1f)); // a mirror flips left and right
            mirrorMat.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
            var mirrorRoot = new GameObject("PREF_Mirror (greybox)");
            mirrorRoot.transform.SetParent(root, false);
            mirrorRoot.transform.position = new Vector3(sinkX, G + 1.6f, zSouth + 0.015f); // forward (+z) faces into the room
            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "PRT_Glass";
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.transform.SetParent(mirrorRoot.transform, false);
            glass.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            glass.transform.localScale = new Vector3(0.6f, 0.8f, 1f);
            glass.GetComponent<Renderer>().sharedMaterial = mirrorMat;
            var crackMat = Unlit("M_Greybox_MirrorCrack", Color.white);
            crackMat.SetTexture("_BaseMap", CrackTexture());
            crackMat.SetFloat("_AlphaClip", 1f); crackMat.SetFloat("_Cutoff", 0.5f); crackMat.EnableKeyword("_ALPHATEST_ON");
            var crack = GameObject.CreatePrimitive(PrimitiveType.Quad);
            crack.name = "PRT_Crack";
            Object.DestroyImmediate(crack.GetComponent<Collider>());
            crack.transform.SetParent(mirrorRoot.transform, false);
            crack.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            crack.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            crack.transform.localScale = new Vector3(0.6f, 0.8f, 1f);
            crack.GetComponent<Renderer>().sharedMaterial = crackMat;

            var camGo = new GameObject("CAM_Mirror");
            camGo.transform.SetParent(root, false);
            var mirrorCam = camGo.AddComponent<Camera>();
            mirrorCam.targetTexture = rt; mirrorCam.enabled = false; mirrorCam.depth = -5f;
            mirrorCam.clearFlags = CameraClearFlags.SolidColor; mirrorCam.backgroundColor = Color.black;
            mirrorCam.nearClipPlane = 0.05f; mirrorCam.farClipPlane = 30f; mirrorCam.allowHDR = false;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;
            var view = mirrorRoot.AddComponent<MirrorView>();
            GreyboxBuilder.Wire(view, ("viewer", mainCam), ("mirrorCamera", mirrorCam));

            // the figure (#36): right behind you, between you and the doorway, visible only in the mirror. Pale, so
            // it reads in a split second in the dark.
            var figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            figure.name = "GB_MirrorFigure";
            Object.DestroyImmediate(figure.GetComponent<Collider>());
            figure.transform.SetParent(root, false);
            figure.transform.position = new Vector3(doorMid, G + 0.88f, zNorth - 0.45f);
            figure.transform.localScale = new Vector3(0.5f, 0.88f, 0.35f);
            figure.layer = mirrorOnly;
            // unlit: a flat pale shape that shows whatever the light (lit materials sank into the dark)
            figure.GetComponent<Renderer>().sharedMaterial = Unlit("M_Greybox_FigureUnlit", new Color(0.62f, 0.63f, 0.66f));
            // your stand-in body for the reflection (the greybox player has none)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "GB_ReflectionBody";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var bodyMesh = body.transform;
            var bodyRoot = new GameObject("CTRL_ReflectionBody").transform;
            bodyRoot.SetParent(root, false);
            bodyMesh.SetParent(bodyRoot, false);
            bodyMesh.localPosition = new Vector3(0f, 0.88f, 0f);
            bodyMesh.localScale = new Vector3(0.48f, 0.88f, 0.3f);
            body.layer = mirrorOnly;
            body.GetComponent<Renderer>().sharedMaterial = GreyboxBuilder.Mat("M_Greybox_Shirt", new Color(0.12f, 0.13f, 0.16f)); // dark, so the pale figure behind it pops

            var mirrorScare = mirrorRoot.AddComponent<MirrorScare>();
            GreyboxBuilder.Wire(view, ("reflectionBody", bodyRoot));
            GreyboxBuilder.Wire(mirrorScare, ("director", director), ("sink", sinkTI), ("mirror", view), ("eye", eye), ("figure", figure), ("crack", crack), ("lines", lines));

            // ---------- the bathroom door (#37, #38): hinged on the doorway's east edge, open into the room ----------
            var hinge = new GameObject("CTRL_BathroomDoorHinge").transform;
            hinge.SetParent(root, false);
            hinge.position = new Vector3(doorMax + 0.02f, G, zNorth + 0.03f);
            var leaf = G1.Box("PRT_BathroomDoor", hinge, Vector3.zero, new Vector3(doorWidth, 2.1f, 0.05f), GreyboxBuilder.Mat("M_Greybox_Door", new Color(0.32f, 0.27f, 0.22f)), 0);
            leaf.transform.localPosition = new Vector3(-doorWidth / 2f, 1.05f, 0f);
            var doorway = new GameObject("CTRL_BathroomDoorway");
            doorway.transform.SetParent(root, false);
            doorway.transform.position = new Vector3(doorMid, G, zNorth);
            var fog = FogPuffs(doorway.transform, doorWidth);
            GreyboxBuilder.Wire(mirrorScare, ("wayOut", doorway.transform));
            var slam = doorway.AddComponent<DoorSlamScare>();
            GreyboxBuilder.Wire(slam, ("director", director), ("armedAfter", mirrorScare), ("hinge", hinge), ("player", c.Motor.transform),
                ("eye", eye), ("fog", fog), ("lines", lines));

            // ---------- footsteps (#34): the long hallway from the stairs to the back hall ----------
            var hall = new GameObject("CTRL_HallwayFootsteps");
            hall.transform.SetParent(root, false);
            hall.transform.position = new Vector3(-32.5f, G + 1f, 1.0f);
            var steps = hall.AddComponent<FootstepStalker>();
            GreyboxBuilder.Wire(steps, ("director", director), ("player", c.Motor.transform), ("eye", eye), ("lines", lines));

            // ---------- tasks ----------
            var tv = Object.FindAnyObjectByType<TvTask>();
            var sleep = systems.AddComponent<SleepTask>();
            sleep.objective = "Lie down on the couch";
            GreyboxBuilder.Wire(sleep, ("couch", lieTI), ("lieSpot", GameObject.Find("CTRL_CouchSeat").transform), ("standSpot", GameObject.Find("CTRL_CouchStand").transform),
                ("lookAt", GameObject.Find("PREF_TVScreen (greybox)").transform), ("tv", tv), ("phase", phase), ("cutscenes", Object.FindAnyObjectByType<CutscenePlayer>()),
                ("wakeUp", wakeUp), ("motor", c.Motor), ("interactor", c.Interactor), ("lines", lines), ("subtitles", subtitles));
            var splash = systems.AddComponent<UseTask>();
            splash.objective = "Splash water on your face (downstairs bathroom)";
            GreyboxBuilder.Wire(splash, ("target", sinkTI), ("lines", lines));
            G1.SetString(splash, "lineId", "sink_splash");
            var back = systems.AddComponent<ReachTask>();
            back.objective = "Go back to the living room";
            GreyboxBuilder.Wire(back, ("destination", GameObject.Find("ROOM_LivingRoom").transform), ("player", c.Motor.transform), ("lines", lines));
            G1.SetString(back, "lineId", "gate3_done"); // "...I'm not going back to sleep." (the night carries on)

            var runner = systems.GetComponent<TaskRunner>();
            var so = new SerializedObject(runner);
            var list = so.FindProperty("tasks");
            foreach (var t in new TaskBehaviour[] { sleep, splash, back })
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = t;
            }
            so.FindProperty("allDoneLineId").stringValue = "gate3_done";
            so.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"gate 3: bathroom doorway x {doorMin:F2}..{doorMax:F2} at z {zNorth:F2}; sink + mirror at x {sinkX:F2} z {zSouth:F2}; " +
                           $"{roomLights.Length} room lights, {keep.Length} kept lit; footsteps zone at {hall.transform.position:F1}");
        }

        // ------------------------------------------------------------------ helpers

        static Vector3 Hit(Vector3 from, Vector3 dir, float max) => Physics.Raycast(from, dir, out var h, max) ? h.point : from + dir * max;

        static AudioSource NewSource(string name, Transform parent, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false; a.loop = true; a.spatialBlend = 0f; a.volume = volume;
            return a;
        }

        static VolumeProfile Profile(string path, System.Action<VolumeProfile> init)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p != null) return p;
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            init(p);
            foreach (var comp in p.components) AssetDatabase.AddObjectToAsset(comp, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        internal static Material Unlit(string name, Color c)
        {
            string path = $"Assets/_Project/Art/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Dark crack lines radiating from an impact point, on a transparent background.</summary>
        static Texture2D CrackTexture()
        {
            const string path = "Assets/_Project/Art/Textures/T_MirrorCrack.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int w = 256, h = 340;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) tex.SetPixel(x, y, clear);
            var rng = new System.Random(7);
            Vector2 impact = new Vector2(w * 0.62f, h * 0.58f);
            for (int r = 0; r < 11; r++)
            {
                float a = (float)(rng.NextDouble() * Mathf.PI * 2);
                Vector2 p = impact;
                float len = 60 + (float)rng.NextDouble() * 160;
                for (float s = 0; s < len; s += 1f)
                {
                    a += (float)(rng.NextDouble() - 0.5) * 0.12f;
                    p += new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    for (int dx = -1; dx <= 0; dx++)
                    {
                        int px = (int)p.x + dx, py = (int)p.y;
                        if (px >= 0 && px < w && py >= 0 && py < h) tex.SetPixel(px, py, new Color(0.05f, 0.05f, 0.06f, 1f));
                    }
                }
            }
            tex.Apply();
            System.IO.Directory.CreateDirectory("Assets/_Project/Art/Textures");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        internal static Texture2D SoftPuffTexture()
        {
            const string path = "Assets/_Project/Art/Textures/T_SoftPuff.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int x = 0; x < n; x++) for (int y = 0; y < n; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            System.IO.Directory.CreateDirectory("Assets/_Project/Art/Textures");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Purple fog seeping out along the floor (GDD: purple fog is the supernatural signal).</summary>
        internal static ParticleSystem FogPuffs(Transform doorway, float width)
        {
            var go = new GameObject("FX_DoorFog");
            go.transform.SetParent(doorway, false);
            go.transform.localPosition = new Vector3(0f, 0.25f, -0.4f); // just inside, drifting out through the gap
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 4f;
            main.startLifetime = 5f; main.startSpeed = 0.3f; main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startColor = new Color(0.5f, 0.32f, 0.72f, 0.32f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 80; main.gravityModifier = -0.01f;
            var em = ps.emission; em.rateOverTime = 7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(width, 0.2f, 0.2f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Fx_FogPuff.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, "Assets/_Project/Art/Materials/M_Fx_FogPuff.mat");
            }
            mat.SetTexture("_BaseMap", SoftPuffTexture());
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        static int EnsureLayer(string name, StringBuilder log)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = name;
                tm.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine($"added layer {name} at {i}");
                return i;
            }
            Debug.LogError("No free layer for " + name);
            return 0;
        }

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
            Add("sleep_lie", "Five minutes. Just five.", 2.4f);
            Add("wake_cold", "…Cold. Why is it so cold.", 2.6f);
            Add("sink_splash", "Get it together. It's just a house.", 2.6f);
            Add("steps_turn", "…Hello?", 1.8f);
            Add("mirror_after", "No. No, no, no.", 2.2f);
            Add("door_slam", "I'm not going back in there.", 2.4f);
            Add("back_living", "Lights. I need lights.", 2.2f);
            Add("gate3_done", "…I'm not going back to sleep.", 2.6f);
            if (added > 0) { EditorUtility.SetDirty(lib); log.AppendLine($"monologue: added {added} new lines"); }
        }
    }
}
