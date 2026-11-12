$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$output=Join-Path $root 'Assets/SkyTree/Resources/SkyTree/Title'
$verification=Join-Path $root 'Verification/TitleHeroPlacement'
New-Item -ItemType Directory -Force -Path $verification | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @('System.Drawing.Common','System.Drawing.Primitives','System.Runtime.InteropServices','System.Runtime','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.ComponentModel.Primitives','System.Collections') -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class TitleHeroSplit {
    public static string Split(string source, string output) {
        using(var src=new Bitmap(source)) {
            int w=src.Width,h=src.Height,n=w*h;
            var data=src.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            var bytes=new byte[data.Stride*h];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);int stride=data.Stride;src.UnlockBits(data);
            var label=new int[n];var queue=new int[n];var groups=new List<int[]>();int id=0;
            for(int seed=0;seed<n;seed++) {
                if(label[seed]!=0||bytes[(seed/w)*stride+(seed%w)*4+3]==0)continue;
                id++;int head=0,tail=1,left=w,right=-1,top=h,bottom=-1;queue[0]=seed;label[seed]=id;
                while(head<tail) {
                    int p=queue[head++],x=p%w,y=p/w;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                        int k=yy*w+xx;if(label[k]==0&&bytes[yy*stride+xx*4+3]>0){label[k]=id;queue[tail++]=k;}
                    }
                }
                if(tail>5000)groups.Add(new[]{id,left,top,right,bottom,tail});
            }
            if(groups.Count!=2)throw new Exception("Expected exactly two complete character silhouettes, found "+groups.Count);
            int visiblePixels=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(bytes[y*stride+4*x+3]>0)visiblePixels++;
            if(groups[0][5]+groups[1][5]!=visiblePixels)throw new Exception("Splitting would discard visible pixels.");
            groups.Sort((a,b)=>a[1].CompareTo(b[1]));
            string[] names={"TitleFemaleNavigator_ComfyUI01","TitleMaleCaptain_ComfyUI01"};
            var report=new System.Text.StringBuilder("[\n");
            for(int i=0;i<groups.Count;i++) {
                var g=groups[i];int left=Math.Max(0,g[1]-16),top=Math.Max(0,g[2]-16),cw=g[3]-left+17,ch=g[4]-top+17;
                using(var dst=new Bitmap(cw,ch,PixelFormat.Format32bppArgb)) {
                    var dd=dst.LockBits(new Rectangle(0,0,cw,ch),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);var db=new byte[dd.Stride*ch];
                    for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)if(label[(top+y)*w+left+x]==g[0])Array.Copy(bytes,(top+y)*stride+(left+x)*4,db,y*dd.Stride+x*4,4);
                    Marshal.Copy(db,0,dd.Scan0,db.Length);dst.UnlockBits(dd);dst.Save(System.IO.Path.Combine(output,names[i]+".png"),ImageFormat.Png);
                }
                if(i>0)report.Append(",\n");
                report.AppendFormat("{{\"name\":\"{0}\",\"x\":{1},\"y\":{2},\"width\":{3},\"height\":{4},\"pixels\":{5}}}",names[i],left,top,cw,ch,g[5]);
            }
            return report.Append("\n]").ToString();
        }
    }
}
'@
$json=[TitleHeroSplit]::Split((Join-Path $root 'Assets/ArtResources/Title/TitleHeroDuo_ComfyUI01.png'),$output)
$json | Set-Content -LiteralPath (Join-Path $verification 'SplitManifest.json') -Encoding utf8
$json
