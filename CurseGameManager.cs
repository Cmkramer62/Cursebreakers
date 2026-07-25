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
    private GameObject ghostReference;

    public int oddsSpawnRate = 3, curseSpawnBufferMax = 6, curseSpawnBuffer = 0;

    public NetworkVariable<int> goalCurseIndex, latestFalseCurseIndex = new NetworkVariable<int>(-1);
    public NetworkVariable<ulong> goalCurseTrackedID = new NetworkVariable<ulong>();
    public GameObject goalCurse;

    //public Animator ghostAnimator;
    public RuntimeAnimatorController floatingController;
    //public GameObject ghostGeistParticles;
    //public GameObject[] ghostHorns;
    public Bell bellScript;

    //public GameObject[] enviroParticles;
    //public GhostRandomizer ghostRandomizer;

    public int timeSpent = 0, livesLeft = 3, timeSpotted = 0, longestChase = 0, purifyState = 0;

    private CurseGameManagerClient curseManagerClientScript;

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
        goalCurse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)], curseManagerClientScript.spawnPoints[goalCurseIndex.Value].transform);
        goalCurse.GetComponent<NetworkObject>().Spawn();

        //goalCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
        goalCurse.GetComponentInChildren<CursedObject>().SetRandomGoal(); // set the curses to be a random 3.

        goalCurseTrackedID.Value = goalCurse.GetComponent<NetworkObject>().NetworkObjectId;
        // Freebie is found and handled on client-side manager script, ONLY after the networked cursedObject is given its curses.

        goalCurse.name = "Goal Curse";

        // Spawn in ghost before the curses are revealed.
        ghostReference = GameObject.Instantiate(ghostPrefab); // where?
        ghostReference.GetComponent<NetworkObject>().Spawn();

        ApplyCursedAura(); // Second curse reveal.
        ApplyCursedEnvironment(); // Third curse reveal.

       // RemovePropItem(goalCurseIndex);
        for (int i = 0; i < curseManagerClientScript.spawnPoints.Count; i++) {
            if(i != goalCurseIndex.Value) {
                if(curseSpawnBuffer >= curseSpawnBufferMax) {
                    if(UnityEngine.Random.Range(0, oddsSpawnRate) == 0) {
                        GameObject newCurse = GameObject.Instantiate(cursedObjectPrefabs[UnityEngine.Random.Range(0, cursedObjectPrefabs.Length)], curseManagerClientScript.spawnPoints[i].transform);
                        Debug.Log("--Spawned in new non-goal curse: " + newCurse.name);
                        newCurse.GetComponent<NetworkObject>().Spawn();
                        Debug.Log("--Spawn in new non-goal curse.");

                        //newCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
                        newCurse.GetComponentInChildren<CursedObject>().curseGameManager = this;
                        newCurse.GetComponentInChildren<CursedObject>().SetRandomCurses();
                        Debug.Log("--Spawn in new non-goal curse.");

                        // set random number of curses
                        curseSpawnBuffer = 0;
                        Debug.Log("--About to call remove with: " + i);
                        latestFalseCurseIndex.Value = i;
                     //   RemovePropItem(i);
                    }
                }
                else curseSpawnBuffer++;
            }
        }
    }

    private void ApplyCursedEnvironment() {
        var goalCurseSpecific = goalCurse.GetComponentInChildren<CursedObject>().cursesList[1];
        
        //enviroParticles[0].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Glowing);
        //enviroParticles[1].SetActive(goalCurseSpecific == CursedObject.CursedTypes.EMF);
        //enviroParticles[2].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Aura);
        //enviroParticles[3].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Thermo);
        //enviroParticles[4].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Unholy);

        //if(goalCurseSpecific == CursedObject.CursedTypes.Sound) bellScript.ghostSearchWithSound = true;
    }
    
    private void ApplyCursedAura() {
        var goalCurseSpecific = goalCurse.GetComponentInChildren<CursedObject>().cursesList[2];
        if(goalCurseSpecific == (int)CursedObject.CursedTypes.Glowing) {
            //ghostGeistParticles.SetActive(true);
            //ghostAnimator.transform.parent.gameObject.GetComponent<Enemy>().geistAura = true;
        }
        else if(goalCurseSpecific == (int)CursedObject.CursedTypes.EMF) {
            //ghostAnimator.runtimeAnimatorController = floatingController;
        }
        else if(goalCurseSpecific == (int)CursedObject.CursedTypes.Aura) {
            //ghostRandomizer.overrideEyes = true;
        }
        else if(goalCurseSpecific == (int)CursedObject.CursedTypes.Thermo) {
            //ghostAnimator.transform.parent.gameObject.GetComponent<Enemy>().freezingAura = true;
        }
        else if(goalCurseSpecific == (int)CursedObject.CursedTypes.Unholy) {
            //foreach(GameObject horns in ghostHorns) {
            //    horns.SetActive(true);
            //}
        }
        else {
            //bellScript.ghostSearchWithSound = true;
        }
        // This will need to change. Each client will do it differently.
        ghostReference.GetComponent<GhostRandomizer>().RandomizeGhost();
    }

    
    
}
