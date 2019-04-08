using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    Action start;
    Action end;
    Action next;
    float duration;
    float restTime = -1f;

    public Item(float duration, Action start, Action end, Action next = null)
    {
        this.duration = duration;
        this.start = start;
        this.end = end;
        this.next = next;
    }
    public void Start()
    {
        this.restTime = this.duration;
        this.start();
    }
    public void End()
    {
        if (this.restTime >= 0)
        {
            this.restTime = -1f;
            this.end();
        }
    }
    public bool Next(float deltaTime)
    {
        if (restTime < 0)
        {
            return false;
        }
        if (this.restTime > 0 && this.next != null)
        {
            this.next();
        }
        this.restTime = Mathf.Max(this.restTime - deltaTime, 0f);
        return this.restTime > 0;
    }
}
