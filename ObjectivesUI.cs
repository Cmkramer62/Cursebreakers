using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectivesUI : MonoBehaviour {

    public GameObject[] traitsColumnB, traitsColumnC;
    public TextMeshProUGUI traitTitleA, traitTitleB, traitTitleC;

    private string[] storedTitles = { "Glowing", "Radioactive", "Vision", "Freezing", "Unholy", "Whispering" };

    public void ButtonPressedB(int index) {
        CycleButton(index, true);
    }

    public void ButtonPressedC(int index) {
        CycleButton(index, false);
    }

    private void CycleButton(int index, bool listB) {
        GameObject[] currentListReference = listB ? traitsColumnB : traitsColumnC;
        GameObject[] oppositeListReference = listB ? traitsColumnC : traitsColumnB;
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
                if(i != index && currentListReference[i].transform.GetChild(1).gameObject.activeSelf) SetButton(currentListReference, i, 0);
            }
        }

        if(highlightB.activeSelf) SetButton(oppositeListReference, index, 2);
        else if(highlightC.activeSelf) SetButton(oppositeListReference, index, 0);

        SetNames();
    }

    private void SetNames() {
        int foundInBIndex = -1;
        int foundInCIndex = -1;

        for(int i = 0; i < traitsColumnB.Length; i++) {
            if(traitsColumnB[i].transform.GetChild(1).gameObject.activeSelf) {
                foundInBIndex = i;
            }
            if(traitsColumnC[i].transform.GetChild(1).gameObject.activeSelf) {
                foundInCIndex = i;
            }
        }

        traitTitleB.text = "\"" + (foundInBIndex == -1 ? "???" : storedTitles[foundInBIndex]) + "\"";
        traitTitleB.GetComponent<TextAdder>().endWord = "\"" + (foundInBIndex == -1 ? "???" : storedTitles[foundInBIndex]) + "\"";
        traitTitleC.text = "\"" + (foundInCIndex == -1 ? "???" : storedTitles[foundInCIndex]) + "\"";
        traitTitleC.GetComponent<TextAdder>().endWord = "\"" + (foundInCIndex == -1 ? "???" : storedTitles[foundInCIndex]) + "\"";
    }

    public void ClearRows(int index) {
        SetButton(traitsColumnB, index, 0);
        SetButton(traitsColumnC, index, 0);

        SetNames();
    }

    public void UntrueRows(int index) {
        SetButton(traitsColumnB, index, 2);
        SetButton(traitsColumnC, index, 2);

        SetNames();
    }

    public void SetFreebieTrait(int index) {
        traitTitleA.text = "\"" + storedTitles[index] + "\"";
        traitTitleA.GetComponent<TextAdder>().endWord = "\"" + storedTitles[index] + "\"";
    }

    private void SetButton(GameObject[] specificList, int buttonIndex, int childIndex) {
        GameObject[] subChildren = { specificList[buttonIndex].transform.GetChild(0).gameObject, 
            specificList[buttonIndex].transform.GetChild(1).gameObject, 
                specificList[buttonIndex].transform.GetChild(2).gameObject };

        for(int i = 0; i < subChildren.Length; i++) {
            subChildren[i].SetActive(i == childIndex);
        }
    }

    public void ClearAll() {
        for(int i = 0; i < traitsColumnB.Length; i++) {
            ClearRows(i);
        }
    }

}
