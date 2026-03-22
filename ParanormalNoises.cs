using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParanormalNoises : MonoBehaviour {

    [SerializeField] private AudioSource spatialSource, twoDimSource;
    [SerializeField] private AudioClip[] stingerClips, diageticClips;
    // diagetic can be shuffle behind you, clanging of bottles, whisper in your ear.
    // Stinger is creepy noise, like you hear in FNAF
    [SerializeField, Range(0.8f, 1.2f)] private float randomMinPitch = 0.95f, randomMaxPitch = 1.05f;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 0.9f;
    [SerializeField] private bool randomlyDelayIfStinger = true;
    [SerializeField] private float minRandomDelay = 0f, maxRandomDelay = 8f;
    [SerializeField, Range(1, 5), Tooltip("Random(0, lateOdds): 1=alwaysLate, 2=1/2, 3=1/3")] private int lateOdds = 3;

    private Enemy ghostScript;
    private AudioClip lastPlayedClip;
    private Coroutine noiseRoutine;

    private void Start() {
        ghostScript = transform.parent.GetComponent<Enemy>();
    }

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player") && !spatialSource.isPlaying) {
            PlayRandomNoise(true);
        }
    }

    // Can make this public and have something distant cause all players to hear this.
    private void PlayRandomNoise(bool spatial) {
        bool playStinger = Random.Range(0, 2) == 0;
        lastPlayedClip = playStinger ? FindRandomClip(stingerClips) : FindRandomClip(diageticClips);

        if(randomlyDelayIfStinger && playStinger && Random.Range(0, lateOdds) == 0) {
            if(noiseRoutine != null) StopCoroutine(noiseRoutine);
            noiseRoutine = StartCoroutine(NoiseDelay(Random.Range(minRandomDelay, maxRandomDelay), spatial));
        }
        else {
            PlayNoise(spatial);
            ghostScript.IncreaseCharges();
        }
    }

    private void PlayNoise(bool spatial) {
        if(spatial) {
            spatialSource.pitch = Random.Range(randomMinPitch, randomMaxPitch);
            spatialSource.PlayOneShot(lastPlayedClip, volumeScale);
        }
        else {
            twoDimSource.PlayOneShot(lastPlayedClip, volumeScale);
        }
        // Lantern Flicker
    }

    private IEnumerator NoiseDelay(float delay, bool spatial) {
        yield return new WaitForSeconds(delay);
        PlayNoise(spatial);
    }

    private AudioClip FindRandomClip(AudioClip[] listOfClips) {
        AudioClip randClip = listOfClips[Random.Range(0, listOfClips.Length)];
        if(lastPlayedClip == null || randClip != lastPlayedClip) return randClip;
        else return FindRandomClip(listOfClips);
    }
}
