# Fed Up — Devlog

## Slice 5b (Greybox Gate 5b) — 26 Oct 2026

**Question for you:** *"Does every ending feel like the night you played?"*

### What changed (Gate 5b)
- **Mom's voicemail** (your Option A):
  - His phone lies on the entrance table beside her birthday card, the screen pulsing "1 new voicemail".
  - Listening is optional. It's a butt-dial: her voice in pieces mid-prayer (*"…I see you… I'm here… stay with— stay with me, stay—"*), then *"oh— did I— is this on?"*, then a click.
  - You never hear the whole mantra. He says *"…Later, Mom."*
  - The phone goes with him when the house turns, because it's his torch.
- **The tea is upstairs now** (your approved P1). When the ingredients vanish, a tin drops and rolls across the ceiling over the kitchen: *"…That came from upstairs."*
- **The Storeroom** is a walk-through room full of a life:
  - boxes, a taped-up trophy box, a broken chair
  - **the axe on the wall** straight ahead as you come in from the landing, with **the tea tin on the shelf right under it**
  - against the north wall, a tall shelf with **the top of a door frame showing above it**, and dust drifting towards it
- **The blackout:** take the tea and your breath is cold, the light goes, black.
  - **The Locked Room, dreamt**, for six seconds. You can look but not walk. It's a desk and an upright chair facing a shut window and the factory glow, a water stain, a mattress on the floor. A shape passes the window (a 0.26 s glimpse).
  - Black again, then **you wake lying in the bathtub**, the milk beside you (if you hadn't found it), still holding the tea.
  - Once a night, and the hunt carries on.
- **The endings now have scenes** (in-engine, first person, with placeholder captions; real cutscenes are A6):
  - **The loop:** black, 7:00 AM, the alarm three times, then you're on the porch at dusk again, the house lit for the evening as if nothing happened. *"…Again."*
  - **Unemployed, ghost angry:** 7:02 AM, *"…don't bother coming in."* Then the house rages: six doors slam upstairs in turn, the TV comes on to static, the lights stutter. *"…Yeah. Me too."*
  - **You die in your sleep:** a slow fade, then the night's sounds stop. Morning. His phone rings, and rings: *Mom*. Nobody picks up.
  - **Ghost Burial, the part in the house:**
    1. He wakes calm and says his mother's words whole for the first time: *"I see you. I'm here. Stay with me."* (*"…That's what she used to say. When I was sick."*)
    2. The ghost runs, never letting you closer than 3.5 m, through the Storeroom past the axe, to the north end of the east hallway.
    3. It walks into the wall and leaves a crack. You take the axe; three swings.
    4. You **squeeze sideways into the cavity between the double walls** and shuffle the length of the house to the triangle.
    5. Darkness rises from a vent. Crawl in.
    6. The other world is art pass A4, after the sensitivity read.
- **Every ending card has a third line:** what happened to his job, his home and the ghost. The card lines got their final pass, e.g. the loop: *"7:00 AM. The alarm. The office. The bag. The toaster. He tells himself tonight will be different."*

### What I decided and why
- **The mantra's words are a proposal (P3 in the bible), waiting for your yes:** *"I see you. I'm here. Stay with me."*
  - It's his mother's prayer, so it's also where his "I see you. I'm here." to the player comes from.
  - The ghost wanted to be seen.
  - One line in the monologue file changes it.
- **The ghost's chase goes through the Storeroom on purpose.** The shortest route skips it, but the axe needs to be fresh in your mind when the crack appears.
- **The cavity between the east walls measures about 0.3 m, not 0.6.** The hallway wall is 0.3 m thick (x −25.59 to −25.29) and the cavity ends at −24.97. So "squeezes sideways" is literal: in there your body is thin and you move at about a third of walking speed. `CLAUDE.md` says 0.6 m; that's your file, so I haven't changed it.
- **The triangle has floor in this house model,** so the vent is a dark opening I placed in it, with darkness rising.
- **The dream room keeps to research rule 10.** The chair is upright, nothing hangs, nothing is knocked over, and the stain could be water. A test checks the chair.
- **Mom's voicemail doesn't use any word that would set the family's culture.** That's yours to decide if you want it.

### Evidence
- **Rules, test-first:** the blackout and the Burial chase went red `8 tests, 4 passed, 4 failed`, then green **8/8**.
  - New card checks: every ending has a title, a line and an outcome; the Burial cards carry the canon voicemails; Mom's voicemail never contains the whole mantra.
  - **EditMode 221 passed, 0 failed.**
- **Play Mode: 109 passed, 0 failed** (13 new).
  - The new tests cover the voicemail, the tea upstairs, the blackout, all four new ending scenes (including the whole Burial walked on foot through the cavity), and the design reasons.
  - Older tests updated for the changes you approved:
    - the tea's hiding place is now under the axe
    - the Gate 4 tea tests reach upstairs, with the blackout switched off so they measure the tea task alone
    - the 5a sleep tests wait for each ending's scene before its card
- **Packaged build** `builds/slice_5b/FedUp.exe`, run hands-free, 0 exceptions:
  `AUTOPILOT VOICEMAIL listened=True` · `AUTOPILOT BLACKOUT happened=True glimpse=0.25s milkBeside=True tea=Carried` · `AUTOPILOT DONE gate5b … ending=BurialAnswered hits=3 card=GHOST BURIAL outcome='Job: safe · Home: safe · The ghost: at peace'`
- **Smoothness** (a plain run): average **7.1 ms**, worst **17.9 ms**. The first run had a 124 ms hitch from sounds made on the spot (the tin, the alarm, the ring), now made at load.
- **Found from the video and fixed:**
  - **The Burial was lit red, torch and all.** The bed's red light stayed on after you woke; waking now restores the house's light.
  - **A cat visit that hadn't started yet when you lay down started afterwards,** and said its line inside the wall. Going to bed now cancels cats that haven't come yet, as it does calls.
- **Video:** `evidence/slice_5b.mp4`, 60 s, cut from 145 s of the real build in eleven pieces:
  1. the voicemail
  2. the vanish and the tin overhead
  3. the axe
  4. the dreamt Locked Room
  5. waking in the bath
  6. the mantra
  7. the ghost going into the wall
  8. the axe on the wall
  9. the squeeze
  10. the darkness and the vent
  11. the card

  No sound.

### Playtest checklist (20 minutes)
1. **The evening:** do you notice the phone on the table? If you listen, do the pieces of her voice stay with you?
2. **The tea:** do you hear it go upstairs, and find the Storeroom? When you take the tea, does the blackout land as "did that happen?", and does waking in the bath make the bathroom worse after?
3. **Two endings:**
   - one calm night: wake up below 100% and follow the ghost into the walls
   - one where you stay asleep

   Does each scene say who he is?

**Feel question:** *"Does every ending feel like the night you played?"*

### Designer's answer: *awaiting your one sentence*

### Known limits (not bugs)
- **The other world (the Burial's second half) is art pass A4**, after the sensitivity read. The greybox ends at the vent.
- **The scenes are greybox:** captions, fades and the house itself. Proper cutscenes are A6.
- **The mantra's words are a proposal** until you say yes (P3).
- **Placeholder tones throughout,** including Mom's voice and the alarm (A2 gives six real sounds).

## Slice 5a (Greybox Gate 5a) — 26 Oct 2026

**Question for you:** *"Does the ending choice feel earned?"*

### Your review, done first
- **Your rulings are written in:**
  - `CLAUDE.md`:
    - the writing rules
    - "answered = 3 of 4 calls"
    - the 5a/5b split
    - the six-sound A2 gate
    - the sensitivity reader before A4
    - room names
  - `STORY_BIBLE.md` Part 1:
    - the boss dies at the gate either way
    - Mom's butt-dial
    - the Storeroom blackout
    - your two Mr. Robot lines
  - `MACRO_PLAN.md` and `DESIGN_LOGIC.md`.
- **Your two approvals are canon:** P1 (the Storeroom details) and P2 (the charger trip for the gun).
- **"How would the player know to go upstairs?"** Three layers, loudest first: the objective line, his voice, then the house itself (a light left on, a sound from that direction). The full answer is in `DESIGN_LOGIC.md`, "How the player knows where to go":
  - **The charger:** the torch starts dying, the landing is the one light on upstairs, and he says *"4%. The charger's upstairs. Of course it is."*
  - **The tea (5b):** you hear the tin drop and roll across the ceiling above the kitchen, because the Storeroom is right above it.
  - **The final sleep:** "Go to bed". The charger trip already showed you the way.
- **Writing rule 1 is applied to every line.** All 82 existing lines are tagged with the rule they play. Nine broke it and were rewritten (listed in `DESIGN_LOGIC.md`; veto any). For example:
  - *"No. No, no, no."* → *"…That's new."*
  - *"Get off. GET OFF."* → *"Off. Thank you."*
- **"Designer's answer" lines** are now under every gate's question in this log. Gate 2b has your words; Gates 3 and 4 are waiting for yours.

### What changed (Gate 5a)
- **The night now has an end.** In order:
  - Phase 2: the charger trip, the snack, call #2
  - Phase 3: the tea, cat #2, the appliance chaos with calls #3 and #4, cat #3
  - **the final sleep**, then **the ending**
- **Phase 2, the charger** (your approved P2):
  - Back in the living room the torch starts dying.
  - Upstairs, the bedside drawer is ajar. The first time it's in your beam, **the gun is there for 0.2 s** and the drawer slides shut by itself. No line, no sting.
  - Plug in: *"Twelve percent. Living the dream."*
- **Calls #2–4** (the phone is never in the same place twice):
  - **#2** rings on the couch, where your head was.
  - **#3** lies on the back-hall floor under the fuse box, and it's the distorted one: your own voice, slowed down, then backwards.
  - **#4** sits on the file at the front door: *"Fine. I'm coming over to get it myself."* Ignore it and the answering machine plays it to the empty room.
  - Each ring after the first costs +2. Answering within 10 s is −5. The distorted voice is +5.
  - #3 and #4 ring **over** the appliance chaos, so you choose: the phone or the microwave.
- **Cats #2–3** (perched, with a collar bell so you can find them by ear):
  - #2 is **on the kitchen counter**, judging you after the tea. *(The kitchen's fridge has cabinets up to the ceiling, so "top of the fridge" doesn't exist here.)*
  - #3 is **asleep on the guest bed**, the only guest who ever came.
  - Walk up and hold E to pet it. **Run at it and it jumps to its other perch.**
  - A pet hushes the house for 20 s: the drone dips and the ghost holds off.
  - The night doesn't stop for the cat: it waits while you carry on.
- **The final sleep** is in his own bed upstairs, for the first time tonight:
  - The lights turn red.
  - Above 50% the ghost disrupts you: **breathe on the heartbeat**, three beats on the toast's timing bar.
    - A miss is a scream: +15, and the first retry costs +10 more (once).
    - Three misses and the nightmare just happens.
  - A clean sleep is −20, which can pull you back under 100%.
  - Then the blanket lifts. **Press E to wake up, or do nothing and stay asleep.** Doing nothing is the passive choice.
- **The ending resolver** reads the cat, the phone (3 of 4) and how you woke, exactly per `MACRO_PLAN.md` Appendix 2. Every one of the eight endings finishes on a title card and one line.
- **The Showdown** (wake at 100%):
  1. He takes the gun from the same drawer. Every light dies.
  2. A red trail runs from the bed down to the porch, and the ghost's image waits at each turn. Shoot it and it's further on; every shot breaks something in the house.
  3. The front door that locked behind you at the start of the night bursts open.
  4. The last image stands at the gate, under the street lamp.
     - **Phone answered:** as you aim, the lamp stutters and it's the boss.
     - **Ignored:** it stays the ghost until the shot.
  5. The lamp comes up on the boss, dead, the file in his hand. *"…Sir?"*
  6. You turn round, and **the ghost is standing in the doorway.**
- **The secret ending:** go straight to his bed before your first chore. *"Maybe ignoring it all was the real answer."* Then, at random, the loop or the ghost wins.
- **Your two Mr. Robot lines are in:**
  - after cat #1 leaves: *"You saw that too, right? …Right."*
  - after the mirror cracks: *"You're still here. Huh. Nobody stays."*
- **The front yard and the gate** now exist: lit by a street lamp, visible behind you on the porch at the very start of the night.

### What I decided and why
- **Cats #2–3 and calls #3–4 run alongside the chores.** A minute of waiting for an optional cat would kill the pace, and a call that interrupts nothing isn't "mid-task".
- **Going to bed ends whatever's waiting.** A cat on its perch leaves, unpetted. A ringing call goes to the answering machine. Nothing rings or costs you while you lie in bed.
- **Lines never talk over each other.** The next chore waits about 3 s before speaking.
- **A short pause before the blanket lifts,** so your last breath press can't count as "wake up". The tests caught exactly that.
- **The upstairs bed was a 2.25 m solid block** in greybox (the model's whole bounding box, frame and all). It's now a mattress with a headboard, same footprint.
- **The ghost at the end is in the doorway, not the upstairs window.** From the porch, the porch roof hides that window.

### Evidence
- **Rules, test-first:**
  - The ending table, calls #2–4 and the final sleep: red `208 tests, 168 passed, 40 failed` (e.g. "Expected: DieInSleep, But was: Loop"), then green.
  - The director's hush after a cat: red `Expected: Quiet, But was: Allowed`, then green.
  - Final: **EditMode 210 passed, 0 failed.** That includes every 5a frustration value from Appendix 1 and the writing rules (every line tagged; at least two lines to the player).
- **Play Mode: 96 passed, 0 failed** (26 new). Every older gate's tests pass under the new night order.
  - Four older tests were updated because the night changed:
    - there are now four calls, not one
    - call #2 comes after the snack
    - bed comes after the chaos
    - the charger comes after the wake-up
  - The Gate 4 appliance tests mute the 5a mid-chaos calls, so they measure the appliances alone.
- **Real bugs the in-game tests and the build found, fixed:**
  - **The front door re-locked behind you in the Showdown,** so you could never reach the porch. It now bursts open and stays open, with a test that walks out through it.
  - **Your last breath press counted as waking up,** taking the choice away.
  - **The charger line talked over** *"…I'm not going back to sleep."*
  - **The gun's sightline was blocked by the player's own body.**
  - **The breath bar showed a stale marker between beats.**
  - **A still-ringing call could cost frustration while you lay in bed.**
  - **The bed's red light turned the street lamp red,** so the reveal was unlit.
  - **The Showdown was almost black on screen.** The trail now faces you and glows, and the fog is half as thick.
- **Packaged build** `builds/slice_5a/FedUp.exe`, run hands-free, 0 exceptions:
  `AUTOPILOT CHARGER gunShown=True visible=0.20s` · `AUTOPILOT SLEEP fails=1 choice=WakeUp ending=ShowdownAnswered frustration=117` · `AUTOPILOT DONE gate5a gun=True calls=4 cats=True/True/True ending=ShowdownAnswered transformed=True shots=4 wrecked=4 card=HORROR SHOWDOWN`
- **Smoothness** (a plain run, not recording): average frame **2.4 ms**, worst **13 ms**.
- **Video:** `evidence/slice_5a.mp4`, 59 s, cut from 115 s of the real build in ten pieces:
  1. the stairs and the charger
  2. call #2
  3. the cat on the counter as the microwave starts
  4. call #3
  5. call #4 on the file
  6. cat #3
  7. the red bed and the breathing
  8. waking fed up
  9. the trail
  10. the gate, the boss and the doorway

  No sound, as before.

### Playtest checklist (15 minutes: one full night, then a second)
1. **Play the whole night your way.** Did you notice the gun? Did you know where to go each time (the charger, each phone, each cat)? Write down any moment you were lost.
2. **At the final sleep:** did "press E or do nothing" feel like a real choice, and did the ending you got feel like *yours*?
3. **Second night, the opposite:** ignore the calls and the cats, and rush. You should die in your sleep. Does that read as a loss, not an escape?

**Feel question:** *"Does the ending choice feel earned?"*

### Designer's answer: *awaiting your one sentence*

### Known limits (not bugs)
- **Only the Showdown and the secret ending have scenes.** The loop, unemployed, both Burials and dying in your sleep are title cards for now (Gate 5b). The Burial itself is art pass A4.
- **5b content isn't here yet:**
  - the Storeroom blackout
  - Mom's voicemail
  - the tea moving upstairs
  - the final content pass on the ending cards
- **The reveal at the gate is small and far away** in greybox; the Showdown's camera work is pass A3.
- **Placeholder tones throughout**, including the boss's voice, which is a caption.
- **One older test is flaky:** Gate 3's lie-down test. Its helper drops you next to the couch without checking the view. It failed once in three full runs (it passed on its own and in the final run). The fix is to make its helper pick a clear spot, as the Gate 4 and 5a helpers do.
- **The hands-free demo** snags on the hall corner by the stairs, so the video skips that walk. A player walks round it; the stairs themselves are tested on foot.

## Slice 4 (Greybox Gate 4) — 24 Oct 2026

**Question for you:** *"Does the night feel like it's escalating, and do the calm moments land?"*

### Your notes from Gate 3, done first
- **"No way to make a mistake on the first try" (toast).**
  - It *was* possible to fail, but far too easy to succeed: the marker crawled across in 3.5 s and the golden zone lasted over half a second.
  - The toaster is now **crooked**. The marker starts slow and **lurches faster right through the golden zone**, the sweep is 2.8 s, and the zone is 62–73%.
  - That leaves about a quarter of a second to hit. Fair, but you have to watch.
- **"The clothes should keep being snatched" (bath).** For as long as you lie there, the ghost snatches at the clothes **every 2.6 s, going round the hooks**.
  - A piece you save goes back on its hook and can be snatched again.
  - Each snatch asks for a different arrow.
  - Lost pieces stay lost (+6 each); nothing lost is still −10.
- **Research.** `HORROR_RESEARCH.md` collects papers and studies on fear, immersion and storytelling, and turns them into **10 rules for Fed Up**, with sources. The highlights:
  - **Fear has a sweet spot:** a heart-rate study at a haunted house found enjoyment peaks at medium fear, and the peak differs per person. That's why the director and the calm items matter.
  - **Sound does most of the work.**
  - **The familiar made slightly wrong** (the uncanny).
  - **Show less than you could.**
  - **Absurd first, then scary.**
  - **He under-reacts to the impossible and over-reacts to the trivial:** that's what "real and unreal at the same time" is made of.
  - **Depict despair honestly and suicide responsibly.** Your Burial ending (a promise to live) is already the protective kind of story.
  - It ends with **three questions for you** (below).

### What changed (Gate 4)
- **Grab a snack** (back in the living room at 3 AM, then the fridge):
  - The fridge greets you: *"Back so soon?"*
  - **Above 30% the snack bar slides away** if you stare at it. Grab it mid-slide and you miss (+5).
  - **Hover 3 s** and the fridge mocks you (+3).
  - **Leave it open 10 s** and a hand at the back pulls it the other way. Mash E to win, or lose it (+10).
  - **15 s** and the door slams.
  - The snack itself: −5.
  - **Sometimes there's a juice box** (−5, optional): *"Apple. Like the ones Mom used to pack."*
- **The radio** on the dining table (optional, from the wake-up on): the lo-fi song from his old relationship. Let it play **15 s within earshot** for −5, once a night.
- **Phase 3 begins with Make Tea.** The fog turns **purple** (the game's supernatural signal).
  1. **Gather** the milk, sugar and tea in the kitchen. **The lights stutter, and they're gone.**
  2. **Find them:**
     - the milk **in the bathtub**
     - the sugar **beside the silent phone**
     - the tea **on the guest-room bed** (a made bed nobody sleeps in)
  3. **Carry two at a time** to the tea station.
  4. Walk back into the kitchen with the last one and **the power cuts** (once).
     - **Every light and the TV** go off; the wall switches stop working, and only your torch works.
     - **You have 30 s to flip the breaker in the back hall**, down the dark footsteps corridor.
     - **Too slow** and everything goes back to its hiding place (+10). That happens **once a night at most** (the GDD rebalance).
  5. **Put the kettle on.** It screams **in the boss's voice**: *"WHERE. IS. MY. FILE."*
  6. **The cabinets start throwing.** Each throw is announced by a cabinet banging open. **Stay in the kitchen and dodge for 20 s** (leaving pauses it).
     - Each hit is +7.
     - **Three hits and the stove catches fire** (+15, no tea).
     - Survive, turn the stove off: **tea (−10)**: *"Tea. I made tea. In this economy."*
- **Appliance chaos:** when the ghost director allows, **the microwave, then the blender, then the bathroom sink** switch themselves on.
  - **Hold E within 10 s** to shut each off, or it overloads (+10).
  - **Above 50%** a pale hand may cover the switch (+7). **Shine the torch on it and press E** to scare it off (−5).
  - The sink is the one you splashed your face at in Gate 3, now overflowing, across the house from the kitchen.
- *"The whole house is awake. So I am too."* That's the end of this slice; Gate 5 brings phone calls #2–4, cat visits #2–3, the final sleep and the endings.

### What I decided and why
- **Every placement has its reason** in `DESIGN_LOGIC.md` #40–52. The hiding places send you back through Gate 3's scare rooms, the fuse box sits at the dark end where the cat came and went, and the throws come from above the stove where you're already looking.
- **The guest room was empty in the house model,** so I added a made double bed. The tea needs a bed nobody sleeps in.
- **The power cuts once per hunt.** The tests caught that walking back in after fixing the fuse cut it again, forever. The GDD says "upon returning with the 3rd item", so it's once.
- **The dodge pauses if you leave the kitchen,** so running away isn't a skip.
- **Appliances are released by the ghost director, one at a time,** so they space out like scares instead of piling into noise.
- **The line "…I'm not going back to sleep."** now plays when you get back to the living room, since the night now carries on from there.

### Evidence
- **Rules, test-first:**
  - Bath and toast: red `118 tests, 106 passed, 12 failed`, green **118/118**.
  - Gate 4 (snack, tea, dodge, appliances, radio, plus 13 frustration values): red `162 tests, 123 passed, 39 failed`, green.
  - One more rule found by the in-game tests (the power cut must not repeat): red `Expected: False, But was: True`, green.
  - Final: **163 passed, 0 failed**.
- **Play Mode: 70 passed, 0 failed** (20 new). Highlights:
  - **Snack:** "Back so soon?"; above 30% it slides away and a grab mid-slide fails; at 10 s the ghost hand wins (+10).
  - **Tea:** gathering makes everything vanish; you carry two at a time; the last one cuts the power **once**; the fuse box fixes it; too slow re-scatters everything once (+10).
  - **Dodge:** dodge every throw and turn the stove off for −10; three hits start the fire.
  - **Appliances:** above 50% the ghost covers the switch, and the torch scares it off.
  - **Design reasons are tested** for every Gate 4 placement:
    - the hiding places (bathtub, the phone, the guest bed)
    - the fuse box in the back hall
    - the radio on the dining table
    - throws coming from above the stove
    - **new:** the guest bed stands against a wall.
- **Two real problems found and fixed while proving it:**
  - **The guest bed was in the middle of the room, in the way.** My builder looked for "the far wall" in a direction where the room has an opening, so it fell back to the centre. The new design test went red ("the bed touches a wall: was False"), and the "every room reachable" test failed for the Guest Bedroom. The bed now has its headboard against the east wall, straight ahead as you come in the west door, with the tea at the foot nearest you (`DESIGN_LOGIC.md` #47, updated). Both tests are green.
  - **The evidence recorder filmed the desktop,** so another window in front of the game ended up in the first take. **The game now records its own frames** (`-record`). The video can only ever contain the game, and recording no longer takes your focus or your mouse.
- **Packaged build** `builds/slice_4/FedUp.exe`, run hands-free, 0 exceptions:
  `AUTOPILOT SNACK got=True juice=True` · `AUTOPILOT HUNT placed=2 carrying=1` · `AUTOPILOT POWER cut=True` · `AUTOPILOT DODGE hits=0 step=StoveOff` · `AUTOPILOT DONE gate4 … tea=Done appliances=3/3 allDone=True`
- **Smoothness** (a plain run, not recording): average frame **2.6 ms**, worst **8.8 ms**. The first build had one **86 ms** hitch, the same cause as Gate 3's: the new sounds (kettle, fire, lo-fi…) were being generated mid-scene. They're now made at load too.
- **Video:** `evidence/slice_4.mp4`, 59 s, cut from 123 s of the real build in nine pieces:
  1. fridge and juice box
  2. radio
  3. gather and vanish
  4. power cut
  5. fuse
  6. the kettle's boss voice and the cabinets
  7. stove off and the microwave
  8. blender
  9. the overflowing sink and the closing line

  There's no sound in the video (as before); play the build for that.

### Playtest checklist (10 minutes)
1. **Toast and bath first:** miss the toast on purpose once, then hit it. **Is the crooked lurch readable, or does it feel unfair?** Lie in the bath for a while. **Do the repeated snatches feel like a ghost toying with you, or like a chore?**
2. **The tea hunt:** find the milk, sugar and tea without hints, then walk back into the kitchen. **Did the power cut make you run, and did 30 s feel tight but fair?**
3. **The dodge and the appliances:** **does the kettle's boss voice get a laugh, and then do the cabinets make you stop laughing?**

**Feel question:** *"Does the night feel like it's escalating, and do the calm moments land?"*

### Designer's answer: *awaiting your one sentence*

### Three questions for you (from the research)
1. **When does real sound arrive?**
   - **A (I recommend this):** a small scare-sound pass now, about 15 sounds (footsteps, slam, sting, drone, fridge, kettle…). Every playtest after it tests the real fear.
   - **B:** keep the placeholder tones until art pass A2, as planned. It's faster now, but your feel answers about scares are weaker until then.
2. **A sensitivity reader for the Burial ending** before A4? I recommend yes.
3. **The corrupted home-video idea:** in Phase 3, the kid on the tape looks at the camera a beat too long. Should I add it to the bible's proposals (Part 2)?

### Known limits (not bugs)
- **All sounds are still placeholder tones,** including the kettle's "voice", which is a subtitle over a sting. (See question 1.)
- **The ghost hand, the thrown junk and the fire are grey boxes and glow.** They get real looks in the art passes.
- **The appliances only switch on once each, in a fixed order** (microwave, blender, sink), as the GDD lists them. Gate 5 can let the director pick.
- **The tea station is a thin pad on the counter.** Aim at its top; aiming at the front edge hits the counter. It becomes a proper tray in A1.

## Slice 3 (Greybox Gate 3) — 23 Oct 2026

**Question for you:** *"Does the first scare land?"*

### What changed
- **He falls asleep on the couch.** After the cat, the objective is *"Lie down on the couch"*. Hold E: the room dims, the home video slows and warps, *"Five minutes. Just five."* The screen goes black: **3:07 AM**.
- **He wakes in the "sick" house (Phase 2).** It's the same rooms, but:
  - **Every light is off except three:** the TV (now static, all by itself), a weak bulb at the far end of the hallway, and a weak bulb in the downstairs bathroom. **Those three draw the path.** A switch still works, but only gives a weak glow.
  - **Thicker, colder fog and a colder, greyer picture.**
  - **A low drone and faint whispers** under everything.
  - ***"…Cold. Why is it so cold."***
- **Phone torch (F):** a cone of light from his phone. It cuts out for a moment on its own now and then. Phase 2 only.
- **The objective at 3 AM:** *"Splash water on your face"* in the downstairs bathroom. *(My proposal, see below.)* It takes you down the long hallway to a mirror.
- **Three scares, in the order you meet them:**
  1. **Footsteps in the hallway.** Walk towards the bathroom and a step lands behind you for every step you take, a few metres back. Stop, and it takes one more. Turn around: nothing. *"…Hello?"*
  2. **The mirror.**
     - The mirror over the sink faces the doorway, the only way in.
     - After you splash your face, your reflection starts to follow you a beat late.
     - Then, for a split second, **something pale stands right behind your shoulder**.
     - Then the mirror **cracks, silently**. *"No. No, no, no."*
  3. **The door.** As you walk away down the hallway, **the bathroom door slams shut behind you**. *"I'm not going back in there."* Go back anyway, and it **creaks open by itself, purple fog seeping out**.
- **Back in the living room:** *"…I'm not going back to sleep."* That sets up Gate 4 (snack, juice box, radio, phone #2).
- **The ghost director** decides when each scare may happen:
  - **Every scare fires once.**
  - **None during the first 6 s after you wake**, so the change itself can land.
  - **After a scare the house goes quiet:** 10 s when you're calm, 5 s when you're fed up, ×1.3 after a big scare.
  - If you rush, a scare waits for its moment rather than piling up.

### What I decided and why
- **Every placement has its reason** in `DESIGN_LOGIC.md` #30–39.
  - **The idea:** Phase 2 is the sick version of the same house (from the Cry of Fear study). Same rooms, most of the light gone, fog and sound added, and the scares sit along a route you already know.
  - **Two placements changed after I saw them in the engine:**
    - **The figure** started in the doorway. It was too small and far to read in a split second, so it now stands right behind you.
    - **The director's quiet** started at 14 s / 7 s / ×1.5. That left 21 s after the mirror, so you'd be back in the living room before the door could slam behind you. Now 10 s / 5 s / ×1.3.
- **"Splash water on your face" is my proposal, not the GDD.** At 3 AM he needs a reason to walk somewhere, and this one leads past all three scares. Easy to veto.
- **Scares cost no frustration.** Your table has no Phase 2 scare values (only the final-sleep jumpscare, +15). They're atmosphere until you say otherwise.
- **The mirror shows a pale figure, not a face.** The GDD's "distorted smile or missing eyes" needs a real face model; that comes in the art pass.
- **The coat hook was hard to aim at.** It was an 8 cm target, and a Gate 1 test kept missing it now and then. The hook and the jacket under it are now one easy target. That's a real fix for players, not just the test.
- **Phase 2 darkness, tuned once.** The first recording was nearly black looking back down the hallway. I eased the colour grade and widened the torch beam. It's still the darkest the game has been.

### Evidence
- **Director rules, test-first:**
  - Red: `113 tests, 101 passed, 12 failed` (e.g. "Expected: Grace, But was: Quiet").
  - Green: **`113 passed, 0 failed`**.
- **Play Mode: 50 passed, 0 failed** (10 new).
  - Lying down → Phase 2: thicker fog, only 2 room lights left on, drone playing, torch available, the TV switched to static by itself.
  - The torch only works from the wake-up.
  - The hallway is silent in the evening.
  - Footsteps follow you, then stop the moment you turn.
  - The mirror lags, shows the figure, then cracks.
  - The door slams once, reopens with fog, and never slams again.
  - **The director keeps the quiet:** the mirror can't fire until the quiet after the footsteps is over (checked to the second).
  - The gate ends in the living room.
  - Two design-reason checks: **the mirror faces the only way in**, and **the lights left on are exactly the path ones**.
  - Two older tests updated for the new order: after the cat comes *"Lie down on the couch"*, not the end of the night.
  - **Two flaky failures fixed at the cause:**
    - **The Gate 1 jacket test:** the coat hook was an 8 cm target (now the hook and the jacket together).
    - **The Gate 2a clean-up test:** its helper sometimes teleported you *inside the couch* to look at the takeout box. It now picks a free spot with a clear view; the checks themselves are unchanged.
  - **A real game bug fixed too:** a flown-back clutter item could land half under the couch. It now always lands on open floor.
- **Packaged build** `builds/slice_3/FedUp.exe`, run hands-free, 0 exceptions:
  `AUTOPILOT STEPS played=6 stoppedByTurning=True` · `AUTOPILOT MIRROR figure=True cracked=True` · `AUTOPILOT DOOR state=Reopened slams=1` · `AUTOPILOT DONE gate3 phase=2 … allDone=True`
- **Director log from the same run:** footsteps at 23.8 s → mirror at 36.1 s (held back 2.3 s for the quiet) → door at 49.1 s (the moment the quiet ended).
- **Smoothness:** average frame 2.5 ms. The first recording had one 98 ms hitch when the house turned (the night's sounds were being generated on the spot). They're now made at load, and the worst frame is **14 ms**.
- **Video:** `evidence/slice_3.mp4`, 59 s from the real build. I cut about 12 s of walking and the splash screen. It starts at the end of the evening (Gates 1–2b have their own videos).

### Playtest checklist (5 minutes)
1. Fall asleep and wake up. **Can you tell at once that the house has changed, and is it too dark to find your way?**
2. Walk the hallway to the bathroom. When the footsteps start, keep walking a few steps, then turn around. **Did your stomach drop?**
3. At the mirror, sway your head a little after splashing your face. **Did you catch the figure, and did the silent crack land harder than a sound would have?**

**Feel question:** *"Does the first scare land?"*

### Designer's answer: *awaiting your one sentence* (your Gate 3 notes, the toast and the bath, are under Slice 4)

### Known limits (not bugs)
- **All sounds are placeholder tones**, footsteps and slam included. Real sound design comes in pass A2, and scares lean on sound more than anything.
- **The figure and your reflection are capsules.** Faces and bodies come in the art passes.
- **The GDD's other scares** (TV turning on by itself, the drawer gun, dolls, photos, red eyes, truck/train) come in Gates 4–5 on top of this director.

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

### Designer's answer (23 Oct 2026): "The apartment feels alive without being hostile."

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
- *(Retired 23 Oct: the designer's floor plans make this room the Bathroom Downstairs; the Storeroom is upstairs.)* **The store room** (the GDD's "active store room") is the room east of the kitchen. In the cleaned house its doorway is open; in the old scene something in front of it blocked the way.
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
