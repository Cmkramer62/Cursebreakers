using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Unity.Netcode;

public class Death : NetworkBehaviour {

    // Listen for lives on the CurseGameManagerClient.cs or UIManager.cs. Act accordingly upon life loss or gain for UI.
    public NetworkVariable<int> lives = new NetworkVariable<int>(3);

    public GameObject playerController, jumpscareObject, cameraParent, jumpscareGhost, jumpscareAngel, handObjectParent, ghostShadow;
    public AudioSource source;
    public AudioClip jumpscareClip, hitDamageClip;
    public AudioClip[] stingerClips;
    public float scareVolume = 1.0f;

    public bool allowDeath = true;

    //[HideInInspector]
    //public GameObject realGhostChild, realGhost, jumpscareChildObject, deathUI;

    public SaveDataHandler saveSystem;
    public AudioMixer masterMixer;

    // Ghost calls this. Always server.
    // Client side UIManager.cs handles local visuals.
    public void LoseLife(bool ghostAttack) {
        source.PlayOneShot(hitDamageClip);
        if(playerController.GetComponent<PlayerMovement>().isHiding) {
            foreach(HidingSpot spot in GameObject.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None)) {
                if(spot.hidingHere.Value && spot.MatchesPlayer(NetworkManager.Singleton.LocalClientId)) {
                    spot.Unhide();
                }
            }
        }
        lives.Value--;
        source.PlayOneShot(stingerClips[lives.Value]); // I inverted this, invert the sound list.

        if(lives.Value == 0) {
            GetComponent<Animator>().SetBool("Dead", true); // Synced?
            SetPlayerPerms(false);
            // Jumpscare visuals IF ghostAttack. ISOLATE JUMPSCARE CODE TO ONLY BE WHAT'S NEEDED FOR ANIMATIONS.
            // Then set animator of player to dead bool.
            // Player cannot move temporarily. This and the 2 above happen simultaneously.
            
            Jumpscare(!ghostAttack);
            // Animation plays of player falling down from first person point of view... Camera effects...Darkness.
            // More camera effects..glowing green flame spiritual energy. You now are ghost above your body.
            // UI looks different. Toolbar only has Nihil. Stamina is green hued.

            // fade to black, and then to a new pale greenish bluish color. (this is a new overlay. Both the fade and this are
            // new gameobject to make. Worry about animations later.
            // Update player perms.
        }


    }

    public void Jumpscare(bool angel) {
        //saveSystem.SetMissionData(-1, GetComponent<CurseGameManager>().timeSpent, GetComponent<CurseGameManager>().livesLeft,
       //     GetComponent<CurseGameManager>().timeSpotted, GetComponent<CurseGameManager>().longestChase, GetComponent<CurseGameManager>().purifyState);

        if(!angel) StartCoroutine(JumpscareTimer());
        else StartCoroutine(JumpscareAngelTimer());
    }

    // This needs to occur ONLY on client side, to the client that's being jumpscared.
    private IEnumerator JumpscareTimer() {
        //masterMixer.SetFloat("MainVolumeParam", -80);

        #region Ghost Teleportation
        // Simply make the ghost go invisible and uninteractable for a bit..?
        /*
        realGhost.SetActive(false);
        realGhostChild.SetActive(false);
        handObjectParent.SetActive(false);
        realGhostChild.transform.parent = jumpscareObject.transform;
        realGhostChild.transform.position = jumpscareChildObject.transform.position;
        realGhostChild.transform.rotation = jumpscareChildObject.transform.rotation;
        realGhostChild.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
        realGhostChild.GetComponent<Animator>().runtimeAnimatorController = jumpscareChildObject.GetComponent<Animator>().runtimeAnimatorController;
        realGhost.GetComponent<Enemy>().MakeVisible();
        realGhostChild.SetActive(true);
        */
        #endregion

        source.PlayOneShot(jumpscareClip, 0.4f);
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = false;
        jumpscareObject.SetActive(true);
        jumpscareGhost.SetActive(true);
        ghostShadow.SetActive(true); // animator on shadow not enabled.
        ghostShadow.transform.GetChild(0).GetComponent<Animator>().Play("Scream 0"); // done to only mimic the position.
        //Cursor.lockState = CursorLockMode.None;
        //GetComponent<PauseGame>().normalUI.SetActive(false);
        //GetComponent<PauseGame>().pausedUI.SetActive(false);
        //GetComponent<GameTimer>().KillTimer();
        //GetComponent<PurificationManager>().KillTimer();
        //GetComponent<ToolController>().playerAlive = false;
        //GetComponent<PauseGame>().allowedToPause = false;
        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
        //realGhostChild.GetComponent<Animator>().speed = 0;
        //jumpscareGhost.GetComponent<Animator>().speed = 0;
        ghostShadow.GetComponent<Animator>().enabled = true;
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Play();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = true;
        jumpscareGhost.SetActive(false);
        //if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        //yield return new WaitForSeconds(1.5f);
        //  deathUI.SetActive(true);

    }

    private IEnumerator JumpscareAngelTimer() {
        masterMixer.SetFloat("MainVolumeParam", -80);

       // realGhost.SetActive(false);

       // jumpscareChildObject.SetActive(false);
        //playerController.SetActive(false);
        jumpscareAngel.SetActive(true);
        jumpscareObject.SetActive(true);

        source.PlayOneShot(jumpscareClip, 0.4f);

        Cursor.lockState = CursorLockMode.None;
        GetComponent<PauseGame>().normalUI.SetActive(false);
        GetComponent<PauseGame>().pausedUI.SetActive(false);
        //GetComponent<GameTimer>().KillTimer();
        GetComponent<PurificationManager>().KillTimer();
        GetComponent<ToolController>().playerAlive = false;
        GetComponent<PauseGame>().allowedToPause = false;
        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
       // realGhostChild.GetComponent<Animator>().speed = 0;

        if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        yield return new WaitForSeconds(2.5f);
        // deathUI.SetActive(true);

    }

    private void SetPlayerPerms(bool state) {
        // No movement input. 1*
        // No camera movement input. 2*
        // No tool/ability input. 3*
        // Disable hand visuals. 4*
        // No allowing input (F/LMB) for Interact Raycast. 5*
        // No crosshair allowed.
        // Turn off Normal UI, and Pause UI if it was on. (players can still pause though?)

        //GetComponentInChildren<PlayerMovement>().allowedToCrouch = false;
        //GetComponentInChildren<PlayerMovement>().allowedToMove = false;
        // Dont need to do above, since it checks on its own if this script's lives==0. 1
        CameraFollow cameraReference = GetComponent<PlayerHandler>().cameraReference;
        cameraReference.GetComponent<MouseLook>().playerAlive = state; // 2
        GetComponent<ToolController>().playerAlive = state; // 3

        cameraReference.GetComponent<InteractRaycast>().playerAlive = state; // 5
        playerController.GetComponent<PlayerMovement>().enabled = state;
        //Cursor.lockState = CursorLockMode.None;
        //GetComponent<PauseGame>().normalUI.SetActive(false);
        //GetComponent<PauseGame>().pausedUI.SetActive(false);
    }

}
