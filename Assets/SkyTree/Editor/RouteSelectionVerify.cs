using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Editor
{
    public static class RouteSelectionVerify
    {
        [MenuItem("SkyTree/Verify Route Selection")]
        public static void Run()
        {
            int validPairs = 0, bentPairs = 0;
            for (int source = 0; source < 9; source++)
            for (int destination = 0; destination < 9; destination++)
            {
                bool valid = RoutePlan.TryCreate(source, destination, out var plan);
                Require(valid == (source != destination), "Unexpected route validity");
                if (!valid) continue;
                validPairs++;
                int dx = Math.Abs(source % 3 - destination % 3);
                int dy = Math.Abs(source / 3 - destination / 3);
                bool bends = dx != 0 && dy != 0 && dx != dy;
                Require(plan.OptionCount == (bends ? 2 : 1), "Wrong option count");
                if (bends) bentPairs++;
                var middles = new HashSet<int>();
                for (int option = 0; option < plan.OptionCount; option++)
                {
                    Require(RoutePlan.TryCreate(source, destination, out var candidate, option), "Missing option");
                    Require(candidate.Source == source && candidate.Destination == destination, "Wrong endpoints");
                    Require(candidate.Waypoints[0] == source && candidate.Waypoints[candidate.Distance] == destination, "Wrong path endpoints");
                    Require(candidate.Distance == Math.Max(dx, dy), "Route is not shortest");
                    Require(candidate.WaveCount == candidate.Distance * 2 && candidate.BossCount == 1, "Wrong battle counts");
                    Require(candidate.OptionIndex == option, "Wrong selected option");
                    for (int i = 1; i < candidate.Waypoints.Count; i++)
                    {
                        int a = candidate.Waypoints[i - 1], b = candidate.Waypoints[i];
                        Require(b >= 0 && b < 9 && a != b, "Invalid waypoint");
                        Require(Math.Abs(a % 3 - b % 3) <= 1 && Math.Abs(a / 3 - b / 3) <= 1, "Skipped a cell");
                    }
                    if (candidate.Distance == 2) Require(middles.Add(candidate.Waypoints[1]), "Duplicate option");
                    if (bends)
                    {
                        int via = candidate.Waypoints[1];
                        bool firstStraight = source % 3 == via % 3 || source / 3 == via / 3;
                        Require(firstStraight == (option == 0), "Expected straight-first ordering");
                    }
                }
                Require(RoutePlan.TryCreate(source, destination, out var wrapped, plan.OptionCount), "Option wrap failed");
                Require(wrapped.Waypoints.SequenceEqual(plan.Waypoints), "Option did not wrap");
            }
            foreach (int invalid in new[] { -1, 9, int.MinValue, int.MaxValue })
            {
                Require(!RoutePlan.TryCreate(invalid, 7, out _), "Accepted invalid source");
                Require(!RoutePlan.TryCreate(7, invalid, out _), "Accepted invalid destination");
            }
            Require(!RoutePlan.TryCreate(7, 2, out _, -1), "Accepted negative option");

            var prefab = AssetDatabase.LoadAssetAtPath<RouteSelectionPopup>("Assets/UIPrefabs/Popups/RouteSelectionPopup.prefab");
            Require(prefab, "Missing route popup prefab");
            var root = new GameObject("Route selection verification", typeof(RectTransform));
            root.hideFlags = HideFlags.HideAndDontSave;
            root.SetActive(false);
            var font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 32);
            try
            {
                var popup = UnityEngine.Object.Instantiate(prefab, root.transform);
                var serialized = new SerializedObject(popup);
                var pointProperties = serialized.FindProperty("points");
                var buttons = new Button[9];
                for (int i = 0; i < buttons.Length; i++)
                    buttons[i] = (Button)pointProperties.GetArrayElementAtIndex(i).objectReferenceValue;
                var summary = (Text)serialized.FindProperty("summary").objectReferenceValue;
                var route = (RouteMapGraphic)serialized.FindProperty("route").objectReferenceValue;
                var depart = (Button)serialized.FindProperty("depart").objectReferenceValue;
                int departures = 0;
                RoutePlan departed = null;
                Action<RoutePlan> onDepart = p => { departures++; departed = p; };
                popup.Open(font, 7, onDepart);
                Require(!popup.CanDepart && popup.SelectedRoute == null && !route.gameObject.activeSelf, "Initial state not cleared");
                buttons[2].onClick.Invoke();
                RequirePath(popup, 7, 4, 2);
                Require(summary.text == "4 웨이브 · 보스 1회", "Wrong two-move summary");
                buttons[2].onClick.Invoke();
                RequirePath(popup, 7, 5, 2);
                buttons[2].onClick.Invoke();
                RequirePath(popup, 7, 4, 2);
                buttons[5].onClick.Invoke();
                RequirePath(popup, 7, 5);
                Require(summary.text == "2 웨이브 · 보스 1회", "Wrong one-move summary");
                buttons[2].onClick.Invoke();
                RequirePath(popup, 7, 4, 2);
                buttons[1].onClick.Invoke();
                RequirePath(popup, 7, 4, 1);
                buttons[1].onClick.Invoke();
                RequirePath(popup, 7, 4, 1);
                buttons[7].onClick.Invoke();
                Require(!popup.CanDepart && popup.SelectedRoute == null && !route.gameObject.activeSelf, "Current point did not clear selection");
                buttons[2].onClick.Invoke();
                buttons[2].onClick.Invoke();
                popup.Close();
                popup.Open(font, 7, onDepart);
                Require(!popup.CanDepart && popup.SelectedRoute == null, "Reopen did not reset");
                buttons[2].onClick.Invoke();
                RequirePath(popup, 7, 4, 2);
                buttons[2].onClick.Invoke();
                depart.onClick.Invoke();
                depart.onClick.Invoke();
                Require(departures == 1 && departed.Waypoints.SequenceEqual(new[] { 7, 5, 2 }), "Departure lost route or fired twice");
                Require(!popup.gameObject.activeSelf, "Departure did not close popup");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(font);
            }
            string report = $"Route selection verification: PASS\n81 pairs: {validPairs} valid, 9 current-point rejections; {bentPairs} bent pairs with two distinct options.\nAll steps adjacent; shortest distance, waypoint order, wave/boss counts and invalid inputs verified.\nPrefab button events: 8->5->3, 8->6->3, wrap, destination change, straight routes, current point, reopen reset and one-shot departure PASS.\n";
            Directory.CreateDirectory("Verification");
            File.WriteAllText("Verification/RouteSelection-results.txt", report);
            Debug.Log(report);
        }

        static void RequirePath(RouteSelectionPopup popup, params int[] expected)
        {
            Require(popup.CanDepart && popup.SelectedRoute != null && popup.SelectedRoute.Waypoints.SequenceEqual(expected), "Unexpected popup route");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
