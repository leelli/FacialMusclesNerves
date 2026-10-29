using UnityEngine;
using UnityEngine.UI;

public class FullModelBehavior : MonoBehaviour
{
    //Animator animator;
    [SerializeField] private Transform targetCamera;
    private Vector3 initialRot;
    private Vector3 initialPos;

    public static bool muscleSelected;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //animator = GetComponent<Animator>();
        transform.LookAt(targetCamera);
        initialRot = transform.localEulerAngles;
        initialPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void XRotator(Slider slider)
    {
       Vector3 finalRot = initialRot + new Vector3 (slider.value, 0.0f, 0.0f);
       transform.localRotation = Quaternion.Euler(finalRot);
    }

    public void YRotator(Slider slider)
    {
       Vector3 finalRot = initialRot + new Vector3 (0.0f, slider.value, 0.0f);
       transform.localRotation = Quaternion.Euler(finalRot);
    }

    public void ZoomSlider(Slider slider)
    {
        Vector3 finalZoom = initialPos + new Vector3 (0.0f, 0.0f, slider.value);
        transform.position = finalZoom;
    }

    /*public void HomeModel()
    {
        animator.SetBool("InteractionActive", false);
    }

    public void ResetModel()
    {
        animator.SetBool("InteractionActive", true);
    }*/

    public void OnMouseDown()
    {
        muscleSelected = !muscleSelected;
    }

}
