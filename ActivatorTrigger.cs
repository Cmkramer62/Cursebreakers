using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivatorTrigger : MonoBehaviour {
    public ActivatorSwitch activatorScript;
    public LightFlicker lightScriptDirect;

    private void OnTriggerEnter(Collider other) {
        if(other.name == "Ghost Enemy" && activatorScript != null && activatorScript.state) {
            activatorScript.Activate();
        }
        else if(other.name == "Ghost Enemy" && lightScriptDirect != null && lightScriptDirect.alive) {
            lightScriptDirect.TurnOffLight(false);
        }
    }
}
