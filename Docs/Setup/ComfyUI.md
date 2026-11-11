# 회사 PC의 ComfyUI 설정

이 문서는 2026-11-11 집 PC에서 확인한 설치와 프로젝트 워크플로를 기준으로 한다. 서버 API는 문서 작성 중 읽기 전용으로 확인했고 신규 이미지를 생성하지 않았다.

## 확인된 집 PC 환경

| 항목 | 값 |
|---|---|
| 앱 | Comfy Desktop, 로컬 Standalone 설치 |
| 서버 | `http://127.0.0.1:8188` |
| ComfyUI | `0.45.2`, commit `6e8488ef986067631e9c86e2a14360644b105663` |
| 서버 Python | `3.13.12` |
| PyTorch | `2.12.1+cu130` |
| 환경 식별자 | `local-desktop2-standalone` |
| 집 GPU | RTX 4070 SUPER, 약 12GB VRAM |
| 앱 실행 파일 | `D:/ComfyUI/Comfy Desktop/Comfy Desktop.exe` |
| 설치 루트 | `D:/Comfy-Desktop/ComfyUI-Installs/ComfyUI` |
| 실제 코드/노드 폴더 | 위 루트의 `ComfyUI/`, `ComfyUI/custom_nodes/` |
| 공용 데이터 | `D:/Comfy-Desktop/ComfyUI-Shared/{models,input,output}` |

Desktop의 최초 설치 패키지 표기는 v0.42.1-env1이지만, 실제 실행 서버 버전은 0.45.2다. 회사에서 앱 런처 버전과 서버 버전을 혼동하지 않는다. 위 경로를 동일하게 만들 필요는 없다.

## 설치와 모델 준비

1. [공식 Windows Desktop 안내](https://docs.comfy.org/installation/desktop/windows)에 따라 로컬 설치를 만든다. 설치 관리자가 생성한 Python 환경을 사용한다.
2. 회사 GPU와 VRAM을 확인한다. 서버가 정상 기동하는 구성에서 시작하고 집의 torch/CUDA 패키지를 임의로 덮어쓰지 않는다.
3. 아래 모델을 회사 설치가 사용하는 모델 폴더에 배치하거나, Desktop 모델 경로 설정에서 기존 모델 폴더를 연결한다.
4. 모델 목록을 다시 불러오고 서버 `/object_info`에 필요한 로더와 모델명이 나타나는지 확인한다.

| 모델 폴더 | 정확한 파일명 | 집 파일 크기(bytes) | 용도 |
|---|---|---:|---|
| `diffusion_models` | `krea2_turbo_int8_convrot.safetensors` | 13,492,686,496 | Krea2 Turbo INT8 본체 |
| `text_encoders` | `qwen3vl_4b_fp8_scaled.safetensors` | 5,242,467,968 | CLIPLoader, type=`krea2` |
| `vae` | `qwen_image_vae.safetensors` | 253,806,246 | VAE |
| `loras` | `ExSieAcidic-krea2.safetensors` | 228,588,984 | acidic 스타일 워크플로 |
| `loras` | `krea2_darkbrush.safetensors` | 469,291,992 | 설치되어 있던 추가 LoRA, 워크플로 참조 시만 필요 |

파일 크기는 식별용이며 체크섬 검증을 대신하지 않는다. 모델 약 19GB 이상과 출력/설치 공간이 별도로 필요하다. **모델은 이 저장소에 복사하지 않았다.** 집에서 해당 파일을 별도 저장장치로 가져오거나, 공식 모델 배포처에서 정확히 일치하는 파일을 받는다. Krea2 기본 모델 경로는 [공식 Krea2 가이드](https://docs.comfy.org/tutorials/image/krea/krea-2)의 모델 링크를 사용한다. 위 두 LoRA의 원래 다운로드 URL은 확인하지 못했으므로 임의의 동명 파일로 대체하지 말고 집 파일을 우선 확보한다.

## 커스텀 노드

대포 워크플로가 요구하는 12개 노드는 현재 서버에서 모두 `python_module=nodes`인 기본 노드로 확인했다:

`UNETLoader`, `CLIPLoader`, `VAELoader`, `CLIPTextEncode`, `ConditioningZeroOut`, `ImageScale`, `KSampler`, `VAEDecodeTiled`, `VAEEncodeTiled`, `LoadImage`, `SaveImage`, `LoraLoaderModelOnly`.

텍스트 생성 기본 그래프는 `EmptyLatentImage`도 사용한다. 확인한 `custom_nodes` 폴더에는 `__pycache__` 외 설치 패키지가 없었다. 이 워크플로 때문에 임의의 커스텀 노드 묶음을 설치할 필요는 없다. 노드가 없거나 `krea2` CLIP 타입이 없으면 ComfyUI 버전/기동 로그를 먼저 확인한다. 다른 워크플로를 추가할 때는 [공식 커스텀 노드 안내](https://support.comfy.org/articles/9321608144-installing-custom-nodes)를 따른다.

## 프로젝트에 동봉한 워크플로

집 PC의 `D:/ComfyUI/krea2-automation/workflow*api.json`을 `Tools/ComfyUI/Workflows/`로 복사했다. 이들은 `/prompt` 제출용 API 그래프이며 UI 편집기용 배치·연결 좌표 JSON과는 다르다.

- `workflow_api.json`: 입력 이미지 없는 기본 텍스트 생성. 768×768, seed 42, steps 8, CFG 1, Euler/simple, denoise 1, tiled VAE.
- `workflow_ship_acidic_preserve_lora08_api.json`: 배 형태 보존 스타일 작업에 사용한 그래프.
- `workflow_clouds_*`, `workflow_tree_*`, `workflow_garden_ship_*`, `workflow_wheat_planter_*`: 제작 중 사용한 변형. 파일의 입력 이미지명과 실제 입력 파일을 먼저 확인한다.
- 기존 `Tools/ArtImport/ComfyCannonV2/workflow_api.json`: 대포용 최종 그래프. 1024×960, seed 42, steps 8, CFG 1, Euler/simple, denoise 0.3, acidic LoRA 0.8, tiled encode/decode(512/64).
- 대포 입력 원본은 `Tools/ArtImport/ComfyCannonV2/Input.png`에 있다. 서버 입력에는 `skytree_cannon_v2_source.png`라는 이름으로 업로드했던 기록이다.

기존 `ComfyCannonV2/submit.py`는 집의 외부 워크플로 경로를 읽는다. 회사에서 그대로 실행하지 않는다. 새 제출 작업은 동봉된 API JSON과 회사 프로젝트 경로를 사용하도록 경로를 바꾼 뒤 진행한다. `Prepare.ps1`은 집의 임시 클립보드 파일을 참조하므로 대포 입력 이미지가 이미 있으면 다시 실행할 필요가 없다. `check.py`와 `job.json`의 과거 작업 ID는 회사 서버의 새 작업을 의미하지 않는다.

## 연결 점검과 첫 생성

프로젝트 루트의 PowerShell에서 읽기 전용 점검:

```powershell
$comfyServer = 'http://127.0.0.1:8188'
Invoke-RestMethod "$comfyServer/system_stats" -TimeoutSec 10
Invoke-RestMethod "$comfyServer/queue" -TimeoutSec 10
$nodeInfo = Invoke-RestMethod "$comfyServer/object_info" -TimeoutSec 20
$nodeInfo.UNETLoader.input.required.unet_name
$nodeInfo.CLIPLoader.input.required.clip_name
$nodeInfo.VAELoader.input.required.vae_name
$nodeInfo.LoraLoaderModelOnly.input.required.lora_name
```

8188이 닫혀 있다면 Desktop의 실제 포트를 확인한다. 집에서도 처음 예상한 8189가 아닌 8188에서 실행됐다. 다른 서버로 자동 전환하지 않는다.

모델이 준비되면 다음 명령은 **실제로 이미지 한 장을 생성**한다. 기본 텍스트 그래프는 LoRA나 입력 이미지가 없어 첫 점검에 적합하다. 이 문서 작성 중에는 실행하지 않았다.

```powershell
$graph = Get-Content -Raw -Encoding UTF8 'Tools/ComfyUI/Workflows/workflow_api.json' | ConvertFrom-Json
$graph.'29'.inputs.filename_prefix = 'SkyTree/OfficeSetup/FirstCheck'
$payload = @{ prompt = $graph; client_id = [guid]::NewGuid().ToString() } | ConvertTo-Json -Depth 100
$job = Invoke-RestMethod -Uri "$comfyServer/prompt" -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($payload))
$job.prompt_id
# 시간이 지난 뒤 조회. 완료 전에는 빈 결과일 수 있다.
Invoke-RestMethod "$comfyServer/history/$($job.prompt_id)" -TimeoutSec 20
```

서버 출력 폴더의 `SkyTree/OfficeSetup`에서 결과를 확인한다. 회사의 서버 출력 경로는 Desktop에서 확인하며 집 경로와 같다고 가정하지 않는다. 오류가 나면 `/history`와 `/queue`를 확인한 후 재시도한다. 요청 타임아웃이 생성 취소를 뜻하지는 않는다.

이미지 기반 그래프는 먼저 ComfyUI에 입력 이미지를 올린 뒤 `LoadImage`의 `image` 값을 서버에 저장된 정확한 이름으로 맞춘다. 위 텍스트 생성 명령을 이미지 기반 그래프에 그대로 적용하면 입력 누락 오류가 날 수 있다.

집의 12GB GPU는 CPU 오프로딩을 사용하는 환경이었다. 우선 768×768, 한 장으로 확인하고 그다음 1024로 늘린다. 서버 Python 환경에 일반 터미널의 pip로 무작정 패키지를 설치하지 않는다. 집의 범용 `generate.py`는 특정 Desktop 출력 경로까지 검사하므로 이식용 스크립트로 동봉하지 않았다.
