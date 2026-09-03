using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerDeadBody : MonoBehaviour {

    public NetworkVariable<ulong> playerID = new NetworkVariable<ulong>();

    public bool BodyMatchPlayer(ulong comparing) {
        //if(playerID.Value == 0 || comparing == 0) return false;
        return comparing == playerID.Value;
    }

}
