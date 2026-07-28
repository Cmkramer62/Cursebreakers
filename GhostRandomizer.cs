using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GhostRandomizer : NetworkBehaviour {

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
    //public NetworkVariable<string> generatedCode = new NetworkVariable<string>();

    // This needs to be called by the server only.
    // It then runs randomization, a code of which is stored in a synced int.
    // Then it runs a client rpc, and using that synced int it sets the body.
    public void RandomizeGhost() {
        
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

        string generatedCode = mainBodyRandom + " " + skinRandom + " " + eyesRandom + " " + teethRandom +
            " " + hairRandom + " " + gownRandom + " " + robeRandom + " " + hoodRandom + " " + shoulderRandom +
            " " + veilRandom + " " + headclothRandom + " " + dressRandom;

        Debug.Log("Random code: " + generatedCode);
        SetRandomizationOffCodeClientRpc(generatedCode);
        
    }

    [ClientRpc]
    public void SetRandomizationOffCodeClientRpc(string generatedCode) {
        Debug.Log("Random code received: " + generatedCode);

        string[] codeDecon = generatedCode.Split(' ');
        Debug.Log("ghost body: " + int.Parse(codeDecon[0]));
        ghostBodies[int.Parse(codeDecon[0])].SetActive(true);
        //deathScript.realGhostChild = ghostBodies[index];
        ghostScript.animator = ghostBodies[int.Parse(codeDecon[0])].GetComponent<Animator>();

        if(int.Parse(codeDecon[0]) == 0) SetGownLongHair(generatedCode);
        else if(int.Parse(codeDecon[0]) == 1) SetGownShortHair(generatedCode);
        else if(int.Parse(codeDecon[0]) == 2) SetNakedLongHair(generatedCode);
        else if(int.Parse(codeDecon[0]) == 3) SetNakedShortHair(generatedCode);
        else if(int.Parse(codeDecon[0]) == 4) SetNakedBald(generatedCode);
        else if(int.Parse(codeDecon[0]) == 5) SetRobe(generatedCode);
        else if(int.Parse(codeDecon[0]) == 6) SetVeil(generatedCode);
        else SetVictorian(generatedCode);
    }

    private void SetGownLongHair(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var bodyMats = gownLongHair[0].materials;
        bodyMats[0] = skinMats[int.Parse(codeDecon[1])];
        bodyMats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) bodyMats[1] = glowingEyesMat;
        bodyMats[2] = teethMats[int.Parse(codeDecon[3])];
        gownLongHair[0].materials = bodyMats;

        var hairMats = gownLongHair[1].materials;
        hairMats[0] = this.hairMats[int.Parse(codeDecon[4])];
        gownLongHair[1].materials = hairMats;

        var gownMats = gownLongHair[2].materials;
        gownMats[0] = gownMats[int.Parse(codeDecon[5])];
        gownLongHair[2].materials = gownMats;
    }
    
    private void SetGownShortHair(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var mats = gownShortHair[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        gownShortHair[0].materials = mats;

        var mats2 = gownShortHair[1].materials;
        mats2[0] = gownMats[int.Parse(codeDecon[5])];
        gownShortHair[1].materials = mats2;

        var mats3 = gownShortHair[2].materials;
        mats3[0] = hairMats[int.Parse(codeDecon[4])];
        gownShortHair[2].materials = mats3;
    }

    private void SetNakedLongHair(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var mats = nakedLongHair[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        nakedLongHair[0].materials = mats;

        var mats2 = nakedLongHair[1].materials;
        mats2[0] = hairMats[int.Parse(codeDecon[4])];
        nakedLongHair[1].materials = mats2;
    }

    private void SetNakedShortHair(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var mats = nakedShortHair[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        nakedShortHair[0].materials = mats;

        var mats2 = nakedShortHair[1].materials;
        mats2[0] = hairMats[int.Parse(codeDecon[4])];
        nakedShortHair[1].materials = mats2;
    }

    private void SetNakedBald(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var mats = nakedBald[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        nakedBald[0].materials = mats;
    }

    private void SetRobe(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');
        
        var mats = robe[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        robe[0].materials = mats;

        var mats2 = robe[1].materials;
        mats2[0] = hoodMats[int.Parse(codeDecon[7])];
        robe[1].materials = mats2;

        var mats3 = robe[2].materials;
        mats3[0] = robeMats[int.Parse(codeDecon[6])];
        robe[2].materials = mats3;

        var mats4 = robe[3].materials;
        mats4[0] = shoulderclothMats[int.Parse(codeDecon[8])];
        robe[3].materials = mats4;
    }

    private void SetVeil(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');

        var mats = veil[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        veil[0].materials = mats;

        var mats2 = veil[1].materials;
        mats2[0] = veilMats[int.Parse(codeDecon[9])];
        veil[1].materials = mats2;
    }

    private void SetVictorian(string generatedCode) {
        string[] codeDecon = generatedCode.Split(' ');
        
        var mats = victorian[0].materials;
        mats[0] = skinMats[int.Parse(codeDecon[1])];
        mats[1] = eyeMats[int.Parse(codeDecon[2])];
        if(overrideEyes.Value) mats[1] = glowingEyesMat;
        mats[2] = teethMats[int.Parse(codeDecon[3])];
        victorian[0].materials = mats;

        var mats2 = victorian[1].materials;
        mats2[0] = headclothMats[int.Parse(codeDecon[10])];
        victorian[1].materials = mats2;

        var mats3 = victorian[2].materials;
        mats3[0] = dressMats[int.Parse(codeDecon[11])];
        victorian[2].materials = mats3;
    }

}
