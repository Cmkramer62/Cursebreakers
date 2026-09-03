using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;

public class CurseGameManager : NetworkBehaviour {

    //public NetworkVariable<List<GameObject>> spawnPoints = new NetworkVariable<List<GameObject>>();
    [SerializeField] private GameObject[] cursedObjectPrefabs;
    [SerializeField] private GameObject ghostPrefab;
    public GameObject ghostReference;

    public int oddsSpawnRate = 3, curseSpawnBufferMax = 6, curseSpawnBuffer = 0;

    public NetworkVariable<int> goalCurseIndex, latestFalseCurseIndex = new NetworkVariable<int>(-1);
    //public NetworkVariable<ulong> goalCurseTrackedID = new NetworkVariable<ulong>();
    public NetworkVariable<NetworkObjectReference> goalCurse =
        new NetworkVariable<NetworkObjectReference>();


    //public Animator ghostAnimator;
    public RuntimeAnimatorController floatingController;
    //public GameObject ghostGeistParticles;
    //public GameObject[] ghostHorns;
    public Bell bellScript;

    //public GameObject[] enviroParticles;
    //public GhostRandomizer ghostRandomizer;

    public int timeSpent = 0, timeSpotted = 0, longestChase = 0, purifyState = 0;

    private CurseGameManagerClient curseManagerClientScript;
    public bool spawnGhost = true;

    private void OnClientConnected(ulong clientId) {
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        // Now you have the player's NetworkObject. Needed?
    }

    public override void OnNetworkSpawn() {
        // spawnPoints = GameObject.FindGameObjectsWithTag("CurseSpawn");
        curseManagerClientScript = GameObject.Find("Client Curse Game Manager").GetComponent<CurseGameManagerClient>();
        if(!IsServer) return;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;

        
        goalCurseIndex.Value = UnityEngine.Random.Range(0, curseManagerClientScript.spawnPoints.Count);

        // goal curse section. goalCurseIndex used to be 'i'
        //old goalCurse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)], curseManagerClientScript.spawnPoints[goalCurseIndex.Value].transform);
        //old goalCurse.GetComponent<NetworkObject>().Spawn();
        //  THE 2 ABOVE HAS BEEN CONVERTED TO THE 4 BELOW  \/
        GameObject curse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)],
            curseManagerClientScript.spawnPoints[goalCurseIndex.Value].transform);
        
        curse.name = "Goal Curse";
        NetworkObject networkObject = curse.GetComponent<NetworkObject>();
        networkObject.Spawn();
        goalCurse.Value = networkObject;


        //goalCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
        curse.GetComponentInChildren<CursedObject>().SetRandomGoal(); // set the curses to be a random 3.


        // We don't also have a goalCurseTrackedID anymore. goalCurseTrackedID.Value = goalCurse.GetComponent<NetworkObject>().NetworkObjectId;
        // Freebie is found and handled on client-side manager script, ONLY after the networked cursedObject is given its curses.


        // Spawn in ghost before the curses are revealed.
        if(spawnGhost) {
            ghostReference = GameObject.Instantiate(ghostPrefab); // where?
            ghostReference.GetComponent<GhostRandomizer>().serverGameManagerScript = this;

            ghostReference.GetComponent<NetworkObject>().Spawn();
            ghostReference.GetComponent<Enemy>().musicSource = curseManagerClientScript.musicSource;
            ghostReference.GetComponent<Enemy>().allowedToMove.Value = true;
        }

        // RemovePropItem(goalCurseIndex);
        for (int i = 0; i < curseManagerClientScript.spawnPoints.Count; i++) {
            if(i != goalCurseIndex.Value) {
                if(curseSpawnBuffer >= curseSpawnBufferMax) {
                    if(UnityEngine.Random.Range(0, oddsSpawnRate) == 0) {
                        GameObject newCurse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)], curseManagerClientScript.spawnPoints[i].transform);
                        //Debug.Log("--Spawned in new non-goal curse: " + newCurse.name);
                        newCurse.GetComponent<NetworkObject>().Spawn();
                        //Debug.Log("--Spawn in new non-goal curse.");

                        //newCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
                        newCurse.GetComponentInChildren<CursedObject>().curseGameManager = this;
                        newCurse.GetComponentInChildren<CursedObject>().SetRandomCurses();
                        //Debug.Log("--Spawn in new non-goal curse.");

                        // set random number of curses
                        curseSpawnBuffer = 0;
                        //Debug.Log("--About to call remove with: " + i);
                        latestFalseCurseIndex.Value = i;
                     //   RemovePropItem(i);
                    }
                }
                else curseSpawnBuffer++;
            }
        }
    }

}
