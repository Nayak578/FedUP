# Fed Up — Devlog

## Slice 0 (Greybox Gate 0) — 22 Oct 2026

**Question for you:** *"Does moving through the apartment feel right?"*

### What changed
- **A grey version of the real house you can walk through.** `_Project/Scenes/Greybox.unity` uses the cleaned house walls (from `HOUSE_CHECK.md`) in plain grey. Simple boxes (`GB_…`) stand exactly where today's furniture stands; there are 32 of them, copied automatically from `firstdraft.unity`.
- **You start just inside the front door, in a dark entrance corridor.** A light switch sits on the wall: look at it, the prompt says "E — Light switch", press E and the entrance light comes on.
- **The atmosphere kit is in from day one:** near-black ambient light, dim warm lights in each room, dark purple fog, film grain, vignette and desaturation.
- **Walking:** tired pace 2.5 m/s, sprint 4 m/s (Shift), no jump, slight head-bob. All of these live in `Data/Player/PlayerTuning.asset`; tell me a feeling and I change a number.
- **You can't leave the house.** The front door is closed and the 4.8 m gap on the north side is walled.
- **Room names are provisional.** Downstairs: Entrance, Living Room, Kitchen, Store Room, plus two spare rooms. Upstairs: Landing, Bedroom 1, Bathroom 1, Bedroom 2, Bathroom 2, Hallway, plus two spare rooms.

### What I decided and why
- **Front door = the doorway at the east end of the entrance corridor, not the west "porch opening".** The deeper check showed the porch route was really a path across the floorless courtyard onto the ground outside. The east doorway is the only door-sized way in, and its corridor makes a natural entrance hall for "Drop Your Things".
- **The store room** (the GDD's "active store room") is the room east of the kitchen. In the cleaned house its doorway is open; in the old scene something in front of it blocked the way.
- **I wrote our own player controller** instead of reusing Unity's Starter Assets. Theirs can't be linked to our separately compiled code and brings mobile joysticks along. Same feel, every number editable.
- **The test guard is live.** During this slice it blocked me twice: once a false alarm, which I fixed, and once correctly, which made me add tests as a new file instead of moving old ones. It now fails safe: if the guard itself breaks, it blocks rather than lets changes through.

### Evidence
- **Edit Mode tests (press/hold timing):** red first, then green.
  - Red: `5 failed, 2 passed` (e.g. "HoldingPartWay_ReportsProgress — Expected 0.25, But was 0.0").
  - Green: `7 passed, 0 failed`.
- **Play Mode tests: 9 passed, 0 failed.**
  - The scene loads with no errors.
  - You start at the spawn point, on the floor.
  - Two seconds of walking covers 3.5–5.5 m.
  - **Every room can be reached on foot, every room has a ceiling, and the outside can't be reached.**
  - The prompt shows at 1.5 m, E toggles the light on and then off, and there's no prompt at 3 m.
- **Packaged build** `builds/slice_0/FedUp.exe`, run hands-free with `-autopilot`. Player log:
  `AUTOPILOT DONE visited=9/9 switchToggles=1 avgFrameMs=2.54 worstFrameMs=6.09` and **0 exceptions** (about 390 fps average on this PC, far above the 60 target).
- **Video:** `evidence/slice_0.mp4`, 60 s at 1280×720, recorded from the real build window.
- **Test-guard proof** (simulated calls in a fresh session):
  - Blocked: deleting an assertion, a `sed` rewrite via Bash, deleting via PowerShell, wiping via Unity C#.
  - Allowed: adding a test, reading tests.
  - Blocked at session end: a changed test with no code change.

### Playtest checklist (5 minutes)
1. Launch `builds\slice_0\FedUp.exe`. You're in a dark corridor. Find the light switch on the left wall and press **E**. Does the prompt appear before you have to hunt for it?
2. Walk to the living room and kitchen, then up the stairs and into the bedroom and bathroom. Is it always clear which way leads where, and do you ever get stuck on a box or doorway?
3. Walk normally, then hold **Shift**. Does the normal pace feel *tired*, not sluggish, and does sprinting feel like hurrying rather than running a race?

**Feel question:** *"Walking through this place, does it feel like coming home to somewhere slightly too big and slightly wrong?"*

### Known limits (not bugs)
- **Everything is grey boxes on purpose.** Real furniture comes in art pass A1.
- **The video looks very dark** because the scene is dark. If it's *too* dark to read the rooms when you play, say so; it's one number.
- **The light switch makes no click yet:** there's an empty sound slot, and audio comes later.
- **The build is a "development" build** and runs only on this PC. Before any build is shared, I'll move the project to Unity 6000.0.61 (already installed) for the security fix.

### Your feedback (22 Oct)
- **The west porch is the main entrance.** It moves there in the next build; the east doorway becomes a locked back door.
- **Too dark:** the next build is slightly brighter.
- You asked for the whole night as a story before going on: [`STORY_FLOW.md`](STORY_FLOW.md).

### Next
Gate 1: Frustration bar + **Drop Your Things** + **Make Toast**. Question: *"Is the toast loop annoying-but-fair within 90 seconds?"*
