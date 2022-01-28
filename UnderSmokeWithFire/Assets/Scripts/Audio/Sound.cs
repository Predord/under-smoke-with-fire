using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound 
{
    public string name;

    public AudioMixerGroup mixerGroup;
    public AudioClip[] clips;

    [Range(0f, 1f)]
    public float volume;
    [Range(.1f, 3f)]
    public float pitch;

    public bool loop;
    public bool ignoreListenerPause;

    [HideInInspector]
    public AudioSource source;
}
