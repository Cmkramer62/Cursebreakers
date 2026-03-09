using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurseGameManager : MonoBehaviour {

    public List<GameObject> spawnPoints = new List<GameObject>();
    public GameObject[] cursedObjectPrefabs;
    public int oddsSpawnRate = 3, goalCurseIndex, curseSpawnBufferMax = 6, curseSpawnBuffer = 0;

    public GameObject goalCurse;

    public Image goalCurseImage;
    public TextMeshProUGUI goalCurseText;
    public Sprite[] curseTypeSprites;

    public Animator ghostAnimator;
    public RuntimeAnimatorController floatingController;
    public GameObject ghostGeistParticles;
    public GameObject[] ghostHorns;
    public Bell bellScript;

    public GameObject[] enviroParticles;
    public GhostRandomizer ghostRandomizer;
    public ObjectivesUI objectivesScript;
    //public ParticleSystem auraEnviroParticles;
    
    // Start is called before the first frame update
    void Start() {

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("CurseSpawn")) {
            spawnPoints.Add(obj);
        }
        goalCurseIndex = Random.Range(0, spawnPoints.Count);

        // goal curse section. goalCurseIndex used to be 'i'
        goalCurse = GameObject.Instantiate(cursedObjectPrefabs[Random.Range(0, cursedObjectPrefabs.Length)], spawnPoints[goalCurseIndex].transform);
        goalCurse.GetComponentInChildren<CursedObject>().toolControllerScript = GetComponent<ToolController>();
        goalCurse.GetComponentInChildren<CursedObject>().SetRandomGoal(); // set the curses to be random 3.
        goalCurse.name = "Goal Curse";
        goalCurseImage.sprite = curseTypeSprites[goalCurse.GetComponentInChildren<CursedObject>().index[0]]; // First curse reveal.
        goalCurseText.text = curseTypeSprites[goalCurse.GetComponentInChildren<CursedObject>().index[0]].name;
        objectivesScript.SetFreebieTrait(goalCurse.GetComponentInChildren<CursedObject>().index[0]);

        ApplyCursedAura(); // Second curse reveal.
        ApplyCursedEnvironment(); // Third curse reveal.

        RemovePropItem(goalCurseIndex);
        for (int i = 0; i < spawnPoints.Count; i++) {
            if(i != goalCurseIndex) {
                if(curseSpawnBuffer >= curseSpawnBufferMax) {
                    if(Random.Range(0, oddsSpawnRate) == 0) {
                        GameObject newCurse = GameObject.Instantiate(cursedObjectPrefabs[Random.Range(0, cursedObjectPrefabs.Length)], spawnPoints[i].transform);
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

    public void ApplyCursedEnvironment() {
        //foreach(var goalCurseSpecific in goalCurse.GetComponentInChildren<CursedObject>().cursesList) {

        //}
        var goalCurseSpecific = goalCurse.GetComponentInChildren<CursedObject>().cursesList[1];
        
        enviroParticles[0].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Glowing);
        enviroParticles[1].SetActive(goalCurseSpecific == CursedObject.CursedTypes.EMF);
        enviroParticles[2].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Aura);
        enviroParticles[3].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Thermo);
        enviroParticles[4].SetActive(goalCurseSpecific == CursedObject.CursedTypes.Unholy);

        if(goalCurseSpecific == CursedObject.CursedTypes.Sound) bellScript.ghostSearchWithSound = true;

    }
    
    public void ApplyCursedAura() {
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

    // Update is called once per frame
    void Update() {

    }
}
