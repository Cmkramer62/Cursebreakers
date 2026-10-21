using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class StellaeClue : NetworkBehaviour {

    [HideInInspector] public StellaeClueEmitter emitterScript;
    [HideInInspector] public int duration;

    [Header("Charging")]
    [SerializeField] private bool charging;

    [SerializeField] private float chargeRate = 1f;

    public NetworkVariable<float> charge = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private float maxCharge = 5f;

    [Header("Growth")]
    [SerializeField] private DecalProjector projector;
    [SerializeField] private ParticleSystem particles;

    [SerializeField] private float growthSpeed = 5f;

    [SerializeField] private NetworkVariable<int> playersWithAlgorPresent = new NetworkVariable<int>(0);
    [SerializeField] private bool kill = false;


    // SOUND Section
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] iceFormingClips; // ice ambient sheen jingle?
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
        NetworkObject.Despawn();
    }

    private void Awake() {
        // Make sure the growth object starts at zero scale.
        if(projector != null)
            projector.fadeFactor = 0;
        source.clip = iceFormingClips[Random.Range(0, iceFormingClips.Length)];
        source.pitch = Random.Range(.85f, 1.1f);
    }

    public void PlayerWithAlgorEntered() {
        playersWithAlgorPresent.Value++;
    }

    public void PlayerWithAlgorLeft() {
        playersWithAlgorPresent.Value--;
    }

    private void Update() {
        charging = playersWithAlgorPresent.Value > 0;

        if(charging && !kill) {
            // Build charge on the server.
            if(IsServer && charge.Value < maxCharge) {
                charge.Value = Mathf.Min(
                    charge.Value + chargeRate * Time.deltaTime,
                    maxCharge
                );
            }

            // Don't grow until fully charged.
            if(charge.Value < maxCharge)
                return;

            if(!played) {
                Debug.Log("Playing Ice.");
                played = true;
                source.Play();

                particles.Play();
            }
            IncreaseGrowth();
        }
        else {
            if(IsServer && charge.Value > 0) {
                charge.Value = Mathf.Max(charge.Value - chargeRate * Time.deltaTime, 0);
            }
            ShrinkGrowth();
        }
    }

    private void IncreaseGrowth() {
        if(projector == null)
            return;

        float currentScale = projector.fadeFactor;

        float newScale = Mathf.MoveTowards(
            currentScale,
            1f,
            growthSpeed * Time.deltaTime
        );

        projector.fadeFactor = newScale;
    }

    private void ShrinkGrowth() {
        if(projector == null)
            return;

        if(played) {
            particles.Stop();
        }

        played = false;
        //AudioController.FadeOutAudio(this, source, 1);
        float currentScale = projector.fadeFactor;

        float newScale = Mathf.MoveTowards(
            currentScale,
            0f,
            growthSpeed * Time.deltaTime
        );

        projector.fadeFactor = newScale;
    }
}