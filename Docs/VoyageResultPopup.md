# 항해 성공 팝업

## 저장 위치

- `Assets/UIPrefabs/Popups/VoyageResultPopup.prefab`: 모달 배경, 황금 나침반 타이틀, 도착 지도, 보상 스크롤, 확인 버튼.
- `Assets/UIPrefabs/Common/SrcItemSlot.prefab`: 재사용 아이템 슬롯. 아이콘·수량·배경·클릭 영역을 Inspector에서 연결한다.
- `Assets/ArtResources/Atlas/Popup/`: 나침반 장식, 9-slice 양피지 패널, `Popup.spriteatlas`.
- `Assets/Prefabs/UI/Icon/`: 요청한 예외 구조로 골드·목재·젬의 투명 512×512 PNG와 각각의 512×512 UI Image 프리팹을 함께 둔다. 슬롯은 동일 Sprite를 직접 참조한다.
- `Assets/SkyTree/Data/Items/`: Gold, Wood, Gem 아이템 정의. ID·이름·Sprite·슬롯 배경 색상을 데이터로 관리한다.

지도와 배 마커는 기존 RouteMap 리소스를, 확인 버튼은 기존 Lobby 공통 버튼 Sprite를 공유한다. 기존 에셋을 옮기거나 복제하지 않는다.

## 실행 흐름

선택한 항로의 마지막 보스 처치 → 도착 지점 저장 → 해당 지역 로비 구성 → `VoyageResultPopup` 표시 → 선택한 경유 지점을 따라 배 이동 → 확인 → 도착 지역 로비.

이동은 `Time.unscaledTime`을 사용한다. 확인을 먼저 눌러도 이미 저장된 도착 지점은 유지된다. 패배·중단은 기존 체크포인트 복귀 지도 흐름을 유지한다. 성공 팝업은 출항 버튼을 갖지 않으며 확인 뒤 로비의 모험하기를 통해 다음 항로를 선택한다.

## 보상 데이터 계약

사용자 결정: 이번에는 실제 보상 데이터의 표시 구조만 구현한다. 지급 규칙·인벤토리 갱신은 추가하지 않는다.

- 보상 제공자가 `VoyageProgress.SetEarnedRewards(IReadOnlyList<ItemReward>)`에 확정된 습득량을 전달한다.
- 현재 전투에는 보상 제공자가 없으므로 기본 목록은 비어 있다. 빈 목록은 “습득한 아이템이 없습니다”로 표시한다.
- `ItemReward`는 ItemDefinition과 양수 long 수량이다. 보유량이 아닌 이번 습득량을 담는다. 유효하지 않은 항목은 표시하지 않는다.
- `SetEarnedRewards`는 전달된 목록을 복사한다. 표시할 목록은 `EarnedRewards`에서 읽는다.
- `VoyageResultPopup.Open`은 이 목록을 받아 슬롯을 생성·재사용한다. 3열 GridLayoutGroup/ScrollRect로 개수에 맞게 확장하고 갱신 시 스크롤을 맨 위로 되돌린다.
- `UIItemSlot.Bind`는 이전 아이템·수량·콜백을 초기화한다. 콜백이 없으면 표시 전용이다. 수량은 천 단위 쉼표를 사용하며 긴 수량은 글자 크기를 조정한다.
- 확인은 한 번만 호출되고 재화 지급 동작은 없다.

새 아이템은 ItemDefinition 에셋을 추가하고 해당 Sprite를 아틀라스에 등록한다. 팝업이나 슬롯에 아이템 종류별 분기를 추가할 필요가 없다. 배경 색상은 ItemDefinition.frameTint에서 확장할 수 있으며, 픽셀워쉽의 게임 전용 등급·서버 의존성은 가져오지 않았다.

## 미리보기와 검증

Unity 메뉴 `SkyTree > Preview > Voyage Success (Sample Rewards)`는 Play로 진입해 예시 골드 1,200·목재 8·젬 3을 표시한다. 이는 Editor 전용 코드이고 재화/월드 위치를 저장하지 않는다. 전투 중에는 실행하지 않는다.

`VoyageResultVerify`는 0/1/3/7개 표시, 스크롤, 슬롯 재사용, 잘못된 항목, 콜백 교체, 일시정지 중 이동, 확인 중복 호출, 실제 1칸/2칸 전투 연결 및 다음 출항 위치를 검사한다. 종료 시 PlayerPrefs/시간 배율을 검사 전 값으로 복원한다. 결과와 화면 캡처는 `Verification/ResultPopup`에 기록한다.

2026-11-12 Unity Play 검증: `VoyageResultVerify` 전체 통과. `WorldLobbyTableVerify`의 월드 1 모든 지역·월드 9 기본 배경·상선/부유섬 승리 복귀·중간 지역 패배 복귀도 통과했다. 세 아이콘은 실제 PNG 512×512/투명 모서리/Single Sprite이며 신규 프리팹 5개의 컴포넌트 및 직렬화 참조에 누락이 없다. 기존 저장 씬은 `resultPopupPrefab` 참조 한 줄만 추가되었다.

프리팹은 Inspector에서 직접 편집한다. `VoyageResultBuilder`는 최초 생성용이며 기존 프리팹이 있으면 덮어쓰지 않는다. 씬 전체 저장이나 기존 UI 재생성을 실행하지 않는다.

이미지는 내장 image_gen으로 제작했다. 프롬프트 원문은 `Verification/ResultPopup/ArtPrompts.json`에 보관한다. 아이콘은 생성 후 Unity로 알파를 유지하며 최종 512×512 PNG로 규격화했다. 픽셀워쉽은 UIItemSlot.cs와 SrcItemSlot 구조를 읽는 참고 대상으로만 사용했다.
