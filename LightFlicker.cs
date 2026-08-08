using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class LightFlicker : NetworkBehaviour {

    public Light source;
    public bool makesNoise, materialSwap = false, flicker = true, alive = true;
    public float maximumDim = 0f, maximumBoost = 1f, tickSpeed = 0.04f, strength = 200;
    public int minSecAwake = 9, maxSecAwake = 30, minSecDead = 3, maxSecDead = 15;

    public Material deadBulbMat, originalAliveBulbMat, aliveBulbMat;
    public MeshRenderer bulbRenderer;
    public AudioSource buzzingSource, flickeringSource, interactSource;
    public AudioClip turnOnSound, turnOffSound, blowUpSound;

    [SerializeField] ParticleSystem blowUpParticles;

    private float defaultIntensity;
    // IF YOU SYNC ANY VARS, REMOVE THEIR ASSIGNMENTS OF THE CLIENT RPCs AND PUT THEM IN THE SERVER RPCs!
    private bool flickeringActive = false, stored = false, forceChange = false;

    public void OnEnable() {
        if(source == null) source = GetComponent<Light>();

        if(!stored) {
            defaultIntensity = source.intensity;
            stored = true;
        }
        source.intensity = defaultIntensity;
        maximumBoost = defaultIntensity;

        if(makesNoise) buzzingSource.Play();
        if(materialSwap) aliveBulbMat = new Material(originalAliveBulbMat);

        StartCoroutine(StartCycleBuffer());

    }

    // These should all be called from the server only.
    // AKA all the serverRPC methods below this region at line 120.
    // IF YOU SYNC ANY VARS, REMOVE THEIR ASSIGNMENTS OF THE CLIENT RPCs AND PUT THEM IN THE SERVER RPCs!
    #region COROUTINE PROCEDURES FOR FLICKER CYCLE

    /*
     * For some unkown reason, setting the light's intensity to 0 on the very
     * first frame does not work. Waiting half of a second solves this.
     */
    private IEnumerator StartCycleBuffer() {
        yield return new WaitForSeconds(.3f);
        if(alive && flicker) StartCoroutine(AwakenLight());
        if(!alive) TurnOffLightServerRpc(false);
    }

    private IEnumerator AwakenLight() {
        if(makesNoise) {
            buzzingSource.UnPause();
        }
        if(materialSwap) {
            bulbRenderer.material = aliveBulbMat;
            //if(materialSwap) aliveBulbMat.SetColor("_EmissionColor", Color.white * 1f);
        }
        source.intensity = defaultIntensity;
        yield return new WaitForSeconds(Random.Range(minSecAwake, maxSecAwake));
        if(!forceChange && flicker) StartCoroutine(StartFlickerLight());
    }

    private IEnumerator StartFlickerLight() {
        if(makesNoise && flickeringSource.isPlaying) flickeringSource.UnPause();
        else if(makesNoise) flickeringSource.Play();

        flickeringActive = true;
        StartCoroutine(FlickerLight());
        yield return new WaitForSeconds(1f);
        flickeringActive = false;

        if(makesNoise) flickeringSource.Pause();
        if(!forceChange) {
            if(Random.Range(1, 3) == 1) StartCoroutine(KillLight()); // 1 in 3 chance light dies when flickering.
            else StartCoroutine(AwakenLight());
        }
        else StartCoroutine(KillLight());
    }

    // Only called by lantern.
    public void StartFlickerPeriod(float duration) {
        StartCoroutine(StartFlickerPeriodTimer(duration));
    }

    private IEnumerator StartFlickerPeriodTimer(float duration) {
        flickeringActive = true;
        StartCoroutine(FlickerLight());
        yield return new WaitForSeconds(duration);
        flickeringActive = false;
    }

    private IEnumerator FlickerLight() {
        source.intensity = Mathf.Lerp(source.intensity, Random.Range(maximumDim, maximumBoost), strength * Time.deltaTime);
        //if(materialSwap) aliveBulbMat.SetColor("_EmissionColor", Color.white * source.intensity);
        yield return new WaitForSeconds(Random.Range(0.01f, tickSpeed + 0.01f));
        if(flickeringActive) StartCoroutine(FlickerLight());
    }

    private IEnumerator KillLight() {
        if(makesNoise) {
            flickeringSource.Pause();
            buzzingSource.Pause();
        }
        if(materialSwap) {
            bulbRenderer.material = deadBulbMat;
        }
        source.intensity = 0;

        yield return new WaitForSeconds(Random.Range(minSecDead, maxSecDead));
        //Debug.Log(source.intensity);
        if(!forceChange) StartCoroutine(AwakenLight());
    }

    #endregion

    /*
     * Public method used for remotely turning off the light.
     * Bypasses any protocol for the flicker cycle.
     */
    [ServerRpc(RequireOwnership = false)]
    public void TurnOffLightServerRpc(bool flickerOff) {
        TurnOffLightClientRpc(flickerOff);
    }

    [ClientRpc]
    private void TurnOffLightClientRpc(bool flickerOff) {
        alive = false;
        forceChange = true;

        if(flickerOff) StartCoroutine(StartFlickerLight());
        else StartCoroutine(KillLight());
        interactSource.PlayOneShot(turnOffSound);
    }

    /*
     * Public method used for remotely turning off the light.
     * Bypasses any protocol for the flicker cycle.
     */
    [ServerRpc]
    public void BlowUpLightServerRpc() {
        BlowUpLightClientRpc();
    }

    [ClientRpc]
    private void BlowUpLightClientRpc() {
        alive = false;
        forceChange = true;

        StartCoroutine(KillLight());
        interactSource.PlayOneShot(blowUpSound);
        blowUpParticles.Play();

        gameObject.tag = "Generic";
    }

    [ServerRpc(RequireOwnership = false)]
    public void InvertLightStateServerRpc() {
        InvertLightStateClientRpc();
    }

    [ClientRpc]
    private void InvertLightStateClientRpc() {
        if(alive) TurnOffLightServerRpc(false);
        else {
            alive = true;
            forceChange = false;
            StartCoroutine(AwakenLight());
            interactSource.PlayOneShot(turnOnSound);
        }
    }

}


