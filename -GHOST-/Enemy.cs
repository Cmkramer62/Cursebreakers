using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

/// <summary>Server-authoritative ghost AI with client-side presentation.</summary>
public class Enemy : NetworkBehaviour {

    [Header("Navigation and Combat")]
    [Tooltip("Layers the ghost may use when choosing patrol destinations.")]
    public LayerMask groundLayer;
    [Tooltip("Layers used to detect players within melee range.")]
    public LayerMask playerLayer;
    public float health;
    public float walkPointMin;
    public float walkPointRange;
    public float timeBetweenAttacks;
    public float attackRange;
    public float walkSpeed;
    public float runSpeed;
    public float invisSpeed;
    public float chaseMeter = 100f;
    public float rotationSpeed = 5f;

    [Header("Ragdoll hit")]
    [Tooltip("Impulse in ghost-local space: X is sideways, Y is upward, and Z is forward.")]
    [SerializeField] private Vector3 ghostRagdollImpulse = new Vector3(0f, 2f, 8f);

    [Tooltip("Server-owned charge count used by ghost aggression and manifestation.")]
    public NetworkVariable<int> aggressionCharges = new NetworkVariable<int>(0);

    [SerializeField] private int damage;
    [SerializeField] private int invisibilityOdds = 3;
    [SerializeField] private int pauseChance = 4;
    [SerializeField] private int deAggroCooldown = 10;
    [Header("Authored NavMesh Link traversal")]
    [Tooltip("Time the ghost takes to travel across an authored NavMesh Link.")]
    [SerializeField, Min(0.1f)] private float linkJumpDuration = 0.8f;
    [Tooltip("Height added to the midpoint of the ghost's jump arc.")]
    [SerializeField, Min(0f)] private float linkJumpHeight = 1.5f;
    [Tooltip("How often patrol checks charge-driven visibility.")]
    [SerializeField, Min(0.1f)] private float manifestationCheckInterval = 1f;
    [Tooltip("Animator bool set while traversing a NavMesh Link. Add this parameter to each ghost Animator Controller.")]
    [SerializeField] private string linkJumpAnimatorParameter = "Jumping";
    public ParticleSystem hitEffect;

    [Header("Replicated Ghost State")]
    public NetworkVariable<bool> invisible = new NetworkVariable<bool>();
    public NetworkVariable<bool> freezingAura = new NetworkVariable<bool>();
    public NetworkVariable<bool> attractedToSound = new NetworkVariable<bool>();
    public NetworkVariable<bool> allowedToMove = new NetworkVariable<bool>();
    public NetworkVariable<bool> geistAura = new NetworkVariable<bool>(false);

    [Header("Visibility Presentation")]
    public SkinnedMeshRenderer[] meshRenderers;
    public GameObject[] horns;
    public ParticleSystem geistlightAura;
    public GameObject shadow;
    public GameObject paranormalSounds;

    /// <summary>Current server-side ghost behavior state.</summary>
    public enum Mode { Chasing, Patrolling }
    public Mode currentMode;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource monsterSource;
    public AudioSource ambientSource;
    public AudioClip attackClip;
    public AudioClip chaseMusicClip;
    public AudioClip normalMusicClip;
    public AudioClip[] screamClips;
    [Tooltip("Optional looping 3D source for footsteps heard near the ghost. Assign the finished clip to this source.")]
    [SerializeField] private AudioSource chaseFootstepsSource;
    [Tooltip("Maximum distance at which nearby players can hear the footsteps.")]
    [SerializeField, Min(0.1f)] private float chaseFootstepsMaxDistance = 20f;
    [Header("Chase Murmurs")]
    [Tooltip("Optional secondary source for murmuring during a chase. Assign a looping clip to this source.")]
    [SerializeField] private AudioSource chaseMurmurSource;
    [Tooltip("Time taken to raise murmur volume when a chase starts.")]
    [SerializeField, Min(0f)] private float murmurFadeInDuration = 2f;
    [Tooltip("Time taken to lower murmur volume when a chase ends.")]
    [SerializeField, Min(0f)] private float murmurFadeOutDuration = 2f;

    [Header("Animation and Targeting")]
    [Tooltip("Current patrol destination or last known target position.")]
    public Vector3 walkPoint;
    public Animator animator, shadowAnimator, fulgorShadowAnimator;

    [SerializeField] private GameObject playerLastSeenMarkerPrefab;
    public NetworkVariable<NetworkObjectReference> playerLastSeen = new NetworkVariable<NetworkObjectReference>();
    public NetworkVariable<float> veloNetwork = new NetworkVariable<float>(0f);
    // Replicates traversal state so remote clients play the same jump animation.
    public NetworkVariable<bool> jumpingLink = new NetworkVariable<bool>(false);

    // The player the ghost is currently looking at.
    // Written by the server, read by all clients.
    private NetworkVariable<NetworkObjectReference> lookTargetPlayer =
        new NetworkVariable<NetworkObjectReference>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Replicated separately from the local AI mode so late-joining clients
    // immediately receive the correct chase-footstep state.
    private NetworkVariable<bool> chaseFootstepsActive =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Head Tracking")]
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Transform defaultLookTarget;

    [SerializeField] private float lookSmoothTime = 0.25f;
    [SerializeField] private float lookHeightOffset = 1.8f;

    private Vector3 lookVelocity;

    #region Runtime State
    private NavMeshAgent agent;
    /// <summary>Server-maintained player objects used by target selection.</summary>
    public List<GameObject> listOfPlayers = new List<GameObject>();
    private Transform cachedTransform;
    // Patrol and combat state are authoritative and only advanced by the server.
    private bool hasPatrolDestination;
    private bool attackOnCooldown;
    private bool waitingForScream;
    private bool patrolIsPaused;
    private bool traversingNavMeshLink;
    private float nextManifestationCheckTime;
    private float murmurTargetVolume;
    private Coroutine murmurFadeRoutine;
    private bool? appliedChaseAudioState;
    public NetworkVariable<bool> normalAggro = new NetworkVariable<bool>(true);
    private ConeLOSDetector coneDetector;
    private float longestChaseDuration;
    private float currentChaseDuration;
    private float originalWalkSpeed = -1f;

    /// <summary>Server-side cache of living players currently visible to the ghost.</summary>
    public List<GameObject> playersVisible = new List<GameObject>();
    private GameObject currentTargetPlayer;
    #endregion

    /// <summary>Move the ghost to the spawn transform on every connected client.</summary>
    [ClientRpc]
    public void SetSpawnPositionClientRpc(Vector3 position, Quaternion rotation) {
        GetComponent<NavMeshAgent>().enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        GetComponent<NavMeshAgent>().enabled = true;
    }

    /// <summary>
    /// Moves the ghost across its current authored Off-Mesh Link using a simple
    /// arc, then hands movement back to the NavMeshAgent at the far endpoint.
    /// </summary>
    private IEnumerator TraverseCurrentNavMeshLink() {
        traversingNavMeshLink = true;
        jumpingLink.Value = true;
        agent.isStopped = true;

        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = link.startPos;
        Vector3 end = link.endPos;
        float duration = Mathf.Max(0.1f, linkJumpDuration);
        float elapsed = 0f;

        while(elapsed < duration) {
            float t = elapsed / duration;
            cachedTransform.position = Vector3.Lerp(start, end, t) +
                Vector3.up * (4f * linkJumpHeight * t * (1f - t));

            Vector3 direction = end - start;
            direction.y = 0f;
            if(direction.sqrMagnitude > 0.001f)
                cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation,
                    Quaternion.LookRotation(direction), Time.deltaTime * 8f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cachedTransform.position = end;
        agent.CompleteOffMeshLink();
        agent.isStopped = false;
        jumpingLink.Value = false;
        traversingNavMeshLink = false;
    }

    /// <summary>Applies the replicated jump state to one of the ghost's animators.</summary>
    private void SetLinkJumpAnimation(Animator targetAnimator) {
        if(targetAnimator != null && targetAnimator.gameObject.activeInHierarchy &&
            !string.IsNullOrEmpty(linkJumpAnimatorParameter))
            targetAnimator.SetBool(linkJumpAnimatorParameter, jumpingLink.Value);
    }

    /// <summary>Rebuild the server's cache of connected player objects.</summary>
    private void RefreshPlayerList() {
        listOfPlayers.Clear();

        foreach(var client in NetworkManager.Singleton.ConnectedClientsList) {
            if(client.PlayerObject != null) {
                GameObject player = client.PlayerObject.gameObject;
                if(player != null) listOfPlayers.Add(player);
            }
        }
    }

    /// <summary>Add a newly spawned player to the server's target cache.</summary>
    private void OnClientConnected(ulong clientId) {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if(playerObject != null && !listOfPlayers.Contains(playerObject.gameObject))
            listOfPlayers.Add(playerObject.gameObject);

        coneDetector.SetMyList();
    }

    /// <summary>Remove a disconnected player and refresh line-of-sight targets.</summary>
    private void OnClientDisconnected(ulong clientId) {
        listOfPlayers.RemoveAll(player => player == null ||
            (player.GetComponent<NetworkObject>() != null &&
             player.GetComponent<NetworkObject>().OwnerClientId == clientId));

        coneDetector.SetGhostsListOnDisconnects(listOfPlayers);
    }

    /// <summary>Cache components, initialize presentation, and register server callbacks.</summary>
    public override void OnNetworkSpawn() {

        agent = GetComponent<NavMeshAgent>();
        coneDetector = GetComponent<ConeLOSDetector>();
        agent.updateRotation = false;
        // The server manually traverses links so the ghost follows a visible jump arc.
        agent.autoTraverseOffMeshLink = false;
        cachedTransform = gameObject.transform;
        originalWalkSpeed = walkSpeed;
        ConfigureChaseFootstepsSource();
        ConfigureChaseMurmurSource();
        chaseFootstepsActive.OnValueChanged += OnChaseFootstepsStateChanged;
        if(IsClient && chaseFootstepsActive.Value)
            ApplyChaseFootstepsState(chaseFootstepsActive.Value);

        if(IsClient) {
            CurseGameManagerClient clientGameManager =
                FindAnyObjectByType<CurseGameManagerClient>();
            if(clientGameManager != null)
                musicSource = clientGameManager.musicSource;

            GhostVisionController vision =
                FindAnyObjectByType<GhostVisionController>();

            if(vision != null) {
                vision.ghost = transform.Find("VFX Position");
                Debug.Log("Ghost VFX reference assigned on client!");
            }
        }

        if(IsServer) {
            RefreshPlayerList();
            coneDetector.SetMyList();

            var playerLastSeemMarker = GameObject.Instantiate(playerLastSeenMarkerPrefab);
            playerLastSeemMarker.GetComponent<NetworkObject>().Spawn();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            playerLastSeen.Value = playerLastSeemMarker;
        }
        invisible.OnValueChanged += (_, newState) => {
            InvertVisibility(newState);
        };
        // Do you want the ghost to start out as invisible? If so give it some aggro charges too.
        if(IsServer) {
            invisible.Value = true;
        }
        InvertVisibility(invisible.Value);
        
    }

    /// <summary>Unregister callbacks so a despawned ghost cannot retain subscriptions.</summary>
    private void OnDestroy() {
        chaseFootstepsActive.OnValueChanged -= OnChaseFootstepsStateChanged;
        if(murmurFadeRoutine != null)
            StopCoroutine(murmurFadeRoutine);
        if(chaseFootstepsSource != null)
            chaseFootstepsSource.Stop();
        if(chaseMurmurSource != null)
            chaseMurmurSource.Stop();

        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /// <summary>Return the nearest living player, or null when no player is available.</summary>
    public GameObject ClosestPlayer() {
        return ClosestPlayer(listOfPlayers);
    }

    /// <summary>Return the active main camera, if the scene has one.</summary>
    public GameObject ClosestCamera() {
        return GameObject.FindGameObjectWithTag("MainCamera");
    }

    /// <summary>Return the nearest living player from the supplied candidate list.</summary>
    private GameObject ClosestPlayer(List<GameObject> targetSubset) {
        if(targetSubset == null || targetSubset.Count == 0)
            return null;

        GameObject nearestPlayer = null;
        float nearestDistanceSquared = float.PositiveInfinity;
        Vector3 ghostPosition = cachedTransform != null ? cachedTransform.position : transform.position;
        for(int i = 0; i < targetSubset.Count; i++) {
            GameObject candidate = targetSubset[i];
            if(candidate == null)
                continue;

            Death death = candidate.GetComponent<Death>();
            if(death == null || death.lives.Value <= 0)
                continue;

            float distanceSquared = (candidate.transform.position - ghostPosition).sqrMagnitude;
            if(distanceSquared < nearestDistanceSquared) {
                nearestDistanceSquared = distanceSquared;
                nearestPlayer = candidate;
            }
        }

        return nearestPlayer;
    }

    /// <summary>
    /// Find and cache the nearest visible living player. If no player is visible,
    /// fall back to the nearest living player in the session.
    /// </summary>
    public GameObject SeenAndClosestPlayer() {
        playersVisible.Clear();

        foreach(GameObject player in listOfPlayers) {
            Death death = player != null ? player.GetComponent<Death>() : null;
            if(death != null && death.lives.Value > 0 && coneDetector.SeeParticularTarget(player.transform)) {
                playersVisible.Add(player);
            }
        }

        // Closest player from people we can see.
        if(playersVisible.Count > 0) {
            GameObject nearestVisiblePlayer = ClosestPlayer(playersVisible);
            SetLookTargetPlayer(nearestVisiblePlayer);
            return nearestVisiblePlayer;
        }
        // Cannot see anyone. But this is the closest player in the game.
        else {
            GameObject nearestPlayer = ClosestPlayer(listOfPlayers);
            SetLookTargetPlayer(null);
            return nearestPlayer;
        }
    }

    /// <summary>Replicate the player's network object used by client-side head tracking.</summary>
    private void SetLookTargetPlayer(GameObject player) {
        if(!IsServer)
            return;

        if(player == null) {
            lookTargetPlayer.Value = default;
            return;
        }

        NetworkObject playerNetworkObject =
            player.GetComponent<NetworkObject>();

        if(playerNetworkObject == null ||
            !playerNetworkObject.IsSpawned) {
            lookTargetPlayer.Value = default;
            return;
        }

        lookTargetPlayer.Value =
            new NetworkObjectReference(playerNetworkObject);
    }

    /// <summary>Smooth the client-side look target toward the replicated player.</summary>
    private void UpdateHeadLookTarget() {
        Transform desiredTarget = defaultLookTarget;

        if(lookTargetPlayer.Value.TryGet(
                out NetworkObject targetNetworkObject)) {
            desiredTarget = targetNetworkObject.transform;
        }

        Vector3 targetPosition = desiredTarget.position;

        // Only add the head-height offset when tracking a player.
        if(desiredTarget != defaultLookTarget) {
            targetPosition += Vector3.up * lookHeightOffset;
        }

        lookAtTarget.position = Vector3.SmoothDamp(
            lookAtTarget.position,
            targetPosition,
            ref lookVelocity,
            lookSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    /// <summary>Update local presentation and advance authoritative ghost AI on the server.</summary>
    private void Update() {
        if(IsClient && IsSpawned)
            UpdateHeadLookTarget();

        UpdateAnimatorPresentation();

        if(!IsServer)
            return;

        // Link traversal is authoritative and synchronized through the network transform.
        if(allowedToMove.Value && !traversingNavMeshLink && agent.isOnOffMeshLink)
            StartCoroutine(TraverseCurrentNavMeshLink());

        if(traversingNavMeshLink)
            return;

        if(!allowedToMove.Value) {
            SetChaseFootstepsState(false);
            return;
        }

        currentTargetPlayer = SeenAndClosestPlayer();
        if(currentTargetPlayer == null) {
            SetChaseFootstepsState(false);
            return;
        }

        if(allowedToMove.Value) {

            bool ableToSeePlayer = coneDetector.aTargetVisible && 
                !currentTargetPlayer.GetComponentInChildren<PlayerMovement>().isHiding && 
                    normalAggro.Value && 
                        currentTargetPlayer.GetComponent<Death>().lives.Value > 0;

            bool playerInAttackRange = Physics.CheckSphere(cachedTransform.position, attackRange, playerLayer) && normalAggro.Value;

            // If ghost can't see any player and there's no one in melee range:
            if(!ableToSeePlayer && !playerInAttackRange) {
                if(chaseMeter == 100f || invisible.Value || !normalAggro.Value) {
                    if(currentMode == Mode.Chasing) {
                        SetChaseFootstepsState(false);
                        SetChaseMusicClientRpc(false);
                        hasPatrolDestination = false;
                        aggressionCharges.Value--;
                        if(aggressionCharges.Value < 0) aggressionCharges.Value = 0;
                    }
                    ModePatrolling();
                    if(currentChaseDuration > longestChaseDuration) {
                        longestChaseDuration = currentChaseDuration;
                        //deathScript.GetComponent<CurseGameManager>().longestChase = (int)longestChaseDuration;
                        currentChaseDuration = 0f;
                    }
                }
                else {
                    currentChaseDuration += 1 * Time.deltaTime;
                    chaseMeter += 2 * Time.deltaTime;
                    if(chaseMeter > 100f) chaseMeter = 100f;
                    if(chaseMeter >= 100f) hasPatrolDestination = false;

                    if(currentMode == Mode.Chasing) ModeChase();
                }
            }

            // If I'm not invis and I see you and you're NOT in melee range
            else if(normalAggro.Value && !invisible.Value && ableToSeePlayer && !playerInAttackRange) {
                if(currentMode != Mode.Chasing && !waitingForScream) {
                    if(coneDetector.visibilityOverride) chaseMeter = 30f;
                    else chaseMeter = 80f;

                    Scream();
                    SetChaseMusicClientRpc(true);

                    if(playerLastSeen.Value.TryGet(out NetworkObject networkObject)) {
                        networkObject.transform.position = currentTargetPlayer.transform.position;
                    }
                }
                else if(currentMode == Mode.Chasing) {
                    chaseMeter -= 1f * Time.deltaTime;
                    if(chaseMeter < 0f) chaseMeter = 0f;
                    if(playerLastSeen.Value.TryGet(out NetworkObject networkObject)) {
                        networkObject.transform.position = currentTargetPlayer.transform.position;
                    }
                }

                if(!waitingForScream) ModeChase();
            }

            // If I'm not invis and I see you and you're within melee range //OR if I'm not invis and I can't see you but you ARE in melee range AND hiding
            else if(normalAggro.Value && ((!invisible.Value && playerInAttackRange && chaseMeter != 100f))) {
                AttackPlayer(currentTargetPlayer);
                StartCoroutine(DeAggroTimer());
            }

            if(waitingForScream) {
                Vector3 direction = currentTargetPlayer.transform.position - cachedTransform.position;
                direction.y = 0f; // ignore vertical difference

                if(direction.sqrMagnitude < 0.0001f)
                    return;

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                cachedTransform.rotation = Quaternion.Slerp(
                    cachedTransform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }


            #region Angular Rotation
            Vector3 desired = agent.desiredVelocity;

            Vector3 repulsion = Vector3.zero;
            float checkDistance = 1.2f;
            float strength = 0f;

            RaycastHit hit;

            // Left
            if(Physics.Raycast(cachedTransform.position, -cachedTransform.right, out hit, checkDistance)) {
                repulsion += hit.normal;
            }

            // Right
            if(Physics.Raycast(cachedTransform.position, cachedTransform.right, out hit, checkDistance)) {
                repulsion += hit.normal;
            }

            Vector3 finalVelocity = desired + repulsion * strength;
            finalVelocity.y = 0;

            agent.velocity = Vector3.Lerp(
                agent.velocity,
                finalVelocity,
                Time.deltaTime * 5f
            );
            if(finalVelocity.sqrMagnitude > 0.01f) {
                Quaternion rot = Quaternion.LookRotation(finalVelocity);
                cachedTransform.rotation = Quaternion.Slerp(
                    cachedTransform.rotation,
                    rot,
                    Time.deltaTime * 6f
                );
            }

            veloNetwork.Value = agent.velocity.magnitude;
            #endregion

        }

    }

    /// <summary>Apply replicated movement state to the ghost's visible animators.</summary>
    private void UpdateAnimatorPresentation() {
        if(allowedToMove.Value) {
            SetAnimatorVelocity(animator);
            SetAnimatorVelocity(shadowAnimator);
            SetAnimatorVelocity(fulgorShadowAnimator);
        }

        SetLinkJumpAnimation(animator);
        SetLinkJumpAnimation(shadowAnimator);
        SetLinkJumpAnimation(fulgorShadowAnimator);
    }

    /// <summary>Update one animator's movement parameter when it is active.</summary>
    private void SetAnimatorVelocity(Animator targetAnimator) {
        if(targetAnimator != null && targetAnimator.gameObject.activeInHierarchy)
            targetAnimator.SetFloat("Velocity", veloNetwork.Value);
    }

    // A cooldown that occurs after attacking a player.
    /// <summary>Temporarily suppress normal aggression after a successful attack.</summary>
    private IEnumerator DeAggroTimer() {
        normalAggro.Value = false;
        float prevWalkSpeed = walkSpeed;
        walkSpeed = 5f;
        yield return new WaitForSeconds(deAggroCooldown);
        walkSpeed = prevWalkSpeed;
        normalAggro.Value = true;
    }

    /// <summary>Start the replicated scream presentation and pause server navigation.</summary>
    private void Scream() {
        ScreamClientRpc();
        StartCoroutine(ScreamAnimTimer());
    }

    /// <summary>Play the scream sound and synchronized animation on all clients.</summary>
    [ClientRpc]
    private void ScreamClientRpc() {
        if(monsterSource != null && screamClips != null && screamClips.Length > 0) {
            monsterSource.pitch = Random.Range(.85f, 1.2f);
            monsterSource.PlayOneShot(screamClips[Random.Range(0, screamClips.Length)]);
        }
        waitingForScream = true;
        if(animator != null) animator.Play("Scream");
        if(shadowAnimator != null) shadowAnimator.Play("Scream");
        if(fulgorShadowAnimator != null) fulgorShadowAnimator.Play("Scream");
        StartCoroutine(ClearScreamWaitFlag());
    }

    /// <summary>Clear the local scream state after its authored animation duration.</summary>
    private IEnumerator ClearScreamWaitFlag() {
        yield return new WaitForSeconds(1.533f);
        waitingForScream = false;
    }

    /// <summary>Apply the chase or patrol soundtrack on each connected client.</summary>
    [ClientRpc]
    private void SetChaseMusicClientRpc(bool chasing) {
        CurseGameManagerClient clientGameManager =
            FindAnyObjectByType<CurseGameManagerClient>();
        if(clientGameManager != null)
            musicSource = clientGameManager.musicSource;

        if(musicSource != null) {
            AudioController.FadeToAnother(this, musicSource,
                chasing ? 0.3f : 4f,
                chasing ? chaseMusicClip : normalMusicClip, 0.1f);
        }
    }

    /// <summary>Configure this client's source as a local 3D looping emitter.</summary>
    private void ConfigureChaseFootstepsSource() {
        if(chaseFootstepsSource == null)
            return;

        chaseFootstepsSource.playOnAwake = false;
        chaseFootstepsSource.loop = true;
        chaseFootstepsSource.spatialBlend = 1f;
        chaseFootstepsSource.maxDistance = chaseFootstepsMaxDistance;
        chaseFootstepsSource.Stop();
    }

    /// <summary>Update the authoritative chase state used by every client.</summary>
    private void SetChaseFootstepsState(bool isActive) {
        if(!IsServer || chaseFootstepsActive.Value == isActive)
            return;

        chaseFootstepsActive.Value = isActive;
        // Apply directly on a host as well; the applied-state guard prevents
        // the NetworkVariable callback from starting the same fade twice.
        if(IsClient)
            ApplyChaseFootstepsState(isActive);
    }

    /// <summary>Apply a replicated chase-state change to this client's source.</summary>
    private void OnChaseFootstepsStateChanged(bool previousState, bool newState) {
        ApplyChaseFootstepsState(newState);
    }

    /// <summary>Start or stop the local footstep loop without fading.</summary>
    private void ApplyChaseFootstepsState(bool isActive) {
        if(!IsClient || appliedChaseAudioState == isActive)
            return;
        appliedChaseAudioState = isActive;

        if(chaseFootstepsSource != null && chaseFootstepsSource.clip != null) {
            if(isActive) {
                if(!chaseFootstepsSource.isPlaying)
                    chaseFootstepsSource.Play();
            }
            else if(chaseFootstepsSource.isPlaying) {
                chaseFootstepsSource.Stop();
            }
        }

        ApplyChaseMurmurState(isActive);
    }

    /// <summary>Cache the authored full volume and start the murmur track silent.</summary>
    private void ConfigureChaseMurmurSource() {
        if(chaseMurmurSource == null)
            return;

        murmurTargetVolume = chaseMurmurSource.volume;
        chaseMurmurSource.playOnAwake = false;
        chaseMurmurSource.loop = true;
        chaseMurmurSource.Stop();
        chaseMurmurSource.volume = 0f;
    }

    /// <summary>Fade the murmur track in or out, cancelling the previous fade first.</summary>
    private void ApplyChaseMurmurState(bool isActive) {
        if(!IsClient || chaseMurmurSource == null || chaseMurmurSource.clip == null)
            return;

        if(murmurFadeRoutine != null)
            StopCoroutine(murmurFadeRoutine);

        murmurFadeRoutine = isActive
            ? AudioController.FadeInAudio(this, chaseMurmurSource,
                murmurFadeInDuration, murmurTargetVolume)
            : AudioController.FadeOutAudio(this, chaseMurmurSource,
                murmurFadeOutDuration);
    }

    /// <summary>Stop server navigation during the scream, then resume the chase.</summary>
    private IEnumerator ScreamAnimTimer() {
        float priorSpeed = agent.speed;
        agent.speed = 0;
        yield return new WaitForSeconds(1.533f);
        agent.speed = priorSpeed;
        waitingForScream = false;
        ModeChase();
    }

    /// <summary>Choose and follow a patrol point, periodically updating visibility.</summary>
    private void ModePatrolling() {
        currentMode = Mode.Patrolling;

        // This timer is independent of waypoint selection, so charges can
        // change visibility while the ghost is already walking its patrol.
        if(Time.time >= nextManifestationCheckTime) {
            nextManifestationCheckTime = Time.time + manifestationCheckInterval;
            UpdatePatrolVisibilityFromCharges();
        }

        if(patrolIsPaused) {
            agent.SetDestination(cachedTransform.position);
            return;
        }

        if(!hasPatrolDestination) {
            Vector3 point = RandomNavSphere(cachedTransform.position, walkPointRange, groundLayer);
            
            if(point != cachedTransform.position) {
                walkPoint = point;
                hasPatrolDestination = true;

                if(!invisible.Value && Random.Range(0, pauseChance) == 0) {
                    StartCoroutine(PausingPatrol());
                }

            }
        }
        else {
            agent.SetDestination(walkPoint);
        }
        Vector3 distanceToWalkPoint = cachedTransform.position - walkPoint;
        agent.speed = walkSpeed;
        if(distanceToWalkPoint.magnitude < 1f) {
            hasPatrolDestination = false;
        }
    }

    /// <summary>Apply charge-driven visibility changes while the ghost is patrolling.</summary>
    private void UpdatePatrolVisibilityFromCharges() {
        if(coneDetector.visibilityOverride)
            return;

        GameObject nearest = ClosestPlayer();
        float distance = nearest != null
            ? Vector3.Distance(nearest.transform.position, cachedTransform.position)
            : float.MaxValue;

        if(invisible.Value && aggressionCharges.Value > 0 && distance > walkPointRange * 0.5f) {
            InvertVisibilityServerRpc();
            if(distance < walkPointRange) {
                PlayerMovement movement = nearest.GetComponentInChildren<PlayerMovement>();
                if(movement != null && movement.lanternReference != null)
                    movement.lanternReference.StartFlickerPeriod(2f);
            }
        }
        else if(!invisible.Value && aggressionCharges.Value <= 0) {
            InvertVisibilityServerRpc();
        }
    }

    /// <summary>Pause briefly at a patrol point before resuming movement.</summary>
    private IEnumerator PausingPatrol() {
        patrolIsPaused = true;
        agent.ResetPath();
        yield return new WaitForSeconds(Random.Range(1, 14));
        patrolIsPaused = false;
    }

    /// <summary>Set chase navigation toward the current target or its last seen position.</summary>
    private void ModeChase() {
        if(currentTargetPlayer == null)
            return;

        SetChaseFootstepsState(true);
        currentMode = Mode.Chasing;

        if(currentTargetPlayer.GetComponentInChildren<PlayerMovement>().isHiding) {
            if(playerLastSeen.Value.TryGet(out NetworkObject networkObject)) {
                agent.SetDestination(networkObject.transform.position);
            }
        }
        else {
            agent.SetDestination(currentTargetPlayer.transform.position);
        }

        if(currentTargetPlayer.GetComponentInChildren<PlayerMovement>().isHiding) {
            if(playerLastSeen.Value.TryGet(out NetworkObject networkObject)) {
                walkPoint = networkObject.transform.position;
            }
        }
        else {
            walkPoint = currentTargetPlayer.transform.position;
        }

        hasPatrolDestination = true;
        agent.speed = runSpeed;
        agent.isStopped = false;
    }

    /// <summary>Add one server-authoritative aggression charge.</summary>
    public void IncreaseCharges() {
        if(IsServer) {
            aggressionCharges.Value += 1;
            return;
        }

        if(IsSpawned)
            RequestChargeIncreaseServerRpc();
    }

    /// <summary>Route client-side prop interactions through the server authority.</summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestChargeIncreaseServerRpc() {
        aggressionCharges.Value += 1;
    }

    /// <summary>Toggle visibility on the server and replicate the state.</summary>
    [ServerRpc]
    public void InvertVisibilityServerRpc() {
        invisible.Value = !invisible.Value;
    }

    /// <summary>Apply visibility changes to ghost renderers, audio, aura, and effects.</summary>
    public void InvertVisibility(bool invisState) {
        if(invisState) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.gameObject.layer =LayerMask.NameToLayer("Afterlife");
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(false);
            }

            walkSpeed = invisSpeed;
        }
        else {
            walkSpeed = originalWalkSpeed;
        }


        if(geistAura.Value && !invisState) geistlightAura.Play();
        else if(geistAura.Value) geistlightAura.Stop();
        ambientSource.volume = invisState ? 0 : 1;
        StartCoroutine(ShadowAnimTimer(invisState));
    }

    /// <summary>Play the shadow transition and delay reactivating the noise trigger.</summary>
    private IEnumerator ShadowAnimTimer(bool invisState) {
        shadow.SetActive(true);
        if(!invisState) shadow.GetComponent<Animator>().Play("ShadowAnim 0");
        yield return new WaitForSeconds(1f);
        shadow.SetActive(false);

        if(!invisState) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(true);
            }

        }
        if(invisState) yield return new WaitForSeconds(15f);
        else yield return new WaitForSeconds(2f);
        paranormalSounds.SetActive(invisState);
    }

    /// <summary>Reveal the mesh and horns immediately for a jumpscare sequence.</summary>
    public void MakeVisible() {
        if(invisible.Value) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.gameObject.layer = LayerMask.NameToLayer("Afterlife");
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(true);
            }
        }
    }

    /// <summary>Perform a melee attack and apply damage if the ray hits the target.</summary>
    private void AttackPlayer(GameObject playerAttacked) {
        agent.SetDestination(cachedTransform.position);
        GameObject targetPlayerAttacked = playerAttacked;
        if(!playerAttacked.GetComponent<Death>()) {
            targetPlayerAttacked = playerAttacked.transform.parent.gameObject;
        }

        if(!attackOnCooldown) {
            cachedTransform.LookAt(targetPlayerAttacked.transform.position);
            attackOnCooldown = true;
            animator.SetBool("Attack", true);

            Invoke(nameof(ResetAttack), timeBetweenAttacks);

            RaycastHit hit;
            if(Physics.Raycast(cachedTransform.position, cachedTransform.forward, out hit, attackRange + 4)) {
                animator.Play("Attack" + Random.Range(1, 4).ToString());
                monsterSource.pitch = 1;
                monsterSource.PlayOneShot(attackClip, 0.5f);
                Death targetDeath = targetPlayerAttacked.GetComponent<Death>();
                if(hit.collider.GetComponentInParent<Death>() == targetDeath) {
                    // A nonfatal ghost hit triggers the shared ragdoll reaction before life loss.
                    if(targetDeath.lives.Value > 1) {
                        RagdollController ragdoll = targetPlayerAttacked.GetComponent<RagdollController>();
                        if(ragdoll != null) {
                            Vector3 worldImpulse = cachedTransform.TransformDirection(ghostRagdollImpulse);
                            ragdoll.TriggerRagdollForAllClients(worldImpulse);
                        }
                    }
                    targetDeath.LoseLife(true);
                }
            }
            aggressionCharges.Value--;
            if(aggressionCharges.Value < 0) aggressionCharges.Value = 0;
        }
        else {
            animator.SetBool("Attack", false);
        }

    }

    /// <summary>Restore attack animations when the attack cooldown expires.</summary>
    private void ResetAttack() {
        attackOnCooldown = false;
        animator.SetBool("Attack", false);
        shadowAnimator.SetBool("Attack", false);
        fulgorShadowAnimator.SetBool("Attack", false);
    }

    /// <summary>Apply damage and schedule the ghost's death sequence when defeated.</summary>
    public void TakeDamage(float damage) {
        health -= damage;
        hitEffect.Play();
        if(health <= 0) {
            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    /// <summary>Begin the ghost death animation and remove it after it completes.</summary>
    private void DestroyEnemy() {
        StartCoroutine(DestroyEnemyCoroutine());
    }

    /// <summary>Play death animations before destroying the networked ghost object.</summary>
    private IEnumerator DestroyEnemyCoroutine() {
        animator.SetBool("Dead", true);
        shadowAnimator.SetBool("Dead", true);
        fulgorShadowAnimator.SetBool("Dead", true);
        yield return new WaitForSeconds(1.8f);
        Destroy(gameObject);
    }

    /// <summary>Draw the selected ghost's attack and patrol ranges in the editor.</summary>
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkPointRange);
    }

    /// <summary>Sample a reachable patrol destination around the supplied origin.</summary>
    public Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 samplePosition = origin + Random.insideUnitSphere * dist;
        if(!NavMesh.SamplePosition(samplePosition, out NavMeshHit navHit, dist, NavMesh.AllAreas))
            return origin;

        NavMeshPath path = new NavMeshPath();
        if(agent == null || !agent.CalculatePath(navHit.position, path) ||
            path.status != NavMeshPathStatus.PathComplete)
            return origin;

        return navHit.position;
    }

}

