using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sounds 
{
    [HideInInspector]
    public AudioSource source;
    
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume;
    [Range(0f, 1f)]
    public float pitch;
    public bool loop;
}

