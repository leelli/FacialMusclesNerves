using UnityEngine;
using UnityEngine.UI;

public class FullModelBehavior : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;


    private Vector3 initialRot;
    private Vector3 initialPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.LookAt(targetCamera);
        initialRot = transform.localEulerAngles;
        initialPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ManageButtonActions.interactionActive)
        {
            Vector3 idleRot = new Vector3(0.0f, 0.2f, 0.0f);
            transform.Rotate(idleRot, Space.Self);
        }
        else if (ManageButtonActions.interactionActive)
        {
            Vector3 noRot = new Vector3(0.0f, 0.0f, 0.0f);
            transform.localRotation = Quaternion.Euler (initialRot);
        }
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

    public void HomeModel()
    {
        transform.localRotation = Quaternion.Euler(initialRot);
        transform.position = initialPos;
    }

    public void ResetModel()
    {
        transform.position = Vector3.Lerp(initialPos, initialPos + new Vector3 (29f, 0.0f,0.0f), 10f * Time.deltaTime);
        //position = middlePos * Time.deltaTime;
        transform.LookAt(targetCamera);
    }

}
