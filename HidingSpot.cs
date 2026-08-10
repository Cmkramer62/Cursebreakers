using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HidingSpot : NetworkBehaviour {

    public Transform positionHide;
    public NetworkVariable<bool> hidingHere = new NetworkVariable<bool>(false);
    public bool scareOnExit = false;
    public AudioClip enterClip, exitClip;
    public GameObject hidingUI;

    public bool tutorialHidingSpot = false;

    private GameObject player;
    private Vector3 initialPosVec;
    //private Quaternion initRot;
    private Animator fadeAnimator;
    private int storedItem = 0;
    private Enemy ghostScript;
    private MouseLook mouseLookScript;

    [HideInInspector] public bool hidingAnimOnCooldown = false;
    public NetworkVariable<ulong> occupantClientId = new NetworkVariable<ulong>(ulong.MaxValue);

    public override void OnNetworkSpawn() {
        hidingHere.OnValueChanged += OnHidingChanged;
        //serverCurseManager.goalCurseTrackedID.OnValueChanged += OnObjectChanged;

    }

    // Update is called once per frame
    void Update() {
        // if the your id is equal to the stored id, then we listen to you.
        if(MatchesPlayer(NetworkManager.Singleton.LocalClientId)
            && !hidingAnimOnCooldown && hidingHere.Value && Input.GetKeyDown(KeyCode.E)) {
           
            Unhide();
        }
    }

    public bool MatchesPlayer(ulong clientID) {
        return clientID == occupantClientId.Value;
    }

    private void OnHidingChanged(bool oldState, bool newState) {
         GetComponent<Animator>().Play("LockerOpen");
    }

    #region Hiding methods

    [ServerRpc (RequireOwnership = false)]
    public void HideServerRpc(ulong clientID) {
        if(hidingHere.Value) return;

        occupantClientId.Value = clientID;
    }

    // Will only be called by the owner, since the camera is client's only.?
    public void Hide(GameObject player) {
        if(hidingHere.Value) return;

        this.player = player;
        // This will be called by the camera, which is always an "owner", but this script is always owned by the server...
        hidingAnimOnCooldown = true;
        if(fadeAnimator == null) fadeAnimator = GameObject.Find("Fade Animation").GetComponent<Animator>();
        fadeAnimator.Play("Fade to Black");
        GetComponent<AudioSource>().PlayOneShot(enterClip);
        player.GetComponentInChildren<PlayerMovement>().allowedToMove = false;
        player.GetComponent<CharacterController>().enabled = false;
        //player.transform.GetChild(1).GetChild(0).GetComponent<InteractRaycast>().allowedToRaycast = false;
        if(mouseLookScript == null) {
            mouseLookScript = GameObject.FindAnyObjectByType<MouseLook>();
        }
        mouseLookScript.GetComponent<InteractRaycast>().allowedToRaycast = false;
        player.GetComponentInChildren<PlayerMovement>().isHiding = true;

        if(ghostScript == null) {
            ghostScript = GameObject.FindFirstObjectByType<Enemy>();
        }

        if(ghostScript.GetComponent<ConeLOSDetector>().SeeParticularTarget(player.transform)) {
            if(ghostScript.playerLastSeen.Value.TryGet(out NetworkObject networkObject)) {
                networkObject.transform.position = transform.position;
            }
        }
        storedItem = player.GetComponent<ToolController>().heldIndex.Value;
        player.GetComponent<ToolController>().ForceToBarehand();
        StartCoroutine(HideTimer());

        //if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerOpen");
    }

    private IEnumerator HideTimer() {
        yield return new WaitForSeconds(.5f);
        hidingUI.SetActive(true);
        fadeAnimator.Play("Fade from Black");
        ChangeHidingStateServerRpc(true);
        initialPosVec = player.transform.position;
        //initRot = player.transform.rotation;
        player.transform.position = positionHide.position;
        //player.transform.rotation = positionHide.rotation;
        //if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerClose");
        hidingAnimOnCooldown = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHidingStateServerRpc(bool newState) {
        hidingHere.Value = newState;
        if(!newState) occupantClientId.Value = (ulong)99f;
    }


    public void Unhide() {
        hidingAnimOnCooldown = true;
        GetComponent<AudioSource>().PlayOneShot(exitClip);
        fadeAnimator.Play("Fade to Black");
        //if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerOpen");

        StartCoroutine(UnhideTimer());
    }

    private IEnumerator UnhideTimer() {
        yield return new WaitForSeconds(.5f);
        fadeAnimator.Play("Fade from Black");

        hidingUI.SetActive(false);
        ChangeHidingStateServerRpc(false);
        if(mouseLookScript == null) {
            mouseLookScript = GameObject.FindAnyObjectByType<MouseLook>();
        }
        mouseLookScript.GetComponent<InteractRaycast>().allowedToRaycast = true;
        player.transform.position = initialPosVec;
        //player.transform.rotation = initRot;
        player.GetComponent<CharacterController>().enabled = true;
        player.GetComponentInChildren<PlayerMovement>().allowedToMove = true;
        player.GetComponentInChildren<PlayerMovement>().isHiding = false;
        player.GetComponent<ToolController>().ForceToPrevhand(storedItem);

        if(tutorialHidingSpot) GameObject.Find("TutorialManager").GetComponent<Tutorial>().usedHidingSpot = true;
        //if(GetComponent<Animator>()) GetComponent<Animator>().Play("LockerClose");

        if(scareOnExit) {
            gameObject.layer = 0;
            yield return new WaitForSeconds(.5f);
            GameObject.Find("Game Manager").GetComponent<Death>().Jumpscare(false);
        }
        hidingAnimOnCooldown = false;
    }
    #endregion

}
