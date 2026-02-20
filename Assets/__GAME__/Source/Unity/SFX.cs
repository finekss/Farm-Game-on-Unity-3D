
using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class SFXData
{
    public AudioClip clip;
    public float vol = 1f;
}
public class SFX : MonoBehaviour
{
    public static SFX i;
    public static float SFXVOL = 1f;

    private void Awake()
    {
        i = this;
    }
    public void Mute()
    {
        AudioListener.pause = !AudioListener.pause;
    }

    public void PlayAudio(SFXData[] clip)
    {
        if (clip.Length == 0)
            return;

        var sfxData = clip[Random.Range(0, clip.Length)];
        PlayAudio(sfxData.clip, sfxData.vol);
    }

    public void PlayAudio(AudioClip clip, float vol = 1f)
    {
        if (clip == null)
            return;
        
        var transformPosition = Camera.main.transform.position + Vector3.forward;
        AudioSource.PlayClipAtPoint(clip, transformPosition, vol * SFXVOL);
    }
}