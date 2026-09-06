using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Unity.Netcode;
using Dissonance;

public class Death : NetworkBehaviour {

    // Listen for lives on the CurseGameManagerClient.cs or UIManager.cs. Act accordingly upon life loss or gain for UI.
    public NetworkVariable<int> lives = new NetworkVariable<int>(3);
    public NetworkVariable<bool> afterlifePlayer = new NetworkVariable<bool>(false);

    public GameObject playerController, jumpscareObject, cameraParent, jumpscareAngel, handObjectParent, ghostShadow, playerDeadBodyPrefab;
    [SerializeField] private GameObject[] jumpscareGhost;
    public int jumpscareGhostBodyIndex = -1;
    public AudioSource twoDimAudioSource, musicSourceA, musicSourceB, channelSourceThreeDim;
    public AudioClip jumpscareClip, hitDamageClip, afterlifeAClip, afterlifeBClip, transitionClip;
    public AudioClip[] stingerClips;
    public float scareVolume = 1.0f;

    public bool allowDeath = true;

    //[HideInInspector]
    //public GameObject realGhostChild, realGhost, jumpscareChildObject, deathUI;

    public SaveDataHandler saveSystem;
    public AudioMixer masterMixer;

    [SerializeField] private SkinnedMeshRenderer[] bodyMeshes;
    [SerializeField] private MeshRenderer halo;
    [SerializeField] private Material normalBodyAMat, normalBodyBMat, ghostBodyMat, haloNormalMat;
    [SerializeField] private Light jumpscareLight;
    public Animator playerArmsAnimator;
  //  public bool reviving = false;
    public GameObject playerDeadBody;
    public float returnDuration = 4f;
    private AudioClip storedClipA, storedClipB;
    private float storedVolumeA, storedVolumeB;

    public bool channelingLife = false, channelingDone = false;
    public float lifeChannelAmount, lifeChannelRecoveryRate, lifeChannelDuration;

    public PurificationManager purificationManagerScript;

    public override void OnNetworkSpawn() {        
        afterlifePlayer.OnValueChanged += OnAfterlifeChanged;
        lives.OnValueChanged += OnLifeChanged;

        StartCoroutine(FindClientGameManager());
    }

    // The OnNetworkSpawn occurs before the scene has fully loaded. So we wait until it has, and find what we need.
    private IEnumerator FindClientGameManager() {
        CurseGameManagerClient clientCG = FindAnyObjectByType<CurseGameManagerClient>();
        while(clientCG == null) {
            clientCG = FindAnyObjectByType<CurseGameManagerClient>();
            yield return null;
        }

        musicSourceA = clientCG.musicSource;
        musicSourceB = clientCG.musicAlternate;
    }

    private void Update() {
        ChannelUpdate(); // always?
    }

    // Called from inside this.Update();
    // Any changes here must be mirrored in the UIManager version.
    private void ChannelUpdate() {
        if(channelingLife && !channelingDone) lifeChannelAmount -= 1 * Time.deltaTime;
        else lifeChannelAmount = Mathf.Clamp(lifeChannelAmount += lifeChannelRecoveryRate * Time.deltaTime, 0, lifeChannelDuration);

        if(lifeChannelAmount <= 0) {
            channelingDone = true;
            // probably hide the UI now.
            //source.PlayOneShot(breathClip);
        }
        if(lifeChannelAmount == lifeChannelDuration) {
            channelingDone = false;
        }
    }

    #region AFTERLIFE
    public void OnAfterlifeChanged(bool oldValue, bool newValue) {
        playerArmsAnimator.SetBool("Afterlife", newValue);

        GetComponentInChildren<PlayerMovement>().GetComponentInChildren<GroundChecker>().afterlife = newValue;
        GetComponentInChildren<PlayerMovement>().GetComponentInChildren<GroundChecker>().UpdateClips();
    }

    private void OnLifeChanged(int valueOld, int valueNew) {
        if(valueOld == 0 && valueNew == 1) {
            SetRevivalPerms();
            // reviving = true;
            StartCoroutine(ReturnToBodyCoroutine());
        }

        if(valueNew == 0) {
            // turn off mic.
            GameObject.FindAnyObjectByType<VoiceBroadcastTrigger>().enabled = false;
        }
        else {
            // turn on mic.
            GameObject.FindAnyObjectByType<VoiceBroadcastTrigger>().enabled = true;

        }
    }

    private IEnumerator ReturnToBodyCoroutine() {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;
        while(elapsed < returnDuration) {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            // Smooth the movement
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                playerDeadBody.transform.position,
                t
            );

            yield return null;
        }

        // Make absolutely sure we're exactly at the body
        transform.position = playerDeadBody.transform.position;

        // Restore player control here
        // Finish your transformation back into the player here
       
        afterlifePlayer.Value = false;
        // UI manager will hear this and take 1.3 seconds to fade to black. We mimic that time and then return control.
        StartCoroutine(UndoAfterlifeEffects());
        
    }

    private IEnumerator UndoAfterlifeEffects() {
        yield return new WaitForSeconds(3f);
        SetPlayerPerms(true);
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().NormalLayers();
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().NormalEffects();
        GameObject.Destroy(playerDeadBody);
        if(storedClipA != null) AudioController.FadeToAnother(this, musicSourceA, .5f, storedClipA, storedVolumeA);
        else AudioController.FadeOutAudio(this, musicSourceA, .5f);
        if(storedClipB != null) AudioController.FadeToAnother(this, musicSourceB, .5f, storedClipB, storedVolumeB);
        else AudioController.FadeOutAudio(this, musicSourceB, .5f);
        if(IsServer) SetBodyMaterialsClientRpc(false); // do this false when..?
        else SetBodyMaterialsServerRpc(false);
        GetComponent<ToolController>().ForceToPrevhand(0);
    }
    #endregion


    // Ghost calls this. Always server.
    // Client side UIManager.cs handles local visuals.
    public void LoseLife(bool ghostAttack) {
        AssignPurificationReference();

        twoDimAudioSource.PlayOneShot(hitDamageClip);
        if(playerController.GetComponent<PlayerMovement>().isHiding) {
            foreach(HidingSpot spot in GameObject.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None)) {
                if(spot.hidingHere.Value && spot.MatchesPlayer(NetworkManager.Singleton.LocalClientId)) {
                    spot.Unhide();
                }
            }
        }
        int whatLivesWillBe = lives.Value - 1;
        LoseLifeServerRpc(); // may take time to register the rpc after this.
        twoDimAudioSource.PlayOneShot(stingerClips[whatLivesWillBe]); // I inverted this, invert the sound list.

        if(whatLivesWillBe == 0) {
            GetComponent<Animator>().SetBool("Dead", true); // Synced?
            if(ghostAttack) playerArmsAnimator.SetTrigger("DyingFromGhost");
            else playerArmsAnimator.SetTrigger("DyingFromAngel");
            SetPlayerPerms(false);
            // Jumpscare visuals IF ghostAttack. ISOLATE JUMPSCARE CODE TO ONLY BE WHAT'S NEEDED FOR ANIMATIONS.
            // Then set animator of player to dead bool.
            // Player cannot move temporarily. This and the 2 above happen simultaneously.

            Jumpscare(!ghostAttack);
            // Animation plays of player falling down from first person point of view... Camera effects...Darkness.
            // More camera effects..glowing green flame spiritual energy. You now are ghost above your body.
            // UI looks different. Toolbar only has Nihil. Stamina is green hued.

            // IF there are more lives/people left, then transition to a ghost. Otherwise...death UI?
            if(purificationManagerScript.totalLives.Value >= 2) {
                StartCoroutine(AfterlifeSequence());
            }

            // fade to black, and then to a new pale greenish bluish color. (this is a new overlay. Both the fade and this are
            // new gameobject to make. Worry about animations later.
            // Update player perms.
        }

        else {
            purificationManagerScript.UpdateCurrentLivesServerRpc();
        }
    }

    private void AssignPurificationReference() {
        if(purificationManagerScript == null) {
            purificationManagerScript = GameObject.FindAnyObjectByType<PurificationManager>();
        }
    }

    [ServerRpc (RequireOwnership = false)]
    private void LoseLifeServerRpc() {
        lives.Value--;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAfterLifeBoolChangeServerRpc(bool state) {
        afterlifePlayer.Value = state;
    }

    // Called by player reviving another. Will trigger OnLifeChanged in this script.
    [ServerRpc(RequireOwnership = false)]
    public void GainLifeServerRpc() {
        AssignPurificationReference();

        lives.Value++;

        purificationManagerScript.UpdateCurrentLivesServerRpc();
    }

    private IEnumerator AfterlifeSequence() {
        cameraParent.transform.parent.GetComponent<Animator>().Play("DeathAnim");

        yield return new WaitForSeconds(1f);

        RequestAfterLifeBoolChangeServerRpc(true);

        yield return new WaitForSeconds(4f);

        if(IsServer) SetBodyMaterialsClientRpc(true); // do this false when..?
        else SetBodyMaterialsServerRpc(true);

        SetPlayerGhostPerms();

        yield return new WaitForSeconds(1f);
        cameraParent.transform.parent.GetComponent<Animator>().Play("AfterlifeAnim");
        UndoJumpscareEffects();
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().AfterlifeLayers();
        // Spawn the player's ghost body now. Other players will see no change, because it will be spawned while
        // Simultaneously setting the real player's body (who would be starting to stand) as invisible.
        playerDeadBody =  GameObject.Instantiate(playerDeadBodyPrefab);
        playerDeadBody.transform.position = transform.position;
        playerDeadBody.transform.rotation = transform.rotation;
        playerDeadBody.GetComponent<PlayerDeadBody>().playerID.Value = NetworkManager.Singleton.LocalClientId;
        playerDeadBody.GetComponent<NetworkObject>().Spawn();

        GetComponent<Animator>().SetBool("Dead", false);
        storedClipA = musicSourceA.clip;
        storedClipB = musicSourceB.clip;
        storedVolumeA = musicSourceA.volume;
        storedVolumeB = musicSourceB.volume;
        AudioController.FadeToAnother(this, musicSourceA, .5f, afterlifeAClip, 0.217f);//FadeInAudio(this, chaseClip, 3, .1f);
        AudioController.FadeToAnother(this, musicSourceB, .5f, afterlifeBClip, .85f);//FadeInAudio(this, chaseClip, 3, .1f);
        twoDimAudioSource.PlayOneShot(transitionClip);
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
        twoDimAudioSource.PlayOneShot(jumpscareClip, 0.4f);
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = false;
        jumpscareObject.SetActive(true);
        //foreach(GameObject ghostModel in jumpscareGhost) ghostModel.SetActive(true);
        jumpscareGhost[jumpscareGhostBodyIndex].SetActive(true);
        ghostShadow.SetActive(true); // animator on shadow not enabled.
        ghostShadow.transform.GetChild(0).GetComponent<Animator>().Play("Scream 0"); // done to only mimic the position.

        yield return new WaitForSeconds(1.13333f);
        //realGhostChild.GetComponent<Animator>().speed = 0;
        //jumpscareGhost.GetComponent<Animator>().speed = 0;
        ghostShadow.GetComponent<Animator>().enabled = true;
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Play();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = true;
        jumpscareGhost[jumpscareGhostBodyIndex].SetActive(false);
        //if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        //yield return new WaitForSeconds(1.5f);
        //  deathUI.SetActive(true);
        StartCoroutine(UpdateGrandLivesAndEndGame());

    }

    private IEnumerator JumpscareAngelSequence() {
        jumpscareLight.intensity = 5;
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().JumpscareOnlyLayer();
        GetComponent<PlayerHandler>().cameraReference.GetComponent<CameraEffectsManager>().AfterlifeEffects(2f);

        jumpscareAngel.SetActive(true);
        jumpscareObject.SetActive(true);

        twoDimAudioSource.PlayOneShot(jumpscareClip, 0.4f);

        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
        // realGhostChild.GetComponent<Animator>().speed = 0;

        //if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        //yield return new WaitForSeconds(2.5f);
        // deathUI.SetActive(true);

        StartCoroutine(UpdateGrandLivesAndEndGame());
    }

    // If the purification manager finds that the new total Lives is 0, it will kick everyone.
    private IEnumerator UpdateGrandLivesAndEndGame() {
        yield return new WaitForSeconds(1f); // better way of doing this? What should we wait on?
        purificationManagerScript.UpdateCurrentLivesServerRpc();
    }

    private void UndoJumpscareEffects() {
        // turn off jumpscare parent, children.
        // stop particle system parent.
        ghostShadow.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();
        ghostShadow.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().enabled = false;
        jumpscareObject.SetActive(false);
        jumpscareGhost[jumpscareGhostBodyIndex].SetActive(false);
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
        if(state) {
            GetComponentInChildren<CapsuleCollider>().enabled = true;
        }
    }

    // SetPlayerGhostPerms Activates a specific restricted permission set, allowing the player to do some things.
    private void SetPlayerGhostPerms() {
        CameraFollow cameraReference = GetComponent<PlayerHandler>().cameraReference;
        cameraReference.GetComponent<MouseLook>().playerAlive = true; // 2
        GetComponent<ToolController>().playerAlive = false; // 3

        cameraReference.GetComponent<InteractRaycast>().playerAlive = true; // change this to true, and a ghostbool to true.
        cameraReference.GetComponent<InteractRaycast>().afterlife = true; // change this to true, and a ghostbool to true.

        // the interact raycast should do unique things if the ghostbool is true.
        // playerController.GetComponent<PlayerMovement>().enabled = true;
        GetComponentInChildren<PlayerMovement>().playerAlive = true;

    }

    private void SetRevivalPerms() {
        CameraFollow cameraReference = GetComponent<PlayerHandler>().cameraReference;

        cameraReference.GetComponent<InteractRaycast>().playerAlive = false; // change this to true, and a ghostbool to true.
        cameraReference.GetComponent<InteractRaycast>().afterlife = false; // change this to true, and a ghostbool to true.
        GetComponentInChildren<PlayerMovement>().playerAlive = false;
        GetComponentInChildren<CapsuleCollider>().enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBodyMaterialsServerRpc(bool ghost) {
        SetBodyMaterialsClientRpc(ghost);
    }

    // SetBodyMaterials changes the player's body and hands to be the normal color or a ghostly color.
    [ClientRpc(RequireOwnership = false)]
    private void SetBodyMaterialsClientRpc(bool ghost) {
        for(int i = 0; i < bodyMeshes.Length; i++) {
            
            bodyMeshes[i].material = ghost ? ghostBodyMat : normalBodyAMat;
            bodyMeshes[i].gameObject.layer = ghost ? LayerMask.NameToLayer("Afterlife") : 
                LayerMask.NameToLayer("Default");
        }
        bodyMeshes[0].material = ghost ? ghostBodyMat : normalBodyBMat;
        halo.material = ghost ? ghostBodyMat : haloNormalMat;
        halo.gameObject.layer = ghost ? LayerMask.NameToLayer("Afterlife") :
                LayerMask.NameToLayer("Default");
    }
}
