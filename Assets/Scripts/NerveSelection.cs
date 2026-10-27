using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NerveSelection : MonoBehaviour
{
    public bool selectedNerve = false;
    public bool hideNerve = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowNerve()
    {
        selectedNerve = !selectedNerve;
    }
}

/*
 [SerializeField]
    objList[i].SetActive(!isHidden);

    public void ShowNerve()
    {
        if (showNerve)
        {
            
        }
    }
*/
