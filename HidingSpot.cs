using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingSpot : MonoBehaviour {

    public Transform positionHide;
    public bool hidingHere = false, scareOnExit = false;
    public AudioClip enterClip, exitClip;
    public GameObject hidingUI;

    public bool tutorialHidingSpot = false;

    private GameObject player;
    private Vector3 initialPosVec;
    private Quaternion initRot;
    private Animator fadeAnimator;
    private int storedItem = 0;

    [HideInInspector] public bool hidingAnimOnCooldown = false;

    // Update is called once per frame
    void Update() {
        if(!hidingAnimOnCooldown && hidingHere && Input.GetKeyDown(KeyCode.E)) {
            Unhide();
        }
    }

    #region Hiding methods
    public void Hide(GameObject thisPlayer) {
        hidingAnimOnCooldown = true;
        if(fadeAnimator == null) fadeAnimator = GameObject.Find("Fade Animation").GetComponent<Animator>();
        fadeAnimator.Play("Fade to Black");
        GetComponent<AudioSource>().PlayOneShot(enterClip);
        player = thisPlayer;
        player.GetComponent<PlayerMovement>().allowedToMove = false;
        player.GetComponent<CharacterController>().enabled = false;
        player.transform.GetChild(1).GetChild(0).GetComponent<InteractRaycast>().allowedToRaycast = false;
        player.GetComponent<PlayerMovement>().isHiding = true;
        if(player.GetComponent<PlayerMovement>().enemyVisionScript.SeeParticularTarget(player.transform)) 
            player.GetComponent<PlayerMovement>().enemyVisionScript.GetComponent<Enemy>().playerLastSeen.position = transform.position;
        storedItem = player.transform.parent.GetComponentInChildren<ToolController>().heldIndex.Value;
        player.transform.parent.GetComponentInChildren<ToolController>().ForceToBarehand();
        StartCoroutine(HideTimer());

        if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerOpen");
    }

    private IEnumerator HideTimer() {
        yield return new WaitForSeconds(.5f);
        hidingUI.SetActive(true);
        fadeAnimator.Play("Fade from Black");
        hidingHere = true;
        initialPosVec = player.transform.position;
        initRot = player.transform.rotation;
        player.transform.position = positionHide.position;
        player.transform.rotation = positionHide.rotation;
        if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerClose");
        hidingAnimOnCooldown = false;
    }

    public void Unhide() {
        hidingAnimOnCooldown = true;
        GetComponent<AudioSource>().PlayOneShot(exitClip);
        fadeAnimator.Play("Fade to Black");
        if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerOpen");

        StartCoroutine(UnhideTimer());
    }

    private IEnumerator UnhideTimer() {
        yield return new WaitForSeconds(.5f);
        fadeAnimator.Play("Fade from Black");

        hidingUI.SetActive(false);
        hidingHere = false;
        player.transform.GetChild(1).GetChild(0).GetComponent<InteractRaycast>().allowedToRaycast = true;
        player.transform.position = initialPosVec;
        player.transform.rotation = initRot;
        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponent<PlayerMovement>().allowedToMove = true;
        player.GetComponent<PlayerMovement>().isHiding = false;
        player.transform.parent.GetComponentInChildren<ToolController>().ForceToPrevhand(storedItem);

        if(tutorialHidingSpot) GameObject.Find("TutorialManager").GetComponent<Tutorial>().usedHidingSpot = true;
        if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerClose");

        if(scareOnExit) {
            gameObject.layer = 0;
            yield return new WaitForSeconds(.5f);
            GameObject.Find("Game Manager").GetComponent<Death>().Jumpscare(false);
        }
        hidingAnimOnCooldown = false;
    }
    #endregion

}
