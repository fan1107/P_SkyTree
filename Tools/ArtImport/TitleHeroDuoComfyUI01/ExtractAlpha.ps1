param([string]$Source,[string]$Destination)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @('System.Drawing.Common','System.Drawing.Primitives','System.Runtime.InteropServices','System.Runtime','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.ComponentModel.Primitives') -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class HeroDuoAlpha {
    public static string Extract(string source, string destination) {
        using(var original = new Bitmap(source))
        using(var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb)) {
            using(var graphics = Graphics.FromImage(bitmap)) graphics.DrawImageUnscaled(original, 0, 0);
            int w=bitmap.Width,h=bitmap.Height,n=w*h;
            var rectangle=new Rectangle(0,0,w,h);
            var data=bitmap.LockBits(rectangle,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            var bytes=new byte[data.Stride*h]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
            var candidate=new bool[n]; var background=new bool[n]; var queue=new int[n]; int head=0,tail=0;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                int p=y*data.Stride+4*x, min=Math.Min(bytes[p],Math.Min(bytes[p+1],bytes[p+2])), max=Math.Max(bytes[p],Math.Max(bytes[p+1],bytes[p+2]));
                candidate[y*w+x]=min>=239 && max-min<=18;
            }
            for(int i=0;i<n;i++) if((i<w || i>=n-w || i%w==0 || i%w==w-1) && candidate[i]) { background[i]=true; queue[tail++]=i; }
            while(head<tail) {
                int k=queue[head++],x=k%w,y=k/w;
                for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++) {
                    int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                    int j=yy*w+xx;if(!background[j] && candidate[j]) {background[j]=true;queue[tail++]=j;}
                }
            }
            // Clear sizeable white pockets enclosed by curled hair or bent limbs, retaining tiny eye glints.
            var pockets=new bool[n];
            for(int seed=0;seed<n;seed++) {
                if(background[seed] || !candidate[seed] || pockets[seed])continue;
                head=0;tail=1;queue[0]=seed;pockets[seed]=true;
                while(head<tail) {
                    int k=queue[head++],x=k%w,y=k/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                        int j=yy*w+xx;if(!background[j]&&candidate[j]&&!pockets[j]){pockets[j]=true;queue[tail++]=j;}
                    }
                }
                if(tail>500)for(int i=0;i<tail;i++)background[queue[i]]=true;
            }
            // Preserve the two complete silhouettes; remove only tiny disconnected background flecks.
            var seen=new bool[n]; int components=0;
            for(int seed=0;seed<n;seed++) {
                if(background[seed] || seen[seed])continue;
                head=0;tail=1;queue[0]=seed;seen[seed]=true;
                while(head<tail) {
                    int k=queue[head++],x=k%w,y=k/w;
                    for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++) {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                        int j=yy*w+xx;if(!background[j] && !seen[j]){seen[j]=true;queue[tail++]=j;}
                    }
                }
                if(tail<5000) {for(int i=0;i<tail;i++)background[queue[i]]=true;} else components++;
            }
            var originalBytes=(byte[])bytes.Clone();
            int transparent=0,left=w,right=-1,top=h,bottom=-1;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int k=y*w+x,p=y*data.Stride+4*x;
                if(background[k]) {bytes[p]=bytes[p+1]=bytes[p+2]=bytes[p+3]=0;transparent++;continue;}
                left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
                bool edge=false;
                for(int dy=-2;dy<=2 && !edge;dy++)for(int dx=-2;dx<=2;dx++) {
                    int xx=x+dx,yy=y+dy;if(xx>=0&&xx<w&&yy>=0&&yy<h&&background[yy*w+xx]){edge=true;break;}
                }
                if(edge) {
                    int nearest=-1,best=100;
                    for(int dy=-5;dy<=5;dy++)for(int dx=-5;dx<=5;dx++) {
                        int xx=x+dx,yy=y+dy,dist=dx*dx+dy*dy;
                        if(xx<2||xx>=w-2||yy<2||yy>=h-2||dist>=best||background[yy*w+xx])continue;
                        bool core=true;
                        for(int cy=-2;cy<=2 && core;cy++)for(int cx=-2;cx<=2;cx++)if(background[(yy+cy)*w+xx+cx]){core=false;break;}
                        if(core){best=dist;nearest=yy*data.Stride+4*xx;}
                    }
                    if(nearest>=0) {
                        float numerator=0,denominator=0;
                        for(int c=0;c<3;c++){float v=255-originalBytes[nearest+c];numerator+=(255-originalBytes[p+c])*v;denominator+=v*v;}
                        float alpha=Math.Min(1f,Math.Max(0f,numerator/Math.Max(denominator,1)));
                        bytes[p+3]=(byte)Math.Round(alpha*255);
                        for(int c=0;c<3;c++)bytes[p+c]=(byte)Math.Max(0,Math.Min(255,Math.Round((originalBytes[p+c]-255*(1-alpha))/Math.Max(alpha,.001f))));
                    }
                }
            }
            Marshal.Copy(bytes,0,data.Scan0,bytes.Length);bitmap.UnlockBits(data);bitmap.Save(destination,ImageFormat.Png);
            return string.Format("{0}x{1} RGBA; transparent={2:P1}; foreground components={3}; bounds=({4},{5})-({6},{7})",w,h,(double)transparent/n,components,left,top,right,bottom);
        }
    }
}
'@
[HeroDuoAlpha]::Extract((Resolve-Path -LiteralPath $Source).Path,$Destination)
