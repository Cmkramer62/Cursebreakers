using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour {

    public enum TutorialState { Waiting, Wasd, Shift, Crouch, Jump, Hiding, CursedObjects, TabTraitsAndClues, TabTools, Tools, Done}
    public TutorialState currentState = TutorialState.Waiting;

    public string[] tutorialLists;
    public Door[] tutorialDoors;
    public CursedObject[] pingObjects;
    public int index = 0, doorIndex = 0;

    public AudioClip textSound;
    public PlayerMovement playerScript;
    public ToolController toolScript;

    public bool usedHidingSpot = false, walkedAround = false, checkedInventory = false, finishedClues = false, toolDone = false;

    public float walkedAroundAmount = 0f;
    private TextPrompter textPromptScript;
    private Coroutine routine;
    private bool done = false;
    private float checkPingsInterval = 0f;

    private void OnEnable() {
        //start the first one. index = 0
    }


    private void Increment() {
        index++;
        // sound jingle
    }

    private void IncrementDoor() {
        tutorialDoors[doorIndex].UnlockDoor();
        index++;
        doorIndex++;
        // door unlock
        // better sound
    }

    private bool CheckPingObjects() {
        bool found = true;
        foreach(CursedObject curse in pingObjects) {
            if(!curse.pinged) found = false;
        }
        return found;
    }

    // Called by individual hitboxes for the major tutorial starts. Called by Update() for the mini tutorial starts.
    public void StartNextTutorial() {
        if(currentState == TutorialState.Waiting) {
            playerScript.allowedToMove = true;
            currentState = TutorialState.Wasd;
        }
        else if(currentState == TutorialState.TabTools) {
            finishedClues = true;
            toolScript.masterAllowed = true;
        }
        DisplayIt(tutorialLists[index]);
    }

    // Update is called once per frame
    void Update() {

        if(!walkedAround && (Input.GetAxis("Horizontal") + Input.GetAxis("Vertical") != 0)) {
            walkedAroundAmount += 1 * Time.deltaTime;
            if(walkedAroundAmount >= 5) {
                walkedAround = true;
            }
        }

        // The text box should already be there.
        if(currentState == TutorialState.Wasd && walkedAround) {
            // Done with movement.
            Increment();
            currentState = TutorialState.Shift;
            StartNextTutorial();
        }
        else if(currentState == TutorialState.Shift && playerScript.GetRemainingStam() < 0.9f) {//(currentState == TutorialState.Shift && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) && Input.GetKeyDown(KeyCode.LeftShift)) {
            // Done with sprinting.
            Increment();
            currentState = TutorialState.Crouch;
            StartNextTutorial();
        }
        else if(currentState == TutorialState.Crouch && Input.GetKeyDown(KeyCode.LeftControl)) {
            Increment();
            currentState = TutorialState.Jump;
            StartNextTutorial();
        }
        else if(currentState == TutorialState.Jump && Input.GetKeyDown(KeyCode.Space)) {
            IncrementDoor();
            StartNextTutorial();
            index++; // We are doing one more because of the door hint.
            currentState = TutorialState.Hiding;
        }
        else if(currentState == TutorialState.Hiding && usedHidingSpot) {
            IncrementDoor();
            currentState = TutorialState.CursedObjects;
        }
        else if(currentState == TutorialState.CursedObjects) {
            checkPingsInterval += 1 * Time.deltaTime;
            if(checkPingsInterval >= 2f) {
                checkPingsInterval = 0f;
                if(CheckPingObjects()) {
                    IncrementDoor();
                    currentState = TutorialState.TabTraitsAndClues;
                }
            }
        }
        else if(currentState == TutorialState.TabTraitsAndClues) {
            if(Input.GetKeyDown(KeyCode.Tab)) checkedInventory = true;
            if(checkedInventory == true && Input.GetKeyDown(KeyCode.Tab)) {
                IncrementDoor();
                currentState = TutorialState.TabTools;
            }
        }
        else if(currentState == TutorialState.TabTools && checkedInventory == true && Input.GetKeyDown(KeyCode.Tab) && finishedClues) {
            IncrementDoor();
            currentState = TutorialState.Tools;
        }
        else if(currentState == TutorialState.Tools && toolDone) {
            IncrementDoor();
            currentState = TutorialState.Done;
        }
    }


    private void DisplayIt(string displayText) {
        if(textPromptScript == null) textPromptScript = GameObject.Find("Game Manager").GetComponent<TextPrompter>();

        if(displayText.Contains("_")) {
            textPromptScript.QueueTextPrompt(displayText.Split('_'), textSound);
            var disList = displayText.Split('_');
            tutorialDoors[doorIndex].GetComponent<InteractPrompt>().displayText = disList[disList.Length-1];
        }
        else {
            textPromptScript.QueueTextPrompt(displayText, textSound);
            tutorialDoors[doorIndex].GetComponent<InteractPrompt>().displayText = displayText;
        }
        
    }

}
