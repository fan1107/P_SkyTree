using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    /// <summary>Run in Play mode on a temporary object; uses the live battle's actual prefab.</summary>
    public sealed class SkybirdAnimationVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Result { get; private set; } = "";
        private bool previousBackground;
        private float previousScale;

        public void Begin()
        {
            previousBackground = Application.runInBackground;
            previousScale = Time.timeScale;
            Application.runInBackground = true;
            Time.timeScale = 1;
            StartCoroutine(Guarded());
        }

        private IEnumerator Guarded()
        {
            var test = Run();
            while (true)
            {
                object instruction;
                try
                {
                    if (!test.MoveNext()) { Passed = true; break; }
                    instruction = test.Current;
                }
                catch (Exception exception) { Result += "FAIL: " + exception + "\n"; break; }
                yield return instruction;
            }
            Restore();
            Finished = true;
            Directory.CreateDirectory("Verification/SkybirdAnimation");
            File.WriteAllText("Verification/SkybirdAnimation/PlayModeResults.txt", Result);
            if (Passed) Debug.Log(Result); else Debug.LogError(Result);
        }

        private IEnumerator Run()
        {
            var game = FindFirstObjectByType<SkyTreeGame>();
            Check(game, "Missing game");
            game.SendMessage("BeginBattle");
            yield return null; yield return null;
            var battle = FindFirstObjectByType<BattleCannonSequence>();
            var swarm = battle.Swarm;
            var target = swarm.GetTarget(1);
            var animation = target.GetComponent<SkybirdAnimation>();
            var animator = target.GetComponent<Animator>();
            var image = target.GetComponent<RawImage>();
            Check(animation && animator && animator.runtimeAnimatorController, "Live enemy lacks animation prefab components");
            var textures = new HashSet<Texture>();
            float end = Time.time + .65f;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                textures.Add(image.texture);
                Check(!animation.IsAttacking, "Enemy attacks before its idle wait");
                Check(target.localScale == Vector3.one, "Old squash flap still runs");
            }
            Check(textures.Count == 3, "Idle did not display all three frames: " + textures.Count);
            Result += "PASS: actual battle prefab cycles all three textures without stretching.\n";
            bool sawAttack = false, sawReturn = false;
            int attackFrames = 0;
            end = Time.time + 6;
            while (Time.time < end && !sawReturn)
            {
                yield return new WaitForEndOfFrame();
                if (animation.IsAttacking)
                {
                    sawAttack = true; attackFrames++;
                    Check(image.texture.name == "SkybirdTopDown", "Attack flaps or uses a different texture");
                    Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack_Glide"), "Attack state not entered");
                }
                else if (sawAttack)
                {
                    sawReturn = true;
                    Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Flap"), "Return did not resume idle");
                }
            }
            Check(sawAttack && sawReturn && attackFrames > 2, "Natural attack and return were not observed");
            Result += "PASS: natural attack holds original wings for " + attackFrames + " rendered frames, then returns to Idle.\n";

            // Interrupt another natural strike to cover the separate knockback path.
            SkybirdAnimation interrupted = null;
            end = Time.time + 4;
            while (!interrupted && Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                foreach (var candidate in swarm.GetComponentsInChildren<SkybirdAnimation>())
                    if (candidate.IsAttacking && swarm.GetHealth((RectTransform)candidate.transform).Current > 1)
                    { interrupted = candidate; break; }
            }
            Check(interrupted, "No strike available to interrupt");
            var interruptedTarget = (RectTransform)interrupted.transform;
            Check(swarm.Hit(interruptedTarget, 1), "Knockback hit failed");
            yield return new WaitForEndOfFrame();
            Check(!interrupted.IsAttacking && !interrupted.IsStopped, "Knockback kept attack pose");
            Check(interrupted.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle_Flap"), "Knockback did not flap");
            Check(swarm.Hit(interruptedTarget, int.MaxValue), "Lethal hit failed");
            Check(interrupted.IsStopped, "Dead enemy keeps animating");
            Check(!swarm.Hit(interruptedTarget, 1), "Dead enemy can still be hit");
            yield return new WaitForSeconds(.5f);
            Check(!interruptedTarget.gameObject.activeSelf, "Dead enemy did not disappear");
            Result += "PASS: interrupted strikes resume wingbeats; death freezes animation and removes the bird.\n";

            battle.StartEncounter(1, true);
            yield return null; yield return new WaitForEndOfFrame();
            var boss = battle.Swarm.GetTarget(0).GetComponent<SkybirdAnimation>();
            Check(boss && !boss.IsAttacking && !boss.IsStopped, "Boss/wave spawn did not reset animation");
            Check(battle.Swarm.GetComponentsInChildren<SkybirdAnimation>().Length == 1, "Old wave animation leaked");
            Result += "PASS: a fresh boss encounter starts Idle with the same animation prefab.\n";
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private void Restore()
        {
            Application.runInBackground = previousBackground;
            Time.timeScale = previousScale;
        }

        private void OnDestroy() { if (!Finished) Restore(); }
    }
}
