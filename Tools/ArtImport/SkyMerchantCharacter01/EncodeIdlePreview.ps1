param(
    [string]$FramesDirectory = 'Verification/SkyMerchantRig/Frames',
    [string]$Destination = 'Verification/SkyMerchantRig/SkyMerchant-Idle.gif'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$frames = @(Get-ChildItem -LiteralPath $FramesDirectory -Filter 'frame*.png' | Sort-Object Name)
if ($frames.Count -ne 48) { throw 'Expected 48 rendered frames.' }
$encoder = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
$palette = $null
for ($i = 0; $i -lt $frames.Count; $i++) {
    $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new()
    $bitmap.BeginInit()
    $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bitmap.DecodePixelWidth = 480
    $bitmap.UriSource = [Uri]::new($frames[$i].FullName)
    $bitmap.EndInit()
    $bitmap.Freeze()
    if ($null -eq $palette) { $palette = [System.Windows.Media.Imaging.BitmapPalette]::new($bitmap, 256) }
    $indexed = [System.Windows.Media.Imaging.FormatConvertedBitmap]::new($bitmap, [System.Windows.Media.PixelFormats]::Indexed8, $palette, 0)
    if ($indexed.CanFreeze) { $indexed.Freeze() }
    $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('gif')
    $delay = if ($i % 3 -eq 0) { 6 } else { 7 }
    $metadata.SetQuery('/grctlext/Delay', [UInt16]$delay)
    $metadata.SetQuery('/grctlext/Disposal', [byte]2)
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($indexed, $null, $metadata, $null))
}
$outputPath = [System.IO.Path]::GetFullPath($Destination)
$stream = [System.IO.File]::Create($outputPath)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
# Write explicit GIF control blocks: WIC may omit the per-frame timing metadata.
$gif = [System.IO.File]::ReadAllBytes($outputPath)
$offset = 13
if (($gif[10] -band 128) -ne 0) { $offset += 3 * [Math]::Pow(2, ($gif[10] -band 7) + 1) }
$loop = [byte[]]@(0x21,0xff,0x0b) + [System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0') + [byte[]]@(3,1,0,0,0)
function Get-SubBlocksEnd([byte[]]$data, [int]$position) {
    while ($position -lt $data.Length) {
        $size = $data[$position]
        $position++
        if ($size -eq 0) { return $position }
        $position += $size
    }
    throw 'Truncated GIF block.'
}
$buffer = [System.IO.MemoryStream]::new()
$buffer.Write($gif,0,$offset)
$buffer.Write($loop,0,$loop.Length)
$frameIndex = 0
while ($offset -lt $gif.Length) {
    $start = $offset
    if ($gif[$offset] -eq 0x2c) {
        $delay = if ($frameIndex % 3 -eq 0) { 6 } else { 7 }
        $control = [byte[]]@(0x21,0xf9,4,8,$delay,0,0,0)
        $buffer.Write($control,0,$control.Length)
        $flags = $gif[$offset+9]
        $offset += 10
        if (($flags -band 128) -ne 0) { $offset += 3 * [Math]::Pow(2, ($flags -band 7) + 1) }
        $offset++ # LZW minimum code size
        $offset = Get-SubBlocksEnd $gif $offset
        $buffer.Write($gif,$start,$offset-$start)
        $frameIndex++
    } elseif ($gif[$offset] -eq 0x21) {
        $label = $gif[$offset+1]
        $offset = Get-SubBlocksEnd $gif ($offset+2)
        if ($label -ne 0xf9) { $buffer.Write($gif,$start,$offset-$start) }
    } elseif ($gif[$offset] -eq 0x3b) {
        $buffer.WriteByte(0x3b)
        break
    } else { throw "Unknown GIF block at $offset" }
}
if ($frameIndex -ne 48) { throw 'Unexpected encoded frame count.' }
$complete = $buffer.ToArray()
$buffer.Dispose()
[System.IO.File]::WriteAllBytes($outputPath,$complete)
$read = [System.IO.File]::OpenRead($outputPath)
try {
    $decoder = [System.Windows.Media.Imaging.GifBitmapDecoder]::new($read,[System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,[System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $hundredths = 0
    foreach ($frame in $decoder.Frames) { $hundredths += [int]$frame.Metadata.GetQuery('/grctlext/Delay') }
    if ($decoder.Frames.Count -ne 48 -or $hundredths -ne 320) { throw 'Animation timing validation failed.' }
    [pscustomobject]@{ Path=$outputPath; Frames=$decoder.Frames.Count; DurationSeconds=$hundredths/100.0; Width=$decoder.Frames[0].PixelWidth; Height=$decoder.Frames[0].PixelHeight; Bytes=$complete.Length } | ConvertTo-Json
} finally { $read.Dispose() }
