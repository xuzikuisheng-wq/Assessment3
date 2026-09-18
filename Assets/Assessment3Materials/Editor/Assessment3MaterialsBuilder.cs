using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Companion material preparation for the step-by-step learning example.
// Runs only when the menu is clicked. Artwork source records are in the pack.
public static class Assessment3MaterialsBuilder
{
    const string Root="Assets/Assessment3Materials/";
    static readonly string[] Dirs={"Right","Down","Left","Up"};
    static Dictionary<string,Sprite[]> sequences=new Dictionary<string,Sprite[]>();
    static AnimatorController player, power;
    static AnimatorController[] ghosts=new AnimatorController[4];
    static GameObject[] prefabs=new GameObject[9];
    static Sprite bonus,life;
    static void Folder(string p){Directory.CreateDirectory(Root+p);}

    [MenuItem("Assessment 3/Prepare reference materials")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogError("Exit Play mode before preparing materials."); return; }
        if(!Directory.Exists(Root+"SourceSheets") || Directory.GetFiles(Root+"SourceSheets","*.png").Length!=10)
        { Debug.LogError("Copy the complete Assessment3Materials folder to Assets, keeping its name unchanged."); return; }
        foreach(string folder in new[]{"Sprites","Animations","Animators","Prefabs"})
            if(Directory.Exists(Root+folder) && Directory.EnumerateFiles(Root+folder,"*",SearchOption.AllDirectories).Any())
            { Debug.LogWarning("Generated materials already exist. Preparation stopped to preserve existing references and edits. Use the existing materials or a fresh copy in a new test project."); return; }
        sequences.Clear();
        foreach(string p in new[]{"Sprites/Frames","Sprites/Tiles","Animations/Player","Animations/Power","Animators","Prefabs/Tiles"})Folder(p);
        for(int i=1;i<=4;i++)Folder("Animations/Drone"+i);
        AssetDatabase.Refresh();
        foreach(string p in Directory.GetFiles(Root+"SourceSheets","*.png"))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(p.Replace('\\','/'));
            ti.textureType=TextureImporterType.Default;ti.isReadable=true;ti.mipmapEnabled=false;
            ti.alphaIsTransparency=true;ti.filterMode=FilterMode.Point;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.SaveAndReimport();
        }
        PlayerSprites();DroneSprites();TileSprites();Controllers();Prefabs();
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root.TrimEnd('/'));
        Debug.Log("ASSESSMENT3_MATERIALS_READY: 95 sprite frames, 7 tile PNGs, 46 animation clips, 6 controllers, 8 tile prefabs. See Assets/Assessment3Materials.");
    }
    static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"SourceSheets/"+name+".png");
    // Cuts are sprite metadata; the original artwork is retained without destructive editing.
    static Sprite[] Cut(string key,Texture2D tex,RectInt[] cells,float size=.88f)
    {
        int minx=99999,miny=99999,maxx=-1,maxy=-1;
        foreach(var cell in cells)
            for(int y=0;y<cell.height;y++)for(int x=0;x<cell.width;x++)
                if(tex.GetPixel(cell.x+x,tex.height-1-cell.y-y).a>.45f)
                {minx=Math.Min(minx,x);miny=Math.Min(miny,y);maxx=Math.Max(maxx,x);maxy=Math.Max(maxy,y);}
        if(maxx<0)throw new Exception("Empty sprite slice "+key);
        minx=Math.Max(0,minx-3);miny=Math.Max(0,miny-3);
        maxx=Math.Min(cells[0].width-1,maxx+3);maxy=Math.Min(cells[0].height-1,maxy+3);
        float ppu=Math.Max(maxx-minx+1,maxy-miny+1)/size;
        Sprite[] frames=new Sprite[cells.Length];
        for(int i=0;i<cells.Length;i++)
        {
            var cell=cells[i];var rect=new Rect(cell.x+minx,tex.height-cell.y-maxy-1,maxx-minx+1,maxy-miny+1);
            var sprite=Sprite.Create(tex,rect,new Vector2(.5f,.5f),ppu,0,SpriteMeshType.FullRect);
            sprite.name=key+"_"+i;string path=Root+"Sprites/Frames/"+sprite.name+".asset";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path))AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(sprite,path);frames[i]=sprite;
        }
        sequences[key]=frames;return frames;
    }
    static RectInt R(int col,int row,int width=256,int height=256)=>new RectInt(col*width,row*height,width,height);
    static void PlayerSprites()
    {
        var t=Texture("Player");
        Cut("Player_Walk_Right",t,new[]{R(0,0,256,300),R(1,0,256,300)});
        Cut("Player_Walk_Down",t,new[]{R(2,0,256,300),R(3,0,256,300)});
        Cut("Player_Walk_Left",t,new[]{R(4,0,256,300),R(5,0,256,300)});
        Cut("Player_Walk_Up",t,new[]{R(0,1,256,300),R(1,1,256,300)});
        Cut("Player_Dead",t,new[]{R(2,1,256,300),R(3,1,256,300),R(4,1,256,300)});
    }
    static void DroneSprites()
    {
        for(int i=1;i<=4;i++)
        {
            string id="Drone"+i;var t=Texture("Drone0"+i);
            for(int d=0;d<4;d++)Cut(id+"_Walk_"+Dirs[d],t,d==3?new[]{R(0,1),R(1,1)}:new[]{R(d*2,0),R(d*2+1,0)});
            var scared=Texture("Drone0"+i+"_Scared");int w=scared.width/4,h=scared.height/2;
            for(int d=0;d<4;d++)Cut(id+"_Scared_"+Dirs[d],scared,new[]{R(d,0,w,h),R(d,1,w,h)});
            int row=i==4?3:2,col=(i==1||i==4)?0:2;
            Cut(id+"_Recovering",t,new[]{R(col,row),R(col+1,row)});
            Cut(id+"_Dead",t,new[]{R(col+2,row),R(col+3,row)},.65f);
        }
        var item=Texture("ItemsTiles");
        bonus=Cut("BonusSaturn",item,new[]{R(3,1)},.85f)[0];
        life=Cut("LifeIcon",item,new[]{R(4,1)},.7f)[0];
        sequences["PowerPulse"]=Cut("PowerPulse",item,new[]{R(1,1),R(2,1)},.65f);
    }
    static float Distance(Vector2 p,Vector2 a,Vector2 b)
    {var v=b-a;if(v.sqrMagnitude<.000001f)return Vector2.Distance(p,a);float u=Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude);return Vector2.Distance(p,a+v*u);}
    static Sprite GeometryTile(string name,int type)
    {
        const int size=64;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var colors=new Color[size*size];var points=new List<Vector2>();
        bool corner=type==1||type==3;
        if(corner)
        {
            points.Add(new Vector2(64,32));points.Add(new Vector2(43,32));
            for(int j=0;j<=12;j++){float a=(90+j*90f/12)*Mathf.Deg2Rad;points.Add(new Vector2(43+11*Mathf.Cos(a),21+11*Mathf.Sin(a)));}
            points.Add(new Vector2(32,0));
        }
        else {points.Add(new Vector2(0,32));points.Add(new Vector2(64,32));}
        bool outer=type==1||type==2||type==7;
        var bright=outer?new Color(.25f,.84f,.96f):new Color(.99f,.64f,.28f);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var p=new Vector2(x+.5f,y+.5f);float distance=999;
            for(int j=1;j<points.Count;j++)distance=Math.Min(distance,Distance(p,points[j-1],points[j]));
            if(type==7)distance=Math.Min(distance,Distance(p,new Vector2(32,32),new Vector2(32,0)));
            Color color=Color.clear;
            if(type==5)
            {
                float diamond=Mathf.Abs(p.x-32)+Mathf.Abs(p.y-32);
                if(diamond<8)color=diamond<4?new Color(1,.94f,.65f):new Color(1,.64f,.23f);
            }
            else if(type==8)
            {
                if(Mathf.Abs(p.y-32)<3&&x%12<8)color=new Color(.97f,.40f,.74f);
            }
            else if(distance<6)color=distance<1.2f?new Color(.82f,1,1):distance<2.7f?bright:new Color(.09f,.18f,.25f);
            colors[y*size+x]=color;
        }
        tex.SetPixels(colors);tex.Apply();string path=Root+"Sprites/Tiles/"+name+".png";
        File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.spritePixelsPerUnit=64;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void TileSprites()
    {
        foreach(int t in new[]{1,2,3,4,5,7,8})sequences["Tile"+t]=new[]{GeometryTile("Tile_"+t,t)};
        sequences["Tile6"]=new[]{sequences["PowerPulse"][0]};
    }
    static AnimationClip Clip(string path,Sprite[] frames,bool loop)
    {
        var clip=new AnimationClip{frameRate=10,name=Path.GetFileNameWithoutExtension(path)};
        float length=loop?.8f:1.2f;
        var keys=new ObjectReferenceKeyframe[frames.Length+1];
        for(int i=0;i<frames.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=length*i/frames.Length,value=frames[i]};
        keys[frames.Length]=new ObjectReferenceKeyframe{time=length,value=loop?frames[0]:frames[frames.Length-1]};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(AssetDatabase.LoadAssetAtPath<AnimationClip>(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static AnimatorController Controller(string name,string owner,string folder,string[] states,bool demoFlag)
    {
        string path=Root+"Animators/"+name+".controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path))AssetDatabase.DeleteAsset(path);
        var ac=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(demoFlag){ac.AddParameter("Showcase",AnimatorControllerParameterType.Bool);var p=ac.parameters;p[0].defaultBool=true;ac.parameters=p;}
        var machine=ac.layers[0].stateMachine;var made=new List<AnimatorState>();
        for(int i=0;i<states.Length;i++)
        {
            string state=states[i];var st=machine.AddState(state,new Vector3(240+(i%3)*220,80+(i/3)*100));
            st.motion=Clip(Root+"Animations/"+folder+"/"+state+".anim",sequences[owner+"_"+state],state!="Dead");
            made.Add(st);
        }
        machine.defaultState=made[0];
        for(int i=0;i<made.Count;i++)
        {
            var tr=made[i].AddTransition(made[(i+1)%made.Count]);tr.hasExitTime=true;tr.exitTime=states[i]=="Dead"?1f:3f;
            tr.duration=0;tr.hasFixedDuration=true;
            if(demoFlag)tr.AddCondition(AnimatorConditionMode.If,0,"Showcase");
        }
        return ac;
    }
    static void Controllers()
    {
        player=Controller("PacStudentAnimator","Player","Player",new[]{"Walk_Right","Walk_Down","Walk_Left","Walk_Up","Dead"},true);
        for(int i=0;i<4;i++)ghosts[i]=Controller("GhostAnimator_"+(i+1),"Drone"+(i+1),"Drone"+(i+1),new[]{"Walk_Right","Walk_Down","Walk_Left","Walk_Up","Scared_Right","Scared_Down","Scared_Left","Scared_Up","Recovering","Dead"},false);
        string path=Root+"Animators/PowerPelletAnimator.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path))AssetDatabase.DeleteAsset(path);
        power=AnimatorController.CreateAnimatorControllerAtPath(path);
        power.layers[0].stateMachine.AddState("Pulse").motion=Clip(Root+"Animations/Power/Pulse.anim",sequences["PowerPulse"],true);
    }
    static void Prefabs()
    {
        for(int i=1;i<=8;i++)
        {
            var go=new GameObject("Tile_"+i);go.AddComponent<SpriteRenderer>().sprite=sequences["Tile"+i][0];
            if(i==6)go.AddComponent<Animator>().runtimeAnimatorController=power;
            prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/Tiles/Tile_"+i+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
