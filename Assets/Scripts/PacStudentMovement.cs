using UnityEngine;
public sealed class PacStudentMovement : MonoBehaviour
{
    public float speed=3f;
    public Animator animator;
    public AudioSource movingAudio;
    public bool moving=true;
    public Vector3[] corners=
    {
        new Vector3(-12.5f,13),new Vector3(-7.5f,13),new Vector3(-7.5f,9),new Vector3(-12.5f,9)
    };
    int segment;
    float elapsed;
    readonly string[] states=
    {
        "Walk_Right","Walk_Down","Walk_Left","Walk_Up"
    };
    void Start()
    {
        ResetRoute();
    }
    public void ResetRoute()
    {
        segment=0;
        elapsed=0;
        transform.position=corners[0];
        if(animator)
        {
            animator.SetBool("Showcase",false);
            animator.Play(states[0],0,0);
        }
        if(moving&&movingAudio&&!movingAudio.isPlaying)movingAudio.Play();
    }
    public static Vector3 Sample(Vector3 from,Vector3 to,float distance)
    {
        float length=Vector3.Distance(from,to);
        return length==0?to:from+(to-from)*Mathf.Clamp01(distance/length);
    }
    void Update()
    {
        if(!moving||speed<=0||corners.Length<2)
        {
            if(movingAudio&&movingAudio.isPlaying)movingAudio.Stop();
            return;
        }
        if(movingAudio&&!movingAudio.isPlaying)movingAudio.Play();
        elapsed+=Time.deltaTime;
        int guard=0;
        while(guard++<1000)
        {
            float duration=Vector3.Distance(corners[segment],corners[(segment+1)%corners.Length])/speed;
            if(duration<=0)throw new System.InvalidOperationException("Route contains duplicate corners.");
            if(elapsed<duration)break;
            elapsed-=duration;
            segment=(segment+1)%corners.Length;
            if(animator)animator.Play(states[segment%4],0,0);
        }
        transform.position=Sample(corners[segment],corners[(segment+1)%corners.Length],elapsed*speed);
    }
}
