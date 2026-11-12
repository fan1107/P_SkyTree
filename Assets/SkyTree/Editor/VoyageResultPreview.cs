using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Editor
{
    public static class VoyageResultPreview
    {
        const string Pending = "SkyTree.ResultPreview.Pending";
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayState;
            EditorApplication.playModeStateChanged += OnPlayState;
        }
        static void OnPlayState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false); EditorApplication.delayCall += Show;
        }
        [MenuItem("SkyTree/Preview/Voyage Success (Sample Rewards)")]
        public static void Show()
        {
            if (!EditorApplication.isPlaying) { SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode(); return; }
            var game = Object.FindFirstObjectByType<SkyTreeGame>();
            if (!game || game.ActiveVoyage) { Debug.LogWarning("로비에서 결과 미리보기를 실행하세요."); return; }
            game.SendMessage("Lobby");
            var lobby = Object.FindFirstObjectByType<LobbyController>();
            var popup = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(VoyageResultBuilder.PopupPath), lobby.PopupRoot, false).GetComponent<VoyageResultPopup>();
            RoutePlan.TryCreate(7, 2, out var route);
            var progress = new VoyageProgress(route);
            while (!progress.Finished) { progress.ClearEncounter(); progress.BeginNextEncounter(); }
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 32);
            var rewards = new[] {
                new ItemReward(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SkyTree/Data/Items/Gold.asset"), 1200),
                new ItemReward(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SkyTree/Data/Items/Wood.asset"), 8),
                new ItemReward(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SkyTree/Data/Items/Gem.asset"), 3) };
            popup.Open(font, progress, rewards, () => { Object.Destroy(popup.gameObject); Object.Destroy(font); });
            Debug.Log("결과창 미리보기: 수량은 예시이며 재화와 월드 위치를 변경하지 않습니다.");
        }
    }
}
