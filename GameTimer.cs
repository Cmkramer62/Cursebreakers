using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameTimer : MonoBehaviour {

    public GameObject[] flameObjects;
    public int totalTimeLimit, timeSpent = 0;
    public int minFlameTick = 3, maxFlameTick = 10;
    public Volume mainVolume;
    public Death deathScript;
    [SerializeField] private Enemy ghostScript;

    public AudioSource source;
    public AudioClip[] warningClipsA, warningClipsB, warningClipsC;
    [SerializeField] private AudioClip rumbleSmall, rumbleMedium, rumbleLarge;

    public Material smokeMaterial;

    public bool startTimer = true;

    [SerializeField] private Animator cameraShakeAnimator;
    
    private Vignette vignetteComponent;
    private int totalTimeStored, stageOne, stageTwo, stageThree;
    private bool allowedToTimer = true;

    // stage 0 is it's off. stage 1 is it's turned on at half size. stage 2 is normal size.
    // after stage one, increase vignette constantly over time until max intensity at 0.

    // Start is called before the first frame update
    void Start() {
        if(startTimer) {
            totalTimeStored = totalTimeLimit;
            StartCoroutine(Tick());

            int diff = totalTimeLimit / 4;
            stageOne = totalTimeLimit - diff;
            stageTwo = totalTimeLimit - 2 * diff;
            stageThree = totalTimeLimit - 3 * diff;
        }

        mainVolume.profile.TryGet(out vignetteComponent);

        vignetteComponent.intensity.value = 0f;
        Color c = smokeMaterial.color;
        c.a = 0;
        smokeMaterial.color = c;
    }

    private void Update() {
        if(startTimer && allowedToTimer) {
            /*
            float diff = secondsOfTimer / 4;
            float t = 1f - ((float)secondsOfTimer / ((float)maxTime - diff));
            Debug.Log(t + " " + vignetteComponent.intensity.value);
            if(secondsOfTimer <= stageOne) vignetteComponent.intensity.value = Mathf.Lerp(0.203f, 1f, t);
            */
            if(totalTimeLimit <= stageTwo) {
                vignetteComponent.intensity.value = Mathf.Lerp(vignetteComponent.intensity.value, 1f, Time.deltaTime / totalTimeLimit);
                Color c = smokeMaterial.color;
                c.a = Mathf.Lerp(c.a, .8f, Time.deltaTime / totalTimeLimit * .25f);
                smokeMaterial.color = c;
            }
        } 
    }

    private IEnumerator Tick() {
        yield return new WaitForSeconds(1f);
        totalTimeLimit--;
        timeSpent++;
        GetComponent<CurseGameManager>().timeSpent = timeSpent;

        int diff = flameObjects.Length / 3;

        if(totalTimeLimit == stageOne && allowedToTimer) {
            StartCoroutine(SpawnFlames(0, diff));
            source.PlayOneShot(warningClipsA[Random.Range(0, warningClipsA.Length)], .7f);
            ghostScript.invisSpeed += 2;
            cameraShakeAnimator.Play("ShakeSmall");
            source.PlayOneShot(rumbleSmall);
        }
        else if(totalTimeLimit == stageTwo && allowedToTimer) {
            StartCoroutine(SpawnFlames(diff, diff * 2));
            source.PlayOneShot(warningClipsB[Random.Range(0, warningClipsB.Length)], .8f);
            ghostScript.invisSpeed += 2;
            cameraShakeAnimator.Play("ShakeMedium");
            source.PlayOneShot(rumbleMedium);
        }
        else if(totalTimeLimit == stageThree && allowedToTimer) {
            StartCoroutine(SpawnFlames(diff * 2, diff * 3 + 1));
            source.PlayOneShot(warningClipsC[Random.Range(0, warningClipsC.Length)], 1f);
            ghostScript.invisSpeed += 3;
            cameraShakeAnimator.Play("ShakeLarge");
            source.PlayOneShot(rumbleLarge);
        }

        if(totalTimeLimit <= 0 && allowedToTimer) {
            // Death?
            deathScript.Jumpscare();
            // Can also set ghost to a new mode, where it perma hunts player.
        }
        else if(allowedToTimer) {
            StartCoroutine(Tick());
        }
    }

    private IEnumerator SpawnFlames(int start, int end) {
        for(int i = start; i < end; i++) {
            yield return new WaitForSeconds(Random.Range(minFlameTick, maxFlameTick));
            //if(!flameObjects[i].activeSelf) {
                flameObjects[i].SetActive(true);
                //break;
            //}
        }
    }

    public void KillTimer() {
        allowedToTimer = false;
    }
}
