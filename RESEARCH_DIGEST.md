# Fed Up — Research Digest (Phase 1)

*How small teams actually build first-person psychological horror in Unity, and what that means for Fed Up. Researched 22 Oct 2026. Every claim has a link. Where a reference from the brief couldn't be found or verified, I say so plainly.*

---

## 1. Indie workflow and vertical slices

**What the sources say**
- **Unity Learn's "Why Agile?" lesson** teaches sprints for solo and small teams. Its milestone *is* a vertical slice, and it points solo devs to the "Scrum of One" article. A "sprint" is a short fixed block of work with a goal and a review at the end.
  → https://learn.unity.com/course/agile-development/tutorial/why-agile
  → https://www.kodeco.com/585-scrum-of-one-how-to-bring-scrum-into-your-one-person-operation
  → https://lucid.co/blog/scrum-for-one (timeboxing when you're your own Scrum master)
- **Slice size:** a polished slice (one level, a working core mechanic, basic UI and audio) is about **1–3 months of part-time work**. A playable loop with placeholder art is **1–4 weeks**.
  → https://www.strayspark.studio/blog/how-long-does-it-take-to-make-an-indie-game
- **Write a one-line "slice statement" plus an explicit list of non-goals.** Anything that doesn't answer the slice's question goes to a parking lot, not into the scene.
  → https://www.wayline.io/blog/game-planning-tools-vertical-slice
  → https://www.wayline.io/blog/vertical-slice-setup-problems-fixes
- **Cut scope, not quality.** A low-quality slice can't prove the real game is buildable. Solo devs **underestimate timelines 2–3×**, and slices balloon because every feature looks small on its own.
  → https://cdarklock.substack.com/p/the-vertical-slice
  → https://www.wayline.io/blog/vertical-slices-kill-indie-games-2024
- **Solo playtesting trick:** step away a few days, then do one "silent walkthrough" in a single sitting and log every hesitation.
  → https://tessl.io/registry/skills/github/Donchitos/Claude-Code-Game-Studios/vertical-slice

**What this means for Fed Up**
- Your slice list (0–6) is a good shape. One nuance from the research: **Slices 0–2 are really milestones, not vertical slices.** Each answers one question, which is right. But the first *true* vertical slice by industry standards (start-to-end, representative look and sound) is **Slices 0–3 together**: arrive home, do Phase 1, sleep, first scare. I'll still gate each slice with a build, but I'll make sure the **look (grain, fog, lighting) and the first sounds arrive by Slice 3**, not all in Slice 6. Otherwise we'd be judging "does the scare land?" in a silent, untextured house.
- **"Prove the build path first"** is already in your brief. Slice 0 will produce a packaged Windows build on day one, before any polish.
- Expect the 2–3× underestimate. I'll give every slice estimate as a range.

---

## 2. Reference implementations

| Reference | What it actually is (verified) | Use it how |
|---|---|---|
| **stevenharmongames / Unity-FirstPersonInteractionToolkit** https://github.com/stevenharmongames/Unity-FirstPersonInteractionToolkit | Unity 2022.3, **MPL-2.0 licence**, 75★. Dynamic crosshair, popup text, highlighting, pickup/inspect/throw, physics doors (open/closed/locked), drawers that hold objects, light switch, read-note UI, height-safe teleport triggers. The README doesn't say which input system or render pipeline it uses. | **Borrow patterns, don't adopt the files.** MPL-2.0 means any of its files we modify must stay open-source. Its features map almost 1:1 onto ours (doors for "door slam", drawers for "object swap", light switch, notes), so I'll study its structure and write our own, built on the new Input System and our frustration events. |
| **MIBAHNAF / Hallucinations** https://github.com/MIBAHNAF/Hallucinations | URP psychological horror, apartment prologue then dream level. Scripted moments run on **Timeline + Signals**. Small helper scripts: `SceneLoaderSignal.cs`, `TriggerActivate.cs`, `TriggerDeactivate.cs`, `ActivateAfterDelay.cs`. **Its player and AI come from a paid asset (ThunderWire UHFPS). No licence file.** | **Read only, for structure.** The useful lesson is *how* it's built: small single-purpose trigger scripts plus Timeline for anything cinematic. That's the pattern for our jumpscares and the Phase 1 → Phase 2 wake-up. |
| **rifts-project-graveyard / dbd-quick-time-event** https://github.com/rifts-project-graveyard/dbd-quick-time-event | MIT licence. Dead-by-Daylight-style skill check: a circle with a rotating cursor and a target zone. Includes a demo scene and prefab. | **Pattern for the timing QTE** (toast timing, sleep breathing rhythm). MIT allows copying, but I'll rewrite it so it runs on our input and test setup. |
| **CrissNamon / unityqte** https://github.com/CrissNamon/unityqte | `QTEManager` (runs events) + `QTEEvent` (settings per event). Keyboard and gamepad, multi-key, fail-on-wrong-key. **Archived Dec 2021, no visible licence**, tested on Unity 2018/2019. | **Design idea only:** "one manager + a settings asset per QTE". That lets each QTE (dodge, mash-to-unjam, flashlight scare, tug-of-war) be tuned without code. |
| **"ECHOES TO KNOWWHERE" Sanity Manager** | ⚠️ **I couldn't find this project anywhere**: not on GitHub, itch.io, or Unity forums, under any spelling I tried. I can't verify its classes. | The *pattern* the brief describes is sound and well documented elsewhere: data asset + level assets + manager with weighted random effect picking. **I'll build that pattern for the Frustration Meter** from Unity's own documentation (below), not from this reference. |

**Supporting architecture sources (verified):**
- **Unity's official ScriptableObject architecture guide and e-book.** "Event channels" are shared data files that systems broadcast on and listen to, like radio channels, so systems never need direct links to each other.
  → https://unity.com/how-to/scriptableobjects-event-channels-game-code
- **Unity Open Project #1 event-system wiki.** Same pattern, used to avoid "Singletons" (one global object everything depends on), because they make systems "harder to test and reuse".
  → https://github.com/UnityTechnologies/open-project-1/wiki/Event-system
- **Weighted random picking** (sum the weights, roll, walk the list):
  → https://discussions.unity.com/t/fast-weighted-random-selection-tutorial-code/950094

**In designer terms:** Every frustration value, threshold and ghost-event chance will live in **editable data files**, not code. You'll be able to tune "burnt toast +10%" or "door slam chance at 60% frustration" by typing a number, without me recompiling anything.

---

## 3. Jumpscare and horror-event patterns

- **Trigger volume + "only once" flag + Timeline.** An invisible box fires a Timeline sequence when you walk in, and a flag stops it re-firing. Timeline's start/stop callbacks freeze and restore player control during the moment.
  → https://www.wayline.io/blog/unity-timeline-triggered-cutscenes
  → https://learn.unity.com/tutorial/starting-timeline-through-a-c-script-2019-3
- **Simple scares don't need Timeline.** A flash plus a sound plus a short delay is enough for one-frame mirror faces or the TV figure.
  → https://discussions.unity.com/t/jumpscare-script/169408
- **The "AI director" with a tension gauge and cooldown (Alien: Isolation).** A hidden *menace gauge* rises with the threat's presence. When it crosses a threshold, the director **sends the threat "backstage" to give the player breathing room**. The design rule: tension spikes followed by real downtime, and the threat never cheats.
  → https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation
  → https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation
  → https://bloody-disgusting.com/editorials/3587886/alien-isolations-stalking-xenomorph-needs-horror-games/

**What this means for Fed Up:** The GDD already asks for this. "Pause ghost events if player stress is peaking too early" and "1 calming task every 2 high-stress tasks" *are* a director with cooldowns. So the **Ghost Event System will be a small director**: it reads frustration and phase, picks an allowed scare by weighted chance, then enforces a cooldown. Jumpscares are trigger volumes that *ask the director for permission* before firing, so two scares never stack and calm moments stay calm.

---

## 4. Testing and enforcement tooling

| Tool | Verified facts | Fits C#/Unity? |
|---|---|---|
| **TDD Guard** (nizos) https://github.com/nizos/tdd-guard | Blocks edits that skip red→green→refactor. Supports Vitest, Jest, Storybook, pytest, PHPUnit, Go, Rust, RSpec, Minitest. **The README now says it "grew into Probity"; new projects should start there.** | ❌ **No C#, no NUnit, no Unity.** |
| **agent-guard** (hahahahahahahahah6) https://github.com/hahahahahahahahah6/agent-guard | Python stdlib. Includes: **session-start/stop test-tampering diff** (blocks finishing if tests changed with no source change); **cross-tool Bash write guard** (parses `>`, heredoc, `sed -i`, `tee`, `cp`/`mv`); mutation checker; cheat-sniffer; tamper-shape checker. Admits gaps: `awk -i inplace`, `dd`, `python -c` writes aren't caught before the fact (the stop-time diff still catches them). | ⚠️ **Partly.** Test-file detection and mutation testing are **Python/JS-only**, and Windows isn't mentioned. The *ideas* transfer; the tool doesn't. |

**Decision: write our own small project-local hook**, ~150 lines of Python stdlib (Python 3.14 is installed). It copies agent-guard's two best ideas and adapts them to Unity's C# test folders:
1. **Before any Write/Edit/Bash:** if the change touches a test file (`Assets/_Project/Tests/**`), check that a matching game-code file changed earlier in the same session, or that the test is brand-new and has been recorded as failing (red). Bash commands are scanned for file-write targets (same list as agent-guard), so Bash can't sneak around the rule.
2. **At session end:** compare fingerprints of all test and code files against the start of the session. If tests changed while no game code did, the session can't be closed as "done".

**Honest limit:** no hook catches every trick (agent-guard's authors say the same). The end-of-session fingerprint check is the safety net, and I'll paste its output as evidence. **I'll set this up as the first step of Slice 0, before any game code.** It's tooling, not gameplay.

**Running tests:** through Unity MCP's test runner while the Editor is open, the same tool that produced this audit. Deterministic rules (frustration math, ending table, QTE timing windows) get "Edit Mode" tests, which run instantly without pressing Play. Movement and interaction get "Play Mode" tests.

---

## 5. Tooling decisions (made, not asked)

| Area | Decision | What it means for you |
|---|---|---|
| Unity version | **Stay on Unity 6 LTS (6000.0). Update the patch to ≥ 6000.0.58f2** before the first packaged build. Unity's Oct 2025 security advisory requires it for shipped builds. https://unity.com/security/sept-2025-01 | Same Editor look. Builds you share are safe to run. |
| Render pipeline | **URP 17** (per GDD). Remove the unused old Post Processing v2 package. | Grain, fog and colour grading all set from one place. |
| Input | **New Input System only.** Switch off the old one. | Rebinding keys and controller support come almost free later. |
| Camera | Keep **Cinemachine**. | Head-bob, shake on scares and the cat "screen soften" are just camera settings. |
| Data layer | **ScriptableObject data + event channels** (section 2). | You tune numbers in the Editor, no code. |
| Scripted scares | **Timeline + Signals** for cinematic moments, trigger volumes for simple ones. | Scares are edited on a visual timeline, like a video editor. |
| Cat movement | **AI Navigation** (already installed). | The cat can walk around furniture to reach its spots. |
| MCP | **Keep CoplayDev "MCP for Unity"**, already installed and verified. | — |
| Hooks | **Project-local Python hook** (section 4). | Rules about tests are enforced by the machine, not by my promises. |
| Evidence video | **Unity Recorder** for Play Mode captures (Editor only, MP4). **ffmpeg** (already installed) for recording the *packaged build* window. https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html | Each slice gets a real video of the real build, not just the Editor. |
| Builds | Windows 64-bit via Unity MCP's build tool into `builds/slice_N/`. That folder is already ignored by git, so big binaries stay out of the repo. | — |
| Version control | **Get the apartment and settings into git first** (they're untracked today). Asset packs stay local-only unless you approve the LFS cost. | Protects the house from a disk failure. |

---

## 6. Conflicts between the GDD and reality

**Between the GDD and the project as it exists:**
1. **The layout is missing Entrance, Hallway, 1 Bedroom, 1.5 Bathrooms.** Several GDD scares depend on them: hallway footsteps, door slam, mirror, bath clothes. This is the biggest scope question for planning.
2. **The GDD's art budgets (≤5k triangles per furniture piece, 512px props) are broken by bought assets.** That's fixable without re-modelling, by swapping, simplifying and changing import settings.
3. **The GDD SOP says to export FBX. The house is a `.blend`.** I'll convert it.
4. **The GDD wants yellowed single-source light, near-black ambient and purple fog.** The scene has teal/cyan light, sky ambient and no fog.
5. **The GDD SOP's sample code uses `OnMouseDown`** (click-on-object). For a first-person game I'm using **look-at-and-press** instead. Same feel for the player, and it works with controllers.

**Inside the GDD itself:** eight contradictions are listed in `PROJECT_AUDIT.md`, section 8 (differing frustration values, thresholds, cap at 100%, how Job/Apartment flags get set, mismatched outcome tables, secret ending vs. cat rule, two different finales, Bath task placement). **I won't resolve any of these myself. They're yours.**

**Between the brief and what I could verify:**
- **"ECHOES TO KNOWWHERE"** could not be found. I'm using Unity's documented pattern instead.
- **TDD Guard and agent-guard don't support C#.** Replaced with our own hook built on agent-guard's design.
- **The connected MCP is CoplayDev's, not Singtaa's or IvanMurzak's.** It works, so I'm keeping it.

---

## Recommended references (top 5, specific files and classes)

1. **Unity — ScriptableObject event channels guide** (https://unity.com/how-to/scriptableobjects-event-channels-game-code) and **Open Project #1 event system** (https://github.com/UnityTechnologies/open-project-1/wiki/Event-system). The pattern behind `VoidEventChannelSO` / `IntEventChannelSO` → our Frustration, Phase and Ending channels.
2. **Unity-FirstPersonInteractionToolkit.** Interaction, door, drawer and light-switch structure → our InteractionSystem (patterns only, MPL-2.0).
3. **dbd-quick-time-event (MIT)** + **unityqte's `QTEManager`/`QTEEvent` split** → our QTE system: one runner, one settings asset per QTE.
4. **Hallucinations: `TriggerActivate.cs`, `ActivateAfterDelay.cs`, `SceneLoaderSignal.cs`** → small trigger scripts + Timeline Signals for jumpscares and the Phase 1→2 wake-up.
5. **Alien: Isolation AI director** (Game Developer article) → GhostEventSystem as a director with a tension gauge and "backstage" cooldown.
