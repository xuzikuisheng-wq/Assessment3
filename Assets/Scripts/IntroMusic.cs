using System.Collections;
using UnityEngine;

public sealed class IntroMusic : MonoBehaviour
{
    public AudioSource music;
    public AudioClip intro;
    public AudioClip normal;

    IEnumerator Start()
    {
        if (!music || !intro || !normal)
        {
            Debug.LogError("Assign Music, Intro and Normal.", this);
            yield break;
        }
        music.loop = false;
        music.clip = intro;
        music.Play();
        yield return new WaitForSeconds(Mathf.Min(intro.length, 3f));
        music.Stop();
        music.clip = normal;
        music.loop = true;
        music.Play();
    }
}
