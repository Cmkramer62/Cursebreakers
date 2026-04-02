using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivatorTrigger : MonoBehaviour {
    public ActivatorSwitch activatorScript;
    public LightFlicker lightScriptDirect;

    [SerializeField] private float distToPlayer = 10f;
    [SerializeField] private int chargesToAdd = 1;
    private Transform[] playerReferences;
    private Enemy ghostScript;
    
    private void Awake() {
        //playerReference = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void OnTriggerEnter(Collider other) {

        if(other.name == "Ghost Enemy" && activatorScript != null && activatorScript.state && other.GetComponent<Enemy>().invisible) {
            ghostScript = other.GetComponent<Enemy>();

            activatorScript.Activate();
            if(AnyPlayerWithinRange() && !other.GetComponentInChildren<ParanormalNoises>().IsPlayingParanormal()) {
                other.GetComponent<Enemy>().IncreaseCharges();
                other.GetComponentInChildren<ParanormalNoises>().StartCooldown();
            }
        }
        else if(other.name == "Ghost Enemy" && lightScriptDirect != null && lightScriptDirect.alive && other.GetComponent<Enemy>().invisible) {
            ghostScript = other.GetComponent<Enemy>();

            if(AnyPlayerWithinRange() && !other.GetComponentInChildren<ParanormalNoises>().IsPlayingParanormal()) {
                other.GetComponent<Enemy>().IncreaseCharges();
                other.GetComponentInChildren<ParanormalNoises>().StartCooldown();
                if(other.GetComponent<Enemy>().invisSpeed >= 7) {
                    lightScriptDirect.BlowUpLight();
                }
                else {
                    lightScriptDirect.TurnOffLight(false);
                }
            }
            else {
                lightScriptDirect.TurnOffLight(false);
            }
        }

        
    }


    private bool AnyPlayerWithinRange() {
        //return Vector3.Distance(playerReference.position, transform.position) < distToPlayer;
        playerReferences = ghostScript.GetComponent<ConeLOSDetector>().PlayerTranforms().ToArray();
        foreach(Transform playerTransform in playerReferences) {
            if(Vector3.Distance(playerTransform.position, transform.position) < distToPlayer) return true;
        }
        return false;
    }
}
