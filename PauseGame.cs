using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour {

    public GameObject normalUI, pausedUI, objectivesUI, questionUI;//, question, title, mapUI;
    public bool paused = false, questionPaused = false;
    public MouseLook mouseLook;
    public PlayerMovement playerMovement;

    public AudioSource source;
    public AudioClip openClip, closeClip;

    public GameObject[] pauseMainChildren, pauseMainButtons;

    public float inactivePauseButtonDarkenIntensity = .3f;
    public bool allowedToPause = true, allowedToQuestionPause = true, pauseCooldown = false;
    public Animator pauseUIAnimator;

    // Update is called once per frame
    void Update() {
        if(allowedToPause && (Input.GetKeyDown(KeyCode.Tab))) {
            //mapUI.SetActive(false);
            PauseGameHandler();
        }
    }

    // Pauses game if playing, unpauses if paused. Can be remotely called by the PostRoundManager
    public void PauseGameHandler() {
        if(!pauseCooldown && !paused) {
            source.PlayOneShot(openClip);
            normalUI.SetActive(false);
            pausedUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            paused = true;

            mouseLook.allowedToLook = false;
            playerMovement.allowedToMove = false;
            pauseUIAnimator.SetBool("Paused", true);
        }
        else if(!pauseCooldown){
            StartCoroutine(PauseCooldownTimer());
        }
    }

    private IEnumerator PauseCooldownTimer() {
        pauseCooldown = true;
        pauseUIAnimator.SetBool("Paused", false);
        normalUI.SetActive(true);
        source.PlayOneShot(closeClip);

        yield return new WaitForSeconds(0.375f);
        //AudioManager.PlayDeny(); // should make more entries for this, such as the open and close used here?

        pausedUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        paused = false;

        mouseLook.allowedToLook = true;
        playerMovement.allowedToMove = true;
        ExitPausedUI();
        pauseCooldown = false;
    }

    public void ActivateQuestionPause() {
        if(allowedToQuestionPause) {
            source.PlayOneShot(openClip);
            if(!questionPaused) {
                //normalUI.SetActive(false);
                questionUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                allowedToPause = false;
                questionPaused = true;
                GetComponent<ToolController>().allowedToCycle = false;
                mouseLook.allowedToLook = false;
                playerMovement.allowedToMove = false;
            }
            else {
                //normalUI.SetActive(true);
                questionUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                allowedToPause = true;
                questionPaused = false;
                GetComponent<ToolController>().allowedToCycle = true;
                mouseLook.allowedToLook = true;
                playerMovement.allowedToMove = true;
            }
        }
    }

    public void PauseTitleButtonPressed(int index) {
        for(int i = 0; i < pauseMainChildren.Length; i++) {
            pauseMainChildren[i].SetActive(i == index);
        }
        for(int i = 0; i < pauseMainButtons.Length; i++) {
            pauseMainButtons[i].GetComponent<CanvasGroup>().alpha = index == i ? 1 : inactivePauseButtonDarkenIntensity;
            pauseMainButtons[i].GetComponent<CanvasGroup>().interactable = !(index == i);
        }
    }

    public void ExitPausedUI() {
        for(int i = 0; i < pauseMainChildren.Length; i++) {
            pauseMainChildren[i].SetActive(false);
        }
        PauseTitleButtonPressed(2);
        objectivesUI.SetActive(true);
    }

}
