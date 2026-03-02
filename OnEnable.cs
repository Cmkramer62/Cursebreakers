using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnEnable : MonoBehaviour {

    public GameObject[] objectsToAffect;
    public bool reverse, state;
    public bool repeatable = true;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other) {
        if(other.name == "Player") {
            if(repeatable || !triggered) {
                triggered = true;
                foreach(GameObject objectee in objectsToAffect) {
                    if(reverse) objectee.SetActive(!objectee.activeSelf);
                    else objectee.SetActive(state);
                }
            }
        }
        
        
    }

}
