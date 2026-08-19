using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public struct GhostAppearance : INetworkSerializable {
    public int body;
    public int skin;
    public int eyes;
    public int teeth;
    public int hair;
    public int gown;
    public int robe;
    public int hood;
    public int shoulder;
    public int veil;
    public int headcloth;
    public int dress;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter {
        serializer.SerializeValue(ref body);
        serializer.SerializeValue(ref skin);
        serializer.SerializeValue(ref eyes);
        serializer.SerializeValue(ref teeth);
        serializer.SerializeValue(ref hair);
        serializer.SerializeValue(ref gown);
        serializer.SerializeValue(ref robe);
        serializer.SerializeValue(ref hood);
        serializer.SerializeValue(ref shoulder);
        serializer.SerializeValue(ref veil);
        serializer.SerializeValue(ref headcloth);
        serializer.SerializeValue(ref dress);
    }
}

public class GhostRandomizer : NetworkBehaviour {

    public NetworkVariable<GhostAppearance> generatedRanString = new NetworkVariable<GhostAppearance>();
    [SerializeField]
    private GhostAppearance debugAppearance;

    public GameObject[] ghostBodies;
    public int mainBodyRandom;
    public Death deathScript;

    #region MeshLists

    public SkinnedMeshRenderer[] gownLongHair, gownShortHair, nakedLongHair, nakedShortHair, nakedBald, robe, veil, victorian;

    public Material[] skinMats, eyeMats, teethMats, hairMats; // shared amongst all ghosts, if they have hair. Naked only uses these.
    public Material[] gownMats; // only for the gown ghost.
    public Material[] hoodMats, robeMats, shoulderclothMats; // only for the robe ghost. One index needed for all, except shoulders.
    public Material[] veilMats; // only one for the veil ghost.
    public Material[] headclothMats, dressMats; // only for the victorian ghost.
    public Material glowingEyesMat;
    #endregion

    public Enemy ghostScript;
    public NetworkVariable<bool> overrideEyes = new NetworkVariable<bool>(false);
    public CurseGameManager serverGameManagerScript;
    
    public GameObject ghostGeistParticles;
    public Bell bellScript;
    public GameObject[] enviroParticles, horns;
    public RuntimeAnimatorController floatingController;
    public bool searchWithSound = false;

    public override void OnNetworkSpawn() {
        generatedRanString.OnValueChanged += (_, newCode) =>
        {
            ApplyRandomization(newCode);
        };


        if(IsServer) GenerateCode();
        else ApplyRandomization(generatedRanString.Value);

    }



    // Called by SERVER. Generates code for visual randomization.
    public void GenerateCode() {
        
        Debug.Log("Randomizing ghost");
        mainBodyRandom = Random.Range(0, ghostBodies.Length);
        
        // generate 6 random numbers. Not all will use all 6
        int skinRandom = Random.Range(0, skinMats.Length);
        int eyesRandom = Random.Range(0, eyeMats.Length);
        int teethRandom = Random.Range(0, teethMats.Length);
        int hairRandom = Random.Range(0, hairMats.Length);
        int gownRandom = Random.Range(0, gownMats.Length);
        int robeRandom = Random.Range(0, robeMats.Length);
        int hoodRandom = Random.Range(0, hoodMats.Length);
        int shoulderRandom = Random.Range(0, shoulderclothMats.Length);
        int veilRandom = Random.Range(0, veilMats.Length);
        int headclothRandom = Random.Range(0, headclothMats.Length);
        int dressRandom = Random.Range(0, dressMats.Length);

        GhostAppearance newAppearance = new GhostAppearance();
        newAppearance.skin = skinRandom;
        newAppearance.eyes = eyesRandom;
        newAppearance.teeth = teethRandom;
        newAppearance.hair = hairRandom;
        newAppearance.gown = gownRandom;
        newAppearance.robe = robeRandom;
        newAppearance.hood = hoodRandom;
        newAppearance.shoulder = shoulderRandom;
        newAppearance.veil = veilRandom;
        newAppearance.headcloth = headclothRandom;
        newAppearance.dress = dressRandom;
        newAppearance.body = mainBodyRandom;

        generatedRanString.Value = newAppearance;
        //SetRandomizationOffCodeClientRpc(generatedCode);
        
    }

    // Called by EITHER. Runs when code changes, or on network spawn.
    public void ApplyRandomization(GhostAppearance generatedCode) {
        Debug.Log("Applying randomization: " + generatedCode.body + " " + generatedCode.eyes);
        debugAppearance = generatedCode;
        ghostBodies[generatedCode.body].SetActive(true);
        //deathScript.realGhostChild = ghostBodies[index];
        ghostScript.animator = ghostBodies[generatedCode.body].GetComponent<Animator>();

        if(generatedCode.body == 0) SetGownLongHair(generatedCode);
        else if(generatedCode.body == 1) SetGownShortHair(generatedCode);
        else if(generatedCode.body == 2) SetNakedLongHair(generatedCode);
        else if(generatedCode.body == 3) SetNakedShortHair(generatedCode);
        else if(generatedCode.body == 4) SetNakedBald(generatedCode);
        else if(generatedCode.body == 5) SetRobe(generatedCode);
        else if(generatedCode.body == 6) SetVeil(generatedCode);
        else SetVictorian(generatedCode);

        ApplyClues();
    }

   // [ClientRpc]
    private void ApplyClues() {
        ApplyCursedAura();
        ApplyCursedEnvironment();
    }

    public void ApplyCursedEnvironment() {
        GameObject potentialGoalCurse = null;
        if(serverGameManagerScript.goalCurse.Value.TryGet(out NetworkObject networkObject)) {
            potentialGoalCurse = networkObject.gameObject;
        }
        if(potentialGoalCurse == null) Debug.Log("ERROR IN CURSED OBJECT, COULD NOT GET GOALCURSE.");

        var goalCurseEnviroSlot = potentialGoalCurse.GetComponentInChildren<CursedObject>().cursesList[1];

        enviroParticles[0].SetActive(goalCurseEnviroSlot == (int)CursedObject.CursedTypes.Glowing);
        enviroParticles[1].SetActive(goalCurseEnviroSlot == (int)CursedObject.CursedTypes.EMF);
        enviroParticles[2].SetActive(goalCurseEnviroSlot == (int)CursedObject.CursedTypes.Aura);
        enviroParticles[3].SetActive(goalCurseEnviroSlot == (int)CursedObject.CursedTypes.Thermo);
        enviroParticles[4].SetActive(goalCurseEnviroSlot == (int)CursedObject.CursedTypes.Unholy);

        searchWithSound = goalCurseEnviroSlot == (int)CursedObject.CursedTypes.Sound;
        //if(goalCurseSpecific == (int)CursedObject.CursedTypes.Sound) bellScript.ghostSearchWithSound = true;
        // this needs to be moved somewhere else. /\
    }

    public void ApplyCursedAura() {
        Debug.Log("Starting apply aura");

        GameObject potentialGoalCurse = null;
        if(serverGameManagerScript.goalCurse.Value.TryGet(out NetworkObject networkObject)) {
            potentialGoalCurse = networkObject.gameObject;
        }
        if(potentialGoalCurse == null) Debug.Log("ERROR IN CURSED OBJECT, COULD NOT GET GOALCURSE.");
        int goalCurseAuraSlot = potentialGoalCurse.GetComponentInChildren<CursedObject>().cursesList[2];

        if(goalCurseAuraSlot == (int)CursedObject.CursedTypes.Glowing) {
            ghostGeistParticles.SetActive(true);
            GetComponent<Enemy>().animator.transform.parent.gameObject.GetComponent<Enemy>().geistAura.Value = true;
        }
        else if(goalCurseAuraSlot == (int)CursedObject.CursedTypes.EMF) {
            GetComponent<Enemy>().animator.runtimeAnimatorController = floatingController;
        }
        else if(goalCurseAuraSlot == (int)CursedObject.CursedTypes.Aura) {
            overrideEyes.Value = true; // Does this happen too late?
        }
        else if(goalCurseAuraSlot == (int)CursedObject.CursedTypes.Thermo) {
            // debug
            GetComponent<Enemy>().freezingAura.Value = true;
        }
        else if(goalCurseAuraSlot == (int)CursedObject.CursedTypes.Unholy) {
            foreach(GameObject horns in horns) {
                horns.SetActive(true);
            }
        }
        else {
            bellScript.ghostSearchWithSound = true;
        }
        
        Debug.Log("done apply aura");
    }


    #region Setting Body Parts
    private void SetGownLongHair(GhostAppearance generatedCode) {
        var bodyMats = gownLongHair[0].materials;
        bodyMats[0] = skinMats[generatedCode.skin];
        bodyMats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) bodyMats[1] = glowingEyesMat;
        bodyMats[2] = teethMats[generatedCode.teeth];
        gownLongHair[0].materials = bodyMats;

        var hairMats = gownLongHair[1].materials;
        hairMats[0] = this.hairMats[generatedCode.hair];
        gownLongHair[1].materials = hairMats;

        var gownMats = gownLongHair[2].materials;
        gownMats[0] = gownMats[0]; // generatedcode.gown seems to keep throwing out of bounds.
        gownLongHair[2].materials = gownMats;
    }
    
    private void SetGownShortHair(GhostAppearance generatedCode) {
        var mats = gownShortHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        gownShortHair[0].materials = mats;

        var mats2 = gownShortHair[1].materials;
        mats2[0] = gownMats[generatedCode.gown];
        gownShortHair[1].materials = mats2;

        var mats3 = gownShortHair[2].materials;
        mats3[0] = hairMats[generatedCode.hair];
        gownShortHair[2].materials = mats3;
    }

    private void SetNakedLongHair(GhostAppearance generatedCode) {
        var mats = nakedLongHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedLongHair[0].materials = mats;

        var mats2 = nakedLongHair[1].materials;
        mats2[0] = hairMats[generatedCode.hair];
        nakedLongHair[1].materials = mats2;
    }

    private void SetNakedShortHair(GhostAppearance generatedCode) {
        var mats = nakedShortHair[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedShortHair[0].materials = mats;

        var mats2 = nakedShortHair[1].materials;
        mats2[0] = hairMats[generatedCode.hair];
        nakedShortHair[1].materials = mats2;
    }

    private void SetNakedBald(GhostAppearance generatedCode) {
        var mats = nakedBald[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        nakedBald[0].materials = mats;
    }

    private void SetRobe(GhostAppearance generatedCode) {        
        var mats = robe[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        robe[0].materials = mats;

        var mats2 = robe[1].materials;
        mats2[0] = hoodMats[generatedCode.hood];
        robe[1].materials = mats2;

        var mats3 = robe[2].materials;
        mats3[0] = robeMats[generatedCode.robe];
        robe[2].materials = mats3;

        var mats4 = robe[3].materials;
        mats4[0] = shoulderclothMats[generatedCode.shoulder];
        robe[3].materials = mats4;
    }

    private void SetVeil(GhostAppearance generatedCode) {
        var mats = veil[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        veil[0].materials = mats;

        var mats2 = veil[1].materials;
        mats2[0] = veilMats[generatedCode.veil];
        veil[1].materials = mats2;
    }

    private void SetVictorian(GhostAppearance generatedCode) {
        var mats = victorian[0].materials;
        mats[0] = skinMats[generatedCode.skin];
        mats[1] = eyeMats[generatedCode.eyes];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[generatedCode.teeth];
        victorian[0].materials = mats;

        var mats2 = victorian[1].materials;
        mats2[0] = headclothMats[generatedCode.headcloth];
        victorian[1].materials = mats2;

        var mats3 = victorian[2].materials;
        mats3[0] = dressMats[generatedCode.dress];
        victorian[2].materials = mats3;
    }
    #endregion
}
