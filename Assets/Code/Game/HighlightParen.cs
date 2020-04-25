﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HighlightParen : MonoBehaviour
{
    private float power;
    private Image im;
    private Color oldColor;
    private Color newColor = new Color(1f, 0.07f, 0.46f);
    private IEnumerator myloop;

    private void Awake()
    {
        myloop = loop();
    }

    // Start is called before the first frame update
    void Start()
    {
        im = GetComponent<Image>();
        oldColor = im.color;
        
    }

    private void OnEnable()
    {
        StartCoroutine(myloop);
    }

    private void OnDisable()
    {
        StopCoroutine(myloop);
    }

    IEnumerator loop()
    {
        yield return null;
        while (true)
        {
            if (!im)
                Start();
            if(0 < power)
                im.color = Color.Lerp(im.color,newColor,Time.deltaTime * 10);
            if(power <= 0)
                im.color = Color.Lerp(im.color,oldColor,Time.deltaTime * 30);
            
            
            yield return null;
        }
    }

    public void toggleOn()
    {
        power = 1;
    }

    public void toggleOff()
    {
        power = 0;
    }



}
