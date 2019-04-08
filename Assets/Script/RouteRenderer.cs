using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


public class RouteRenderer : MonoBehaviour
{
    LineRenderer line;

    // Use this for initialization
    void Start()
    {
        line = (new GameObject("RouterRenderer")).AddComponent<LineRenderer>();
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.material = new Material(Shader.Find("Mobile/Particles/Additive"));
    }
    public void Render(List<int> route, Func<int, Vector3> i2p, Color color)
    {
        //Debug.Log(color);
        line.startColor = color;
        line.endColor = line.startColor;
        line.positionCount = route.Count;
        foreach (var item in route.Select((v, i) => new { v, i }))
        {
            line.SetPosition(item.i, i2p(item.v));
        }
        line.startColor = color;
        line.endColor = line.startColor;
    }
    public void Render(List<int> route, Func<int, Vector3> i2p)
    {
        Render(route, i2p, Color.yellow);
    }
    public void Clear()
    {
        line.positionCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
