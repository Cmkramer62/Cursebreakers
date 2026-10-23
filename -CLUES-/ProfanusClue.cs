using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class ProfanusClue : NetworkBehaviour {

    [HideInInspector] public ProfanusClueEmitter emitterScript;
    [HideInInspector] public int duration;

    [SerializeField] private NetworkVariable<int> playersWithAlgorPresent = new NetworkVariable<int>(0);
    [SerializeField] private bool kill = false;

    // SOUND Section
    [SerializeField] private AudioSource source, sourceClueEmit;
    [SerializeField] private AudioClip clueAppearClip;
    [SerializeField] private AudioClip[] iceFormingClips;
    [SerializeField] private float appearVolume = 1f, windEnergyVolume = 1f;
    public bool played = false;
    private Coroutine audioLerpingRoutine;

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

        //if(stellaeScript != null) {
            // (The player may not have come into contact with this clue ever.)
          //  stellaeScript.RemoveAtCollider(GetComponent<Collider>());
        //}
        NetworkObject.Despawn();
    }

    private void Awake() {
        source.clip = iceFormingClips[Random.Range(0, iceFormingClips.Length)];
        source.pitch = Random.Range(.85f, 1.1f);
    }

    public void PlayerWithAlgorEntered() {
        playersWithAlgorPresent.Value++;
    }

    public void PlayerWithAlgorLeft() {
        playersWithAlgorPresent.Value--;
    }


   
}