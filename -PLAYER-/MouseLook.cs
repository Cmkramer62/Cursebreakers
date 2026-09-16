using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    public float mouseSensitivity = 100f;
    public Transform playerBody, cameraParent;
    public float xRotation = 0f;
    public bool allowedToLook = true, playerAlive = true;
    [HideInInspector] public Animator cameraAnimator;

    // Start is called before the first frame update
    void Start() {
        //Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update() {
        if(playerBody != null && allowedToLook && playerAlive) {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            // rotate the camera vertically. Pitch only.
            playerBody.Rotate(Vector3.up * mouseX); // rotation should be controlled by the anim?
            cameraParent.Rotate(Vector3.up * mouseX);

            //float angle = Mathf.DeltaAngle(playerBody.parent.eulerAngles.y, cameraParent.eulerAngles.y);
            // Optional: normalize to -1 to 1
            //float normalized = Mathf.Clamp(angle / 90f, -90f, 90f);
            //cameraAnimator.SetFloat("InputAngle", angle);
        }

    }
}
