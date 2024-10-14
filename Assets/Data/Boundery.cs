using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Boundery")]
public class Boundery : ScriptableObject
{
    private float xLimit;
    private float yLimit;

    public float YLimit
    {
        get
        {
            CalculateBoundery();
            return yLimit;
        }
    }

    public float XLimit
    {
        get
        {
            CalculateBoundery();
            return xLimit;
        }
    }

    private void CalculateBoundery()
    {
        yLimit = Camera.main.orthographicSize;
        xLimit = yLimit * Screen.width / Screen.height;
    }

}