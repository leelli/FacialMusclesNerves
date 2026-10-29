using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MuscleSelection : MonoBehaviour
{  
    public bool selectedMuscle = false;
    //public bool hideMuscle = false; //muscle is NOT hidden by default
    private MeshRenderer meshRenderer;

    [SerializeField]
    public Material materialSelection;
    
    private Material materialOriginal;

    //link specific nerves
    [SerializeField]
    public GameObject[] nerveInnervation;
    //public List<GameObject> nerveInnervation = new List<GameObject>();

    private NerveSelection nerveSelection;


    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialOriginal = meshRenderer.material;

        /*if (!hideMuscle) // muscle default = NOT hidden
        {
            gameObject.SetActive(true);
        }*/
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    void OnMouseEnter()
    {
        Debug.Log ("fuck");
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

        foreach (GameObject nerve in nerveInnervation)
        {
            nerveSelection = nerve.GetComponent<NerveSelection>();
            if (nerveSelection != null)
            {
                Debug.Log ("nerve variable changed");
                nerveSelection.selectedNerve = selectedMuscle;
                //nerveSelection.showNerve = selectedMuscle;
            }
        }
    }

}

/*if (selectedMuscle && hideMuscle)//if Hide Muscle button is clicked hideMuscle is true
        {
            Debug.Log (transform.name + " is shown");
            //show menu
            gameObject.SetActive(false);
        }

        if (hideMuscle && selectedMuscle) //if Hide Muscle button is clicked hideMuscle is true
        {
            gameObject.SetActive(false);
        }
        else if (selectedMuscle) //if (!hideMuscle && selectedMuscle) //if selected and hideMuscle button NOT clicked
        {
            gameObject.SetActive(true);
        }

    clickCount ++; //3 clicks, at = 0, not highlighted. 
        if (clickCount >= materialSelection.Length)
        {
            clickCount = 0;
        }

        meshRenderer.sharedMaterial = materialSelection[clickCount];
*/
