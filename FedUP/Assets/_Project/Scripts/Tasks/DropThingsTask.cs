using FedUp.Story;
using UnityEngine;

namespace FedUp.Tasks
{
    /// <summary>
    /// GDD Phase 1, "Drop Your Things": hang the jacket, kick off the shoes, empty the bag. Emptying the bag reveals
    /// the boss's CONFIDENTIAL file (STORY_BIBLE.md) and plays the absurd discovery cutscene.
    /// No frustration either way. Afterwards the jacket starts haunting (twitches, falls) via <see cref="JacketHaunt"/>.
    /// </summary>
    public class DropThingsTask : TaskBehaviour
    {
        [SerializeField] TaskInteractable coatHook;
        [SerializeField] TaskInteractable shoeRack;
        [SerializeField] TaskInteractable bagTable;
        [SerializeField] GameObject jacketOnHook;
        [SerializeField] GameObject shoesOnRack;
        [SerializeField] GameObject bagContents;
        [SerializeField] GameObject bossFile;
        [SerializeField] JacketHaunt jacket;
        [SerializeField] CutscenePlayer cutscenes;
        [SerializeField] CutsceneSO fileDiscovery;
        [SerializeField] MonologueLibrarySO lines;

        public bool JacketHung { get; private set; }
        public bool ShoesOff { get; private set; }
        public bool BagEmptied { get; private set; }
        public bool FileFound => bossFile != null && bossFile.activeSelf;

        void Awake()
        {
            foreach (var t in new[] { coatHook, shoeRack, bagTable }) if (t != null) t.available = false;
            if (jacketOnHook) jacketOnHook.SetActive(false);
            if (shoesOnRack) shoesOnRack.SetActive(false);
            if (bagContents) bagContents.SetActive(false);
            if (bossFile) bossFile.SetActive(false);
        }

        protected override void OnBegin()
        {
            coatHook.available = shoeRack.available = bagTable.available = true;
            coatHook.Used += OnHook;
            shoeRack.Used += OnShoes;
            bagTable.Used += OnBag;
        }

        void OnHook(TaskInteractable t)
        {
            if (JacketHung) return; // re-hanging after a fall is handled by JacketHaunt
            JacketHung = true;
            coatHook.available = false;
            jacketOnHook.SetActive(true);
            lines?.Say("hang_jacket");
            CheckDone();
        }

        void OnShoes(TaskInteractable t)
        {
            ShoesOff = true;
            shoeRack.available = false;
            shoesOnRack.SetActive(true);
            lines?.Say("shoes_off");
            CheckDone();
        }

        void OnBag(TaskInteractable t)
        {
            BagEmptied = true;
            bagTable.available = false;
            bagContents.SetActive(true);
            bossFile.SetActive(true);
            if (cutscenes != null && fileDiscovery != null) cutscenes.Play(fileDiscovery, CheckDone);
            else CheckDone();
        }

        void CheckDone()
        {
            if (!(JacketHung && ShoesOff && BagEmptied) || State == TaskState.Done) return;
            if (cutscenes != null && cutscenes.Playing) return; // the file scene calls back when it ends
            if (jacket != null) jacket.Arm(coatHook);
            Complete();
        }

        public override void DebugComplete()
        {
            foreach (var t in new[] { coatHook, shoeRack, bagTable }) if (t != null) t.available = false;
            base.DebugComplete();
        }
    }
}
