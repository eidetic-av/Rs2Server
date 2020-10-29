using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rs2RuntimeController : MonoBehaviour
{
    bool ToggleUI = true;
    GameObject UI;

    void Start()
    {
        UI = GameObject.Find("UI");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            UI.SetActive(ToggleUI = !ToggleUI);
    }
}
