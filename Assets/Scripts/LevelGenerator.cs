using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelGenerator : MonoBehaviour
{
    public GameObject manualLevel;
    public GameObject[] tilePrefabs = new GameObject[9];
    public Camera levelCamera;
    public int[,] levelMap =
    {
        { 1,2,2,2,2,2,2,2,2,2,2,2,2,7 },
        { 2,5,5,5,5,5,5,5,5,5,5,5,5,4 },
        { 2,5,3,4,4,3,5,3,4,4,4,3,5,4 },
        { 2,6,4,0,0,4,5,4,0,0,0,4,5,4 },
        { 2,5,3,4,4,3,5,3,4,4,4,3,5,3 },
        { 2,5,5,5,5,5,5,5,5,5,5,5,5,5 },
        { 2,5,3,4,4,3,5,3,3,5,3,4,4,4 },
        { 2,5,3,4,4,3,5,4,4,5,3,4,4,3 },
        { 2,5,5,5,5,5,5,4,4,5,5,5,5,4 },
        { 1,2,2,2,2,1,5,4,3,4,4,3,0,4 },
        { 0,0,0,0,0,2,5,4,3,4,4,3,0,3 },
        { 0,0,0,0,0,2,5,4,4,0,0,0,0,0 },
        { 0,0,0,0,0,2,5,4,4,0,3,4,4,8 },
        { 2,2,2,2,2,1,5,3,3,0,4,0,0,0 },
        { 0,0,0,0,0,0,5,0,0,0,4,0,0,0 }
    };
    public GameObject GeneratedRoot
    {
        get;
        private set;
    }
    public static int[,] Mirror(int[,] quarter)
    {
        int h=quarter.GetLength(0), w=quarter.GetLength(1);
        if(h<1||w<1) throw new ArgumentException("Map must not be empty.");
        var whole=new int[h*2-1,w*2];
        for(int r=0;r<whole.GetLength(0);r++)
        for(int c=0;c<whole.GetLength(1);c++)
        whole[r,c]=quarter[r<h?r:2*h-2-r,c<w?c:2*w-1-c];
        return whole;
    }
    void Start()
    {
        // Build successfully first; a malformed practice array does not erase the reference.
        var map=Mirror(levelMap);
        var rotations=TileConnections.Solve(map);
        GeneratedRoot=CreateLevel(map,rotations,tilePrefabs,"GeneratedLevel");
        if(manualLevel)
        {
            manualLevel.SetActive(false);
            Destroy(manualLevel);
        }
        var fit=levelCamera.GetComponent<FitLevelCamera>();
        fit.mapWidth=map.GetLength(1);
        fit.mapHeight=map.GetLength(0);
        fit.Apply();
    }
    public static Vector3 Cell(int r,int c,int height,int width)
    =>new Vector3(c-(width-1)*0.5f,(height-1)*0.5f-r,0);
    public static GameObject CreateLevel(int[,] map,int[] masks,GameObject[] prefabs,string name)
    {
        int h=map.GetLength(0),w=map.GetLength(1);
        var root=new GameObject(name);
        for(int r=0;r<h;r++)
        {
            var row=new GameObject($"Row_{r:00}");
            row.transform.SetParent(root.transform);
            for(int c=0;c<w;c++)
            {
                int type=map[r,c];
                if(type==0)continue;
                if(type<0||type>=prefabs.Length||!prefabs[type])throw new ArgumentException($"Missing tile {type}");
                var tile=Instantiate(prefabs[type],row.transform);
                tile.name=$"Tile_{r:00}_{c:00}_{type}";
                tile.transform.position=Cell(r,c,h,w);
                tile.transform.rotation=Quaternion.Euler(0,0,TileConnections.Angle(type,masks[r*w+c]));
            }
        }
        return root;
    }
}
// Ports U/R/D/L = 1/2/4/8. Adjacent walls need reciprocal ports, not merely adjacency.
public static class TileConnections
{
    static readonly int[] DR=
    { -1,0,1,0 }, DC=
    { 0,1,0,-1 }, Bit=
    { 1,2,4,8 }, Opp=
    { 4,8,1,2 };
    public static int[] Solve(int[,] map)
    {
        int h=map.GetLength(0),w=map.GetLength(1);
        var domains=new List<int>[h*w];
        for(int r=0;r<h;r++)for(int c=0;c<w;c++)
        {
            int t=map[r,c];
            if(t<0||t>8)throw new ArgumentException($"Unknown map legend {t} at {r},{c}");
            domains[r*w+c]=new List<int>(t==1||t==3?new[]
            { 6,12,9,3 }
            :t==2||t==4||t==8?new[]
            { 10,5 }
            :t==7?new[]
            { 14,13,11,7 }
            :new[]
            { 0 }
            );
        }
        int attempts=0;
        var solved=Search(map,domains,ref attempts);
        if(solved==null)throw new InvalidOperationException("No consistent wall connections. Check the custom map and its boundary openings.");
        var answer=new int[h*w];
        for(int i=0;i<answer.Length;i++)answer[i]=solved[i][0];
        return answer;
    }
    static int Family(int type,int mask,int port)
    {
        if(type==1||type==2)return 1;
        if(type==7)return ((mask&10)==10&&(port==2||port==8))||((mask&5)==5&&(port==1||port==4))?1:2;
        return 2;
    }
    static List<int>[] Search(int[,] map,List<int>[] domains,ref int attempts)
    {
        if(++attempts>20000)throw new InvalidOperationException("Map search budget exceeded; simplify an ambiguous map.");
        int h=map.GetLength(0),w=map.GetLength(1);
        bool changed=true;
        while(changed)
        {
            changed=false;
            for(int r=0;r<h;r++)for(int c=0;c<w;c++)
            {
                int index=r*w+c;
                var options=domains[index];
                for(int k=options.Count-1;k>=0;k--)
                {
                    int mask=options[k];
                    bool valid=true;
                    for(int d=0;d<4&&valid;d++)
                    {
                        int nr=r+DR[d],nc=c+DC[d];
                        bool linked=(mask&Bit[d])!=0;
                        if(nr<0||nr>=h||nc<0||nc>=w)
                        {
                            // Horizontal wall endpoints can open onto side tunnels.
                            if(linked&&!((c==0||c==w-1)&&(d==1||d==3)))valid=false;
                            continue;
                        }
                        bool supported=false;
                        foreach(int other in domains[nr*w+nc])
                        if(linked==((other&Opp[d])!=0)&&(!linked||Family(map[r,c],mask,Bit[d])==Family(map[nr,nc],other,Opp[d])))
                        {
                            supported=true;
                            break;
                        }
                        if(!supported)valid=false;
                    }
                    if(!valid)
                    {
                        options.RemoveAt(k);
                        changed=true;
                    }
                }
                if(options.Count==0)return null;
            }
        }
        int choice=-1,count=5;
        for(int i=0;i<domains.Length;i++)if(domains[i].Count>1&&domains[i].Count<count)
        {
            choice=i;
            count=domains[i].Count;
        }
        if(choice<0)return domains;
        foreach(int mask in domains[choice])
        {
            var copy=new List<int>[domains.Length];
            for(int i=0;i<copy.Length;i++)copy[i]=new List<int>(domains[i]);
            copy[choice]=new List<int>
            {
                mask
            };
            var result=Search(map,copy,ref attempts);
            if(result!=null)return result;
        }
        return null;
    }
    public static float Angle(int type,int mask)
    {
        int original=type==1||type==3?6:type==7?14:10;
        if(mask==0)return 0;
        for(int step=0;step<4;step++)
        {
            if(original==mask)return -90*step;
            original=((original<<1)&15)|((original>>3)&1);
        }
        throw new ArgumentException("Unsupported tile orientation.");
    }
}
