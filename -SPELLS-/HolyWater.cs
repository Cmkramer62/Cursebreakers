using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HolyWater : NetworkBehaviour {


    public NetworkVariable<bool> active = new NetworkVariable<bool>(false);
    public ParticleSystem steamParticle, sprayParticle;
    public Animator chaliceAnimator;
    [SerializeField] private GameObject holyWaterProjectilePrefab;
    [SerializeField] private Transform waterSpawnPoint;

    [SerializeField] private float throwForce = 8f;
    [SerializeField] private float upwardForce = 2f;

    // AUDIO
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] waterSplashClips;

    public void OnEnable() {
        if(IsServer) transform.parent.parent.parent.GetComponent<ToolController>().CheckHolyWater();
        if(active.Value) steamParticle.Play();
        /*
                if(active.Value) {
                    steamParticle.Pause();
                    steamParticle.gameObject.SetActive(false);
                    steamParticle.gameObject.SetActive(true);
                    steamParticle.

                }
        */
    }

    public override void OnNetworkSpawn() {
        // Subscribe to changes
        active.OnValueChanged += OnHolyChanged;

        gameObject.SetActive(false);
    }

    public void OnHolyChanged(bool oldValue, bool newValue) {
        TurnSteam(newValue);
    }

    public void TurnSteam(bool state) {
        if(state) steamParticle.Play();
        else steamParticle.Stop();
    }


    private void Update() {
        if(!IsOwner) {
            return;
        }

        // Activate while holding F, provided the player isn't tired.
        if(KeyBindingManager.GetKeyDown(GameAction.UseAbility)) {
            // Splash Water
            ThrowHolyWaterServerRpc();
        }
    }

    [ServerRpc]
    private void ThrowHolyWaterServerRpc() {
        // Visuals.
        ThrowHolyWaterClientRpc();

        // Physics detection.
        ThrowHolyWater();
        // How to detect if it touches a blood spot?
    }

    [ClientRpc]
    private void ThrowHolyWaterClientRpc() {
        // animation of chalice.
        // sound effects.
        // spawn water particles. They have collision enabled.
        sprayParticle.Play();
        chaliceAnimator.Play("Holy Water Throw Anim");
        //source.pitch = Random.Range(.9f, 1.1f);
        //source.PlayOneShot(waterSplashClips[Random.Range(0, waterSplashClips.Length)]);
    }

    private void ThrowHolyWater() {
        if(!IsServer)
            return;

        GameObject projectile = Instantiate(
            holyWaterProjectilePrefab,
            waterSpawnPoint.position,
            waterSpawnPoint.rotation
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        Vector3 launchDirection =
            waterSpawnPoint.forward + Vector3.up * (upwardForce / throwForce);

        launchDirection.Normalize();

        rb.AddForce(launchDirection * throwForce, ForceMode.Impulse);
    }
}

