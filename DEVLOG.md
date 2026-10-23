# Fed Up — Devlog

## Slice 2b (Greybox Gate 2b) — 23 Oct 2026

**Question for you:** *"Does the apartment feel alive without being hostile yet?"* (part 2: now with the TV and the cat)

### Your new rule: every thing has a reason
- **It's canon now.** I added it to `STORY_BIBLE.md` Part 1 ("Design logic") in your words, to the project rules and to my memory.
- **`DESIGN_LOGIC.md` is the register.**
  - It holds the evening route and **29 numbered placements**, each with its reason: the route, what you can see, what it says about him, what it sets up. Plus the decisions that aren't places.
  - It opens with an annotated ground-floor map (`evidence/design/ground_floor_logic.png`).
- **Some reasons are tested, so they can't quietly break:**
  - you can't see the phone from the couch, but you can walk to it
  - from the phone you can see down the corridor to where the cat appears
  - the couch faces the TV and nothing blocks the screen
- **Applying the rule moved three things from Gate 2a:**
  - **The phone.** It was on a table in the living room for no reason. The GDD says "the hallway shelf", so it's now on the hallway wall past the stairs. **You can't see it from the couch:** you hear it ring, get up, leave the TV and walk into the dark corridor. My first hallway spot *was* visible through the stair doorway, and the new test caught it, so it moved 2.6 m further.
  - **The laundry basket** now sits by the doorway to the stairs ("I'll take it up later", for weeks). It's at the opposite end from the bin, so every trip crosses the room.
  - **The broken mug** lies where it smashed, between the armchair and the kitchen. Before, it had landed right next to the bin.

### What changed
- **The evening is in the GDD order:** toast → bath → clean-up → **TV → the boss calls → the cat**. In 2a I'd put the call before TV; the GDD has it after TV, "before the sleep trigger".
- **Watch TV** (living room):
  - Hold E on the couch to sit; the camera drops and faces the screen. While sitting, **E flips the channel** and **moving stands you up**.
  - The channels: **static** (50%), **infomercials** (30%, *"FocusMax! Work twenty hours. Feel nothing."*), **horror** (15%: AI-layoff news, or whispering) and, rarely, **calm** (5%): **old home video of him as a kid on a red bike**.
  - Static and horror cost **+7**, ads **+5**, but **the TV adds +10 at most in total**. The room's light changes colour with every channel.
  - **Spam it** (10 presses in 5 s) and the batteries pop out under the couch: *"You lost the remote again…"* (+10, within the cap). Get up and feel under the couch to find them.
  - **Above 50%, a hand under the couch may grab the remote:** mash E to win the tug of war. Lose, and it leaves the remote at the foot of the TV (+5).
  - **Stay on the home video for 10 s:** **−10**, and *"That kid had plans. …Sorry, buddy."*
- **The boss calls** while the home video plays. The phone is out of sight, in the hallway.
- **The cat, visit #1:**
  - Stand still after the call and a **black cat** comes down the dark corridor from the back hall, where the back door is locked. Nobody can come in that way. Its eyes catch the light first.
  - It sits in front of you. *"…I don't have a cat."*
  - **Hold E to pet it:** it purrs, the picture warms and softens for a moment, **−20**, *"Finally, someone chill."* Then it walks back into the dark.
  - **Ignore it for 20 s** and it leaves: *"Cats are smart. They don't stay where they're not loved."* That costs nothing now, but **the night remembers** (the ending's cat rule).
  - **Above 60%** it may hiss and vanish onto the couch armrest (your spot). Try again after 30 s, for −10.

### What I decided and why
- **The +10 TV cap covers everything the TV does** (channels, batteries, the ghost grab). Your table says "TV can add at most +10 in total". Calming isn't capped.
- **The ghost grab is a 10% chance per press, only above 50%.** The GDD says "low chance". Ten percent keeps it a surprise. It's one number in `Data/Tasks/TvTuning.asset`.
- **The cat waits 20 s.** The GDD doesn't say. That's long enough to notice it and walk over, short enough that ignoring it is a choice. It's in `Data/Tasks/CatTuning.asset`.
- **A black cat with eyes that catch the light.** The old omen reads instantly, even as boxes, and you see it coming down the corridor before you see the cat.

### Evidence
- **Rules, test-first:**
  - Red: `101 tests, 66 passed, 35 failed` (e.g. "Expected: Stolen, But was: Ignored", "FE_Cat_Petted: Expected not null").
  - Green: **`101 passed, 0 failed`**.
  - Your frustration table is now guarded for every Gate 2 value too.
- **Play Mode: 40 passed, 0 failed** (12 new).
  - The first run had **6 failures, all fixed:**
    - A skipped phone call could still ring out later (+2). Fixed in the game, not the test.
    - The under-couch search spot was out of arm's reach.
    - The phone was visible from the couch (see above).
    - A Gate 1 jacket test failed once, then passed on two reruns. I'm watching it.
  - TV: bad channels **0 → 7 → 10, then capped**. Spamming loses the batteries and you find them. Lose the tug: **55 → 60**, remote at the TV's foot. Win the tug: no change. Calm 10 s: **30 → 20**, then the phone rings.
  - Cat: petted **30 → 10**, recorded, screen softens. Ignored: walks off, recorded as not petted, costs nothing. At 70% it hisses, reappears on the couch, and the retry after 30 s gives **70 → 60**.
  - The three design-reason checks above.
- **Packaged build** `builds/slice_2b/FedUp.exe`, run hands-free. Player log:
  `AUTOPILOT DONE gate2b tv=Done batteries=1 phone=answered cat=petted frustration=0.0`, **0 exceptions**.
- **Session log from that run** (`evidence/slice_2b_session_log.csv`): static +7 → infomercial +3 (cap reached) → calm −10 → phone and cat both calm, but the meter was already at 0.
- **Video:** `evidence/slice_2b.mp4`, 60 s from the real build. I cut about 18 s of walking and waiting. It starts at the TV (Gates 1 and 2a have their own videos). The demo fixes the dice so every outcome shows up in a minute. It can't show the ghost grab, which only happens above 50%; the tests cover that.

### Playtest checklist (5 minutes)
1. Sit and flip channels until you find the home video. **Did hunting for it feel like a small hope, or just a chore?**
2. When the phone rings, **did getting up and leaving the TV feel like an intrusion?**
3. Meet the cat. Pet it once, then replay and ignore it. **Did ignoring it feel like it mattered, even though nothing happened?**

**Feel question:** *"Does the apartment feel alive without being hostile yet?"*

### Known limits (not bugs)
- **The channels are coloured screens with captions.** Real footage comes in the art passes.
- **The cat is boxes, with no animation** (it slides and sits). The real cat comes in pass A1.
- **He doesn't fall asleep on the couch yet.** That's Gate 3. The night ends on *"Eyes… heavy. Just five minutes on the couch."*

## Slice 2a (Greybox Gate 2a) — 23 Oct 2026

**Question for you:** *"Does the apartment feel alive without being hostile yet?"*

### What changed
- **Your room labels are in.** Every room now carries the name from your floor plans:
  - **Upstairs:** Bedroom, Work Room, Balcony, Bathroom Upstairs, Bathroom Middle, Wardrobe, Storeroom, Locked Room.
  - **Downstairs:** Guest Bedroom, Garage, Bathroom Downstairs.
  - **The Locked Room really is locked:** its door is shut and the game proves you can't get in.
  - **The garage shutter is closed** (it used to open onto nothing).
- **Take a Bath** (Bathroom Downstairs):
  - Hold E at the tub to run the bath. You sink in (the camera drops to bath height) and **hold E to relax**: the bar only fills while you hold.
  - Your clothes hang on three hooks. Every few seconds one starts to slide. **Press the arrow shown (← → ↑ ↓) within 1.6 s to snatch it.** Too slow and it's gone: **+6** and *"…Not again."*
  - Nothing lost: **−10**. Lost anything: *"Fine. I didn't need it anyway."*
- **Clean Up the Mess** (Living Room):
  - Four bits of mess: a shirt, cans, a takeout box and a broken mug. Each has its own line (*"'World's Okayest Employee.' Even the mug gave up."*).
  - Pick one up with E and carry it. Shirts go in the **laundry basket**, everything else in the **bin**. The wrong one is refused (*"Not in there."*).
  - **The joke, once per night:** the first time you bin three in a row, the third item **flies back out** (+6) and something giggles (+2). *"…Was that you?"*
  - **Above 40%, the ghost may knock a drop back out** (40% chance; +6 and +2). After a streak of three, it leaves you alone for 60 s.
  - **After that streak, if the room stays clean for 30 s, you calm down (−10).**
- **Boss call #1:**
  - After the clean-up, the house phone in the living room rings for 15 s. The line is bad: *"Hey— it's me… you l—ft something on… the f—le…"*
  - **Answer it: −5. Ignore it: +2.** Either way the game now **remembers** it for the ending (the "phone answered" flag).
- **Two things you can read** (optional; they cost nothing): an unopened prescription on the coffee table, and a birthday card *"from Mom. …That was March."*

### What I decided and why
- **The bath's "slide" is a direction prompt (← → ↑ ↓).** It reads instantly and works on a gamepad later. The timings (12 s of relaxing, clothes at 3.5, 7 and 10.5 s, 1.6 s to react) live in `Data/Tasks/BathTuning.asset`.
- **The phone rings after the clean-up.** That gives the evening a clear beat (bath, tidy, then the boss intrudes). Moving it is a one-line change.
- **Clean-up numbers** (40% gate, 40% chance, 60 s of peace, 30 s for the clean-room bonus) are in `Data/Tasks/CleanupTuning.asset`.
- **Mess never hides behind furniture.** The first run dropped the takeout box in the gap behind the sofa's arm, where you could barely see it. Every item now needs open floor around it.

### Evidence
- **Rules, test-first** (bath and clean-up maths):
  - Red: `57 tests, 42 passed, 15 failed` (all 15 new bath and clean-up rules).
  - Green: **`57 passed, 0 failed`**.
- **Play Mode, 28 passed, 0 failed** (the 18 earlier checks plus 10 new):
  - Perfect bath: **20% → 10%**, control comes back. Lose everything: **+18**.
  - Clean everything: done, with exactly one flyback: **+8**.
  - Wrong container refused.
  - At 50%, the ghost rejects a drop: **→ 58%**.
  - Both readables speak.
  - Phone answered: **20% → 15%** and recorded. Ignored: **+2** and recorded as missed.
  - The Locked Room can't be reached, and every one of your room names exists.
- **Packaged build** `builds/slice_2a/FedUp.exe`, run hands-free. Player log:
  `AUTOPILOT BATH saved=2 lost=1`, `AUTOPILOT CLEANUP done=True flybacks=1 rejections=0`, `AUTOPILOT DONE gate2a bath=Done cleanup=Done phone=answered frustration=9.0 avgFrameMs=2.68 worstFrameMs=11.39`, **0 exceptions**.
- **Session log from that run** (`evidence/slice_2a_session_log.csv`): lost a shirt +6 → comedy flyback +6, giggle +2 → answered the boss −5 → **9%**.
- **Video:** `evidence/slice_2a.mp4`, 60 s from the real build. It skips the Gate 1 chores (they're in the Slice 1 video). I cut about 38 s of walking between rooms.

### Playtest checklist (5 minutes)
1. Take the bath. Save one item and let one go on purpose. **Was the slide prompt easy to see while you were relaxing?**
2. Clean the living room. **Did the item flying back make you laugh, or feel like a bug?**
3. When the phone rings, answer it once, then replay and ignore it. **Did either choice feel like it mattered?**

**Feel question:** *"Does the apartment feel alive without being hostile yet?"*

### Known limits (not bugs)
- **Sounds are still placeholder tones** (the ring, the giggle).
- **Clothes and mess are grey boxes**; real props come in pass A1.
- **The test demo skips Gate 1.** In the build you play, you still do all of Gate 1 first.

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
