using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Animations;
using Unity.Netcode;


public class PurificationManager : NetworkBehaviour {

    public List<GameObject> listOfPlayers = new List<GameObject>();
    public NetworkVariable<int> totalLives = new NetworkVariable<int>(0);
    //  public int totalLives = 0;

    public override void OnNetworkSpawn() {
        if(!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        RefreshPlayerList();
    }

    private void OnDestroy() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void RefreshPlayerList() {
        listOfPlayers.Clear();

        foreach(var client in NetworkManager.Singleton.ConnectedClientsList) {
            if(client.PlayerObject != null) {
                GameObject player = client.PlayerObject.gameObject;
                if(player != null) listOfPlayers.Add(player);
            }
        }
        UpdateCurrentLivesServerRpc();
    }

    private void OnClientConnected(ulong clientId) {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if(playerObject != null && !listOfPlayers.Contains(playerObject.gameObject))
            listOfPlayers.Add(playerObject.gameObject);
        
        Death deathScriptOfNewPlayer = playerObject.gameObject.GetComponent<Death>();
        totalLives.Value += deathScriptOfNewPlayer.lives.Value;
    }

    // When a client disconnects, remove all null entries in listOfPlayers,
    // but also remove any clientID in the list that match whomever disconnected.
    // (Done because their disconnect can fire before player destruction!
    private void OnClientDisconnected(ulong clientId) {
        listOfPlayers.RemoveAll(player => player == null ||
            player.GetComponent<NetworkObject>().OwnerClientId == clientId);

        UpdateCurrentLivesServerRpc();
    }
     
    // Does not need server rpc, only called by server scripts. Upon host/server triggering last IF
    // statement, the client will get kicked along with host.
    [ServerRpc (RequireOwnership = false)]
    public void UpdateCurrentLivesServerRpc() {
        int currentLives = 0;

        foreach(GameObject player in listOfPlayers) {
            currentLives += player.GetComponent<Death>().lives.Value;
        }

        totalLives.Value = currentLives;

        // This is called by life loss and life gain.
        // Life gain does not do anything. But upon life loss, game could end.

        if(totalLives.Value == 0) {
            // This will only be run and called by the Host/server.
            // Will that force everyone out?
            MultiplayerManager.Instance.LeaveGame();
        }
    }
}
