using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTrap : MonoBehaviour {

    public AudioSource source, sourceMine;
    public AudioClip clipA, clipB;

    public bool triggered = false;

    private void OnTriggerEnter(Collider other) {
        if(!triggered && (other.CompareTag("Player") || other.transform.parent.CompareTag("Player")) ) {
            triggered = true;
            source.PlayOneShot(clipA);
            sourceMine.PlayOneShot(clipB);
        }
    }
    
}
