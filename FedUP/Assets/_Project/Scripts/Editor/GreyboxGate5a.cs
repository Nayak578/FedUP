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
using FedUp.Tasks.Cat;
using FedUp.Tasks.Night;
using FedUp.Tasks.Phone;
using FedUp.Tasks.Snack;
using FedUp.Tasks.Tea;
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
    /// Gate 5a: the night's framework and two endings. The charger trip upstairs (and the gun), calls #2-4, cat visits
    /// #2-3, the final sleep in his own bed, the ending resolver, the secret ending and the Showdown. Every placement has
    /// its design reason beside it; the full list is DESIGN_LOGIC.md #61-76.
    /// </summary>
    public static class GreyboxGate5a
    {
        const float G = 4.83f, Up = 8.28f;

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");

            // ---------- data (Appendix 1) ----------
            FrustrationEventSO Fe(string name, float amount, string what) =>
                G1.Once<FrustrationEventSO>("Frustration/" + name, e => { e.amount = amount; e.description = what + " Appendix 1."; });
            var feRing = Fe("FE_Phone_RingAfterFirst", 2f, "Phone #2-4: each ring after the first (+2%).");
            var feDistorted = Fe("FE_Phone_Distorted", 5f, "Phone #2-4: distorted voice line (+5%).");
            var feQuick = Fe("FE_Phone_AnsweredQuickly", -5f, "Phone #2-4: found and answered quickly (-5%).");
            var feScream = Fe("FE_FinalSleep_Scream", 15f, "Final Sleep: jumpscare, failed to respond (+15%).");
            var feClean = Fe("FE_FinalSleep_Clean", -20f, "Final Sleep: sleep success (-20%).");
            var feRetry = Fe("FE_FinalSleep_Retry", 10f, "Final Sleep: retry, capped at one +10% (rebalance).");
            var callTuning = G1.Once<CallTuningSO>("Tasks/CallTuning", t => { t.ringAfterFirst = feRing; t.distorted = feDistorted; t.answeredQuickly = feQuick; });
            var sleepTuning = G1.Once<FinalSleepTuningSO>("Tasks/FinalSleepTuning", t => { t.scream = feScream; t.clean = feClean; t.retry = feRetry; });
            var requests = G1.Once<FrustrationRequestChannelSO>("Channels/FrustrationRequests", null);
            var changed = G1.Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var qte = G1.Once<QteDisplayChannelSO>("Channels/QteDisplay", null);
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            var runState = G1.Once<RunStateSO>("Story/RunState", null);
            EnsureLines(lines, log);

            var root = new GameObject("GATE5A").transform;
            var systems = GameObject.Find("SYSTEMS");
            var phase = systems.GetComponent<NightPhaseController>();
            var torch = Object.FindAnyObjectByType<PhoneTorch>();
            var runner = systems.GetComponent<TaskRunner>();
            var ghostMat = G3.Unlit("M_Greybox_FigureUnlit", new Color(0.62f, 0.63f, 0.66f));
            var dark = GreyboxBuilder.Mat("M_Greybox_Gun", new Color(0.08f, 0.08f, 0.09f));
            var wood = GreyboxBuilder.Mat("M_Greybox_DrawerWood", new Color(0.36f, 0.27f, 0.2f));

            // ---------- the Bedroom upstairs (#61, #62, #70) ----------
            // the greybox bed was the model's whole bounding box, frame and all: a 2.25 m block you can't lie on. Make it a
            // mattress you can lie on, with a headboard against the wall (same footprint, so the walking map is unchanged).
            var bedGo = GameObject.Find("GB_SingleBed");
            var full = bedGo.GetComponent<Renderer>().bounds;
            bedGo.transform.position = new Vector3(full.center.x, Up + 0.25f, full.center.z);
            bedGo.transform.localScale = new Vector3(full.size.x, 0.5f, full.size.z);
            var bedMat = bedGo.GetComponent<Renderer>().sharedMaterial;
            G1.Box("GB_SingleBed_Headboard", bedGo.transform.parent, new Vector3(full.min.x + 0.05f, Up + 0.55f, full.center.z), new Vector3(0.1f, 1.1f, full.size.z), bedMat, 0);
            Physics.SyncTransforms();
            var bedBox = bedGo.GetComponent<Renderer>().bounds;
            var cabinet = GameObject.Find("GB_SmallCloset").GetComponent<Renderer>().bounds;
            float mattress = Physics.Raycast(new Vector3(bedBox.center.x + 0.6f, Up + 1.4f, bedBox.center.z), Vector3.down, out var mh, 2f) ? mh.point.y : Up + 0.55f;
            // the mattress you can lie on (the bed model's own collider isn't on the Interactable layer)
            var bedTop = G1.Box("GB_BedTop", root, new Vector3(bedBox.center.x + 0.3f, mattress + 0.03f, bedBox.center.z), new Vector3(2.2f, 0.06f, 1.5f), c.InteractMat, interactable);
            bedTop.GetComponent<Renderer>().enabled = false;
            var bedTI = G1.Interactable(bedTop, "bed", "Go to bed", InteractionMode.Hold, 1.5f);
            bedTI.available = false;
            var lieSpot = G1.Marker("CTRL_BedLie", root, new Vector3(bedBox.center.x + 0.2f, mattress, bedBox.center.z));
            lieSpot.rotation = Quaternion.Euler(0f, 90f, 0f); // head to the wall, looking down the bed to the door
            var ceilingLook = G1.Marker("CTRL_BedCeiling", root, new Vector3(bedBox.max.x + 0.4f, Up + 2.4f, bedBox.center.z));
            var blanket = G1.Box("GB_Blanket", root, new Vector3(bedBox.center.x + 0.55f, mattress + 0.08f, bedBox.center.z), new Vector3(1.6f, 0.06f, 1.35f),
                GreyboxBuilder.Mat("M_Greybox_Blanket", new Color(0.45f, 0.42f, 0.5f)), 0);
            Object.DestroyImmediate(blanket.GetComponent<Collider>());
            // GDD: "ghost arms barely form in the fog around the bed frame"
            var arms = new GameObject("GB_GhostArms").transform;
            arms.SetParent(root, false);
            foreach (var z in new[] { -0.55f, 0.55f })
            {
                var a = G1.Box("PRT_Arm", arms, new Vector3(bedBox.max.x + 0.05f, mattress + 0.15f, bedBox.center.z + z), new Vector3(0.75f, 0.06f, 0.07f), ghostMat, 0);
                Object.DestroyImmediate(a.GetComponent<Collider>());
                a.transform.rotation = Quaternion.Euler(0f, z * 30f, 25f);
            }
            // the charger lives on the bedside cabinet at the head of the bed (#61)
            var chargerGo = G1.Box("PROP_Charger (greybox)", root, new Vector3(cabinet.center.x, cabinet.max.y + 0.03f, cabinet.center.z), new Vector3(0.12f, 0.04f, 0.08f), dark, interactable);
            var chargerCol = chargerGo.GetComponent<BoxCollider>(); chargerCol.size = new Vector3(3f, 6f, 4f);
            var chargerTI = G1.Interactable(chargerGo, "charger", "Plug in your phone", InteractionMode.Hold, 2f);
            chargerTI.available = false;
            // the drawer below it, pulled out a hand's width, facing the room (#62)
            var drawerRoot = new GameObject("CTRL_BedsideDrawer").transform;
            drawerRoot.SetParent(root, false);
            drawerRoot.position = new Vector3(cabinet.max.x, cabinet.max.y - 0.22f, cabinet.center.z);
            var drawerGo = G1.Box("PRT_Drawer", drawerRoot, new Vector3(cabinet.max.x + 0.05f, cabinet.max.y - 0.22f, cabinet.center.z), new Vector3(0.4f, 0.12f, 0.55f), wood, 0);
            var gunGo = G1.Box("PROP_Gun (greybox)", drawerGo.transform, drawerGo.transform.position + new Vector3(0f, 0.07f, 0f), new Vector3(0.18f, 0.04f, 0.12f), dark, 0);
            Object.DestroyImmediate(gunGo.GetComponent<Collider>());
            var cameo = drawerRoot.gameObject.AddComponent<GunCameo>();
            GreyboxBuilder.Wire(cameo, ("drawer", drawerGo.transform), ("gun", gunGo), ("eye", c.Interactor.transform), ("torch", torch));
            var cso = new SerializedObject(cameo);
            cso.FindProperty("shutOffset").vector3Value = new Vector3(-0.25f, 0f, 0f);
            cso.FindProperty("seeAngle").floatValue = 30f;
            cso.ApplyModifiedPropertiesWithoutUndo();
            // the one light upstairs at 3 AM: the landing, at the top of the stairs (wayfinding, see DESIGN_LOGIC)
            var landing = GameObject.Find("ROOM_Landing").transform.position;
            var landingLight = GreyboxBuilder.RoomLight("LGT_LandingPath", landing, root, 1.6f);
            landingLight.color = new Color(0.75f, 0.8f, 1f);

            // ---------- the phone moves (#63-66) ----------
            var call1 = Object.FindObjectsByType<PhoneCallTask>(FindObjectsSortMode.None).First(p => p.CallNumber == 1);
            var phoneTI = call1.Phone;
            var sleep = Object.FindAnyObjectByType<SleepTask>();
            var couchLie = (Transform)new SerializedObject(sleep).FindProperty("lieSpot").objectReferenceValue;
            Vector3 OnSurface(Vector3 p, float above = 1.2f) => Physics.Raycast(p + Vector3.up * above, Vector3.down, out var h, above + 1f) ? h.point : p;
            var spawn2 = G1.Marker("CTRL_Phone2_Couch", root, OnSurface(couchLie.position) + Vector3.up * 0.04f);
            var fuse = GameObject.Find("PREF_FuseBox (greybox)").transform.position;
            var spawn3 = G1.Marker("CTRL_Phone3_BackHallFloor", root, new Vector3(fuse.x, G + 0.04f, fuse.z - 0.3f));
            spawn3.rotation = Quaternion.Euler(0f, 37f, 0f); // dropped, not placed
            var file = GameObject.Find("GB_BossFile").GetComponent<Renderer>().bounds;
            var spawn4 = G1.Marker("CTRL_Phone4_OnTheFile", root, new Vector3(file.center.x, file.max.y + 0.04f, file.center.z));
            PhoneCallTask Call(int n, Transform at, bool distorted, string[] boss, string[] voicemail, string after)
            {
                var t = systems.AddComponent<PhoneCallTask>();
                t.objective = "Answer the phone";
                GreyboxBuilder.Wire(t, ("phone", phoneTI), ("spawnAt", at), ("callTuning", callTuning), ("frustration", requests), ("runState", runState),
                    ("lines", lines), ("subtitles", subtitles));
                var so = new SerializedObject(t);
                so.FindProperty("callNumber").intValue = n;
                so.FindProperty("distorted").boolValue = distorted;
                so.FindProperty("afterAnswerLineId").stringValue = after;
                so.ApplyModifiedPropertiesWithoutUndo();
                G1.SetStrings(t, "bossLineIds", boss);
                G1.SetStrings(t, "voicemailLineIds", voicemail);
                return t;
            }
            var call2 = Call(2, spawn2, false, new[] { "boss_call2_a", "boss_call2_b" }, new string[0], "phone2_after");
            var call3 = Call(3, spawn3, true, new[] { "boss_call3_a", "boss_call3_b" }, new string[0], "phone3_after");
            var call4 = Call(4, spawn4, false, new[] { "boss_call4_a", "boss_call4_b" }, new[] { "boss_call4_machine" }, "phone4_after");

            // ---------- cats #2-3 (#67-69) ----------
            var visit1 = Object.FindAnyObjectByType<CatVisitTask>();
            var v1 = new SerializedObject(visit1);
            var catTuning = (CatTuningSO)v1.FindProperty("tuning").objectReferenceValue;
            var catAgent = (NavMeshAgent)v1.FindProperty("cat").objectReferenceValue;
            var catTI = (TaskInteractable)v1.FindProperty("petTarget").objectReferenceValue;
            var catBody = (Transform)v1.FindProperty("body").objectReferenceValue;
            var glow = v1.FindProperty("glow").objectReferenceValue;
            var counter = GameObject.Find("GB_Kitchen_Set03").GetComponent<Renderer>().bounds;
            var table = GameObject.Find("GB_Dining_Table").GetComponent<Renderer>().bounds;
            var guestBed = GameObject.Find("GB_GuestBed").GetComponent<Renderer>().bounds;
            var phoneShelf = GameObject.Find("CTRL_HideSugar").transform.position;
            Transform Perch(string name, Vector3 at, Vector3 facing)
            {
                var m = G1.Marker(name, root, at);
                Vector3 d = facing - at; d.y = 0f;
                m.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : Vector3.forward);
                return m;
            }
            var kitchen = GameObject.Find("ROOM_Kitchen").transform.position;
            // on the counter where the toast went wrong, facing the stove you're turning from
            var p2a = Perch("CTRL_Cat2_Counter", new Vector3(counter.center.x, counter.max.y, counter.max.z - 1.1f), kitchen);
            var p2b = Perch("CTRL_Cat2_Table", new Vector3(table.center.x, table.max.y, table.center.z), kitchen);
            // on the bed nobody sleeps in, at the end nearest the door
            var p3a = Perch("CTRL_Cat3_GuestBed", new Vector3(guestBed.min.x + 0.45f, guestBed.max.y, guestBed.center.z), GameObject.Find("ROOM_GuestBedroom").transform.position + Vector3.left * 3f);
            var p3b = Perch("CTRL_Cat3_EmptyShelf", phoneShelf + new Vector3(0.15f, 0f, 0f), phoneShelf + Vector3.left * 3f);
            CatPerchTask PerchVisit(int n, Transform a, Transform b)
            {
                var t = systems.AddComponent<CatPerchTask>();
                t.objective = "";
                GreyboxBuilder.Wire(t, ("tuning", catTuning), ("cat", catAgent), ("petTarget", catTI), ("body", catBody), ("perch", a), ("otherPerch", b),
                    ("motor", c.Motor), ("phase", phase), ("frustration", requests), ("frustrationState", changed), ("runState", runState), ("lines", lines), ("glow", glow));
                var so = new SerializedObject(t);
                so.FindProperty("visit").intValue = n;
                so.ApplyModifiedPropertiesWithoutUndo();
                return t;
            }
            var cat2 = PerchVisit(2, p2a, p2b);
            var cat3 = PerchVisit(3, p3a, p3b);

            // ---------- the front yard and the gate (#73) ----------
            var spawnPos = GreyboxBuilder.PorchSpawn;
            float yard = spawnPos.y - 0.55f;
            var grass = GreyboxBuilder.Mat("M_Greybox_Yard", new Color(0.13f, 0.14f, 0.12f));
            var fenceMat = GreyboxBuilder.Mat("M_Greybox_Fence", new Color(0.3f, 0.28f, 0.25f));
            var yardRoot = new GameObject("GB_FrontYard").transform;
            yardRoot.SetParent(root, false);
            G1.Box("GB_YardGround", yardRoot, new Vector3(-56.3f, yard - 0.05f, -3.3f), new Vector3(11.6f, 0.1f, 14f), grass, 0);
            G1.Box("GB_YardPath", yardRoot, new Vector3(-55.25f, yard + 0.005f, -3.3f), new Vector3(9.5f, 0.02f, 1.2f), GreyboxBuilder.Mat("M_Greybox_Path", new Color(0.32f, 0.31f, 0.29f)), 0);
            const float fenceX = -60.5f;
            G1.Box("GB_Fence_North", yardRoot, new Vector3(fenceX, yard + 0.55f, 0.85f), new Vector3(0.08f, 1.1f, 6.3f), fenceMat, 0);
            G1.Box("GB_Fence_South", yardRoot, new Vector3(fenceX, yard + 0.55f, -7.45f), new Vector3(0.08f, 1.1f, 6.3f), fenceMat, 0);
            var gateGo = G1.Box("GB_Gate", yardRoot, new Vector3(fenceX, yard + 0.5f, -3.3f), new Vector3(0.06f, 1.0f, 1.9f), fenceMat, 0);
            var gate = G1.Marker("CTRL_Gate", root, new Vector3(fenceX + 0.7f, yard, -3.3f));
            var lampPost = G1.Box("GB_StreetLamp", yardRoot, new Vector3(fenceX - 1.2f, yard + 2.2f, -1.6f), new Vector3(0.12f, 4.4f, 0.12f), fenceMat, 0);
            var lampGo = new GameObject("LGT_StreetLamp");
            lampGo.transform.SetParent(yardRoot, false);
            lampGo.transform.position = new Vector3(fenceX - 0.6f, yard + 4.2f, -2.4f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.62f, 0.25f); lamp.intensity = 10f; lamp.range = 11f; lamp.shadows = LightShadows.None;
            var porchShoot = G1.Marker("CTRL_PorchShoot", root, spawnPos);

            // ---------- the Showdown (#74) ----------
            var bossMat = GreyboxBuilder.Mat("M_Greybox_BossSuit", new Color(0.12f, 0.13f, 0.17f));
            var boss = new GameObject("GB_BossAtGate");
            boss.transform.SetParent(root, false);
            boss.transform.position = gate.position;
            boss.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // facing the house
            var bossBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bossBody.name = "PRT_BossBody";
            Object.DestroyImmediate(bossBody.GetComponent<Collider>());
            bossBody.transform.SetParent(boss.transform, false);
            bossBody.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            bossBody.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
            bossBody.GetComponent<Renderer>().sharedMaterial = bossMat;
            var bossFile = G1.Box("PROP_FileInHand", bossBody.transform, Vector3.zero, new Vector3(0.24f, 0.31f, 0.03f), GreyboxBuilder.Mat("M_Greybox_File", new Color(0.85f, 0.8f, 0.6f)), 0, true);
            Object.DestroyImmediate(bossFile.GetComponent<Collider>());
            bossFile.transform.localPosition = new Vector3(0.7f, -0.1f, 0.3f);
            GameObject Figure(string name, Vector3 at)
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                f.name = name;
                Object.DestroyImmediate(f.GetComponent<Collider>());
                f.transform.SetParent(root, false);
                f.transform.position = at;
                f.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
                f.GetComponent<Renderer>().sharedMaterial = ghostMat;
                return f;
            }
            var figure = Figure("GB_GhostImage", gate.position + Vector3.up * 0.9f);
            // the ghost is still here: in the burst-open front door behind you, when you turn round from the gate
            var doorPos = Object.FindAnyObjectByType<FrontDoor>().transform.position;
            var doorwayGhost = Figure("GB_GhostInDoorway", new Vector3(doorPos.x + 0.7f, G + 0.9f, doorPos.z));
            var held = G1.Box("PROP_HeldGun (greybox)", c.Interactor.transform, new Vector3(0.22f, -0.2f, 0.45f), new Vector3(0.05f, 0.08f, 0.22f), dark, 0, true);
            Object.DestroyImmediate(held.GetComponent<Collider>());
            var muzzleGo = new GameObject("LGT_Muzzle");
            muzzleGo.transform.SetParent(c.Interactor.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0.22f, -0.15f, 0.8f);
            var muzzle = muzzleGo.AddComponent<Light>();
            muzzle.type = LightType.Point; muzzle.color = new Color(1f, 0.8f, 0.5f); muzzle.intensity = 6f; muzzle.range = 8f;
            var trailGo = new GameObject("FX_BloodTrail");
            trailGo.transform.SetParent(root, false);
            var trail = trailGo.AddComponent<LineRenderer>();
            trail.sharedMaterial = G3.Unlit("M_Greybox_Trail", new Color(0.9f, 0.05f, 0.04f));
            trail.widthMultiplier = 0.16f;
            trail.alignment = LineAlignment.View; // always facing you, so it reads in the dark from any angle
            trail.useWorldSpace = true;
            var showStart = G1.Marker("CTRL_ShowdownStart", root, NavMesh.SamplePosition(new Vector3(bedBox.max.x + 0.7f, Up, bedBox.center.z), out var ss, 1.5f, NavMesh.AllAreas) ? ss.position : new Vector3(bedBox.max.x + 0.7f, Up, bedBox.center.z));
            showStart.rotation = Quaternion.Euler(0f, 90f, 0f);
            // what breaks: the furniture along the trail, in the order you pass it
            var wreck = new List<Transform>();
            var path = new NavMeshPath();
            if (NavMesh.CalculatePath(showStart.position, spawnPos, NavMesh.AllAreas, path))
            {
                var near = new List<(float along, Transform t)>();
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!r.name.StartsWith("GB_") || r.bounds.size.magnitude > 2.6f || r.bounds.size.magnitude < 0.3f || r.transform.IsChildOf(root)) continue;
                    float best = float.MaxValue, along = 0f, walked = 0f;
                    for (int i = 1; i < path.corners.Length; i++)
                    {
                        Vector3 a = path.corners[i - 1], b = path.corners[i];
                        Vector3 dir = (b - a).normalized;
                        Vector3 q = a + dir * Mathf.Clamp(Vector3.Dot(r.bounds.center - a, dir), 0f, Vector3.Distance(a, b));
                        float d = Vector3.Distance(new Vector3(q.x, 0, q.z), new Vector3(r.bounds.center.x, 0, r.bounds.center.z));
                        if (d < best && Mathf.Abs(r.bounds.center.y - q.y) < 2f) { best = d; along = walked + Vector3.Distance(a, q); }
                        walked += Vector3.Distance(a, b);
                    }
                    if (best < 2.5f) near.Add((along, r.transform));
                }
                wreck = near.OrderBy(n => n.along).Select(n => n.t).Take(10).ToList();
            }
            var frontDoor = Object.FindAnyObjectByType<FrontDoor>();
            var showdown = systems.AddComponent<ShowdownSequence>();
            GreyboxBuilder.Wire(showdown, ("drawer", cameo), ("heldGun", held), ("motor", c.Motor), ("interactor", c.Interactor), ("torch", torch), ("phase", phase),
                ("frontDoor", frontDoor), ("start", showStart), ("porch", porchShoot), ("gate", gate), ("figure", figure), ("bossAtGate", boss), ("bossBody", bossBody.transform),
                ("lamp", lamp), ("ghostBehind", doorwayGhost), ("trail", trail), ("muzzle", muzzle), ("wreckables", wreck.Cast<Object>().ToArray()),
                ("lines", lines), ("subtitles", subtitles));

            // ---------- the ending card (#76) ----------
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cardRt = G1.Rect("EndingCard", c.Hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            cardRt.SetAsLastSibling();
            var black = cardRt.gameObject.AddComponent<Image>();
            black.color = new Color(0f, 0f, 0f, 0f);
            var title = G1.TextAt("Title", cardRt, font, 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1600, 110), new Vector2(0.5f, 0.5f));
            var cardLine = G1.TextAt("Line", cardRt, font, 34, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1300, 160), new Vector2(0.5f, 1f));
            var again = G1.TextAt("Again", cardRt, font, 26, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(900, 40), new Vector2(0.5f, 0f));
            var endings = systems.AddComponent<EndingPlayer>();
            GreyboxBuilder.Wire(endings, ("showdown", showdown), ("motor", c.Motor), ("interactor", c.Interactor), ("lines", lines), ("card", cardRt.gameObject),
                ("black", black), ("title", title), ("line", cardLine), ("prompt", again));

            // ---------- the tasks ----------
            var charger = systems.AddComponent<ChargerTask>();
            charger.objective = "Charge your phone. The charger's by the bed, upstairs.";
            GreyboxBuilder.Wire(charger, ("charger", chargerTI), ("torch", torch), ("landingLight", landingLight), ("gun", cameo), ("lines", lines));
            var finalSleep = systems.AddComponent<FinalSleepTask>();
            finalSleep.objective = "Go to bed. Your own, for once.";
            GreyboxBuilder.Wire(finalSleep, ("tuning", sleepTuning), ("bed", bedTI), ("lieSpot", lieSpot), ("lookAt", ceilingLook), ("blanket", blanket.transform),
                ("ghostArms", arms.gameObject), ("motor", c.Motor), ("interactor", c.Interactor), ("frustration", requests), ("frustrationState", changed),
                ("runState", runState), ("qte", qte), ("lines", lines), ("subtitles", subtitles), ("endings", endings), ("waitingCats", new Object[] { cat2, cat3 }),
                ("lateCalls", new Object[] { call3, call4 }));
            var secret = systems.AddComponent<SecretSleep>();
            GreyboxBuilder.Wire(secret, ("bed", bedTI), ("runner", runner), ("lieSpot", lieSpot), ("motor", c.Motor), ("lines", lines), ("endings", endings));
            var chaos = systems.GetComponent<ApplianceChaosTask>();
            var tea = systems.GetComponent<TeaTask>();
            var mid = systems.AddComponent<MidTaskCalls>();
            GreyboxBuilder.Wire(mid, ("chaos", chaos), ("call3", call3), ("call4", call4));
            var afterTea = systems.AddComponent<BeginAfter>();
            GreyboxBuilder.Wire(afterTea, ("after", tea), ("begin", new Object[] { cat2 }));
            var afterChaos = systems.AddComponent<BeginAfter>();
            GreyboxBuilder.Wire(afterChaos, ("after", chaos), ("begin", new Object[] { cat3 }));
            // let his line ("Tea. I made tea." / "Bed. A real one.") land before the bell
            foreach (var ba in new[] { afterTea, afterChaos }) G1.SetFloat(ba, "delaySeconds", 3.2f);

            // the night's order: ... back to the living room, ★ charger, snack, ★ call #2, tea, chaos, ★ final sleep
            var snack = systems.GetComponent<SnackTask>();
            var so2 = new SerializedObject(runner);
            var list = so2.FindProperty("tasks");
            var order = new List<Object>();
            for (int i = 0; i < list.arraySize; i++) order.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            order.Insert(order.IndexOf(snack), charger);
            order.Insert(order.IndexOf(snack) + 1, call2);
            order.Add(finalSleep);
            list.arraySize = order.Count;
            for (int i = 0; i < order.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = order[i];
            so2.FindProperty("allDoneLineId").stringValue = "";
            so2.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"gate 5a: bed top {bedTop.transform.position:F2}, charger {chargerGo.transform.position:F2}, drawer {drawerGo.transform.position:F2}, " +
                           $"phone #2 {spawn2.position:F2} #3 {spawn3.position:F2} #4 {spawn4.position:F2}, cat #2 {p2a.position:F2} #3 {p3a.position:F2}, " +
                           $"gate {gate.position:F2}, showdown stops from {showStart.position:F2}, {wreck.Count} things to wreck; " +
                           "order: " + string.Join(" > ", order.Select(o => o.GetType().Name)));
        }

        /// <summary>
        /// Adds the lines the library doesn't have yet, and tags every line with the writing rule it plays (CLAUDE.md
        /// writing rule 1). Lines that broke the rule are rewritten, but only if they still read as originally written:
        /// a line the designer has edited is never touched.
        /// </summary>
        static void EnsureLines(MonologueLibrarySO lib, StringBuilder log)
        {
            int added = 0, tagged = 0, rewritten = 0;
            void Add(string id, string text, float s, LineRule rule)
            {
                if (lib.TryGet(id, out _)) return;
                lib.lines.Add(new MonologueLibrarySO.Entry { id = id, text = text, seconds = s, rule = rule });
                added++;
            }
            const LineRule F = LineRule.FlatAboutTheUnreal, R = LineRule.RawAboutHisLife, P = LineRule.ToThePlayer, O = LineRule.OtherVoice;
            Add("charger_low", "4%. The charger's upstairs. Of course it is.", 2.8f, R);
            Add("charger_done", "Twelve percent. Living the dream.", 2.4f, R);
            Add("boss_call2_a", "BOSS:  I need that file first thing. [static] Are you even listening?", 3.2f, O);
            Add("boss_call2_b", "BOSS:  …Hello? Are you— [static] …unbelievable.", 2.8f, O);
            Add("phone2_after", "Listening. Always listening. That's the job.", 2.6f, R);
            Add("boss_call3_a", "BOSS:  [ your own voice, slowed down ]  'Home. …Technically.'", 3.2f, O);
            Add("boss_call3_b", "BOSS:  '…yllacinhceT …emoH'  [ click ]", 2.6f, O);
            Add("phone3_after", "…That wasn't him.", 2.0f, F);
            Add("boss_call4_a", "BOSS:  You know what? Fine.", 2.2f, O);
            Add("boss_call4_b", "BOSS:  I'm coming over to get it myself.", 2.6f, O);
            Add("boss_call4_machine", "ANSWERING MACHINE:  'Fine. I'm coming over to get it myself.'  [ beep ]", 3.4f, O);
            Add("phone4_after", "He's coming here. At four in the morning. For a folder.", 3.0f, R);
            Add("cat_back", "…The bell. The cat's back.", 2.2f, F);
            Add("cat_spooked", "Okay. Slowly. Nobody likes being rushed.", 2.4f, R);
            Add("final_go_to_bed", "Bed. A real one, for once.", 2.4f, R);
            Add("final_lie", "…Just sleep. That's all I want. Just sleep.", 3.0f, R);
            Add("final_scream", "…Breathe. Just breathe.", 2.0f, F);
            Add("final_nightmare", "Fine. Have it your way.", 2.0f, F);
            Add("secret_line", "Maybe ignoring it all was the real answer.", 3.2f, R);
            Add("showdown_wake", "Okay. Enough.", 1.8f, R);
            Add("showdown_after", "…Sir?", 1.8f, R);
            Add("to_player_cat", "You saw that too, right? …Right.", 2.8f, P);
            Add("to_player_mirror", "You're still here. Huh. Nobody stays.", 3.0f, P);
            Add("ending_die_title", "YOU DIE IN YOUR SLEEP", 3f, O);
            Add("ending_die_line", "In the morning his phone rings, and rings. Nobody picks up.", 3f, O);
            Add("ending_loop_title", "THE LOOP", 3f, O);
            Add("ending_loop_line", "7:00 AM. The alarm. The bag. The toaster. Again.", 3f, O);
            Add("ending_unemployed_title", "UNEMPLOYED. GHOST ANGRY.", 3f, O);
            Add("ending_unemployed_line", "Voicemail, 7:02 AM: '…don't bother coming in.' Upstairs, something is furious.", 3f, O);
            Add("ending_showdown_title", "HORROR SHOWDOWN", 3f, O);
            Add("ending_showdown_line", "The boss came for the file. The house is wrecked. The ghost is still here.", 3f, O);
            Add("ending_burial_answered_title", "GHOST BURIAL", 3f, O);
            Add("ending_burial_answered_line", "The ghost runs, and he follows it. (The Burial itself is built in art pass A4.) Voicemail: '…take a few days off.'", 3f, O);
            Add("ending_burial_ignored_title", "GHOST BURIAL", 3f, O);
            Add("ending_burial_ignored_line", "The ghost runs, and he follows it. (The Burial itself is built in art pass A4.) Voicemail: '…don't bother coming in.'", 3f, O);
            Add("ending_secret_loop_title", "THE LOOP", 3f, O);
            Add("ending_secret_loop_line", "He sleeps through all of it. 7:00 AM. Again.", 3f, O);
            Add("ending_secret_ghost_title", "THE GHOST WINS", 3f, O);
            Add("ending_secret_ghost_line", "He sleeps through all of it. The house keeps him.", 3f, O);

            // lines that played neither rule (writing rule 1), rewritten so they do; skipped if anyone edited them
            void Rewrite(string id, string was, string now)
            {
                if (lib.TryGet(id, out var e) && e.text == was) { e.text = now; rewritten++; }
            }
            Rewrite("wrong_bin", "Not in there.", "Not in there. I have a system.");
            Rewrite("steps_turn", "…Hello?", "…Hello? …Okay. Footsteps. Sure.");
            Rewrite("mirror_after", "No. No, no, no.", "…That's new.");
            Rewrite("back_living", "Lights. I need lights.", "Lights. Lights would be nice.");
            Rewrite("tea_dark", "Can't see a thing. The fuse box first.", "Can't see a thing. Fuse box first. Then tea. Then a new life.");
            Rewrite("tea_dodge", "What— the cabinets—", "The cabinets. Sure. Why not.");
            Rewrite("tea_hit", "Ow—", "Ow. Rude.");
            Rewrite("appliance_need_light", "I can't see it. Torch.", "Something's on the switch. …Torch.");
            Rewrite("appliance_scared", "Get off. GET OFF.", "Off. Thank you.");

            var rules = new Dictionary<string, LineRule>
            {
                ["prologue_chatbot"] = R, ["prologue_later"] = R, ["door_home"] = R, ["hang_jacket"] = R, ["shoes_off"] = R, ["file_what"] = R,
                ["file_not_mine"] = R, ["toaster_start"] = R, ["toaster_jammed"] = R, ["toaster_unjammed"] = R, ["toast_burnt"] = R, ["toast_launched"] = R,
                ["toast_golden"] = R, ["toast_taken"] = R, ["toast_eaten"] = F, ["alarm"] = R, ["alarm_silenced"] = R, ["gate1_done"] = P,
                ["bath_start"] = R, ["cloth_lost"] = F, ["bath_perfect"] = R, ["bath_lost"] = F, ["pick_shirt"] = R, ["pick_cans"] = R,
                ["pick_takeout"] = R, ["pick_mug"] = R, ["wrong_bin"] = R, ["flyback"] = F, ["clean_bonus"] = R, ["read_prescription"] = R,
                ["read_card"] = R, ["boss_call1_a"] = O, ["boss_call1_b"] = O, ["phone_after"] = R, ["phone_ignored"] = R, ["gate2a_done"] = R,
                ["tv_sit"] = R, ["tv_batteries"] = R, ["tv_found_batteries"] = R, ["tv_tug_won"] = F, ["tv_tug_lost"] = F, ["tv_calm_win"] = R,
                ["cat_arrive"] = F, ["cat_pet"] = R, ["cat_hiss"] = R, ["cat_left"] = R, ["gate2b_done"] = R, ["sleep_lie"] = R,
                ["wake_cold"] = F, ["sink_splash"] = F, ["steps_turn"] = F, ["mirror_after"] = F, ["door_slam"] = F, ["back_living"] = F,
                ["gate3_done"] = F, ["fridge_slam"] = F, ["snack_missed"] = R, ["snack_lost"] = F, ["snack_got"] = R, ["juice_box"] = R,
                ["radio_song"] = R, ["tea_start"] = R, ["tea_vanished"] = F, ["tea_hands_full"] = R, ["tea_dark"] = R, ["tea_power_cut"] = R,
                ["tea_power_back"] = R, ["tea_rescatter"] = F, ["tea_all_here"] = R, ["tea_dodge"] = F, ["tea_hit"] = F, ["tea_survived"] = R,
                ["tea_fire"] = F, ["tea_no_tea"] = R, ["tea_made"] = R, ["appliance_on_0"] = F, ["appliance_on_1"] = F, ["appliance_on_2"] = F,
                ["appliance_need_light"] = F, ["appliance_scared"] = F, ["appliance_overload"] = R, ["gate4_done"] = F,
            };
            foreach (var l in lib.lines)
                if (l.rule == LineRule.Unset && rules.TryGetValue(l.id, out var r)) { l.rule = r; tagged++; }
            if (added + tagged + rewritten > 0)
            {
                EditorUtility.SetDirty(lib);
                log.AppendLine($"monologue: added {added} lines, tagged {tagged} with their writing rule, rewrote {rewritten} that broke it");
            }
        }
    }
}
