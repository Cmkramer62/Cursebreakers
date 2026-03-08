using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectivesUI : MonoBehaviour {

    public GameObject[] traitsColumnB, traitsColumnC;
    public TextMeshProUGUI traitTitleB, traitTitleC;

    public void SetTraitBName(string name) {
        traitTitleB.text = name;
    }
    
    public void SetTraitCName(string name) {
        traitTitleC.text = name;
    }

    public void ButtonPressedB(int index) {
        CycleButton(index, true);
    }

    public void ButtonPressedC(int index) {
        CycleButton(index, false);
    }

    private void SetButton(int index, bool listB) {

    }

    private void CycleButton(int index, bool listB) {
        GameObject[] currentListReference = listB ? traitsColumnB : traitsColumnC;
        var highlightA = currentListReference[index].transform.GetChild(0).gameObject;
        var highlightB = currentListReference[index].transform.GetChild(1).gameObject;
        var highlightC = currentListReference[index].transform.GetChild(2).gameObject;
        bool activeA = highlightA.activeSelf;
        bool activeB = highlightB.activeSelf;
        bool activeC = highlightC.activeSelf;

        highlightA.SetActive(activeC);
        highlightB.SetActive(activeA);
        highlightC.SetActive(activeB);

        if(highlightB.activeSelf) { // If we are highlighting something as selected, turn off all other selected highlights in this row.
            for(int i = 0; i < currentListReference.Length; i++) {
                if(i != index && currentListReference[i].transform.GetChild(1).gameObject.activeSelf) {
                    currentListReference[i].transform.GetChild(0).gameObject.SetActive(true);
                    currentListReference[i].transform.GetChild(1).gameObject.SetActive(false);
                    currentListReference[i].transform.GetChild(2).gameObject.SetActive(false);
                }
            }
        }
        



        GameObject[] currentListReference2 = listB ? traitsColumnC : traitsColumnB;
        var highlightA2 = currentListReference2[index].transform.GetChild(0).gameObject;
        var highlightB2 = currentListReference2[index].transform.GetChild(1).gameObject;
        var highlightC2 = currentListReference2[index].transform.GetChild(2).gameObject;

        if(highlightB.activeSelf) {
            highlightA2.SetActive(false);
            highlightB2.SetActive(false);
            highlightC2.SetActive(true);
        }
        else if(highlightC.activeSelf) {
            highlightA2.SetActive(true);
            highlightB2.SetActive(false);
            highlightC2.SetActive(false);
        }


    }

}
