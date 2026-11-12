using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class BattleHudView : MonoBehaviour
    {
        private BattleShipView ship;
        private BattleCannonSequence battle;
        private VoyageBattleController voyage;
        private Text healthText, status, heading;
        public BattleHealthBar ShipBar { get; private set; }

        public void Build(Font font, BattleShipView player, BattleCannonSequence sequence, Action returnToLobby, VoyageBattleController journey = null)
        {
            ship = player; battle = sequence; voyage = journey;
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(400, 720);
            var panel = Node("Ship health panel", root, new Vector2(0, -289), new Vector2(364, 80));
            panel.gameObject.AddComponent<BattleHudGraphic>().raycastTarget = false;
            var shield = Node("Ship emblem", panel, new Vector2(-151, 13), new Vector2(23, 28));
            var emblem = shield.gameObject.AddComponent<BattleHudGraphic>(); emblem.emblem = true; emblem.raycastTarget = false;
            Label("Hull label", panel, font, "함선 내구도", new Vector2(-76, 13), new Vector2(118, 26), 16, TextAnchor.MiddleLeft);
            healthText = Label("Hull HP", panel, font, "", new Vector2(102, 13), new Vector2(132, 26), 18, TextAnchor.MiddleRight);
            ShipBar = Node("Hull health bar", panel, new Vector2(0, -18), new Vector2(330, 21)).gameObject.AddComponent<BattleHealthBar>();
            ShipBar.Bind(player.Health, true);

            var back = Node("Return to lobby", root, new Vector2(-150, 329), new Vector2(76, 30));
            var plate = back.gameObject.AddComponent<BattleHudGraphic>();
            var button = back.gameObject.AddComponent<Button>(); button.targetGraphic = plate;
            button.onClick.AddListener(() => returnToLobby());
            Label("Return label", back, font, voyage ? "← 귀환" : "← 로비", Vector2.zero, new Vector2(68, 24), 14);
            heading = Label("Battle title", root, font, "구름새 습격", new Vector2(0, 306), new Vector2(240, 36), 25);
            status = Label("Battle status", root, font, "", new Vector2(0, 277), new Vector2(352, 24), 14);
            status.color = new Color32(244, 192, 99, 255);
            ship.Health.Changed += Refresh;
            battle.EnemiesChanged += Refresh;
            if (voyage) voyage.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            healthText.text = $"{ship.Health.Current:N0} / {ship.Health.Maximum:N0}";
            var swarm = battle.Swarm;
            if (voyage)
            {
                var progress = voyage.Progress;
                switch (progress.Phase)
                {
                    case VoyagePhase.NormalWave:
                        heading.text = "구름새 습격";
                        status.text = $"{progress.WaveNumber} / {progress.Route.WaveCount} 웨이브 · 남은 적 {swarm.AliveCount}";
                        break;
                    case VoyagePhase.Intermission:
                        heading.text = "웨이브 클리어";
                        status.text = progress.ClearedWaves == progress.Route.WaveCount ? "최종 보스가 다가옵니다" : $"{progress.ClearedWaves} / {progress.Route.WaveCount} 완료 · 다음 웨이브 준비";
                        break;
                    case VoyagePhase.Boss:
                        heading.text = "최종 보스"; status.text = "보스를 처치하면 목적지에 도착합니다";
                        break;
                    case VoyagePhase.Victory:
                        heading.text = "항해 완료"; status.text = "목적지로 이동합니다";
                        break;
                    default:
                        heading.text = "긴급 귀환"; status.text = "마지막 통과 지점으로 돌아갑니다";
                        break;
                }
                return;
            }
            status.text = !ship.Health.IsAlive ? "선체 파손 · 로비로 귀환하세요"
                : swarm.AliveCount == 0 ? "적 전멸 · 전투 승리" : $"SKYBIRD × {swarm.AliveCount}   ·   충돌 공격";
        }

        private void OnDestroy()
        {
            if (ship && ship.Health != null) ship.Health.Changed -= Refresh;
            if (battle) battle.EnemiesChanged -= Refresh;
            if (voyage) voyage.Changed -= Refresh;
        }

        public static RectTransform Node(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        public static Text Label(string name, Transform parent, Font font, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Node(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold; label.alignment = alignment;
            label.color = new Color32(255, 242, 212, 255); label.raycastTarget = false;
            return label;
        }
    }
}
