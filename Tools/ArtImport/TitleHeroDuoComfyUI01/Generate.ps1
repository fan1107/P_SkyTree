param([ValidateSet('submit','status','collect')][string]$Action='status')
$ErrorActionPreference='Stop'
$server='http://127.0.0.1:8188'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$verify=Join-Path $root 'Verification/TitleHeroDuoComfyUI01'
New-Item -ItemType Directory -Force -Path $verify | Out-Null
$jobPath=Join-Path $PSScriptRoot 'job.json'
if($Action -eq 'submit') {
    if(Test-Path -LiteralPath $jobPath) { throw 'Job already recorded.' }
    $sketch=Join-Path $PSScriptRoot 'PoseSketch.png'
    Copy-Item -LiteralPath 'C:/Users/eicke/AppData/Local/Temp/codex-clipboard-30487e33-616e-4fd6-8709-c9f43c0a247e.png' -Destination $sketch
    $upload=Invoke-RestMethod -Uri "$server/upload/image" -Method Post -Form @{image=Get-Item -LiteralPath $sketch;type='input';overwrite='false'} -TimeoutSec 30
    $inputName=if($upload.subfolder){$upload.subfolder+'/'+$upload.name}else{$upload.name}
    $graph=Get-Content -Raw -LiteralPath (Join-Path $root 'Tools/ArtImport/ComfyMerchantParallel03/workflow_api.json') | ConvertFrom-Json -AsHashtable
    $graph['6']['inputs']['text']=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Prompt.txt')
    $graph['20']['inputs']['image']=$inputName
    $graph['22']['inputs']['width']=864
    $graph['22']['inputs']['height']=1024
    $graph['3']['inputs']['seed']=2026111221
    $graph['3']['inputs']['denoise']=0.85
    $graph['3']['inputs']['steps']=10
    $graph['90']['inputs']['width']=1728
    $graph['90']['inputs']['height']=2048
    $graph['29']['inputs']['filename_prefix']='SkyTree/TitleHeroDuoComfyUI01/HeroDuo_1728x2048'
    $graph['30']['inputs']['filename_prefix']='SkyTree/TitleHeroDuoComfyUI01/Native_864x1024'
    $json=$graph | ConvertTo-Json -Depth 100
    $json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'workflow_api.json') -Encoding utf8
    $json | Set-Content -LiteralPath (Join-Path $root 'Tools/ComfyUI/Workflows/workflow_title_hero_duo_comfy01_api.json') -Encoding utf8
    $payload=@{prompt=$graph;client_id='skytree-title-hero-'+[guid]::NewGuid().ToString('N')} | ConvertTo-Json -Depth 100
    $job=Invoke-RestMethod -Uri "$server/prompt" -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($payload)) -TimeoutSec 30
    $job | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jobPath -Encoding utf8
    $job | ConvertTo-Json -Depth 20 -Compress
    exit
}
$job=Get-Content -Raw -LiteralPath $jobPath | ConvertFrom-Json
$history=Invoke-RestMethod -Uri ($server+'/history/'+$job.prompt_id) -TimeoutSec 20
$record=$history.PSObject.Properties[$job.prompt_id].Value
if(-not $record) {
    $queue=Invoke-RestMethod -Uri "$server/queue" -TimeoutSec 10
    [pscustomobject]@{prompt_id=$job.prompt_id;completed=$false;running=@($queue.queue_running).Count;pending=@($queue.queue_pending).Count} | ConvertTo-Json -Compress
    exit
}
if($Action -eq 'status') { [pscustomobject]@{prompt_id=$job.prompt_id;status=$record.status;outputs=$record.outputs} | ConvertTo-Json -Depth 20 -Compress; exit }
$record | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $verify 'History.json') -Encoding utf8
if($record.status.status_str -ne 'success') { throw 'Generation failed. See History.json.' }
foreach($node in @('29','30')) {
    $d=$record.outputs.PSObject.Properties[$node].Value.images[0]
    $query='filename='+[Uri]::EscapeDataString($d.filename)+'&subfolder='+[Uri]::EscapeDataString($d.subfolder)+'&type='+[Uri]::EscapeDataString($d.type)
    $name=if($node -eq '29'){'HeroDuo_1728x2048.png'}else{'Native_864x1024.png'}
    Invoke-WebRequest -Uri ($server+'/view?'+$query) -OutFile (Join-Path $verify $name) -TimeoutSec 30
}
[pscustomobject]@{prompt_id=$job.prompt_id;completed=$true;output=(Join-Path $verify 'HeroDuo_1728x2048.png')} | ConvertTo-Json -Compress
