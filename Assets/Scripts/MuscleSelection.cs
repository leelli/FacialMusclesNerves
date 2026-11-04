using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MuscleSelection : MonoBehaviour
{  
    public bool selectedMuscle = false;//specifies the selection
    public static bool muscleSelected = false;//specifies state of interaction
    private MeshRenderer meshRenderer;

    [SerializeField] public Material materialSelection;
    private Material materialOriginal;

    //link specific nerves
    [SerializeField] public GameObject[] nerveInnervation;
    //public List<GameObject> nerveInnervation = new List<GameObject>();

    private NerveSelection nerveSelection;
    //private FullModelBehavior fullModelBehavior;

    [SerializeField] public GameObject fullModel;
    [SerializeField] public GameObject menu;
    [SerializeField] public TMP_Text textLabel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialOriginal = meshRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    void OnMouseDown()
    {
        Debug.Log (transform.name + " clicked");
        Animator modelAnimator = fullModel.GetComponent<Animator>();
        if (modelAnimator.GetBool("InteractionActive")) //&& modelAnimator.GetBool("MuscleSelected"))
        {
            selectedMuscle = !selectedMuscle;

            if (selectedMuscle && !muscleSelected)
            {
                meshRenderer.sharedMaterial = materialSelection;
                menu.SetActive(true);
                muscleSelected = !muscleSelected;
                textLabel.text = gameObject.name;
            }
            else if (!selectedMuscle)
            {
                meshRenderer.sharedMaterial = materialOriginal;
                menu.SetActive(false);
                muscleSelected = !muscleSelected;
                textLabel.text = "";
            }

            foreach (GameObject nerve in nerveInnervation)
            {
                nerveSelection = nerve.GetComponent<NerveSelection>();
                if (nerveSelection != null)
                {
                    Debug.Log ("nerve variable changed");
                    nerveSelection.selectedNerve = selectedMuscle;
                    nerveSelection.showNerve = selectedMuscle;
                }
            }


        }
    }

}

/*recycling if needed
        if (selectedMuscle && hideMuscle)//if Hide Muscle button is clicked hideMuscle is true
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
