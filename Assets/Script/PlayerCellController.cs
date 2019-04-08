using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class PlayerCellController : MonoBehaviour
{
    Dictionary<string, int[]> nextPosition = new Dictionary<string, int[]>()
    {
        {"up",      new int[]{ 0, 1, 0, 1 } },//グローバル座標におけるx,zの変化量
        {"down",    new int[]{ 0,-1, 0, 1 } },
        {"left",    new int[]{-1, 0, 0, 1 } },
        {"right",   new int[]{ 1, 0, 0, 1 } },
    };
    // moving direction, rotating axis
    Dictionary<string, int[]> nextAction = new Dictionary<string, int[]>()
    {
        {"up",      new int[]{0, 1, 0, 0 } },//ローカル座標におけるx,z軸の変化量
        {"down",    new int[]{0,-1, 0, 0 } },
        {"left",    new int[]{0, 0,-90, 0 } },
        {"right",   new int[]{0, 0, 90, 0 } },
    };

    Dictionary<string, int[]> actions;
    public int ActionType
    {
        set { actions = value == 0 ? nextPosition : nextAction; }
    }

    Floor floor;
    PlayerMotion pmotion;

    public float AutoMovingSpan { get; set; }
    float autoMovedTime = 0f;
    float autoMovingSpeed = 1.0f;

    public AudioClip audio_walk;
    public AudioClip audio_turn;
    public AudioClip audio_hit_wall;
    public float volume = 0.1f;
    Dictionary<string, AudioClip> sounds;
    AudioSource audio_source;

    Dictionary<string, Action> triggerActions = new Dictionary<string, Action>();
    public void AddTriggerAction(string opponent, Action a)
    {
        triggerActions[opponent] = a;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (triggerActions.ContainsKey(other.name))
        {
            triggerActions[other.name]();
        }
    }
    ModalDialog dlg;

    public Transform TrackingObject { get; set; }
    LineRenderer radarLine;

    List<int> nextNavigation = new List<int>();
    Color32 defaultColor;
    RouteRenderer routeRenderer;


    // Start is called before the first frame update
    void Start()
    {
        //ActionType =  1;
        floor = GameObject.Find("Floor").GetComponent<Floor>();
        pmotion = GetComponent<PlayerMotion>();

        dlg = GameObject.Find("Canvas").GetComponent<ModalDialog>();

        audio_source = gameObject.AddComponent<AudioSource>();
        sounds = new Dictionary<string, AudioClip>(){
            { "walk", audio_walk },
            { "turn", audio_turn },
            { "hit_wall",audio_hit_wall },
        };

        radarLine = gameObject.AddComponent<LineRenderer>();
        radarLine.material = new Material(Shader.Find("Mobile/Particles/Additive"));
        radarLine.startColor = new Color32(12, 220, 12, 128);
        radarLine.endColor = radarLine.startColor;
        radarLine.startWidth = 0.1f;
        radarLine.endWidth = 0.1f;

        defaultColor = GetComponent<Transform>().Find("Body").GetComponent<Renderer>().material.color;
        Debug.Log(defaultColor);
        routeRenderer = gameObject.AddComponent<RouteRenderer>();

    }

    public void SetLayer(int layer)
    {
        gameObject.layer = layer;
        GetComponent<Transform>().Find("Body").gameObject.layer = layer;
    }
    void ViewRadar(float camera_angle, float toCheckDistance, Vector3 p0)
    {
        float da = 2.0f;
        radarLine.positionCount = Mathf.CeilToInt(camera_angle / da) + 2;
        radarLine.SetPosition(0, p0);
        radarLine.SetPosition(radarLine.positionCount - 1, p0);
        Vector3 forward = GetComponent<Transform>().localRotation * Vector3.forward * toCheckDistance;
        for (int cnt = 0; cnt < radarLine.positionCount - 2; cnt++)
        {
            Quaternion q = Quaternion.Euler(0f, da * cnt - camera_angle / 2f, 0f);
            radarLine.SetPosition(cnt + 1, q * forward + p0);
        }
        radarLine.startWidth = 0.1f;
        radarLine.endWidth = 0.1f;
        radarLine.startColor = new Color32(12, 220, 12, 128);
        radarLine.endColor = radarLine.startColor;
        radarLine.startWidth = 0.1f;
        radarLine.endWidth = 0.1f;
    }
    int Search(float camera_angle, float toCheckDistance, Vector3 p0, Transform target)
    {
        float targetWidthRatio = 0.9f;
        Vector3[] pts =
        {
            (Vector3.Cross(Vector3.up,target.position-p0).normalized * target.localScale.x*targetWidthRatio/2f + target.position - p0).normalized,
            (Vector3.Cross(Vector3.up,p0-target.position).normalized * target.localScale.x*targetWidthRatio/2f + target.position - p0).normalized,
        };

        int index = -1;
        if (
            pts.Any(v => {
                if (Mathf.Abs(Vector3.Angle(GetComponent<Transform>().localRotation * Vector3.forward, v)) <= camera_angle / 2f)
                {
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(p0, v, out hit, toCheckDistance))
                    {
                        if (hit.collider.gameObject.name.StartsWith("Block") == false)
                        {
                            return true;
                        }
                    }
                }
                return false;
            })
        )
        {
            index = floor.blocks.GetBlockIndex(target.GetComponent<Transform>().position);
        }
        else
        {
            index = -1;
        }
        return index;
    }

    // Update is called once per frame
    void Update()
    {
        if (dlg.Active == true)
        {
            return;
        }
        if(TrackingObject != null)
        {
            float camera_angle = 90f;
            float toCheckDistance = 6f;
            ViewRadar(camera_angle, toCheckDistance, GetComponent<Transform>().position);
            int index = Search(camera_angle, toCheckDistance, GetComponent<Transform>().position, TrackingObject);
            if (index == -1)
            {
                if (floor.BGM() == "Warning")
                {
                    floor.BGM("Default");
                }
                TrackingObject.GetComponent<PlayerCellController>().SetColor();
            }
            else
            {
                floor.BGM("Warning");
                TrackingObject.GetComponent<PlayerCellController>().SetColor(new Color32(255, 0, 255,
                    TrackingObject.GetComponent<PlayerCellController>().defaultColor.a));
                autoMovingSpeed = 5.0f;
                if (nextNavigation.Count == 0 || nextNavigation[nextNavigation.Count - 1] != index)
                {
                    pmotion.Cancel();
                    autoMovedTime = Time.realtimeSinceStartup - AutoMovingSpan;
                    List<int> route = floor.blocks.Solve(floor.blocks.GetBlockIndex(GetComponent<Transform>().position), index);
                    nextNavigation.Clear();
                    for (int cnt = 1; cnt < route.Count; cnt++)
                    {
                        nextNavigation.Add(route[cnt]);
                    }
                    Debug.Log(route);
                    Debug.Log(floor.blocks);
                    routeRenderer.Render(route, i => floor.blocks.GetBlockPosition(i), Color.red);
                }
            }
        }
        if (AutoMovingSpan.CompareTo(0f) == 0)
        {
            foreach (var elem in actions)
            {
                if (Input.GetKeyDown(elem.Key))
                {
                    Move(elem.Value);
                }
            }
        }
        else if (Time.realtimeSinceStartup > autoMovedTime + AutoMovingSpan / autoMovingSpeed)
        {
            autoMovedTime = Time.realtimeSinceStartup;
            pmotion.Unset();

            int[] pos = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);

            bool moved = false;
            while (nextNavigation.Count > 0)
            {
                int next = nextNavigation[0];
                int x = floor.blocks.i2x(next);
                int z = floor.blocks.i2z(next);
                if (x - pos[0] != 0 || z - pos[1] != 0)
                {
                    Move(new int[] { x - pos[0], z - pos[1], 0, 1 }, () =>
                    {
                        nextNavigation.RemoveAt(0);
                        if (nextNavigation.Count == 0)
                        {
                            autoMovingSpeed = 1.0f;
                            routeRenderer.Clear();
                        }
                    });
                    moved = true;
                    break;
                }
                else
                {
                    nextNavigation.RemoveAt(0);
                }
            }

            if (moved == false)
            {

                List<string> avail = new List<string>();
                foreach (var d in nextPosition)
                {
                    if (floor.blocks.IsWall(pos[0] + d.Value[0], pos[1] + d.Value[1]) == false)
                    {
                        avail.Add(d.Key);
                    }
                }
                if (avail.Count != 0)
                {
                    Move(nextPosition[avail[UnityEngine.Random.Range(0, avail.Count)]]);
                }
            }
        }
        floor.UpdateObjPosition(gameObject.name, GetComponent<Transform>().position, GetComponent<Transform>().rotation);
    }
     
    public void SetColor()
    {
        SetColor(defaultColor);
    }

    public void SetColor(Color32 col)
    {
         GetComponent<Transform>().Find("Body").GetComponent<Renderer>().material.color = col;
    }
    public void Move(int[] pos, Action aniComplete = null)
    {
        pmotion.Unset();
        if (pos[0] != 0 || pos[1] != 0)
        {
            Vector3 d = new Vector3(pos[0], 0, pos[1]);
            if (pos[3] == 1)
            {
                Quaternion q = new Quaternion();
                q.SetFromToRotation(Vector3.forward, new Vector3(pos[0], 0, pos[1]));
                int y = Mathf.RoundToInt((q.eulerAngles.y - GetComponent<Transform>().eulerAngles.y)) % 360;
                if (y != 0)
                {
                    Turn(NormalizedDegree(y), null);
                }
            }
            else
            {
                d = GetComponent<Transform>().localRotation * d;
            }
            int[] index = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);
            Forward(index[0] + Mathf.RoundToInt(d.x), index[1] + Mathf.RoundToInt(d.z), aniComplete);
        }
        if (pos[2] != 0)
        {
            Turn(pos[2], aniComplete);
        }
    }
    float NormalizedDegree(float deg)
    {
        while (deg >= 180)
        {
            deg -= 360;
        }
        while (deg < -180)
        {
            deg += 360;
        }
        return deg;
    }
    void Forward(int x, int z, Action aniComplete)
    {
        if (floor.blocks.IsWall(x, z) == false)
        {
            Vector3 pos0 = GetComponent<Transform>().position;
            Vector3 pos1 = floor.blocks.GetBlockPosition(x, z);
            pos1.y = pos0.y;
            pmotion.Add(p =>
            {
                GetComponent<Transform>().position = (pos1 - pos0) * p + pos0;
            }, 0.5f, aniComplete, sounds["walk"], volume);
        }
        else
        {
            audio_source.PlayOneShot(sounds["hit_wall"], volume);
        }
    }
    void Turn(float deg, Action aniComplete)
    {
        float deg0 = GetComponent<Transform>().eulerAngles.y;
        float deg1 = RoundDegree(deg0 + deg);
        pmotion.Add(p =>
        {
            GetComponent<Transform>().rotation = Quaternion.Euler(0f, (deg1 - deg0) * p + deg0, 0f);
        }, 0.5f, aniComplete, sounds["turn"], volume);
    }
    float RoundDegree(float deg)
    {
        return Mathf.FloorToInt((deg + 45) / 90) * 90;
    }
    public void CancelMotions()
    {
        pmotion.Cancel();
    }

}