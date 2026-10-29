using UnityEngine;

public class MuscleNerveMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (FullModelBehavior.muscleSelected)
        {
            gameObject.SetActive(true);
            Vector3 mousePos = Input.mousePosition;
            transform.position = Vector3.Lerp(gameObject.transform.position, mousePos, 1);
        }
    }
}
