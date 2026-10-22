# Slice 0 (Greybox Gate 0): Micro Plan

**Decision question:** *"Does moving through the apartment feel right?"*
**Effort:** 2–4 sessions. **Status: built 22 Oct 2026, waiting for your playtest.** Evidence in `DEVLOG.md`.

## What you'll be able to do at the end

Launch `builds/slice_0/FedUp.exe`. You stand just inside the front door at night, in a grey version of the real house: same walls, same rooms, boxes where the furniture is. It's dark, with a little fog and film grain. You walk through the entrance, living room and kitchen, into the store room, up the stairs, along the hallway, and into both bedrooms and both bathrooms. You can't leave the house, fall through anything, or get stuck. By the front door there's a light switch: look at it, see "E — Light switch", press E, and the light clicks on and off.

## Steps, in order

| # | Step | Evidence I'll paste |
|---|---|---|
| 1 | **Test-guard hook** (`.claude/hooks/test_guard.py` + `.claude/settings.json`): blocks rewriting or deleting existing tests unless game code changed in the same session, scans Bash/PowerShell/Unity-tool writes for test-file targets, and does an end-of-session fingerprint check | Simulated blocked and allowed calls |
| 2 | **Project hygiene:** remove the unused Post Processing v2 package, create `_Project/` folders and code assemblies (separate code bundles for game, editor tools and tests) | Package list, clean console |
| 3 | **Code, test-first:** `HoldProgress` (press/hold timing) red → green; then player motor, interaction, light switch, prompt UI, autopilot, build script | Red output → green output |
| 4 | **Greybox scene** `_Project/Scenes/Greybox.unity`: cleaned house in grey, `GB_*` boxes at today's furniture spots (fridge kept clear of the store-room door), `ROOM_*` markers, closed front door, locked back door, north gap walled | Floor-plan image and screenshots |
| 5 | **Atmosphere kit:** near-black ambient, dim warm lights per room, dark purple fog, film grain, vignette, desaturation | Screenshots |
| 6 | **Tests:** Edit Mode (`HoldProgress`) and Play Mode (scene loads clean, spawn, walk speed, every room reachable, no sky overhead, light switch) | Test results |
| 7 | **Build path:** Windows build, launch with `-autopilot` (walks every room, flips the switch, quits), log check, ffmpeg recording of the real build window | Launch log + `evidence/slice_0.mp4` |
| 8 | **Gate package:** `DEVLOG.md`, checklist, feel question, git commit tagged `slice_0` | — |

## Decisions I made (technical, mine to make)

- **Own first-person controller instead of Unity's Starter Assets.** The Starter Assets code can't be linked to our separately compiled game code, and it drags in mobile joysticks and a camera rig we don't need. Ours is ~150 lines, reads every number from a data file, and is easy to test. *For you: the same WASD + mouse feel, but every speed and head-bob value is editable.*
- **Input: new Input System only in our code.** The project-wide "old + new" switch stays as it is for now, because flipping it needs an Editor restart and would break the team's old prototype scripts in `Main.unity`. It gets flipped when those prototypes are retired.
- **Unity patch update (6000.0.61f1, already installed) is deferred until before any build leaves this PC.** Slice 0 builds are only run here.
- **The hook goes live in the next Claude session.** Claude Code reads hook settings when a session starts; this session proves the script works with simulated calls.

## Exact files

```
.claude/settings.json, .claude/hooks/test_guard.py
FedUP/Assets/_Project/Scripts/FedUp.Runtime.asmdef
  Core/EventChannelSO.cs
  Player/PlayerTuningSO.cs, FirstPersonMotor.cs, SpawnPoint.cs
  Interaction/IInteractable.cs, InteractionMode.cs, HoldProgress.cs, Interactor.cs,
              InteractionSettingsSO.cs, InteractionFocusChannelSO.cs, LightSwitch.cs
  UI/InteractionPromptUI.cs
  World/RoomMarker.cs
  Debug/Autopilot.cs
FedUP/Assets/_Project/Scripts/Editor/FedUp.Editor.asmdef, SliceBuilder.cs, GreyboxBuilder.cs
FedUP/Assets/_Project/Input/FedUpControls.inputactions
FedUP/Assets/_Project/Data/{Player/PlayerTuning, Interaction/InteractionSettings, Channels/InteractionFocus}.asset
FedUP/Assets/_Project/Art/Materials/M_Greybox*.mat
FedUP/Assets/_Project/Settings/GreyboxVolume.asset
FedUP/Assets/_Project/Scenes/Greybox.unity
FedUP/Assets/_Project/Tests/EditMode/FedUp.Tests.EditMode.asmdef, HoldProgressTests.cs
FedUP/Assets/_Project/Tests/PlayMode/FedUp.Tests.PlayMode.asmdef, GreyboxSmokeTests.cs, LightSwitchTests.cs
tools/capture_build.ps1
DEVLOG.md
```

## Tests

**Edit Mode:** `HoldProgressTests`. Zero time means not complete; a full hold completes exactly once; letting go resets to zero; a huge time step doesn't overshoot 100%; a press (zero-second hold) completes immediately.

**Play Mode:**
- `GreyboxSmokeTests`:
  - the scene loads with zero errors
  - the player starts within 0.5 m of the spawn point, standing on the floor
  - holding forward for 2 s moves the player 3.5–5.5 m
  - every `ROOM_*` marker can be reached on foot from the spawn
  - every room marker has a ceiling overhead
  - the player can't reach any point outside the house
- `LightSwitchTests`:
  - looking at the switch from 1.5 m shows "Light switch"
  - pressing E toggles the light on, then off
  - from 3 m there's no prompt

**Build check:** launch `FedUp.exe -autopilot -logFile`. Pass means no `Exception` lines in the log, all rooms visited, the switch flipped, and average frame time ≤16.7 ms (60 fps).

## Gate deliverables

1. `builds/slice_0/FedUp.exe`
2. `evidence/slice_0.mp4`, 30–60 s of the autopilot in the real build
3. `DEVLOG.md` entry
4. Three-item playtest checklist
5. Feel question
6. Git commit tagged `slice_0`
