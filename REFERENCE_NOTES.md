# Reference Notes — what other horror games teach us

*Knowledge only. Nothing from these games (models, textures, sounds, animations, maps) goes into Fed Up. See "Rules" at the bottom.*

## Cry of Fear (read-only study of the installed game, 23 Oct 2026)

**Method:** I parsed the scripting data (entity list) inside all **235 maps** in `Cry of Fear\cryoffear\maps`. Nothing was copied or modified.

**How its scares are built, across the whole game:**

| Building block | Count | What it does |
|---|---|---|
| Invisible one-shot trigger zones | 1,152 | Step here once and something fires |
| Repeatable trigger zones | 367 | |
| Timed sequences ("multi_manager") | 1,432 | One trigger fires several things over a few seconds |
| Placed sounds | 2,360 | |
| Screen fades | 459 | |
| Screen shakes | 15 | Used very sparingly |
| Scripted creature animations | 7 | Almost none |

**What this tells us:**
- **Cry of Fear scares with sound, timing and fades, not choreography.** The typical scare is *walk into an invisible zone → a timed sequence plays a sound, a door, a fade*. That's exactly the trigger + director setup planned for gate 3, and it's cheap to build. **We'll copy the *method*, not the content.**
- **Light is mostly cones.** 3,874 spotlights vs 2,686 round lights: pools of light in darkness, not lit rooms. That's our "slightly brighter but moody" target.
- **The most-used sounds are ambience beds:** rain, drips, wind, tunnel hum, crickets, distant traffic lights. Background sound carries most of the dread.

**Its apartment has a normal version and a "sick" version of the same rooms.** That's exactly our Phase 1 → Phase 2 idea.

| Map | Trigger zones | Timed sequences | Sounds | Fades | Spotlights | Round lights | Creatures |
|---|---|---|---|---|---|---|---|
| `c_apartment1` (first visit) | 29 | 24 | 35 | 17 | 71 | 14 | 5 |
| `c_apartment3` | 13 | 12 | 14 | 4 | 25 | 7 | 6 |
| `c_apartment3sick` | 18 | 2 | 16 | 2 | 15 | **3** | 5 |
| `c_apartmentsick` | 5 | 9 | 17 | 10 | 53 | **2** | 1 |

**In practice:**
- The first visit is the densest in scripted moments; it teaches the space.
- The **"sick" versions cut most of the light** (14 round lights down to 2–3) and keep the trigger count similar.
- **Lesson for our night phase:** take light away, keep the scripted moments, add fog and sound.

## Max Payne and Silent Hill 2 (planned, when we reach those gates)

- **Max Payne:** comic-panel cutscene pacing (panels per scene, seconds per panel, voiceover rhythm) → informs the A6 comic-panel style. Measured from the original game, which has open, well-documented formats.
- **Silent Hill 2:** fog distance and density, radio-static warning, cutscene framing → informs the A2 look and gate 3 scares. Only from formats that aren't encrypted. If your copy is the 2024 remake, its files are locked, so I'd learn from play-throughs and screenshots instead.

## Rules for using other games

1. **Study, measure, learn: yes.** Structure, pacing, numbers, techniques.
2. **Copying their models, textures, sounds, animations or maps into Fed Up: no, not even as placeholders.** They're the studios' copyrighted work (Team Psykskallar, Remedy/Rockstar, Konami). One ripped sound in a build can get the game pulled from a store, and placeholders have a way of shipping.
3. **Never break copy protection or encryption** to look inside a game.
4. **Our own assets come from:** the asset packs already in the project, fal-generated originals (cat, ghost, voices, SFX) once the fal connection works, and Blender.
