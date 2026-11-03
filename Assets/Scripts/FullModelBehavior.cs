using UnityEngine;
using UnityEngine.UI;

public class FullModelBehavior : MonoBehaviour
{
    //Animator animator;
    [SerializeField] private Transform targetCamera;


    public static bool muscleSelected;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //animator = GetComponent<Animator>();
        transform.LookAt(targetCamera);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnMouseDown()
    {
        //muscleSelected = !muscleSelected;
    }

}
