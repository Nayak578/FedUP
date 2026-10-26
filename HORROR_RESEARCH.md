# Fed Up — Horror Research (what the papers say, and what it means for us)

*Compiled 24 Oct 2026, at the designer's request: research that helps the game feel immersive and frightening, and tell its story while being gloomy, gritty, absurd, and real and unreal at the same time.*

**How to read this:** each finding has a plain-English summary, how strong the evidence is, and **the rule it gives Fed Up**, with what we already do and what changes. Where a source is only a blog, a student thesis or press coverage, I say so. Several horror-game studies are small. Sources are at the end.

---

## The short version: 10 rules for Fed Up

1. **Make the familiar slightly wrong.** The house you learned in the evening is the house that turns on you. *(Freud's uncanny; P.T.)*
2. **Alternate "something there that shouldn't be" with "nothing there where something should be".** Footsteps with no one behind you; an empty seat where the cat was. *(Mark Fisher's "eerie")*
3. **Show less.** Suggest, don't display. Any reveal of the ghost before the finale lasts a fraction of a second, or is partial. *(Radcliffe's terror vs horror; Frictional)*
4. **Fear has a sweet spot.** Too little is boring and too much stops being fun, and every player's spot differs. The director and the calm moments keep each player near theirs. *(Andersen, Clasen et al. 2020)*
5. **Sound does most of the frightening.** Silence used as contrast is part of it. *(Garner & Grimshaw; Slender startle study)*
6. **Plausible beats spectacular.** Players fear what behaves believably. The haunting should follow house logic before it breaks it. *(VR presence study)*
7. **A little out of sync is scarier than plainly monstrous.** A lagging reflection, or a voice that doesn't match the mouth. *(Tinwell's uncanny behaviour)*
8. **Absurd first, then scary.** A safe absurd moment lowers the player's guard, and the scare lands in that gap. *(Benign violation theory)*
9. **He under-reacts to the unreal and over-reacts to the mundane.** That's what makes it read as real and unreal at the same time. *(Magical realism, Kentucky Route Zero, Lynch)*
10. **Depict despair honestly and suicide responsibly.** End on coping, never on method. That's what the "promise to live" ending already is. *(WHO guidance; the Papageno effect; Hellblade)*

---

## 1. The uncanny: the familiar, made slightly wrong

- **What it says:** Freud's *unheimlich* ("un-homely") is the feeling when something familiar and safe turns out slightly off. Horror games use it on purpose. *P.T.*'s single looping hallway is the textbook case (Kalinowski 2019). Practitioner advice puts **repetition** first: the same space, again, but changed (Game Developer, "Designing the Uncanny"). Dales (2021, an MSc thesis that built and playtested a game) found aesthetics, sound and play can all produce the effect.
- **Evidence:** a long theory tradition plus game analyses. It's a design principle, not a measured effect.
- **Rule for Fed Up:**
  - Every Phase 2/3 scare should be a **small deviation from a Phase 1 memory the player formed**.
  - **Already true:** the "sick house" uses the same rooms, and the bathroom you bathed in turns.
  - **Gate 4:** the appliances you used in the evening (toaster, kettle, fridge, TV) are the ones that misbehave at night.

## 2. The eerie: a failure of presence or absence

- **What it says:** Mark Fisher (*The Weird and the Eerie*, 2017) separates two feelings:
  - **The weird:** something present that doesn't belong.
  - **The eerie:** "a failure of absence or a failure of presence". Something is there when there should be nothing, or nothing is there when there should be something. The eerie is quiet and doesn't shock.
- **Evidence:** cultural theory, read here through reviews and an extract. Widely used by horror writers.
- **Rule for Fed Up:**
  - Use both kinds and alternate them:
    - **Presence where there should be none:** the footsteps (built), the TV on in an empty room (Gate 4/5).
    - **Absence where there should be presence:** the cat's empty spot, a missing jacket, the phone that stops ringing just as you reach it.
  - The eerie needs no jump. It's the cheapest way to make the house feel inhabited.

## 3. Terror vs horror: show less

- **What it says:**
  - Ann Radcliffe (1826) distinguishes the two. **Terror** comes from obscurity: it "expands the soul" and lets the imagination fill the gaps. **Horror** displays the thing, and it "contracts, freezes" the mind.
  - Frictional Games (*Amnesia*, *SOMA*) say the same from practice. Thomas Grip: when people hear sounds they *imagine* things, and that imagery drives the fear. Their tips include "long build-up" and "doubt".
  - Perron's study of Silent Hill (*The Terror Engine*) describes fear as *staged* ("mise-en-scène of fear"). Camera and framing decide when to hide and when to show.
- **Evidence:** a classic literary distinction, supported by successful practice. Practitioner, not experimental.
- **Rule for Fed Up:**
  - **Until the finale, the ghost is never shown in full.** Glimpses last under ~0.3 s, or show part of it (a hand, a reflection, eyes).
  - **Already true:** the mirror figure (0.22 s), and the hand under the couch you never see.
  - **The finale is where horror finally shows itself:** the boss at the gate, the body you burn. That's earned by withholding until then.

## 4. The sweet spot of fear

- **What it says:** Andersen, Schjoedt, Price, Rosas, Scrivner & Clasen (2020, *Psychological Science*) put heart-rate monitors on 110 people in a haunted attraction. **Enjoyment followed an inverted U:**
  - too little fear: little fun
  - too much: less fun
  - the peak was in the middle, and it **differs between people**
- **Evidence:** a peer-reviewed field study, the strongest single source here.
- **Rule for Fed Up:**
  - Keep each player near *their* peak, rather than maximising scares.
  - **Already true:**
    - The ghost director spaces scares and **backs off after big ones**.
    - Its timing depends on frustration.
    - The GDD's calm moments (the cat, the juice box, the radio, the home-video channel) are the relief valves.
  - **Gate 4:** place the juice box and radio *right after* the night's hardest stretch, not at random.
  - The *Left 4 Dead* "AI Director" (Valve) is commonly described as cycling build-up → peak → relax. That's the same idea, though I couldn't confirm the details from a primary source.

## 5. Sound does most of the work

- **What it says:**
  - Garner, Grimshaw & Abdel Nabi (2010): changing even coarse audio settings changes the intensity of players' fear in a survival-horror game.
  - A Newcastle study of *Slender*: turning the sound off didn't change self-reported immersion, but **body startle responses were clearly higher with sound on**.
  - A survey of 128 players ranked audio (creaks, silence) as the strongest fear driver.
- **Evidence:** small studies, but they all point the same way.
- **Rule for Fed Up:**
  - **Our placeholder beeps are the weakest part of every scare.** The silent mirror crack works *because* it breaks a pattern of sound.
  - **Design decision for you:** should real sound come earlier than art pass A2? See "Open questions".

## 6. Plausible beats spectacular

- **What it says:** in a VR horror study (145 players), what frightened people most was **plausibility**: things behaving as they believably could. That beat "being there" (place illusion), and it predicted overall fear.
- **Evidence:** one peer-reviewed study (VR, not desktop). Consistent with practice.
- **Rule for Fed Up:**
  - **Grounded first, unreal second.** A second-hand toaster that jams and burns is plausible, and so is a fridge door that won't stay shut. *Then* it's slightly too much: the toast flies to the ceiling, the fridge sneers in the boss's voice.
  - That ladder from real to unreal is the "real and unreal at the same time" you asked for.

## 7. A little out of sync

- **What it says:** Tinwell, Grimshaw & Williams (survival-horror characters, N=100) found motion and sound make characters feel stranger. **Poorly synchronised lips were rated the most frightening.** Uncanny enemy designs measurably raised fear and tension in another study (IEEE 2022, with heart rate).
- **Evidence:** small studies. The authors call the Bolton results hypotheses, not facts.
- **Rule for Fed Up:**
  - **Built:** the lagging reflection; the boss's garbled, cut-up voice.
  - **Next:**
    - The voice on the TV a beat behind the picture (Gate 4/5).
    - In the finale, the ghost-into-boss transformation slightly out of sync.

## 8. Absurd and scary

- **What it says:** benign violation theory (McGraw & Warren; six studies, 1,200+ participants) says **we laugh when something breaks the rules but is safe**. When the safety goes, it stops being funny and becomes threat. *(Applying this to horror is my inference, not the authors' claim.)*
- **Rule for Fed Up:**
  - **Absurd beats are "safe violations".** The toast to opera, the comedy flyback, the fridge that sneers.
  - **Put them just before a real scare.** The laugh lowers the guard, and the scare enters through the gap. *Already true:* the flyback, then the giggle.
  - Absurd must never undercut a sad beat. Never joke **after** the deep moments (Mom's card, the ghost's story).

## 9. Real and unreal at once: magical realism

- **What it says:** critics describe *Kentucky Route Zero*'s magical realism as treating the surreal as ordinary. A floor of bears that no one comments on: "the lack of commentary is the commentary" (NPR/GPB). Its makers cite David Lynch, who puts the uncanny inside the most ordinary American spaces.
- **Evidence:** criticism, not experiment. It's a craft principle.
- **Rule for Fed Up:**
  - **His voice under-reacts to the impossible and over-reacts to the trivial.** *"…Was that you?"* to an item flying back out of the bin; real anguish over the takeout guy being the only person who knows his name. That's the Wolfenstein/Mr. Robot tone already in the bible.
  - **New writing guide for every monologue line:** flat about the ghost, raw about his life.

## 10. Empty places built for people (liminal space)

- **What it says:**
  - Bray (2023, an MSc thesis with built environments): an empty station and an empty shop, places meant to be busy, caused discomfort, while an empty forest relaxed people. Key factors: ambience, theme, and **the absence of others**.
  - Diel & Lewis (2022, *Journal of Environmental Psychology*, seen secondhand) tie eeriness to buildings that depart from familiar patterns.
- **Evidence:** weak to moderate.
- **Rule for Fed Up:**
  - **The house is too big for one man.** It has a guest bedroom, a work room and a family-sized dining table, and nobody uses them. That's the GDD's "suspiciously big apartment", and it's eerie by itself.
  - **Gate 5 / A1:** dress the unused rooms as built for people who never came (six chairs, one plate).

## 11. Haunted media and VHS nostalgia (analog horror)

- **What it says:** analog horror (*Local 58*, *The Mandela Catalogue*) borrows VHS and old-TV aesthetics and treats media as "possessed". Its appeal is **nostalgia turned sinister** (Georgia Tech thesis; York St John paper). Degraded footage reads as authentic.
- **Evidence:** academic theses plus criticism.
- **Rule for Fed Up:**
  - Our VHS-grain look is on target.
  - The **calm channel is home video of him as a kid**, the perfect target to corrupt later.
  - **Proposal (Part 2, not canon):** in Phase 3 the home video frame-skips, and the kid looks at the camera a beat too long.

## 12. Story through the space and the task

- **What it says:**
  - Henry Jenkins (2004) names "embedded narrative": story information placed in the setting itself, in lighting, sound, set and props.
  - Frictional's "4 Layers" (Grip & Chmielarz, used for *SOMA*) builds each scene from gameplay up. On top go a narrative goal, the narrative background, and what the player comes to understand. **The task itself carries the story.**
- **Evidence:** widely used design frameworks.
- **Rule for Fed Up:**
  - **Every task needs a narrative-background layer** that says something about him. **Already true:** the clutter is his life (takeout, energy cans, the "Okayest Employee" mug), and the calm channel is his childhood.
  - **Gate 4:** each appliance and snack item gets one line or prop that says who he is.

## 13. Depicting depression and suicide responsibly

- **What it says:**
  - **The Werther effect:** sensational portrayals of suicide are linked to imitation (Niederkrotenthaler & Till, review).
  - **The Papageno effect:** stories of **someone who struggles and copes** are linked to *lower* risk in some audiences.
  - The WHO's media resource (2023 update): avoid method detail, focus on hope and recovery, include where to get help.
  - Ninja Theory's *Hellblade* worked with a neuroscientist and people with lived experience of psychosis. It's praised for it, though critics still point to scenes that slip into stereotype (Austin, *Game Studies*).
- **Evidence:** strong for news reporting. For fiction it's less settled, but the direction is consistent.
- **Rule for Fed Up:**
  - **Already true:**
    - The Burial ending is a Papageno story: he finds the ghost's story and **promises to live**.
    - The approved "player care" items: a content note at launch and a support-line card in the credits.
  - **Keep to:**
    - **Never show or describe a suicide method.** The ghost's death stays implied, through broken fragments of his life.
    - **Don't make death look like relief.** The "die in your sleep" ending must read as loss, not escape.
    - **His passive death wish is shown honestly but never endorsed.**
  - **Proposal (Part 2, not canon):** before art pass A4 (the Burial dream), have someone with lived experience or a mental-health professional review the script. That's the step *Hellblade* is praised for.

## 14. Why people want this at all

- **What it says:** Scrivner and colleagues (2021, *Personality and Individual Differences*, N=322) found horror fans reported **less psychological distress** early in the pandemic. Morbidly curious people showed more resilience. The authors' idea: frightening fiction lets people **rehearse coping safely** (a hypothesis, not proven).
- **Rule for Fed Up:**
  - The game's whole arc is a rehearsal: facing a version of yourself that gave up, and choosing to live.
  - The scares serve that. They are never the point on their own.

---

## What this changes now

- **Gate 4, built on these rules:**
  - **Appliances you know turn on you** (rules 1 and 6).
  - **The calm items come right after the hardest stretch** (rule 4).
  - **Every item says something about him** (rule 12).
  - **Absurd beats are placed before scares, never after sad ones** (rule 8).
- **A writing guide for his lines:** flat about the ghost, raw about his life (rule 9). This goes into the monologue file comments.

## Open questions for you (your call)

1. **When does real sound arrive?**
   - **A (recommended):** a small "scare sound" pass now: footsteps, slam, sting, drone, fridge, kettle, about 15 sounds. Every playtest after that tests the real fear.
   - **B:** keep placeholders until art pass A2, as planned. Faster now, but your feel answers about scares are weaker until then.
2. **A sensitivity reader for the Burial ending** (before A4). Yes or no? I recommend yes.
3. **The corrupted home-video idea** (rule 11): in Phase 3, the kid on the tape looks at the camera a beat too long. Add it to the bible's proposals?

---

## Sources

- Kalinowski, "P.T." and the uncanny, *Press Start* (2019): https://press-start.gla.ac.uk/index.php/press-start/article/download/121/78
- Dales, *Designing Digital Uncanny*, MSc thesis, University of York (2021): https://etheses.whiterose.ac.uk/31625
- Calvo Anoro, the uncanny in survival horror, *Tropelías* (2020): https://papiro.unizar.es/ojs/index.php/tropelias/article/view/4356
- "Designing the Uncanny", Game Developer: https://www.gamedeveloper.com/design/designing-the-uncanny
- Fisher, *The Weird and the Eerie* (2017), reviews and extract: https://thelondongroup.com/the-weird-and-the-eerie/ · https://thequietus.com/?p=21524
- Radcliffe, "On the Supernatural in Poetry" (1826): https://repositorio.ufsc.br/bitstream/handle/123456789/208925/On%20Supernatural%20in%20Poetry%20(Ann%20Radcliffe).pdf?sequence=1
- Perron, *Silent Hill: The Terror Engine* (open access): https://library.oapen.org/handle/20.500.12657/24016 · review: https://firstpersonscholar.com/?p=1744
- Frictional Games / Thomas Grip interviews: https://www.engadget.com/2012-05-01-amnesia-dev-shares-secrets-of-evolving-the-horror-genre.html · https://80.lv/articles/frictional-games-on-its-history-developing-horror-games/
- Grip, "4 Layers, a narrative design approach": https://gamedeveloper.com/design/4-layers-a-narrative-design-approach · critique: https://www.theastronauts.com/2015/09/soma-and-the-lost-layer-of-narrative-design/
- Andersen, Schjoedt, Price, Rosas, Scrivner & Clasen, "Playing With Fear", *Psychological Science* (2020): https://www.psychologicalscience.org/journals/psychological-science/0956797620972116/ · https://pure.au.dk/portal/en/publications/playing-with-fear-a-field-study-in-recreational-horror/
- Garner, Grimshaw & Abdel Nabi, fear value of sound parameters (2010): https://vbn.aau.dk/en/publications/a-preliminary-experiment-to-assess-the-fear-value-of-preselected-
- Newcastle *Slender* sound/startle study: https://nova.newcastle.edu.au/vital/access/%20/manager/Repository/uon:22465
- Ambient surreal horror player study (SKANIKA): https://jom.fti.budiluhur.ac.id/SKANIKA/article/view/3653
- VR horror presence/plausibility study (NCCU): https://ah.lib.nccu.edu.tw/bitstream/140.119/110472/1/350.pdf
- Zhang, Li & Qiu, "Decoding Fear" (2023): https://arxiv.org/pdf/2312.15582
- Tinwell, Grimshaw & Williams, "Uncanny behaviour in survival horror games": https://vbn.aau.dk/ws/files/63140720/Uncanny_behaviour_in_survival_horror_games.pdf
- Uncanny enemy design and fear (IEEE 2022): https://scholar.xjtlu.edu.cn/en/publications/what-design-choices-are-effective-in-inducing-fear-and-tension-in/
- McGraw & Warren, benign violation theory: https://leeds-faculty.colorado.edu/mcgrawp/PDF/Benign_Violation_Theory.html
- Kentucky Route Zero and magical realism (NPR/GPB): https://www.npr.org/2021/02/11/966499158/reading-the-game-kentucky-route-zero · https://www.pcgamer.com/au/welcome-to-the-zero-the-making-of-kentucky-route-zeros-open-road
- Bray, liminal spaces, MSc thesis, University of York (2023): https://etheses.whiterose.ac.uk/34902
- Analog horror, *The Mandela Catalogue* thesis (Georgia Tech): https://repository.gatech.edu/entities/publication/b26e1f62-2b28-4c91-bbf8-b980031ea435 · York St John paper: https://ray.yorksj.ac.uk/id/eprint/15244/3/D%20Henty%20Haunted%20Soundscapes.pdf
- Jenkins, "Game Design as Narrative Architecture" (2004): https://web.mit.edu/~21fms/People/henry3/games&narrative.html
- Niederkrotenthaler & Till / Domaradzki, Werther and Papageno effects: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC7967741/ · https://www.cambridge.org/core/journals/the-british-journal-of-psychiatry/article/role-of-media-reports-in-completed-and-prevented-suicide-werther-v-papageno-effects/DFF62CAE7A44147EE9CAB4DFB50B49F0
- WHO, *Preventing suicide: a resource for media professionals* (2023): https://who.int/publications/i/item/9789240076846
- *Hellblade* and psychosis consultation: https://gameinformer.com/b/features/archive/2017/04/17/three-big-takeaways-from-hellblade-s-psychosis-expert · https://results2021.ref.ac.uk/impact/9aeba804-2f6a-4864-a874-d9394bd025ed/pdf · Austin, *Game Studies*: https://gamestudies.org/2104/articles/austin
- Scrivner et al., "Pandemic practice" (2021): https://www.psypost.org/2020/10/horror-fandom-linked-to-reduced-psychological-distress-amid-the-covid-19-pandemic-58165
