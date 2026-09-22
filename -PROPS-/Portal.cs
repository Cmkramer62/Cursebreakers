using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Portal : NetworkBehaviour {

    public enum PortalTypes { Teleport, SceneTransition}
    public PortalTypes portalType;
    public ParticleSystem sparksA, sparksB;
    public int sceneNumber;
    public Portal otherPortal;

    public AudioSource source;
    public AudioClip chargingClip, enterClip, leaveClip;

    [SerializeField] private float teleportSpawnHeight = 10f;

    private bool used = false;
    private float charge = 0.0f;
    [HideInInspector] public float animspeedA = .5f, animspeedB = .4f;
    private Animator fadeAnimator;
    //public GameObject gameManager
    public GameObject playerReference;
    private bool inside = false;

    public override void OnNetworkSpawn() {
        //gameManager = GameObject.Find("Game Manager");
        //sourceTwoDim = gameManager.transform.Find("Audio Source 2D").GetComponent<AudioSource>();
        fadeAnimator = GameObject.Find("Warp Animation").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update() {

        if(!IsOwner) {
            return;
        }

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
        if(!IsOwner) {
            return;
        }
        if(!used && other.CompareTag("Player")) {
            source.PlayOneShot(enterClip, 1f);
            playerReference = other.gameObject;
            inside = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if(!IsOwner) {
            return;
        }
        if(other.CompareTag("Player")) {
            source.PlayOneShot(leaveClip, 1f);
            inside = false;
            used = false;
        }
    }

    public void Teleport(GameObject player) {
        StartCoroutine(TeleportOut(player));
    }

    private IEnumerator TeleportOut(GameObject player) {
        source.PlayOneShot(chargingClip, .7f);

        player.GetComponent<Death>().SetPlayerPerms(false);

        if(otherPortal != null && portalType == PortalTypes.Teleport) otherPortal.used = true;

        fadeAnimator.Play("FadeToWarp");
        yield return new WaitForSeconds(animspeedA);

        if(portalType == PortalTypes.Teleport) StartCoroutine(TeleportIn(player));
        else StartCoroutine(TeleportScene());
    }

    private IEnumerator TeleportIn(GameObject player) {
        Vector3 playerDestination = new Vector3(otherPortal.transform.position.x, otherPortal.transform.position.y + teleportSpawnHeight, otherPortal.transform.position.z);
        player.GetComponent<PlayerHandler>().SetSpawnPosition(playerDestination);

        if(teleportSpawnHeight > 1) player.GetComponent<PlayerHandler>().TurnOnSpawnParticles();

        fadeAnimator.Play("FadeFromWarp");

        yield return new WaitForSeconds(animspeedB);

        player.GetComponent<Death>().SetPlayerPerms(true);
    }

    private IEnumerator TeleportScene() {
        yield return new WaitForSeconds(animspeedB);
        //gameManager.GetComponent<SceneLoader>().LoadScene(sceneNumber);
    }

}
