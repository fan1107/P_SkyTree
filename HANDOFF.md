# 하늘나무 프로젝트 인수인계

2026-11-12 로비 UI·지역 배경·주인공 배 구성 분리 갱신. 환경 설정 기록은 2026-11-11 기준이다. 회사 PC의 새 Codex 대화는 이 문서부터 읽고, 현재 파일과 Git 상태를 확인한 후 작업을 이어간다. 이전 채팅이나 집 PC의 설치 폴더가 회사 PC에도 있다고 가정하지 않는다.

## 회사에서 시작하는 순서

1. 집에서 이 문서와 `Docs/Setup`, `Tools/ComfyUI/Workflows`를 커밋하고 원격 저장소에 Push한다. 회사 소스트리에서 Clone/Pull 후 `main`을 사용한다.
2. Unity Hub에 **6000.3.28f1**과 Android Build Support(SDK/NDK/OpenJDK)를 설치한다. 최종 기준은 `ProjectSettings/ProjectVersion.txt`이다.
3. 프로젝트 루트를 Hub에 등록한다. `Assets/Scenes/SampleScene.unity`를 열고 패키지 복원과 컴파일을 기다린다. `Library`는 다시 생성되므로 가져오지 않는다.
4. [Meshy 및 Unity MCP 설정](Docs/Setup/Meshy-UnityMCP.md)의 Unity MCP 연결부터 구성한다.
5. [ComfyUI 설정](Docs/Setup/ComfyUI.md)에 따라 로컬 설치와 모델을 준비한다. 모델 파일은 Git에 들어 있지 않다.
6. Meshy 키를 회사 PC에 등록하고, 기존 모델을 열어 임포트 상태를 확인한다. 설정 확인만을 위해 유료 모델 생성을 자동 실행하지 않는다.
7. Play에서 타이틀 → 로비 → 모험하기 → 항로 선택 → 출항 → 웨이브/보스 → 도착 월드맵을 확인한다.

회사 PC의 GPU·VRAM·드라이버와 설치 경로는 아직 확인하지 않았다. 집과 다른 GPU라면 집의 PyTorch 버전을 무조건 강제하지 말고 설치 관리자의 호환 구성을 먼저 사용한다.

## 프로젝트와 현재 구현

- 2026-11-12 항해 성공 결과창: `Assets/UIPrefabs/Popups/VoyageResultPopup.prefab`. 황금 나침반, 선택 항로의 배 이동, 데이터 기반 `SrcItemSlot` 목록, 확인 버튼. [구조·보상 계약·미리보기](Docs/VoyageResultPopup.md).
- 보상 지급 규칙은 아직 추가하지 않는다. `VoyageProgress.SetEarnedRewards`로 전달받은 목록만 표시하며, 골드 1,200·목재 8·젬 3은 Editor 미리보기 전용이다.

- Android 세로 화면 게임. 기준 UI 프레임은 400×720이며 안전 영역에 맞춰 표시한다.
- 진입 씬: `Assets/Scenes/SampleScene.unity`.
- 화면 흐름: `Assets/SkyTree/SkyTreeGame.cs`.
- 타이틀: `Assets/SkyTree/TitleScreenView.cs`.
- 로비: Assets/UIPrefabs/Lobby/LobbyRoot.prefab의 LobbyController가 공통 LobbyView, 지역 배경, 로비용 배를 조합한다. 재화 HUD는 Assets/UIPrefabs/HUD/LobbyCurrencyHud.prefab이다.
- 배 프리팹: `Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab`은 로비용, `MainShip_Step01_Adventure.prefab`은 모험·전투용이다. 기존 에셋 GUID를 유지해 로비 및 게임 컨트롤러 참조가 연결되어 있다.
- 하단 메뉴: 배 관리, 동료 관리, 뽑기, 상점. 보석 없는 공용 `MainTabCommon.prefab` 배경 사용.
- 배·나무·동료 프리팹은 `Assets/Prefabs`, 원본 아트는 `Assets/ArtResources`에서 관리한다. 이름이 유사한 시안보다 현재 프리팹의 실제 참조를 우선한다.
- 나무 잎 흔들림, 깃발, 동료 애니메이션, 로비 배경 구름 및 전투 연출이 구현되어 있다. 최신 튜닝은 해당 스크립트·프리팹 값이 기준이다.
- 현재 출항은 선택 항로를 사용하는 `VoyageBattleController`로 연결된다. 일반 웨이브마다 구름새 3마리, 마지막에 보스 1마리를 처치하면 월드맵으로 돌아간다. 항로 없이 호출하는 개발용 전투만 기존 7마리 단일 전투다.

### 프리팹 폴더 관리 (2026-11-12)

- Assets/UIPrefabs/Lobby: LobbyRoot(조합/레이어), LobbyView(공통 UI). HUD, Common, Popups는 기존 역할을 유지한다.
- `Assets/Prefabs/MainShip`: 로비/전투 함선과 ShipFlag. `Trees`: SkyTreeTrunk/SkyTreeFoliage. `Coworker`, `Cannon`, `Enemy`: 동료·대포·적. `Ships/MeshyWoodenShip`: 3D 배 프리팹.
- `Assets/Prefabs/Resources/SkyTree`: 이름으로 로드하는 BattleBackground/EnemyShip. 기존 `Resources.Load("SkyTree/...")` 주소를 유지한다. 원본 이미지·재질은 기존 리소스 폴더에 남아 있다.
- UI 여부는 화면에서 맡는 역할로 분류한다. RectTransform을 사용하는 배·나무도 게임 오브젝트 리소스다. 앞으로 프리팹은 위 두 루트 아래에 생성한다.
- 기존 19개 프리팹 중 13개를 Unity AssetDatabase로 이동했다. 19개 모두 내용·GUID·meta 파일이 같고 씬/로비 테이블의 저장 파일도 그대로 유지했다. 이 기록은 폴더 이동 당시 기준이다. 이후 아래 로비 분리에서 두 UI 변형을 독립 배경으로 전환했다.
- 제작/검증 도구의 경로를 새 위치로 갱신했다. 이동 목록 및 참조 검증 기록은 `Verification/PrefabOrganization/`에 있다.

### 전투 HP와 충돌 (2026-11-12)

- `BattleHealth`는 HP를 0 이상으로 제한한다. 처치된 적과 파손된 함선에 중복 피해가 적용되지 않는다.
- 구름새 HP 144, 포탄 피해 48, 함선 HP 1,000, 충돌 피해 18이 기본값이다. 각각 `SkybirdSwarm`, `BattleCannonSequence`, `BattleShipView`의 직렬화 필드에서 조절한다.
- 포탄은 동료 애니메이션 이벤트에 맞춰 발사되고 도착 시 HP를 감소시킨다. 구름새는 HP 0일 때 HP바를 숨기고 짧게 사라지며, 이후 표적 선택과 충돌에서 제외된다.
- 그리기 순서는 배경 → 구름새/적 HP바 → 배 → 충돌/피해 효과 → 전투 HUD다. 구름새가 배와 나무 앞에 겹쳐 보이지 않게 유지한다.
- 충돌은 `BattleShipView.upperHullEdge`에 정의된 현재 선체 이미지의 상단 윤곽선을 따른다. 새 몸통은 테두리보다 조금 위에서 멈추고 반동 후 돌아간다. 선체 이미지를 교체하면 이 정규화 좌표도 재조정한다.
- `BattleHudView`, `BattleHudGraphic`, `BattleHealthBar`가 하단 금색/청록색 패널, 함선 HP 수치와 초록 체력바, 적 빨간 체력바, 상단 귀환 버튼을 만든다. 최근 피해량은 잠시 금색으로 남고, 함선 HP 25% 이하에서는 빨간색으로 변한다.
- 적 전멸 시 해당 전투의 추가 포격·충돌을 멈추고 다음 웨이브를 준비한다. 함선 HP는 웨이브/보스 사이에 유지하며, 새로운 출항에서 초기화한다. 함선 HP 0이면 긴급 귀환한다.
- 검증 컴포넌트: `Assets/Tests/BattleHealthVerify.cs` (`SampleScene` Play 중 별도 GameObject에 추가 후 `Begin()`). 결과와 캡처는 `Verification/BattleHealth/`에 저장된다.

### 항로 선택 팝업

관련 파일:

- `Assets/UIPrefabs/Popups/RouteSelectionPopup.prefab`
- `Assets/SkyTree/RouteSelectionPopup.cs`
- `Assets/SkyTree/RoutePlan.cs`
- `Assets/SkyTree/RouteMapGraphic.cs`
- `Assets/ArtResources/Atlas/RouteMap/`

승인된 디자인은 세피아 구름 지도, 노란 지점 9개, 점선 3×3 구역, 컬러 하늘나무 배 아이콘이다. 지도에 위치 번호를 표시하지 않는다. 2026-11-12 요청에 따라 3번(오른쪽 위)에 상선, 4번(가운데 왼쪽)에 작은 부유섬을 세피아 판화풍으로 추가했다. 적용 배경은 `CloudChartLandmarks.png`, 원본은 `CloudChart.png`로 보존한다. 7번에는 섬이 없다. 팝업 가장자리 뒤로 로비가 보여야 한다.

모험하기를 누르면 팝업이 열리며 출항 버튼과 항로는 처음에 비활성이다. 현재 지점을 제외한 목적지 선택 시 항로와 목적지 강조, 웨이브 정보, 출항 버튼이 활성화된다. 직선으로 갈 수 없는 목적지는 경유 항로를 설정하며, 같은 목적지를 다시 누르면 두 경유 항로가 번갈아 선택된다. 현재 지점을 누르면 선택과 출항이 비활성화된다. 닫았다 다시 열거나 다른 목적지를 선택하면 첫 번째 항로부터 시작하며 뒤쪽 로비 입력을 차단한다.

### 확정된 이동 규칙

- 내부 인덱스는 0~8. 시작은 **7**, 즉 하단 가운데(사용자가 말한 8번 위치)다.
- 한 번에 가로·세로·대각선으로 인접한 한 칸을 이동하며, 목적지까지 최단 1~2회 이동한다. 직선 목적지는 기존 직선 항로를 유지한다.
- 직선으로 갈 수 없는 목적지는 첫 클릭에 직선 → 대각선, 두 번째 클릭에 대각선 → 직선으로 이동한다. 예: 화면 번호 8→5→3, 다시 3번 클릭 시 8→6→3, 다음 클릭 시 처음 항로로 순환한다. 지도에는 번호를 표시하지 않는다.
- `RoutePlan.Waypoints`는 출발지·경유지·도착지를 내부 인덱스로 순서대로 보관한다. 직선 두 칸도 중간 칸을 포함하며, 출항 시 선택한 경로 전체가 `SelectedRoute`로 전달된다.
- 한 칸당 일반 웨이브 **2개**. 두 칸은 일반 웨이브 **4개**. 최신 요청에 따라 이전 3개/6개 규칙을 대체했다.
- 보스는 이동 마지막에 **한 번만** 등장한다.
- 실패하거나 귀환 버튼을 누르면 마지막으로 통과한 칸으로 복귀한다. 두 칸 항로에서 앞의 2웨이브를 넘었다면 후반/보스전 실패 시 선택한 항로의 중간 칸으로 돌아간다. 보스 처치 전에는 최종 도착으로 처리하지 않는다.
- 도착지별 로비는 테이블에 연결되어 있다. 실제 무역 기능은 후속 작업이다.

### 실제 항해 진행과 도착 (2026-11-12)

- `VoyageProgress`가 일반 웨이브 → 대기 → 다음 웨이브/최종 보스 → 승리/패배/중단 상태를 관리한다. `VoyageBattleController`는 현재 적이 전멸하면 2초 뒤 다음 적을 생성한다. 보스는 전체 일반 웨이브가 끝난 뒤 한 번만 등장한다.
- `BattleCannonSequence.StartEncounter`는 기존 무리와 효과를 정리하고 다음 무리를 만든다. 함선·동료를 다시 생성하지 않아 체력과 포격 애니메이션 연결이 유지된다.
- 보스는 현재 구름새를 1.85배로 확대한 우두머리이며 HP 720, 충돌 피해 36이다. 전용 이름/HP바와 최종 보스 안내를 표시한다. 전용 보스 아트는 별도로 제작하지 않았다.
- 중간 칸의 2웨이브를 완료하면 체크포인트를 저장한다. 보스 처치 후에만 목적지를 저장하며, 결과 안내 후 월드맵을 자동으로 연다. 배 아이콘은 실제 선택한 경유점을 따라 이동하고 최종 위치에 남는다. 닫았다 열거나 다시 실행해도 저장 위치에서 다음 항로를 선택한다.
- 위치 키는 `PlayerPrefs`의 `SkyTree.World.CurrentPoint`, 기본값은 7이다. 재화·위치·로그인 상태는 PC마다 다르며 Git으로 이동하지 않는다.
- 검증 컴포넌트: `Assets/Tests/VoyageProgressVerify.cs` (`SampleScene` Play 중 별도 GameObject에 추가 후 `Begin()`). 전체 항로 규칙, 실제 자동 포격으로 한 칸/두 칸 승리, 보스 1회, 체력 유지, 중간/보스 패배 복귀, 귀환 버튼, 지도 재열기와 다음 출항 위치를 검증한다. 검증 후 기존 위치·시간 배속·백그라운드 설정을 복구한다. 결과/캡처는 `Verification/Voyage/`.

### 로비 UI·배경·배 분리 (2026-11-12)

- 진입은 SkyTreeGame.Lobby → LobbyRoot 생성 → WorldLobbyTable.Resolve(world, region) → LobbyController.Open 순서다.
- LobbyRoot의 순서는 BackgroundRoot → ShipRoot → ForegroundRoot → UIRoot → PopupRoot다. 공통 UI는 중첩 프리팹이며 배경과 배를 각각 한 개씩 생성한다. 로비 종료 시 루트 전체를 정리한다.
- Assets/UIPrefabs/Lobby/LobbyView.prefab은 재화, 월드·지역명, 메뉴와 버튼만 담당한다. 배·배경·구름·배 흔들림·동료 배회 컴포넌트를 포함하지 않는다.
- Assets/Prefabs/Lobby/Backgrounds에는 LobbyBackground_Default, LobbyBackground_SkyMerchant, LobbyBackground_LoneSkyTreeIsland가 있다. 기존 두 UI 변형은 GUID를 유지한 독립 배경 프리팹으로 전환·이동했다.
- LobbyBackgroundView의 Artwork는 기존 텍스처·UV·배치를 보존한다. 구름 4개도 프리팹에 저장하며 LobbyPassingClouds는 이동만 담당한다. ShipAnchor의 위치·크기·회전·배율로 주인공 배 배치를 조절한다.
- 배경의 Sunlight overlay는 실행 시 ForegroundRoot로 옮겨 배 위·UI 아래에 그린다. 빛 설정은 배경 프리팹에서 관리한다.
- MainShip_Step01_Lobby.prefab의 LobbyShipView가 부유 연출을, 같은 루트의 LobbyCaptainWander 두 개가 선장·항해사 배회를 담당한다. 나무·깃발·대포의 중첩 프리팹 연결을 보존한다.
- 배 외형은 LobbyRoot.shipPrefab에서 선택한다. 지역 테이블에는 UI나 주인공 배를 넣지 않는다. 성장 단계는 기존 ShipTreeGrowth가 소유하고 UI는 콜백으로 값을 읽는다. 전투용 MainShip_Step01_Adventure와 전투 규칙은 별개다.
- SampleScene의 SkyTreeGame.lobbyRootPrefab에 조합 프리팹이 연결된다. 저장된 씬은 로비 참조 한 항목만 변경하며 미저장 씬 전체를 저장하지 않았다.
- BattleShipBuilder, CrewDeploymentSetup, FlagSpriteBuilder는 로비 배 프리팹을 직접 읽는다. Tools/ArtImport의 이전 일회성 스크립트는 새 구조용 도구가 아니므로 그대로 실행하지 않는다.

### 월드별 로비 테이블 (2026-11-12)

- Assets/SkyTree/Data/WorldLobbyTable.asset의 기존 GUID와 씬 참조를 유지한다.
- 월드 1~9 × 지역 1~9의 81칸을 유지한다. 지역마다 표시 이름·배경 프리팹·WORLD 글자색을 설정한다. 월드 2~9 등 배경 미지정 칸은 명시된 기본 지역을 공유한다.
- Resolve(worldNumber, regionNumber)는 둘 다 1부터 시작한다. 항로/저장 지점은 0부터 시작하므로 Lobby에서 CurrentWorldPoint + 1을 전달한다. 잘못된 범위는 null을 반환한다.
- 월드 저장 키는 SkyTree.World.CurrentWorld(기본 1), 지역 저장 키는 SkyTree.World.CurrentPoint(기본 7)다. 월드 해금/전환 기능은 아직 없다.
- 월드 1 지역 3: LobbyBackground_SkyMerchant / 하늘나무 상선. 지역 4: LobbyBackground_LoneSkyTreeIsland / 외딴 하늘나무 섬. 나머지: LobbyBackground_Default / 바람초원 정박지. 지역별 분기는 게임 코드에 넣지 않는다.
- Assets/Tests/WorldLobbyTableVerify.cs는 지역 9곳과 월드 9, UI·배·구름 단일 인스턴스, 레이어, 독립 연출, 상선/섬 도착, 부유섬 체크포인트 패배 복귀를 검사한다. 원래 저장 위치·시간 배속·백그라운드 설정을 복구한다. 결과와 캡처는 Verification/WorldLobby에 있다.

### 아직 연결하지 않은 작업

1. 상선 도착 후 실제 무역 기능과 월드 해금/전환.
2. 실제 항해 완료 보상 연결과 적/보스 종류 확장.

## 검증과 편집 주의점

- 기존 검증은 항로 81쌍, 한 칸/두 칸/대각선, 재열기 초기화, 중복 출항 방지, 실제 플레이 터치 판정과 전투 진입을 포함한다. 경유 항로 변경 후의 재검증 메뉴는 `SkyTree/Verify Route Selection`, 결과는 `Verification/RouteSelection-results.txt`이다.
- 상세 기록: `Verification/RoutePopup-Notes.txt`. 캡처 이미지는 ignore 대상이라 회사 Clone에는 없을 수 있다.
- 에디터가 백그라운드에서 프레임 갱신을 멈추면 Canvas depth가 -1이고 터치 검사도 실패할 수 있다. Game View에 프레임이 렌더링된 뒤 재검증한다.
- `SkyTree/Build Route Selection Popup`과 `SkyTree/Apply Lobby UI Renewal` 메뉴는 프리팹을 다시 쓰는 제작 도구다. 수작업 수정 후 무심코 재실행하지 않는다.
- `VoyageBattle.cs`에는 이전 프로토타입 전투 로직이 남아 있다. 현재 진입 코드와 연결 여부를 확인하고 수정한다.
- 일부 제작 도구는 `D:/MyProject/P_SkyTree`, `D:/ComfyUI`, 집 PC 임시 이미지 경로를 하드코딩했다. 특히 `Tools/ArtImport`의 일회성 스크립트를 바로 실행하지 않는다.

## Git와 파일 정리

확인 당시 브랜치는 `main`, 최신 커밋은 `0cf56a3` (`미사용리소스삭제`)였다. 이후 상태는 `git status`와 `git log -1`로 다시 확인한다. 원격 Push 완료 여부는 이 문서에서 보장하지 않는다.

`Assets`와 모든 `.meta`, `Packages`, `ProjectSettings`를 함께 관리한다. 루트 PNG 43개는 시안/검수용이라 삭제했고 루트 PNG 재생성분은 ignore한다. `Library`, `Temp`, `Logs`, `UserSettings`, 루트 `Backups`, 검수 이미지도 ignore 대상이다. 삭제한 이미지는 이전 커밋 이력에는 남아 있다.

앞으로 게임용 최종 리소스는 `Assets/ArtResources`, 검수 캡처는 `Verification` 하위에 둔다. 프로젝트 루트에 PNG를 쌓지 않는다. `.meta`를 재생성하거나 참조 GUID를 무작정 바꾸지 않는다.

## 새 Codex 대화에 붙여넣을 요청

> 이 프로젝트의 HANDOFF.md, Docs/Setup/ComfyUI.md, Docs/Setup/Meshy-UnityMCP.md를 읽고 현재 Git 상태와 회사 PC 환경을 확인해줘. 집 PC 설정은 참고값으로만 보고 회사 PC의 Unity, ComfyUI, Unity MCP, Meshy 연결을 순서대로 점검해줘. API 키 값은 출력하거나 프로젝트에 저장하지 마. 항로 선택, 한 칸당 2웨이브와 최종 보스 1회, 체크포인트·목적지 저장 및 월드맵 배 이동이 구현되어 있다. 월드별 로비 테이블에서 월드 1의 지역 3은 상선, 지역 4는 부유섬, 나머지는 기본 로비로 연결한다. 실제 무역 기능, 월드 해금/전환과 항해 보상은 후속 작업이다.
