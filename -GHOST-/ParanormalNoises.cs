using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles player contact with the ghost's paranormal sound trigger.
/// The server validates contacts and changes aggression; the touched player's
/// client plays the local audio and lantern effect.
/// </summary>
public sealed class ParanormalNoises : NetworkBehaviour {
    [Header("Audio Sources")]
    [SerializeField] private AudioSource spatialSource;
    [SerializeField, FormerlySerializedAs("twoDimSource")] private AudioSource twoDimensionalSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] stingerClips;
    [SerializeField, FormerlySerializedAs("diageticClips")] private AudioClip[] diegeticClips;

    [Header("Playback")]
    [SerializeField, FormerlySerializedAs("randomMinPitch"), Range(0.8f, 1.2f)] private float minimumPitch = 0.95f;
    [SerializeField, FormerlySerializedAs("randomMaxPitch"), Range(0.8f, 1.2f)] private float maximumPitch = 1.05f;
    [SerializeField, FormerlySerializedAs("volumeScale"), Range(0f, 1f)] private float volume = 0.9f;
    [SerializeField, FormerlySerializedAs("randomlyDelayIfStinger")] private bool delaySomeStingers = true;
    [SerializeField, FormerlySerializedAs("minRandomDelay"), Min(0f)] private float minimumStingerDelay;
    [SerializeField, FormerlySerializedAs("maxRandomDelay"), Min(0f)] private float maximumStingerDelay = 8f;
    [SerializeField, Range(1, 5), Tooltip("Chance of delaying a stinger is 1 in this value.")]
    [FormerlySerializedAs("lateOdds")] private int stingerDelayOdds = 3;

    [Header("Touch Cooldown")]
    [Tooltip("Local prop-trigger cooldown, refreshed through StartCooldown().")]
    [SerializeField, FormerlySerializedAs("onCooldown")] private bool isOnCooldown;

    private Enemy ghost;
    private int playerLayerIndex;
    private AudioClip previousClip;
    private LightFlickerNonNetworked localLantern;
    private Coroutine pendingPlayback;
    private Coroutine cooldownRoutine;

    /// <summary>Cache the ghost controller attached to the parent object.</summary>
    private void Awake() {
        ghost = GetComponentInParent<Enemy>();
        playerLayerIndex = LayerMask.NameToLayer("Player");
    }

    /// <summary>
    /// Validate contact on the server, add an aggression charge, then target
    /// sound playback to the contacted player's client.
    /// </summary>
    private void OnTriggerEnter(Collider other) {
        // Filter the contacting collider itself. Checking the parent player's
        // tag here would accept every child collider and add one charge per hit.
        if(!IsServer || ghost == null || !other.CompareTag("Player") ||
            other.gameObject.layer != playerLayerIndex)
            return;

        Death playerDeath = other.GetComponentInParent<Death>();
        if(playerDeath == null || playerDeath.lives.Value <= 0)
            return;

        NetworkObject playerObject = playerDeath.GetComponentInParent<NetworkObject>();
        if(playerObject == null || !playerObject.IsSpawned)
            return;

        // Charge the authoritative ghost for every valid touch, even when the
        // recipient delays the sound or cannot play the configured audio.
        ghost.IncreaseCharges();

        if(!isOnCooldown && spatialSource != null && !spatialSource.isPlaying)
            PlaySoundRpc(RpcTarget.Single(playerObject.OwnerClientId, RpcTargetUse.Temp));
    }

    /// <summary>Play a selected sound on the targeted client.</summary>
    [Rpc(SendTo.SpecifiedInParams)]
    private void PlaySoundRpc(RpcParams rpcParams = default) {
        PlayRandomNoise(spatial: true);
    }

    /// <summary>Return whether a spatial paranormal sound is playing.</summary>
    public bool IsPlayingParanormal() {
        return spatialSource != null && spatialSource.isPlaying;
    }

    /// <summary>Start or refresh the local five-second prop cooldown.</summary>
    public void StartCooldown() {
        if(cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownTimer());
    }

    /// <summary>Select a sound and optionally delay a stinger before playback.</summary>
    private void PlayRandomNoise(bool spatial) {
        bool isStinger = Random.Range(0, 2) == 0;
        AudioClip clip = ChooseClip(isStinger ? stingerClips : diegeticClips);
        if(clip == null)
            return;

        previousClip = clip;
        if(delaySomeStingers && isStinger && stingerDelayOdds > 0 &&
            Random.Range(0, stingerDelayOdds) == 0) {
            if(pendingPlayback != null)
                StopCoroutine(pendingPlayback);

            float delay = Random.Range(
                Mathf.Min(minimumStingerDelay, maximumStingerDelay),
                Mathf.Max(minimumStingerDelay, maximumStingerDelay));
            pendingPlayback = StartCoroutine(PlayAfterDelay(delay, spatial, clip));
            return;
        }

        PlayNoise(spatial, clip);
    }

    /// <summary>Play a clip and flicker the local player's lantern.</summary>
    private void PlayNoise(bool spatial, AudioClip clip) {
        if(clip == null)
            return;

        if(spatial) {
            if(spatialSource == null)
                return;
            FlickerLocalPlayerLantern(clip.length);
            spatialSource.pitch = Random.Range(minimumPitch, maximumPitch);
            spatialSource.PlayOneShot(clip, volume);
        }
        else if(twoDimensionalSource != null) {
            twoDimensionalSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>Flicker the lantern on this client's local player, if present.</summary>
    private void FlickerLocalPlayerLantern(float duration) {
        NetworkManager manager = NetworkManager.Singleton;
        if(manager == null || !manager.IsListening || manager.LocalClient == null)
            return;

        NetworkObject localPlayer = manager.LocalClient.PlayerObject;
        if(localPlayer == null)
            return;

        // Locate the component by type instead of relying on a fixed child index.
        if(localLantern == null)
            localLantern = localPlayer.GetComponentInChildren<LightFlickerNonNetworked>(true);
        if(localLantern != null && localLantern.gameObject.activeInHierarchy)
            localLantern.StartFlickerPeriod(duration);
    }

    /// <summary>Wait for a stinger's delay, then play the selected clip.</summary>
    private IEnumerator PlayAfterDelay(float delay, bool spatial, AudioClip clip) {
        yield return new WaitForSeconds(delay);
        pendingPlayback = null;
        PlayNoise(spatial, clip);
    }

    /// <summary>Choose a non-null clip, avoiding the immediately previous clip.</summary>
    private AudioClip ChooseClip(AudioClip[] clips) {
        if(clips == null || clips.Length == 0)
            return null;

        int firstIndex = Random.Range(0, clips.Length);
        for(int offset = 0; offset < clips.Length; offset++) {
            AudioClip candidate = clips[(firstIndex + offset) % clips.Length];
            if(candidate != null && candidate != previousClip)
                return candidate;
        }
        // Repeating is preferable to silently dropping the sound when every
        // configured array entry references the same clip.
        for(int i = 0; i < clips.Length; i++) {
            if(clips[i] != null)
                return clips[i];
        }
        return null;
    }

    /// <summary>Keep the prop cooldown active for five seconds.</summary>
    private IEnumerator CooldownTimer() {
        isOnCooldown = true;
        yield return new WaitForSeconds(5f);
        isOnCooldown = false;
        cooldownRoutine = null;
    }
}

