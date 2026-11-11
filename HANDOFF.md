# 하늘나무 프로젝트 인수인계

2026-11-11 기준. 회사 PC의 새 Codex 대화는 이 문서부터 읽고, 현재 파일과 Git 상태를 확인한 후 작업을 이어간다. 이전 채팅이나 집 PC의 설치 폴더가 회사 PC에도 있다고 가정하지 않는다.

## 회사에서 시작하는 순서

1. 집에서 이 문서와 `Docs/Setup`, `Tools/ComfyUI/Workflows`를 커밋하고 원격 저장소에 Push한다. 회사 소스트리에서 Clone/Pull 후 `main`을 사용한다.
2. Unity Hub에 **6000.3.28f1**과 Android Build Support(SDK/NDK/OpenJDK)를 설치한다. 최종 기준은 `ProjectSettings/ProjectVersion.txt`이다.
3. 프로젝트 루트를 Hub에 등록한다. `Assets/Scenes/SampleScene.unity`를 열고 패키지 복원과 컴파일을 기다린다. `Library`는 다시 생성되므로 가져오지 않는다.
4. [Meshy 및 Unity MCP 설정](Docs/Setup/Meshy-UnityMCP.md)의 Unity MCP 연결부터 구성한다.
5. [ComfyUI 설정](Docs/Setup/ComfyUI.md)에 따라 로컬 설치와 모델을 준비한다. 모델 파일은 Git에 들어 있지 않다.
6. Meshy 키를 회사 PC에 등록하고, 기존 모델을 열어 임포트 상태를 확인한다. 설정 확인만을 위해 유료 모델 생성을 자동 실행하지 않는다.
7. Play에서 타이틀 → 로비 → 모험하기 → 항로 선택 → 출항 → 기존 전투 화면을 확인한다.

회사 PC의 GPU·VRAM·드라이버와 설치 경로는 아직 확인하지 않았다. 집과 다른 GPU라면 집의 PyTorch 버전을 무조건 강제하지 말고 설치 관리자의 호환 구성을 먼저 사용한다.

## 프로젝트와 현재 구현

- Android 세로 화면 게임. 기준 UI 프레임은 400×720이며 안전 영역에 맞춰 표시한다.
- 진입 씬: `Assets/Scenes/SampleScene.unity`.
- 화면 흐름: `Assets/SkyTree/SkyTreeGame.cs`.
- 타이틀: `Assets/SkyTree/TitleScreenView.cs`.
- 로비: `Assets/SkyTree/Prefabs/LobbyView.prefab`, `LobbyCurrencyHud.prefab`.
- 하단 메뉴: 배 관리, 동료 관리, 뽑기, 상점. 보석 없는 공용 `MainTabCommon.prefab` 배경 사용.
- 로비 배·나무·동료 관련 파일은 `Assets/ArtResources`, `Assets/SkyTree/Prefabs`에 있다. 이름이 유사한 시안보다 현재 프리팹의 실제 참조를 우선한다.
- 나무 잎 흔들림, 깃발, 동료 애니메이션, 로비 배경 구름 및 전투 연출이 구현되어 있다. 최신 튜닝은 해당 스크립트·프리팹 값이 기준이다.
- 현재 전투 진입은 `BattleCannonSequence.Build`를 호출하는 기존 구름새/대포 연출이다.

### 항로 선택 팝업

관련 파일:

- `Assets/SkyTree/Prefabs/RouteSelectionPopup.prefab`
- `Assets/SkyTree/RouteSelectionPopup.cs`
- `Assets/SkyTree/RoutePlan.cs`
- `Assets/SkyTree/RouteMapGraphic.cs`
- `Assets/ArtResources/Atlas/RouteMap/`

승인된 디자인은 세피아 구름 지도, 노란 지점 9개, 점선 3×3 구역, 컬러 하늘나무 배 아이콘이다. 지도에 위치 번호를 표시하지 않는다. 위에서부터 번호로 생각할 때 3·4·7 위치에는 섬이 없다. 팝업 가장자리 뒤로 로비가 보여야 한다.

모험하기를 누르면 팝업이 열리며 출항 버튼과 항로는 처음에 비활성이다. 유효한 목적지 선택 시 항로와 목적지 강조, 웨이브 정보, 출항 버튼이 활성화된다. 현재 지점이나 직선 이동이 불가능한 지점은 선택 안내 후 출항을 비활성화한다. 닫았다 다시 열면 선택을 초기화하며 뒤쪽 로비 입력을 차단한다.

### 확정된 이동 규칙

- 내부 인덱스는 0~8. 시작은 **7**, 즉 하단 가운데(사용자가 말한 8번 위치)다.
- 가로·세로·대각선 **직선**으로 한 칸 또는 두 칸 이동한다.
- 한 칸당 일반 웨이브 3개. 두 칸은 일반 웨이브 6개.
- 보스는 이동 마지막에 **한 번만** 등장한다.
- 실패하면 마지막으로 통과한 칸으로 복귀한다. 두 칸 항로에서 앞의 3웨이브를 넘었다면 후반/보스전 실패 시 중간 칸으로 돌아간다. 보스 처치 전에는 최종 도착으로 처리하지 않는다.
- 무역선 지점에 도착하면 로비에 무역선이 나타나는 등 도착지에 따라 로비와 기능이 달라진다.

### 아직 연결하지 않은 작업

**팝업에서 계산한 웨이브 수가 실제 전투를 제어하는 상태는 아니다.** `SkyTreeGame.SelectedRoute`에 계획을 넘긴 뒤 기존 전투 화면으로 들어가는 연결까지만 완료했다.

다음 구현 순서:

1. 선택 항로를 사용하는 전투 진행 상태와 일반 웨이브/최종 보스 구분.
2. 3웨이브 단위의 중간 체크포인트 확정, 패배 복귀 처리.
3. 최종 보스 승리 시 도착 확정과 위치 저장.
4. 도착지에 맞는 로비 구성 및 무역선 기능 표시.

현재 위치는 `PlayerPrefs`의 `SkyTree.World.CurrentPoint`를 읽으며 기본값은 7이다. 현재 코드가 항로 진행에 따라 이 값을 갱신한다고 가정하지 않는다. PlayerPrefs의 재화·위치·로그인 상태는 PC마다 다르며 Git으로 이동하지 않는다.

## 검증과 편집 주의점

- 항로 81쌍 검사, 한 칸/두 칸/대각선, 잘못된 선택, 재열기 초기화, 중복 출항 방지, 실제 플레이 터치 판정과 전투 진입을 확인했다.
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

> 이 프로젝트의 HANDOFF.md, Docs/Setup/ComfyUI.md, Docs/Setup/Meshy-UnityMCP.md를 읽고 현재 Git 상태와 회사 PC 환경을 확인해줘. 집 PC 설정은 참고값으로만 보고 회사 PC의 Unity, ComfyUI, Unity MCP, Meshy 연결을 순서대로 점검해줘. API 키 값은 출력하거나 프로젝트에 저장하지 마. 환경 확인 후에는 항로 선택 팝업까지 구현된 현재 상태에서 작업을 이어갈 것이다. 실제 웨이브·보스·체크포인트·도착지 로비 변경은 아직 미구현이라는 점을 유지해줘.
