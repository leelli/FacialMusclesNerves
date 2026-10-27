using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MuscleSelect : MonoBehaviour
{  
    public bool selectedMuscle = false;
    public bool hideMuscle = false;
    public bool isHidden = false;
    private MeshRenderer meshRenderer;

    [SerializeField]
    public Material materialSelection;
    
    private Material materialOriginal;

    //link specific nerves
    [SerializeField]
    public GameObject[] nerveList;
    //public List<GameObject> nerveList = new List<GameObject>();

    private NerveSelection nerveSelection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialOriginal = meshRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        if (selectedMuscle)
        {
            Debug.Log (transform.name + " is selected");
            //show menu
        }
        if (!selectedMuscle)
        {
            Debug.Log (transform.name + " is not selected");
            //show menu
        }
    }

    void OnMouseDown()
    {
        Debug.Log (transform.name + " clicked");
        selectedMuscle = !selectedMuscle;
        
        if (selectedMuscle)
        {
            meshRenderer.sharedMaterial = materialSelection;
        }
        else if (!selectedMuscle)
        {
            meshRenderer.sharedMaterial = materialOriginal;
        }

        for (int i = 0; i < nerveList.Length; i++)
        {
            nerveList[i].nerveSelection.ShowNerve();
        }
    }

    public void HideMuscle()
    {
        if (hideMuscle && !selectedMuscle) //need to make button change this variable later
        {
            gameObject.SetActive(false);
        }
        else if (!hideMuscle && selectedMuscle)
        {
            gameObject.SetActive(true);
        }
    }
}

/*
 //nerveList.nerveSelection.ShowNerve();
        //nerveList.nerveSelection.selectedNerve = !nerveList.nerveSelection.selectedNerve;
            for (int i = 0; i < nerveList.Count; i++)
        {
            nerveList[i].SetActive(!isHidden);
        }
    clickCount ++; //3 clicks, at = 0, not highlighted. 
        if (clickCount >= materialSelection.Length)
        {
            clickCount = 0;
        }

        meshRenderer.sharedMaterial = materialSelection[clickCount];
*/
