param(
    [ValidateSet('submit','status','collect')][string]$Action = 'status'
)
$ErrorActionPreference = 'Stop'
$comfyServer = 'http://127.0.0.1:8188'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$jobPath = Join-Path $PSScriptRoot 'job.json'
$verificationRoot = Join-Path $projectRoot 'Verification/TitleSkyTreeComfyUI01'
New-Item -ItemType Directory -Force -Path $verificationRoot | Out-Null

if ($Action -eq 'submit') {
    if (Test-Path -LiteralPath $jobPath) { throw 'A job is already recorded. Check its status before submitting again.' }
    $reference = 'C:\Users\eicke\AppData\Local\Temp\codex-clipboard-fdba764a-2af4-4ea3-8cd1-5df15fa2c8f4.png'
    $localReference = Join-Path $PSScriptRoot 'Reference.png'
    if (-not (Test-Path -LiteralPath $localReference)) { Copy-Item -LiteralPath $reference -Destination $localReference }
    $upload = Invoke-RestMethod -Uri ($comfyServer + '/upload/image') -Method Post -Form @{
        image = Get-Item -LiteralPath $localReference
        type = 'input'
        overwrite = 'false'
    } -TimeoutSec 30
    $inputName = if ($upload.subfolder) { $upload.subfolder + '/' + $upload.name } else { $upload.name }
    $graph = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Tools/ComfyUI/Workflows/workflow_api.json') | ConvertFrom-Json -AsHashtable
    $prompt = @'
A magnificent ancient golden sky tree growing directly from a huge flying wooden galleon, one complete tree and one ship forming a single integrated fantasy vessel, soaring through a deep dark petrol-blue and teal sky above a diagonal luminous turquoise atmospheric horizon and warm pearl-gold clouds. Majestic view from far below at a three-quarter angle, the richly shadowed curved wooden hull is clearly visible. Preserve the reference composition and its monumental tree, massive twisting trunk, broad crown of warm ochre and golden autumn leaves, intricate wooden balconies and rounded stern cabins. The living roots embrace and merge into the ship's timber decks, beautiful handcrafted wooden planks, aged brass accents, a few tiny warm amber windows. No fabric sails; the enormous tree canopy is the ship's visual sail. Elegant readable coherent ship architecture, not a futuristic spacecraft or a building on a flat platform. Grand, dreamlike, inviting fantasy adventure mood. The entire ship is suspended in open air, not floating on water.

Vertical 9:16 mobile game title background illustration. Center the complete tree-ship in the middle of the picture, with its canopy around the upper middle and its hull below the center. The top fifth is calm deep teal atmospheric sky, spacious and uncluttered, ready for a game title added later. The bottom fifth is soft dark teal mist and subtle cloud layers, quiet space for a start button added later. These are naturally continuous painted sky areas, never blank bars or rectangular panels. Warm golden light from upper left softly illuminates leaves and wood, cool turquoise reflected light reveals the hull. Tiny sparse drifting gold leaves and one very small distant pale bird convey scale. Painterly premium fantasy RPG key art, rich finely brushed foliage, expressive ancient bark, meticulous wooden ship details, atmospheric perspective, warm gold against cool deep teal, cinematic luminosity, restrained texture, strong silhouette. Seamless continuous composition, finished artwork from edge to edge. Background only: absolutely no text, no letters, no logo, no UI, no buttons, no border, no watermark, no humans. No duplicated trees or extra ships.
'@
    $graph['6']['inputs']['text'] = $prompt
    $graph.Remove('5')
    $graph['20'] = @{class_type='LoadImage'; inputs=@{image=$inputName}}
    $graph['21'] = @{class_type='ImageScale'; inputs=@{image=@('20',0); upscale_method='lanczos'; width=720; height=720; crop='disabled'}}
    $graph['22'] = @{class_type='EmptyImage'; inputs=@{width=720; height=1280; batch_size=1; color=283432}}
    $graph['23'] = @{class_type='SolidMask'; inputs=@{value=1.0; width=720; height=720}}
    $graph['24'] = @{class_type='FeatherMask'; inputs=@{mask=@('23',0); left=8; top=64; right=8; bottom=80}}
    $graph['25'] = @{class_type='ImageCompositeMasked'; inputs=@{destination=@('22',0); source=@('21',0); x=0; y=248; resize_source=$false; mask=@('24',0)}}
    $graph['26'] = @{class_type='VAEEncodeTiled'; inputs=@{pixels=@('25',0); vae=@('12',0); tile_size=512; overlap=64; temporal_size=64; temporal_overlap=8}}
    $graph['3']['inputs']['latent_image'] = @('26',0)
    $graph['3']['inputs']['seed'] = 2026111207
    $graph['3']['inputs']['denoise'] = 0.75
    $graph['3']['inputs']['steps'] = 8
    $graph['90'] = @{class_type='ImageScale'; inputs=@{image=@('8',0); upscale_method='lanczos'; width=1080; height=1920; crop='disabled'}}
    $graph['29']['inputs']['images'] = @('90',0)
    $graph['29']['inputs']['filename_prefix'] = 'SkyTree/TitleGoldenCanopy_ComfyUI01/Title_1080x1920'
    $graph['30'] = @{class_type='SaveImage'; inputs=@{images=@('8',0); filename_prefix='SkyTree/TitleGoldenCanopy_ComfyUI01/Native_720x1280'}}
    $graph['31'] = @{class_type='SaveImage'; inputs=@{images=@('25',0); filename_prefix='SkyTree/TitleGoldenCanopy_ComfyUI01/ReferenceGuide'}}
    $workflowJson = $graph | ConvertTo-Json -Depth 100
    $workflowJson | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'workflow_api.json') -Encoding utf8
    $workflowJson | Set-Content -LiteralPath (Join-Path $projectRoot 'Tools/ComfyUI/Workflows/workflow_title_skytree_golden_canopy01_api.json') -Encoding utf8
    $prompt | Set-Content -LiteralPath (Join-Path $verificationRoot 'Prompt.txt') -Encoding utf8
    $payload = @{prompt=$graph; client_id='skytree-title-' + [guid]::NewGuid().ToString('N')} | ConvertTo-Json -Depth 100
    $job = Invoke-RestMethod -Uri ($comfyServer + '/prompt') -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($payload)) -TimeoutSec 30
    $job | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jobPath -Encoding utf8
    $job | ConvertTo-Json -Depth 20 -Compress
    exit
}

$job = Get-Content -Raw -LiteralPath $jobPath | ConvertFrom-Json
$history = Invoke-RestMethod -Uri ($comfyServer + '/history/' + $job.prompt_id) -TimeoutSec 20
$record = $history.PSObject.Properties[$job.prompt_id].Value
if (-not $record) {
    $queue = Invoke-RestMethod -Uri ($comfyServer + '/queue') -TimeoutSec 10
    [pscustomobject]@{prompt_id=$job.prompt_id; completed=$false; running=@($queue.queue_running).Count; pending=@($queue.queue_pending).Count} | ConvertTo-Json -Compress
    exit
}
if ($Action -eq 'status') {
    [pscustomobject]@{prompt_id=$job.prompt_id; status=$record.status; outputs=$record.outputs} | ConvertTo-Json -Depth 12 -Compress
    exit
}
$record | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $verificationRoot 'History.json') -Encoding utf8
if ($record.status.status_str -ne 'success' -or -not $record.status.completed) { throw 'ComfyUI did not complete successfully. See History.json.' }
$destinations = @{
    '29' = (Join-Path $projectRoot 'Assets/ArtResources/Backgrounds/TitleSkyTreeGoldenCanopy_ComfyUI01.png')
    '30' = (Join-Path $verificationRoot 'Native_720x1280.png')
    '31' = (Join-Path $verificationRoot 'ReferenceGuide.png')
}
foreach ($nodeId in @('29','30','31')) {
    $descriptor = $record.outputs.PSObject.Properties[$nodeId].Value.images[0]
    $query = 'filename=' + [Uri]::EscapeDataString($descriptor.filename) + '&subfolder=' + [Uri]::EscapeDataString($descriptor.subfolder) + '&type=' + [Uri]::EscapeDataString($descriptor.type)
    Invoke-WebRequest -Uri ($comfyServer + '/view?' + $query) -OutFile $destinations[$nodeId] -TimeoutSec 30
}
$manifest = [ordered]@{
    generator='Local ComfyUI'; server=$comfyServer; prompt_id=$job.prompt_id; completed=$true
    model='krea2_turbo_int8_convrot.safetensors'; seed=2026111207; denoise=0.75
    native_resolution='720x1280'; output_resolution='1080x1920'; upscale='Lanczos in ComfyUI'
    reference='Tools/ArtImport/ComfyTitleSkyTree01/Reference.png'
    workflow='Tools/ComfyUI/Workflows/workflow_title_skytree_golden_canopy01_api.json'
    output='Assets/ArtResources/Backgrounds/TitleSkyTreeGoldenCanopy_ComfyUI01.png'
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $verificationRoot 'Generation.json') -Encoding utf8
$manifest | ConvertTo-Json -Depth 6 -Compress
