using UnityEngine;
using UnityEngine.UI;

public class SliderManager : MonoBehaviour
{
    public GameObject fullModel;
    public Slider slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void XRotator()//didn't use
    {
       Vector3 initialRot = fullModel.transform.localEulerAngles;
       Vector3 finalRot = initialRot + new Vector3 (slider.value, 0.0f, 0.0f);
       fullModel.transform.localRotation = Quaternion.Euler(finalRot);
    }

    public void YRotator()//horizontal slider
    {
        //Animator modelAnimator = fullModel.GetComponent<Animator>();
        //modelAnimator.SetFloat("RotateBy", slider.value);
        Vector3 initialRot = fullModel.transform.localEulerAngles;
        Vector3 finalRot = new Vector3 (0.0f, slider.value, 0.0f);
        fullModel.transform.localRotation = Quaternion.Euler(finalRot);
    }

    public void ZoomSlider()//vertical slider
    {
        //Animator modelAnimator = fullModel.GetComponent<Animator>();
        //modelAnimator.SetFloat("ZoomBy", slider.value);
        Vector3 finalZoom = new Vector3 (0.0f, 0.0f, slider.value);
        fullModel.transform.position = finalZoom;
    }

    

}
