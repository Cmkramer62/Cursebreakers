using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractPrompt : MonoBehaviour {

    public string displayText = "This doesn't look important.";
    public AudioClip textSound, interactWithSound;
    public bool list = false, oneTime = false, touchTrigger = false, curseObjectInteract = false, hubTutorial = false;
    public float volumeOfPopup = 0.2f; // Should be same as InteractRaycast's volume float.
    public float delay = 0f;
    public GameObject[] objectsToAffect;
    

    private TextPrompter textPromptScript;
    private Coroutine routine;
    private bool done = false;

    private void OnTriggerEnter(Collider other) {
        if(touchTrigger && other.name.Equals("Player") && ((oneTime && !done) || !oneTime)) {
            InteractWithObject();
            if(textPromptScript == null) textPromptScript = GameObject.Find("Game Manager").GetComponent<TextPrompter>();
            textPromptScript.source.PlayOneShot(interactWithSound, volumeOfPopup);

            if(hubTutorial) GameObject.FindAnyObjectByType<AudioSettings>().SetHubFirst();
        }
    }

    public void InteractWithObject() {
        if((oneTime && !done) || !oneTime) {
            if(delay == 0f) DisplayIt();
            else {
                if(routine != null) StopCoroutine(routine);
                routine = StartCoroutine(DisplayTimer());
            }
            done = true;
        }
    }

    public void EndEffect() {
        foreach(GameObject objectee in objectsToAffect) {
            objectee.SetActive(!objectee.activeSelf);
        }
    }


    private IEnumerator DisplayTimer() {
        yield return new WaitForSeconds(delay);
        DisplayIt();
    }

    private void DisplayIt() {
        if(textPromptScript == null) textPromptScript = GameObject.Find("Game Manager").GetComponent<TextPrompter>();

        if(!curseObjectInteract) {
            if(!list) textPromptScript.QueueTextPrompt(displayText, textSound, this);
            else textPromptScript.QueueTextPrompt(displayText.Split('_'), textSound, this);
        }
        else {
            textPromptScript.GetComponent<PurificationManager>().DisplayQuestion();
            textPromptScript.GetComponent<PurificationManager>().potentialCursedItem = gameObject;
            textPromptScript.GetComponent<PurificationManager>().cursedObjectScript = gameObject.GetComponentInChildren<CursedObject>();

        }

    }
}
