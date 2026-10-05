using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enables and disables the physics rig created with Unity's Ragdoll Wizard.
/// The test hotkeys are intentionally local-only and can be disabled in the Inspector.
/// </summary>
public class RagdollController : NetworkBehaviour {
    [Header("Step 2 local test")]
    [SerializeField] private bool enableTestHotkeys = true;
    [SerializeField] private KeyCode ragdollTestKey = KeyCode.F8;
    [SerializeField] private KeyCode recoverTestKey = KeyCode.F9;

    [Header("Ragdoll view")]
    [SerializeField] private Vector3 ragdollCameraRotationOffset;
    [SerializeField] private float automaticRecoveryDelay = 2f;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Animator bodyAnimator;
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private Collider[] movementObjectColliders;
    private bool[] movementObjectColliderStates;
    private MouseLook mouseLook;
    private Rigidbody pelvisBody;
    private Transform ragdollHead;
    private CameraFollow cameraFollow;
    private Transform previousCameraTarget;
    private Transform previousCameraRotationTarget;
    private Quaternion previousCameraRotation;
    private Vector3 previousCameraRotationOffset;
    private bool cameraStateCaptured;
    private ToolController toolController;
    private Death death;
    private readonly Dictionary<Renderer, bool> hiddenRendererStates = new Dictionary<Renderer, bool>();
    private Transform movementRoot;
    private Vector3 pelvisLocalPositionAtStart;
    private Vector3 lastMovementRootPosition;
    private Vector3 observedMovementVelocity;
    private Vector3 queuedAttackImpulse;
    private bool hasMovementRootSample;
    private bool attackImpulsePending;
    private bool movementWasAlive;
    private bool animatorWasEnabled;
    private bool controllerWasEnabled;
    private bool mouseLookWasAlive;
    private bool mouseLookStateCaptured;
    private bool isRagdolled;
    private bool initialized;

    public bool IsRagdolled => isRagdolled;

    private void Awake() {
        InitializeRagdollRig();
    }

    public override void OnNetworkSpawn() {
        InitializeRagdollRig();
    }

    private void Update() {
        UpdateObservedMovementVelocity();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(!enableTestHotkeys || !IsSpawned || !IsOwner) return;

        if(Input.GetKeyDown(ragdollTestKey)) {
            StartRagdoll();
        }

        if(Input.GetKeyDown(recoverTestKey)) {
            RecoverFromRagdoll();
        }
#endif
    }

    private void FixedUpdate() {
        if(!attackImpulsePending) return;
        attackImpulsePending = false;

        if(!isRagdolled) return;

        float totalMass = 0f;
        for(int i = 0; i < ragdollBodies.Length; i++) {
            Rigidbody body = ragdollBodies[i];
            if(body != null && !body.isKinematic) totalMass += body.mass;
        }
        if(totalMass <= 0f) {
            Debug.LogError("Ragdoll attack impulse was received but no dynamic ragdoll bodies were available.", this);
            return;
        }

        // Spread the same total impulse over the connected rig. Giving every body the same
        // delta velocity prevents the joints from absorbing most of a single-body shove.
        Vector3 deltaVelocity = queuedAttackImpulse / totalMass;
        for(int i = 0; i < ragdollBodies.Length; i++) {
            Rigidbody body = ragdollBodies[i];
            if(body == null || body.isKinematic) continue;
            body.velocity += deltaVelocity;
            body.WakeUp();
        }
        Debug.Log($"Distributed ghost ragdoll impulse {queuedAttackImpulse} across {totalMass:0.##} kg (velocity change {deltaVelocity}).", this);
    }

    private void InitializeRagdollRig() {
        if(initialized) return;
        initialized = true;

        CharacterJoint[] joints = GetComponentsInChildren<CharacterJoint>(true);
        HashSet<Rigidbody> bodySet = new HashSet<Rigidbody>();
        for(int i = 0; i < joints.Length; i++) {
            Rigidbody jointBody = joints[i].GetComponent<Rigidbody>();
            if(jointBody != null) bodySet.Add(jointBody);
            if(joints[i].connectedBody != null) bodySet.Add(joints[i].connectedBody);
        }

        if(bodySet.Count == 0) {
            Debug.LogError("RagdollController could not find the CharacterJoint/Rigidbody rig. " +
                "Create the ragdoll with Unity's Ragdoll Wizard first.", this);
            return;
        }

        ragdollBodies = new Rigidbody[bodySet.Count];
        bodySet.CopyTo(ragdollBodies);

        List<Collider> colliderList = new List<Collider>();
        for(int i = 0; i < ragdollBodies.Length; i++) {
            colliderList.AddRange(ragdollBodies[i].GetComponents<Collider>());
            if(pelvisBody == null && IsRootRagdollBody(ragdollBodies[i])) {
                pelvisBody = ragdollBodies[i];
            }
        }
        ragdollColliders = colliderList.ToArray();

        playerMovement = GetComponentInChildren<PlayerMovement>(true);
        characterController = GetComponentInChildren<CharacterController>(true);
        CacheMovementObjectColliders();
        toolController = GetComponent<ToolController>();
        death = GetComponent<Death>();
        movementRoot = playerMovement != null ? playerMovement.transform.parent : transform;
        bodyAnimator = FindAnimatorContainingRagdoll();
        ragdollHead = FindRagdollHead();
        FindMouseLook();

        if(pelvisBody == null) pelvisBody = ragdollBodies[0];
        if(movementRoot != null) {
            pelvisLocalPositionAtStart = movementRoot.InverseTransformPoint(pelvisBody.position);
            lastMovementRootPosition = movementRoot.position;
            hasMovementRootSample = true;
        }

        SetRagdollPhysicsEnabled(false);
    }

    private static bool IsRootRagdollBody(Rigidbody body) {
        // The pelvis is the rig root: it has a Rigidbody but no CharacterJoint of its own.
        // Child joints point back to it through connectedBody.
        return body.GetComponent<CharacterJoint>() == null;
    }

    private Animator FindAnimatorContainingRagdoll() {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator best = null;
        int bestDepth = -1;

        for(int i = 0; i < animators.Length; i++) {
            bool containsAllBodies = true;
            for(int bodyIndex = 0; bodyIndex < ragdollBodies.Length; bodyIndex++) {
                Transform bodyTransform = ragdollBodies[bodyIndex].transform;
                if(bodyTransform != animators[i].transform && !bodyTransform.IsChildOf(animators[i].transform)) {
                    containsAllBodies = false;
                    break;
                }
            }

            if(!containsAllBodies) continue;
            int depth = 0;
            Transform parent = animators[i].transform;
            while(parent != null) {
                depth++;
                parent = parent.parent;
            }
            if(depth > bestDepth) {
                best = animators[i];
                bestDepth = depth;
            }
        }

        return best;
    }

    /// <summary>Starts a local ragdoll. Gameplay callers will be added in a later step.</summary>
    public void StartRagdoll() {
        StartRagdoll(Vector3.zero);
    }

    private void StartRagdoll(Vector3 attackImpulse) {
        if(isRagdolled || ragdollBodies == null || ragdollBodies.Length == 0) return;

        Vector3 inheritedVelocity = GetInheritedVelocity();

        if(movementRoot != null) {
            pelvisLocalPositionAtStart = movementRoot.InverseTransformPoint(pelvisBody.position);
        }

        movementWasAlive = playerMovement == null || playerMovement.playerAlive;
        animatorWasEnabled = bodyAnimator != null && bodyAnimator.enabled;
        controllerWasEnabled = characterController != null && characterController.enabled;
        FindMouseLook();
        mouseLookStateCaptured = mouseLook != null;
        if(mouseLookStateCaptured) mouseLookWasAlive = mouseLook.playerAlive;

        CaptureCameraState();
        HideFirstPersonRenderers();

        if(playerMovement != null) playerMovement.playerAlive = false;
        if(mouseLook != null) mouseLook.playerAlive = false;
        if(characterController != null) characterController.enabled = false;
        SetMovementObjectCollidersEnabled(false);
        if(bodyAnimator != null) bodyAnimator.enabled = false;

        SetRagdollPhysicsEnabled(true);
        ApplyInheritedVelocity(inheritedVelocity);
        if(ragdollBodies != null && attackImpulse.sqrMagnitude > 0f) {
            queuedAttackImpulse = attackImpulse;
            attackImpulsePending = true;
        }
        if(cameraFollow != null && ragdollHead != null) {
            cameraFollow.SetTarget(ragdollHead);
            cameraFollow.rotationTarget = ragdollHead;
            cameraFollow.rotationOffset = ragdollCameraRotationOffset;
        }
        isRagdolled = true;
        Debug.Log("Ragdoll test started. Press F9 to recover.", this);
    }

    private void ApplyInheritedVelocity(Vector3 velocity) {
        for(int i = 0; i < ragdollBodies.Length; i++) {
            Rigidbody body = ragdollBodies[i];
            if(body == null) continue;
            body.velocity = velocity;
            body.WakeUp();
        }
    }

    /// <summary>Requests a short ragdoll reaction on every client for a server-approved event.</summary>
    public void TriggerRagdollForAllClients(Vector3 attackImpulse) {
        if(!IsServer || !IsSpawned || isRagdolled) return;
        Debug.Log($"Sending ghost ragdoll impulse {attackImpulse} to clients.", this);
        TriggerRagdollClientRpc(automaticRecoveryDelay, attackImpulse);
    }

    [ClientRpc]
    private void TriggerRagdollClientRpc(float recoveryDelay, Vector3 attackImpulse) {
        if(isRagdolled) return;
        Debug.Log($"Received ghost ragdoll impulse {attackImpulse}.", this);
        StartRagdoll(attackImpulse);
        if(isRagdolled) StartCoroutine(RecoverAfterDelay(recoveryDelay));
    }

    private System.Collections.IEnumerator RecoverAfterDelay(float delay) {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        RecoverFromRagdoll();
    }

    private Vector3 GetInheritedVelocity() {
        if(IsOwner && playerMovement != null) return playerMovement.GetCurrentVelocity();
        return observedMovementVelocity;
    }

    private void UpdateObservedMovementVelocity() {
        if(movementRoot == null) return;

        Vector3 currentPosition = movementRoot.position;
        if(hasMovementRootSample && Time.deltaTime > 0f) {
            observedMovementVelocity = (currentPosition - lastMovementRootPosition) / Time.deltaTime;
        }
        lastMovementRootPosition = currentPosition;
        hasMovementRootSample = true;
    }

    /// <summary>Stops the local ragdoll and restores the saved movement/animation state.</summary>
    public void RecoverFromRagdoll() {
        if(!isRagdolled) return;

        if(movementRoot != null && pelvisBody != null) {
            movementRoot.position = pelvisBody.position - movementRoot.TransformVector(pelvisLocalPositionAtStart);
        }

        SetRagdollPhysicsEnabled(false);
        PlaceCharacterControllerAboveGround();
        Physics.SyncTransforms();

        RestoreFirstPersonRenderers();
        RestoreCameraState();

        if(playerMovement != null) playerMovement.ResetMovementVelocity();
        if(bodyAnimator != null) bodyAnimator.enabled = animatorWasEnabled;
        if(characterController != null) characterController.enabled = controllerWasEnabled;
        SetMovementObjectCollidersEnabled(true);
        if(playerMovement != null) playerMovement.playerAlive = movementWasAlive;
        if(mouseLook != null && mouseLookStateCaptured) mouseLook.playerAlive = mouseLookWasAlive;

        isRagdolled = false;
        Debug.Log("Ragdoll test recovered. Press F8 to ragdoll again.", this);
    }

    private void FindMouseLook() {
        if(mouseLook != null) return;
        PlayerHandler playerHandler = GetComponent<PlayerHandler>();
        if(playerHandler != null && playerHandler.cameraReference != null) {
            mouseLook = playerHandler.cameraReference.GetComponent<MouseLook>();
        }
    }

    private Transform FindRagdollHead() {
        if(bodyAnimator != null && bodyAnimator.isHuman) {
            Transform humanoidHead = bodyAnimator.GetBoneTransform(HumanBodyBones.Head);
            if(humanoidHead != null) return humanoidHead;
        }

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        for(int i = 0; i < allTransforms.Length; i++) {
            if(allTransforms[i].name.ToLowerInvariant().Contains("head")) return allTransforms[i];
        }
        return null;
    }

    private void CaptureCameraState() {
        PlayerHandler playerHandler = GetComponent<PlayerHandler>();
        cameraFollow = playerHandler != null ? playerHandler.cameraReference : null;
        cameraStateCaptured = cameraFollow != null;
        if(!cameraStateCaptured) return;

        previousCameraTarget = cameraFollow.target;
        previousCameraRotationTarget = cameraFollow.rotationTarget;
        previousCameraRotation = cameraFollow.transform.rotation;
        previousCameraRotationOffset = cameraFollow.rotationOffset;
    }

    private void RestoreCameraState() {
        if(!cameraStateCaptured || cameraFollow == null) return;
        cameraFollow.SetTarget(previousCameraTarget);
        cameraFollow.rotationTarget = previousCameraRotationTarget;
        cameraFollow.rotationOffset = previousCameraRotationOffset;
        cameraFollow.transform.rotation = previousCameraRotation;
        cameraStateCaptured = false;
    }

    private void HideFirstPersonRenderers() {
        hiddenRendererStates.Clear();
        if(death != null && death.playerArmsAnimator != null) {
            HideRenderersUnder(death.playerArmsAnimator.transform);
        }

        if(toolController != null && toolController.playerItemMeshes != null) {
            for(int i = 0; i < toolController.playerItemMeshes.Length; i++) {
                GameObject item = toolController.playerItemMeshes[i];
                if(item != null) HideRenderersUnder(item.transform);
            }
        }
    }

    private void HideRenderersUnder(Transform parent) {
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
        for(int i = 0; i < renderers.Length; i++) {
            Renderer renderer = renderers[i];
            if(renderer == null || hiddenRendererStates.ContainsKey(renderer)) continue;
            hiddenRendererStates.Add(renderer, renderer.enabled);
            renderer.enabled = false;
        }
    }

    private void RestoreFirstPersonRenderers() {
        foreach(KeyValuePair<Renderer, bool> entry in hiddenRendererStates) {
            if(entry.Key != null) entry.Key.enabled = entry.Value;
        }
        hiddenRendererStates.Clear();
    }

    private void CacheMovementObjectColliders() {
        if(playerMovement == null) return;

        Collider[] colliders = playerMovement.GetComponents<Collider>();
        HashSet<Collider> ragdollColliderSet = new HashSet<Collider>(ragdollColliders);
        List<Collider> movementColliders = new List<Collider>();
        for(int i = 0; i < colliders.Length; i++) {
            if(colliders[i] != null && !ragdollColliderSet.Contains(colliders[i])) {
                movementColliders.Add(colliders[i]);
            }
        }
        movementObjectColliders = movementColliders.ToArray();
        movementObjectColliderStates = new bool[movementObjectColliders.Length];
    }

    private void SetMovementObjectCollidersEnabled(bool enabled) {
        if(movementObjectColliders == null) return;

        for(int i = 0; i < movementObjectColliders.Length; i++) {
            Collider collider = movementObjectColliders[i];
            if(collider == null) continue;
            if(!enabled) movementObjectColliderStates[i] = collider.enabled;
            collider.enabled = enabled && movementObjectColliderStates[i];
        }
    }

    private void PlaceCharacterControllerAboveGround() {
        if(characterController == null || movementRoot == null) return;

        Vector3 center = characterController.transform.TransformPoint(characterController.center);
        float scaledHeight = characterController.height * characterController.transform.lossyScale.y;
        Vector3 rayOrigin = center + Vector3.up * (scaledHeight * 0.5f + 1f);
        float rayDistance = scaledHeight + 4f;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for(int i = 0; i < hits.Length; i++) {
            Transform hitTransform = hits[i].collider.transform;
            if(hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if(Vector3.Dot(hits[i].normal, Vector3.up) < 0.25f) continue;

            float bottomY = center.y - scaledHeight * 0.5f;
            float desiredBottomY = hits[i].point.y + characterController.skinWidth;
            movementRoot.position += Vector3.up * (desiredBottomY - bottomY);
            return;
        }
    }

    private void SetRagdollPhysicsEnabled(bool enabled) {
        if(ragdollBodies != null) {
            for(int i = 0; i < ragdollBodies.Length; i++) {
                Rigidbody body = ragdollBodies[i];
                if(body == null) continue;
                if(!enabled) {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = !enabled;
                if(enabled) body.WakeUp();
            }
        }

        if(ragdollColliders != null) {
            for(int i = 0; i < ragdollColliders.Length; i++) {
                if(ragdollColliders[i] != null) ragdollColliders[i].enabled = enabled;
            }
        }
    }
}
