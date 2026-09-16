using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System;

public class GameTimer : NetworkBehaviour {

    public NetworkVariable<int> timeLeft = new NetworkVariable<int>(60);
    public NetworkVariable<bool> isPaused = new NetworkVariable<bool>(false);
    //public Death deathScript;

    private float timer;
    private float tickRate = 1f;
    //[HideInInspector] public event Action OnGameOver;

    //[ClientRpc]
    //void GameOverClientRpc() {
    //    OnGameOver?.Invoke(); // notify local listeners
   // }
    

    private void Update() {
        if(!IsServer || isPaused.Value) return;

        timer += Time.deltaTime;

        if(timer >= tickRate) {
            timer = 0f;

            if(timeLeft.Value > 0)
                timeLeft.Value--;
            else {
                // kill all non-ghosts.
                // this is the server. Use server/s player list. call death on all.
                foreach(var client in NetworkManager.Singleton.ConnectedClientsList) {
                    if(client.PlayerObject != null) {
                        GameObject player = client.PlayerObject.gameObject;
                        if(player != null && player.GetComponent<Death>().lives.Value > 0) {
                            player.GetComponent<Death>().LoseRemainingLives(false);
                        }
                    }
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPauseServerRpc(bool pause) {
        // You can add validation here
        isPaused.Value = pause;
    }

}
