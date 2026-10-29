using UnityEngine;
using System.Collections.Generic;

public class ManageButtonActions : MonoBehaviour
{
    private MuscleSelection muscleSelection;
    private NerveSelection nerveSelection;

    [SerializeField]
    public List<GameObject> nerveList = new List<GameObject>();

    [SerializeField]
    public List<GameObject> muscleList = new List<GameObject>();

    public static bool interactionActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void HideMuscle() //button to hide SELECTED muscle
    {
        foreach (GameObject muscle in muscleList)
        {
            muscleSelection = muscle.GetComponent<MuscleSelection>();
            if (muscleSelection.selectedMuscle) // muscleSelection.hideMuscle &&  if Hide Muscle button is clicked hideMuscle is true
            {
                muscle.SetActive(false);
            }
            else //if (!hideMuscle && selectedMuscle) //if selected and hideMuscle button NOT clicked
            {
                muscle.SetActive(true);
            }
        }
        
    }

    public void ShowMuscle() //button to hide SELECTED muscle
    {
        foreach (GameObject muscle in muscleList)
        {
            muscleSelection = muscle.GetComponent<MuscleSelection>();
            if (muscleSelection.selectedMuscle) // muscleSelection.hideMuscle &&  if Hide Muscle button is clicked hideMuscle is true
            {
                muscle.SetActive(true);
            }
        }
        
    }


    public void ShowNerve() //button to show associated nerves of SELECTED muscle
    {
        foreach (GameObject nerve in nerveList)
        {
            nerveSelection = nerve.GetComponent<NerveSelection>();
            if (nerveSelection.selectedNerve) //nerveSelection.showNerve &&  if showNerve is true (button clicked) and nerve is associated with selected muscle
            {
                nerve.SetActive(true);
            }
            else //by default nerves are hidden
            {
                nerve.SetActive(false);
            }
        }
    }

    public void HideNerve() //button to show associated nerves of SELECTED muscle
    {
        foreach (GameObject nerve in nerveList)
        {
            nerveSelection = nerve.GetComponent<NerveSelection>();
            if (nerveSelection.selectedNerve) //nerveSelection.showNerve &&  if showNerve is true (button clicked) and nerve is associated with selected muscle
            {
                nerve.SetActive(false);
            }
        }
    }

    public void StartInteraction()
    {
        interactionActive = true;
    }

    public void EndInteraction()
    {
        interactionActive = false;
    }
}


/*
    //public GameObject[] nerveList;
    //public GameObject[] muscleList;
*/