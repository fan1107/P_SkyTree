using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    // Exercises actual animation events, projectile arrivals and bird collisions.
    public sealed class BattleHealthVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Result { get; private set; }
        private float originalScale;
        private bool originalBackground;

        public void Begin()
        {
            originalScale = Time.timeScale;
            originalBackground = Application.runInBackground;
            Application.runInBackground = true;
            Time.timeScale = 3;
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
                catch (Exception error) { Result += "FAIL: " + error + "\n"; break; }
                yield return instruction;
            }
            Time.timeScale = originalScale;
            Application.runInBackground = originalBackground;
            Finished = true;
            Directory.CreateDirectory("Verification/BattleHealth");
            File.WriteAllText("Verification/BattleHealth/results.txt", Result);
            if (Passed) Debug.Log(Result); else Debug.LogError(Result);
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory("Verification/BattleHealth");
            var pool = new BattleHealth(100);
            int changes = 0;
            pool.Changed += () => changes++;
            Check(pool.Damage(-1) == 0 && pool.Damage(0) == 0, "Nonpositive damage changed HP");
            Check(pool.Damage(30) == 30 && pool.Current == 70, "Ordinary damage failed");
            Check(pool.Damage(int.MaxValue) == 70 && !pool.IsAlive && pool.Current == 0, "Overkill was not clamped");
            Check(pool.Damage(50) == 0 && changes == 2, "Dead target took damage again");
            Result = "Health arithmetic, overkill, zero/negative damage and dead-target idempotence PASS\n";

            var game = FindFirstObjectByType<SkyTreeGame>();
            game.SendMessage("BeginBattle");
            yield return null; yield return null;
            var battle = FindFirstObjectByType<BattleCannonSequence>();
            var ship = FindFirstObjectByType<BattleShipView>();
            var swarm = battle.Swarm;
            var hud = FindFirstObjectByType<BattleHudView>();
            Check(ship.Health.Current == 1000 && swarm.AliveCount == 7, "Wrong initial HP/enemy count");
            Check(swarm.transform.GetSiblingIndex() < ship.transform.GetSiblingIndex(), "Birds are above the ship");
            Check(hud.transform.GetSiblingIndex() > ship.transform.GetSiblingIndex(), "HUD is behind the ship");
            Check(swarm.GetComponentsInChildren<BattleHealthBar>().Length == 7, "Missing enemy HP bars");
            yield return new WaitForEndOfFrame();
            foreach (var graphic in FindObjectsByType<BattleHealthBar>(FindObjectsSortMode.None))
            {
                Check(graphic.canvasRenderer && graphic.depth >= 0, "Health bar not rendered");
                var mesh = graphic.canvasRenderer.GetMesh();
                Check(mesh.vertexCount > 0, "Health bar mesh is empty");
            }
            foreach (var graphic in hud.GetComponentsInChildren<BattleHudGraphic>())
            {
                Check(graphic.canvasRenderer && graphic.depth >= 0, "HUD frame not rendered");
                var mesh = graphic.canvasRenderer.GetMesh();
                Check(mesh.vertexCount > 0, "HUD frame mesh is empty");
            }
            var firstTarget = swarm.GetTarget(0);
            float deadline = Time.time + 12;
            bool collisionCapture = false, hitCapture = false;
            while ((battle.Hits < 2 || swarm.CollisionCount < 2) && Time.time < deadline)
            {
                yield return new WaitForEndOfFrame();
                Check(battle.TimingMismatches == 0, "Cannon animation cue drifted");
                Check(ship.Health.Current == 1000 - swarm.CollisionCount * 18, "Collision damage did not match HP");
                Check(Mathf.Approximately(hud.ShipBar.Fraction, ship.Health.Fraction), "HUD HP stale");
                if (!collisionCapture && swarm.CollisionCount > 0)
                { ScreenCapture.CaptureScreenshot("Verification/BattleHealth/FirstCollision.png"); collisionCapture = true; }
                if (!hitCapture && battle.Hits > 0)
                { ScreenCapture.CaptureScreenshot("Verification/BattleHealth/FirstHit.png"); hitCapture = true; }
            }
            Check(battle.Hits >= 2 && swarm.CollisionCount >= 2, "Natural bullets/collisions did not run");
            Check(swarm.GetHealth(firstTarget).Current < swarm.GetHealth(firstTarget).Maximum, "Projectile did not damage its target");
            int shots = battle.ShotsFired, collisions = swarm.CollisionCount, hp = ship.Health.Current;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.35f);
            Check(shots == battle.ShotsFired && collisions == swarm.CollisionCount && hp == ship.Health.Current, "Combat advanced while paused");
            Time.timeScale = 3;
            Result += "Actual animation-event shots, projectile HP damage, collision damage, HUD synchronization, layer order and pause PASS\n";

            deadline = Time.time + 65;
            while (!battle.Finished && Time.time < deadline) yield return null;
            Check(swarm.AliveCount == 0 && ship.Health.IsAlive, "Natural battle failed to defeat all seven birds");
            shots = battle.ShotsFired; collisions = swarm.CollisionCount;
            yield return new WaitForSeconds(2);
            Check(battle.ShotsFired == shots && swarm.CollisionCount == collisions, "Combat continued after victory");
            Check(swarm.GetTarget(0) == null && swarm.GetTarget(int.MaxValue) == null, "Returned a dead target");
            Check(!swarm.Hit(firstTarget, 48) && swarm.AliveCount == 0, "Repeated lethal hit changed death count");
            Check(!firstTarget.gameObject.activeSelf && swarm.GetComponentsInChildren<BattleHealthBar>().Length == 0, "Dead enemy or HP bar remains visible");
            Check(battle.TimingMismatches == 0, "Animation/shot timing regression");
            Result += $"Natural victory: shots={shots}, hits={battle.Hits}, collisions={collisions}, hull={ship.Health.Current}; death cleanup, retargeting and all-dead idle PASS\n";
            ScreenCapture.CaptureScreenshot("Verification/BattleHealth/Victory.png");
            yield return null;

            game.SendMessage("BeginBattle"); yield return null; yield return null;
            battle = FindFirstObjectByType<BattleCannonSequence>();
            ship = FindFirstObjectByType<BattleShipView>();
            hud = FindFirstObjectByType<BattleHudView>();
            ship.TakeDamage(int.MaxValue);
            shots = battle.ShotsFired; collisions = battle.Swarm.CollisionCount;
            yield return new WaitForSeconds(2);
            Check(ship.Health.Current == 0 && hud.ShipBar.Fraction == 0 && battle.Finished, "Hull defeat did not reach zero");
            Check(battle.ShotsFired == shots && battle.Swarm.CollisionCount == collisions, "Combat continued after hull destruction");
            Check(ship.TakeDamage(18) == 0, "Destroyed hull took repeated damage");
            ScreenCapture.CaptureScreenshot("Verification/BattleHealth/Defeat.png");
            Result += "Hull zero HP, defeat stop, empty health bar and repeated collision immunity PASS\n";
            yield return null;

            yield return new WaitForEndOfFrame();
            var back = hud.GetComponentInChildren<Button>();
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, back.transform.position);
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject == back.gameObject, "Return button raycast missed");
            UnityEngine.EventSystems.ExecuteEvents.Execute(back.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            Check(FindObjectsByType<BattleHealthBar>(FindObjectsSortMode.None).Length == 0, "Battle HP UI leaked into lobby");
            game.SendMessage("BeginBattle"); yield return null; yield return null;
            ship = FindFirstObjectByType<BattleShipView>();
            battle = FindFirstObjectByType<BattleCannonSequence>();
            Check(ship.Health.Current == 1000 && battle.Swarm.AliveCount == 7, "Reentry retained old combat HP");
            Check(FindObjectsByType<BattleHudView>(FindObjectsSortMode.None).Length == 1, "Duplicate HUD after reentry");
            Result += "Rendered HP/frame meshes, return-button raycast, lobby cleanup and fresh battle reentry PASS\n";
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private void OnDestroy()
        {
            if (Finished) return;
            Time.timeScale = originalScale;
            Application.runInBackground = originalBackground;
        }
    }
}
