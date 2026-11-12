using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SkyTree
{
    public sealed class SkyTreeGame : MonoBehaviour
    {
        private enum Page { Title, Lobby, Battle, Result }
        private static readonly Color Ink = new Color32(29, 54, 62, 255);
        private static readonly Color Cream = new Color32(255, 246, 219, 255);
        private static readonly Color Gold = new Color32(244, 192, 99, 255);
        private static readonly Color Green = new Color32(116, 186, 120, 255);
        private static readonly Color Panel = new Color32(25, 49, 54, 242);
        private RectTransform frame, page, ship, bird, shot, enemyBar, hullBar;
        private Text waveText, hullText, repairText;
        private Button repairButton;
        [SerializeField] private LobbyController lobbyRootPrefab;
        private LobbyController lobby;
        [SerializeField] private WorldLobbyTable worldLobbyTable;
        [SerializeField] private RouteSelectionPopup routePopupPrefab;
        private RouteSelectionPopup routePopup;
        public RoutePlan SelectedRoute { get; private set; }
        public VoyageBattleController ActiveVoyage { get; private set; }
        public const string WorldPointKey = "SkyTree.World.CurrentPoint";
        public const string WorldNumberKey = "SkyTree.World.CurrentWorld";
        public int CurrentWorldNumber => Mathf.Clamp(PlayerPrefs.GetInt(WorldNumberKey, 1), 1, WorldLobbyTable.WorldCount);
        public int CurrentWorldPoint => Mathf.Clamp(PlayerPrefs.GetInt(WorldPointKey, 7), 0, 8);
        [SerializeField] private BattleShipView battleShipPrefab;
        [SerializeField] private RawImage skybirdPrefab;
        [SerializeField] private RawImage skybirdMotherPrefab;
        [SerializeField] private Sprite titleButtonSprite;
        private Font font;
        private Texture2D skyArt, shipArt, birdArt;
        private VoyageBattle battle;
        private Page current;
        private int gold;
        private const string GoldKey = "SkyTree.Guest.Gold";
        private Rect area;
        private int lastWidth, lastHeight;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            skyArt = Resources.Load<Texture2D>("SkyTree/Sky");
            shipArt = Resources.Load<Texture2D>("SkyTree/Airship");
            birdArt = skybirdPrefab ? skybirdPrefab.texture as Texture2D : null;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans KR", "Droid Sans Fallback", "Arial" }, 32);
            gold = PlayerPrefs.GetInt(GoldKey, 0);
            var canvas = new GameObject("Portrait Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var backdrop = Node("Sky surround", canvas.transform);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = Ink;
            frame = Node("Safe portrait", canvas.transform);
            if (!EventSystem.current)
            {
                var input = new GameObject("Touch input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.transform.SetParent(transform, false);
            }
            Layout();
            Title();
        }

        private static RectTransform Node(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private void Layout()
        {
            area = Screen.safeArea;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float scale = Mathf.Max(.01f, Mathf.Min(area.width / 400f, area.height / 720f));
            // Scale the Canvas so dynamic fonts rasterize at the device resolution.
            // Scaling only the frame magnifies low-resolution glyphs on mobile.
            frame.GetComponentInParent<Canvas>().scaleFactor = scale;
            frame.localScale = Vector3.one;
            frame.sizeDelta = new Vector2(400, 720);
            frame.anchoredPosition = (area.center - new Vector2(Screen.width, Screen.height) * .5f) / scale;
        }

        private RectTransform Box(string name, float x, float y, float w, float h)
        {
            var rect = Node(name, page);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x + w / 2, -y - h / 2);
            return rect;
        }

        private RectTransform Block(string name, float x, float y, float w, float h, Color color)
        {
            var rect = Box(name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private RectTransform Art(string name, Texture2D tex, float x, float y, float w, float h, bool cover = false)
        {
            var rect = Box(name, x, y, w, h);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = tex;
            image.raycastTarget = false;
            if (tex)
            {
                float aspect = (float)tex.width / tex.height;
                if (cover)
                {
                    float u = Mathf.Min(1, w / h / aspect);
                    float v = Mathf.Min(1, aspect / (w / h));
                    image.uvRect = new Rect((1-u)/2, (1-v)/2, u, v);
                }
                else rect.sizeDelta = new Vector2(Mathf.Min(w, h * aspect), Mathf.Min(h, w / aspect));
            }
            return rect;
        }

        private Text Label(string value, float x, float y, float w, float h, int size, Color color,
                           TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
        {
            var label = Box(value, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = align;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private Button ActionButton(string title, float x, float y, float w, Action action, out Text caption)
        {
            var rect = Block(title, x, y, w, 52, Gold);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            caption = Label(title, x, y, w, 52, 18, Ink, bold:true);
            return button;
        }

        private void Open(Page next)
        {
            if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            routePopup = null;
            lobby = null;
            current = next;
            ship = null;
            page = Node(next.ToString(), frame);
            page.sizeDelta = frame.sizeDelta;
            // 로비의 그림·구름·배·UI는 저장된 조합 프리팹이 담당한다.
            if (next == Page.Lobby) return;
            if (next == Page.Battle)
            {
                Instantiate(Resources.Load<BattleBackgroundView>("SkyTree/BattleBackground"), page, false);
                return;
            }
            var background = next switch
            {
                Page.Title => Resources.Load<Texture2D>("SkyTree/Title/TitleArtwork"),
                _ => skyArt
            };
            Art("Background", background, 0, 0, 400, 720, true);
        }

        private void Title()
        {
            Open(Page.Title);
            // Keep the approved key art fixed; text and controls remain real, accessible UI.
            page.gameObject.AddComponent<TitleScreenView>().Build(font, PlayerPrefs.HasKey(GoldKey), Login, titleButtonSprite);
        }

        private void Login()
        {
            PlayerPrefs.SetInt(GoldKey, gold);
            PlayerPrefs.Save();
            Lobby();
        }

        private void Lobby()
        {
            Open(Page.Lobby);
            if (!worldLobbyTable || !lobbyRootPrefab)
                throw new InvalidOperationException("SkyTreeGame의 로비 루트와 지역 테이블 연결이 필요합니다.");
            lobby = Instantiate(lobbyRootPrefab, page, false);
            lobby.Open(worldLobbyTable.Resolve(CurrentWorldNumber, CurrentWorldPoint + 1),
                font, PlayerPrefs.GetInt("SkyTree.Guest.Energy", 30), gold,
                PlayerPrefs.GetInt("SkyTree.Guest.Gems", 0), OpenRouteMap, CurrentWorldNumber);
        }

        private void OpenRouteMap()
        {
            if (!routePopupPrefab) { Debug.LogError("Route selection prefab is missing."); return; }
            if (!routePopup) routePopup = Instantiate(routePopupPrefab, lobby ? lobby.PopupRoot : page, false);
            routePopup.Open(font, CurrentWorldPoint, BeginVoyage);
        }

        public void BeginVoyage(RoutePlan plan)
        {
            if (plan == null || plan.Source != CurrentWorldPoint) return;
            if (ActiveVoyage && !ActiveVoyage.Progress.Finished) return;
            SelectedRoute = plan;
            BeginBattle();
        }

        private void BeginBattle()
        {
            battle = null;
            Open(Page.Battle);
            ActiveVoyage = null;
            var player = Instantiate(battleShipPrefab, page, false);
            var sequence = page.gameObject.AddComponent<BattleCannonSequence>();
            sequence.Build(player, skybirdPrefab, font, SelectedRoute == null ? 7 : VoyageBattleController.EnemiesPerWave, skybirdMotherPrefab);
            if (SelectedRoute != null)
            {
                ActiveVoyage = page.gameObject.AddComponent<VoyageBattleController>();
                ActiveVoyage.Initialize(SelectedRoute, sequence, player);
                ActiveVoyage.CheckpointReached += SaveWorldPoint;
                ActiveVoyage.Completed += CompleteVoyage;
            }
            var hud = BattleHudView.Node("Battle HUD", page, Vector2.zero, new Vector2(400, 720)).gameObject.AddComponent<BattleHudView>();
            hud.Build(font, player, sequence, LeaveBattle, ActiveVoyage);
        }

        private void LeaveBattle()
        {
            if (ActiveVoyage) ActiveVoyage.Retreat();
            else Lobby();
        }

        private void SaveWorldPoint(int point)
        {
            PlayerPrefs.SetInt(WorldPointKey, Mathf.Clamp(point, 0, 8));
            PlayerPrefs.Save();
        }

        private void CompleteVoyage(VoyageProgress progress)
        {
            SaveWorldPoint(progress.ReachedPoint);
            ActiveVoyage = null;
            SelectedRoute = null;
            Lobby();
            OpenRouteMap();
            routePopup.ShowVoyageResult(progress);
        }
        private RectTransform Meter(string name, int x, int y, int width, Color color)
        {
            Block(name + " track", x, y, width, 7, Ink);
            var bar = Block(name, x, y, width, 7, color);
            bar.pivot = new Vector2(0, .5f);
            bar.anchoredPosition -= Vector2.right * width / 2;
            return bar;
        }

        private void RefreshBattle()
        {
            waveText.text = battle.Wave == 3 ? "03 / 03     우두머리 출현" : $"0{battle.Wave} / 03     구름새 접근";
            hullText.text = $"선체 내구도     {battle.Hull} / 100";
            hullBar.localScale = new Vector3(battle.Hull / 100f, 1, 1);
            enemyBar.localScale = new Vector3((float)battle.Enemy / battle.EnemyCapacity, 1, 1);
            repairButton.interactable = battle.Hull < 100 && battle.RepairCooldown <= 0;
            repairText.text = battle.RepairCooldown > 0 ? $"정비 중  {Mathf.CeilToInt(battle.RepairCooldown)}초" : "긴급 수리  +25";
            float t = Time.time;
            bird.localScale = Vector3.one * (battle.Wave == 3 ? 1.22f : 1f);
            bird.anchoredPosition = new Vector2(279 + 16 * Mathf.Sin(t * 1.5f), -207 + 8 * Mathf.Sin(t * 2.4f));
            shot.gameObject.SetActive(battle.ShotAge < .3f);
            shot.anchoredPosition = Vector2.Lerp(new Vector2(177, -414), bird.anchoredPosition, battle.ShotAge / .3f);
        }

        private void Result()
        {
            bool won = battle.Hull > 0;
            if (won)
            {
                gold += VoyageBattle.Reward;
                PlayerPrefs.SetInt(GoldKey, gold);
                PlayerPrefs.Save();
            }
            Open(Page.Result);
            ship = Art("Returning airship", shipArt, 32, 100, 336, 320);
            Block("Voyage log", 24, 418, 352, 264, Panel);
            Label("V O Y A G E   L O G", 24, 438, 352, 26, 13, Gold, bold:true);
            Label(won ? "항로 확보" : "긴급 귀환", 24, 468, 352, 55, 32, Cream, bold:true);
            Label(won ? $"항해 보상   +{VoyageBattle.Reward} 골드" : "선체를 정비하고 다시 출항하세요", 32, 530, 336, 42, 17, Cream);
            ActionButton("비행선으로 돌아가기", 48, 603, 304, Lobby, out _);
        }

        private void Update()
        {
            if (area != Screen.safeArea || lastWidth != Screen.width || lastHeight != Screen.height) Layout();
            if (current == Page.Battle && battle != null)
            {
                battle.Tick(Time.deltaTime);
                if (battle.Finished) { Result(); return; }
                RefreshBattle();
            }
            if (ship)
            {

                var pos = ship.anchoredPosition;
                float baseY = current == Page.Title ? -380 : current == Page.Battle ? -441 : -260;
                ship.anchoredPosition = new Vector2(pos.x, baseY + Mathf.Sin(Time.time * .6283f) * 2.8f);
            }
        }
    }
}

