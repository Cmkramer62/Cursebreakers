using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HolyWater : NetworkBehaviour {


    public NetworkVariable<bool> active = new NetworkVariable<bool>(false);
    public ParticleSystem steamParticle;

    public void OnEnable() {
        if(IsServer) transform.parent.parent.parent.GetComponent<ToolController>().CheckHolyWater();
        if(active.Value) steamParticle.Play();
        /*
                if(active.Value) {
                    steamParticle.Pause();
                    steamParticle.gameObject.SetActive(false);
                    steamParticle.gameObject.SetActive(true);
                    steamParticle.

                }
        */
    }

    public override void OnNetworkSpawn() {
        // Subscribe to changes
        active.OnValueChanged += OnHolyChanged;

        gameObject.SetActive(false);
    }

    public void OnHolyChanged(bool oldValue, bool newValue) {
        TurnSteam(newValue);
    }

    public void TurnSteam(bool state) {
        if(state) steamParticle.Play();
        else steamParticle.Stop();
    }

}
