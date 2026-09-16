using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/*
 * ToolController acts as both owner-only input for selecting tools and
 * a synchronized manager of various tool variables.
 * This exists on each player themself. (4 in total if across 2 players/systems).
 */
public class ToolController : NetworkBehaviour {

    public bool cycleCooldown = false, playerAlive = true;
    public bool allowedToCycle = true;
    public Animator swapperAnimator;
    public GameObject[] playerItemMeshes;

    public NetworkVariable<int> heldIndex = new NetworkVariable<int>();

    public AudioSource source;
    public AudioClip swapClip;

    // cursedObjectsWithinRange are only the CO's that the player has touched their interaction radius.
    public List<CursedObject> cursedObjectsWithinRange = new List<CursedObject>();

    private CursedObject[] listOfAllCurses;
    public NetworkVariable<int> defaultEMF = new NetworkVariable<int>(0), 
        defaultTemp = new NetworkVariable<int>(60);
    public float averageTemperature = -1;

    public Flashlight geistLightScript;
    public SpellFulgor cameraScript;
    public Thermometer thermometerScript;
    [SerializeField] private PlayerHandler playerHandlerScript;

    // Arm Variables
    [SerializeField] private SkinnedMeshRenderer[] armMeshRenderers;
    [SerializeField] private Animator firstPersonArmsAnimator;

    private Transform algorCrystalTransform;

    public override void OnNetworkSpawn() {
        heldIndex.OnValueChanged += OnHeldIndexChanged;

        if(!IsOwner) {
            foreach(SkinnedMeshRenderer meshRenderObj in armMeshRenderers) {
                meshRenderObj.enabled = false;
            }
        }
    }

    void OnHeldIndexChanged(int oldValue, int newValue) {
        // play animation here instead of ClientRpc
        // WAS REAL ONE // swapperAnimator.GetComponent<ClientNetworkAnimator>().SetTrigger("SwapTrigger");
        StartCoroutine(ToolbeltSwap(newValue));
    }

    private void Start() {
        if(!IsServer) {
            return;
        }
        algorCrystalTransform = playerItemMeshes[5].transform.GetChild(0).transform;
        InvokeRepeating("UpdateEMF", 0, 2);
       // InvokeRepeating("CheckHolyWater", 0, Random.Range(2, 4));
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) {
            return;
        }

        UpdateTemp();

        if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetAxis("Mouse ScrollWheel") > 0f) CycleDownServerRpc();
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetAxis("Mouse ScrollWheel") < 0f) CycleUpServerRpc();
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha1)) CycleToServerRpc(0);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha2)) CycleToServerRpc(1);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha3)) CycleToServerRpc(2);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha4)) CycleToServerRpc(3);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha5)) CycleToServerRpc(4);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha6)) CycleToServerRpc(5);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha7)) CycleToServerRpc(6);
        else if(!cycleCooldown && playerAlive && allowedToCycle && Input.GetKeyDown(KeyCode.Alpha8)) CycleToServerRpc(7);

        //geistLightScript.GeistLightUIUpdate();
        cameraScript.CameraUIUpdate();
        //thermometerScript.ThermometerStatusAndUIUpdate();
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
        ArmsAnimationClientRpc(heldIndex.Value);
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
        ArmsAnimationClientRpc(heldIndex.Value);
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
        ArmsAnimationClientRpc(to);
    }

    [ClientRpc]
    public void ArmsAnimationClientRpc(int index) {
        firstPersonArmsAnimator.SetInteger("ToolIndex", index);
    }

    [ClientRpc]
    public void ArmsAnimationTriggerClientRpc(string triggerName) {
        firstPersonArmsAnimator.SetTrigger(triggerName);
    }

    [ClientRpc]
    public void ArmsAnimationBoolClientRpc(string triggerName, bool state) {
        firstPersonArmsAnimator.SetTrigger(triggerName);
    }

    private IEnumerator ToolbeltSwap(int newIndex) {
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

        if(!playerItemMeshes[5].activeInHierarchy) {
            return;
        }
        
        const float roomTemperature = 60f;
        const float maxRange = 5f;

        float temperature = roomTemperature;

        foreach(CursedObject cursedObject in cursedObjectsWithinRange) {
            float distance = Vector3.Distance(algorCrystalTransform.position, cursedObject.transform.position);
            float influence = 1f - Mathf.Clamp01(distance / maxRange);

            temperature = Mathf.Lerp(temperature, cursedObject.temperature, influence);
        }

        // Hidden ice
        //int hiddenIceAmount = playerItemMeshes[5].GetComponent<Thermometer>().AmountOfHiddenIceCrystalsNearby();
        foreach(Collider col in playerItemMeshes[5].GetComponent<Thermometer>().collidersInside) {
            float iceDist = Vector3.Distance(algorCrystalTransform.position, col.transform.position);
            float influence = 1f - Mathf.Clamp01(iceDist / maxRange);

            temperature = Mathf.Lerp(temperature, -20f, influence);
        }

        temperature = Mathf.Clamp(temperature, -20f, 60f);

        defaultTemp.Value = Mathf.RoundToInt(temperature);
    }

    private void UpdateEMF() {
        //playerItemMeshes[3].GetComponent<Scanner>().levelEMF = defaultEMF;
        if(playerItemMeshes[3].activeInHierarchy && defaultEMF.Value != 7) {
            int fluctuation = Random.Range(-1, 2);
            defaultEMF.Value += fluctuation;
            if(defaultEMF.Value == 7) defaultEMF.Value = 6;
            else if(defaultEMF.Value == -1) defaultEMF.Value = 0;

        }
    }

    public void CheckHolyWater() {
        //if(!IsOwner) return; We are already doing this where we are calling it.
        Debug.Log("Checking holy.");
        bool anyActive = false;
        if(playerItemMeshes[6].activeSelf) {
            foreach(CursedObject curse in cursedObjectsWithinRange) {
                if(curse.cursesList.Contains((int)CursedObject.CurseType.ProfanusTrait)) {
                    playerItemMeshes[6].GetComponent<HolyWater>().active.Value = true; //TurnHolyOnServerRpc();
                    anyActive = true;
                }
            }
            if(!anyActive) playerItemMeshes[6].GetComponent<HolyWater>().active.Value = false;//.TurnHolyOffServerRpc();
        }
    }

    // animations

    // In future, play animation on both the arms and the masked upper body.
    // One will simply be invisible, depending on who's looking.
    public void GeistlightAnimation(bool active) {
        ArmsAnimationClientRpc(heldIndex.Value);
       // firstPersonArmsAnimator.SetBool("Geistlight", active);
        ArmsAnimationBoolClientRpc("Geistlight", active);

    }

    public void BellAnimation() {
        ArmsAnimationTriggerClientRpc("BellActivation");
    }
    
    public void CameraAnimation() {
        ArmsAnimationTriggerClientRpc("CameraActivation");
    }
}
