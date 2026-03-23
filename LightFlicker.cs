using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour {

    public Light source;
    public bool makesNoise, materialSwap = false;
    public bool flicker = true;

    public float maximumDim = 0f;
    // maximumBoost currently being overidden to the default intensity.
    public float maximumBoost = 1f;
    public float tickSpeed = 0.04f;
    public float strength = 200;
    private float defaultIntensity;

    public int minSecAwake = 9;
    public int maxSecAwake = 30;
    public int minSecDead = 3;
    public int maxSecDead = 15;
    private bool flickeringActive = false;

    // Used to forcefully interrupt flickering routines and make light only turn On or Off.
    private bool forceChange = false;

    public Material deadBulbMat, originalAliveBulbMat;
    [SerializeField] private Material aliveBulbMat;
    public MeshRenderer bulbRenderer;
    public AudioSource buzzingSource, flickeringSource, interactSource;
    public AudioClip turnOnSound, turnOffSound;

    public bool alive = true;

    public void OnEnable() {
        if(source == null) source = GetComponent<Light>();

        defaultIntensity = source.intensity;
        maximumBoost = defaultIntensity;

        if(makesNoise) buzzingSource.Play();
        if(materialSwap) aliveBulbMat = new Material(originalAliveBulbMat);

        StartCoroutine(StartCycleBuffer());

    }




    private void Update() {
        // Debug.Log(source.intensity);
    }

    #region COROUTINE PROCEDURES FOR FLICKER CYCLE

    /*
     * For some unkown reason, setting the light's intensity to 0 on the very
     * first frame does not work. Waiting half of a second solves this.
     */
    private IEnumerator StartCycleBuffer() {
        yield return new WaitForSeconds(.3f);
        if(alive && flicker) StartCoroutine(AwakenLight());
        if(!alive) TurnOffLight(false);
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
    public void TurnOffLight(bool flickerOff) {
        alive = false;
        forceChange = true;

        if(flickerOff) StartCoroutine(StartFlickerLight());
        else StartCoroutine(KillLight());
        interactSource.PlayOneShot(turnOffSound);
    }

    /*
     * Public method used for remotely turning on the light.
     * Bypasses any protocol for the flicker cycle.
     */
    public void TurnOnLight() {
        alive = true;
        forceChange = false;
        StartCoroutine(AwakenLight());
        interactSource.PlayOneShot(turnOnSound);
    }

    public void InvertLightState() {
        if(alive) TurnOffLight(false);
        else TurnOnLight();
    }

}


