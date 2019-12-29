using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideCursor : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    void Start()
    {
        Instantiate(target, transform);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Cursor.visible = false;
    }
}
