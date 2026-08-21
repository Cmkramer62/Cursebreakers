using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Unity.Netcode;

public class Death : NetworkBehaviour {

    // Listen for lives on the CurseGameManagerClient.cs or UIManager.cs. Act accordingly upon life loss or gain for UI.
    public NetworkVariable<int> lives = new NetworkVariable<int>(3);
    public NetworkVariable<bool> afterlifePlayer = new NetworkVariable<bool>(false);

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

    [SerializeField] private SkinnedMeshRenderer[] bodyMeshes;
    [SerializeField] private Material normalBodyAMat, normalBodyBMat, ghostBodyMat;
    [SerializeField] private Light jumpscareLight;

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

            // IF there are more lives/people left, then transition to a ghost. Otherwise...death UI?
            StartCoroutine(AfterlifeSequence());
            // fade to black, and then to a new pale greenish bluish color. (this is a new overlay. Both the fade and this are
            // new gameobject to make. Worry about animations later.
            // Update player perms.
        }


    }

    private IEnumerator AfterlifeSequence() {
        cameraParent.transform.parent.GetComponent<Animator>().Play("DeathAnim");

        yield return new WaitForSeconds(1f);

        afterlifePlayer.Value = true;
        yield return new WaitForSeconds(4f);
        SetBodyMaterials(true); // do this false when..?
        SetPlayerGhostPerms();
        yield return new WaitForSeconds(1f);
        cameraParent.transform.parent.GetComponent<Animator>().Play("AfterlifeAnim");
        UndoJumpscareEffects();
    }

    public void Jumpscare(bool angel) {
        //saveSystem.SetMissionData(-1, GetComponent<CurseGameManager>().timeSpent, GetComponent<CurseGameManager>().livesLeft,
       //     GetComponent<CurseGameManager>().timeSpotted, GetComponent<CurseGameManager>().longestChase, GetComponent<CurseGameManager>().purifyState);

        if(!angel) StartCoroutine(JumpscareGhostSequence());
        else StartCoroutine(JumpscareAngelSequence());
    }

    // This needs to occur ONLY on client side, to the client that's being jumpscared.
    private IEnumerator JumpscareGhostSequence() {
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
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().AfterlifeEffects(2f);
        source.PlayOneShot(jumpscareClip, 0.4f);
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = false;
        jumpscareObject.SetActive(true);
        jumpscareGhost.SetActive(true);
        ghostShadow.SetActive(true); // animator on shadow not enabled.
        ghostShadow.transform.GetChild(0).GetComponent<Animator>().Play("Scream 0"); // done to only mimic the position.

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

    private IEnumerator JumpscareAngelSequence() {
        jumpscareLight.intensity = 5;
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().JumpscareOnlyLayer();
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().AfterlifeEffects(2f);

        jumpscareAngel.SetActive(true);
        jumpscareObject.SetActive(true);

        source.PlayOneShot(jumpscareClip, 0.4f);

        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
       // realGhostChild.GetComponent<Animator>().speed = 0;

        //if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        //yield return new WaitForSeconds(2.5f);
        // deathUI.SetActive(true);

    }

    private void UndoJumpscareEffects() {
        // turn off jumpscare parent, children.
        // stop particle system parent.
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = false;
        jumpscareObject.SetActive(false);
        jumpscareGhost.SetActive(false);
        ghostShadow.SetActive(false);
        ghostShadow.GetComponent<Animator>().enabled = false;
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().NormalLayers();

    }



    // SetPlayerPerms Activates or deactivates player permissions.
    private void SetPlayerPerms(bool state) {
        // No movement input. 1*
        // No camera movement input. 2*
        // No tool/ability input. 3*
        // Disable hand visuals. 4*
        // No allowing input (F/LMB) for Interact Raycast. 5*
        // No crosshair allowed.
        // Turn off Normal UI, and Pause UI if it was on. (players can still pause though?)
        // Stop timer?

        GetComponentInChildren<PlayerMovement>().playerAlive = state;
        CameraFollow cameraReference = GetComponent<PlayerHandler>().cameraReference;
        cameraReference.GetComponent<MouseLook>().playerAlive = state; // 2
        if(!state) GetComponent<ToolController>().ForceToBarehand();
        GetComponent<ToolController>().playerAlive = state; // 3

        cameraReference.GetComponent<InteractRaycast>().playerAlive = state; // 5
       // playerController.GetComponent<PlayerMovement>().enabled = state;
        //Cursor.lockState = CursorLockMode.None;
        //GetComponent<PauseGame>().normalUI.SetActive(false);
        //GetComponent<PauseGame>().pausedUI.SetActive(false);
    }

    // SetPlayerGhostPerms Activates a specific restricted permission set, allowing the player to do some things.
    private void SetPlayerGhostPerms() {
        CameraFollow cameraReference = GetComponent<PlayerHandler>().cameraReference;
        cameraReference.GetComponent<MouseLook>().playerAlive = true; // 2
        GetComponent<ToolController>().playerAlive = false; // 3

        cameraReference.GetComponent<InteractRaycast>().playerAlive = false; // change this to true, and a ghostbool to true.
        // the interact raycast should do unique things if the ghostbool is true.
        // playerController.GetComponent<PlayerMovement>().enabled = true;
        GetComponentInChildren<PlayerMovement>().playerAlive = true;

    }

    // SetBodyMaterials changes the player's body and hands to be the normal color or a ghostly color.
    private void SetBodyMaterials(bool ghost) {
        for(int i = 0; i < bodyMeshes.Length; i++) {
            
            bodyMeshes[i].material = ghost ? ghostBodyMat : normalBodyAMat;
            
        }
        bodyMeshes[0].material = ghost ? ghostBodyMat : normalBodyBMat;
    }
}
