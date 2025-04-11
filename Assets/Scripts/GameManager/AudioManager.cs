using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource _soundtrack;
    [SerializeField] private List<AudioSource> _effectSourceList = new();
    
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

        if (_soundtrack != null)
        {
            _soundtrack.loop = true;
        }

        if (_effectSourceList.Count > 0)
        {
           
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
        AudioSource src = _effectSourceList.Find(x => x.isPlaying == false);
        if (!src)
        {
            src = _effectSourceList[0];
        }
        src.PlayOneShot(clip);
    }

    public void PlayEffectHalfVolume(AudioClip clip)
    {
        AudioSource src = _effectSourceList.Find(x => x.isPlaying == false);
        if (!src)
        {
            src = _effectSourceList[0];
        }
        src.PlayOneShot(clip, 0.5f);
    }
    
    public void PlayEffectDoubleVolume(AudioClip clip)
    {
        AudioSource src = _effectSourceList.Find(x => x.isPlaying == false);
        if (!src)
        {
            src = _effectSourceList[0];
        }
        src.PlayOneShot(clip, 2.0f);
    }
    
    public void PlayEffectVariableVolume(AudioClip clip, float volume)
    {
        
    }

    public void PlayLevelMusic(AudioClip clip)
    {
        StopSoundtrack();
        _soundtrack.clip = clip;
        _soundtrack.Play();
    }

    public void PlayFootsteps(AudioClip clip)
    {
        
    }
    
}
