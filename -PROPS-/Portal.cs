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

    [SerializeField] private bool invisible = false, instantTeleport = false;

    [SerializeField] private float teleportSpawnHeight = 10f;

    public bool used = false;
    public float charge = 0.0f;
    [HideInInspector] public float animspeedA = .5f, animspeedB = .4f;
    private Animator fadeAnimator;
    //public GameObject gameManager
    public GameObject playerReference;
    public bool inside = false;

    public override void OnNetworkSpawn() {
        //gameManager = GameObject.Find("Game Manager");
        //sourceTwoDim = gameManager.transform.Find("Audio Source 2D").GetComponent<AudioSource>();
        fadeAnimator = GameObject.Find("Warp Animation").GetComponent<Animator>();

        sparksA.gameObject.SetActive(!invisible);
        sparksB.gameObject.SetActive(!invisible);

    }

    // Update is called once per frame
    void Update() {

        if(!used && inside) {
            charge += 1 * Time.deltaTime;
        }
        else if(charge > 0f) {
            charge -= 1 * Time.deltaTime;
        }

        if(!invisible) {
            sparksA.gravityModifier = -4f * charge;
            sparksB.gravityModifier = -.5f * charge;
        }

        if(playerReference == null || !playerReference.TryGetComponent<NetworkObject>(out var netObj))
            return;

        if(netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        if(charge >= 1f) {
            Teleport(playerReference);
            //charge = 0f;
            inside = false;
        }


    }

    private void OnTriggerEnter(Collider other) {
        if(!other.CompareTag("Player") || !other.GetComponent<Death>())
            return; 
        
        if(!other.TryGetComponent<NetworkObject>(out var netObj))
            return;

        if(netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        // This is the local player's object.

        if(!invisible) {
            source.PlayOneShot(enterClip, 1f);
        }
        playerReference = other.gameObject;
        inside = true;
        if(instantTeleport) {
            charge = 1f;
        }
    }

    private void OnTriggerExit(Collider other) {
        if(!other.CompareTag("Player") || !other.GetComponent<Death>())
            return;

        if(!other.TryGetComponent<NetworkObject>(out var netObj))
            return;

        if(netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        // This is the local player's object.

        if(!invisible) {
            source.PlayOneShot(leaveClip, 1f);
        }
        inside = false;
        used = false;
        
    }

    public void Teleport(GameObject player) {
        StartCoroutine(TeleportOut(player));
    }

    private IEnumerator TeleportOut(GameObject player) {

        player.GetComponent<Death>().SetPlayerPerms(false);

        if(otherPortal != null && portalType == PortalTypes.Teleport) otherPortal.used = true;
        
        if(!invisible) {
            source.PlayOneShot(chargingClip, .7f);
            fadeAnimator.Play("FadeToWarp");
            yield return new WaitForSeconds(animspeedA);
        }
       
        if(portalType == PortalTypes.Teleport) StartCoroutine(TeleportIn(player));
        else StartCoroutine(TeleportScene());
    }

    private IEnumerator TeleportIn(GameObject player) {
        Vector3 playerDestination = new Vector3(otherPortal.transform.position.x, otherPortal.transform.position.y + teleportSpawnHeight, otherPortal.transform.position.z);
        player.GetComponent<PlayerHandler>().SetSpawnPosition(playerDestination);

        if(!invisible) {
            fadeAnimator.Play("FadeFromWarp");
            // non local only method being called in a local only context. (Here, we are only running if this is my client machine. The turn on spawn should be called 
            // on other players, not me, on my machine.) Even though they are the ones triggering the portal, not me.
            if(teleportSpawnHeight > 1) player.GetComponent<PlayerHandler>().TurnOnSpawnParticlesServerRpc();
            yield return new WaitForSeconds(animspeedB);
        }

        player.GetComponent<Death>().SetPlayerPerms(true);
    }

    private IEnumerator TeleportScene() {
        yield return new WaitForSeconds(animspeedB);
        //gameManager.GetComponent<SceneLoader>().LoadScene(sceneNumber);
    }

}
