using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class GameTimer : NetworkBehaviour {

    public GameObject[] flameObjects;
    public NetworkVariable<int> totalTimeLimit = new NetworkVariable<int>(
       600,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
    );
    private int timeSpent = 0;
    public int minFlameTick = 3, maxFlameTick = 10;
    public Volume mainVolume;
    public Death deathScript;
    [SerializeField] private Enemy ghostScript;

    public AudioSource source;
    public AudioClip[] warningClipsA, warningClipsB, warningClipsC;
    [SerializeField] private AudioClip rumbleSmall, rumbleMedium, rumbleLarge;
    [SerializeField] private ParticleSystem[] rumbleParticlesSmall, rumbleParticlesMedium, rumbleParticlesLarge;
    public Material smokeMaterial;

    public bool startTimer = true;

    [SerializeField] private Animator cameraShakeAnimator;
    
    private Vignette vignetteComponent;
    private int totalTimeStored, stageOne, stageTwo, stageThree;
    private bool allowedToTimer = true;

    // stage 0 is it's off. stage 1 is it's turned on at half size. stage 2 is normal size.
    // after stage one, increase vignette constantly over time until max intensity at 0.

    // Start is called before the first frame update
    public override void OnNetworkSpawn() {
        if(!IsServer) return;

        if(startTimer) {
            totalTimeStored = totalTimeLimit.Value;
            StartCoroutine(Tick());

            int diff = totalTimeLimit.Value / 4;
            stageOne = totalTimeLimit.Value - diff;
            stageTwo = totalTimeLimit.Value - 2 * diff;
            stageThree = totalTimeLimit.Value - 3 * diff;
        }

        mainVolume.profile.TryGet(out vignetteComponent);

        vignetteComponent.intensity.value = 0f;
        Color c = smokeMaterial.color;
        c.a = 0;
        smokeMaterial.color = c;
    }

    private void Update() {
        if(!IsServer) return;

        if(startTimer && allowedToTimer) {
            /*
            float diff = secondsOfTimer / 4;
            float t = 1f - ((float)secondsOfTimer / ((float)maxTime - diff));
            Debug.Log(t + " " + vignetteComponent.intensity.value);
            if(secondsOfTimer <= stageOne) vignetteComponent.intensity.value = Mathf.Lerp(0.203f, 1f, t);
            */
            if(totalTimeLimit.Value <= stageTwo) {
                vignetteComponent.intensity.value = Mathf.Lerp(vignetteComponent.intensity.value, .8f, Time.deltaTime / totalTimeLimit.Value);
                Color c = smokeMaterial.color;
                c.a = Mathf.Lerp(c.a, .8f, Time.deltaTime / totalTimeLimit.Value * .25f);
                smokeMaterial.color = c;
            }
        } 
    }

    private IEnumerator Tick() {
        yield return new WaitForSeconds(1f);
        totalTimeLimit.Value--;
        timeSpent++;
        GetComponent<CurseGameManager>().timeSpent = timeSpent;

        int diff = flameObjects.Length / 3;

        if(totalTimeLimit.Value == stageOne && allowedToTimer) {
            AngelEffectsClientRpc(1, diff);
        }
        else if(totalTimeLimit.Value == stageTwo && allowedToTimer) {
            AngelEffectsClientRpc(2, diff);
        }
        else if(totalTimeLimit.Value == stageThree && allowedToTimer) {
            AngelEffectsClientRpc(3, diff);
        }

        if(totalTimeLimit.Value <= 0 && allowedToTimer) {
            // Death?
            deathScript.Jumpscare(true);
            // Can also set ghost to a new mode, where it perma hunts player.
        }
        else if(allowedToTimer) {
            StartCoroutine(Tick());
        }
    }

    [ClientRpc]
    public void AngelEffectsClientRpc(int level, int diff) {
        if(level == 1) {
            StartCoroutine(SpawnFlames(0, diff));
            source.PlayOneShot(warningClipsA[Random.Range(0, warningClipsA.Length)], .7f);
            ghostScript.invisSpeed += 2;
            cameraShakeAnimator.Play("ShakeSmall");
            source.PlayOneShot(rumbleSmall);
            StartCoroutine(StartEffects(rumbleParticlesSmall));
        }
        else if(level == 2) {
            StartCoroutine(SpawnFlames(diff, diff * 2));
            source.PlayOneShot(warningClipsB[Random.Range(0, warningClipsB.Length)], .8f);
            ghostScript.invisSpeed += 2;
            cameraShakeAnimator.Play("ShakeMedium");
            source.PlayOneShot(rumbleMedium);
            StartCoroutine(StartEffects(rumbleParticlesMedium));

            foreach(ParticleSystem particleRumble in rumbleParticlesSmall) particleRumble.gameObject.SetActive(false);
        }
        else if(level == 3) {
            StartCoroutine(SpawnFlames(diff * 2, diff * 3 + 1));
            source.PlayOneShot(warningClipsC[Random.Range(0, warningClipsC.Length)], 1f);
            ghostScript.invisSpeed += 3;
            cameraShakeAnimator.Play("ShakeLarge");
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
        allowedToTimer = false;
    }
}
