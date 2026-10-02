using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;

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
    public KeyCode crouchKey = KeyCode.LeftControl;

    [SerializeField] Animator playerAnimator, armsAnimator;

    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
    public AudioSource source;
    public AudioClip breathClip, crouchClip, ghostJump, slideClip;
    public AudioClip[] jumpClip;
    #endregion

    public LightFlickerNonNetworked lanternReference;


    [SerializeField] private PlayerHandler playerHandlerScript;
    [SerializeField] private Transform headTransform;

    [SerializeField] private ParticleSystem feathersVFXA, feathersVFXB;



    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float groundedStickForce = 8f;

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

    private void Awake() {

        //sprintRemaining = sprintDuration;
        cachedTransform = transform.parent.GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
        originalHeadHeight = headTransform.localPosition;
    }

    private void Start() {

        originalSpeed = speed;
        crouchingSpeed = speed / 2;
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void OnNetworkSpawn() {
        if(!IsOwner) return;

        StartCoroutine(FindUIManager());
    }

    // The OnNetworkSpawn occurs before the scene has fully loaded. So we wait until it has, and find what we need.
    private IEnumerator FindUIManager() {
        var uiManagerInstance = UIManager.Instance;
        while(uiManagerInstance == null) {
            uiManagerInstance = UIManager.Instance;
            yield return null;
        }

        // Pass this self to the client-side UI Manager so it can handle the UI of this.
        UIManager.Instance.RegisterPlayer(this);
        UIManager.Instance.RegisterToolController(transform.parent.GetComponent<ToolController>());

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
        Debug.Log("Crouch called.");
        CrouchEffectsServerRpc();
        if(affectSpeed) speed = isCrouched ? originalSpeed : crouchingSpeed;
        currentHeight = isCrouched ? crouchHeight : originalHeadHeight.y;
        //enemyVisionScript.fieldOfViewAngle += isCrouched ? 30 : -30;
        isCrouched = !isCrouched;
    }

    [ServerRpc]
    private void CrouchEffectsServerRpc() {
        CrouchEffectsClientRpc();
    }

    [ClientRpc]
    private void CrouchEffectsClientRpc() {
        source.PlayOneShot(crouchClip);
    }

    private void Jump() {
        int spellHeldIndex = transform.parent.GetComponent<ToolController>().heldIndex.Value;
        verticalVelocity.y = Mathf.Sqrt( jumpHeight * -2f * afterlifeJumpAugment * gravityStrength );

        source.PlayOneShot( afterlife ? ghostJump : jumpClip[Random.Range(0, jumpClip.Length)] );

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

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!IsOwner) {
            enabled = false;
            return;
        }

        if(!playerAlive) return;

        // =========================
        // MOVEMENT / JUMP / GRAVITY
        // =========================
        float horiz = 0f;
        float vert = 0f;
        // Horizontal Movement
        if(allowedToMove) {
            horiz = Input.GetAxis("Horizontal");
            vert = Input.GetAxis("Vertical");
            Vector3 inputVector = cachedTransform.right * horiz + cachedTransform.forward * vert;

            headTransform.localPosition = new Vector3(originalHeadHeight.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalHeadHeight.y), originalHeadHeight.z);



            // SPRINT & SPRINT UI Section
            if(groundCheckerScript.isGrounded) {
                if(isSprinting && !isTired) {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value -= 1 * Time.deltaTime;
                }
                else {
                    transform.parent.GetComponent<PlayerHandler>().stamina.Value = Mathf.Clamp(transform.parent.GetComponent<PlayerHandler>().stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
                }

                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value <= 0) {
                    source.PlayOneShot(breathClip);
                }
                if(transform.parent.GetComponent<PlayerHandler>().stamina.Value == staminaDuration) {
                    isTired = false;
                }

                StaminaUpdate();
                if((Input.GetKey(KeyCode.W) && groundCheckerScript.isGrounded && Input.GetKey(KeyCode.LeftShift) && !isTired && allowedToMove && !isCrouched) || sliding) {
                    isSprinting = true;
                    sprintActualMultiplier = sprintMultiplier;
                }
                else if((isTired || groundCheckerScript.isGrounded) || (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.LeftShift))) // or is Grounded (we don't want to disable sprinting 
                {
                    isSprinting = false;
                    sprintActualMultiplier = 1;
                }

                if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) && !isSprinting && !afterlife) {
                    isCrouched = !Input.GetKeyDown(crouchKey);
                    Crouch(true);
                }
                // Start Slide
                else if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey))
                    && isSprinting && groundCheckerScript.isGrounded && !slideOnCooldown && !afterlife) {

                    sliding = true;
                    slideDirection = cachedTransform.forward;
                    horizontalVelocity = slideDirection * slideSpeed * speed;
                    Crouch(false);
                    playerAnimator.SetTrigger("Slide");
                    armsAnimator.SetTrigger("Slide");
                    allowedToCrouch = false;
                    SlideEffectsServerRpc();
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

        // Grounding
        if(groundCheckerScript.isGrounded && verticalVelocity.y < 0f) {
            verticalVelocity.y = -groundedStickForce;
        }

        // Jump
        if(Input.GetButtonDown("Jump") && allowedToMove && groundCheckerScript.isGrounded) {
            Jump();
        }

        // Gravity
        verticalVelocity.y += gravityStrength * afterlifeFallAugment * Time.deltaTime;

        // Final Movement
        controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);

        // Animations
        playerAnimator.SetFloat("Vertical", vert);
        playerAnimator.SetFloat("Horizontal", horiz);
        playerAnimator.SetBool("Sprinting", isSprinting);
        armsAnimator.SetBool("Sprinting", isSprinting);
        playerAnimator.SetBool("Crouching", isCrouched);
        playerAnimator.SetBool("Walking", horiz != 0 || vert != 0);
    }

    // Called from inside this.Update();
    // Any changes here must be mirrored in the UIManager version.
    private void StaminaUpdate() {
        if(isSprinting && !isTired && !afterlife) playerHandlerScript.stamina.Value -= 1 * Time.deltaTime;
        else playerHandlerScript.stamina.Value = Mathf.Clamp(playerHandlerScript.stamina.Value += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
        
        if(playerHandlerScript.stamina.Value <= 0) {
            isTired = true;
            source.PlayOneShot(breathClip);
        }
        if(playerHandlerScript.stamina.Value == staminaDuration) {
            isTired = false;
        }
    }

    private IEnumerator SlideRoutine() {
        StartCoroutine(SlideCooldown());
        playerAnimator.SetTrigger("Slide");
        armsAnimator.SetTrigger("Slide");
        sliding = true;

        allowedToCrouch = false;
        //allowedToMove = false;
        //isCrouched = true;

        SlideEffectsServerRpc();

        Crouch(false);

        yield return new WaitForSeconds(slideDuration);
        sliding = false;
        //isCrouched = false;

        Crouch(false);
        StopSlideEffectsServerRpc();
        allowedToCrouch = true;
        //allowedToMove = true;
        //transform.parent.GetComponent<PlayerHandler>().stamina.Value = 0;
        transform.parent.GetComponent<PlayerHandler>().stamina.Value -= staminaConsumtionOnSlide;
        if(transform.parent.GetComponent<PlayerHandler>().stamina.Value < 0) transform.parent.GetComponent<PlayerHandler>().stamina.Value = 0;
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
        source.PlayOneShot(slideClip);
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
