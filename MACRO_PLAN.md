# Fed Up — Macro Plan (Phase 2)

*Drafted 22 Oct 2026. **Approved 22 Oct 2026 with one change: greybox-first** (sections B, E, F and G updated). `CLAUDE.md` is written from this version.*

## Summary

Fed Up will be **one apartment scene that changes over the night**, not a series of levels. Phase 1, the wake-up and Phase 3 happen in the same rooms; lighting, fog, sound and ghost activity shift around you. A single **frustration number** sits at the centre. Every task nudges it up or down, and everything else listens to it: the lights, the grain, the sounds, how often the ghost acts, and finally which ending you get. All the numbers (task penalties, thresholds, scare odds) live in **editable data files**, so you can tune the game by typing values, never code.

**We build the whole game in grey first.** A greybox scene uses the real house's walls in plain grey, with simple boxes standing in for furniture in the same spots. Every task, scare, phone call, cat visit and all 8 endings get built and played there, start to finish. Only then do we dress it: real furniture, lighting, sound and the two finales. A small **atmosphere kit** (darkness, fog, grain, placeholder sounds) is in the greybox from day one, because a scare in a bright grey room tells us nothing. The work still moves in **gated slices**. Each ends with a Windows build you play for five minutes, a video, and one feel question.

---

## A. Your answers → decisions (my proposals where you asked for help)

| # | Topic | Decision | What you'll notice |
|---|---|---|---|
| D1 | **Frustration values** (you asked for help) | **Use the first-listed values** (the narrative task pages, pages 2–6) as the base table. Events that appear *only* in the detailed task pages are added because they don't contradict anything (e.g. "fridge sarcastic line +3%"). Full table in **Appendix 1**. All values live in one data file. | After the Slice 1 playtest we tune from the session log the game records, not from guesses. |
| D2 | **Thresholds** (you asked for help) | **Four atmosphere bands from the first-listed 30/60/90 milestones:** *Calm* 0–29, *Uneasy* 30–59 (flicker, small object twitches), *Agitated* 60–89 (loud noises, aggressive movement, cat may hiss), *Breaking* 90–100 (red edge, heartbeat), with **100 = Fed Up**. Per-task gates in the GDD (toast jams above 30%, bin flybacks above 40%, and so on) stay exactly as written. | The house gets visibly worse in three clear steps, not a slow blur. |
| D3 | **Above 100%** | The **bar stops at 100**, but the hidden number keeps counting (up to 200). | ⚠️ A player at 140% must calm down 41 points to get back under 100. "Waking up calm" gets much harder once you've overshot. Say if you'd rather the hidden number also stop at 100. |
| D4 | **Ending table** (your own notes, pages 55–56) | Your raw table is consistent. The AI-written version on page 7 scrambled it. Decoded, **only three things decide the ending**: *Did you pet the cat?* *Did you answer the phone?* *Did you stay asleep, wake up at 100%, or wake up below 100%?* "Job" and "Apartment" aren't things the player sets. They're the *result*: **the phone decides your job, and how you woke decides your home and the ghost.** Table in **Appendix 2**. **Please confirm before Slice 5.** | 6 main endings + cat death + secret ending = 8. |
| D5 | **Secret ending** | Triggers if you go to bed **before starting any task**. The cat hasn't appeared yet, so the **cat rule doesn't apply** to this ending. The result is random: loop ending or "ghost wins". | — |
| D6 | **Finale** | **Superseded 22 Oct by `STORY_BIBLE.md`:** the Horror Showdown is a **gun** sequence in the dark, following an image of the ghost. The last shot kills **the boss at the gate**; the house is wrecked and **the ghost survives**. The Burial is a mantra chase → fake wall → axe → between-the-walls squeeze → vent portal → other world → burn the body. | — |
| D7 | **Bath** | A Phase 1 task. It goes into **Slice 2**. | — |
| D8 | **Tasks your slice list didn't place** | **Grab a Snack, juice box, radio** → Slice 4. **Phone calls #2–4 and cat visits #2–3** → Slice 5, because they feed the ending (cat rule, phone outcome). | Nothing from the GDD is dropped. |

---

## B. Scene and asset structure

- **Greybox scene first: `_Project/Scenes/Greybox.unity`.** It holds the cleaned house shell (`ENV_Apartment_v01.fbx`, see `HOUSE_CHECK.md`) in plain grey, labelled boxes (`GB_*`) at the exact spots of today's furniture, room markers (`ROOM_*`) and the atmosphere kit. All gameplay is built and tested here.
- **Art scene later: `Apartment.unity`** gets the same systems with real furniture swapped in for the boxes. Because the footprint is identical, swapping means replacing objects, not moving walls. Phase changes (evening → night) are lighting, fog, post-processing and sound blends inside the same scene, which keeps the "same home, getting wrong" feeling P.T. is known for.
- **Two small extra scenes later:** `MainMenu.unity`, and `Ending_Burial.unity` (the trapdoor maze and puzzle room, which is a different place).
- **`firstdraft.unity` stays untouched** as the art reference. Restore point: commit `a4cfe44`, local only and not pushed.
- **All new work goes under `Assets/_Project/`**, sorted to the top, following GDD naming:
  ```
  _Project/
    Art/Models/{Props, Furniture, Interactables}   PROP_*, FURN_*, int_*
    Art/{Materials, Textures, Shaders, VFX}
    Audio/{SFX, Ambience, Music, VO, Mixers}
    Prefabs/{Interactables, Environment, Systems, UI}   PREF_*
    Data/{Frustration, Tasks, GhostEvents, Endings, Channels, QTE}
    Scenes/   Timelines/
    Scripts/{Core, Player, Interaction, Frustration, Tasks, Ghost, Phone, Cat,
             Jumpscare, QTE, Endings, Atmosphere, UI, Debug}
    Tests/{EditMode, PlayMode}
  ```
  Bought asset packs stay where they are. Moving them risks breaking links for no player-visible gain.
- **House pipeline:**
  1. Export `ver2.blend` to FBX, so the project no longer needs Blender installed.
  2. Fix the one-sided roof.
  3. Swap or simplify the over-budget furniture (bed 363k → ≤5k triangles, chairs, stove, spoon stand). I'll use Blender's "Decimate" from the command line, or swap in a lighter model from the packs we already own.
  4. Set textures to GDD sizes.
  5. Turn each interactive object into a `PREF_` prefab (a reusable pre-built object) with simple box colliders.
  6. Bake "occlusion culling" once interior walls exist. Unity then skips drawing rooms you can't see, which matters more now that the house has walls.

---

## C. Systems map: what the player experiences

| System | One sentence, from the player's side |
|---|---|
| **FirstPersonController** | You walk at a tired pace through the house, with a slight head-bob and no jumping. |
| **InteractionSystem** | Look at something within arm's reach, a prompt appears, and you **press** or **hold** E to use it. |
| **FrustrationSystem** | Everything that annoys or calms you moves one meter, and the house reacts as it climbs. |
| **TaskStateMachine** | The game always knows which tasks are open, done or failed, and moves you from evening to night to the final sleep. |
| **GhostEventSystem** ("director") | The ghost acts more often as you get angrier, but it always backs off after a big moment so you can breathe. |
| **PhoneSystem** | The phone rings, sometimes from somewhere new, and whether you answer shapes your job. |
| **CatSystem** | The cat turns up 2–3 times in harder places, and you must pet it at least once or the night kills you. |
| **JumpscareSystem** | Walking into certain spots *may* trigger a scare, but only if the director allows it, so scares never pile up. |
| **QTESystem** | Short on-screen challenges (timing, mashing, direction prompts, dodging) use one consistent visual language. |
| **EndingResolver** | When you make the final choice, the game reads your cat, phone and sleep history and plays the matching ending. |
| **Atmosphere (lighting, post-processing, audio)** | Grain, fog, colour, light flicker, whispers and drones all deepen with frustration and with each phase. |
| **Data layer** | Every number above is a data file you can open and edit in Unity. |
| **Session log** (from GDD pp. 10–11) | Each playthrough writes a small spreadsheet of frustration over time, task results and scares fired, so we tune from facts. |

**Saving:** there's no mid-game save. The GDD's pacing curve puts a run at 11–13 minutes, and like P.T. you play it in one sitting. We only remember settings and which endings you've unlocked.

---

## D. Data flow: how frustration reaches everything

```
 Task / Phone / Cat / QTE result
        │ raises a "FrustrationEvent" (data file: id, +/- amount, caps)
        ▼
 ┌──────────────────┐   FrustrationChanged channel (display %, hidden value, band)
 │ FrustrationSystem│ ─────────────────────────────────────────────┐
 │ (pure rules,     │                                              │
 │  unit-tested)    │                                              │
 └──────────────────┘                                              │
        ┌──────────────┬──────────────┬──────────────┬─────────────┤
        ▼              ▼              ▼              ▼             ▼
   Frustration    Atmosphere     Audio mixer     Ghost director   Session log
   bar (UI)       (grain, fog,   (whisper/drone  (scare odds by   (CSV per run)
                  light flicker) layers)          band + cooldown)
                                                      │
 RunState data file (cat petted, calls answered, ─────┘
 sleep choice, final frustration)  ──────────────▶  EndingResolver
```

- Systems never talk to each other directly. They broadcast on **event channels**, small shared data files (see the research digest). Adding the cat or a new scare never means rewiring the toaster.
- The **rules** (frustration math, band changes, ending table, QTE timing windows, director cooldowns) are written as plain logic with **automated tests**, red → green, with evidence pasted.

---

## E. Build order: greybox gates, then art passes

**Effort unit:** one **session** = one sitting where I work toward the gate (a few hours of my work; your part is ~5–10 minutes of playing at the end). The research says solo projects run **2–3× over estimates**, so every number below is a range, and the top of the range is the honest one.

### Greybox gates (everything playable, in grey)

| Gate | Decision question (you answer by playing) | Contents | Observable exit criteria | Effort |
|---|---|---|---|---|
| **0** | *"Does moving through the apartment feel right?"* | Tooling (test guard, tests, build script, video capture) · greybox scene · atmosphere kit · walking · **one interactable: a light switch** | Windows build launches · you start inside the front door · an automated test confirms every room is reachable · the light switch works · zero console errors · ≥60 fps · 30–60 s video | **2–4** |
| **1** | *"Is the toast loop annoying-but-fair within 90 seconds?"* | Frustration system + bar · **inner-monologue subtitles** · placeholder cutscene player · **Drop Your Things** (incl. finding the boss's file) · **Make Toast** (timing QTE, jam + mash, burn, launch, smoke alarm) · session log | Every toast outcome reachable · bar moves by exactly the Appendix 1 values (tested) · alarm adds +3%/s until silenced · log written | **3–5** |
| **2a** | *"Does the apartment feel alive without being hostile yet?"* (part 1) | **Clean Up** · **Bath** · **Phone #1** | Each task can be completed and failed · answer/ignore logged | **3–4** |
| **2b** | Same question, part 2 | **TV** · **Cat #1** (a capsule that walks, sits and can be petted) | Cat can be petted, or walks off into the hallway · TV channels and remote loss work | **3–4** |
| **3** | *"Does the first scare land?"* | Sleep → **Phase 2 wake-up** · ghost director v1 · **2–3 jumpscares** (mirror flicker, door slam, hallway footsteps) · atmosphere kit gets darker per phase | Phase change is visible and audible · each scare fires once, only when the director allows · cooldown proven by test | **3–5** |
| **4** | *"Does chaos feel absurd, not punishing?"* | **Make Tea** · **appliance chaos** + flashlight QTE · **dodge QTE** · **Grab a Snack** · juice box · radio | All fail and recover paths reachable · "1 calm task per 2 stressful" holds in the log | **4–6** |
| **5a** | *"Does the ending choice feel earned?"* | **Final Sleep** · **EndingResolver** with all four flags wired · **Phone #2–4** · **Cat #2–3** · secret ending · **one Showdown variant** · the gun cameo | Resolver passes a test for every row of Appendix 2 · the night plays from start to both built endings | **2–3** |
| **5b** | *(set when 5a is answered)* | the other six endings · the Storeroom blackout · Mom's voicemail · the Mr. Robot lines · the content pass on every ending card | **the whole night is playable from start to every ending** | **3–4** |
| | | *Gate 5 split by the designer, 26 Oct: if 5b slips, the game still has two endings on a working framework.* | | |

### Art passes (dress the proven game)

| Pass | Contents | Effort |
|---|---|---|
| **A1** | Real furniture and props replace the boxes · doors · the cleaned house's real materials · performance pass (bed, chairs, stove) | **3–5** |
| **A2** | Lighting rebake per phase · grain, fog, purple hallway · first real sounds and ambience · first look-and-feel gate in the real house | **3–5** |
| **A3** | Horror Showdown: gun in the dark, follow the ghost image, boss-at-the-gate reveal (phone-dependent transformation) | **3–5** |
| **A4** | Burial: mantra chase, fake wall + axe, squeeze between the walls, 90° vent portal, other world with the ghost's life fragments, burning the body | **5–8** |
| **A5** | Remaining scares, photo swap, eyes in the dark, environmental-storytelling props, voice/SFX, menu, settings, final optimisation | **3–5** |
| **A6** | **Cutscenes** (in-engine cinematic, Max Payne-style comic panels, absurd mini-scenes) replacing the greybox placeholders | **5–8** |

---

## F. How long will this take?

| | Sessions | If we do ~4 sessions a week |
|---|---|---|
| Greybox gates 0–5 (whole night playable in grey, all 8 endings) | **22–34** | **~1.5–2 months** |
| Art passes A1–A6 (incl. cutscenes, story props) | **22–36** | **+1.5–2.5 months** |
| **Total** | **44–70** | **~3.5–4.5 months on paper, realistically 5–7** |

**Why "realistically 4–6":** the research's 2–3× rule, plus three things outside my control:

1. **Assets nobody has made yet.** The packs we own have no cat, ghost, toaster, phone, TV remote, radio, kettle or doll. In greybox these are boxes and capsules, so they no longer block anything; they become the risk for the art passes instead.
2. **Voice.** The GDD relies on sarcastic lines and phone voices. Greybox uses on-screen text; the recorded / generated / subtitles-only call comes at A5.
3. **Gate turnaround.** The calendar moves only as fast as you can play each build.

**Nothing gets cut without your sign-off.**

---

## G. Provisional layout calls (greybox makes these cheap to change; say if any is wrong)

1. **Front door = the west porch (designer ruling, 22 Oct).** The night starts on the porch; you come in through the front door into the living room.
2. **The east corridor doorway is a locked back door, and the 4.8 m north gap is walled up.** The player can't leave the house.
3. **Room names: the designer's labelled floor plans (23 Oct) rule** (`evidence/house/DESIGNER_room_labels_*.png`).
   - **Upstairs:** Bedroom, Work Room (west front), Balcony, Bathroom Upstairs (west), Bathroom Middle, Wardrobe, **Storeroom** (where the axe will be), **Locked Room** (stays locked and can't be entered), plus the Landing and Hallway.
   - **Downstairs:** Entrance, Living Room, Kitchen, **Bathroom Downstairs** (the room east of the kitchen, where you take the bath), Guest Bedroom, Garage (its big shutter stays closed), Back Hall.
4. *(Replaced by 3.)* The old guesses (Bedroom 1/2, Bathroom 1/2, a downstairs Store Room) are retired.
5. **Walking:** no jump, tired walk at ~2.5 m/s, sprint at ~4 m/s.
6. **Brightness:** Slice 0 was too dark (designer feedback). From the next build: more ambient light and stronger room lights, still dark and moody.
7. **The full night as a story, every branch:** see [`STORY_FLOW.md`](STORY_FLOW.md).

The Slice 0 build plan is in [`SLICE_0_PLAN.md`](SLICE_0_PLAN.md).

---

## Appendix 1 — Frustration table (first-listed values; all editable)

| Task | Event | Value |
|---|---|---|
| Drop Your Things | — | none (tone only) |
| Make Toast | Burnt toast / failed catch | +10 |
| | Smoke alarm left ringing | +3 per second |
| | Success | −5 |
| | *Added from task page:* ghost eats the toast (not picked up within 15 s) | +10 |
| Take a Bath | Each lost clothing item | +6 |
| | Success | −10 |
| Clean Up | Failed placement / flyback | +6 |
| | Ghost giggle | +2 |
| | Room stays clean 30 s | −10 |
| Watch TV | Static / horror channel | +7 |
| | Infomercial | +5 |
| | Lost remote | +10 |
| | Calm channel | −10 |
| | *Rebalance rule (applied in GDD):* TV can add at most +10 in total | cap |
| | *Added from task page:* remote stolen, tug-of-war failed | +5 |
| Phone #1 | Ignored | +2 |
| | Answered | −5 |
| Cat (any visit) | Petted | −20 |
| | Ignored / missed | 0 |
| | Petted on retry after a hiss | −10 |
| Make Tea | Each power-cut reset | +10 |
| | Hit during dodge | +7 per hit |
| | Fire ignites | +15 |
| | Tea made | −10 |
| Grab a Snack | Failed grab | +5 |
| | QTE fail | +10 |
| | Success | −5 |
| | *Added from task page:* fridge sarcastic line | +3 |
| Phone #2–4 | Each ring after the first | +2 |
| | Distorted voice line | +5 |
| | Found and answered quickly | −5 |
| | Long ring ignored (mid-task calls) | +1 per second |
| Appliance chaos | Not shut off in time | +10 |
| | Ghost blocks the control | +7 |
| | Flashlight scare success | −5 |
| Final Sleep | Jumpscare, failed to respond | +15 |
| | Sleep success | −20 |
| | Retry (capped at one +10, per rebalance) | +10 once |
| Calm extras | Juice box | −5 |
| | Radio lo-fi uninterrupted 15 s | −5 |

## Appendix 2 — Endings (updated 22 Oct from `STORY_BIBLE.md`; 🟡 = still to confirm)

**Inputs:** Cat petted (yes/no) · Phone answered (yes/no) · Final choice: *stay asleep* / *wake at 100%* / *wake below 100%*

| Cat | Phone | Final choice | Ending | Job | Home | Ghost |
|---|---|---|---|---|---|---|
| **No** | any | any | **You die in your sleep** (ghost consumes you) | — | — | alive |
| Yes | Answered | Stay asleep | **Loop of daily routine** | safe | safe | alive |
| Yes | Answered | Wake at 100% | **Horror Showdown: the ghost visibly turns into the boss; you shoot him at the gate** | (boss dead) | destroyed | **alive** |
| Yes | Answered | Wake below 100% | **Ghost Burial: burn the body in the other world; promise to live** | safe 🟡 | safe | at peace |
| Yes | Ignored | Stay asleep | **Unemployed, ghost angry** | gone | safe | alive |
| Yes | Ignored | Wake at 100% | **Horror Showdown: no transformation; you still shoot the boss at the gate** | (boss dead) | destroyed | **alive** |
| Yes | Ignored | Wake below 100% | **Ghost Burial (same journey)** | gone 🟡 | safe | at peace |
| — | — | Bed before any task | **Secret: "Maybe ignoring it all was the real answer."** (random: loop or ghost wins) | — | — | — |

**Ruled (designer, 26 Oct): Option B, at least 3 of 4.** There are four calls, so what counts as "answered the phone"? *(Options kept for the record.)*
- **Option A — the last call only.** Simple and dramatic.
- **Option B — at least 3 of 4.** Rewards attention and forgives one miss. **← I recommend B.**
- **Option C — all four.** Harsh.
