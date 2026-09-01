using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/*
 * This script is client side only. One will exist per player.
 * The server's GameTimer will call this script to play local effects.
 */
public class GameTimerClient : MonoBehaviour {

    public GameObject[] flameObjects;
    public Volume mainVolume;
    public AudioSource source;
    public AudioClip[] warningClipsA, warningClipsB, warningClipsC;
    [SerializeField] private AudioClip rumbleSmall, rumbleMedium, rumbleLarge;
    [SerializeField] private ParticleSystem[] rumbleParticlesSmall, rumbleParticlesMedium, rumbleParticlesLarge;
    public Material smokeMaterial;
    private Vignette vignetteComponent;

    public static GameTimer serverTimer;
    public int maxTime = -1;

    public bool pausedInitially = true;
    private int totalTimeStored, stageOne, stageTwo, stageThree = -1;
    public int minFlameTick = 3, maxFlameTick = 10;
    private int timeSpent = 0;

    IEnumerator Start() {
        // Wait until the timer exists on this client
        while(serverTimer == null) {
            serverTimer = FindFirstObjectByType<GameTimer>();
            yield return null;
        }

        // Subscribe to changes
        serverTimer.timeLeft.OnValueChanged += OnTimeChanged;

        maxTime = serverTimer.timeLeft.Value;
        Debug.Log("Found: " + maxTime + " servers: " + serverTimer.timeLeft.Value);


        serverTimer.RequestPauseServerRpc(pausedInitially);

        // Initialize UI immediately
        OnTimeChanged(0, serverTimer.timeLeft.Value);

        mainVolume.profile.TryGet(out vignetteComponent);

        vignetteComponent.intensity.value = 0f;
        Color c = smokeMaterial.color;
        c.a = 0;
        smokeMaterial.color = c;


        int diff = maxTime / 4;
        stageOne = maxTime - diff;
        stageTwo = maxTime - 2 * diff;
        stageThree = maxTime - 3 * diff;
    }

    // Updating UI live. Could move into OnTimeChanged for efficiency.
    private void Update() {
        if(serverTimer == null || stageThree == -1) return;

        float diff = maxTime / 4;
        float t = 1f - ((float)serverTimer.timeLeft.Value / ((float)maxTime - diff));
        //Debug.Log(t + " " + vignetteComponent.intensity.value);
        if(maxTime <= stageOne) vignetteComponent.intensity.value = Mathf.Lerp(0.203f, 1f, t);

        if(serverTimer.timeLeft.Value <= stageTwo) {
            vignetteComponent.intensity.value = Mathf.Lerp(vignetteComponent.intensity.value, .8f, Time.deltaTime / serverTimer.timeLeft.Value);
            Color c = smokeMaterial.color;
            c.a = Mathf.Lerp(c.a, .8f, Time.deltaTime / serverTimer.timeLeft.Value * .25f);
            smokeMaterial.color = c;
        }
    }

    private void OnTimeChanged(int initialValue, int newValue) {
        timeSpent++;
        Debug.Log("Time changed: " + newValue);

        //GetComponent<CurseGameManager>().timeSpent = timeSpent;

        int diff = flameObjects.Length / 3;

        if(serverTimer.timeLeft.Value == stageOne) AngelEffectsClientRpc(1, diff);
        else if(serverTimer.timeLeft.Value == stageTwo) AngelEffectsClientRpc(2, diff);
        else if(serverTimer.timeLeft.Value == stageThree) AngelEffectsClientRpc(3, diff);

        if(serverTimer.timeLeft.Value <= 0) {
            // Death?
            //deathScript.Jumpscare(true);
            // Can also set ghost to a new mode, where it perma hunts player.
        }
    }

    void OnDestroy() {
        if(serverTimer != null)
            serverTimer.timeLeft.OnValueChanged -= OnTimeChanged;
    }

    public void AngelEffectsClientRpc(int level, int diff) {
        if(level == 1) {
            StartCoroutine(SpawnFlames(0, diff));
            source.PlayOneShot(warningClipsA[Random.Range(0, warningClipsA.Length)], .7f);
            //ghostScript.invisSpeed += 2;
            //cameraShakeAnimator.Play("ShakeSmall");
            source.PlayOneShot(rumbleSmall);
            StartCoroutine(StartEffects(rumbleParticlesSmall));
        }
        else if(level == 2) {
            StartCoroutine(SpawnFlames(diff, diff * 2));
            source.PlayOneShot(warningClipsB[Random.Range(0, warningClipsB.Length)], .8f);
            //ghostScript.invisSpeed += 2;
            //cameraShakeAnimator.Play("ShakeMedium");
            source.PlayOneShot(rumbleMedium);
            StartCoroutine(StartEffects(rumbleParticlesMedium));

            foreach(ParticleSystem particleRumble in rumbleParticlesSmall) particleRumble.gameObject.SetActive(false);
        }
        else if(level == 3) {
            StartCoroutine(SpawnFlames(diff * 2, diff * 3 + 1));
            source.PlayOneShot(warningClipsC[Random.Range(0, warningClipsC.Length)], 1f);
            //ghostScript.invisSpeed += 3;
            //cameraShakeAnimator.Play("ShakeLarge");
            source.PlayOneShot(rumbleLarge);

            StartCoroutine(StartEffects(rumbleParticlesLarge));

            foreach(ParticleSystem particleRumble in rumbleParticlesSmall) particleRumble.gameObject.SetActive(false);
            foreach(ParticleSystem particleRumble in rumbleParticlesMedium) particleRumble.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartEffects(ParticleSystem[] particleEffects) {
        foreach(ParticleSystem particleRumble in particleEffects) {
            yield return new WaitForSeconds(.3f);
            particleRumble.Play();
        }
    }

    private IEnumerator SpawnFlames(int start, int end) {
        for(int i = start; i < end; i++) {
            yield return new WaitForSeconds(Random.Range(minFlameTick, maxFlameTick));
            flameObjects[i].SetActive(true);
        }
    }
    
    public void KillTimer() {
        //allowedToTimer = false;
    }

}
