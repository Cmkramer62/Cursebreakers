using Unity.Netcode;
using UnityEngine;
using System;

// This simply functions to spawn in the relevant server scripts.
// Unlike the normal ServerManager, this only spawns in the GameSpawnManager. (Controls player spawn location).
public class ServerHubManager : NetworkBehaviour {

    [SerializeField] private GameObject gameSpawnManager;

    public override void OnNetworkSpawn() {
        if(!IsServer) return;
        if(MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsMatchTransitioning) return;

        StartGame();
    }

    void StartGame() {
        var spawnManagerInstance = Instantiate(gameSpawnManager);
        spawnManagerInstance.GetComponent<NetworkObject>().Spawn();
    }

}
