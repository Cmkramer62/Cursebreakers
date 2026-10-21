using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations;

public class SpellStellae : NetworkBehaviour {
    public NetworkVariable<bool> stellaeBeamShooting =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> stellaeCharging = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public GameObject flashlightUI, defaultLookPoint;

    public AudioSource source;
    public AudioClip magicSoundClip;
    public Light lightsource;

    public float sprintDuration, staminaRemaining, sprintRecoveryDec;
    public bool isTired, isFlashing = false;

    public InteractRaycast raycastScript;
    public ParticleSystem stellaeBeamParticles, stellaeAbsorbParticles;

    [SerializeField] private PlayerHandler playerHandlerScript;

    private ToolController toolController;
    public GameObject spawnedLightInstance;
    public Transform magicFollowPoint, luxTransformPoint;
    public GameObject luxSpell, stellaeParticlesParent;
    public HashSet<Collider> collidersInside = new HashSet<Collider>();

    // ============
    // Light Mode Variables
    // ============

    [SerializeField] private Color luxColor, stellaeColor;
    [SerializeField] private float luxLightIntensity, stellaeLightIntensity = 2.8f;
    [SerializeField] private float luxLightRange, stellaeLightRange = 11.6f;
    [SerializeField] private Material luxMaterial, stellaeMaterial;

    public override void OnNetworkSpawn() {
        stellaeBeamShooting.OnValueChanged += OnFlashlightChanged;
        stellaeCharging.OnValueChanged += StellaeChargingEffects; 

        // Cache the controller used by the existing animation effects.
        if(playerHandlerScript != null) {
            toolController =
                playerHandlerScript.GetComponent<ToolController>();
        }

        // Apply the current state on every instance, including late joiners.
        ApplyFlashlightEffects(stellaeBeamShooting.Value);

        // Only the owning player needs to register with their UI manager.
        if(IsOwner) {
            StartCoroutine(FindUIManager());
        }

        StartCoroutine(SetStellaeVFXPosition());
    }

    // SetStellaeVFXPosition() waits for LUX to spawn the light.
    // After, it assigns the lux transform as the constraint of the VFX.
    private IEnumerator SetStellaeVFXPosition() {
        var luxScript = luxSpell.GetComponent<SpellLux>();

        while(luxScript.spawnedLightInstance == null) {
            yield return null;
        }

        PositionConstraint constraint = stellaeParticlesParent.GetComponent<PositionConstraint>();
        List<ConstraintSource> sources = new List<ConstraintSource>();
        constraint.GetSources(sources);
        ConstraintSource newSource = new ConstraintSource();
        newSource.sourceTransform = luxScript.spawnedLightInstance.transform;
        newSource.weight = 1.0f;
        sources.Add(newSource);
        constraint.SetSources(sources);
        constraint.enabled = true;
        constraint.constraintActive = true;
    }

    public override void OnNetworkDespawn() {
        stellaeBeamShooting.OnValueChanged -= OnFlashlightChanged;
    }

    // Wait for the scene's UI manager to become available.
    private IEnumerator FindUIManager() {
        while(UIManager.Instance == null) {
            yield return null;
        }

        UIManager.Instance.RegisterGeistlight(this);
    }

    private void OnEnable() {
        SetLightToStellaeMode();
    }

    
    private void OnDisable() {
        if(stellaeBeamParticles != null && stellaeBeamParticles.isPlaying) {
            stellaeBeamParticles.Stop();
        }

        SetLightToLuxMode();

        if(!IsOwner)
            return;

        foreach(var thing in collidersInside) {
            thing.GetComponent<StellaeClue>().PlayerWithAlgorLeft();
        }
        collidersInside.Clear();
        ChangeChargingServerRpc(false);
    }

    // Sets the light to have following presets of Lux. And targeting the left side.
    private void SetLightToLuxMode() {
        if(spawnedLightInstance != null) {
            spawnedLightInstance.GetComponent<DelayFollow>().target = luxTransformPoint;
            //spawnedLightInstance.GetComponent<DelayFollow>().SetNewValues(76.8f, 1200f, 22.5f, 20f);
            spawnedLightInstance.GetComponent<DelayFollow>().SetNewValues(76.8f, 300f, 15f, 99f, 500f);
            if(!luxSpell.activeSelf) {
                spawnedLightInstance.transform.GetChild(0).gameObject.SetActive(false);
                spawnedLightInstance.transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
                spawnedLightInstance.transform.GetChild(2).GetComponent<ParticleSystem>().Play();
            }
            var mainLight = spawnedLightInstance.GetComponentInChildren<Light>();
            mainLight.range = luxLightRange;
            mainLight.color = luxColor;
            mainLight.intensity = luxLightIntensity;
            mainLight.GetComponent<MeshRenderer>().material = luxMaterial;
        }
    }

    // Set the light to have the following presets of Stellae. And targeting the right side.
    private void SetLightToStellaeMode() {
        // Set Follow point of the Lux Light to be target transform.
        // Change Lux light color.
        if(spawnedLightInstance != null) {
            spawnedLightInstance.GetComponent<DelayFollow>().target = magicFollowPoint;
            //spawnedLightInstance.GetComponent<DelayFollow>().SetNewValues(380f, 1200f, 7.2f, 100.8f);
            spawnedLightInstance.GetComponent<DelayFollow>().SetNewValues(600f, 600f, 25f, 150f, 1500f);

            spawnedLightInstance.transform.GetChild(0).gameObject.SetActive(true);
            spawnedLightInstance.transform.GetChild(1).GetComponent<ParticleSystem>().Play();

            var mainLight = spawnedLightInstance.GetComponentInChildren<Light>();
            mainLight.range = stellaeLightRange;
            mainLight.color = stellaeColor;
            mainLight.intensity = stellaeLightIntensity;
            mainLight.GetComponent<MeshRenderer>().material = stellaeMaterial;
        }
    }


    private void OnFlashlightChanged(bool oldValue, bool newValue) {
        ApplyFlashlightEffects(newValue);
    }

    // Handles local visual, audio, particle, and animation effects.
    // This method does NOT change the NetworkVariable.
    private void ApplyFlashlightEffects(bool enabled) {
        isFlashing = enabled;

        if(lightsource != null) {
            StartCoroutine(
                LightHelper.LerpLight(enabled, lightsource, 0.5f, 0.3f)
            );
        }

        if(enabled) {
            if(stellaeBeamParticles != null) {
                stellaeBeamParticles.Play();
            }

            if(toolController != null) {
                toolController.GeistlightAnimation(true);
            }

            if(source != null) {
                AudioController.FadeInAudio(this, source, 0.5f, 0.35f);
            }
        }
        else {
            if(stellaeBeamParticles != null) {
                stellaeBeamParticles.Stop();
            }

            if(toolController != null) {
                toolController.GeistlightAnimation(false);
            }

            if(source != null) {
                AudioController.FadeOutAudio(this, source, 0.5f);
            }
        }
    }

    private void StellaeChargingEffects(bool oldValue, bool newValue) {
        if(newValue) {
            stellaeAbsorbParticles.Play();

           // if(source != null) {
            //    AudioController.FadeInAudio(this, source, 0.5f, 0.35f);
           // }
        }
        else {
            stellaeAbsorbParticles.Stop();

           // if(source != null) {
           //     AudioController.FadeOutAudio(this, source, 0.5f);
           // }
        }
    }

    // Requests a state change from the server.
    [ServerRpc]
    private void SetFlashlightServerRpc(bool enabled) {
        stellaeBeamShooting.Value = enabled;
    }

    private void Update() {
        if(!IsOwner) {
            return;
        }

        // Activate while holding F, provided the player isn't tired.
        if(Input.GetKeyDown(KeyCode.F) && !isTired && !stellaeBeamShooting.Value) {
            SetFlashlightServerRpc(true);
        }

        // Turn off when F is released or the player becomes tired.
        else if( (Input.GetKeyUp(KeyCode.F) && stellaeBeamShooting.Value) || (isTired && stellaeBeamShooting.Value) ) {
            SetFlashlightServerRpc(false);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(!IsOwner)
            return;

        if(other.CompareTag("StellaeClue")) {
            collidersInside.Add(other);
            other.GetComponent<StellaeClue>().PlayerWithAlgorEntered();
            other.GetComponent<StellaeClue>().stellaeScript = this;

            if(collidersInside.Count > 0) {
                StartCoroutine(StellaeSuckRoutine());
            }
        }
    }

    public void RemoveAtCollider(Collider other) {
        collidersInside.Remove(other);
    }

    private IEnumerator StellaeSuckRoutine() {
        yield return new WaitForSeconds(2f);
        if(collidersInside.Count > 0) {
            ChangeChargingServerRpc(true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if(!IsOwner)
            return;

        if(other.CompareTag("StellaeClue")) {
            RemoveAtCollider(other);
            other.GetComponent<StellaeClue>().PlayerWithAlgorLeft();
            
            if(collidersInside.Count == 0) {
                ChangeChargingServerRpc(false);
            }
        }
    }

    [ServerRpc]
    private void ChangeChargingServerRpc(bool state) {
        stellaeCharging.Value = state;
    }
}
