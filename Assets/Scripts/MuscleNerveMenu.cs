using UnityEngine;

public class MuscleNerveMenu : MonoBehaviour
{
    public GameObject menuMuscleNerve;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        if (!FullModelBehavior.muscleSelected)
        {
            menuMuscleNerve.SetActive(false);
        }
        else if (FullModelBehavior.muscleSelected)
        {
            menuMuscleNerve.SetActive(true);
        }
    }


    public void MuscleNerve()
    {
        {
            menuMuscleNerve.SetActive(true);
            /*Vector3 mousePos = Input.mousePosition;
            GameObject menuClone = 
                Instantiate(menuPrefab, mousePos, transform.rotation);*/
        }

    }
}
