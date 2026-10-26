using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class ProfanusClue : NetworkBehaviour {
    public NetworkVariable<bool> profaneBlood = new NetworkVariable<bool>(false);

    [HideInInspector] public ProfanusClueEmitter emitterScript;
    [HideInInspector] public int duration;

    [SerializeField] private NetworkVariable<int> playersWithAlgorPresent = new NetworkVariable<int>(0);
    [SerializeField] private bool kill = false;

    // SOUND Section
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip hissingClip;

    // VFX
    [SerializeField] private ParticleSystem steamParticles;

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

    public void PlayerWithAlgorEntered() {
        playersWithAlgorPresent.Value++;
    }

    public void PlayerWithAlgorLeft() {
        playersWithAlgorPresent.Value--;
    }

    public void ApplyHolyWater() {
        if(profaneBlood.Value) {
            Debug.Log("HOLY REACT.");
            ApplyHolyEffectsServerRpc();
        }
        else {
            Debug.Log("HOLY REACT but not profane.");
        }
    }
   
    [ServerRpc]
    private void ApplyHolyEffectsServerRpc() {
        ApplyHolyEffectsClientRpc();
    }

    [ClientRpc]
    private void ApplyHolyEffectsClientRpc() {
        steamParticles.Play();
        source.pitch = Random.Range(.9f, 1.1f);
        source.PlayOneShot(hissingClip);
    }
}