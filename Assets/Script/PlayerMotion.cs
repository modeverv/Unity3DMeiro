using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerMotion : MonoBehaviour
{
    public class Animation
    {
        public float Duration { get; set; }
        public Action Complete { get; set; }
        Action<float> animate;
        public AudioClip Sound { get; set; }
        public float Volume { get; set; }

        public Animation(Action<float> a, float d, Action c = null, AudioClip sound = null, float volume = 0)
        {
            this.animate = a;
            this.Duration = d;
            this.Complete = c;
            this.Sound = sound;
            this.Volume = volume;
        }
        public void Animate(float p)
        {
            animate(p);
            if (p >= 1.0f && Complete != null)
            {
                Complete();
            }
        }
    }

    List<Animation> animations = new List<Animation>();
    float started_time = 0;
    AudioSource audio_source;

    public void Add(Action<float> animate, float duration, Action complete = null, AudioClip sound = null, float volume = 0)
    {
        Add(new Animation(animate, duration, complete, sound, volume));
    }
    public void Add(Animation[] anis)
    {
        foreach (Animation ani in anis)
        {
            Add(ani);
        }
    }
    public void Add(Animation ani)
    {
        this.animations.Add(ani);
    }
    public void Unset()
    {
        animations.ForEach(a => a.Animate(1f));
        animations.Clear();
        started_time = 0f;
    }
    public void Cancel()
    {
        animations.Clear();
        started_time = 0f;
    }
    public void Set(Action<float> animate, float duration, Action complete = null)
    {
        Unset();
        Add(new Animation(animate, duration, complete));
    }


    // Start is called before the first frame update
    void Start()
    {
        audio_source = gameObject.AddComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animations.Count > 0)
        {
            if (started_time.CompareTo(0f) == 0)
            {
                started_time = Time.realtimeSinceStartup;
                if (animations[0].Sound != null)
                {
                    audio_source.PlayOneShot(animations[0].Sound, animations[0].Volume);
                }
            }
            float progress = (Time.realtimeSinceStartup - started_time) / animations[0].Duration;
            animations[0].Animate(Mathf.Min(1f, progress));
            if (progress >= 1.0f)
            {
                animations.RemoveAt(0);
                started_time = 0f;
            }
        } 
    }
}
