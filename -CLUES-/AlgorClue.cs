using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AlgorClue : NetworkBehaviour {


    [HideInInspector] public AlgorClueEmitter emitterScript;
    [HideInInspector] public int duration;
    // This object enters, and then will grow upon being in the presence of Algor.

    [Header("Charging")]
    [SerializeField] private bool charging;

    [SerializeField] private float chargeRate = 1f;

    public NetworkVariable<float> charge = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private const float MaxCharge = 5f;

    [Header("Growth")]
    [SerializeField] private Transform growthObject;
    public GameObject targetObject;

    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float growthSpeed = 5f;

    private float maxDistanceSqr;

    [SerializeField] private NetworkVariable<int> playersWithAlgorPresent = new NetworkVariable<int>(0);
    [SerializeField] private bool kill = false;


    // SOUND Section
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] iceFormingClips; // ice ambient sheen jingle?
    public bool played = false;
    private Coroutine audioLerpingRoutine;
    [HideInInspector] public Thermometer thermometerScript;

    // Start is called before the first frame update
    public override void OnNetworkSpawn() {
        if(IsServer) {
            StartCoroutine(LifecycleTimer());
        }
    }

    private void Awake() {
        maxDistanceSqr = maxDistance * maxDistance;

        // Make sure the growth object starts at zero scale.
        if(growthObject != null)
            growthObject.localScale = Vector3.zero;

        source.clip = iceFormingClips[Random.Range(0, iceFormingClips.Length)];
        source.pitch = Random.Range(.85f, 1.1f);
    }

    // Simply starts a timer of duration x, and then destroys itself after that duration.
    private IEnumerator LifecycleTimer() {
        yield return new WaitForSeconds(duration - 5);

        kill = true;
        //StopParticle();

        yield return new WaitForSeconds(5f);
        // kill self
        if(thermometerScript != null) thermometerScript.RemoveAlgorClue(GetComponent<Collider>());
        emitterScript.RemoveLastCrystal();
        NetworkObject.Despawn();
    }

    public void PlayerWithAlgorEntered() {
        playersWithAlgorPresent.Value++;
    }

    public void PlayerWithAlgorLeft() {
        playersWithAlgorPresent.Value--;
    }


    // Called by the emitter if emitImmediately is true, or by algor if it's close enough.
    public void EmitParticle() {
        //GetComponent<ParticleSystem>().Play();
        //GetComponentInChildren<Animator>().SetBool("Active", true);
        charging = true;
    }

    public void StopParticle() {
        //GetComponent<ParticleSystem>().Stop();
        //GetComponentInChildren<Animator>().SetBool("Active", false);
    }
    private void Update() {
        charging = playersWithAlgorPresent.Value > 0;

        if(charging && !kill) {
            // Build charge on the server.
            if(IsServer && charge.Value < MaxCharge) {
                charge.Value = Mathf.Min(
                    charge.Value + chargeRate * Time.deltaTime,
                    MaxCharge
                );
            }

            // Don't grow until fully charged.
            if(charge.Value < MaxCharge)
                return;

            if(!played) {
                Debug.Log("Playing Ice.");
                played = true;
                source.Play();
            }
            //UpdateGrowthBasedOnPlayerDistance();
            IncreaseGrowth();
        }
        else {
            if(IsServer && charge.Value > 0) {
                charge.Value = Mathf.Max(charge.Value - chargeRate * Time.deltaTime, 0);
            }
            // No longer charging — shrink back to zero.
            ShrinkGrowth();
        }
    }

    private void UpdateGrowthBasedOnPlayerDistance() {
        if(growthObject == null || targetObject == null)
            return;

        Vector3 offset = targetObject.transform.position - transform.position;

        float distanceSqr = offset.sqrMagnitude;

        // Convert distance into a 0-1 value.
        float closeness = 1f - Mathf.Clamp01(
            distanceSqr / maxDistanceSqr
        );

        // Smoothly approach the desired scale.
        float currentScale = growthObject.localScale.x;

        float newScale = Mathf.MoveTowards(
            currentScale,
            closeness,
            growthSpeed * Time.deltaTime
        );

        growthObject.localScale = Vector3.one * newScale;
    }

    private void IncreaseGrowth() {
        if(growthObject == null)
            return;

        float currentScale = growthObject.localScale.x;

        float newScale = Mathf.MoveTowards(
            currentScale,
            1f,
            growthSpeed * Time.deltaTime
        );

        growthObject.localScale = Vector3.one * newScale;
    }

    private void ShrinkGrowth() {
        if(growthObject == null)
            return;

        played = false;
        //AudioController.FadeOutAudio(this, source, 1);
        float currentScale = growthObject.localScale.x;

        float newScale = Mathf.MoveTowards(
            currentScale,
            0f,
            growthSpeed * Time.deltaTime
        );

        growthObject.localScale = Vector3.one * newScale;
    }
    // ALGOR:
    // Player's algor compiles a list of all algorClues in the game.
    // updates this list every x seconds.
    // it then sets it's temperature based on that.
    // -
    // THIS CLUE
    // if server only. tracker is shared?
    // if any player algor become close enough to this clue, clue increments a personal int tracker.
    // if player algor leaves range, lower tracker by 1.
    // on disable and enable works with this..(?)

    // if the tracker is above 1, reveal.
    // if tracker is 0 or lower, hide.
    // Even if particle is playing while revealed, it will Hide(stop particle) and die, as it should.
}
