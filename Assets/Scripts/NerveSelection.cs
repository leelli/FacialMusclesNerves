using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NerveSelection : MonoBehaviour
{
    public bool selectedNerve = false;
    public bool showNerve = false; //nerve hidden by default

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!showNerve || !selectedNerve) // nerve default = hidden
        {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}

/*
    /*if (selectedNerve && showNerve) //if showNerve is true (button clicked) and nerve is associated with selected muscle
        {
            gameObject.SetActive(true);
            Debug.Log ("problem not here");
        }
*/

