using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System;

public class GameSpawnManager : NetworkBehaviour {
    public GameObject[] spawnPoints;

    public override void OnNetworkSpawn() {
        if(!IsServer)
            return;

        PopulateSpawnPoints();

        NetworkManager.OnClientConnectedCallback += OnClientConnected;

        StartCoroutine(PlaceExistingPlayers());
    }


    private void OnDestroy() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId) {
        StartCoroutine(PlacePlayerWhenReady(clientId));
    }

    private IEnumerator PlaceExistingPlayers() {
        yield return null;

        foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            yield return PlacePlayerWhenReady(clientId);
        }
    }

    private IEnumerator PlacePlayerWhenReady(ulong clientId) {
        NetworkObject playerObj = null;

        while(playerObj == null) {
            playerObj = NetworkManager.Singleton.SpawnManager
                .GetPlayerNetworkObject(clientId);

            yield return null;
        }

        ClientRpcParams rpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new[] { clientId }
            }
        };

       // playerObj.GetComponent<CharacterController>().enabled = false;
        Transform spawn = GetSpawnPoint(clientId);
        //playerObj.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
        playerObj.GetComponent<PlayerHandler>().SetSpawnPositionClientRpc(spawn.position, spawn.rotation, rpcParams);

        //playerObj.GetComponent<CharacterController>().enabled = true;

    }


    private void PopulateSpawnPoints() {
        Debug.Log("SPAWN-Server-PopulateSpawnPoints.");
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        Array.Sort(spawnPoints, (a, b) =>
            string.Compare(a.name, b.name, StringComparison.Ordinal)
        );
    }

    private Transform GetSpawnPoint(ulong clientId) {
        int index = (int)(clientId % (ulong)spawnPoints.Length);
        return spawnPoints[index].transform;
    }

}