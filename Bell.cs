using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bell : NetworkBehaviour {

    public AudioSource source;
    public AudioClip ringClip;

    public bool bellOnCooldown = false;

    public Animator bellAnimator;

    private Enemy ghostScript;
    public bool ghostSearchWithSound = false;
    public int ghostSoundOdds = 3;

    public NetworkVariable<bool> bellTriggered = new NetworkVariable<bool>();

    public override void OnNetworkSpawn() {
        bellTriggered.OnValueChanged += OnBellTriggered;
    }

    public override void OnNetworkDespawn() {
        bellTriggered.OnValueChanged -= OnBellTriggered;
    }

    void Start() {
        ghostScript = GameObject.Find("Ghost Enemy").GetComponent<Enemy>();
    }

    void Update() {
        if(!IsOwner) return;

        if(Input.GetKeyUp(KeyCode.F)) {
            RingBellServerRpc();
        }
    }

    [ServerRpc]
    void RingBellServerRpc() {
        if(bellOnCooldown) return;
        
        bellOnCooldown = true;
        
        bellTriggered.Value = !bellTriggered.Value; // Setting RPC value
        
        TriggerCurse(true); // state here is not used. This may be wrong, depending on how ghost is synced.
        
        if(ghostScript != null && ghostSearchWithSound && !ghostScript.invisible)
            ghostScript.walkPoint = gameObject.transform.parent.parent.parent.transform.GetChild(1).transform.position;

        StartCoroutine(BellCooldownTimer());
    }

    void OnBellTriggered(bool oldValue, bool newValue) {
        bellAnimator.Play("BellRing");

        source.pitch = Random.Range(.95f, 1.1f);
        source.PlayOneShot(ringClip);
    }

    private IEnumerator BellCooldownTimer() {
        yield return new WaitForSeconds(2f);
        bellOnCooldown = false;
    }

    private void OnDisable() {
        bellOnCooldown = false;
    }

    private void TriggerCurse(bool state) {
        foreach(CursedObject objectee in gameObject.transform.parent.parent.parent.GetComponentInChildren<ToolController>().objectsList) {
            objectee.DisplayCurse(CursedObject.CursedTypes.Sound, state);
        }
    }
}
