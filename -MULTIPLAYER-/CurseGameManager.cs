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
    public bool spawnGhost = true, dontMoveGhost = false, randomizeCurse = true;
    public GameObject[] spawnPoints;

    
    [Tooltip("Used if randomizeCurse is false.")] public CursedObject.CurseType freebieCurse, enviroCurse, auraCurse;

    private void OnClientConnected(ulong clientId) {
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        // Now you have the player's NetworkObject. Needed?
    }

    public override void OnNetworkSpawn() {
        curseManagerClientScript = GameObject.FindAnyObjectByType<CurseGameManagerClient>();
        
        if(!IsServer) {
            return;
        }

        NetworkManager.OnClientConnectedCallback += OnClientConnected;

        goalCurseIndex.Value = UnityEngine.Random.Range(0, curseManagerClientScript.spawnPoints.Count);

        GameObject curse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)],
            curseManagerClientScript.spawnPoints[goalCurseIndex.Value].transform);
        
        curse.name = "Goal Curse";
        NetworkObject networkObject = curse.GetComponent<NetworkObject>();
        networkObject.Spawn();
        goalCurse.Value = networkObject;

        if(randomizeCurse) {
            curse.GetComponentInChildren<CursedObject>().SetRandomGoal();
        }
        else {
            curse.GetComponentInChildren<CursedObject>().SetSpecificGoal((int)freebieCurse, (int)enviroCurse, (int)auraCurse);
        }
        curse.GetComponentInChildren<CursedObject>().goalCurse.Value = true;

        // Spawn in ghost before the curses are revealed.
        if(spawnGhost) {
            PopulateGhostSpawnPoints();
            ghostReference = GameObject.Instantiate(ghostPrefab);
            StartCoroutine(PlaceGhostWhenReady());
            ghostReference.GetComponent<NetworkObject>().Spawn();
            ghostReference.GetComponent<Enemy>().musicSource = curseManagerClientScript.musicSource;
            ghostReference.GetComponent<Enemy>().allowedToMove.Value = !dontMoveGhost;
        }

        for (int i = 0; i < curseManagerClientScript.spawnPoints.Count; i++) {
            if(i != goalCurseIndex.Value) {
                if(curseSpawnBuffer >= curseSpawnBufferMax) {
                    if(UnityEngine.Random.Range(0, oddsSpawnRate) == 0) {
                        GameObject newCurse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)], curseManagerClientScript.spawnPoints[i].transform);
                        newCurse.GetComponent<NetworkObject>().Spawn();
                        newCurse.GetComponentInChildren<CursedObject>().curseGameManager = this;
                        newCurse.GetComponentInChildren<CursedObject>().SetRandomCurses();
                        curseSpawnBuffer = 0;
                        latestFalseCurseIndex.Value = i;
                     //   RemovePropItem(i);
                    }
                }
                else curseSpawnBuffer++;
            }
        }
    }

    private IEnumerator PlaceGhostWhenReady() {
        NetworkObject ghostObj = null;

        while(ghostObj == null) {
            if(ghostReference != null)
                ghostObj = ghostReference.GetComponent<NetworkObject>();

            yield return null;
        }

        Transform spawn = GetSpawnPoint();

        ghostObj.GetComponent<Enemy>().SetSpawnPositionClientRpc(spawn.position, spawn.rotation);
    }

    private void PopulateGhostSpawnPoints() {
        spawnPoints = GameObject.FindGameObjectsWithTag("GhostSpawnPoint");
        Array.Sort(spawnPoints, (a, b) =>
            string.Compare(a.name, b.name, StringComparison.Ordinal)
        );
    }

    private Transform GetSpawnPoint() {
        int index = UnityEngine.Random.Range(0, spawnPoints.Length);
        return spawnPoints[index].transform;
    }
}
