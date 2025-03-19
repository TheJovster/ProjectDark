using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource _soundtrack;
    [SerializeField] private AudioSource _effects;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void PlaySoundtrack(AudioClip clip)
    {
        _soundtrack.clip = clip;
        _soundtrack.Play();
    }

    public void StopSoundtrack()
    {
        _soundtrack.Stop();
        _soundtrack.clip = null;
    }

    public void ResumeSoundtrack()
    {
        _soundtrack.Play();
    }

    public void PlayEffect(AudioClip clip)
    {
        if (_effects.clip != null)
        {
            _effects.clip = null;
        }
        _effects.PlayOneShot(clip);
    }

    public void PlayLevelMusic(AudioClip clip)
    {
        StopSoundtrack();
        _soundtrack.clip = clip;
        _soundtrack.Play();
    }
}
