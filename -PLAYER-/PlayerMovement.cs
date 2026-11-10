using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerMovement : NetworkBehaviour {
    
    public GroundChecker groundCheckerScript;

    public float speed = 12f, staminaRecoveryRate = 1f, staminaDuration = 40f, jumpHeight = 1;



    public float crouchHeight = -0.696f, currentHeight = 0f, gravityStrength = -22f;

    [SerializeField]
    private bool shouldBeSlowed = false, sliding = false;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false,
        isCrouched = false, isHiding = false, isTired = false, playerAlive = true;

    [SerializeField]
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float amountCrouchSpots = 0f;
    public LayerMask groundMask;

    [SerializeField] Animator playerAnimator, armsAnimator;

    public ConeLOSDetector enemyVisionScript;

    public LightFlickerNonNetworked lanternReference;


    [SerializeField] private PlayerHandler playerHandlerScript;
    [SerializeField] private Transform headTransform;

    [SerializeField] private ParticleSystem feathersVFXA, feathersVFXB;



    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float groundedStickForce = 8f;

    // =================
    // WALL HOP VARIABLES
    // =================
    [SerializeField] private float wallCastRadius = 0.18f;
    [SerializeField] private float wallCastDistance = 0.35f;
    [SerializeField] private float wallCastHeight = 1.1f;
    [SerializeField] private float wallSlideDuration = 1f;
    [SerializeField] private float wallSlideSpeed = 1.25f;
    [SerializeField] private float wallHopOutwardSpeed = 6f;
    [SerializeField] private float wallHopForwardSpeed = 6f;
    [SerializeField] private float sideWallHopUpwardMultiplier = 1f;
    [SerializeField] private float forwardWallHopUpwardMultiplier = 1f;
    [SerializeField] private float maximumWallNormalY = 0.25f;
    [HideInInspector] public bool isWallSticking;
    private bool wallSlideUsedForCurrentContact;
    private Vector3 currentWallNormal;
    private bool currentWallIsSide;
    private bool currentWallIsRightSide;
    private float wallStickTimer;
    private LayerMask wallMask;

    // =================
    // LEDGE GRAB VARIABLES
    // =================
    [SerializeField] private LayerMask ledgeMask;
    [SerializeField] private float ledgeCheckDistance = 0.65f;
    [SerializeField] private float ledgeDetectionHeight = 1.25f;
    [SerializeField] private float minimumLedgeHeight = 0.95f;
    [SerializeField] private float maximumLedgeHeight = 1.65f;
    [SerializeField] private float ledgeHangDrop = 1.35f;
    [SerializeField] private float ledgeHangOffset = 0.08f;
    [SerializeField] private float ledgeClimbDuration = 0.7f;
    [SerializeField] private float ledgeClimbForwardOffset = 0.2f;
    [SerializeField] private float ledgeClimbHeightOffset = 0.35f;
    [HideInInspector] public bool isLedgeHanging;
    private bool isLedgeClimbing;
    private Collider currentLedgeCollider;
    private Vector3 currentLedgeNormal;
    private Collider ledgeRegrabBlockedCollider;
    private Collider ignoredLedgeCollider;
    private Vector3 ledgeHangCenter;
    private Vector3 ledgeClimbLiftCenter;
    private Vector3 ledgeClimbTargetCenter;
    private float ledgeClimbTimer;
    private bool ignoredLedgeCollisionDuringClimb;

    // =================
    // SLIDE VARIABLES
    // =================
    [SerializeField] private bool slideConsumesAllStamina = false;
    [SerializeField] private float staminaConsumtionOnSlide = 0f, slideCooldown = 100f, slideSpeed = 4f, slideDuration = 2f;
    public bool slideOnCooldown = false;
    private Vector3 slideDirection;
    [SerializeField] private float slideDeceleration = 25f;
    [SerializeField] private float downhillSlideAcceleration = 15f;
    [SerializeField] private float uphillSlideDeceleration = 35f;
    [SerializeField] private float slideEndSpeed = 11f;
    [SerializeField] private float slopeEffect = 15f; // How much the degree of the slope effects the change in acceleration of a slide.
    
    // =================
    // CROUCH VARIABLES
    // =================
    [SerializeField] private float crouchedControllerHeight = 1.2f;
    [SerializeField] private float crouchSpeed = 4f;
    [SerializeField] private LayerMask standClearanceMask = ~0;

    private float standingControllerHeight;
    private float targetControllerHeight;
    private Vector3 standingControllerCenter;
    private bool standUpRequested;


    // =================
    // PRIVATE VARIABLES
    // =================
    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;
    private float afterlifeFallAugment = 1f, afterlifeSpeedAugment = 1f, afterlifeJumpAugment = 1f;
    private bool afterlife = false;
    private Vector3 fallingVelocity, originalScale, originalHeadHeight;
    private Transform cachedTransform;
    private Coroutine airRoutine;
    private float originalSpeed, crouchingSpeed, sprintActualMultiplier = 1f, sprintMultiplier = 3f;
    public PlayerSoundsManager playerSoundsManager;

    private void Awake() {

        //sprintRemaining = sprintDuration;
        cachedTransform = transform.parent.GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
        originalHeadHeight = headTransform.localPosition;
        currentHeight = originalHeadHeight.y;

        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        wallMask = 0;
        if(wallLayerIndex >= 0) wallMask |= 1 << wallLayerIndex;
        if(groundLayerIndex >= 0) wallMask |= 1 << groundLayerIndex;

        playerSoundsManager = GetComponentInParent<PlayerSoundsManager>();
    }

    private void Start() {

        originalSpeed = speed;
        crouchingSpeed = speed / 2;
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }

        standingControllerHeight = controller.height;
        standingControllerCenter = controller.center;
        targetControllerHeight = isCrouched
            ? Mathf.Max(crouchedControllerHeight, controller.radius * 2f)
            : standingControllerHeight;
    }

    public override void OnNetworkSpawn() {
        if(!IsOwner) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RegisterWithSceneUI(SceneManager.GetActiveScene()));
    }

    public override void OnNetworkDespawn() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if(IsOwner) StartCoroutine(RegisterWithSceneUI(scene));
    }

    private IEnumerator RegisterWithSceneUI(Scene scene) {
        UIManager uiManager = null;
        while(uiManager == null && scene.IsValid() && scene.isLoaded) {
            foreach(UIManager candidate in FindObjectsOfType<UIManager>(true)) {
                if(candidate.gameObject.scene == scene) { uiManager = candidate; break; }
            }
            if(uiManager == null) yield return null;
        }
        if(uiManager == null) yield break;

        uiManager.RegisterPlayer(this);
        uiManager.RegisterToolController(transform.parent.GetComponent<ToolController>());

        feathersVFXA.Stop();
        feathersVFXB.Stop();

        GetComponentInParent<Death>().afterlifePlayer.OnValueChanged += OnAfterlifeChanged;
    }

    private void OnAfterlifeChanged(bool oldState, bool newState) {
        // Accordingly (as onafterlife may trigger to enter but also to leave).
        // increase speed.
        // increase hop dist.
        // lower gravity rate.
        // disable slide ability.
        // disable crouch ability?

        afterlifeFallAugment = newState ? .5f : 1f;
        afterlifeSpeedAugment = newState ? 1.5f : 1f;
        afterlifeJumpAugment = newState ? 1.4f : 1f;
        afterlife = newState;
    }

    public void ResetVarsToDefaults() {
        allowedToMove = true;
        allowedToCrouch = true;

        if(isCrouched) Crouch(true);   
    }

    public void Crouch(bool affectSpeed) {
        bool shouldCrouch = !isCrouched;

        if(!shouldCrouch && !HasStandingClearance()) {
            standUpRequested = true;
            return;
        }

        ApplyCrouchState(shouldCrouch, affectSpeed);
    }

    private void ApplyCrouchState(bool crouched, bool affectSpeed) {
        if(isCrouched == crouched) return;

        isCrouched = crouched;
        standUpRequested = false;
        if(affectSpeed) speed = crouched ? crouchingSpeed : originalSpeed;
        SetControllerCrouched(crouched);
        playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.CrouchSFX);
    }

    private bool HasStandingClearance() {
        Vector3 worldCenter = controller.transform.TransformPoint(standingControllerCenter);
        float scaleY = controller.transform.lossyScale.y;
        float scaleXZ = Mathf.Max(controller.transform.lossyScale.x, controller.transform.lossyScale.z);
        float radius = controller.radius * scaleXZ;
        float height = Mathf.Max(standingControllerHeight * scaleY, radius * 2f);
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 capsuleBottom = worldCenter - controller.transform.up * halfSegment;
        Vector3 capsuleTop = worldCenter + controller.transform.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            capsuleBottom,
            capsuleTop,
            radius,
            standClearanceMask,
            QueryTriggerInteraction.Ignore
        );

        for(int i = 0; i < overlaps.Length; i++) {
            Transform hitTransform = overlaps[i].transform;
            if(hitTransform == controller.transform || hitTransform.IsChildOf(controller.transform)) continue;
            return false;
        }

        return true;
    }

    private void SetControllerCrouched(bool crouched) {
        targetControllerHeight = crouched
            ? Mathf.Max(crouchedControllerHeight, controller.radius * 2f)
            : standingControllerHeight;
    }

    private void UpdateControllerHeight() {
        controller.height = Mathf.MoveTowards(
            controller.height,
            targetControllerHeight,
            crouchSpeed * Time.deltaTime
        );
        controller.center = standingControllerCenter
            - Vector3.up * ((standingControllerHeight - controller.height) * 0.5f);
    }

    private void Jump() {
        verticalVelocity.y = Mathf.Sqrt( jumpHeight * -2f * afterlifeJumpAugment * gravityStrength );
        PlayJumpEffects(false);
    }

    private bool TryFindWall(out Vector3 wallNormal, out bool isSideWall, out bool isRightSide) {
        wallNormal = Vector3.zero;
        isSideWall = false;
        isRightSide = false;

        Vector3 forward = Vector3.ProjectOnPlane(cachedTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cachedTransform.right, Vector3.up).normalized;
        Vector3 castOrigin = controller.bounds.center + Vector3.up * (wallCastHeight - controller.height * 0.5f);
        Vector3[] directions = { forward, right, -right };
        RaycastHit hit;

        // Check the front first, then either side. This gives corner contacts a stable priority.
        for(int i = 0; i < directions.Length; i++) {
            if(Physics.SphereCast(castOrigin, wallCastRadius, directions[i], out hit, wallCastDistance, wallMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) <= maximumWallNormalY) {
                wallNormal = hit.normal;
                isSideWall = i != 0;
                isRightSide = i == 1;
                return true;
            }
        }

        // Sphere casts can miss a surface when the cast sphere already overlaps it.
        Collider[] nearbyColliders = Physics.OverlapSphere(castOrigin, wallCastRadius, wallMask, QueryTriggerInteraction.Ignore);
        float bestDirectionScore = 0.5f;
        for(int i = 0; i < nearbyColliders.Length; i++) {
            Vector3 closestPoint = nearbyColliders[i].ClosestPoint(castOrigin);
            Vector3 normal = castOrigin - closestPoint;
            if(normal.sqrMagnitude < 0.0001f) continue;

            normal.Normalize();
            if(Mathf.Abs(normal.y) > maximumWallNormalY) continue;

            Vector3 towardWall = -normal;
            for(int directionIndex = 0; directionIndex < directions.Length; directionIndex++) {
                float directionScore = Vector3.Dot(directions[directionIndex], towardWall);
                if(directionScore > bestDirectionScore) {
                    bestDirectionScore = directionScore;
                    wallNormal = normal;
                    isSideWall = directionIndex != 0;
                    isRightSide = directionIndex == 1;
                }
            }
        }

        return wallNormal != Vector3.zero;
    }

    private bool TryFindLedge(out RaycastHit ledgeHit, out float ledgeTopHeight) {
        ledgeHit = default;
        ledgeTopHeight = 0f;

        Vector3 forward = Vector3.ProjectOnPlane(cachedTransform.forward, Vector3.up).normalized;
        if(forward.sqrMagnitude < 0.001f || ledgeMask.value == 0) return false;

        // Do not immediately catch the same marker again after climbing or releasing.
        // Clear this block after the player moves beyond the detector's reach.
        bool blockRecentLedge = false;
        if(ledgeRegrabBlockedCollider != null) {
            Vector3 closestPoint = ledgeRegrabBlockedCollider.ClosestPoint(controller.bounds.center);
            Vector3 horizontalOffset = Vector3.ProjectOnPlane(controller.bounds.center - closestPoint, Vector3.up);
            float clearDistance = ledgeCheckDistance + controller.radius * 2f;
            blockRecentLedge = horizontalOffset.magnitude <= clearDistance;
            if(!blockRecentLedge) {
                SetLedgeMarkerCollisionIgnored(false);
                ledgeRegrabBlockedCollider = null;
            }
        }

        Bounds bounds = controller.bounds;
        Vector3 castOrigin = new Vector3(bounds.center.x, bounds.min.y + ledgeDetectionHeight, bounds.center.z);
        if(!Physics.SphereCast(castOrigin, wallCastRadius, forward, out ledgeHit, ledgeCheckDistance, ledgeMask, QueryTriggerInteraction.Collide)) return false;
        if(blockRecentLedge && ledgeHit.collider == ledgeRegrabBlockedCollider) return false;

        // Marker colliders should face the approach direction and have their top aligned with the walkable ledge surface.
        if(Vector3.Dot(ledgeHit.normal, -forward) < 0.5f) return false;
        ledgeTopHeight = ledgeHit.collider.bounds.max.y;
        float heightAboveFeet = ledgeTopHeight - bounds.min.y;
        return heightAboveFeet >= minimumLedgeHeight && heightAboveFeet <= maximumLedgeHeight;
    }

    private void BeginLedgeHang(RaycastHit ledgeHit, float ledgeTopHeight) {
        currentLedgeCollider = ledgeHit.collider;
        currentLedgeNormal = ledgeHit.normal;
        Vector3 forward = Vector3.ProjectOnPlane(cachedTransform.forward, Vector3.up).normalized;
        Vector3 outward = Vector3.ProjectOnPlane(ledgeHit.normal, Vector3.up).normalized;
        float scaledRadius = controller.radius * Mathf.Max(controller.transform.lossyScale.x, controller.transform.lossyScale.z);

        ledgeHangCenter = ledgeHit.point + outward * (scaledRadius + ledgeHangOffset);
        ledgeHangCenter.y = ledgeTopHeight - ledgeHangDrop + controller.height * controller.transform.lossyScale.y * 0.5f;
        ledgeClimbTargetCenter = ledgeHit.point - forward * (scaledRadius + ledgeClimbForwardOffset);
        ledgeClimbTargetCenter.y = ledgeTopHeight + controller.height * controller.transform.lossyScale.y * 0.5f + ledgeClimbHeightOffset;
        ledgeClimbLiftCenter = ledgeHangCenter;
        ledgeClimbLiftCenter.y = ledgeClimbTargetCenter.y;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        isWallSticking = false;
        if(playerAnimator != null) {
            playerAnimator.SetBool("WallHangFront", false);
            playerAnimator.SetBool("WallRunningRight", false);
            playerAnimator.SetBool("WallRunningLeft", false);
        }
        if(armsAnimator != null) {
            armsAnimator.SetBool("WallHangFront", false);
            armsAnimator.SetBool("WallRunningRight", false);
            armsAnimator.SetBool("WallRunningLeft", false);
        }
        isLedgeHanging = true;
        isLedgeClimbing = false;
        isSprinting = false;
        sliding = false;
        allowedToCrouch = true;
        slideDirection = Vector3.zero;
        StopSlideEffectsServerRpc();
        if(isCrouched) {
            standUpRequested = true;
            if(HasStandingClearance()) ApplyCrouchState(false, true);
        }
        controller.Move(ledgeHangCenter - controller.bounds.center);
        SetLedgeAnimatorState(true);

        playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.LedgeGrabSFX);
    }

    private void UpdateLedgeHang() {
        if(currentLedgeCollider == null) {
            ReleaseLedge();
            return;
        }

        if(isLedgeClimbing) {
            ledgeClimbTimer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, ledgeClimbDuration);
            float progress = Mathf.Clamp01(ledgeClimbTimer / duration);
            Vector3 nextCenter;
            if(progress < 0.4f) {
                // Lift the capsule above the ledge before moving it forward, clearing the vertical face.
                nextCenter = Vector3.Lerp(ledgeHangCenter, ledgeClimbLiftCenter, progress / 0.4f);
            }
            else {
                float forwardProgress = (progress - 0.4f) / 0.6f;
                nextCenter = Vector3.Lerp(ledgeClimbLiftCenter, ledgeClimbTargetCenter, forwardProgress);
            }
            controller.Move(nextCenter - controller.bounds.center);

            if(progress >= 1f) {
                isLedgeHanging = false;
                isLedgeClimbing = false;
                ledgeRegrabBlockedCollider = currentLedgeCollider;
                currentLedgeCollider = null;
                horizontalVelocity = Vector3.zero;
                verticalVelocity.y = -groundedStickForce;
                SetLedgeAnimatorState(false);
            }
            return;
        }

        // Keep the player attached to the marker while the hanging animation plays.
        controller.Move(ledgeHangCenter - controller.bounds.center);
        bool dropRequested = KeyBindingManager.GetKeyDown(GameAction.Crouch) || KeyBindingManager.GetKeyUp(GameAction.Crouch)
            || KeyBindingManager.GetKeyDown(GameAction.MoveBackward);
        if(dropRequested) {
            ReleaseLedge();
            return;
        }

        if(KeyBindingManager.GetKeyDown(GameAction.Jump) && HasLedgeClimbClearance()) {
            isLedgeClimbing = true;
            ledgeClimbTimer = 0f;
            SetLedgeHangingAnimation(false);
            SetLedgeMarkerCollisionIgnored(true);
            SetLedgeClimbTrigger();

            playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.LedgeClimbSFX);
        }
    }

    private bool HasLedgeClimbClearance() {
        float scaleY = controller.transform.lossyScale.y;
        float scaleXZ = Mathf.Max(controller.transform.lossyScale.x, controller.transform.lossyScale.z);
        float radius = controller.radius * scaleXZ;
        float height = Mathf.Max(standingControllerHeight * scaleY, radius * 2f);
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 standingCenter = ledgeClimbTargetCenter - Vector3.up * ledgeClimbHeightOffset;
        Vector3 capsuleBottom = standingCenter - controller.transform.up * halfSegment;
        Vector3 capsuleTop = standingCenter + controller.transform.up * halfSegment;

        Collider[] overlaps = Physics.OverlapCapsule(
            capsuleBottom,
            capsuleTop,
            radius,
            standClearanceMask,
            QueryTriggerInteraction.Ignore
        );

        for(int i = 0; i < overlaps.Length; i++) {
            Transform hitTransform = overlaps[i].transform;
            if(hitTransform == controller.transform || hitTransform.IsChildOf(controller.transform)) continue;
            if(overlaps[i] == currentLedgeCollider) continue;
            return false;
        }
        return true;
    }

    private void SetLedgeMarkerCollisionIgnored(bool ignored) {
        if(ignored) {
            if(currentLedgeCollider != null && controller != null) {
                Physics.IgnoreCollision(controller, currentLedgeCollider, true);
                ignoredLedgeCollider = currentLedgeCollider;
                ignoredLedgeCollisionDuringClimb = true;
            }
        }
        else {
            if(ignoredLedgeCollider != null && controller != null) {
                Physics.IgnoreCollision(controller, ignoredLedgeCollider, false);
            }
            ignoredLedgeCollider = null;
            ignoredLedgeCollisionDuringClimb = false;
        }
    }

    private void ReleaseLedge() {
        if(ignoredLedgeCollisionDuringClimb) SetLedgeMarkerCollisionIgnored(false);
        ledgeRegrabBlockedCollider = currentLedgeCollider;
        isLedgeHanging = false;
        isLedgeClimbing = false;
        currentLedgeCollider = null;
        horizontalVelocity = Vector3.zero;
        verticalVelocity.y = -wallSlideSpeed;
        allowedToCrouch = true;
        if(isCrouched) {
            standUpRequested = true;
            if(HasStandingClearance()) ApplyCrouchState(false, true);
        }
        SetLedgeAnimatorState(false);
    }

    private void SetLedgeHangingAnimation(bool hanging) {
        if(playerAnimator != null) playerAnimator.SetBool("LedgeHanging", hanging);
        if(armsAnimator != null) armsAnimator.SetBool("LedgeHanging", hanging);
    }

    private void SetLedgeAnimatorState(bool hanging) {
        SetLedgeHangingAnimation(hanging);

        PlayerHandler playerHandler = GetComponentInParent<PlayerHandler>();
        if(hanging && playerHandler != null && playerHandler.cameraReference != null) {
            playerHandler.cameraReference.CancelWallGrabImpulses();
        }

        MouseLook mouseLook = playerHandler != null && playerHandler.cameraReference != null
            ? playerHandler.cameraReference.GetComponent<MouseLook>()
            : null;
        if(mouseLook != null) {
            Vector3 facingDirection = hanging ? -currentLedgeNormal : Vector3.zero;
            mouseLook.SetLedgeHangLook(hanging, facingDirection);
        }
    }

    private void SetLedgeClimbTrigger() {
        if(playerAnimator != null) playerAnimator.SetBool("LedgeClimb", true);
        if(armsAnimator != null) armsAnimator.SetBool("LedgeClimb", true);

        StartCoroutine(LedgeClimbDone());
    }

    private IEnumerator LedgeClimbDone() {
        yield return new WaitForSeconds(.5f);
        if(playerAnimator != null) playerAnimator.SetBool("LedgeClimb", false);
        if(armsAnimator != null) armsAnimator.SetBool("LedgeClimb", false);

        
    }

    private void StartWallHop() {
        Vector3 outward = Vector3.ProjectOnPlane(currentWallNormal, Vector3.up).normalized;
        horizontalVelocity = outward * wallHopOutwardSpeed;
        if(currentWallIsSide) {
            Vector3 forwardAlongWall = Vector3.ProjectOnPlane(cachedTransform.forward, outward).normalized;
            if(Vector3.Dot(forwardAlongWall, cachedTransform.forward) < 0f) forwardAlongWall = -forwardAlongWall;
            // A 45 degree launch between the wall's outward normal and forward travel along it.
            horizontalVelocity = outward * wallHopOutwardSpeed + forwardAlongWall * wallHopForwardSpeed;
        }

        float upwardMultiplier = currentWallIsSide ? sideWallHopUpwardMultiplier : forwardWallHopUpwardMultiplier;
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * afterlifeJumpAugment * gravityStrength) * upwardMultiplier;
        isWallSticking = false;
        wallStickTimer = 0f;

        // Reuse the normal jump presentation without overwriting the wall-hop launch velocity.
        PlayJumpEffects(true);
        if(currentWallIsSide) {
            // The clip name follows the direction of travel: a hop from a right wall moves left.
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger(currentWallIsRightSide ? "WallHopRight" : "WallHopLeft");

            armsAnimator.ResetTrigger("Jump");
            armsAnimator.SetTrigger(currentWallIsRightSide ? "WallHopRight" : "WallHopLeft");
        }
        else {
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger("WallHopFront");

            armsAnimator.ResetTrigger("Jump");
            armsAnimator.SetTrigger("WallHopFront");
        }
    }

    private void PlayJumpEffects(bool wallJump) {
        PlayerHandler cameraOwner = transform.parent.GetComponent<PlayerHandler>();
        if(cameraOwner != null && cameraOwner.cameraReference != null) {
            cameraOwner.cameraReference.PlayJumpImpulse();
        }

        int spellHeldIndex = transform.parent.GetComponent<ToolController>().heldIndex.Value;
        //source.PlayOneShot(afterlife ? ghostJump : jumpClip[Random.Range(0, jumpClip.Length)]);

        if(afterlife) {
            playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.GhostJumpSFX);
        }
        else {
            playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.JumpSFX);
            playerSoundsManager.PlaySound(wallJump ? PlayerSoundsManager.PlayerSound.WallJumpGrunt : PlayerSoundsManager.PlayerSound.JumpGrunt);
        }

        playerAnimator.SetTrigger("Jump");
        playerAnimator.SetBool("InAirFromJump", true);
        if(spellHeldIndex == 0 || spellHeldIndex == 3 || spellHeldIndex == 4 || spellHeldIndex == 7) armsAnimator.SetTrigger("Jump");
        armsAnimator.SetBool("Grounded", false);
        if(airRoutine != null) StopCoroutine(airRoutine);
        airRoutine = StartCoroutine(InAirFromJumpTimer());
    }

    // Wrong.
    private IEnumerator InAirFromJumpTimer() {
        yield return new WaitForSeconds(1f);
        playerAnimator.SetBool("InAirFromJump", false);
    }

    public bool TiredState() { return isTired; }

    public bool SlidingState() { return sliding; }

    /// <summary>Returns the current world-space movement velocity for ragdoll handoff.</summary>
    public Vector3 GetCurrentVelocity() {
        return horizontalVelocity + verticalVelocity;
    }

    /// <summary>Clears saved movement momentum and transient movement states after ragdoll recovery.</summary>
    public void ResetMovementVelocity() {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        slideDirection = Vector3.zero;
        sliding = false;
        allowedToCrouch = true;
        isSprinting = false;
        isWallSticking = false;
        wallStickTimer = 0f;
        wallSlideUsedForCurrentContact = false;
        if(ignoredLedgeCollisionDuringClimb) SetLedgeMarkerCollisionIgnored(false);
        isLedgeHanging = false;
        isLedgeClimbing = false;
        currentLedgeCollider = null;
        SetLedgeAnimatorState(false);
        StopSlideEffectsServerRpc();

        if(isCrouched) {
            standUpRequested = true;
            if(HasStandingClearance()) ApplyCrouchState(false, true);
        }
    }

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!IsOwner) {
            enabled = false;
            return;
        }

        if(!playerAlive) return;

        // Keep a released crouch request pending until the standing capsule fits.
        if(standUpRequested && HasStandingClearance()) {
            ApplyCrouchState(false, true);
        }
        UpdateControllerHeight();

        // Handle an existing ledge hang before ordinary movement input is read.
        // This prevents crouch/drop keys that overlap a movement binding from moving the player while attached.
        if(isLedgeHanging) {
            UpdateLedgeHang();
            playerAnimator.SetFloat("Vertical", 0f);
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetBool("Sprinting", false);
            armsAnimator.SetBool("Sprinting", false);
            playerAnimator.SetBool("Walking", false);
            return;
        }

        // =========================
        // MOVEMENT / JUMP / GRAVITY
        // =========================
        float horiz = 0f;
        float vert = 0f;
        // Horizontal Movement
        if(allowedToMove) {
            horiz = (KeyBindingManager.GetKey(GameAction.MoveRight) ? 1f : 0f) - (KeyBindingManager.GetKey(GameAction.MoveLeft) ? 1f : 0f);
            vert = (KeyBindingManager.GetKey(GameAction.MoveForward) ? 1f : 0f) - (KeyBindingManager.GetKey(GameAction.MoveBackward) ? 1f : 0f);
            Vector3 inputVector = cachedTransform.right * horiz + cachedTransform.forward * vert;

            float targetHeadHeight = isCrouched ? crouchHeight : originalHeadHeight.y;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeadHeight, crouchSpeed * Time.deltaTime);
            headTransform.localPosition = new Vector3(originalHeadHeight.x, currentHeight, originalHeadHeight.z);



            // SPRINT & SPRINT UI Section
            if(groundCheckerScript.isGrounded) {
                if(isSprinting && !isTired) {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value -= 1 * Time.deltaTime;
                }
                else {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value = Mathf.Clamp(transform.parent.GetComponent<PlayerHandler>().stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
                }

                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value <= 0) {
                    //source.PlayOneShot(breathClip);
                   // playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.OutOfBreathStamina);
                }
                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value == staminaDuration) {
                    isTired = false;
                }

                StaminaUpdate();
                if((KeyBindingManager.GetKey(GameAction.MoveForward) && groundCheckerScript.isGrounded && KeyBindingManager.GetKey(GameAction.Sprint) && !isTired && allowedToMove && !isCrouched) || sliding) {
                    isSprinting = true;
                    sprintActualMultiplier = sprintMultiplier;
                }
                else if((isTired || groundCheckerScript.isGrounded) || (!KeyBindingManager.GetKey(GameAction.MoveForward) || !KeyBindingManager.GetKey(GameAction.Sprint))) // or is Grounded (we don't want to disable sprinting 
                {
                    isSprinting = false;
                    sprintActualMultiplier = 1;
                }

                if(!sliding && allowedToCrouch && allowedToMove && (KeyBindingManager.GetKeyDown(GameAction.Crouch) || KeyBindingManager.GetKeyUp(GameAction.Crouch)) && !isSprinting && !afterlife) {
                    Crouch(true);
                }
                // Start Slide
                else if(!sliding && allowedToCrouch && allowedToMove && (KeyBindingManager.GetKeyDown(GameAction.Crouch) || KeyBindingManager.GetKeyUp(GameAction.Crouch))
                    && isSprinting && groundCheckerScript.isGrounded && !slideOnCooldown && !afterlife) {

                    sliding = true;
                    slideDirection = cachedTransform.forward;
                    horizontalVelocity = slideDirection * slideSpeed * speed;
                    Crouch(false);
                    playerAnimator.SetTrigger("Slide");
                    armsAnimator.SetTrigger("Slide");
                    allowedToCrouch = false;
                    SlideEffectsServerRpc();
                    playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.SlideSFX);

                    transform.parent.GetComponent<PlayerHandler>().stamina.Value -= staminaConsumtionOnSlide;
                    if(transform.parent.GetComponent<PlayerHandler>().stamina.Value < 0) transform.parent.GetComponent<PlayerHandler>().stamina.Value = 0;
                }
                // Sliding
                if(sliding) {
                    // Gradually lose momentum while sliding.
                    Vector3 groundNormal = groundCheckerScript.GroundNormal;
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    float slopeDot = Vector3.Dot(slideDirection, slopeDirection);

                    float currentSlideDeceleration = slideDeceleration - (slopeDot * slopeEffect);

                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        Vector3.zero,
                        currentSlideDeceleration * Time.deltaTime
                    );

                    // End the slide once momentum becomes sufficiently low.
                    if(horizontalVelocity.magnitude <= slideEndSpeed) {
                        //horizontalVelocity = Vector3.zero;
                        sliding = false;
                        Crouch(false);
                        allowedToCrouch = true;
                        StopSlideEffectsServerRpc();
                    }
                }
                else {
                    // Normal movement on the ground.
                    Vector3 targetHorizontalVelocity =
                    inputVector * sprintActualMultiplier * speed;

                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        targetHorizontalVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else {
                // Normal movement in the air.
                Vector3 targetHorizontalVelocity =
                    inputVector * sprintActualMultiplier * speed;

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetHorizontalVelocity,
                    airAcceleration * Time.deltaTime
                );
            }
            

           
        }

        // Wall contact is evaluated before this frame's controller move so it can affect movement immediately.
        bool hasGroundContact = groundCheckerScript.isGrounded || controller.isGrounded;
        if(hasGroundContact && ledgeRegrabBlockedCollider != null) {
            if(ignoredLedgeCollisionDuringClimb) SetLedgeMarkerCollisionIgnored(false);
            ledgeRegrabBlockedCollider = null;
        }
        if(!hasGroundContact && TryFindLedge(out RaycastHit ledgeHit, out float ledgeTopHeight)) {
            BeginLedgeHang(ledgeHit, ledgeTopHeight);
            playerAnimator.SetFloat("Vertical", 0f);
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetBool("Sprinting", false);
            armsAnimator.SetBool("Sprinting", false);
            playerAnimator.SetBool("Walking", false);
            return;
        }

        if(hasGroundContact) {
            isWallSticking = false;
            wallSlideUsedForCurrentContact = false;
            wallStickTimer = 0f;
        }
        else {
            bool hasWallContact = TryFindWall(out Vector3 detectedWallNormal, out bool detectedWallIsSide, out bool detectedWallIsRightSide);

            if(hasWallContact) {
                currentWallNormal = detectedWallNormal;
                currentWallIsSide = detectedWallIsSide;
                currentWallIsRightSide = detectedWallIsRightSide;

                if(!wallSlideUsedForCurrentContact && !isWallSticking) {
                    isWallSticking = true;
                    wallSlideUsedForCurrentContact = true;
                    wallStickTimer = wallSlideDuration;

                    playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.WallGrabSFX);

                    PlayerHandler cameraOwner = transform.parent.GetComponent<PlayerHandler>();
                    if(cameraOwner != null && cameraOwner.cameraReference != null) {
                        if(!currentWallIsSide) cameraOwner.cameraReference.PlayWallGrabFrontImpulse();
                        else if(currentWallIsRightSide) cameraOwner.cameraReference.PlayWallGrabRightImpulse();
                        else cameraOwner.cameraReference.PlayWallGrabLeftImpulse();
                    }
                }
            }
            else {
                isWallSticking = false;
                wallSlideUsedForCurrentContact = false;
            }
        }

        if(isWallSticking) {
            if(!currentWallIsSide) {
                playerAnimator.SetBool("WallHangFront", true);
                playerAnimator.SetBool("WallRunningRight", false);
                playerAnimator.SetBool("WallRunningLeft", false);

                armsAnimator.SetBool("WallHangFront", true);
                armsAnimator.SetBool("WallRunningRight", false);
                armsAnimator.SetBool("WallRunningLeft", false);
            }
            else if(currentWallIsRightSide) {
                playerAnimator.SetBool("WallHangFront", false);
                playerAnimator.SetBool("WallRunningRight", true);
                playerAnimator.SetBool("WallRunningLeft", false);

                armsAnimator.SetBool("WallHangFront", false);
                armsAnimator.SetBool("WallRunningRight", true);
                armsAnimator.SetBool("WallRunningLeft", false);
            }
            else {
                playerAnimator.SetBool("WallHangFront", false);
                playerAnimator.SetBool("WallRunningLeft", true);
                playerAnimator.SetBool("WallRunningRight", false);

                armsAnimator.SetBool("WallHangFront", false);
                armsAnimator.SetBool("WallRunningLeft", true);
                armsAnimator.SetBool("WallRunningRight", false);
            }
        }
        else {
            playerAnimator.SetBool("WallHangFront", false);
            playerAnimator.SetBool("WallRunningLeft", false);
            playerAnimator.SetBool("WallRunningRight", false);

            armsAnimator.SetBool("WallHangFront", false);
            armsAnimator.SetBool("WallRunningLeft", false);
            armsAnimator.SetBool("WallRunningRight", false);
        }
        if(KeyBindingManager.GetKeyDown(GameAction.Jump) && allowedToMove && hasGroundContact) {
            isWallSticking = false;
            wallSlideUsedForCurrentContact = false;
            Jump();
        }
        else if(isWallSticking && KeyBindingManager.GetKeyDown(GameAction.Jump) && allowedToMove) {
            StartWallHop();
            groundCheck.GetComponent<GroundChecker>().ResetAirTime();
        }

        if(isWallSticking) {
            wallStickTimer -= Time.deltaTime;
            if(wallStickTimer <= 0f) {
                isWallSticking = false;
            }
            else {
                // Keep existing along-wall momentum, but prevent air input from driving into the wall.
                horizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, currentWallNormal);
                // Preserve jump ascent; apply the controlled wall slide only after upward momentum ends.
                if(verticalVelocity.y <= 0f) verticalVelocity.y = -wallSlideSpeed;
            }
        }

        // Grounding
        if(hasGroundContact && verticalVelocity.y < 0f) {
            verticalVelocity.y = -groundedStickForce;
        }

        // Gravity
        if(!isWallSticking || verticalVelocity.y > 0f) {
            verticalVelocity.y += gravityStrength * afterlifeFallAugment * Time.deltaTime;
        }

        // Final Movement
        controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);

        // Animations
        playerAnimator.SetFloat("Vertical", vert);
        playerAnimator.SetFloat("Horizontal", horiz);
        playerAnimator.SetBool("Sprinting", isSprinting);
        armsAnimator.SetBool("Sprinting", isSprinting);
        playerAnimator.SetBool("Crouching", isCrouched);
       // playerAnimator.SetBool("Sliding", sliding);
        playerAnimator.SetBool("Walking", horiz != 0 || vert != 0);
    }

    // Called from inside this.Update();
    // Any changes here must be mirrored in the UIManager version.
    private void StaminaUpdate() {
        if(isSprinting && !isTired && !afterlife) playerHandlerScript.stamina.Value -= 1 * Time.deltaTime;
        else playerHandlerScript.stamina.Value = Mathf.Clamp(playerHandlerScript.stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
        
        if(playerHandlerScript.stamina.Value <= 0) {
            isTired = true;
            playerSoundsManager.PlaySound(PlayerSoundsManager.PlayerSound.OutOfBreathStamina);
        }
        if(playerHandlerScript.stamina.Value == staminaDuration) {
            isTired = false;
        }
    }


    [ServerRpc]
    private void SlideEffectsServerRpc() {
        SlideEffectsClientRpc();
    }

    [ClientRpc]
    private void SlideEffectsClientRpc() {
        Debug.Log("PLAYING DAMN FEATHERS.");
        feathersVFXA.Play();
        feathersVFXB.Play();
    }

    [ServerRpc]
    private void StopSlideEffectsServerRpc() {
        StopEffectsClientRpc();
    }


    [ClientRpc]
    private void StopEffectsClientRpc() {
        feathersVFXA.Stop();
        feathersVFXB.Stop();
    }

    private IEnumerator SlideCooldown() {
        slideOnCooldown = true;
        yield return new WaitForSeconds(slideCooldown);
        slideOnCooldown = false;
    }

}

