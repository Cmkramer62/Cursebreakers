using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectivesUI : MonoBehaviour {

    [SerializeField] private Toggle[] clueAttriToggles;

    // 0=Stellae, 1=Radiatio, 2=Fulgor, 3=Algor, 4=Profanus, 5=Evocare
    [SerializeField] private GameObject[] attributeImages;
    [SerializeField] private GameObject threeCluesWarning;

    private int toggleIndex = -1;

    public void SetAttributeToChange(int index) {
        toggleIndex = index;
    }

    // Called by the toggle buttons and also Game Manager, based on how many are free.
    public void ToggleTrue(bool state) {
        attributeImages[toggleIndex].SetActive(state);
        clueAttriToggles[toggleIndex].isOn = state;

        CheckIfTooManyCluesMarked();
    }

    private void CheckIfTooManyCluesMarked() {
        int amountActive = 0;
        foreach(Toggle toggleState in clueAttriToggles) {
            amountActive += toggleState.isOn ? 1 : 0;
        }
        threeCluesWarning.SetActive(amountActive > 3);
    }

}

