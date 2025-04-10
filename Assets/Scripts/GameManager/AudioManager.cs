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

        if (_soundtrack != null)
        {
            _soundtrack.loop = true;
        }

        if (_effects != null)
        {
            _effects.loop = false;
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

    public void PlayEffectHalfVolume(AudioClip clip)
    {
        if (_effects.clip != null)
        {
            _effects.clip = null;
        }
        _effects.PlayOneShot(clip, 0.5f);
    }
    
    
    public void PlayEffectDoubleVolume(AudioClip clip)
    {
        if (_effects.clip != null)
        {
            _effects.clip = null;
        }
        _effects.PlayOneShot(clip, 2.0f);
    }
    
    public void PlayEffectVariableVolume(AudioClip clip, float volume)
    {
        if (_effects.clip != null)
        {
            _effects.clip = null;
        }
        _effects.PlayOneShot(clip, volume);
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
