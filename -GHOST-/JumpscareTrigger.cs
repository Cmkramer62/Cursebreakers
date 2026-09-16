using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class JumpscareTrigger : MonoBehaviour {

    public enum TriggerType { Touch, Seen, Code};
    public TriggerType currentTrigger = TriggerType.Touch;
    public bool playSound = true, lookAt = false, animate = false, repeatable = false, cantMove, cantLook = false;
    private bool triggered = false, lookNow = false, waiting = false;
    public float delay = 0f;

    // Audio
    public AudioSource source;
    public AudioClip[] clips;
    public float volume = 1f;

    // Visual
    public Transform targetSpot, cameraTransform, bodyTransform; // Assign an empty GameObject in the Inspector
    public float lerpSpeed = 5f;

    // Animation
    public Animator animatorController;
    public string triggerName;

    private void OnTriggerEnter(Collider other) {
        if(currentTrigger == TriggerType.Touch && other.CompareTag("Player") && other.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId) {
            Jumpscare();
        }   
    }
    void LateUpdate() {
        if(targetSpot != null && lookNow) {
            

            Vector3 euler = Quaternion.LookRotation((targetSpot.transform.position - bodyTransform.position).normalized).eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0f, euler.y, 0f);
            bodyTransform.rotation = Quaternion.Slerp(bodyTransform.rotation, targetRotation, lerpSpeed * Time.deltaTime);

            Vector3 euler2 = Quaternion.LookRotation((targetSpot.transform.position - cameraTransform.position).normalized).eulerAngles;
            Quaternion targetRotation2 = Quaternion.Euler(euler2.x, 0f, 0f);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRotation2, lerpSpeed * Time.deltaTime);

            if(Quaternion.Angle(bodyTransform.rotation, targetRotation) < 2f && Quaternion.Angle(cameraTransform.localRotation, targetRotation2) < 2f) {
               lookNow = false;
               cameraTransform.GetComponent<MouseLook>().allowedToLook = true;
               Debug.Log("Stop Rot");
            }

        }

    }

    public void Jumpscare() {
        if(!waiting) StartCoroutine(JumpscareTimer());
    }

    private IEnumerator JumpscareTimer() {
        waiting = true;
        yield return new WaitForSeconds(delay);
        if(repeatable || !triggered) {
            triggered = true;
            if(playSound) {
                foreach(AudioClip clip in clips) {
                    source.PlayOneShot(clip, volume);
                }
            }
            if(lookAt) {
                lookNow = true;
                if(cantLook) cameraTransform.GetComponent<MouseLook>().allowedToLook = false;
                Debug.Log("Start Rot");
            }
            if(animate) {
                animatorController.SetTrigger(triggerName);
            }
        }
        waiting = false;

        if(!repeatable && currentTrigger == TriggerType.Seen) GameObject.Destroy(gameObject);
    }

}
