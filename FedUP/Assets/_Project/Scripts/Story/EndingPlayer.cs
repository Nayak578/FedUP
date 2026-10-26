using System.Collections;
using FedUp.Interaction;
using FedUp.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FedUp.Story
{
    /// <summary>
    /// Plays the ending the resolver picked (DESIGN_LOGIC #74-76). The Showdown has its own scene first; every ending
    /// ends on a title card with one line. Card words live in the monologue library ("ending_*"), so they can be edited
    /// without code. After the card: E to play the night again.
    /// </summary>
    public class EndingPlayer : MonoBehaviour
    {
        [SerializeField] ShowdownSequence showdown;
        [SerializeField] BurialSequence burial;
        [SerializeField] EndingScenes scenes;
        [SerializeField] FirstPersonMotor motor;
        [SerializeField] Interactor interactor;
        [SerializeField] MonologueLibrarySO lines;
        [SerializeField] GameObject card;
        [SerializeField] Image black;
        [SerializeField] Text title;
        [SerializeField] Text line;
        [SerializeField] Text prompt;
        [Tooltip("The small line under the card: job, home, ghost.")]
        [SerializeField] Text outcome;
        [SerializeField, Min(1f)] float againAfterSeconds = 4f;

        public bool Playing { get; private set; }
        public Ending Current { get; private set; }
        public bool CardShown { get; private set; }
        public string CardTitle => title != null ? title.text : null;
        public string CardLine => line != null ? line.text : null;
        public string CardOutcome => outcome != null ? outcome.text : null;

        void Awake()
        {
            if (card != null) card.SetActive(false);
        }

        public void Play(Ending ending)
        {
            if (Playing) return;
            Playing = true;
            Current = ending;
            System.Action card = () => StartCoroutine(Card(ending));
            if ((ending == Ending.ShowdownAnswered || ending == Ending.ShowdownIgnored) && showdown != null) showdown.Play(ending == Ending.ShowdownAnswered, card);
            else if ((ending == Ending.BurialAnswered || ending == Ending.BurialIgnored) && burial != null) burial.Play(card);
            else if ((ending == Ending.Loop || ending == Ending.UnemployedGhostAngry || ending == Ending.DieInSleep) && scenes != null) scenes.Play(ending, card);
            else card();
        }

        /// <summary>The card's id stem for each ending ("ending_loop_title", "ending_loop_line").</summary>
        public static string IdFor(Ending e)
        {
            switch (e)
            {
                case Ending.DieInSleep: return "ending_die";
                case Ending.Loop: return "ending_loop";
                case Ending.UnemployedGhostAngry: return "ending_unemployed";
                case Ending.ShowdownAnswered:
                case Ending.ShowdownIgnored: return "ending_showdown";
                case Ending.BurialAnswered: return "ending_burial_answered";
                case Ending.BurialIgnored: return "ending_burial_ignored";
                case Ending.SecretLoop: return "ending_secret_loop";
                default: return "ending_secret_ghost";
            }
        }

        IEnumerator Card(Ending ending)
        {
            if (motor != null) motor.Locked = true;
            string id = IdFor(ending);
            title.text = Text(id + "_title");
            line.text = Text(id + "_line");
            if (outcome != null) outcome.text = Text(id + "_outcome");
            prompt.text = "";
            card.SetActive(true);
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { SetAlpha(t / 1.5f); yield return null; }
            SetAlpha(1f);
            CardShown = true;
            Debug.Log($"ENDING CARD {ending}: {title.text} / {line.text}");
            yield return new WaitForSeconds(againAfterSeconds);
            prompt.text = "E — play the night again";
            if (interactor != null) interactor.enabled = true;
            while (interactor == null || !interactor.InteractPressedThisFrame) yield return null;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        string Text(string id) => lines != null && lines.TryGet(id, out var e) ? e.text : id;

        void SetAlpha(float a)
        {
            var c = black.color; c.a = a; black.color = c;
            foreach (var t in new[] { title, line, prompt, outcome }) { if (t == null) continue; var k = t.color; k.a = a; t.color = k; }
        }
    }
}
