#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Tests
{
    public sealed class VoyageResultVerify : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public bool Passed { get; private set; }
        public string Result { get; private set; } = "";
        private SkyTreeGame game;
        private bool started, restored, hadPoint, hadWorld, background;
        private int point, world, goldBalance, gemBalance;
        private float scale;
        private Font font;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
        private ItemDefinition Item(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SkyTree/Data/Items/" + name + ".asset");

        public void Begin()
        {
            game = FindFirstObjectByType<SkyTreeGame>();
            hadPoint = PlayerPrefs.HasKey(SkyTreeGame.WorldPointKey); point = PlayerPrefs.GetInt(SkyTreeGame.WorldPointKey, 7);
            hadWorld = PlayerPrefs.HasKey(SkyTreeGame.WorldNumberKey); world = PlayerPrefs.GetInt(SkyTreeGame.WorldNumberKey, 1);
            goldBalance = PlayerPrefs.GetInt("SkyTree.Guest.Gold", 0); gemBalance = PlayerPrefs.GetInt("SkyTree.Guest.Gems", 0);
            scale = Time.timeScale; background = Application.runInBackground; started = true;
            Application.runInBackground = true;
            font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 32);
            Directory.CreateDirectory("Verification/ResultPopup"); StartCoroutine(Guarded());
        }
        private IEnumerator Guarded()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                object next = null;
                try { if (!stack.Peek().MoveNext()) { stack.Pop(); continue; } next = stack.Peek().Current; }
                catch (Exception error) { Result += "FAIL " + error + "\n"; break; }
                if (next is IEnumerator nested) stack.Push(nested); else yield return next;
            }
            Passed = stack.Count == 0; Restore(); Finished = true;
            File.WriteAllText("Verification/ResultPopup/results.txt", Result);
            if (Passed) Debug.Log(Result); else Debug.LogError(Result);
        }
        private IEnumerator Run()
        {
            game.SendMessage("Lobby"); yield return null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UIPrefabs/Popups/VoyageResultPopup.prefab");
            var popup = Instantiate(prefab, FindFirstObjectByType<LobbyController>().PopupRoot, false).GetComponent<VoyageResultPopup>();
            RoutePlan.TryCreate(7, 2, out var route);
            var progress = new VoyageProgress(route);
            while (!progress.Finished) { progress.ClearEncounter(); progress.BeginNextEncounter(); }
            var samples = new[] { new ItemReward(Item("Gold"), 1200), new ItemReward(Item("Wood"), 8), new ItemReward(Item("Gem"), 3) };
            int confirms = 0;
            Time.timeScale = 0;
            popup.Open(font, progress, samples, () => confirms++);
            yield return WaitForArrival(popup);
            Check(!popup.Map.IsTravelling && Vector2.Distance(popup.Map.MarkerPosition, popup.Map.Position(2) + new Vector2(0, 15)) < .01f, "Unscaled arrival failed");
            Check(popup.VisibleRewardCount == 3, "Sample slots missing");
            yield return Capture("Success-Preview");
            int clicks = 0;
            var slot = popup.GetComponentsInChildren<UIItemSlot>()[0];
            slot.Bind(samples[0], font, item => clicks++); Field<Button>(slot, "clickArea").onClick.Invoke();
            slot.Bind(samples[1], font); Field<Button>(slot, "clickArea").onClick.Invoke();
            Check(clicks == 1 && slot.Item == Item("Wood") && slot.Quantity == 8, "Stale slot callback/data");
            var seven = Enumerable.Range(1, 7).Select(i => new ItemReward(Item(i % 2 == 0 ? "Wood" : "Gold"), i * 17)).ToArray();
            popup.BindRewards(seven, font); yield return null;
            var scroll = Field<ScrollRect>(popup, "rewardScroll");
            Check(popup.VisibleRewardCount == 7 && scroll.content.rect.height > scroll.viewport.rect.height * 2, "List does not grow/scroll");
            scroll.verticalNormalizedPosition = 0; yield return null;
            yield return Capture("Seven-Rewards-Scrolled");
            popup.BindRewards(new[] { new ItemReward(Item("Gem"), long.MaxValue) }, font);
            Check(popup.VisibleRewardCount == 1 && popup.GetComponentsInChildren<UIItemSlot>().Length == 1, "Pool stale slots visible");
            Check(popup.GetComponentsInChildren<UIItemSlot>(true).Length == 7, "Pool not reused");
            Check(scroll.verticalNormalizedPosition > .99f, "Rebind did not reset scroll");
            popup.BindRewards(new[] { new ItemReward(null, 10), new ItemReward(Item("Gold"), 0), new ItemReward(Item("Wood"), -1) }, font);
            Check(popup.VisibleRewardCount == 0 && Field<GameObject>(popup, "emptyRewards").activeSelf, "Empty/invalid state incorrect");
            foreach (var s in popup.GetComponentsInChildren<UIItemSlot>(true)) Check(!s.Item && s.Quantity == 0, "Inactive slot retains data");
            yield return Capture("Empty-Rewards");
            popup.Confirm(); popup.Confirm(); Check(confirms == 1 && !popup.gameObject.activeSelf, "Confirm repeated callback");
            popup.Open(font, progress, samples, () => confirms += 10); popup.Confirm(); Check(confirms == 11, "Reopen retained old callback");
            Destroy(popup.gameObject); yield return null;
            Result += "Unscaled route animation; 0/1/3/7 rewards; invalid rows; scroll/reset; pooled slot rebinding; callback clearing; confirm/reopen PASS\n";
            yield return Battle(4, 0, Array.Empty<ItemReward>());
            yield return Battle(2, 1, new[] { new ItemReward(Item("Wood"), 19), new ItemReward(Item("Gem"), 2) });
            Check(PlayerPrefs.GetInt("SkyTree.Guest.Gold", 0) == goldBalance && PlayerPrefs.GetInt("SkyTree.Guest.Gems", 0) == gemBalance, "Display or confirm awarded currency");
            Result += "Result UI and sample preview never award currency PASS\n";
        }
        private IEnumerator Battle(int destination, int option, ItemReward[] rewards)
        {
            Time.timeScale = 8; PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, 1); PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, 7);
            game.SendMessage("Lobby"); yield return null;
            game.SendMessage("OpenRouteMap"); var selection = FindFirstObjectByType<RouteSelectionPopup>();
            for (int i = 0; i <= option; i++) selection.SelectDestination(destination);
            selection.Depart(); yield return null;
            var journey = game.ActiveVoyage; var progress = journey.Progress;
            progress.SetEarnedRewards(rewards);
            float deadline = Time.realtimeSinceStartup + 15;
            while (!progress.Finished && Time.realtimeSinceStartup < deadline)
            {
                if (progress.Phase == VoyagePhase.NormalWave || progress.Phase == VoyagePhase.Boss)
                {
                    var swarm = FindFirstObjectByType<BattleCannonSequence>().Swarm;
                    while (swarm.AliveCount > 0) swarm.Hit(swarm.GetTarget(0), int.MaxValue);
                }
                yield return null;
            }
            Check(progress.Phase == VoyagePhase.Victory && progress.ClearedWaves == progress.Route.Distance * 2 && progress.BossStarts == 1, "Battle progression failed");
            while (game.ActiveVoyage && Time.realtimeSinceStartup < deadline) yield return null;
            var popup = FindFirstObjectByType<VoyageResultPopup>();
            Check(popup && popup.VisibleRewardCount == rewards.Length, "Actual earned rewards not passed to popup");
            Check(game.CurrentWorldPoint == destination, "Arrival not saved");
            Time.timeScale = 0; yield return WaitForArrival(popup);
            Check(!popup.Map.IsTravelling && popup.Map.Destination == destination, "Result map wrong destination");
            yield return Capture("Battle-Arrival-" + (destination + 1));
            popup.Confirm(); yield return null;
            Check(!FindFirstObjectByType<VoyageResultPopup>() && FindFirstObjectByType<LobbyController>(), "Confirm did not return to lobby");
            game.SendMessage("OpenRouteMap"); selection = FindFirstObjectByType<RouteSelectionPopup>();
            selection.SelectDestination(7);
            Check(selection.CurrentPoint == destination && selection.SelectedRoute.Source == destination, "Next route not from saved arrival");
            selection.Close();
            Result += "Battle to region " + (destination + 1) + ": selected route, one boss, supplied reward list, saved arrival, confirm, next departure PASS\n";
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot("Verification/ResultPopup/" + name + ".png");
            yield return new WaitForEndOfFrame();
        }
        private IEnumerator WaitForArrival(VoyageResultPopup popup)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (popup.Map.IsTravelling && Time.realtimeSinceStartup < deadline) yield return null;
        }
        private static void Check(bool okay, string message) { if (!okay) throw new InvalidOperationException(message); }
        private void Restore()
        {
            if (!started || restored) return; restored = true;
            if (game && game.ActiveVoyage) game.ActiveVoyage.Retreat();
            if (hadPoint) PlayerPrefs.SetInt(SkyTreeGame.WorldPointKey, point); else PlayerPrefs.DeleteKey(SkyTreeGame.WorldPointKey);
            if (hadWorld) PlayerPrefs.SetInt(SkyTreeGame.WorldNumberKey, world); else PlayerPrefs.DeleteKey(SkyTreeGame.WorldNumberKey);
            PlayerPrefs.Save(); Time.timeScale = scale; Application.runInBackground = background;
            if (game) game.SendMessage("Lobby"); if (font) Destroy(font);
        }
        private void OnDestroy() => Restore();
    }
}
#endif
