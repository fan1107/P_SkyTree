Add-Type -AssemblyName System.Drawing
$src=[Drawing.Bitmap]::FromFile('C:/Users/fan/AppData/Local/Temp/codex-clipboard-30e9e9ee-7469-4490-81a5-740b4ec388b6.png')
$dst=New-Object Drawing.Bitmap($src.Width,$src.Height)
$g=[Drawing.Graphics]::FromImage($dst);$g.Clear([Drawing.Color]::FromArgb(225,225,225))
$coords='580,114 614,119 646,147 654,190 647,232 599,262 579,291 601,294 632,315 607,343 606,354 613,387 628,430 615,476 586,507 557,501 534,484 515,502 516,535 512,572 494,616 455,651 430,644 410,626 409,585 395,575 352,591 303,572 239,537 207,511 195,494 178,488 162,465 165,428 190,388 217,370 218,339 230,326 230,301 258,268 280,257 291,229 310,210 329,199 345,211 380,193 444,177 468,169 510,176 540,147 555,133'
$pts=[Drawing.Point[]]@($coords.Split(' ') | ForEach-Object { $xy=$_.Split(',');New-Object Drawing.Point([int]$xy[0],[int]$xy[1]) })
$path=New-Object Drawing.Drawing2D.GraphicsPath;$path.AddPolygon($pts);$g.SetClip($path);$g.DrawImageUnscaled($src,0,0);$g.Dispose();$dst.Save('D:/MyProject/P_SkyTree/Tools/ArtImport/ComfyCannonV2/Input.png',[Drawing.Imaging.ImageFormat]::Png);$dst.Dispose();$src.Dispose();$path.Dispose()
