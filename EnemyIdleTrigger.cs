using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIdleTrigger : MonoBehaviour {

    [SerializeField] private Enemy enemyScript;

    private void OnTriggerEnter(Collider other) {
        if(other.name == "Ghost Enemy" && enemyScript.playerTransform.GetComponent<PlayerMovement>().isHiding) {
            Debug.Log("Trigger");
            enemyScript.chaseMeter = 100f;
        }

    }
}
