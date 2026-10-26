# House Check: the apartment model, pulled out and inspected

*22 Oct 2026. I opened the Blender source in Blender, measured every mesh, walked the house in Unity with screenshots and raycasts, and mapped which rooms can be reached on foot. Then I fixed the technical problems in a **new, cleaned copy**. Your original `ver2.blend` and `firstdraft.unity` are untouched.*

---

## ⚠️ First, two corrections to my audit

I got two things wrong in `PROJECT_AUDIT.md`, and this deeper check caught them:

1. **"The upper floor has no ceiling, you see the sky." Wrong.** The ceiling is there. The player **spawns 1.7 m in the air**, so for the first moment the camera sits *above* the ceiling and looks out through the roof. That's a spawn bug, not a missing roof. There was one real gap: a 1.5 × 1.5 m ceiling patch (an attic-hatch opening) that couldn't be seen from inside. It's fixed now.
2. **"Mostly open plan, almost no interior walls." Wrong.** The house has **lots of rooms**. My top-down screenshots missed them because the walls are paper-thin surfaces, invisible from directly above. The floor plans below are generated from the actual collision walls, so they're trustworthy.

---

## What the house actually is

Floor plans (white = walls, green = you can walk there from the living room, red = you can't, black = no floor):

| Ground floor | Upper floor |
|---|---|
| ![](evidence/house/floorplan_clean_ground.png) | ![](evidence/house/floorplan_clean_upper.png) |

*Top of each image = north. Both are 24 × 20 m. The green band around the ground floor is the wooden deck that circles the house.*

**Ground floor:**
- A large **living room + kitchen** (bottom left).
- A **closed room east of the kitchen**, about 5 × 9 m. In your scene something in front of its doorway blocks the way in, so you can't get in. In the cleaned house the doorway is clear. *(I first guessed the fridge; the greybox showed the cleaned house is simply open there.)*
- **Two rooms to the north** (about 6 × 7 m and 4 × 8 m) off a short corridor.
- The **staircase**.
- A **floorless area in the north-west**, an open-air courtyard behind a wall. You can't walk into it, so there's no fall risk.

**Upper floor:**
- **Three rooms down the west side:** a front room, the bedroom (bed, desk, wardrobe) and the bathroom (toilet).
- A **landing** at the top of the stairs.
- A **cluster of three rooms in the middle-north**.
- A long **L-shaped hall** wrapping the east and south sides.

**Doors:** the Blender file has 5 door models, but they're **hidden**, so every doorway in the game is an empty hole. They were also very heavy (up to 29,000 triangles each). Real doors should come from the `nappin` pack (`(Prb)Door`).

**Ways out of the house** (all open holes, no doors):

| West: wide opening onto the front porch | East: back doorway | North: gap beside the courtyard |
|---|---|---|
| ![](evidence/house/house_exit_west.png) | ![](evidence/house/house_exit_east.png) | ![](evidence/house/house_exit_north.png) |

**Good news for the layout question:** the GDD's missing rooms mostly **already exist as empty rooms**. Upstairs there are enough rooms for a second bedroom and a second bathroom, and the landing and L-hall can serve as the GDD's hallway. **Layout Option A gets much cheaper**: mostly "decide which room is which, add doors and furniture", with almost no new walls.

---

## Defects found and what I did

| # | Problem | Fixed? |
|---|---|---|
| 1 | **Two stray boxes 1.5–2 km away** (800 m wide) inside the house file | ✅ Removed |
| 2 | **37 hidden objects** still in the file (5 doors at up to 29k triangles each, text, helper boxes) plus 12 lights and a light probe | ✅ Removed from the game copy. Doors will come from the prop pack. |
| 3 | **21 thin pipes/posts at 1,404 triangles each** | ✅ Simplified to 140 each (no visible difference at that thickness) |
| 4 | **Unapplied scale and rotation on 81 objects** (GDD SOP says apply all transforms) | ✅ Applied |
| 5 | **Shell surfaces one-sided** (ceilings and walls invisible from one side; the attic-hatch patch) | ✅ Shell made visible from both sides; hatch now closed |
| 6 | **Flipped faces** on small parts (wall-box switch) and duplicate vertices | ✅ Recalculated and merged |
| 7 | **The house is a `.blend`** (needs Blender installed to open the project) | ✅ Now exported as **`ENV_Apartment_v01.fbx`**, using your existing materials (all 38 matched by name) |
| 8 | **Player spawns in mid-air** upstairs | ⏳ Slice 0 (player setup) |
| 9 | **Doorway blocked** to the room east of the kitchen (in the old scene) | ✅ Open in the cleaned house; this is the **Bathroom Downstairs** (designer floor plans, 23 Oct). The **Storeroom** is upstairs, beside the Locked Room. |
| 10 | **Open holes to the outside** | ✅ Greybox: the east doorway is the front door (closed), the north gap is walled. The west "opening" was a route over the floorless courtyard, not a door. |

**Result:** the house went from **143,358 → 21,249 triangles** (−85%), and from 91 meshes plus 37 hidden objects to 52 clean meshes. Re-checks on the cleaned copy:

```
ceiling scan (0.5 m grid): GROUND ceiling invisible from inside = 0 | UPPER = 0   (was 9)
walkability from living room: ground 307 m² reachable, 0 m² unreachable | upper 320 m² reachable, 3 m² unreachable (outside ledge)
```

Attic hatch from inside, after the fix: ![](evidence/house/clean_upper_patch_lookup.png)

---

## Files

- `FedUP/Assets/_Project/Art/Models/Environment/ENV_Apartment_v01.fbx`: the cleaned house
- `FedUP/Assets/_Project/Scenes/HouseCheck.unity`: the house on its own with neutral lighting, for inspection
- `evidence/house/`: floor plans (original and cleaned) and screenshots
- **Not done on purpose:** the cleaned house is **not yet swapped into `firstdraft.unity`**. That needs a lighting rebake, and it depends on the greybox decision below.

Also found: **Unity 6000.0.60f1 and 6000.0.61f1 are already installed** on this PC (`D:\Unity\`). The security-patched version I said we'd need is already here, so no download is needed.
