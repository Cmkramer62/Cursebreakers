using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Unity.Netcode;

public class Death : NetworkBehaviour {

    // Listen for lives on the CurseGameManagerClient.cs or UIManager.cs. Act accordingly upon life loss or gain for UI.
    public NetworkVariable<int> lives = new NetworkVariable<int>(3);

    public GameObject player, jumpscareObject, cameraParent, realGhost, jumpscareChildObject, jumpscareAngel, deathUI, handObjectParent;
    public AudioSource source;
    public AudioClip jumpscareClip, hitDamageClip;
    public AudioClip[] stingerClips;
    public float scareVolume = 1.0f;

    //public int lives = 3;
    public GameObject[] bloodUI, heartsUI;
    public bool allowDeath = true;

    [HideInInspector]
    public GameObject realGhostChild;

    public SaveDataHandler saveSystem;
    public AudioMixer masterMixer;

    // Ghost calls this. Always server.
    public void LoseLife() {
        //StartCoroutine(BloodTimer());
        source.PlayOneShot(hitDamageClip);
        if(player.GetComponent<PlayerMovement>().isHiding) {
            foreach(HidingSpot spot in GameObject.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None)) {
                if(spot.hidingHere.Value && spot.MatchesPlayer(NetworkManager.Singleton.LocalClientId)) {
                    spot.Unhide();
                }
            }
        }
        lives.Value--;
        source.PlayOneShot(stingerClips[lives.Value]); // I inverted this, invert the sound list.

        if(lives.Value == 0) {

            // Jumpscare visuals.
            // Then set animator of player to dead bool.
            // Player cannot move.
            transform.parent.GetComponent<Animator>().SetBool("Dead", true);
            GetComponent<ToolController>().masterAllowed = false;
            //GetComponentInChildren<PlayerMovement>().allowedToCrouch = false;
            //GetComponentInChildren<PlayerMovement>().allowedToMove = false;
            // Dont need to do above, since it checks on its own if this script's lives==0.
        }


    }

    public void Jumpscare(bool angel) {
        //saveSystem.SetMissionData(-1, GetComponent<CurseGameManager>().timeSpent, GetComponent<CurseGameManager>().livesLeft,
       //     GetComponent<CurseGameManager>().timeSpotted, GetComponent<CurseGameManager>().longestChase, GetComponent<CurseGameManager>().purifyState);

        if(!angel) StartCoroutine(JumpscareTimer());
        else StartCoroutine(JumpscareAngelTimer());
    }

    private IEnumerator JumpscareTimer() {
        masterMixer.SetFloat("MainVolumeParam", -80);

        #region Ghost Teleportation
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
        #endregion

        jumpscareChildObject.SetActive(false);
        player.SetActive(false);
        jumpscareObject.SetActive(true);
        realGhostChild.transform.GetChild(1).GetComponent<Animator>().Play("JumpscareFaceAnimator");

        source.PlayOneShot(jumpscareClip, 0.4f);
        Cursor.lockState = CursorLockMode.None;
        GetComponent<PauseGame>().normalUI.SetActive(false);
        GetComponent<PauseGame>().pausedUI.SetActive(false);
        //GetComponent<GameTimer>().KillTimer();
        GetComponent<PurificationManager>().KillTimer();
        GetComponent<ToolController>().masterAllowed = false;
        GetComponent<PauseGame>().allowedToPause = false;
        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
        realGhostChild.GetComponent<Animator>().speed = 0;

        if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        yield return new WaitForSeconds(1.5f);
        //  deathUI.SetActive(true);

    }

    private IEnumerator JumpscareAngelTimer() {
        masterMixer.SetFloat("MainVolumeParam", -80);

        realGhost.SetActive(false);

        jumpscareChildObject.SetActive(false);
        player.SetActive(false);
        jumpscareAngel.SetActive(true);
        jumpscareObject.SetActive(true);

        source.PlayOneShot(jumpscareClip, 0.4f);

        Cursor.lockState = CursorLockMode.None;
        GetComponent<PauseGame>().normalUI.SetActive(false);
        GetComponent<PauseGame>().pausedUI.SetActive(false);
        //GetComponent<GameTimer>().KillTimer();
        GetComponent<PurificationManager>().KillTimer();
        GetComponent<ToolController>().masterAllowed = false;
        GetComponent<PauseGame>().allowedToPause = false;
        //wait for 1 (?) seconds, then pause the game. Load a menu that's animated without using timescale. What to do about the pause menu functionality?
        yield return new WaitForSeconds(1.13333f);
        realGhostChild.GetComponent<Animator>().speed = 0;

        if(GetComponent<PurificationManager>().cursedObjectScript != null) AudioController.FadeOutAudio(this, GetComponent<PurificationManager>().cursedObjectScript.pSourceB, .5f);
        yield return new WaitForSeconds(2.5f);
        // deathUI.SetActive(true);

    }

    private IEnumerator BloodTimer() {
        source.PlayOneShot(hitDamageClip);
        if(player.GetComponent<PlayerMovement>().isHiding) {
            foreach(HidingSpot spot in GameObject.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None)) {
                if(spot.hidingHere.Value) spot.Unhide();
            }
        }

        if(lives.Value - 1 == 0) { // player is dead.

            //bloodUI[3 - GetComponent<PlayerHandler>().lives.Value].SetActive(true);
            //heartsUI[GetComponent<PlayerHandler>().lives.Value - 1].GetComponent<Animator>().Play("HeartIconLoss");
           // GetComponent<CurseGameManager>().livesLeft = 0;
            source.PlayOneShot(stingerClips[3 - lives.Value]);
            if(allowDeath) Jumpscare(false);
            else {
                yield return new WaitForSeconds(1f);
                //bloodUI[3 - GetComponent<PlayerHandler>().lives.Value].SetActive(false);
                //heartsUI[GetComponent<PlayerHandler>().lives.Value - 1].transform.GetChild(1).gameObject.SetActive(false);
            }
        }
        else {
           // bloodUI[3 - GetComponent<PlayerHandler>().lives.Value].SetActive(true);
           // heartsUI[GetComponent<PlayerHandler>().lives.Value - 1].GetComponent<Animator>().Play("HeartIconLoss");
            source.PlayOneShot(stingerClips[3 - lives.Value]);
            yield return new WaitForSeconds(1f);
           // bloodUI[3 - GetComponent<PlayerHandler>().lives.Value].SetActive(false);
            //heartsUI[GetComponent<PlayerHandler>().lives.Value - 1].transform.GetChild(1).gameObject.SetActive(false);
            lives.Value--;
        }
    }

}
