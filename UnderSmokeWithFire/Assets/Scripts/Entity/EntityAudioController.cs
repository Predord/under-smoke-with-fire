using System;
using UnityEngine;

using Random = UnityEngine.Random;

public class EntityAudioController : MonoBehaviour
{
    public Sound[] sounds;

    private void Awake()
    {
        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.outputAudioMixerGroup = sound.mixerGroup;

            sound.source.clip = sound.clips[0];

            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.ignoreListenerPause = sound.ignoreListenerPause;

            sound.source.spatialBlend = 1f;
        }
    }

    public void PlayAudio(string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        if (sound == null)
        {
            Debug.LogWarning("Audio clip " + name + " was not found");
            return;
        }

        if (!sound.source.isPlaying)
        {
            sound.source.clip = sound.clips[Random.Range(0, sound.clips.Length)];
            sound.source.Play();
        }
    }

    public void StopAudio(string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        if (sound == null)
        {
            Debug.LogWarning("Audio clip " + name + " was not found");
            return;
        }

        if (sound.source.isPlaying)
        {
            sound.source.Stop();
        }
    }

    public void PauseAllAudio()
    {
        foreach(var sound in sounds)
        {
            sound.source.Pause();
        }
    }

    public void UnPauseAllAudio()
    {
        foreach (var sound in sounds)
        {
            sound.source.UnPause();
        }
    }
}
