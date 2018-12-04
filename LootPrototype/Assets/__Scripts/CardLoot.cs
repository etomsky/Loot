﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//CLState includes both states for the game and to... states for movement
public enum CLState
{
    toDrawpile,
    drawpile,
    toTarget,
    target,
    toHand,
    hand,
    toBattle,
    battle,
    discard,
    to,
    idle
}

public class CardLoot : Card {
    //Static variables are shared by all instances of CardLoot
    static public float MOVE_DURATION = 0.5f;
    static public string MOVE_EASING = Easing.InOut;
    static public float CARD_HEIGHT = 3.5f;
    static public float CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardLoot")]
    public CLState state = CLState.drawpile;

    // Fields to store info the card will use to move and rotate
    public List<Vector3> bezierPts;
    public List<Quaternion> bezierRots;
    public float timeStart, timeDuration;
    public int eventualSortOrder;
    public string eventualSortLayer;

    // When the card is done moving, it will call reportFinishTo.SendMessage()
    public GameObject reportFinishTo = null;
    [System.NonSerialized]
    public LootPlayer callbackPlayer = null;

    // MoveTo tells the card to interpolate to a new position and rotation
    public void MoveTo(Vector3 ePos, Quaternion eRot)
    {
        // Make new interpolation lists for the card.
        // Position and Rotation will each have only two points.
        bezierPts = new List<Vector3>();
        bezierPts.Add(transform.localPosition); // Current position
        bezierPts.Add(ePos); // Current rotation

        bezierRots = new List<Quaternion>();
        bezierRots.Add(transform.rotation); // New position
        bezierRots.Add(eRot); // New rotation

        if (timeStart == 0)
        {
            timeStart = Time.time;
        }
        // timeDuration always starts the same but can be overwritten later
        timeDuration = MOVE_DURATION;

        state = CLState.to;
    }

    public void MoveTo(Vector3 ePos)
    {
        MoveTo(ePos, Quaternion.identity);
    }

    private void Update()
    {
        switch (state)
        {
            case CLState.toHand:
            case CLState.toBattle:
            case CLState.toDrawpile:
            case CLState.to:
                float u = (Time.time - timeStart) / timeDuration;
                float uC = Easing.Ease(u, MOVE_EASING);

                if (u < 0)
                {
                    transform.localPosition = bezierPts[0];
                    transform.rotation = bezierRots[0];
                    return;
                }
                else if (u >= 1)
                {
                    uC = 1;
                    // Move from the to... state to the proper next state
                    if (state == CLState.toHand) state = CLState.hand;
                    if (state == CLState.toBattle) state = CLState.battle;
                    if (state == CLState.toDrawpile) state = CLState.drawpile;
                    if (state == CLState.to) state = CLState.idle;

                    // Move to the final position
                    transform.localPosition = bezierPts[bezierPts.Count - 1];
                    transform.rotation = bezierRots[bezierPts.Count - 1];

                    // Reset timeStart to 0 so it gets overwritten next time
                    timeStart = 0;

                    if (reportFinishTo != null)
                    {
                        reportFinishTo.SendMessage("CLCallback", this);
                        reportFinishTo = null;
                    }
                    else if (callbackPlayer != null)
                    {
                        // If there's a callback Player
                        // Call CLCallback directly on the Player
                        callbackPlayer.CLCallback(this);
                        callbackPlayer = null;
                    }
                    else
                    {
                        // If there is nothing to callback
                        // Just let it stay still.
                    }
                }
                else
                {
                    // Normal interpolation behavior (0 <= u < 1)
                    Vector3 pos = Utils.Bezier(uC, bezierPts);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, bezierRots);
                    transform.rotation = rotQ;

                    if (u > 0.5f)
                    {
                        SpriteRenderer sRend = spriteRenderers[0];
                        if (sRend.sortingOrder != eventualSortOrder)
                        {
                            // Jump to the proper sort order
                            SetSortOrder(eventualSortOrder);
                        }
                        if (sRend.sortingLayerName != eventualSortLayer)
                        {
                            // Jump to the proper sort layer
                            SetSortingLayerName(eventualSortLayer);
                        }
                    }
                }
                break;
        }
    }

    // This allows the card to react to being clicked
    public override void OnMouseUpAsButton()
    {
        // Call the CardClicked method on the Loot singleton
        Loot.S.CardClicked(this);
        // Also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton();
    }
}
