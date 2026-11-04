using UnityEngine;
using System.Collections.Generic;

public class ManageButtonActions : MonoBehaviour
{
    Animator modelAnimator;
    private MuscleSelection muscleSelection;
    private NerveSelection nerveSelection;

    [SerializeField]
    public List<GameObject> nerveList = new List<GameObject>();

    [SerializeField]
    public List<GameObject> muscleList = new List<GameObject>();

    [SerializeField]
    public GameObject fullModel;   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modelAnimator = fullModel.GetComponent<Animator>();
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
        modelAnimator.SetBool("InteractionActive", true);
    }

    public void EndInteraction()
    {
        modelAnimator.SetBool("InteractionActive", false);
    }

    public void ResetModel()
    {
        //no time to code, but ultimately want a reset model button on the interactive 3d model screen
    }
}


/*
    //public GameObject[] nerveList;
    //public GameObject[] muscleList;
*/