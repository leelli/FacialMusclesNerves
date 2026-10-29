using UnityEngine;
using UnityEngine.UI;

public class FullModelBehavior : MonoBehaviour
{
    private Vector3 initialRot;
    private Vector3 initialPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

    public void ResetModel()
    {
        transform.localRotation = Quaternion.Euler(initialRot);
        transform.position = initialPos;
    }
}
