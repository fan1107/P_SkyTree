param([ValidateSet('submit','status','collect')][string]$Action='status')
$ErrorActionPreference='Stop'
$server='http://127.0.0.1:8188'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$verify=Join-Path $root 'Verification/TitleHeroDuoComfyUI01'
$jobPath=Join-Path $PSScriptRoot 'job_finish.json'
if($Action -eq 'submit') {
    if(Test-Path -LiteralPath $jobPath) { throw 'Finish job already exists.' }
    $upload=Invoke-RestMethod -Uri "$server/upload/image" -Method Post -Form @{image=Get-Item -LiteralPath (Join-Path $verify 'Native_864x1024.png');type='input';overwrite='false'} -TimeoutSec 30
    $inputName=if($upload.subfolder){$upload.subfolder+'/'+$upload.name}else{$upload.name}
    $g=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'workflow_api.json') | ConvertFrom-Json -AsHashtable
    $g['20']['inputs']['image']=$inputName
    $g['22']['inputs']['width']=720
    $g['22']['inputs']['height']=848
    $g['40']=@{class_type='EmptyImage';inputs=@{width=1024;height=1216;batch_size=1;color=16777215}}
    $g['41']=@{class_type='ImageCompositeMasked';inputs=@{destination=@('40',0);source=@('22',0);x=152;y=184;resize_source=$false}}
    $g['42']=@{class_type='SolidMask';inputs=@{value=1.0;width=720;height=848}}
    $g['43']=@{class_type='FeatherMask';inputs=@{mask=@('42',0);left=48;right=48;top=48;bottom=24}}
    $g['44']=@{class_type='SolidMask';inputs=@{value=0.0;width=1024;height=1216}}
    $g['45']=@{class_type='MaskComposite';inputs=@{destination=@('44',0);source=@('43',0);x=152;y=184;operation='add'}}
    $g['46']=@{class_type='InvertMask';inputs=@{mask=@('45',0)}}
    $g['21']['inputs']['pixels']=@('41',0)
    $g['47']=@{class_type='SetLatentNoiseMask';inputs=@{samples=@('21',0);mask=@('46',0)}}
    $g['3']['inputs']['latent_image']=@('47',0)
    $g['3']['inputs']['seed']=2026111222
    $g['3']['inputs']['denoise']=0.85
    $g['6']['inputs']['text']='Outpaint only the white margins of this finished character illustration. Preserve the TWO existing characters, their faces, costumes and airborne poses exactly. Continue and FINISH the clipped coral-orange scarf on the left and the royal blue gold-trimmed scarf at upper right: elegant tapering fluttering scarf tips with visibly closed ends, naturally flowing from the original scarves. Do not extend any scarf all the way to an image edge. Both scarf tips end inside the canvas with ample white space beyond. Complete all clipped fingertips without adding extra arms or people. Keep the entire expanded background pure white. No shadow, no frame, no text, no new objects. Two complete small heroes centered with generous white margin on every side. '+$g['6']['inputs']['text']
    $g['48']=@{class_type='ImageCompositeMasked';inputs=@{destination=@('41',0);source=@('8',0);x=0;y=0;resize_source=$false;mask=@('46',0)}}
    $g['90']['inputs']['image']=@('48',0)
    $g['90']['inputs']['width']=2048
    $g['90']['inputs']['height']=2432
    $g['29']['inputs']['filename_prefix']='SkyTree/TitleHeroDuoComfyUI01/Finished_2048x2432'
    $g['30']['inputs']['images']=@('48',0)
    $g['30']['inputs']['filename_prefix']='SkyTree/TitleHeroDuoComfyUI01/FinishedNative_1024x1216'
    $json=$g | ConvertTo-Json -Depth 100
    $json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'workflow_finish_api.json') -Encoding utf8
    $json | Set-Content -LiteralPath (Join-Path $root 'Tools/ComfyUI/Workflows/workflow_title_hero_duo_comfy01_finish_api.json') -Encoding utf8
    $g['6']['inputs']['text'] | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'PromptFinish.txt') -Encoding utf8
    $payload=@{prompt=$g;client_id='skytree-hero-finish-'+[guid]::NewGuid().ToString('N')} | ConvertTo-Json -Depth 100
    $job=Invoke-RestMethod -Uri "$server/prompt" -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($payload)) -TimeoutSec 30
    $job | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jobPath -Encoding utf8
    $job | ConvertTo-Json -Depth 20 -Compress
    exit
}
$job=Get-Content -Raw -LiteralPath $jobPath | ConvertFrom-Json
$history=Invoke-RestMethod -Uri ($server+'/history/'+$job.prompt_id) -TimeoutSec 20
$record=$history.PSObject.Properties[$job.prompt_id].Value
if(-not $record) { [pscustomobject]@{prompt_id=$job.prompt_id;completed=$false} | ConvertTo-Json -Compress; exit }
if($Action -eq 'status') { [pscustomobject]@{prompt_id=$job.prompt_id;status=$record.status;outputs=$record.outputs} | ConvertTo-Json -Depth 20 -Compress; exit }
$record | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $verify 'HistoryFinish.json') -Encoding utf8
if($record.status.status_str -ne 'success') { throw 'Generation failed. See HistoryFinish.json.' }
foreach($node in @('29','30')) {
    $d=$record.outputs.PSObject.Properties[$node].Value.images[0]
    $query='filename='+[Uri]::EscapeDataString($d.filename)+'&subfolder='+[Uri]::EscapeDataString($d.subfolder)+'&type='+[Uri]::EscapeDataString($d.type)
    $name=if($node -eq '29'){'Finished_2048x2432.png'}else{'FinishedNative_1024x1216.png'}
    Invoke-WebRequest -Uri ($server+'/view?'+$query) -OutFile (Join-Path $verify $name) -TimeoutSec 30
}
[pscustomobject]@{completed=$true;output=(Join-Path $verify 'Finished_2048x2432.png')} | ConvertTo-Json -Compress
