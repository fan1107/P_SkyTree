Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;using System.Drawing;using System.Drawing.Imaging;
public static class CannonCutout{
 public static void Run(string srcPath,string dstPath){using(var src=new Bitmap(srcPath)){
 int w=src.Width,h=src.Height,N=w*h;var bg=new bool[N];var seen=new bool[N];var q=new int[N];int head=0,tail=0;
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){var c=src.GetPixel(x,y);int mn=Math.Min(c.R,Math.Min(c.G,c.B)),mx=Math.Max(c.R,Math.Max(c.G,c.B));bg[y*w+x]=(mn>155&&mx-mn<24)||(mn>100&&mx-mn<12);}
 for(int i=0;i<N;i++)if((i<w||i>=N-w||i%w==0||i%w==w-1)&&bg[i]){q[tail++]=i;seen[i]=true;}
 while(head<tail){int k=q[head++],x=k%w,y=k/w;int[] ns={x>0?k-1:-1,x<w-1?k+1:-1,y>0?k-w:-1,y<h-1?k+w:-1};foreach(int n in ns)if(n>=0&&bg[n]&&!seen[n]){seen[n]=true;q[tail++]=n;}}
 var labels=new int[N];int label=0,best=0,bestCount=0;for(int i=0;i<N;i++){if(seen[i]||labels[i]!=0)continue;label++;head=0;tail=0;q[tail++]=i;labels[i]=label;while(head<tail){int k=q[head++],x=k%w,y=k/w;int[] ns={x>0?k-1:-1,x<w-1?k+1:-1,y>0?k-w:-1,y<h-1?k+w:-1};foreach(int n in ns)if(n>=0&&!seen[n]&&labels[n]==0){labels[n]=label;q[tail++]=n;}}if(tail>bestCount){bestCount=tail;best=label;}}for(int i=0;i<N;i++)if(labels[i]!=best)seen[i]=true; int left=w,right=0,top=h,bottom=0;for(int i=0;i<N;i++)if(!seen[i]){left=Math.Min(left,i%w);right=Math.Max(right,i%w);top=Math.Min(top,i/w);bottom=Math.Max(bottom,i/w);}
 using(var dst=new Bitmap(right-left+5,bottom-top+5,PixelFormat.Format32bppArgb)){
 for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)if(!seen[y*w+x])dst.SetPixel(x-left+2,y-top+2,src.GetPixel(x,y));dst.Save(dstPath,ImageFormat.Png);Console.WriteLine(dst.Width+"x"+dst.Height+" RGBA PNG");}
 }}
}
"@
[CannonCutout]::Run('D:/MyProject/P_SkyTree/Assets/ArtResources/Cannons/CannonComfyMatched.png','D:/MyProject/P_SkyTree/Assets/ArtResources/Cannons/CannonComfyTransparent.png')


