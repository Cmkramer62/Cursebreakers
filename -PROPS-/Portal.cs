using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {

    public enum PortalTypes { Teleport, SceneTransition}
    public PortalTypes portalType;

    public Portal otherPortal;
    public bool used = false;
    public float charge = 0.0f;
    public ParticleSystem sparksA, sparksB;
    public int sceneNumber;

    public AudioSource source;
    public AudioClip chargingClip, enterClip, leaveClip;

    [HideInInspector]
    public float animspeedA = .5f, animspeedB = .4f;


    private Animator fadeAnimator;
    public GameObject gameManager, playerReference;
    private bool inside = false;
    private AudioSource sourceTwoDim;

    private void OnEnable() {
        gameManager = GameObject.Find("Game Manager");
        sourceTwoDim = gameManager.transform.Find("Audio Source 2D").GetComponent<AudioSource>();
        fadeAnimator = GameObject.Find("Warp Animation").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update() {
        if(!used && inside) {
            charge += 1 * Time.deltaTime;
        }
        else if(charge > 0f) {
            charge -= 1 * Time.deltaTime;
        }

        if(charge >= 1f) {
            Teleport(playerReference);
            //charge = 0f;
            inside = false;
        }

        sparksA.gravityModifier = -4f * charge;
        sparksB.gravityModifier = -.5f * charge;
    }

    private void OnTriggerEnter(Collider other) {
        if(!used && other.name == "Player") {
            source.PlayOneShot(enterClip, 1f);
            playerReference = other.gameObject;
            inside = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.name == "Player") {
            source.PlayOneShot(leaveClip, 1f);
            inside = false;
            used = false;
        }
    }

    public void Teleport(GameObject player) {
        StartCoroutine(TeleportOut(player));
    }

    private IEnumerator TeleportOut(GameObject player) {
        sourceTwoDim.PlayOneShot(chargingClip, .7f);

        playerReference = player;
        player.GetComponent<PlayerMovement>().allowedToMove = false;
        player.GetComponent<PlayerMovement>().allowedToCrouch = false;
        gameManager.GetComponent<PauseGame>().allowedToPause = false;
        player.GetComponent<CharacterController>().enabled = false;
        if(otherPortal != null && portalType == PortalTypes.Teleport) otherPortal.used = true;

        fadeAnimator.Play("FadeToWarp");
        yield return new WaitForSeconds(animspeedA);

        if(portalType == PortalTypes.Teleport) StartCoroutine(TeleportIn(player));
        else StartCoroutine(TeleportScene());
    }

    private IEnumerator TeleportIn(GameObject player) {
        player.transform.position = new Vector3(otherPortal.transform.position.x, otherPortal.transform.position.y + 10f, otherPortal.transform.position.z);
        fadeAnimator.Play("FadeFromWarp");

        yield return new WaitForSeconds(animspeedB);

        player.GetComponent<PlayerMovement>().allowedToMove = true;
        player.GetComponent<PlayerMovement>().allowedToCrouch = true;
        gameManager.GetComponent<PauseGame>().allowedToPause = true;
        player.GetComponent<CharacterController>().enabled = true;
    }

    private IEnumerator TeleportScene() {
        yield return new WaitForSeconds(animspeedB);
        gameManager.GetComponent<SceneLoader>().LoadScene(sceneNumber);
    }

}
