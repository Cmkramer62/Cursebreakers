using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Animations;

public class PurificationManager : MonoBehaviour {
    
    public GameObject mainCamera;
    [HideInInspector]
    public GameObject potentialCursedItem;
    public int ritualTimer;
    public ConeLOSDetector ghostVisionScript;
    public Enemy ghostScript;
    public bool allowedToDisplayQuestion = false;
    public EndPortal endPortalScript;
    [HideInInspector]
    public CursedObject cursedObjectScript;

    private bool allowedToTimer = true;
    private Coroutine purifyRoutine;

    public void DisplayQuestion() {
        if(!allowedToDisplayQuestion && allowedToTimer) {
            GetComponent<PauseGame>().ActivateQuestionPause();
        }
    }

    public void StartPurificationRitual() {
        DisplayQuestion(); // Turning off the question UI.
        GetComponent<CurseGameManager>().purifyState = potentialCursedItem.name == "Goal Curse" ? -1 : 1;
        purifyRoutine = StartCoroutine(PurificationTimer());

        if(ghostScript.invisible.Value) ghostScript.InvertVisibilityServerRpc();
        ghostVisionScript.visibilityOverride = true;
        endPortalScript.activated = true;
        allowedToDisplayQuestion = true;
    }

    private IEnumerator PurificationTimer() {
        yield return new WaitForSeconds(1f);
        cursedObjectScript.pSourceA.Play();
        yield return new WaitForSeconds(1f);
        cursedObjectScript.pSourceB.Play();
        yield return new WaitForSeconds(1f);
        cursedObjectScript.purificationParticles.Play();

        yield return new WaitForSeconds(ritualTimer);
        if(allowedToTimer) GetComponent<Death>().Jumpscare(false);
    }

    public void KillTimer() {
        allowedToTimer = false;
        if(purifyRoutine != null) StopCoroutine(purifyRoutine);
    }
}
