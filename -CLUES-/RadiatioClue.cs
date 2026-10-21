using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RadiatioClue : NetworkBehaviour {
    [HideInInspector] public RadiatioClueEmitter emitterScript;
    [HideInInspector] public int duration;

    public NetworkVariable<int> emfLevel = new NetworkVariable<int>(8);

    // Start is called before the first frame update
    public override void OnNetworkSpawn() {
        if(IsServer) {
            StartCoroutine(LifecycleTimer());
        }
    }

    // Simply starts a timer of duration x, and then destroys itself after that duration.
    private IEnumerator LifecycleTimer() {
        yield return new WaitForSeconds(duration);
        emitterScript.RemoveLastCrystal();
        NetworkObject.Despawn();
    }

}