using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {

    public SerializableDictionary<string, int> inventoryDictionary;
    public GameObject inventoryParent;
    public GameObject invPrefab;

    public List<Sprite> inventoryItems;

    // Start is called before the first frame update
    void Start() {
        if(inventoryDictionary == null) inventoryDictionary = new SerializableDictionary<string, int>(); // needed?
        LoadData();
    }

    public void AddItem(string itemName) {
        if(inventoryDictionary.ContainsKey(itemName)) inventoryDictionary[itemName]++;
        else inventoryDictionary.Add(itemName, 1);
    }

    public void RemoveItem(string itemName) {
        if(inventoryDictionary.ContainsKey(itemName)) {
            inventoryDictionary[itemName]--;
            if(inventoryDictionary[itemName] < 0) inventoryDictionary[itemName] = 0;
        }
    }

    public void RenderInventory() {
        for(int i = 0; i < inventoryDictionary.Count; i++) {

        }
    }

    public void SaveData() { // only save game on exiting current scene. That way if player exits and loads back,
        // they will not come back to the saved items existing in inventory and world.
        List<string> tempItems = new List<string>();
        List<string> tempAmounts = new List<string>();

        foreach(var pair in inventoryDictionary) {
            tempItems.Add(pair.Key);
            tempAmounts.Add(pair.Value.ToString());
        }

        Debug.Log(string.Join('_', tempItems));
        Debug.Log(string.Join('_', tempAmounts));


        PlayerPrefs.SetString("invNames", string.Join('_', tempItems));
        PlayerPrefs.SetString("invAmounts", string.Join('_', tempAmounts));
    }

    public void LoadData() {
        string[] temp1 = PlayerPrefs.GetString("invNames").Split('_');
        string[] temp2 = PlayerPrefs.GetString("invAmounts").Split('_');

        inventoryDictionary.Clear();
        inventoryDictionary = new SerializableDictionary<string, int>();
        
        for(int i = 0; i < temp1.Length; i++) {
           // Debug.Log(temp1[i] + " " + temp1[i].GetType());
            //Debug.Log(temp2[i] + " " + temp2[i].GetType());
            inventoryDictionary.Add(temp1[i], int.Parse(temp2[i]));
        }
    }

}
