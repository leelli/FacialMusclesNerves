using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DropdownInfo : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown infoDropdown;
    [SerializeField] private GameObject facialNerveInfo;
    [SerializeField] private GameObject functionsInfo;
    [SerializeField] private GameObject palsyInfo;
    [SerializeField] private GameObject citation;

    public void GetDropDownValue ()
    {
        int pickedEntryIndex = infoDropdown.value;
        Debug.Log (pickedEntryIndex);
    }
    public void DropdownOptions (int value)
    {
        if (value ==0)
        {
            facialNerveInfo.SetActive(true);
            functionsInfo.SetActive(false);
            palsyInfo.SetActive(false);
            citation.SetActive(false);
        }
        if (value ==1)
        {
            facialNerveInfo.SetActive(false);
            functionsInfo.SetActive(true);
            palsyInfo.SetActive(false);
            citation.SetActive(false);
        }
        if (value ==2)
        {
            facialNerveInfo.SetActive(false);
            functionsInfo.SetActive(false);
            palsyInfo.SetActive(true);
            citation.SetActive(false);
        }
        if (value ==3)
        {
            facialNerveInfo.SetActive(false);
            functionsInfo.SetActive(false);
            palsyInfo.SetActive(false);
            citation.SetActive(true);
        }
    }

}
