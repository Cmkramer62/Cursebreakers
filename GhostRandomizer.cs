using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostRandomizer : MonoBehaviour {

    public GameObject[] ghostBodies;
    public int index;
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

    public bool overrideEyes = false;
    public Enemy ghostScript;
    
    public void RandomizeGhost() {
        index = Random.Range(0, ghostBodies.Length);
        ghostBodies[index].SetActive(true);
        //deathScript.realGhostChild = ghostBodies[index];
        ghostScript.animator = ghostBodies[index].GetComponent<Animator>();
        SetGhostGeneral();
    }



    private void SetGhostGeneral() {
        if(index == 0) SetGownLongHair();
        else if(index == 1) SetGownShortHair();
        else if(index == 2) SetNakedLongHair();
        else if(index == 3) SetNakedShortHair();
        else if(index == 4) SetNakedBald();
        else if(index == 5) SetRobe();
        else if(index == 6) SetVeil();
        else SetVictorian();
    }

    private void SetGownLongHair() {
        var mats = gownLongHair[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        gownLongHair[0].materials = mats;

        var mats2 = gownLongHair[1].materials;
        mats2[0] = RandomMaterial(hairMats);
        gownLongHair[1].materials = mats2;

        var mats3 = gownLongHair[2].materials;
        mats3[0] = RandomMaterial(gownMats);
        gownLongHair[2].materials = mats3;
    }
    
    private void SetGownShortHair() {
        var mats = gownShortHair[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        gownShortHair[0].materials = mats;

        var mats2 = gownShortHair[1].materials;
        mats2[0] = RandomMaterial(gownMats);
        gownShortHair[1].materials = mats2;

        var mats3 = gownShortHair[2].materials;
        mats3[0] = RandomMaterial(hairMats);
        gownShortHair[2].materials = mats3;
    }

    private void SetNakedLongHair() {
        var mats = nakedLongHair[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        nakedLongHair[0].materials = mats;

        var mats2 = nakedLongHair[1].materials;
        mats2[0] = RandomMaterial(hairMats);
        nakedLongHair[1].materials = mats2;
    }

    private void SetNakedShortHair() {
        var mats = nakedShortHair[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        nakedShortHair[0].materials = mats;

        var mats2 = nakedShortHair[1].materials;
        mats2[0] = RandomMaterial(hairMats);
        nakedShortHair[1].materials = mats2;
    }

    private void SetNakedBald() {
        var mats = nakedBald[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        nakedBald[0].materials = mats;
    }

    private void SetRobe() {
        var mats = robe[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        robe[0].materials = mats;

        var mats2 = robe[1].materials;
        mats2[0] = RandomMaterial(hoodMats);
        robe[1].materials = mats2;

        var mats3 = robe[2].materials;
        mats3[0] = RandomMaterial(robeMats);
        robe[2].materials = mats3;

        var mats4 = robe[3].materials;
        mats4[0] = RandomMaterial(shoulderclothMats);
        robe[3].materials = mats4;
    }

    private void SetVeil() {
        var mats = veil[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        veil[0].materials = mats;

        var mats2 = veil[1].materials;
        mats2[0] = RandomMaterial(veilMats);
        veil[1].materials = mats2;
    }

    private void SetVictorian() {
        var mats = victorian[0].materials;
        mats[0] = RandomMaterial(skinMats);
        mats[1] = RandomMaterial(eyeMats);
        if(overrideEyes) mats[1] = glowingEyesMat;
        mats[2] = RandomMaterial(teethMats);
        victorian[0].materials = mats;

        var mats2 = victorian[1].materials;
        mats2[0] = RandomMaterial(headclothMats);
        victorian[1].materials = mats2;

        var mats3 = victorian[2].materials;
        mats3[0] = RandomMaterial(dressMats);
        victorian[2].materials = mats3;
    }

    private Material RandomMaterial(Material[] materialList) {
        return materialList[Random.Range(0, materialList.Length)];
    }

}
