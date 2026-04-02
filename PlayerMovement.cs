using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : MonoBehaviour {
    
    public GroundChecker groundCheckerScript;

    public float speed = 12f, staminaRecoveryRate = 1f, staminaDuration = 40f;

    

    private float originalSpeed, crouchingSpeed, sprintActualMultiplier = 1f, gravity = -22f, jumpHeight = 1, sprintMultiplier = 3f, crouchHeight = 0.75f, currentHeight = .75f;

    [SerializeField]
    private bool shouldBeSlowed = false, isTired = false;
    public bool allowedToMove = true, allowedToCrouch = true, isSprinting = false, isCrouched = false, isHiding = false;

    [SerializeField]
    public CharacterController controller;
    public bool lockCursor = true;
    public Transform groundCheck;
    public float groundDistance = 0.4f, amountCrouchSpots = 0f;
    public LayerMask groundMask;
    public KeyCode crouchKey = KeyCode.LeftControl;



    public ConeLOSDetector enemyVisionScript;
    #region SOUND VARIABLES
        public AudioSource source;
        public AudioClip breathClip, crouchClip;
        public AudioClip[] jumpClip;
    #endregion

    public LightFlicker lanternReference;

    private Vector3 fallingVelocity, originalScale;
    private Transform cachedTransform;
    [SerializeField] private PlayerHandler playerHandlerScript;

    private void Awake() {

        if(!playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }

        //sprintRemaining = sprintDuration;
        cachedTransform = GetComponent<Transform>();
        originalScale = cachedTransform.localScale;
    }

    private void Start() {
        if(!playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }
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
        source.PlayOneShot(crouchClip);
        speed = isCrouched ? originalSpeed : crouchingSpeed;
        currentHeight = isCrouched ? crouchHeight : originalScale.y;
        //enemyVisionScript.fieldOfViewAngle += isCrouched ? 30 : -30;
        isCrouched = !isCrouched;
    }

    private void Jump() {
        fallingVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        source.PlayOneShot(jumpClip[Random.Range(0, jumpClip.Length)]);
    }

    public bool TiredState() {
        return isTired;
    }

    public float GetRemainingStam() {
        return transform.parent.GetComponent<PlayerHandler>().stamina.Value / staminaDuration;
    }

    void Update() {
        if(!playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }
        // MOVEMENT Section
        Vector3 inputVector = cachedTransform.right * Input.GetAxis("Horizontal") + cachedTransform.forward * Input.GetAxis("Vertical");
        if(inputVector.magnitude > 1)
            inputVector.Normalize();
        if(allowedToMove)
            controller.Move(inputVector * sprintActualMultiplier * speed * Time.deltaTime);

        // CROUCH Section
        cachedTransform.localScale = new Vector3(originalScale.x, Mathf.Clamp(currentHeight -= (isCrouched ? 2f : -2f) * Time.deltaTime, crouchHeight, originalScale.y), originalScale.z);

        if(allowedToCrouch && allowedToMove && (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey))) {
            isCrouched = !Input.GetKeyDown(crouchKey);
            Crouch();
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
        
    }



}
