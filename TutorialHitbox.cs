using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialHitbox : MonoBehaviour {

    public Tutorial tutorialScript;
    public bool done = false;

    private void OnTriggerEnter(Collider other) {
        if(!done && other.name.Equals("Player")) {
            tutorialScript.StartNextTutorial();
            done = true;
        }
    }
}

