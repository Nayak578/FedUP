# Fed Up — Project Rules (read me first)

*Written 22 Oct 2026 from the approved `MACRO_PLAN.md`. Plain English on purpose: the designer must be able to read and check this file. **Change it only when the designer approves a change.***

## Who decides what

- **The designer** owns feel, pacing, visual direction, scope and whether it's fun. The designer doesn't read code and doesn't debug.
- **Claude** owns everything technical (code, Unity setup, art pipeline, audio integration, optimisation, builds, tests, tools). Make technical calls yourself, state each in one sentence, and explain the consequence in designer terms. **Never ask the designer a technical question.**
- **When the designer must choose:** give 2–3 plain-English options with the visible consequence of each, plus your recommendation.

## The pitch

You play a tired, underpaid office worker coming home to a suspiciously big apartment full of second-hand, faulty appliances. One evening of chores becomes a night of haunting, absurdity and psychological horror. Your frustration, your choices and one cat decide how the night ends. Look: **Cry of Fear + P.T.** Grounded, low-poly, VHS grain, desaturated, yellowed light, purple fog as the supernatural signal.

## Story canon (highest authority)

**`STORY_BIBLE.md` is the designer's "holy bible".** Part 1 is canon and beats the GDD and every plan. Part 2 holds proposals, which aren't canon until the designer approves them. **Read it before any story, ending, cutscene, dialogue or environment work.** Key points:
- **He's a burnt-out corporate worker nearing 30** (dreams lost, AI-era despair, addictions, estranged family, unread messages, guilt, passive death wish). He lives in a cheap house by polluting factories.
- **The phone calls are from the boss** about an important office file he brought home by mistake (found while unpacking the bag).
- **Main entrance: the west porch.**
- **Showdown:** a gun in the dark; the last shot kills **the boss at the gate**; the house is wrecked and **the ghost survives**. If the phone was answered, the ghost visibly turns into the boss first. Either way, the body at the gate is the real boss, dead; the phone only changes the staging (designer ruling, 26 Oct).
- **Burial:** mantra → the ghost flees to the upper floor's NE wall → fake wall + axe → squeeze through the 0.6 m gap between the east walls → the bottom-right triangle shaft → 90° vent portal → other world → burn the ghost's body (a depressed man who died by suicide in this house and wanted a friend) → promise to live.
- **Tone:** absurd + deep + scary, never too self-serious; cutscenes like Cry of Fear, Max Payne and Silent Hill (some absurd); inner monologue like Wolfenstein: The New Order; a touch of Mr. Robot.

## The GDD is law (below the story bible)

`D:\Fed Up GDD.pdf` is the design source. Where it contradicts itself, `MACRO_PLAN.md` section A records the designer's ruling. Highlights:

- **Frustration values:** the first-listed values (`MACRO_PLAN.md` Appendix 1). All live in one data file.
- **Frustration bands:** Calm 0–29 · Uneasy 30–59 · Agitated 60–89 · Breaking 90–99 · **Fed Up = 100**. The bar stops at 100, but the hidden value keeps counting (max 200). Per-task gates stay as written in the GDD (toast jams above 30%, flybacks above 40%, and so on).
- **Phases:** Phase 1 Evening Routine (low haunting) → sleep → Phase 2 Wake-Up (darker, fog, whispers) → Phase 3 Haunted Night Tasks → Final Sleep.
- **Finale:** see the story canon above (the gun showdown and the burial route). The comedic appliance boss is not used.
- **Take a Bath is a Phase 1 task.**

## The ending flags

Only these decide the ending (`MACRO_PLAN.md` Appendix 2, decoded from the designer's notes; confirmed by the designer, 26 Oct):

1. **Cat petted** (yes/no) — **mandatory-cat rule:** the cat appears 2–3 times and must be petted at least once. If not, you die in your sleep, whatever else happened.
2. **Phone answered** (yes/no) = **you answered at least 3 of the 4 calls** (designer ruling, 26 Oct: rewards attention, forgives one miss). **The phone decides your job.**
3. **Final choice:** stay asleep / wake at 100% / wake below 100%. **How you woke decides your home and the ghost.**
4. **Frustration at the final sleep** (feeds choice 3).
5. **Job outcome** and 6. **Apartment outcome** are results, not inputs.

**Endings beyond the table:** see `STORY_BIBLE.md` for the Showdown and Burial sequences.

**Secret ending:** going to bed before starting any task. The cat rule doesn't apply (the cat hasn't appeared yet). The result is random: loop or ghost wins.

## How we build

- **Every placement and decision has a written design reason** (designer rule, `STORY_BIBLE.md` "Design logic"). Write the reason in `DESIGN_LOGIC.md` before building the thing: the route, the sightline, what it says about him, what it sets up. Never place by "nearest free floor".
- **Greybox-first.** All gameplay is built and played in `_Project/Scenes/Greybox.unity`: the real house walls in grey, `GB_*` boxes where furniture goes, `ROOM_*` markers, plus an atmosphere kit (dark, fog, grain, placeholder sound). Art passes A1–A6 come after greybox gate 5b.
- **Gate order:**
  - **0:** walk + light switch
  - **1:** Drop Your Things + Make Toast + frustration bar
  - **2a:** Clean Up + Bath + Phone #1
  - **2b:** TV + Cat #1
  - **3:** sleep → Phase 2 + 2–3 jumpscares + ghost director
  - **4:** Tea + appliances + dodge QTE + Snack + juice box + radio
  - **5a — framework + two endings:** Final Sleep, the EndingResolver with all four flags wired, Phone #2–4, Cat #2–3, the secret ending, one Showdown variant, and the gun cameo (the setup ships with its payoff). Feel question: *"Does the ending choice feel earned?"*
  - **5b — the rest + the setup content:** the other six endings, the Storeroom blackout sequence, Mom's voicemail, the Mr. Robot lines, and the final content pass on all ending cards. If 5b slips, the game still has two endings on a working framework.
  - Then art passes:
    - **A1** furniture/doors/performance
    - **A2** lighting/sound look gate. Real sound is gated on **six sounds only**: footsteps, slam, sting, drone, fridge hum, kettle. Everything else stays placeholder.
    - **A3** Showdown
    - **A4** Burial dream. **A sensitivity reader reviews the Burial before A4 starts** (designer ruling, 26 Oct).
    - **A5** polish/voice/menu
    - **A6** cutscenes. Until then, placeholder captions. The Storeroom blackout is *not* a cutscene: it stays in-engine and first-person.
- **Every gate ends with, in order:**
  1. a packaged Windows build in `builds/slice_N/`
  2. a 30–60 s video in `evidence/slice_N.mp4`
  3. a `DEVLOG.md` entry in plain English
  4. a 3-item playtest checklist
  5. one feel question
  6. a git commit tagged `slice_N`
- **Editor-only success doesn't count.**
- **Proof, not claims.** Every "done" is backed by pasted evidence: Unity MCP query output, console log, test results, a Play Mode screenshot, or the build launch log.
- **Rules logic is test-first** (frustration math, ending resolver, QTE timing, director cooldowns): write the failing test, paste red, implement, paste green.
- **Test guard:** `.claude/hooks/test_guard.py` blocks rewriting or deleting existing tests without a game-code change, including via Bash, PowerShell or Unity MCP tools. Don't disable or work around it.
- **Unity MCP** (CoplayDev "MCP for Unity") is how the Editor is checked. If it's not reachable, stop and tell the designer how to fix it. Don't guess about the project.
- **The feedback loop closes in the DEVLOG.** Under each gate's feel question, record `### Designer's answer (date): [one sentence]` in the designer's words. Never invent or paraphrase an answer; leave it marked as awaited until the designer gives it.

### Writing rules (designer, 26 Oct 2026)

- **Writing rule 1** (`HORROR_RESEARCH.md` rule 9): every monologue line is either **flat about the ghost** (under-reacting to the impossible) or **raw about his life** (over-reacting to the trivial). If a line does neither, cut it. Each line in the monologue data is tagged with the rule it plays. Mr. Robot lines (below) are the one exception and carry their own tag.
- **Writing rule 2 (show, don't tell):** foreshadowing is shadowed, half-heard and deniable. Nothing is explained directly:
  - The mantra is fragments of an accidental butt-dial, not a clean voicemail.
  - The ghost's death room is dreamt, not visited.
  - The gun flashes past.

  If a player can point at a line and say "that's the foreshadowing", rewrite it.
- **Mr. Robot touch:** in Phase 1 and Phase 2, at least two monologue lines address the player as an imaginary friend, deniable enough to pass as him talking to himself. They set up the Burial payoff, *"I see you. I'm here."*
- **Never show the ghost's face or whole figure before the finale.** A glimpse lasts under 0.3 s or shows only part of it (`HORROR_RESEARCH.md` rule 3).

## Naming and folders

- **Our work lives in `FedUP/Assets/_Project/`.** Bought asset packs stay where they are.
- **Models:** `PROP_Name_v01.fbx`, `FURN_Name_v01.fbx`, `ENV_Name_v01.fbx`, interactable exports `int_object_variant.fbx`
- **Prefabs:** `PREF_Type_Name`. **Inside prefabs:** `OBJ_` root, `PRT_` moving part, `COL_` collider, `CTRL_` pivot.
- **Textures:** `Name_BaseColor/Normal/Roughness.png`. Props 512px, furniture 1024 (2048 for hero objects only).
- **Triangle budgets:** props 300–2,000 · furniture 1,000–5,000 · hero objects 5,000–10,000.
- **Greybox stand-ins:** `GB_Name`. **Room markers:** `ROOM_Name`.
- **Room names** come from the designer's labelled floor plans (`MACRO_PLAN.md` G.3) and are spelled the same everywhere: in docs, `ROOM_` markers and dialogue. For example, the **Storeroom** (one word) is upstairs beside the Locked Room, where the axe is. The room east of the kitchen is the **Bathroom Downstairs**, never "store room".
- **Data assets** live in `_Project/Data/`; all tunable numbers go there, not in code.

## Do not

- **Do not change** frustration math, values or thresholds, the ending flags or table, or the mandatory-cat rule without the designer's sign-off.
- **Do not add** tasks, scares or systems from a later gate into the current one.
- **Do not cut** a task, jumpscare, room, ending or phase without the designer's sign-off.
- **Do not change** feel or pacing (walk speed, timings, scare frequency) or the visual direction without sign-off. Offer it as an option.
- **Do not take on** anything that adds more than one session beyond the current gate without asking first.
- **Do not change** `STORY_BIBLE.md` Part 1. Add ideas to Part 2 as proposals.
- **Do not edit** `firstdraft.unity` or `MOdel/ver2.blend`. They're the reference originals.
- **Do not push** to GitHub or add asset packs to git without the designer's OK (Git LFS storage costs).
- **Do not weaken, skip or delete tests** to make something pass.
- **Do not claim done** without pasted evidence.
