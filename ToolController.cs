using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ToolController : NetworkBehaviour {

    public bool cycleCooldown = false, masterAllowed = true;
    public bool allowedToCycle = true;
    public Animator swapperAnimator;
    public GameObject[] playerItemMeshes, toolbarUI, toolbarMarkerUI;

    public NetworkVariable<int> heldIndex = new NetworkVariable<int>();

    public AudioSource source;
    public AudioClip swapClip;

    public List<CursedObject> objectsList = new List<CursedObject>();
    public int defaultEMF = 0, defaultTemp = 60;

    public Flashlight geistLightScript;
    public CameraFlash cameraScript;
    public Thermometer thermometerScript;
    [SerializeField] private PlayerHandler playerHandlerScript;

    public override void OnNetworkSpawn() {
        heldIndex.OnValueChanged += OnHeldIndexChanged;
    }

    void OnHeldIndexChanged(int oldValue, int newValue) {
        // play animation here instead of ClientRpc
        //swapperAnimator.SetTrigger("SwapTrigger");
        swapperAnimator.GetComponent<ClientNetworkAnimator>().SetTrigger("SwapTrigger");
        StartCoroutine(AnimationTimer(newValue));
    }

    private void Start() {
        if(!IsServer) {
            return;
        }

        InvokeRepeating("UpdateTemp", 0, Random.Range(0.5f, 1));
        InvokeRepeating("UpdateEMF", 0, Random.Range(1, 4));
        InvokeRepeating("CheckHolyWater", 0, Random.Range(2, 4));
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) {
            return;
        }

        if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetAxis("Mouse ScrollWheel") > 0f) CycleDownServerRpc();
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetAxis("Mouse ScrollWheel") < 0f) CycleUpServerRpc();
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha1)) CycleToServerRpc(0);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha2)) CycleToServerRpc(1);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha3)) CycleToServerRpc(2);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha4)) CycleToServerRpc(3);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha5)) CycleToServerRpc(4);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha6)) CycleToServerRpc(5);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha7)) CycleToServerRpc(6);
        else if(!cycleCooldown && masterAllowed && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha8)) CycleToServerRpc(7);

        geistLightScript.GeistLightUIUpdate();
        cameraScript.CameraUIUpdate();
        thermometerScript.ThermometerStatusAndUIUpdate();
    }

    #region Hand Toolbelt Functions
    [ServerRpc]
    public void CycleUpServerRpc() {
        if(playerItemMeshes[3].activeSelf) playerItemMeshes[3].GetComponent<Scanner>().allowedToScan = false;
        if(playerItemMeshes[5].activeSelf) playerItemMeshes[5].GetComponent<Thermometer>().allowedToScan = false;
        source.PlayOneShot(swapClip);
        bool found = false;
        for(int i = heldIndex.Value + 1; i < playerItemMeshes.Length; i++) {
            found = true;
            //
            //StartCoroutine(AnimationTimer(playerItemMeshes[heldIndex.Value], playerItemMeshes[i], heldIndex.Value, i));
            heldIndex.Value = i;
            break;
        }
        if(!found) {
            for(int i = 0; i < heldIndex.Value; i++) {
                //swapperAnimator.Play("SwapAnim");
                //StartCoroutine(AnimationTimer(playerItemMeshes[heldIndex.Value], playerItemMeshes[i], heldIndex.Value, i));
                heldIndex.Value = i;
                break;
            }
        }
    }

    [ServerRpc]
    public void CycleDownServerRpc() {
        if(playerItemMeshes[3].activeSelf) playerItemMeshes[3].GetComponent<Scanner>().allowedToScan = false;
        if(playerItemMeshes[5].activeSelf) playerItemMeshes[5].GetComponent<Thermometer>().allowedToScan = false;
        source.PlayOneShot(swapClip);
        bool found = false;
        if(heldIndex.Value != 0) {
            for(int i = heldIndex.Value - 1; i >= 0; i--) {
                found = true;
                //swapperAnimator.Play("SwapAnim");
                //StartCoroutine(AnimationTimer(playerItemMeshes[heldIndex.Value], playerItemMeshes[i], heldIndex.Value, i));
                heldIndex.Value = i;
                break;
            }
        }
        if(!found) {
            for(int i = playerItemMeshes.Length - 1; i > heldIndex.Value; i--) {
                //swapperAnimator.Play("SwapAnim");
                //StartCoroutine(AnimationTimer(playerItemMeshes[heldIndex.Value], playerItemMeshes[i], heldIndex.Value, i));
                heldIndex.Value = i;
                break;
            }
        }
    }

    [ServerRpc]
    public void CycleToServerRpc(int to) {
        if(heldIndex.Value == to) return;

        if(playerItemMeshes[3].activeSelf) playerItemMeshes[3].GetComponent<Scanner>().allowedToScan = false;
        if(playerItemMeshes[5].activeSelf) playerItemMeshes[5].GetComponent<Thermometer>().allowedToScan = false;

        source.PlayOneShot(swapClip);
        //swapperAnimator.Play("SwapAnim");

        //StartCoroutine(AnimationTimer(playerItemMeshes[heldIndex.Value], playerItemMeshes[to], heldIndex.Value, to));
        heldIndex.Value = to;
    }

    private IEnumerator AnimationTimer(int newIndex) {
        StartCoroutine(ToolbeltCooldown());
        yield return new WaitForSeconds(.35f);
        //toolbarUI[from].transform.localScale = new Vector3(.35f, .35f, .35f);
        //toolbarUI[from].GetComponent<CanvasGroup>().alpha = .4f;
        //toolbarMarkerUI[from].SetActive(false);

        //toolbarUI[to].transform.localScale = new Vector3(.37f, .37f, .37f);
        //toolbarUI[to].GetComponent<CanvasGroup>().alpha = 1f;
        //toolbarMarkerUI[to].SetActive(true);

        //swapFrom.SetActive(false);
        //swapTo.SetActive(true);
        for(int i = 0; i < playerItemMeshes.Length; i++) {
            playerItemMeshes[i].SetActive(i == newIndex);
        }
    }

    public void ForceToBarehand() {
        CycleToServerRpc(0);
        allowedToCycle = false;
    }

    public void ForceToPrevhand(int index) {
        allowedToCycle = true;
        CycleToServerRpc(index);
    }

    private IEnumerator ToolbeltCooldown() {
        cycleCooldown = true;
        yield return new WaitForSeconds(0.5f);
        cycleCooldown = false;
    }

    #endregion

    private void UpdateTemp() {
        // check distances of all cursedObjects in our radius that have thermo.
        // find the one that has the shortest distance to us.
        // set the thermometer's goal temp to be that cursedObject's temperature + the distance. ie. if the goalTemp is 0, but we are 10 away, it reads 10.
        float smallestDistance = 100f;
        int distTemp = -99;

        foreach(CursedObject curse in objectsList) {
            //if(curse.cursesList.Contains(CursedObject.CursedTypes.Thermo)) {
                float curseDistance = Vector3.Distance(playerItemMeshes[5].transform.position, curse.transform.position);
            //Debug.Log("Distance to " + curse.transform.parent.name + " is " + curseDistance);
            if(curseDistance < smallestDistance) {
                distTemp = curse.temperature + (int)curseDistance;
                smallestDistance = curseDistance;
            }
            //}
        }

        if(distTemp != -99) playerItemMeshes[5].GetComponent<Thermometer>().goalTemp = distTemp;
        else playerItemMeshes[5].GetComponent<Thermometer>().goalTemp = defaultTemp;
    }

    private void UpdateEMF() {
        //playerItemMeshes[3].GetComponent<Scanner>().levelEMF = defaultEMF;
    }

    private void CheckHolyWater() {
        bool anyActive = false;
        //if(playerItemMeshes[6].activeSelf) {
        foreach(CursedObject curse in objectsList) {
            if(curse.cursesList.Contains(CursedObject.CursedTypes.Unholy)) {
                playerItemMeshes[6].GetComponent<HolyWater>().TurnSteam(true);
                anyActive = true;
            }
        }
        if(!anyActive) playerItemMeshes[6].GetComponent<HolyWater>().TurnSteam(false);
        //}
    }
}
