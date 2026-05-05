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



    private float originalSpeed, crouchingSpeed, sprintActualMultiplier = 1f, gravity = -22f, sprintMultiplier = 3f;
    public float crouchHeight = -0.696f, currentHeight = 0f;

    [SerializeField]
    private bool shouldBeSlowed = false, isTired = false, sliding = false;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false, isCrouched = false, isHiding = false;

    [SerializeField]
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float groundDistance = 0.4f, amountCrouchSpots = 0f;
    public LayerMask groundMask;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [SerializeField] Animator playerAnimator;

    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
        public AudioSource source;
        public AudioClip breathClip, crouchClip;
        public AudioClip[] jumpClip;
    #endregion

    public LightFlicker lanternReference;

    private Vector3 fallingVelocity, originalScale, originalHeadHeight;
    private Transform cachedTransform;
    [SerializeField] private PlayerHandler playerHandlerScript;
    [SerializeField] private Transform headTransform;
    private Coroutine airRoutine;

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

    public void ResetVarsToDefaults() {
        allowedToMove = true;
        allowedToCrouch = true;

        if(isCrouched) Crouch();   
    }

    public void Crouch() {
        Debug.Log("Crouch called.");
        source.PlayOneShot(crouchClip);
        speed = isCrouched ? originalSpeed : crouchingSpeed;
        currentHeight = isCrouched ? crouchHeight : originalHeadHeight.y;
        //enemyVisionScript.fieldOfViewAngle += isCrouched ? 30 : -30;
        isCrouched = !isCrouched;
    }

    private void Jump() {
        fallingVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        source.PlayOneShot(jumpClip[Random.Range(0, jumpClip.Length)]);

        playerAnimator.SetTrigger("Jump");
        playerAnimator.SetBool("InAirFromJump", true);
        if(airRoutine != null) StopCoroutine(airRoutine);
        airRoutine = StartCoroutine(InAirFromJumpTimer());
    }

    private IEnumerator InAirFromJumpTimer() {
        yield return new WaitForSeconds(1f);
        playerAnimator.SetBool("InAirFromJump", false);
    }

    public bool TiredState() {
        return isTired;
    }

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!IsOwner) {
            enabled = false;
            return;
        }
        // MOVEMENT Section
        var horiz = Input.GetAxis("Horizontal");
        var vert = Input.GetAxis("Vertical");
        Vector3 inputVector = cachedTransform.right * horiz + cachedTransform.forward * vert;
        
        playerAnimator.SetFloat("Vertical", vert);
        playerAnimator.SetFloat("Horizontal", horiz);

        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }

        playerAnimator.SetBool("Walking", horiz != 0 || vert != 0);
        if(allowedToMove)
            controller.Move(inputVector * sprintActualMultiplier * speed * Time.deltaTime);

        // CROUCH Section
        //cachedTransform.localScale = new Vector3(originalScale.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalScale.y), originalScale.z);
        headTransform.localPosition = new Vector3(originalHeadHeight.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalHeadHeight.y), originalHeadHeight.z);

        if(allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) && !isSprinting) {
            isCrouched = !Input.GetKeyDown(crouchKey);
            Crouch();
        }
        else if(!sliding && allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey)) 
            && isSprinting && groundCheckerScript.isGrounded) {
            StartCoroutine(SlideRoutine());
        }

        // JUMP Section
        if(groundCheckerScript.isGrounded && fallingVelocity.y < 0)
            fallingVelocity.y = -2f;

        if(Input.GetButtonDown("Jump") && allowedToMove && groundCheckerScript.isGrounded)
            Jump();

        fallingVelocity.y += gravity * Time.deltaTime;

        if(allowedToMove)
            controller.Move(fallingVelocity * Time.deltaTime);


        // SPRINT & SPRINT UI Section
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
        /*
        if(isSprinting && !isTired) {
            stamina -= 1 * Time.deltaTime;
            if(hideBarWhenFull && useSprintBar) { sprintBarCG.alpha += 5 * Time.deltaTime; }
        }
        else {
            stamina = Mathf.Clamp(stamina += staminaRecoveryRate * Time.deltaTime, 0, staminaDuration);
        }

        if(useSprintBar) sprintBar.rectTransform.sizeDelta = new Vector2(stamina / staminaDuration * 175, sprintBar.rectTransform.sizeDelta.y);

        if(stamina <= 0) {
            isTired = true;
            sprintBar.color = Color.red;
            source.PlayOneShot(breathClip);
        }
        if(stamina == staminaDuration) {
            isTired = false;
            if(hideBarWhenFull && useSprintBar) sprintBarCG.alpha -= 3 * Time.deltaTime;
            if(useSprintBar) sprintBar.color = stamBarUIColor;
        }
        */
        if((Input.GetKey(KeyCode.W)) && groundCheckerScript.isGrounded && Input.GetKey(KeyCode.LeftShift) && !isTired && allowedToMove && !isCrouched) {
            isSprinting = true;
            sprintActualMultiplier = sprintMultiplier;
        }
        else if((isTired || groundCheckerScript.isGrounded) || (!Input.GetKey(KeyCode.W) || !Input.GetKey(KeyCode.LeftShift))) // or is Grounded (we don't want to disable sprinting 
        {
            isSprinting = false;
            sprintActualMultiplier = 1;
        }
        playerAnimator.SetBool("Sprinting", isSprinting);
        playerAnimator.SetBool("Crouching", isCrouched);

        if(sliding) {
            Vector3 inputVectorSliding = cachedTransform.right * 0 + cachedTransform.forward * 1;
            controller.Move(inputVectorSliding * 3 * speed * Time.deltaTime);
        }
     
    }

    private IEnumerator SlideRoutine() {
        playerAnimator.SetTrigger("Slide");
        sliding = true;

        allowedToCrouch = false;
        //allowedToMove = false;
       
        yield return new WaitForSeconds(4f);
        sliding = false;
        allowedToCrouch = true;
        //allowedToMove = true;

    }

}
