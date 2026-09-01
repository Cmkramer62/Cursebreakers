using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class CurseGameManagerClient : MonoBehaviour {

    private CurseGameManager serverCurseManager;

    public Image goalCurseImage;
    public TextMeshProUGUI goalCurseText;
    public Sprite[] curseTypeSprites;
    public ObjectivesUI objectivesScript;

    public List<GameObject> spawnPoints = new();

    // Also a child of this object. Client-side only. Stored here and used as reference. Don't delete.
    public AudioSource musicSource, musicAlternate;

    IEnumerator Start() {

        // Wait until the timer exists on this client
        while(serverCurseManager == null) {
            serverCurseManager = FindFirstObjectByType<CurseGameManager>();
            yield return null;
        }

        // Subscribe to changes
        //serverCurseManager.goalCurseTrackedID.OnValueChanged += OnObjectChanged;
        serverCurseManager.goalCurseIndex.OnValueChanged += OnNewCurseSpawn;
        serverCurseManager.latestFalseCurseIndex.OnValueChanged += OnNewCurseSpawn;

        OnNewCurseSpawn(0, serverCurseManager.goalCurseIndex.Value);
        OnNewCurseSpawn(0, serverCurseManager.latestFalseCurseIndex.Value);
    
    }

    [ContextMenu("Populate Spawn Points")]
    private void PopulateSpawnPoints() {
        spawnPoints.Clear();

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("CurseSpawn")) {
            spawnPoints.Add(obj);
        }

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
    #endif
    }

    void OnNewCurseSpawn(int oldIndex, int newIndex) {
        Debug.Log("Saw index change. " + newIndex);
        RemovePropItem(newIndex);

        GameObject potentialGoalCurse = null;
        if(serverCurseManager.goalCurse.Value.TryGet(out NetworkObject networkObject)) {
            potentialGoalCurse = networkObject.gameObject;
        }
        if(potentialGoalCurse != null) {
            goalCurseImage.sprite = curseTypeSprites[potentialGoalCurse.GetComponentInChildren<CursedObject>().cursesList[0]]; // First curse reveal.
            goalCurseText.text = curseTypeSprites[potentialGoalCurse.GetComponentInChildren<CursedObject>().cursesList[0]].name.Split(' ')[1];
            objectivesScript.SetFreebieTrait(potentialGoalCurse.GetComponentInChildren<CursedObject>().cursesList[0]);
        }
        else Debug.Log("ERROR IN C-GAME MANAGER, COULD NOT GET GOALCURSE.");

    }

    private void RemovePropItem(int i) {
        Debug.Log("--Called prop removal. index: " + i + ". spawnpoints[i].name = " + spawnPoints[i].transform.gameObject.name);
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
