using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;

public class ModalDialog : MonoBehaviour
{
    GameObject modalPanel;
    public bool Active { get; private set; }

    List<GameObject> gameObjects = new List<GameObject>();
    Action<string> done;

    // Start is called before the first frame update
    void Start()
    {
        this.Active = false;
        modalPanel = GetComponent<Transform>().Find("ModalPanel").gameObject;
        int sindex = modalPanel.GetComponent<Transform>().GetSiblingIndex();
        foreach (Transform c in GetComponent<Transform>())
        {
            if (sindex <= c.GetSiblingIndex())
            {
                gameObjects.Add(c.gameObject);
            }
        }
        Cancel();
        gameObjects.ToList().ForEach(o => {
            Button b = o.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.AddListener(() => onClicked(b.name));
            }
        });
    }
    public void Cancel()
    {
        this.Active = false;
        gameObjects.ToList().ForEach(o => o.SetActive(false));
    }
    void onClicked(string name)
    {
        if (this.done != null)
        {
            this.done(name);
        }
        Cancel();
    }
    public void DoModal(Action<string> done, string text = "")
    {
        this.Active = true;
        this.done = done;
        gameObjects.Where(o => o.name == "Text").First().GetComponent<Text>().text = text;
        gameObjects.ForEach(o => o.SetActive(true));
        StartCoroutine(Fade(0.1f));
    }
    IEnumerator Fade(float df)
    {
        var c = modalPanel.GetComponent<CanvasRenderer>().GetColor();
        c.a = df > 0 ? 0f : 1f;
        modalPanel.GetComponent<CanvasRenderer>().SetColor(c);
        for (var a = c.a; a >= 0f && a <= 1f; a += df)
        {
            c.a = a;
            modalPanel.GetComponent<CanvasRenderer>().SetColor(c);
            yield return new WaitForSeconds(0.1f);
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
