using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DeathTrigger : MonoBehaviour {

    public bool touchTrigger = false, angelDeath = false;
    public Death deathScript;

    [SerializeField]
    private bool triggered = false, repeatable = false;

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player") && touchTrigger && other.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId && (!triggered || repeatable)) {
            //deathScript.Jumpscare(angelDeath);
            triggered = true;
            other.GetComponent<Death>().LoseLife(!angelDeath);
        }    
    }

}
