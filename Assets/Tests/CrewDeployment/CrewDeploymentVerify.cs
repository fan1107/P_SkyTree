using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    // Run only from Play mode. Uses the actual scene, prefabs and animation events.
    [DefaultExecutionOrder(400)]
    public sealed class CrewDeploymentVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public string Result { get; private set; }
        private readonly StringBuilder report = new StringBuilder();
        public void Begin() => StartCoroutine(Run());
        private void Check(bool pass, string message)
        {
            if (!pass) throw new Exception("Crew deployment: " + message);
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory("Verification/CrewDeployment");
            var game = FindFirstObjectByType<SkyTreeGame>();
            game.SendMessage("Lobby");
            yield return null;
            var walkers = FindObjectsByType<LobbyCaptainWander>(FindObjectsSortMode.None);
            Check(walkers.Length == 2, "expected two lobby walkers");
            var initial = new[]{walkers[0].FootPosition, walkers[1].FootPosition};
            bool[] moved = {false,false};
            Time.timeScale = 2;
            float end = Time.time + 22;
            while (Time.time < end)
            {
                for (int i=0;i<2;i++)
                {
                    Check(walkers[i].IsWalkable(walkers[i].FootPosition), "crew left safe deck: " + walkers[i].CrewName);
                    moved[i] |= Vector2.Distance(initial[i], walkers[i].FootPosition) > 4;
                }
                yield return null;
            }
            Time.timeScale = 1;
            Check(moved[0] && moved[1], "both crew must move");
            Check(walkers[0].Visits >= 4 && walkers[1].Visits >= 4, "both crew must complete multiple routes");
            report.AppendLine("Lobby: two independently moving crew, 22-second boundary audit, >=4 routes each PASS");
            ScreenCapture.CaptureScreenshot("Verification/CrewDeployment/Lobby-final.png");
            yield return new WaitForSeconds(.2f);
            game.SendMessage("BeginBattle");
            yield return null;
            var battle = FindFirstObjectByType<BattleCannonSequence>();
            var ship = FindFirstObjectByType<BattleShipView>();
            Check(ship.Crew.Length == 2, "expected two battle crew");
            bool[] captured = {false,false};
            end = Time.time + 20;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                Check(battle.TimingMismatches == 0, "shot differed from animation event frame");
                for (int i=0;i<2;i++)
                {
                    Check(battle.ShotsByCannon[i] == ship.Crew[i].FireEvents, "event/shot count mismatch");
                    foreach (var sr in ship.Crew[i].GetComponentsInChildren<SpriteRenderer>(true))
                        Check(!sr.enabled, "world sprite leaked into Canvas scene");
                    if (!captured[i] && battle.LastShotCannon == i && battle.LastShotFrame == Time.frameCount)
                    {
                        ScreenCapture.CaptureScreenshot("Verification/CrewDeployment/Attack-" + i + ".png"); captured[i] = true;
                    }
                }
            }
            Check(battle.ShotsByCannon[0] >= 5 && battle.ShotsByCannon[1] >= 5, "both crew should repeatedly shoot");
            Check(captured[0] && captured[1], "capture both firing frames");
            report.AppendLine("Battle: Long="+battle.ShotsByCannon[0]+", Navigator="+battle.ShotsByCannon[1]+", event counts equal, same-frame timing, no world sprite leaks PASS");
            int shots = battle.ShotsFired;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.5f);
            Check(battle.ShotsFired == shots, "paused battle fired");
            Time.timeScale = 1;
            game.SendMessage("Lobby"); yield return null; yield return null;
            Check(FindObjectsByType<CanvasCrewRig>(FindObjectsSortMode.None).Length == 0, "old battle rigs leaked");
            Check(FindObjectsByType<LobbyCaptainWander>(FindObjectsSortMode.None).Length == 2, "lobby reentry duplicated crew");
            game.SendMessage("BeginBattle"); yield return null; yield return null;
            Check(FindObjectsByType<CanvasCrewRig>(FindObjectsSortMode.None).Length == 2, "battle reentry duplicated crew");
            Check(FindFirstObjectByType<BattleCannonSequence>().ShotsFired == 0, "new battle retained old shot count");
            report.AppendLine("Pause, lobby return, battle reentry and crew cleanup PASS");
            game.SendMessage("Lobby");
            Finished = true; Result = report.ToString();
            File.WriteAllText("Verification/CrewDeployment/results.txt", Result);
            Debug.Log(Result);
        }
        private void OnDestroy() { Time.timeScale = 1; }
    }
}
