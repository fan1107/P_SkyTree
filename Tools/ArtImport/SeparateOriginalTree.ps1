Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class TreeSeparate {
 public static void Run(string input,string dir,string preview){
 using(var src=new Bitmap(input)){
 int w=src.Width,h=src.Height,N=w*h;var colors=new Color[N];var kind=new int[N];var alpha=new int[N];var labels=new int[N];var q=new int[N];
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){
 int i=y*w+x;var c=src.GetPixel(x,y);colors[i]=c;int max=Math.Max(c.R,Math.Max(c.G,c.B)),min=Math.Min(c.R,Math.Min(c.G,c.B)),chroma=max-min;
 if(chroma<=10)continue;
 alpha[i]=Math.Min(255,(chroma-10)*32);kind[i]=(c.G>=c.R*.87 && c.G>c.B*1.2)?1:2;
 }
 // Grow three regions through connected leaf pixels from interior seeds.
 int head=0,tail=0;for(int i=0;i<N;i++){if(kind[i]!=1)continue;int x=i%w,y=i/w;int seed=y<300?1:(x<380&&y>550?2:(x>660&&y>430?3:0));if(seed>0){labels[i]=seed;q[tail++]=i;}}
 while(head<tail){int k=q[head++],x=k%w,y=k/w;int[] ns={x>0?k-1:-1,x<w-1?k+1:-1,y>0?k-w:-1,y<h-1?k+w:-1};foreach(int n in ns)if(n>=0&&kind[n]==1&&labels[n]==0){labels[n]=labels[k];q[tail++]=n;}}
 for(int i=0;i<N;i++){if(kind[i]!=1)continue;if(labels[i]==0){float nx=(i%w)/(float)w,ny=(i/w)/(float)h;labels[i]=ny>(.43f+.17f*nx)&&nx<.46f?2:(nx>.50f&&ny>(.68f-.44f*nx)?3:1);}kind[i]=labels[i]+2;} string[] names={"TrunkBranches","CanopyTop","CanopyLowerLeft","CanopyRight"};
 var outputs=new Bitmap[4];for(int j=0;j<4;j++)outputs[j]=new Bitmap(w,h,PixelFormat.Format32bppArgb);
 for(int i=0;i<N;i++){int n=kind[i]==2?0:kind[i]>=3?kind[i]-2:-1;if(n<0)continue;var c=colors[i];outputs[n].SetPixel(i%w,i/w,Color.FromArgb(alpha[i],c.R,c.G,c.B));}
 using(var sheet=new Bitmap(1400,460,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(sheet)){
 g.Clear(Color.FromArgb(37,53,65));for(int j=0;j<4;j++){outputs[j].Save(dir+"/"+names[j]+".png",ImageFormat.Png);g.DrawImage(outputs[j],j*350,20,350,416);outputs[j].Dispose();}sheet.Save(preview,ImageFormat.Png);}
 Console.WriteLine("Exported four transparent PNGs: "+w+"x"+h);
 }
 }
}
"@
[TreeSeparate]::Run('C:/Users/fan/AppData/Local/Temp/codex-clipboard-64fea0fb-30c7-474c-8502-6796b5adbaf8.png','D:/MyProject/P_SkyTree/Assets/ArtResources/Ships/MainShip_Step01/TreeOriginalLayers','D:/MyProject/P_SkyTree/TreeOriginalLayers-Preview.png')




