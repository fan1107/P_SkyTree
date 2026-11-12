using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    public sealed class VoyageProgressVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Result { get; private set; } = "";
        private SkyTreeGame game;
        private bool hadPoint, background, restored;
        private int originalPoint;
        private float scale;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public void Begin()
        {
            hadPoint = PlayerPrefs.HasKey(SkyTreeGame.WorldPointKey);
            originalPoint = PlayerPrefs.GetInt(SkyTreeGame.WorldPointKey, 7);
            background = Application.runInBackground; scale = Time.timeScale;
            game = FindFirstObjectByType<SkyTreeGame>();
            Application.runInBackground = true; Time.timeScale = 3;
            Directory.CreateDirectory("Verification/Voyage");
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
                if (next is IEnumerator nested) work.Push(nested);
                else yield return next;
            }
            Passed = work.Count == 0;
            Restore();
            Finished = true;
            File.WriteAllText("Verification/Voyage/results.txt", Result);
            if (Passed) Debug.Log(Result); else Debug.LogError(Result);
        }

        private IEnumerator Run()
        {
            VerifyRules();
            yield return NaturalVoyage(4, 0, "OneCell");
            yield return NaturalVoyage(2, 1, "TwoCells");

            yield return Depart(2, 1);
            var journey = game.ActiveVoyage;
            yield return ClearWaves(journey, 2);
            Check(game.CurrentWorldPoint == 5, "Checkpoint did not use selected alternate route");
            FindFirstObjectByType<BattleShipView>().TakeDamage(int.MaxValue);
            yield return WaitForMap(journey);
            Check(game.CurrentWorldPoint == 5 && journey.Progress.ReachedPoint == 5, "Defeat did not preserve intermediate checkpoint");
            var popup = FindFirstObjectByType<RouteSelectionPopup>();
            popup.Close(); game.SendMessage("OpenRouteMap");
            CheckMap(5);
            Result += "Second-cell defeat saves the selected intermediate point; closing arrival animation/reopening preserves the marker PASS\n";

            yield return Depart(2, 0);
            journey = game.ActiveVoyage;
            yield return ClearWaves(journey, 4);
            Check(journey.Progress.Phase == VoyagePhase.Boss && game.CurrentWorldPoint == 4, "Destination saved before boss victory");
            FindFirstObjectByType<BattleShipView>().TakeDamage(int.MaxValue);
            yield return WaitForMap(journey);
            Check(game.CurrentWorldPoint == 4, "Boss defeat incorrectly arrived at destination");
            Result += "Boss defeat retains checkpoint; no final arrival before boss victory PASS\n";

            yield return Depart(2, 0);
            journey = game.ActiveVoyage;
            FindFirstObjectByType<BattleHudView>().GetComponentInChildren<Button>().onClick.Invoke();
            Check(journey.Progress.Phase == VoyagePhase.Retreat && game.CurrentWorldPoint == 7, "Early return moved the ship");
            yield return null;
            CheckMap(7);
            var wrongSource = new VoyageProgress(CreateRoute(0, 1));
            game.BeginVoyage(wrongSource.Route);
            Check(!game.ActiveVoyage && game.CurrentWorldPoint == 7, "Accepted route from wrong current position");
            Result += "Return button abort, invalid-source guard and source-position preservation PASS\n";
        }

        private void VerifyRules()
        {
            int options = 0;
            for (int a = 0; a < 9; a++)
            for (int b = 0; b < 9; b++)
            {
                if (!RoutePlan.TryCreate(a, b, out var first)) continue;
                for (int option = 0; option < first.OptionCount; option++)
                {
                    var route = CreateRoute(a, b, option);
                    var progress = new VoyageProgress(route);
                    Check(route.WaveCount == route.Distance * 2, "Wrong waves per cell");
                    for (int wave = 1; wave <= route.WaveCount; wave++)
                    {
                        Check(progress.WaveNumber == wave && progress.ClearEncounter(), "Wave order mismatch");
                        Check(!progress.ClearEncounter() && progress.ClearedWaves == wave, "Repeated clear advanced twice");
                        Check(progress.ReachedPoint == route.Waypoints[Math.Min(wave / 2, route.Distance - 1)], "Wrong checkpoint");
                        Check(progress.BeginNextEncounter() && !progress.BeginNextEncounter(), "Repeated transition advanced twice");
                    }
                    Check(progress.Phase == VoyagePhase.Boss && progress.BossStarts == 1, "Boss count/order mismatch");
                    progress.ClearEncounter();
                    Check(progress.Phase == VoyagePhase.Victory && progress.ReachedPoint == b, "Victory did not arrive");
                    progress.Fail(); progress.Retreat();
                    Check(progress.Phase == VoyagePhase.Victory && !progress.ClearEncounter() && !progress.BeginNextEncounter(), "Terminal outcome changed");
                    options++;
                }
            }
            Result += $"{options} route options: two waves per cell, all checkpoints, final boss exactly once and terminal idempotence PASS\n";
        }

        private IEnumerator Depart(int destination, int option)
        {
            if (game.ActiveVoyage) game.ActiveVoyage.Retreat();
            PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, 7); PlayerPrefs.Save();
            game.SendMessage("Lobby"); yield return null; yield return null;
            game.SendMessage("OpenRouteMap");
            var popup = FindFirstObjectByType<RouteSelectionPopup>();
            var buttons = (Button[])typeof(RouteSelectionPopup).GetField("points", Fields).GetValue(popup);
            for (int i = 0; i <= option; i++) buttons[destination].onClick.Invoke();
            popup.Depart();
            yield return null;
            Check(game.ActiveVoyage && game.ActiveVoyage.Progress.Route.Destination == destination, "UI departure did not start voyage");
        }

        private IEnumerator NaturalVoyage(int destination, int option, string capture)
        {
            yield return Depart(destination, option);
            var journey = game.ActiveVoyage;
            var progress = journey.Progress;
            var sequence = FindFirstObjectByType<BattleCannonSequence>();
            var ship = FindFirstObjectByType<BattleShipView>();
            int lastWave = 0, previousHP = ship.Health.Current;
            bool bossSeen = false;
            float deadline = Time.realtimeSinceStartup + 75;
            while (!progress.Finished && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                Check(sequence.TimingMismatches == 0, "Animation events drifted between waves");
                Check(FindObjectsByType<SkybirdSwarm>(FindObjectsSortMode.None).Length == 1, "Old wave remains active");
                Check(ship.Health.Current <= previousHP, "Hull HP reset between waves");
                previousHP = ship.Health.Current;
                if (progress.Phase == VoyagePhase.NormalWave && progress.WaveNumber != lastWave)
                {
                    Check(progress.WaveNumber == lastWave + 1 && sequence.Swarm.Count == 3 && !sequence.Swarm.IsBoss, "Wrong regular wave spawn");
                    lastWave = progress.WaveNumber;
                    ScreenCapture.CaptureScreenshot("Verification/Voyage/" + capture + "-Wave" + lastWave + ".png");
                }
                if (progress.Phase == VoyagePhase.Boss && !bossSeen)
                {
                    Check(lastWave == progress.Route.WaveCount && sequence.Swarm.IsBoss && sequence.Swarm.Count == 1, "Boss spawned early or with wrong count");
                    Check(sequence.Swarm.GetHealth(sequence.Swarm.GetTarget(0)).Maximum == 720, "Boss HP missing");
                    Check(game.CurrentWorldPoint != destination, "Destination stored before killing boss");
                    bossSeen = true;
                    ScreenCapture.CaptureScreenshot("Verification/Voyage/" + capture + "-Boss.png");
                }
            }
            Check(progress.Phase == VoyagePhase.Victory && bossSeen && progress.BossStarts == 1, "Natural voyage did not win exactly one boss battle");
            int shots = sequence.ShotsFired, hp = ship.Health.Current;
            yield return WaitForMap(journey);
            var popup = FindFirstObjectByType<RouteSelectionPopup>();
            Check(popup.CurrentPoint == destination && game.CurrentWorldPoint == destination, "Saved destination/map position mismatch");
            float animationDeadline = Time.realtimeSinceStartup + 4;
            while (popup.IsTravelling && Time.realtimeSinceStartup < animationDeadline) yield return null;
            Check(!popup.IsTravelling, "Map travel animation did not finish");
            CheckMap(destination);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Verification/Voyage/" + capture + "-Arrival.png");
            popup.Close(); game.SendMessage("OpenRouteMap"); CheckMap(destination);
            popup.SelectDestination(7);
            Check(popup.SelectedRoute.Source == destination, "Next departure does not start at saved destination");
            Result += $"{capture}: {lastWave} regular waves + one boss, shots={shots}, hull={hp}; HP carryover, saved arrival, map animation/reopen and next-route source PASS\n";
        }

        private IEnumerator ClearWaves(VoyageBattleController journey, int count)
        {
            for (int wave = 1; wave <= count; wave++)
            {
                var swarm = FindFirstObjectByType<BattleCannonSequence>().Swarm;
                while (swarm.AliveCount > 0) swarm.Hit(swarm.GetTarget(0), int.MaxValue);
                float deadline = Time.realtimeSinceStartup + 5;
                while ((journey.Progress.ClearedWaves < wave || journey.Progress.Phase == VoyagePhase.Intermission) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Check(journey.Progress.ClearedWaves == wave && !journey.Progress.Finished, "Wave transition stalled");
            }
        }

        private IEnumerator WaitForMap(VoyageBattleController journey)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (game.ActiveVoyage && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!game.ActiveVoyage, "Outcome did not finish");
            if (journey.Progress.Phase == VoyagePhase.Victory)
            {
                var result = FindFirstObjectByType<VoyageResultPopup>();
                Check(result, "Victory result popup missing");
                float arrivalDeadline = Time.realtimeSinceStartup + 4;
                while (result.Map.IsTravelling && Time.realtimeSinceStartup < arrivalDeadline) yield return null;
                Check(!result.Map.IsTravelling && result.Map.Destination == game.CurrentWorldPoint, "Result arrival mismatch");
                result.Confirm(); game.SendMessage("OpenRouteMap");
            }
            Check(FindFirstObjectByType<RouteSelectionPopup>(), "Outcome did not reopen world map");
        }

        private void CheckMap(int expected)
        {
            var popup = FindFirstObjectByType<RouteSelectionPopup>();
            Check(popup && popup.CurrentPoint == expected, "Wrong map current point");
            var marker = (RectTransform)typeof(RouteSelectionPopup).GetField("ship", Fields).GetValue(popup);
            var buttons = (Button[])typeof(RouteSelectionPopup).GetField("points", Fields).GetValue(popup);
            var expectedPosition = ((RectTransform)buttons[expected].transform).anchoredPosition + new Vector2(0, 22);
            Check(Vector2.Distance(marker.anchoredPosition, expectedPosition) < .01f, "Ship marker not at saved point");
        }

        private static RoutePlan CreateRoute(int source, int destination, int option = 0)
        {
            Check(RoutePlan.TryCreate(source, destination, out var plan, option), "Invalid test route"); return plan;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private void Restore()
        {
            if (restored) return;
            restored = true;
            if (game && game.ActiveVoyage) game.ActiveVoyage.Retreat();
            if (hadPoint) PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, originalPoint);
            else PlayerPrefs.DeleteKey(SkyTreeGame.WorldPointKey);
            PlayerPrefs.Save();
            if (game) { game.SendMessage("Lobby"); game.SendMessage("OpenRouteMap"); }
            Time.timeScale = scale; Application.runInBackground = background;
        }

        private void OnDestroy() { if (!restored) Restore(); }
    }
}
