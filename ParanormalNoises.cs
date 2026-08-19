using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ParanormalNoises : NetworkBehaviour {

    [SerializeField] private AudioSource spatialSource, twoDimSource;
    [SerializeField] private AudioClip[] stingerClips, diageticClips;
    // diagetic can be shuffle behind you, clanging of bottles, whisper in your ear.
    // Stinger is creepy noise, like you hear in FNAF
    [SerializeField, Range(0.8f, 1.2f)] private float randomMinPitch = 0.95f, randomMaxPitch = 1.05f;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 0.9f;
    [SerializeField] private bool randomlyDelayIfStinger = true;
    [SerializeField] private float minRandomDelay = 0f, maxRandomDelay = 8f;
    [SerializeField, Range(1, 5), Tooltip("Random(0, lateOdds): 1=alwaysLate, 2=1/2, 3=1/3")] private int lateOdds = 3;
    //private LightFlicker lanternScript;
    public bool onCooldown = false; // Cooldown is client-side because we want the ghost to collect 2 charges
    // from touching 2 close players, for example.

    private AudioClip lastPlayedClip;
    private Coroutine noiseRoutine;

    // Does this need to be server only? with client RPC for the effects?
    // Instead, simply only checking server on 51, where we call enemy.IncreaseCharges();
    private void OnTriggerEnter(Collider other) {
        if(!IsServer || !other.CompareTag("Player"))
            return;

        NetworkObject player = other.GetComponentInParent<NetworkObject>();

        if(player != null && !spatialSource.isPlaying && !onCooldown)
            PlaySoundRpc(RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void PlaySoundRpc(RpcParams rpcParams = default) {
        PlayRandomNoise(true);
    }

    public bool IsPlayingParanormal() {
        return spatialSource.isPlaying;
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
            if(transform.parent.GetComponent<Enemy>().IsServer) transform.parent.GetComponent<Enemy>().IncreaseCharges();
            PlayNoise(spatial);
        }
    }

    private void PlayNoise(bool spatial) {
        //lanternScript.StartFlickerPeriod(lastPlayedClip.length);
        FlickerAllLamps();

        if(spatial) {
            spatialSource.pitch = Random.Range(randomMinPitch, randomMaxPitch);
            spatialSource.PlayOneShot(lastPlayedClip, volumeScale);
        }
        else {
            twoDimSource.PlayOneShot(lastPlayedClip, volumeScale);
        }
        // Lantern Flicker
    }

    private void FlickerAllLamps() {
        var lsit = gameObject.GetComponentInParent<Enemy>().listOfPlayers;


        foreach(GameObject player in lsit) {
            if(player.transform.GetChild(12).GetChild(0).GetChild(2).gameObject.activeInHierarchy) 
                player.transform.GetChild(12).GetChild(0).GetChild(2).GetComponent<LightFlickerNonNetworked>().StartFlickerPeriod(lastPlayedClip.length);
            // if light is turned off, problem.
        }
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

    // Because the ghost ran into a light, and turned it off.
    public void StartCooldown() {
        StartCoroutine(CooldownTimer());
    }

    private IEnumerator CooldownTimer() {
        onCooldown = true;
        yield return new WaitForSeconds(5f);
        onCooldown = false;
    }
}
