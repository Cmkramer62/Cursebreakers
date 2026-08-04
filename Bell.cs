using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bell : NetworkBehaviour {

    public AudioSource sourceTwoDim;
    public AudioClip[] evocareClips;

    private bool bellOnCooldown = false;

    //public Animator bellAnimator;
    [SerializeField] private ParticleSystem soundParticles;
    [SerializeField] private float bellCooldownTime = 3.5f;

    private Enemy ghostScript;
    public bool ghostSearchWithSound = false;
    public int ghostSoundOdds = 3;

    public override void OnNetworkSpawn() {
        try {// instead of doing this, wait to find it in enumerator.
            ghostScript = GameObject.Find("Ghost Enemy").GetComponent<Enemy>();
        }
        catch(System.Exception e) {
            // Don't care.
        }
        gameObject.SetActive(false);

    }

    private void OnDisable() {
        bellOnCooldown = false;
    }

    void Update() {
        if(!IsOwner) return;

        if(Input.GetKeyDown(KeyCode.F)) {
            RingBellServerRpc();
        }
    }

    [ServerRpc]
    void RingBellServerRpc() {
        if(bellOnCooldown) return;
        bellOnCooldown = true;
        StartCoroutine(BellCooldownTimer());

        // Local visual & audio effects
        RingBellClientRpc();

        // Ghost effects
        if(ghostScript != null && ghostSearchWithSound && !ghostScript.invisible.Value)
            ghostScript.walkPoint = gameObject.transform.parent.parent.parent.transform.GetChild(1).transform.position;
    }

    [ClientRpc]
    void RingBellClientRpc() {
        // Local visual & audio effects
        //bellAnimator.Play("BellRing");
        soundParticles.Play();
        sourceTwoDim.PlayOneShot(evocareClips[Random.Range(0, evocareClips.Length)]);

        // Cursed Object effects
        TriggerCurse(true); // state here is not used. This may be wrong, depending on how ghost is synced.

        transform.parent.parent.parent.GetComponent<ToolController>().BellAnimation();
    }

    private IEnumerator BellCooldownTimer() {
        yield return new WaitForSeconds(bellCooldownTime);
        bellOnCooldown = false;
    }

    private void TriggerCurse(bool state) {
        foreach(CursedObject objectee in gameObject.transform.parent.parent.parent.GetComponentInChildren<ToolController>().cursedObjectsWithinRange) {
            objectee.DisplayCurse(CursedObject.CursedTypes.Sound, state);
        }
    }
}
