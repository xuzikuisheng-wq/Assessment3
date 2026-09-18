using UnityEngine;
[ExecuteAlways,RequireComponent(typeof(Camera))]
public sealed class FitLevelCamera : MonoBehaviour
{
    public float mapWidth=28,mapHeight=29;
    public void Apply()
    {
        var c=GetComponent<Camera>();
        c.orthographic=true;
        c.rect=new Rect(0,0,1f,1);
        float aspect=Mathf.Max(0.1f,c.aspect);
        c.orthographicSize=Mathf.Max(mapHeight*0.5f+1.6f,(mapWidth*0.5f+1.6f)/aspect);
        transform.position=new Vector3(0,0,-10);
    }
    void OnEnable()
    {
        Apply();
    }
    void Update()
    {
        Apply();
    }
}
