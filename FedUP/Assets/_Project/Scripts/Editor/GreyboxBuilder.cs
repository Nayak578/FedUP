using System.Collections.Generic;
using System.Linq;
using System.Text;
using FedUp.DebugTools;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.UI;
using FedUp.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Builds _Project/Scenes/Greybox.unity from scratch: the cleaned house in grey, box stand-ins at the
    /// furniture positions of firstdraft.unity, room markers, sealed exits, atmosphere kit, player, HUD and
    /// the Slice 0 light switch. Re-runnable: it regenerates the scene, so hand edits belong in the builder.
    /// </summary>
    public static class GreyboxBuilder
    {
        const string Root = "Assets/_Project";
        const string HouseFbx = Root + "/Art/Models/Environment/ENV_Apartment_v02.fbx";
        const string ReferenceScene = "Assets/firstdraft.unity";
        const float GroundFloor = 4.83f, UpperFloor = 8.27f;
        // firstdraft places the house root (ver2) at (-60.4, 0, 30.8); the greybox puts the house at the origin.
        static readonly Vector3 ReferenceToGreybox = new Vector3(60.4f, 0f, -30.8f);

        static readonly (string name, Vector3 pos, int tour)[] Rooms =
        {
            ("Entrance",            new Vector3(-46.6f, GroundFloor, -3.3f), 0),
            ("Back Hall",           new Vector3(-26.3f, GroundFloor, 0.78f), 0),
            ("Living Room",         new Vector3(-41.9f, GroundFloor, -4.6f), 1),
            ("Kitchen",             new Vector3(-34.4f, GroundFloor, -3.9f), 2),
            ("Store Room",          new Vector3(-27.6f, GroundFloor, -4.3f), 3),
            ("Spare Ground North A",new Vector3(-32.5f, GroundFloor,  5.5f), 0),
            ("Spare Ground North B",new Vector3(-27.25f,GroundFloor,  6.6f), 0),
            ("Landing",             new Vector3(-40.0f, UpperFloor, -1.25f), 4),
            ("Bedroom 1",           new Vector3(-45.6f, UpperFloor, -3.9f), 5),
            ("Bathroom 1",          new Vector3(-45.6f, UpperFloor, -7.6f), 6),
            ("Bedroom 2",           new Vector3(-45.6f, UpperFloor,  1.4f), 7),
            ("Hallway",             new Vector3(-27.25f,UpperFloor,  1.75f), 8),
            ("Bathroom 2",          new Vector3(-36.06f,UpperFloor,  5.87f), 9),
            ("Spare Upper North",   new Vector3(-32.5f, UpperFloor,  5.87f), 0),
            ("Spare Upper Middle",  new Vector3(-33.6f, UpperFloor, -2.76f), 0),
        };

        // West porch: the deck in front of the new front door (cut into the north end of the living-room window)
        internal static readonly Vector3 PorchSpawn = new Vector3(-49.4f, 4.73f, -3.33f);

        static void PorchBounds(Transform parent)
        {
            // Invisible walls so the porch is a small pen in front of the door, not the whole deck around the house
            void Wall(string n, Vector3 c, Vector3 size)
            {
                var go = new GameObject(n);
                go.transform.SetParent(parent);
                go.transform.position = c;
                go.AddComponent<BoxCollider>().size = size;
                go.isStatic = true;
            }
            Wall("GB_PorchBounds_West", new Vector3(-50.35f, GroundFloor + 1.5f, -3.5f), new Vector3(0.1f, 3f, 5.6f));
            Wall("GB_PorchBounds_South", new Vector3(-49.1f, GroundFloor + 1.5f, -6.3f), new Vector3(2.6f, 3f, 0.1f));
            Wall("GB_PorchBounds_North", new Vector3(-49.1f, GroundFloor + 1.5f, -0.7f), new Vector3(2.6f, 3f, 0.1f));
        }

        static readonly string[] NotFurniture = { "ver2", "Lights", "Directional Light", "Global Volume", "NestedParent_Unpack", "Area", "GameObject", "Terrain" };

        [MenuItem("FedUp/Rebuild Greybox Scene")]
        public static void BuildFromMenu() => Debug.Log(Build());

        public static string Build()
        {
            var log = new StringBuilder();
            // Create the scene first: NewScene unloads unreferenced assets, which would null out anything loaded before it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tuning = LoadOrCreate<PlayerTuningSO>(Root + "/Data/Player/PlayerTuning.asset");
            var interaction = LoadOrCreate<InteractionSettingsSO>(Root + "/Data/Interaction/InteractionSettings.asset");
            interaction.rayMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            EditorUtility.SetDirty(interaction);
            var focus = LoadOrCreate<InteractionFocusChannelSO>(Root + "/Data/Channels/InteractionFocus.asset");
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Input/FedUpControls.inputactions");

            var wallMat = Mat("M_Greybox_Wall", new Color(0.56f, 0.56f, 0.55f));
            var propMat = Mat("M_Greybox_Prop", new Color(0.36f, 0.33f, 0.30f));
            var interactMat = Mat("M_Greybox_Interactable", new Color(0.85f, 0.72f, 0.30f));
            var blockerMat = Mat("M_Greybox_Blocker", new Color(0.24f, 0.20f, 0.18f));
            SetHumanoidAgent(log);

            // House shell, all grey
            var house = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HouseFbx));
            house.name = "ENV_Apartment_v02";
            foreach (var r in house.GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterials = Enumerable.Repeat(wallMat, r.sharedMaterials.Length).ToArray();
            foreach (var t in house.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            Physics.SyncTransforms();

            // Furniture stand-ins copied from the art reference scene
            var boxes = new GameObject("GB_Furniture");
            int boxCount = CopyFurnitureAsBoxes(boxes.transform, propMat, house, log);
            log.AppendLine($"furniture boxes: {boxCount}");

            // Sealed exits (provisional layout calls, MACRO_PLAN.md section G)
            var blockers = new GameObject("GB_Exits");
            // Front door = the west porch (designer ruling). The east corridor doorway is a locked back door.
            Seal("GB_BackDoor_Locked", new Vector3(-28.5f, GroundFloor + 1.2f, 0.5f), Vector3.right, Vector3.forward, blockers.transform, blockerMat, log);
            Seal("GB_Wall_NorthGap", new Vector3(-37.8f, GroundFloor + 1.2f, 7.5f), Vector3.forward, Vector3.right, blockers.transform, blockerMat, log);
            PorchBounds(blockers.transform);
            Physics.SyncTransforms();

            // Navigation (agent = player-sized) for tests and the autopilot
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string navPath = Root + "/Scenes/Greybox_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);

            // Room markers, snapped onto the navmesh
            var markers = new GameObject("ROOMS");
            foreach (var (name, pos, tour) in Rooms)
            {
                var go = new GameObject("ROOM_" + name.Replace(" ", ""));
                go.transform.SetParent(markers.transform);
                Vector3 p = pos;
                if (NavMesh.SamplePosition(pos, out var hit, 1.5f, NavMesh.AllAreas)) p = hit.position;
                else log.AppendLine($"WARNING room marker off navmesh: {name}");
                go.transform.position = p;
                var m = go.AddComponent<RoomMarker>();
                m.roomName = name; m.tourOrder = tour;
            }

            // The night starts on the west porch, facing the front door (designer ruling 22 Oct)
            var entrance = markers.transform.Find("ROOM_Entrance").position;
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetPositionAndRotation(PorchSpawn, Quaternion.Euler(0, 90, 0));
            spawnGo.AddComponent<SpawnPoint>();
            foreach (Transform m in markers.transform) MoveOntoOpenFloor(m, PorchSpawn, log);

            // Atmosphere kit
            var lightsRoot = new GameObject("Lights");
            foreach (Transform m in markers.transform)
                if (m.name != "ROOM_Entrance") RoomLight(m.name.Replace("ROOM_", "LGT_"), m.position, lightsRoot.transform, 2.8f);
            var entranceLight = RoomLight("LGT_Entrance", entrance, lightsRoot.transform, 3.0f);
            var porchLight = RoomLight("LGT_Porch", PorchSpawn + new Vector3(0.5f, 0f, 0f), lightsRoot.transform, 2.2f);
            porchLight.transform.position += Vector3.up * 0.1f;
            // Slice 0 feedback: "too dark". Brighter ambient and room lights, still dark and moody.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.11f, 0.105f, 0.12f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.06f, 0.12f);
            RenderSettings.fogDensity = 0.035f;
            CreateVolume(log);

            // Light switch on the wall just inside the front door
            var sw = CreateLightSwitch(new Vector3(-46.9f, GroundFloor, -2.45f), new[] { Vector3.left }, entranceLight, interactMat, log);

            // Player. Re-load the data assets: opening/closing the reference scene can unload unreferenced assets.
            tuning = LoadOrCreate<PlayerTuningSO>(Root + "/Data/Player/PlayerTuning.asset");
            interaction = LoadOrCreate<InteractionSettingsSO>(Root + "/Data/Interaction/InteractionSettings.asset");
            focus = LoadOrCreate<InteractionFocusChannelSO>(Root + "/Data/Channels/InteractionFocus.asset");
            controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Input/FedUpControls.inputactions");
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(PorchSpawn, Quaternion.Euler(0, 90, 0));
            player.layer = LayerMask.NameToLayer("Ignore Raycast");
            var cc = player.AddComponent<CharacterController>();
            var motor = player.AddComponent<FirstPersonMotor>();
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0, tuning.eyeHeight, 0);
            var cam = pivot.gameObject.AddComponent<Camera>();
            pivot.gameObject.tag = "MainCamera";
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f; cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            pivot.gameObject.AddComponent<AudioListener>();
            var camData = pivot.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            var interactor = pivot.gameObject.AddComponent<Interactor>();
            Wire(motor, ("tuning", tuning), ("cameraPivot", pivot), ("controls", controls));
            Wire(interactor, ("settings", interaction), ("focusChannel", focus), ("controls", controls));
            var auto = player.AddComponent<Autopilot>();
            Wire(auto, ("motor", motor), ("interactor", interactor), ("firstSwitch", sw));
            cc.height = tuning.bodyHeight; cc.radius = tuning.bodyRadius; cc.center = new Vector3(0, tuning.bodyHeight / 2, 0);

            CreateHud(focus, interaction);
            GreyboxGate1.Build(new GreyboxGate1.Context(motor, interactor, GameObject.Find("HUD").transform, interactMat, propMat, log));

            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(mb);
                for (var it = so.GetIterator(); it.NextVisible(true);)
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.name != "m_Script"
                        && mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("FedUp"))
                        log.AppendLine($"WARNING empty reference {mb.name}.{mb.GetType().Name}.{it.propertyPath}");
            }
            EditorSceneManager.SaveScene(scene, SliceBuilder.GreyboxScene);
            SliceBuilder.ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            log.AppendLine($"saved {SliceBuilder.GreyboxScene}");
            return log.ToString();
        }

        static int CopyFurnitureAsBoxes(Transform parent, Material mat, GameObject house, StringBuilder log)
        {
            var reference = EditorSceneManager.OpenScene(ReferenceScene, OpenSceneMode.Additive);
            var shell = reference.GetRootGameObjects().First(g => g.name == "ver2").transform.Find("Main").GetComponent<Renderer>().bounds;
            shell.Expand(0.3f);
            int count = 0;
            foreach (var root in reference.GetRootGameObjects())
            {
                if (NotFurniture.Contains(root.name) || root.name.StartsWith("edges")) continue;
                var rs = root.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (!shell.Contains(b.center) || b.size.magnitude > 12f) continue;
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "GB_" + root.name.Replace("(Prb)", "").Replace(" ", "");
                box.transform.SetParent(parent);
                box.transform.position = b.center + ReferenceToGreybox;
                box.transform.localScale = Vector3.Max(b.size, Vector3.one * 0.05f);
                box.GetComponent<MeshRenderer>().sharedMaterial = mat;
                box.isStatic = true;
                count++;
            }
            EditorSceneManager.CloseScene(reference, true);
            return count;
        }

        /// <summary>Finds the opening in front of <paramref name="inside"/> along <paramref name="outward"/> and fills it.</summary>
        static void Seal(string name, Vector3 inside, Vector3 outward, Vector3 along, Transform parent, Material mat, StringBuilder log)
        {
            float Probe(float s) => Physics.Raycast(inside + along * s, outward, out var h, 6f) ? h.distance : -1f;
            if (Probe(0) >= 0) { log.AppendLine($"{name}: no opening found ahead (hit at {Probe(0):F2} m), skipped"); return; }
            float lo = 0, hi = 0;
            while (lo > -4f && Probe(lo - 0.05f) < 0) lo -= 0.05f;
            while (hi < 4f && Probe(hi + 0.05f) < 0) hi += 0.05f;
            float wallDist = Mathf.Min(Probe(lo - 0.1f) < 0 ? 6 : Probe(lo - 0.1f), Probe(hi + 0.1f) < 0 ? 6 : Probe(hi + 0.1f));
            float width = hi - lo + 0.3f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            Vector3 c = inside + along * ((lo + hi) * 0.5f) + outward * (wallDist + 0.05f);
            c.y = inside.y - 1.2f + 1.4f;
            go.transform.position = c;
            go.transform.rotation = Quaternion.LookRotation(outward);
            go.transform.localScale = new Vector3(width, 2.8f, 0.25f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            log.AppendLine($"{name}: sealed a {width - 0.3f:F2} m opening at {c:F2}");
        }

        /// <summary>
        /// A marker can land on top of a furniture box (box tops are walkable for the navmesh). Move it to the
        /// nearest spot on the room's actual floor that can be walked to from the entrance.
        /// </summary>
        static void MoveOntoOpenFloor(Transform marker, Vector3 entrance, StringBuilder log)
        {
            var path = new NavMeshPath();
            float floor = marker.position.y > (GroundFloor + UpperFloor) / 2 ? UpperFloor : GroundFloor;
            bool Good(Vector3 p, out Vector3 snapped)
            {
                snapped = p;
                if (!NavMesh.SamplePosition(new Vector3(p.x, floor, p.z), out var h, 0.3f, NavMesh.AllAreas)) return false;
                if (Mathf.Abs(h.position.y - floor) > 0.2f) return false;
                snapped = h.position;
                return NavMesh.CalculatePath(entrance, h.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            }
            var start = marker.position;
            if (Good(start, out var ok)) { marker.position = ok; return; }
            for (float r = 0.25f; r <= 3f; r += 0.25f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    if (Good(start + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r, out var p))
                    {
                        marker.position = p;
                        log.AppendLine($"moved {marker.name} {r:F2} m onto open floor");
                        return;
                    }
                }
            log.AppendLine($"WARNING {marker.name}: no reachable open floor within 3 m");
        }

        internal static Light RoomLight(string name, Vector3 floorPos, Transform parent, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = floorPos + Vector3.up * 2.5f;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.8f, 0.55f);
            l.intensity = intensity;
            l.range = 7.5f;
            l.shadows = LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            return l;
        }

        static LightSwitch CreateLightSwitch(Vector3 origin, Vector3[] directions, Light target, Material mat, StringBuilder log)
        {
            Vector3 eye = origin + Vector3.up * 1.35f;
            RaycastHit best = default; bool found = false;
            foreach (var d in directions)
                if (Physics.Raycast(eye, d, out var h, 3.5f) && (!found || h.distance < best.distance)) { best = h; found = true; }
            if (!found) { log.AppendLine("WARNING: no wall near the entrance for the light switch"); best.point = eye + Vector3.forward; best.normal = Vector3.back; }

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "PREF_Switch_Light (greybox)";
            plate.layer = LayerMask.NameToLayer("Interactable");
            plate.transform.position = best.point + best.normal * 0.02f;
            plate.transform.rotation = Quaternion.LookRotation(best.normal);
            plate.transform.localScale = new Vector3(0.09f, 0.13f, 0.03f);
            plate.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var col = plate.GetComponent<BoxCollider>();
            col.size = new Vector3(3f, 2.5f, 3f); // generous hit area so the small plate is easy to aim at

            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "PRT_Lever";
            Object.DestroyImmediate(lever.GetComponent<BoxCollider>());
            lever.transform.SetParent(plate.transform, false);
            lever.transform.localPosition = new Vector3(0, 0, 0.8f);
            lever.transform.localScale = new Vector3(0.35f, 0.4f, 1f);
            lever.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var sw = plate.AddComponent<LightSwitch>();
            var click = plate.AddComponent<AudioSource>();
            click.playOnAwake = false; click.spatialBlend = 1f;
            Wire(sw, ("targets", new Object[] { target }), ("toggleVisual", lever.transform), ("clickSource", click));
            log.AppendLine($"light switch on '{best.collider?.name}' at {plate.transform.position:F2}");
            return sw;
        }

        static void CreateVolume(StringBuilder log)
        {
            string path = Root + "/Settings/GreyboxVolume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true); ca.saturation.Override(-35f); ca.contrast.Override(12f); ca.postExposure.Override(0.75f);
            var fg = profile.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium3); fg.intensity.Override(0.5f); fg.response.Override(0.8f);
            var vg = profile.Add<Vignette>(true); vg.intensity.Override(0.38f); vg.smoothness.Override(0.45f);
            var bl = profile.Add<Bloom>(true); bl.intensity.Override(0.35f); bl.threshold.Override(1f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            var go = new GameObject("Global Volume (atmosphere kit)");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true; v.sharedProfile = profile;
            log.AppendLine("atmosphere: fog + grain + vignette + desaturation");
        }

        static void CreateHud(InteractionFocusChannelSO focus, InteractionSettingsSO settings)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var dot = new GameObject("Crosshair", typeof(RectTransform)).AddComponent<Image>();
            dot.transform.SetParent(canvasGo.transform, false);
            dot.rectTransform.sizeDelta = new Vector2(6, 6);

            var text = new GameObject("Prompt", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(canvasGo.transform, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(0.95f, 0.92f, 0.85f);
            text.rectTransform.sizeDelta = new Vector2(900, 60);
            text.rectTransform.anchoredPosition = new Vector2(0, -60);
            text.text = "";

            var ui = canvasGo.AddComponent<InteractionPromptUI>();
            Wire(ui, ("focusChannel", focus), ("settings", settings), ("promptText", text), ("crosshair", dot));
        }

        static void SetHumanoidAgent(StringBuilder log)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset").FirstOrDefault();
            if (asset == null) { log.AppendLine("WARNING: NavMeshAreas.asset not found"); return; }
            var so = new SerializedObject(asset);
            var first = so.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            first.FindPropertyRelative("agentRadius").floatValue = 0.3f;
            first.FindPropertyRelative("agentHeight").floatValue = 1.75f;
            first.FindPropertyRelative("agentClimb").floatValue = 0.3f;
            first.FindPropertyRelative("agentSlope").floatValue = 45f;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("navmesh agent = player body (radius 0.3, height 1.75, step 0.3)");
        }

        internal static Material Mat(string name, Color c)
        {
            string path = $"{Root}/Art/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        internal static void Wire(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogError($"GreyboxBuilder: {target.GetType().Name}.{field} not found"); continue; }
                if (value is Object[] arr)
                {
                    p.arraySize = arr.Length;
                    for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                }
                else p.objectReferenceValue = (Object)value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
