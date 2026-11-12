# 회사 PC의 Meshy 및 Unity MCP 설정

2026-11-11 프로젝트 패키지와 집 PC 설정을 기준으로 한다. 여기서 Meshy는 3D 생성 API 서비스이며 로컬 ComfyUI와 별개다. 기존 FBX·텍스처를 사용하는 것만으로 신규 Meshy 생성 요청은 필요하지 않다.

## Unity MCP 연결

1. Unity 6000.3.28f1로 프로젝트를 열고 컴파일 오류가 없는지 확인한다.
2. `Packages/manifest.json`에 포함된 `com.coplaydev.unity-mcp`를 복원한다. Git 패키지이므로 Git이 필요하다. 설치 창이 요청하는 Python 및 uv/uvx 의존성을 준비한다.
3. Unity의 MCP for Unity 창을 열어 로컬 서버와 에디터 연결을 시작한다. 메뉴명은 설치 버전에 따라 확인한다. 서버 시작 명령은 해당 창이 표시하는 값을 사용한다.
4. 집에서는 HTTP MCP 주소 `http://127.0.0.1:8080/mcp`로 Codex가 연결되어 있었다. 회사의 실제 서버 주소와 포트를 확인해서 등록한다.
5. Codex에서 Unity 연결과 활성 프로젝트가 회사의 `P_SkyTree`인지 확인한다. 여러 에디터가 열려 있으면 대상 인스턴스를 먼저 선택한다.

집 Codex 설정의 비밀 없는 최소 예시:

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"
```

회사 Codex의 기존 설정에 **해당 항목만** 추가/수정한다. 집의 전체 config.toml이나 계정 인증 폴더를 덮어쓰지 않는다. 이 TOML만 추가해서 Unity 측 서버가 자동으로 생기는 것은 아니다. 서버 실행과 에디터 연결을 먼저 확인한다.

프로젝트의 패키지 URL은 `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`이고, 확인한 `Packages/packages-lock.json`의 revision은 `81542a7e86292b5c4c21b1782c5af23b567f45ce`였다. 재현 시 lock 파일도 함께 가져온다. 처음부터 패키지를 최신 main으로 갱신하지 않는다. 복원한 버전에서 아래 Asset Generation 기능이 없다면 로컬 캐시와 lock의 실제 패키지 차이를 확인하고 갱신 여부를 결정한다.

참고: [Unity MCP 공식 저장소](https://github.com/CoplayDev/unity-mcp).

## Meshy 키 등록

1. Meshy 계정으로 로그인하고 Developer Platform의 API Keys에서 회사 환경에 사용할 키를 준비한다.
2. Unity MCP 창의 **Asset Generation** 탭에서 **Meshy** 제공자에 키를 등록한다. 실제 패키지 소스가 안내하는 경로다.
3. 대안으로 Unity 프로세스가 읽는 환경 변수 **`MCPFORUNITY_MESHY_API_KEY`**를 사용할 수 있다. 일반적인 `MESHY_API_KEY`와 이름이 다르므로 주의한다. 환경 변수를 변경했다면 Unity/Hub를 재시작해 상속 여부를 확인한다.
4. 연결 도구의 제공자 목록에서 Meshy가 configured로 표시되는지 확인한다. 이는 키가 등록됐다는 뜻이지 잔액이나 API 호출 성공까지 보장하지 않는다.

패키지는 환경 변수 또는 보안 저장소에서 키를 읽는다. Windows 보안 자격 증명은 Git 프로젝트에 포함되지 않는다. **키 값은 문서·스크립트·씬·프리팹·채팅 출력에 넣지 않는다.** 집의 자격 증명을 문서로 추출하지 않았으며 회사에서 따로 등록해야 한다.

Meshy 공식 API는 `Authorization: Bearer <키>`로 인증한다. 키 생성과 관리 방법은 [공식 인증 안내](https://docs.meshy.ai/en/api/authentication)를 따른다. 실제 키를 명령줄에 직접 붙여 기록을 남기는 대신 위 UI 등록 방식을 우선한다.

## 기존 생성 결과

`Assets/ArtResources/Ships/MeshyWoodenShip/`에 다음 파일이 있다:

- `WoodenShip.fbx`
- `WoodenShip_Material.mat`
- `Texture_0_base_color.png`
- `GenerationInfo.json`

`WoodenShip.prefab`과 `New Prefab.prefab`은 UI/리소스 프리팹 분리에 따라 `Assets/Prefabs/Ships/MeshyWoodenShip/`으로 이동했다. 모델·재질·텍스처 참조 GUID는 유지한다.

메타데이터는 제공자 Meshy, 모델 **meshy-6**, 당시 생성 비용 **30 credits**를 기록한다. 이는 과거 한 작업의 기록이며 새 요청의 가격이나 잔액이 아니다. 원본 입력 이미지는 집의 임시 클립보드 파일명으로만 기록되어 있어 회사에서 같은 입력이 있다고 가정하지 않는다. 결과물 자체는 저장소에 있다.

현재 패키지 어댑터는 text-to-3D v2 및 image-to-3D v1 엔드포인트를 사용한다. 새 생성은 Unity MCP의 `generate_model` 도구 스키마와 제공자/모델 목록을 확인한 뒤 수행한다. 모델명·파라미터를 추측해 호출하지 않는다. API 키 등록만을 시험하려고 생성 요청을 하지 않는다.

## 회사에서 완료 판정

- Unity 콘솔에 컴파일 오류가 없다.
- Codex가 Unity 에디터 상태와 활성 씬을 읽을 수 있다.
- Asset Generation 제공자 목록에 Meshy 설정이 표시된다.
- 기존 WoodenShip 프리팹의 메시와 재질을 열어볼 수 있다.
- 새 생성이 필요할 때만 입력, 비용 및 저장 위치를 정하고 실행한다.

키가 없다는 오류는 Asset Generation 등록/환경 변수 상속을 확인한다. 인증 실패는 Meshy 계정의 키 상태를 확인한다. 모델 파일이 있지만 표시되지 않으면 생성 API보다 Unity 임포터·재질 참조부터 확인한다. 키나 계정 문제를 해결하기 위해 프로젝트의 `.meta`를 삭제하지 않는다.
