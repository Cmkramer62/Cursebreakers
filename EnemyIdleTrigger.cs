using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Have this script and object be spawned in by the ghost OnNetworkSpawn(). (only if server?)
// Assign enemyScript to it, from Enemy directly.

// What does this actually do again?
public class EnemyIdleTrigger : MonoBehaviour {

    [HideInInspector] public Enemy enemyScript;

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Ghost") && enemyScript.SeenAndClosestPlayer().GetComponent<PlayerMovement>().isHiding) {
            enemyScript.chaseMeter = 100f;
        }

    }
}
