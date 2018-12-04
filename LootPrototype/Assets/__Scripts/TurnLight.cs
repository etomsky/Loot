using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLight : MonoBehaviour {

    private void Update()
    {
        transform.position = Vector3.back * 3;

        if(Loot.CURRENT_PLAYER == null)
        {
            return;
        }

        transform.position += Loot.CURRENT_PLAYER.handSlotDef.pos;
    }
}
