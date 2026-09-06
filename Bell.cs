using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bell : NetworkBehaviour {

    public AudioSource sourceTwoDim;
    public AudioClip[] evocareClips, airWhooshClips;

    private bool bellOnCooldown = false;

    //public Animator bellAnimator;
    [SerializeField] private ParticleSystem soundParticles;
    [SerializeField] private float bellCooldownTime = 3.5f;

    private Enemy ghostScript;
    public bool ghostSearchWithSound = false;
    public int ghostSoundOdds = 3;

    public override void OnNetworkSpawn() {
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
        if(ghostScript == null) {
            ghostScript = GameObject.FindAnyObjectByType<Enemy>();
        }

        if(ghostScript != null) ghostSearchWithSound = ghostScript.GetComponent<GhostRandomizer>().searchWithSound;

        if(ghostScript != null && ghostSearchWithSound && !ghostScript.invisible.Value)
            ghostScript.walkPoint = gameObject.transform.parent.parent.parent.transform.GetChild(1).transform.position;
    }

    [ClientRpc]
    void RingBellClientRpc() {
        // Local visual & audio effects
        //bellAnimator.Play("BellRing");
        soundParticles.Play();
        sourceTwoDim.PlayOneShot(evocareClips[Random.Range(0, evocareClips.Length)]);
        sourceTwoDim.PlayOneShot(airWhooshClips[Random.Range(0, airWhooshClips.Length)]);

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
