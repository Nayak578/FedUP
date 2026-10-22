# Fed Up — Project Audit (Phase 0)

*Audited 22 Oct 2026 from the running Unity Editor through Unity MCP, plus a read of the files on disk. Nothing in the project was changed.*

> **⚠️ Correction (22 Oct, after the deeper house check):** two findings in section 3 were wrong. The upper floor **does** have a ceiling; the player spawns 1.7 m in the air, which puts the camera above it. And the house is **not** open plan; it has many rooms, which my top-down screenshots couldn't show. See [`HOUSE_CHECK.md`](HOUSE_CHECK.md) for the corrected room map.

> **How to read this:** Each section says what exists, what state it's in, and what that means for the game. Section 9 is a one-page verdict. Section 10 lists the things I may have misread. **Please correct those before I plan anything.**

---

## 1. Is Unity MCP connected? Yes.

I can query the running Editor directly. Here's a sample query, run during this audit:

```
manage_scene → get_active
{"name":"firstdraft","path":"Assets/firstdraft.unity","isDirty":false,"isLoaded":true,"rootCount":62}
```

I also read the scene's object list, every component's settings, and console messages, entered and left Play Mode, and took screenshots. The connector is **"MCP for Unity" by CoplayDev** (v10.3.0), not Singtaa or IvanMurzak. It's already installed in the project, works, and does everything the brief needs, so I'm keeping it.

---

## 2. Scenes

| Scene | What's in it | Verdict |
|---|---|---|
| **`Assets/firstdraft.unity`** (open now) | The apartment: building shell, furniture, 69 lights, a city backdrop, and a first-person player. No gameplay. | **This is the real game scene.** |
| `Assets/Scenes/Main.unity` | A grey-box test room from the team's earlier work: toaster, bread, QTE bar, dustbin, waste items, a coat hanger, walls. | Prototype sandbox. Keep it for reference only. |
| `StarterAssets/.../Playground.unity` | Unity's sample first-person test level. | Not needed. |
| 44 other scenes | Demo scenes that came with asset packs (TextMesh Pro examples, furniture packs, the city pack, and so on). | Clutter. They never ship, but they slow down searching. |

**Broken:** Build Settings (the list of scenes that go into the game) points to `SampleScene.unity`, which **doesn't exist**. A Windows build made today would fail or open on nothing. The apartment scene isn't in the build list at all.

---

## 3. The apartment model

![Exterior](evidence/audit/audit_exterior_overview.png)

### Scale, orientation, location
- **Source file:** `Assets/MOdel/ver2/ver2.blend`, a Blender file imported directly. The main shell is one 664-triangle mesh called `Main`, plus about 45 small pieces (pipes, stairs, wall panels).
- **Footprint:** **24 m × 20 m, two storeys**, each roughly 3.4 m high. The ground floor is at height 4.8, the upper floor at 8.3.
- **Real-world scale is correct.** One Unity unit is one metre, and doors and furniture read at believable sizes.
- **It sits about 100 m from the world centre**, at (-97, 9, 31). That's harmless, but every coordinate in it is a large negative number.
- **This is a two-storey house, not an apartment.** It's about 480 m² per floor. The GDD calls it "compact" in one place and "surprisingly large" in another, so I need your call (see section 10).

### Room layout: what exists vs. the GDD

**Ground floor** (seen from directly above, with the ceiling cut away):

![Ground floor](evidence/audit/audit_topdown_ground.png)

**Upper floor:**

![Upper floor](evidence/audit/audit_topdown_upper.png)

| GDD room | In the model? | Notes |
|---|---|---|
| Entrance | ❌ **Missing** | No defined entry area, no coat hooks, no shoe rack, no key table. "Drop Your Things" has nowhere to happen. |
| Living Room | ✅ Ground floor | Sofa, two armchairs, coffee table, media console (TV unit), ceiling fan, window. Open plan with the kitchen. |
| Kitchen | ✅ Ground floor | Island with sink, cabinets, gas stove, hood, wall oven and microwave, fridge, dining table with 4 chairs, kitchen props. **No toaster in this scene.** The toaster only exists in the grey-box test scene. |
| Hallway | ❌ **Missing** | Both floors are open plan. There are almost no interior walls or doors, so there's nowhere for "door slams behind you", "footsteps in the hallway" or "eyes in the dark". |
| Bedroom ×2 | ⚠️ **1 of 2** | Upstairs: one single bed sharing an open space with an office desk, monitor, chair, floor lamp and two closets. No walls separate it. |
| Bathroom ×2 | ⚠️ **½ of 2** | Upstairs: a tiled corner with a toilet. **No bath or shower** ("Take a Bath" needs one), **no mirror** (needed for the mirror jumpscare), no sink. |
| Stairs | ✅ | A central staircase connects the floors. |
| *(not in GDD)* | ❓ | **Ground floor, top-left of the map:** an area about 8 × 6 m inside the outer walls has **no floor**. You see grass from the terrain underneath. Is it a courtyard, or a missing floor? |
| *(not in GDD)* | ❓ | **Upper floor, right half:** a huge empty space with patterned floor and nothing in it. |

### Visible defects
1. **The upper floor has no visible ceiling from inside.** The roof only shows from outside, a common Blender-export issue where surfaces are one-sided. Standing upstairs, you look straight up into the night sky:
   ![Sky through roof](evidence/audit/audit_upperfloor_lookup.png)
2. **The player spawns upstairs, facing a wall.** This is the view on pressing Play:
   ![Play mode spawn](evidence/audit/audit_playmode_spawn.png)
3. **The kitchen counters are blown out** to near-white by a very strong spotlight (intensity 424). Detail is lost:
   ![Kitchen](evidence/audit/audit_kitchen.png)
4. **The living room is lit teal/cyan.** The GDD asks for "single overhead source, weak and yellowed":
   ![Living room](evidence/audit/audit_livingroom.png)

### Polycount and materials (triangles = the building blocks of 3D models; more means slower)
- **The whole scene has ~720,000 triangles**, about 600,000 of them inside or around the apartment.
- **The apartment shell itself is cheap** (~41,000 triangles across 16 materials). That's fine.
- **A few furniture pieces blow far past the GDD budget** (furniture: 1,000–5,000; props: 300–2,000):

| Object | Triangles | GDD budget | Over by |
|---|---|---|---|
| Single bed (blankets + pillows) | **363,000** | 5,000 | **×72** |
| Dining table + 4 chairs (12,000 per chair) | 51,600 | 5,000 | ×10 |
| Kitchen props (one spoon stand alone is 9,900) | 31,000 | 2,000 per prop | ×5 |
| Gas stove | 26,200 | 5,000 | ×5 |
| City backdrop (spans **3.2 km**) | 117,000 | — | Mostly never seen through the windows |

**What this means for you:** The bed alone costs more than everything else in the apartment combined. It must be replaced or simplified, or the bedroom will stutter on mid-range PCs.

- **71 unique materials** in the scene. 64 use Unity's standard URP Lit shader, 6 use an Autodesk shader that came with an asset pack, and 1 uses an unfinished custom Shader Graph.

---

## 4. Every asset (by folder)

The `Assets` folder is **2.9 GB**, and the git history is another 1.3 GB.

| Folder | Size | What it is | Used in the apartment? |
|---|---|---|---|
| `Furniture/` | 868 MB | Furniture pack. Its `Source/Textures` holds **64 MB single TGA files**. | Yes (sofa, chairs) |
| `ModularHousePack1/` | 464 MB | Modular house pieces, 2048px textures | Yes (fence, a few textures) |
| `Versatile Studio Assets/` | 411 MB | Night city pack (one 292 MB file) | Yes (backdrop) |
| `firstdraft/` | 192 MB | **Baked lighting** for the apartment scene: 13 lightmaps of ~18 MB each | Yes |
| `MOdel/` | 155 MB | **The apartment** (`ver2.blend`, `Environment1.blend`) plus wall, floor and carpet textures | Yes, the core |
| `Gogo Casual Pack/` | 120 MB | Casual modular and light pack | Barely (4 files) |
| `Simple Garage/` | 119 MB | Garage pack | No |
| `GeniusCrate_Games/` | 91 MB | Kitchen and dining set | Yes |
| `ShelfObjects/` | 90 MB | Shelf props | No |
| `nappin/` | 83 MB | House props (bed, toilet, media console, lamps) | Yes |
| `Office Room Furniture/` | 60 MB | Desk, chair, monitor | Yes |
| `house.fbm/`, `Free/`, `New Solution Studio/` | ~47 MB | Misc. textures, PBR lamps | Partly |
| `StarterAssets/` | 3 MB | Unity's first-person controller | Yes (player) |
| `TextMesh Pro/` | 11 MB | Text rendering | Yes (UI) |
| `Scripts/`, `Props/`, `Material/`, `InputAction/` | <1 MB | The team's gameplay code and toaster materials | Only `Interacting` |
| **Loose: `MergedBake_Bake1_PBR_PackedTex.png`** | **200 MB** | A single texture. **Not used anywhere.** | No |

**Totals across the project:** 416 models, 644 prefabs (reusable pre-built objects), 389 PNG + 55 TGA textures, 294 materials.

**What's completely absent:**
- **Audio: 0 sound files.**
- **Animation: 0 animation clips.** One empty Animator Controller exists.
- **UI art: none** besides the mobile joystick graphics.
- **Characters: no cat, no ghost model, no hands.**

### Textures
- The apartment uses **163 textures, ~970 MB of memory in the Editor.** Many props use 2048×2048 textures where the GDD says 512.
- The GDD's texture sizes (props 512, furniture 1024–2048) are achievable just by changing import settings. No re-painting needed.

### Performance risks, ranked
1. **The 363k-triangle bed.**
2. **69 lights, all casting shadows**, 33 of them "mixed" (part baked, part live). Shadows from many overlapping lights are the single most expensive thing in URP.
3. **A 3.2 km city backdrop** behind a house you can never leave.
4. **~1 GB of texture memory.**
5. **The apartment is a `.blend` file.** Unity can only open it if Blender is installed on that computer. On any other machine the house silently disappears. The GDD's own SOP says to export FBX.

---

## 5. Code: what the team already wrote

Git history shows **two other contributors**, `Nayak578` (repo owner) and `DIVI-Hectic4U`, who wrote 14 scripts between July and August 2025:

| System | Scripts | What it does | Matches GDD task |
|---|---|---|---|
| Interaction | `Interacting`, `Interactable`, `Destructable` | Looks where the camera points (2.5 m reach) and shows an "E" prompt; pressing E interacts. | Core interaction |
| Toaster | `Toaster`, `Bread`, `Toasting`, `Eaten`, `QTE1` | Drag the mouse down to press the lever, then a sliding-bar timing check (press **R**). Red, green or yellow sets toast colour; burnt toast gets launched with physics. | **Make Toast**, partly |
| Clutter | `wastes`, `DustBin`, `CanInteractWithBin` | Pick up a waste item, carry it, drop it in a bin. | **Clean Up Mess**, basic |
| Hanging | `ItemHang`, `DoorHanger`, `CanInteractWithHanger` | Carry an item to a hook and it snaps on. | **Drop Your Things**, basic |

**The design ideas are good and match the GDD. The code isn't a safe foundation to build on:**
- **It mixes the old and new keyboard systems.** E, T, R and K are read the old way, the mouse the new way, so the project is set to run both.
- **Shared "global switches"** (`isInteracting`, `isHoldingTrash`, `itemsToBehanged = 2`) never reset. After one use, hooks and bins can lock up for the rest of the session.
- **Nothing talks to a frustration system.** No frustration system exists yet.
- **No automated tests.**
- **Wired up only in the grey-box scene.** In the apartment scene the interaction script has no prompt assigned and its "what can I touch" setting is set to *nothing*. Pressing Play in the apartment produces this error on the first frame, and **nothing in the apartment can be interacted with**:

```
read_console (Play Mode, firstdraft):
UnassignedReferenceException: The variable E of Interacting has not been assigned.
```

### Player controller
- Unity's **Starter Assets first-person controller**: walk 4 m/s, sprint 6 m/s, jump 1.2 m.
- The camera runs through **Cinemachine** (Unity's camera system).
- **The player's collision body is shorter than its eye height** (1.27 m body vs 1.77 m eyes). The camera can poke through low shelves and slanted ceilings.
- **The scene still carries the mobile on-screen joysticks and buttons.**

---

## 6. Packages, settings, pipeline

| Item | State | Note |
|---|---|---|
| Unity version | **6000.0.46f1** (Unity 6 LTS) | Good long-term-support version. **It predates Unity's October 2025 security fix (needs 6000.0.58f2 or newer)**, so I'll update the patch version before the first packaged build. Same major version, no project changes expected. |
| Render pipeline | **URP 17** (as the GDD specifies) | Correct. |
| Old "Post Processing v2" package | Installed | Designed for Unity's old renderer and **does nothing in URP**. Dead weight that causes confusion. |
| Post-processing volume | Tonemapping, Bloom, Vignette, Chromatic Aberration, Colour Adjustments, Lift/Gamma/Gain, Lens Flare | **No Film Grain** (the GDD's VHS grain). |
| Fog | **Off** | The GDD wants purple fog. |
| Ambient light | Taken from a procedural sky | The GDD says near-black. |
| Input | **"Both" old and new** | Should be new-only. |
| Cinemachine 2.10, Timeline, Shader Graph, Test Framework, AI Navigation (pathfinding), TextMesh Pro | Installed | All useful later: Timeline for scripted scares, AI Navigation for the cat. |
| Tags and layers | One custom layer, `Interactable`. No `Interactable` or `Pickup` tags. | The GDD SOP lists both tags. |
| Quality preset | "PC". HDR on, no anti-aliasing, render scale 1.04, shadows to 50 m. | Fine starting point. |

---

## 7. Version control: urgent

- **The apartment is not saved in git.** `firstdraft.unity`, the `MOdel/` folder (the house itself), the baked lighting, and every asset pack are **untracked**. If this drive fails, the apartment is gone.
- The repo (`github.com/Nayak578/FedUP`) stores scenes, models and textures through **Git LFS** (large-file storage). GitHub's free LFS allowance is small, around 1 GB. Pushing 2.9 GB of asset packs would hit the quota or cost money.
- You're on a branch called `design`, with uncommitted changes to render and project settings.
- `.gitignore` already ignores a `Builds/` folder, so packaged builds stay out of git. That's what we want.

---

## 8. GDD vs. GDD: internal contradictions I found

These aren't things I'll change. They're places where the GDD says two different things, and I need to know which one is law:

1. **Frustration values differ between sections.** Grab a Snack failure is +5% (Phase 3), +10% (Core Mechanics) or +8% (Task 3 detail). Sleep success is −20% or −10%. Phone failure is +5%, or +2% per ring.
2. **World-effect thresholds:** "30% / 60% / 90% milestones" (Pacing Curve) vs. "<30% normal, 30–70% flicker, >70% aggressive" (Core Mechanics).
3. **The meter is "0–100%"**, but the balancing section expects players to reach ~160%. Does the meter cap at 100, or keep counting?
4. **"Apartment status" and "Job outcome" are inputs to the ending table**, but no task in the GDD ever changes them. How does the player affect them?
5. **The two outcome tables (page 7 vs. page 55) don't match** row for row.
6. **Secret immediate-sleep ending** says "loop or ghost wins (randomised)". But the mandatory-cat rule says not petting the cat means you die in your sleep, and in that ending you never met the cat.
7. **Final confrontation:** the Narrative doc has a dark showdown with toast, mirrors and flashlight (gun and bat in the raw notes). The Gameplay Flow doc has a comedic boss made of toaster + TV + fridge, beaten with QTEs.
8. **"Take a Bath"** is a Phase 1 task in the GDD but isn't in your slice list. I'm assuming it slots in later, not that it's cut. Tell me if that's wrong.

---

## 9. One-page assessment

### ✅ Keep
- **Unity 6 LTS + URP project.** Correct foundation. Patch update only.
- **The apartment shell (`ver2`) as a "blockout"** (a first-pass layout). Walls, floors and stairs are cheap and correctly scaled.
- **Most furniture choices** in the living room and kitchen. They read right for the late-90s/2000s lived-in look.
- **Starter first-person controller and Cinemachine** as a base, re-tuned.
- **The team's task designs** (toaster lever + timing bar, bin cleanup, coat hook) as behaviour references.
- **Baked-lighting approach** (precomputed lighting, cheap at runtime). Right call for a horror interior.

### 🔧 Rework
- **Apartment layout:** add the entrance, a real hallway, interior walls and doors, a second bedroom and a second bathroom (with bath and mirror). Fix the roof and the floorless area. **This is the biggest piece, and it changes how the game feels, so it needs your sign-off.**
- **Performance:** replace or simplify the bed, dining chairs, stove and spoon stand. Shrink textures to GDD sizes. Trim the city to what the windows show.
- **Export the house as FBX** instead of `.blend`.
- **Lighting:** cut 69 shadow-casting lights to ~15 deliberate ones, warm yellowed sources, near-black ambient, purple fog in the hallway.
- **Post-processing:** add film grain and desaturated grading per the GDD, and remove the unused old package.
- **Player body height**, spawn point (at the front door, ground floor), remove the mobile joysticks.
- **Build settings** (point to the real scene).

### 🆕 Build from scratch
- **Interaction system** (new input only, one consistent "look and press" rule), **frustration system**, **task framework**, phone, cat, ghost events, jumpscares, QTE framework, ending resolver.
- **All audio** (none exists). **All animation** (none exists). **Frustration UI.**
- **Automated tests, the build pipeline, the evidence-capture routine.**

**Bottom line:** You have a correctly scaled house, good furniture taste and a solid engine setup. You have **no working gameplay yet**: the only interactions live in a grey-box test room and fail in the real scene. About a third of the GDD's rooms are missing.

---

## 10. Things I may have misread. Please correct.

1. **Is `firstdraft.unity` the scene you mean by "the apartment"?** (vs. `Scenes/Main.unity`)
2. **Are Nayak578 and DIVI-Hectic4U still on the project?** If so, my rewriting their scripts affects them. If not, I'll treat their code as reference only.
3. **The floorless area on the ground floor (top-left of the map).** Courtyard, or a missing floor?
4. **The empty right half of the upper floor.** Where you intended the second bedroom and bathroom, or open space on purpose?
5. **"Apartment" vs. two-storey house.** Is the big two-floor house intentional (the GDD's "surprisingly large" hint), or should it feel like a compact flat?
6. **Section 8.** Which version of each contradiction is the law?
