# Fed Up — Devlog

## Slice 1 (Greybox Gate 1) — 23 Oct 2026

**Question for you:** *"Is the toast loop annoying-but-fair within 90 seconds?"*

### What changed
- **You now start on the west porch at night**, after a short black-screen opening: the factories, the smog, *"Nine hours of making a chatbot sound human. It's better at it than me."*, your phone buzzing with **212 unread**. You open the front door and step in. It closes and locks behind you.
- **The front door is real.** I cut a doorway into the north end of the big living-room window (floor to 2.2 m, 1.1 m wide); the rest is still a window. The east corridor door is now a locked back door.
- **Brighter**, per your note: about three times the ambient light, stronger and wider room lights, more exposure. Still dark and moody, but you can read the rooms.
- **The FED UP meter** (top-left) fills and changes colour by band (calm green → uneasy yellow → agitated orange → breaking red → FED UP pulsing), and flashes each change (+10, −5).
- **The inner voice:** italic subtitles at the bottom. All his lines live in one editable list (`Data/Story/Monologue.asset`); change the words without touching code.
- **Drop Your Things:**
  - Hang your jacket, kick off your shoes, then hold E to empty your bag. Out comes the **CONFIDENTIAL** file: a placeholder absurd cutscene (camera turns to it, a one-note choir, *"Huh… what the hell is this?"*, *"That's not mine. That is very much not mine."*).
  - Later, when you're away and not looking, the jacket twitches or falls. Hang it back up.
- **Make Toast:**
  - Press E on the toaster, then press again while the marker is in the golden zone.
  - Too early and the crooked toaster launches it (usually to the floor, sometimes the counter, rarely up to the ceiling). Too late and it burns. Both cost **+10**.
  - **Two failures in a row set off the smoke alarm** on the kitchen ceiling: **+3% every second** until you reach up and silence it.
  - **Above 30% the toaster jams first:** mash E to free it.
  - Golden toast **−5** when you take it. Leave it 15 s and **something eats it (+10)**.
- **The session log:** every frustration change is written to a spreadsheet file each run (GDD "analytics hooks"), so we tune from facts.

### What I decided and why
- **Early press = launched, late press = burnt.** The GDD says mistimed toast "gets jammed, burnt, or launches"; this mapping makes each failure readable from the timing bar.
- **Failing to unjam in time counts as burnt (+10).** The GDD doesn't say; this was the least surprising choice.
- **Timing feel defaults:** the marker takes 3.5 s from raw to burnt, and the golden zone is 60–75% of the bar. These are feel numbers, so they live in `Data/Tasks/ToastTuning.asset`. Tell me "too easy" or "too tight" and I'll change one number.
- **Moved one line:** *"Toast. A man can still make toast."* now plays on your first press of the toaster. A test caught it talking over the end of the file scene.
- **The frustration values are guarded by a test.** If a data file drifts from your table by accident, the test fails.

### Evidence
- **Rules, test-first:**
  - Red: `38 tests, 23 failed` (e.g. "Bands: Expected Uneasy but was Calm"; "TwoFailuresInARow: Expected True but was False").
  - Green: **`38 passed`**, then **42 passed** with the table guard added.
- **Play Mode, 18 passed, 0 failed:**
  - The 9 Slice 0 checks, updated for the porch start: every room is still reachable and nothing outside is.
  - The prologue hands control back.
  - The door opens from the porch and locks behind you.
  - Drop Your Things completes, shows the file and costs nothing.
  - The jacket falls and can be re-hung.
  - Golden toast taken: **20% → 15%**, and the bar shows 15%.
  - Two failures ring the alarm, which costs **+5 to +7 over 2 s**, and stops when silenced.
  - At 35% the toaster jams and mashing frees it.
  - Toast left too long is eaten (+10).
  - The session log is written.
- **Packaged build** `builds/slice_1/FedUp.exe`, run hands-free. Player log:
  `AUTOPILOT DONE gate1 dropThings=Done toast=Done attempts=3 last=Golden frustration=30.2 avgFrameMs=2.69` and **0 exceptions**.
- **Session log from that run** (`evidence/slice_1_session_log.csv`): burnt +10 → launched +10 → alarm +3, +3, +3, +3 (band turns *uneasy* at 32) → silenced → jammed and mashed → golden toast −5 → **30%**.
- **Video:** `evidence/slice_1.mp4`, 60 s from the real build. I cut 11 s of plain walking from the living room to the kitchen to stay inside 60 s.

### Playtest checklist (5 minutes)
1. Let the opening play, open the front door and drop your things. **Did the file scene make you smile, and did you read both lines?**
2. Make toast until you get a golden one. Try one too early and one too late on purpose. **Is it clear from the bar *why* each one failed?**
3. Burn two in a row and let the alarm ring a few seconds before you silence it. **Does the meter climbing feel like pressure, or just punishment?**

**Feel question:** *"Is the toast loop annoying-but-fair within 90 seconds?"*

### Known limits (not bugs)
- **Sounds are placeholder beeps and tones.** Real sound comes in the art passes.
- **The cutscenes are placeholder** captions and camera moves; the real ones come in pass A6.
- **The toast doesn't eject on success** (the GDD's task page says it should). I kept the narrative page's version: golden toast pops up and waits for you. Say if you want it launched every time.

### Next
Gate 2a: **Clean Up**, **Take a Bath**, **Phone call #1** (the boss).

---

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
