using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

using Random = UnityEngine.Random;

public class AudioManager : Singleton<AudioManager>
{
    public Sound[] sounds;

#pragma warning disable 0649
    [SerializeField] private AudioMixer mainMixer;
#pragma warning restore 0649

    public float MasterVolume
    {
        get
        {
            float parameter;
            mainMixer.GetFloat("MasterVolume", out parameter);
            return parameter;
        }
        set
        {
            mainMixer.SetFloat("MasterVolume", value);
        }
    }

    public float MusicVolume
    {
        get
        {
            float parameter;
            mainMixer.GetFloat("MusicVolume", out parameter);
            return parameter;
        }
        set
        {
            mainMixer.SetFloat("MusicVolume", value);
        }
    }

    public float SFXVolume
    {
        get
        {
            float parameter;
            mainMixer.GetFloat("SFXVolume", out parameter);
            return parameter;
        }
        set
        {
            mainMixer.SetFloat("SFXVolume", value);
        }
    }

    private void Awake()
    {
        if (!RegisterMe())
            return;

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.outputAudioMixerGroup = sound.mixerGroup;

            sound.source.clip = sound.clips[0];         

            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.ignoreListenerPause = sound.ignoreListenerPause;
        }
    }

    public void PlayAudio(bool fadeIn, string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        if(sound == null)
        {
            Debug.LogWarning("Audio clip " + name + " was not found");
            return;
        }

        if (!sound.source.isPlaying)
        {
            sound.source.clip = sound.clips[Random.Range(0, sound.clips.Length)];

            if (fadeIn)
            {
                StartCoroutine(FadeIn(3f, sound));
            }
            else
            {
                sound.source.Play();
            }                      
        }      
    }

    public void StopAudio(bool fadeOut, string name, string newAudioToPlay = "")
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        if (sound == null)
        {
            Debug.LogWarning("Audio clip " + name + " was not found");
            return;
        }

        if (sound.source.isPlaying)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOut(3f, newAudioToPlay, sound));
            }
            else
            {
                sound.source.Stop();
            }            
        }
    }

    public void PlayOneShotAudio(string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        if (sound == null)
        {
            Debug.LogWarning("Audio clip " + name + " was not found");
            return;
        }

        sound.source.PlayOneShot(sound.clips[Random.Range(0, sound.clips.Length)]);     
    }

    private IEnumerator FadeOut(float fadingTime, string soundToPlayeAfter, Sound sound)
    {
        float startVolume = sound.source.volume;
        float frameCount = fadingTime / Time.deltaTime;
        float framesPassed = 0;

        while (framesPassed <= frameCount)
        {
            var t = framesPassed++ / frameCount;
            sound.source.volume = Mathf.SmoothStep(startVolume, 0, t);
            yield return null;
        }

        sound.source.volume = 0;
        sound.source.Stop();

        if(soundToPlayeAfter != "")
        {
            PlayAudio(false, soundToPlayeAfter);
        }
    }
    private IEnumerator FadeIn(float fadingTime, Sound sound)
    {
        sound.source.Play();
        sound.source.volume = 0;

        float resultVolume = sound.volume;
        float frameCount = fadingTime / Time.deltaTime;
        float framesPassed = 0;

        while (framesPassed <= frameCount)
        {
            var t = framesPassed++ / frameCount;
            sound.source.volume = Mathf.SmoothStep(0, resultVolume, t);
            yield return null;
        }

        sound.source.volume = resultVolume;
    }
}
