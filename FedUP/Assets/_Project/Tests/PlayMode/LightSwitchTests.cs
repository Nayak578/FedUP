using System.Collections;
using FedUp.Interaction;
using FedUp.Player;
using FedUp.Story;
using FedUp.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FedUp.Tests
{
    /// <summary>Slice 0 gate: the one interactable works the way the player will use it.</summary>
    public class LightSwitchTests
    {
        FirstPersonMotor motor;
        Interactor interactor;
        LightSwitch sw;
        InteractionPromptUI prompt;

        [UnitySetUp]
        public IEnumerator LoadGreybox()
        {
            SceneManager.LoadScene("Greybox");
            yield return null;
            yield return new WaitForSeconds(0.3f);
            motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            interactor = Object.FindAnyObjectByType<Interactor>();
            sw = Object.FindAnyObjectByType<LightSwitch>();
            prompt = Object.FindAnyObjectByType<InteractionPromptUI>();
            Object.FindAnyObjectByType<CutscenePlayer>()?.SkipAll(); // the prologue takes control away
            motor.UseVirtualInput = true;
        }

        IEnumerator StandInFrontOfSwitch(float distance)
        {
            // Step back from the wall up to 1.2 m, then move along the wall for the rest, so we stay inside the corridor.
            float back = Mathf.Min(distance, 1.2f);
            float along = Mathf.Sqrt(Mathf.Max(0f, distance * distance - back * back));
            Vector3 feet = sw.transform.position + sw.transform.forward * back + Vector3.Cross(Vector3.up, sw.transform.forward) * along;
            feet.y = motor.transform.position.y;
            motor.Teleport(feet, 0f);
            yield return null;
            Vector3 d = sw.transform.position - interactor.transform.position;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            motor.SetLook(yaw, pitch);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LookingAtSwitch_ShowsPrompt()
        {
            yield return StandInFrontOfSwitch(1.5f);
            Assert.AreSame(sw, interactor.Current as LightSwitch, "focused object");
            Assert.AreEqual("E — Light switch", prompt.CurrentText);
        }

        [UnityTest]
        public IEnumerator PressingInteract_TogglesLightOnThenOff()
        {
            var light = GameObject.Find("LGT_Entrance").GetComponent<Light>();
            Assert.IsFalse(sw.IsOn, "starts off");
            Assert.IsFalse(light.enabled, "entrance light starts off");
            yield return StandInFrontOfSwitch(1.5f);

            interactor.VirtualPress();
            yield return null;
            yield return null;
            Assert.IsTrue(sw.IsOn, "on after first press");
            Assert.IsTrue(light.enabled, "light on after first press");

            interactor.VirtualPress();
            yield return null;
            yield return null;
            Assert.IsFalse(sw.IsOn, "off after second press");
            Assert.IsFalse(light.enabled, "light off after second press");
        }

        [UnityTest]
        public IEnumerator SwitchOutOfReach_ShowsNoPrompt()
        {
            yield return StandInFrontOfSwitch(3f);
            Assert.IsNull(interactor.Current, "nothing focused at 3 m");
            Assert.AreEqual("", prompt.CurrentText);
        }
    }
}
