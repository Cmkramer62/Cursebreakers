using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapJumpscare : MonoBehaviour {

    public float jumpscareDuration = 5f;
    public bool triggered = false;

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player") && !triggered) {
            triggered = true;
            other.GetComponent<PlayerMovement>().allowedToMove = false;
        }
    }

    // touch collider
    // Dissallow player movement. 
    // Lerp camera movement to target.
    // Enable something.
    // play some sound.
    // reallow plapayer movement.

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }
}
