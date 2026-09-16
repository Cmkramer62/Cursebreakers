using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Door : NetworkBehaviour {

    public NetworkVariable<bool> state, locked, unlockable = new NetworkVariable<bool>(false);

    [SerializeField] private bool sceneLoading = false, causeInteraction = false, menuScene = false;
    public string keyname;
    public int sceneName;

    // VISUALS
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip openClip, closeClip, lockedClip, unlockClip;
    //private GameObject gameManager;



    /*
     * Interacts with door, either opening/closing, unlocking, or not budging and saying text.
     * Called by both server and client.
     */
    public void InteractDoor() {
        if(!locked.Value) {

            // Everyone Effects (value change, results SFX/VFX).
            InteractWithDoorServerRpc();

            // Personal Effects
            if(causeInteraction) {
                GetComponent<InteractPrompt>().EndEffect();
                causeInteraction = false;
            }
            if(sceneLoading) {
                if(menuScene) {
                    Cursor.lockState = CursorLockMode.None;
                    MultiplayerManager.Instance.LeaveGame();
                }
                // Cannot support other scene loading.
                //gameManager.GetComponent<SceneLoader>().LoadScene(sceneName); 
            }
        }
    }
    
    [ServerRpc]
    private void InteractWithDoorServerRpc() {
        if(!locked.Value) {
            OpenCloseDoor();
        }
        //else if (unlockable && locked && gameManager.GetComponent<Inventory>().inventoryDictionary.ContainsKey(keyname)){
        //    UnlockDoor();
        //}
        else {
            DoorEffectsLockedClientRpc();
        }
    }

    public void UnlockDoor() {
        locked.Value = false;
        source.Stop();
        source.PlayOneShot(unlockClip);
    }

    /*
     * Opens or closes the door based on the state bool.
     */
    public void OpenCloseDoor() {
        state.Value = !state.Value;
    }

    // ====================
    //  EFFECTS
    // ====================

    public override void OnNetworkSpawn() {
        if(!IsServer) {
            return;
        }
        state.OnValueChanged += OnDoorStateChanged;
    }

    private void OnDoorStateChanged(bool oldVal, bool newVal) {
        DoorEffectsOpenCloseClientRpc(newVal);
    }

    [ClientRpc] // Door won't open effects.
    private void DoorEffectsLockedClientRpc() {
        source.Stop();
        source.pitch = Random.Range(.9f, 1.1f);
        source.PlayOneShot(lockedClip);
        //GetComponent<InteractPrompt>().InteractWithObject(will need to pass the player ref through this method param);
    }

    [ClientRpc] // Opening or Closing door effects.
    private void DoorEffectsOpenCloseClientRpc(bool newVal) {
        animator.SetBool("open", newVal);
        source.Stop();
        source.pitch = Random.Range(.9f, 1.1f);
        if(newVal) source.PlayOneShot(openClip);
        else source.PlayOneShot(closeClip);
    }
}
