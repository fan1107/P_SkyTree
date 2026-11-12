using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    public sealed class WorldLobbyTableVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Result { get; private set; } = "";
        private SkyTreeGame game;
        private bool hadWorld, hadPoint, background, started, restored;
        private int originalWorld, originalPoint;
        private float scale;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public void Begin()
        {
            game = FindFirstObjectByType<SkyTreeGame>();
            hadWorld = PlayerPrefs.HasKey(SkyTreeGame.WorldNumberKey);
            hadPoint = PlayerPrefs.HasKey(SkyTreeGame.WorldPointKey);
            originalWorld = PlayerPrefs.GetInt(SkyTreeGame.WorldNumberKey, 1);
            originalPoint = PlayerPrefs.GetInt(SkyTreeGame.WorldPointKey, 7);
            background = Application.runInBackground; scale = Time.timeScale; started = true;
            Application.runInBackground = true; Time.timeScale = 8;
            Directory.CreateDirectory("Verification/WorldLobby");
            StartCoroutine(Guarded());
        }

        private IEnumerator Guarded()
        {
            var work = new Stack<IEnumerator>(); work.Push(Run());
            while (work.Count > 0)
            {
                object next = null;
                try
                {
                    var current = work.Peek();
                    if (!current.MoveNext()) { work.Pop(); continue; }
                    next = current.Current;
                }
                catch (Exception error) { Result += "FAIL: " + error + "\n"; break; }
                if (next is IEnumerator nested) work.Push(nested); else yield return next;
            }
            Passed = work.Count == 0;
            Restore(); Finished = true;
            File.WriteAllText("Verification/WorldLobby/Runtime-results.txt", Result);
            if (Passed) Debug.Log(Result); else Debug.LogError(Result);
        }

        private IEnumerator Run()
        {
            PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, 1);
            for (int region = 1; region <= 9; region++)
            {
                PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, region - 1);
                game.SendMessage("Lobby"); yield return null;
                ExpectLobby(1, region);
                if (region == 3 || region == 4) yield return Capture("World1-Region" + region);
            }
            Result += "World 1: all 9 saved regions instantiate the configured lobby; correct 1-based/0-based conversion PASS\n";

            PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, 9);
            PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, 8);
            game.SendMessage("Lobby"); yield return null;
            ExpectLobby(9, 9);
            Result += "Unconfigured World 9 uses default lobby and displays WORLD 09 PASS\n";
            yield return CheckLobbyMotion();

            PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, 1);
            PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, 0);
            game.SendMessage("Lobby"); yield return null;
            yield return Depart(3);
            yield return WinVoyage();
            ExpectLobby(1, 3);
            Check(game.CurrentWorldPoint == 2, "Merchant arrival did not save region 3");
            FindFirstObjectByType<RouteSelectionPopup>().Close();
            yield return Capture("Arrival-Merchant");

            yield return Depart(4);
            yield return WinVoyage();
            ExpectLobby(1, 4);
            Check(game.CurrentWorldPoint == 3, "Island arrival did not save region 4");
            FindFirstObjectByType<RouteSelectionPopup>().Close();
            yield return Capture("Arrival-Island");
            Result += "Selected-route victories return to merchant (region 3) then island (region 4) lobbies PASS\n";

            PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, 0);
            game.SendMessage("Lobby"); yield return null;
            yield return Depart(7); // 1 -> 4 -> 7: island is the intermediate checkpoint.
            var journey = game.ActiveVoyage;
            float deadline = Time.realtimeSinceStartup + 8;
            while (journey.Progress.ClearedWaves < 2 && Time.realtimeSinceStartup < deadline)
            {
                KillCurrentWave(); yield return null;
            }
            Check(journey.Progress.ClearedWaves == 2 && game.CurrentWorldPoint == 3, "Island checkpoint not saved");
            FindFirstObjectByType<BattleShipView>().TakeDamage(int.MaxValue);
            yield return WaitForReturn();
            ExpectLobby(1, 4);
            Check(game.CurrentWorldPoint == 3, "Defeat used destination instead of checkpoint lobby");
            Result += "Defeat after intermediate region 4 returns to its island lobby PASS\n";
        }

        // UI 분리 뒤에도 배·구름·두 동료가 각자의 위치와 안전 경계를 유지하며 움직이는지 확인한다.
        private IEnumerator CheckLobbyMotion()
        {
            var composition = FindFirstObjectByType<LobbyController>();
            var ship = (RectTransform)composition.Ship.transform;
            var cloud = (RectTransform)FindFirstObjectByType<LobbyPassingClouds>().transform.GetChild(0);
            var walkers = composition.Ship.GetComponents<LobbyCaptainWander>();
            var shipStart = ship.anchoredPosition; var cloudStart = cloud.anchoredPosition;
            var initial = new[] { walkers[0].FootPosition, walkers[1].FootPosition };
            bool shipMoved = false, cloudMoved = false;
            var moved = new bool[2];
            float end = Time.time + 22;
            while (Time.time < end)
            {
                shipMoved |= Vector2.Distance(shipStart, ship.anchoredPosition) > 1;
                cloudMoved |= Vector2.Distance(cloudStart, cloud.anchoredPosition) > 5;
                for (int i = 0; i < 2; i++)
                {
                    moved[i] |= Vector2.Distance(initial[i], walkers[i].FootPosition) > 4;
                    Check(walkers[i].IsWalkable(walkers[i].FootPosition), "Crew outside deck");
                }
                yield return null;
            }
            Check(shipMoved && cloudMoved && moved[0] && moved[1], "Independent lobby motion failed");
            Result += "Ship/cloud motion and both crew walking inside deck for 22 game seconds PASS\n";
        }

        private IEnumerator Depart(int region)
        {
            game.SendMessage("OpenRouteMap");
            var popup = FindFirstObjectByType<RouteSelectionPopup>();
            popup.SelectDestination(region - 1);
            Check(popup.CanDepart && popup.SelectedRoute.Destination == region - 1, "Route selection failed");
            if (region == 3 || region == 4)
            {
                var label = (RectTransform)typeof(RouteSelectionPopup).GetField("destinationLabel", Fields).GetValue(popup);
                var points = (Button[])typeof(RouteSelectionPopup).GetField("points", Fields).GetValue(popup);
                Check(label.anchoredPosition.y < ((RectTransform)points[region - 1].transform).anchoredPosition.y, "Label overlaps landmark");
                yield return Capture("Map-Region" + region + "-Selected");
            }
            popup.Depart(); yield return null;
            Check(game.ActiveVoyage, "Departure did not create a voyage");
        }

        private IEnumerator WinVoyage()
        {
            var progress = game.ActiveVoyage.Progress;
            float deadline = Time.realtimeSinceStartup + 10;
            while (!progress.Finished && Time.realtimeSinceStartup < deadline)
            {
                KillCurrentWave(); yield return null;
            }
            Check(progress.Phase == VoyagePhase.Victory, "Voyage did not finish with victory");
            yield return WaitForReturn();
        }

        private void KillCurrentWave()
        {
            var phase = game.ActiveVoyage.Progress.Phase;
            if (phase != VoyagePhase.NormalWave && phase != VoyagePhase.Boss) return;
            var swarm = FindFirstObjectByType<BattleCannonSequence>().Swarm;
            while (swarm.AliveCount > 0) swarm.Hit(swarm.GetTarget(0), int.MaxValue);
        }

        private IEnumerator WaitForReturn()
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (game.ActiveVoyage && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!game.ActiveVoyage, "Voyage did not complete");
            var result = FindFirstObjectByType<VoyageResultPopup>();
            if (result)
            {
                float animationDeadline = Time.realtimeSinceStartup + 4;
                while (result.Map.IsTravelling && Time.realtimeSinceStartup < animationDeadline) yield return null;
                Check(!result.Map.IsTravelling && result.Map.Destination == game.CurrentWorldPoint, "Result arrival mismatch");
                result.Confirm(); game.SendMessage("OpenRouteMap");
            }
            Check(FindFirstObjectByType<RouteSelectionPopup>(), "Voyage did not return to map");
        }

        private void ExpectLobby(int world, int region)
        {
            var lobbies = FindObjectsByType<LobbyView>(FindObjectsSortMode.None);
            Check(lobbies.Length == 1, "Multiple active lobbies");
            // 지역이 바뀌어도 공통 UI는 하나이며, 배경만 테이블에 따라 교체된다.
            var composition = FindFirstObjectByType<LobbyController>();
            string expected = world == 1 && region == 3 ? "LobbyBackground_SkyMerchant" :
                world == 1 && region == 4 ? "LobbyBackground_LoneSkyTreeIsland" : "LobbyBackground_Default";
            Check(composition && composition.Background.name == expected + "(Clone)", "Wrong background for " + world + "/" + region);
            Check(lobbies[0].GetComponentsInChildren<ShipTreeGrowth>(true).Length == 0, "UI still owns ship");
            Check(FindObjectsByType<LobbyShipView>(FindObjectsSortMode.None).Length == 1, "Missing or duplicate ship");
            Check(FindObjectsByType<LobbyPassingClouds>(FindObjectsSortMode.None).Length == 1, "Duplicate cloud layer");
            Check(composition.Ship.GetComponents<LobbyCaptainWander>().Length == 2, "Ship must own both crew");
            Check(composition.Background.transform.parent.GetSiblingIndex() < composition.Ship.transform.parent.GetSiblingIndex(), "Ship must draw above background");
            Check(composition.PopupRoot.GetSiblingIndex() > lobbies[0].transform.parent.GetSiblingIndex(), "Popup must draw above UI");
            var title = (Text)typeof(LobbyView).GetField("locationLabel", Fields).GetValue(lobbies[0]);
            string expectedTitle = world == 1 && region == 3 ? "하늘나무 상선" :
                world == 1 && region == 4 ? "외딴 하늘나무 섬" : "바람초원 정박지";
            Check(title.text == expectedTitle, "Wrong location title");
            var label = (Text)typeof(LobbyView).GetField("worldLabel", Fields).GetValue(lobbies[0]);
            Check(label && label.text == $"WORLD {world:00}", "Wrong world label");
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Verification/WorldLobby/" + name + ".png");
            yield return new WaitForEndOfFrame();
        }

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private void Restore()
        {
            if (!started || restored) return;
            restored = true;
            if (game && game.ActiveVoyage) game.ActiveVoyage.Retreat();
            if (hadWorld) PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, originalWorld);
            else PlayerPrefs.DeleteKey(SkyTreeGame.WorldNumberKey);
            if (hadPoint) PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, originalPoint);
            else PlayerPrefs.DeleteKey(SkyTreeGame.WorldPointKey);
            PlayerPrefs.Save();
            if (game) { game.SendMessage("Lobby"); game.SendMessage("OpenRouteMap"); }
            Time.timeScale = scale; Application.runInBackground = background;
        }

        private void OnDestroy() => Restore();
    }
}
