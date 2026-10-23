using System.Collections.Generic;
using System.Linq;
using System.Text;
using FedUp.Frustration;
using FedUp.Qte;
using FedUp.Story;
using FedUp.Tasks;
using FedUp.Tasks.Bath;
using FedUp.Tasks.Cleanup;
using FedUp.Tasks.Phone;
using FedUp.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using InteractionMode = FedUp.Interaction.InteractionMode;
using G1 = FedUp.EditorTools.GreyboxGate1;

namespace FedUp.EditorTools
{
    /// <summary>
    /// Gate 2a content: Take a Bath (downstairs bathroom), Clean Up Mess (living room), boss call #1, plus two
    /// story props. Appends the three tasks to the TaskRunner after Make Toast (GDD pacing: bath before clean-up).
    /// Data assets are created once and never overwritten; missing monologue lines are added, existing ones kept.
    /// </summary>
    public static class GreyboxGate2a
    {
        const float G = 4.83f;

        public static void Build(G1.Context c)
        {
            var log = c.Log;
            int interactable = LayerMask.NameToLayer("Interactable");

            // ---------- data ----------
            var feLost = G1.Once<FrustrationEventSO>("Frustration/FE_Bath_ItemLost", e => { e.amount = 6f; e.description = "Take a Bath: each lost clothing item (+6%). Appendix 1."; });
            var feBathOk = G1.Once<FrustrationEventSO>("Frustration/FE_Bath_Success", e => { e.amount = -10f; e.description = "Take a Bath: nothing lost (-10%). Appendix 1."; });
            var feFly = G1.Once<FrustrationEventSO>("Frustration/FE_Cleanup_Flyback", e => { e.amount = 6f; e.description = "Clean Up: failed placement / flyback (+6%). Appendix 1."; });
            var feGiggle = G1.Once<FrustrationEventSO>("Frustration/FE_Cleanup_Giggle", e => { e.amount = 2f; e.description = "Clean Up: ghost giggle (+2%). Appendix 1."; });
            var feClean = G1.Once<FrustrationEventSO>("Frustration/FE_Cleanup_RoomClean", e => { e.amount = -10f; e.description = "Clean Up: room stays clean 30 s after a 3-item streak (-10%). Appendix 1."; });
            var feAns = G1.Once<FrustrationEventSO>("Frustration/FE_Phone1_Answered", e => { e.amount = -5f; e.description = "Phone #1 answered (-5%). Appendix 1."; });
            var feIgn = G1.Once<FrustrationEventSO>("Frustration/FE_Phone1_Ignored", e => { e.amount = 2f; e.description = "Phone #1 ignored (+2%). Appendix 1."; });
            var bathTuning = G1.Once<BathTuningSO>("Tasks/BathTuning", t => { t.itemLost = feLost; t.success = feBathOk; });
            var cleanTuning = G1.Once<CleanupTuningSO>("Tasks/CleanupTuning", t => { t.flyback = feFly; t.giggle = feGiggle; t.roomClean = feClean; });
            var runState = G1.Once<RunStateSO>("Story/RunState", null);
            var requests = G1.Once<FrustrationRequestChannelSO>("Channels/FrustrationRequests", null);
            var changed = G1.Once<FrustrationChannelSO>("Channels/FrustrationChanged", null);
            var subtitles = G1.Once<SubtitleChannelSO>("Channels/Subtitles", null);
            var qte = G1.Once<QteDisplayChannelSO>("Channels/QteDisplay", null);
            var lines = G1.Once<MonologueLibrarySO>("Story/Monologue", null);
            EnsureLines(lines, log);
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/FedUpControls.inputactions");

            var cloth = GreyboxBuilder.Mat("M_Greybox_Cloth", new Color(0.35f, 0.42f, 0.55f));
            var trashMat = GreyboxBuilder.Mat("M_Greybox_Trash", new Color(0.55f, 0.5f, 0.38f));
            var root = new GameObject("GATE2A").transform;

            // ---------- Take a Bath: downstairs bathroom ----------
            var room = GameObject.Find("ROOM_BathroomDownstairs").transform.position;
            Vector3 eye = room + Vector3.up * 1.0f;
            float xe = Hit(eye, Vector3.right, 6f).x, xw = Hit(eye, Vector3.left, 6f).x, zs = Hit(eye, Vector3.back, 8f).z, zn = Hit(eye, Vector3.forward, 8f).z;
            float tubZ = Mathf.Clamp(zs + 1.2f, zs + 1.0f, zn - 1.0f);
            var tub = G1.Box("PREF_Bathtub (greybox)", root, new Vector3(xe - 0.42f, G + 0.3f, tubZ), new Vector3(0.75f, 0.6f, 1.7f), c.InteractMat, interactable);
            var tubTI = G1.Interactable(tub, "bathtub", "Run a bath", InteractionMode.Hold, 1.2f);
            var spot = G1.Marker("CTRL_BathSpot", root, new Vector3(xe - 1.1f, G, tubZ));
            spot.rotation = Quaternion.Euler(0f, 270f, 0f);
            var lookAt = G1.Marker("CTRL_BathHooks", root, new Vector3(xw + 0.05f, G + 1.35f, tubZ));
            var clothes = new List<Transform>();
            string[] names = { "GB_Clothes_Shirt", "GB_Clothes_Trousers", "GB_Clothes_Towel" };
            for (int i = 0; i < 3; i++)
            {
                float z = tubZ - 0.6f + i * 0.6f;
                var hook = G1.Box("GB_BathHook_" + i, root, new Vector3(xw + 0.04f, G + 1.75f, z), new Vector3(0.06f, 0.05f, 0.12f), c.PropMat, 0);
                Object.DestroyImmediate(hook.GetComponent<Collider>());
                var item = G1.Box(names[i], root, new Vector3(xw + 0.09f, G + 1.45f, z), new Vector3(0.08f, 0.55f, 0.36f), cloth, 0);
                Object.DestroyImmediate(item.GetComponent<Collider>());
                item.transform.rotation = Quaternion.LookRotation(Vector3.right);
                clothes.Add(item.transform);
            }

            // ---------- Clean Up: living room ----------
            var used = new List<Vector3>();
            // clearance: open floor needed around the spot, so floor clutter never hides in a gap behind furniture
            Vector3 PickFloor(Vector3 near, float minGap, float clearance = 0.3f)
            {
                Vector3 best = near; float bestD = float.MaxValue;
                for (float x = -46.8f; x <= -37.4f; x += 0.4f)
                    for (float z = -7.5f; z <= -1.0f; z += 0.4f)
                    {
                        var p = new Vector3(x, G, z);
                        if (!NavMesh.SamplePosition(p, out var h, 0.15f, NavMesh.AllAreas) || Mathf.Abs(h.position.y - G) > 0.15f) continue;
                        if (Physics.CheckBox(p + Vector3.up * 0.35f, new Vector3(clearance, 0.3f, clearance))) continue;
                        if (used.Any(u => (u - p).magnitude < minGap)) continue;
                        if ((p - new Vector3(-46.6f, G, -3.3f)).magnitude < 1.6f) continue; // keep the doorway clear
                        float d = (p - near).sqrMagnitude;
                        if (d < bestD) { bestD = d; best = p; }
                    }
                used.Add(best);
                return best;
            }

            var bins = new List<ClutterBin>();
            var trash = G1.Box("PREF_TrashBin (greybox)", root, PickFloor(new Vector3(-38.0f, G, -7.4f), 1.2f) + Vector3.up * 0.3f, new Vector3(0.4f, 0.6f, 0.4f), c.InteractMat, interactable);
            G1.Interactable(trash, "bin_trash", "Throw it in the bin", InteractionMode.Press, 0f);
            var trashBin = trash.AddComponent<ClutterBin>(); trashBin.accepts = ClutterKind.Trash; bins.Add(trashBin);
            var basket = G1.Box("PREF_LaundryBasket (greybox)", root, PickFloor(new Vector3(-46.5f, G, -7.2f), 1.2f) + Vector3.up * 0.22f, new Vector3(0.55f, 0.45f, 0.4f), c.InteractMat, interactable);
            G1.Interactable(basket, "bin_laundry", "Put it in the laundry basket", InteractionMode.Press, 0f);
            var basketBin = basket.AddComponent<ClutterBin>(); basketBin.accepts = ClutterKind.Laundry; bins.Add(basketBin);

            var clutter = new List<ClutterItem>();
            void Clutter(string id, string prompt, ClutterKind kind, string lineId, Vector3 near, Vector3 size, Material m)
            {
                var p = PickFloor(near, 1.3f, clearance: 0.55f);
                var go = G1.Box("GB_Clutter_" + id, root, p + Vector3.up * (size.y / 2f + 0.01f), size, m, interactable);
                var col = go.GetComponent<BoxCollider>();
                col.size = new Vector3(Mathf.Max(1f, 0.5f / size.x), Mathf.Max(1f, 0.5f / size.y), Mathf.Max(1f, 0.5f / size.z));
                G1.Interactable(go, "clutter_" + id, prompt, InteractionMode.Press, 0f);
                var ci = go.AddComponent<ClutterItem>(); ci.kind = kind; ci.pickupLineId = lineId;
                clutter.Add(ci);
            }
            Clutter("shirt", "Pick up the shirt", ClutterKind.Laundry, "pick_shirt", new Vector3(-44.5f, G, -2.0f), new Vector3(0.45f, 0.06f, 0.35f), cloth);
            Clutter("cans", "Pick up the cans", ClutterKind.Trash, "pick_cans", new Vector3(-40.0f, G, -2.2f), new Vector3(0.2f, 0.12f, 0.12f), trashMat);
            Clutter("takeout", "Pick up the takeout box", ClutterKind.Trash, "pick_takeout", new Vector3(-43.0f, G, -6.6f), new Vector3(0.22f, 0.1f, 0.18f), trashMat);
            Clutter("mug", "Pick up the broken mug", ClutterKind.Trash, "pick_mug", new Vector3(-39.0f, G, -5.0f), new Vector3(0.1f, 0.1f, 0.1f), trashMat);

            // story props: the unopened prescription on the coffee table, Mom's card on the side table
            var coffee = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name.Contains("CoffeTable"));
            Vector3 coffeeTop = coffee != null ? new Vector3(coffee.position.x, coffee.GetComponent<Renderer>().bounds.max.y, coffee.position.z) : new Vector3(-41.6f, G + 0.45f, -4.6f);
            var pills = G1.Box("PROP_Prescription (greybox)", root, coffeeTop + new Vector3(0.15f, 0.06f, 0f), new Vector3(0.06f, 0.11f, 0.06f), GreyboxBuilder.Mat("M_Greybox_Pills", new Color(0.85f, 0.5f, 0.15f)), interactable);
            pills.GetComponent<BoxCollider>().size = new Vector3(5f, 3f, 5f);
            G1.Interactable(pills, "story_prescription", "Look at the prescription", InteractionMode.Press, 0f);
            var pillsProp = pills.AddComponent<StoryProp>();
            GreyboxBuilder.Wire(pillsProp, ("lines", lines));
            G1.SetString(pillsProp, "lineId", "read_prescription");
            var card = G1.Box("PROP_MomsCard (greybox)", root, new Vector3(-47.45f, G + 0.76f, -5.82f), new Vector3(0.15f, 0.01f, 0.11f), GreyboxBuilder.Mat("M_Greybox_Card", new Color(0.8f, 0.7f, 0.75f)), interactable);
            card.GetComponent<BoxCollider>().size = new Vector3(2.5f, 15f, 3f);
            G1.Interactable(card, "story_card", "Look at the card", InteractionMode.Press, 0f);
            var cardProp = card.AddComponent<StoryProp>();
            GreyboxBuilder.Wire(cardProp, ("lines", lines));
            G1.SetString(cardProp, "lineId", "read_card");

            // ---------- Phone call #1 ----------
            var phoneTable = G1.Box("GB_PhoneTable", root, PickFloor(new Vector3(-37.8f, G, -1.2f), 1.2f) + Vector3.up * 0.4f, new Vector3(0.45f, 0.8f, 0.45f), c.PropMat, 0);
            var phone = G1.Box("PREF_Phone (greybox)", root, phoneTable.transform.position + new Vector3(0f, 0.44f, 0f), new Vector3(0.22f, 0.08f, 0.18f), c.InteractMat, interactable);
            phone.GetComponent<BoxCollider>().size = new Vector3(2.5f, 5f, 2.5f);
            var phoneTI = G1.Interactable(phone, "phone", "Answer the phone", InteractionMode.Press, 0f);

            // ---------- systems ----------
            var systems = GameObject.Find("SYSTEMS");
            var hold = new GameObject("CTRL_HoldPoint").transform;
            hold.SetParent(c.Interactor.transform, false);
            hold.localPosition = new Vector3(0.28f, -0.28f, 0.65f);

            var bath = systems.AddComponent<BathTask>();
            bath.objective = "Take a bath";
            GreyboxBuilder.Wire(bath, ("tuning", bathTuning), ("tub", tubTI), ("bathSpot", spot), ("lookAt", lookAt),
                ("clothes", clothes.Cast<Object>().ToArray()), ("motor", c.Motor), ("interactor", c.Interactor), ("controls", controls),
                ("frustration", requests), ("qte", qte), ("lines", lines));
            var clean = systems.AddComponent<CleanupTask>();
            clean.objective = "Clean up the mess";
            var livingLight = GameObject.Find("LGT_LivingRoom");
            GreyboxBuilder.Wire(clean, ("tuning", cleanTuning), ("items", clutter.Cast<Object>().ToArray()), ("bins", bins.Cast<Object>().ToArray()),
                ("holdPoint", hold), ("viewer", c.Interactor.transform), ("flickerLight", livingLight != null ? livingLight.GetComponent<Light>() : null),
                ("frustration", requests), ("frustrationState", changed), ("lines", lines));
            var call = systems.AddComponent<PhoneCallTask>();
            call.objective = "";
            GreyboxBuilder.Wire(call, ("phone", phoneTI), ("answered", feAns), ("ignored", feIgn), ("frustration", requests),
                ("runState", runState), ("lines", lines), ("subtitles", subtitles));
            G1.SetStrings(call, "bossLineIds", new[] { "boss_call1_a", "boss_call1_b" });

            var runner = systems.GetComponent<TaskRunner>();
            var so = new SerializedObject(runner);
            var list = so.FindProperty("tasks");
            foreach (var t in new TaskBehaviour[] { bath, clean, call })
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = t;
            }
            so.FindProperty("runState").objectReferenceValue = runState;
            so.FindProperty("allDoneLineId").stringValue = "gate2a_done";
            so.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"gate 2a: bathtub at x {xe - 0.42f:F2} z {tubZ:F2}, {clutter.Count} clutter items, 2 bins, 2 story props, phone at {phone.transform.position:F2}");
        }

        static Vector3 Hit(Vector3 from, Vector3 dir, float max)
            => Physics.Raycast(from, dir, out var h, max) ? h.point : from + dir * max;

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
            Add("bath_start", "Hot water. The one thing in this house that works.", 2.8f);
            Add("cloth_lost", "…Not again.", 1.8f);
            Add("bath_perfect", "Clean. Dressed. Almost a person.", 2.6f);
            Add("bath_lost", "Fine. I didn't need it anyway.", 2.4f);
            Add("pick_shirt", "Smells like a quarterly review.", 2.4f);
            Add("pick_cans", "Breakfast of champions. And lunch. And dinner.", 2.6f);
            Add("pick_takeout", "Same order, same guy. He knows my name. Nobody else does.", 3.2f);
            Add("pick_mug", "'World's Okayest Employee.' Even the mug gave up.", 2.8f);
            Add("wrong_bin", "Not in there.", 1.6f);
            Add("flyback", "…Was that you?", 2.0f);
            Add("clean_bonus", "Look at that. A clean room. Who even lives here.", 3.0f);
            Add("read_prescription", "Doctor said it'd help me focus. Haven't opened it. Haven't opened anything.", 3.6f);
            Add("read_card", "Happy birthday, from Mom. …That was March.", 3.0f);
            Add("boss_call1_a", "BOSS:  Hey— it's me. [static] …you l—ft something on…", 3.2f);
            Add("boss_call1_b", "BOSS:  …the f—le. The one on y—r… [static] …call me b—ck.", 3.2f);
            Add("phone_after", "Left something on. The lights? The stove? My will to live?", 3.2f);
            Add("phone_ignored", "Later. Everything's later.", 2.4f);
            Add("gate2a_done", "Chores done. Reward: existing.", 2.8f);
            if (added > 0) { EditorUtility.SetDirty(lib); log.AppendLine($"monologue: added {added} new lines"); }
        }
    }
}
