using UnityEngine.Audio;
using System;
using UnityEngine;


public class SoundManager : MonoBehaviour
{
    public Sounds[] Sounds;

	void Awake()
	{
        foreach (Sounds s in Sounds) // properties can be access....
		{
			s.source = gameObject.AddComponent<AudioSource>();
			s.source.clip = s.clip ;
			s.source.volume = s.volume;
			s.source.playOnAwake = false;
		}
    }

	
	public void Play(string name)	
	{
		Sounds s = Array.Find(Sounds, sound => sound.name == name);
		s.source.Play();
	}
	
}
