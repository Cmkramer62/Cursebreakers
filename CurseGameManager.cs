using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class CurseGameManager : NetworkBehaviour {
    
    //public NetworkVariable<List<GameObject>> spawnPoints = new NetworkVariable<List<GameObject>>();
    public List<GameObject> spawnPoints = new List<GameObject>();
    public GameObject[] cursedObjectPrefabs;
    public int oddsSpawnRate = 3, goalCurseIndex, curseSpawnBufferMax = 6, curseSpawnBuffer = 0;

    public NetworkVariable<ulong> goalCurseTrackedID = new NetworkVariable<ulong>();
    public GameObject goalCurse;



    public Animator ghostAnimator;
    public RuntimeAnimatorController floatingController;
    public GameObject ghostGeistParticles;
    public GameObject[] ghostHorns;
    public Bell bellScript;

    public GameObject[] enviroParticles;
    public GhostRandomizer ghostRandomizer;

    public int timeSpent = 0, livesLeft = 3, timeSpotted = 0, longestChase = 0, purifyState = 0;
    // purified, not purified, incorrect object purified

    // Start is called before the first frame update
    public override void OnNetworkSpawn() {
        if(!IsServer) return;

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("CurseSpawn")) {
            spawnPoints.Add(obj);
        }
        goalCurseIndex = Random.Range(0, spawnPoints.Count);

        // goal curse section. goalCurseIndex used to be 'i'
        goalCurse = GameObject.Instantiate(cursedObjectPrefabs[Random.Range(0, cursedObjectPrefabs.Length)], spawnPoints[goalCurseIndex].transform);
        goalCurse.GetComponent<NetworkObject>().Spawn();

        goalCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
        goalCurse.GetComponentInChildren<CursedObject>().SetRandomGoal(); // set the curses to be a random 3.

        goalCurseTrackedID.Value = goalCurse.GetComponent<NetworkObject>().NetworkObjectId;
        // Freebie is found and handled on client-side manager script, ONLY after the networked cursedObject is given its curses.

        goalCurse.name = "Goal Curse";
        ApplyCursedAura(); // Second curse reveal.
        ApplyCursedEnvironment(); // Third curse reveal.

        RemovePropItem(goalCurseIndex);
        for (int i = 0; i < spawnPoints.Count; i++) {
            if(i != goalCurseIndex) {
                if(curseSpawnBuffer >= curseSpawnBufferMax) {
                    if(Random.Range(0, oddsSpawnRate) == 0) {
                        GameObject newCurse = GameObject.Instantiate(cursedObjectPrefabs[Random.Range(0, cursedObjectPrefabs.Length)], spawnPoints[i].transform);
                        newCurse.GetComponent<NetworkObject>().Spawn();

                        newCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
                        newCurse.GetComponentInChildren<CursedObject>().SetRandomCurses();
                        // set random number of curses
                        curseSpawnBuffer = 0;
                        RemovePropItem(i);
                    }
                }
                else curseSpawnBuffer++;
            }
        }
    }

    private void ApplyCursedEnvironment() {
        var goalCurseSpecific = goalCurse.GetComponentInChildren<CursedObject>().cursesList[1];
        
        enviroParticles[0].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Glowing);
        enviroParticles[1].SetActive(goalCurseSpecific == CursedObject.CursedTypes.EMF);
        enviroParticles[2].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Aura);
        enviroParticles[3].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Thermo);
        enviroParticles[4].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Unholy);

        if(goalCurseSpecific == CursedObject.CursedTypes.Sound) bellScript.ghostSearchWithSound = true;
    }
    
    private void ApplyCursedAura() {
        var goalCurseSpecific = goalCurse.GetComponentInChildren<CursedObject>().cursesList[2];
        if(goalCurseSpecific == CursedObject.CursedTypes.Glowing) {
            ghostGeistParticles.SetActive(true);
            ghostAnimator.transform.parent.gameObject.GetComponent<Enemy>().geistAura = true;
        }
        else if(goalCurseSpecific == CursedObject.CursedTypes.EMF) {
            ghostAnimator.runtimeAnimatorController = floatingController;
        }
        else if(goalCurseSpecific == CursedObject.CursedTypes.Aura) {
            ghostRandomizer.overrideEyes = true;
        }
        else if(goalCurseSpecific == CursedObject.CursedTypes.Thermo) {
            ghostAnimator.transform.parent.gameObject.GetComponent<Enemy>().freezingAura = true;
        }
        else if(goalCurseSpecific == CursedObject.CursedTypes.Unholy) {
            foreach(GameObject horns in ghostHorns) {
                horns.SetActive(true);
            }
        }
        else {
            bellScript.ghostSearchWithSound = true;
        }
        ghostRandomizer.RandomizeGhost();
    }

    private void RemovePropItem(int i) {
        if(spawnPoints[i].transform.gameObject.name == "Spawnpoint A1") {
            spawnPoints[i].transform.parent.transform.Find("Item A1").gameObject.SetActive(false);
        }
        else if(spawnPoints[i].transform.gameObject.name == "Spawnpoint A2") {
            spawnPoints[i].transform.parent.transform.Find("Item A2").gameObject.SetActive(false);
        }
        else if(spawnPoints[i].transform.gameObject.name == "Spawnpoint A3") {
            spawnPoints[i].transform.parent.transform.Find("Item A3").gameObject.SetActive(false);
        }
    }
    
}
