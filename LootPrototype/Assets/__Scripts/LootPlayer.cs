using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Enables LINQ queries, which will be explained soon

// The player can either be human or an AI
public enum PlayerType
{
    human,
    ai
}

[System.Serializable]
public class LootPlayer {
    public PlayerType type = PlayerType.ai;
    public int playerNum;
    public SlotDef handSlotDef;
    public List<CardLoot> hand; // The cards in this player's hand

	// Add a card to the hand
    public CardLoot AddCard(CardLoot eCL)
    {
        if (hand == null) hand = new List<CardLoot>();

        // Add the card to the hand
        hand.Add(eCL);

        //Sort the cards by rank using LINQ if this is a human
        if (type == PlayerType.human)
        {
            CardLoot[] cards = hand.ToArray();

            // This is the LINQ call
            cards = cards.OrderBy(cd => cd.value).ToArray();

            hand = new List<CardLoot>(cards);
            // Note: LINQ operations can be a bit slow (like it could take a
            // couple of milliseconds), but since we're only doing it once
            // every round, it isn't a problem.
        }

        eCL.SetSortingLayerName("10"); // Sorts the moving card to the top
        eCL.eventualSortLayer = handSlotDef.layerName;

        FanHand();
        return (eCL);
    }

    // Remove a card from the hand
    public CardLoot RemoveCard(CardLoot cl)
    {
        // If hand is null or doesn't contain cl, return null
        if (hand == null || !hand.Contains(cl)) return null;
        hand.Remove(cl);
        FanHand();
        return (cl);
    }

    public void FanHand()
    {
        // startRot is the rotation about Z of the first card
        float startRot = 0;
        startRot = handSlotDef.rot;
        if(hand.Count > 1)
        {
            startRot += Loot.S.handFanDegrees * (hand.Count - 1) / 2;
        }

        // Move all the cards to their new positions
        Vector3 pos;
        float rot;
        Quaternion rotQ;
        for (int i=0; i<hand.Count; i++)
        {
            rot = startRot - Loot.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);

            pos = Vector3.up * CardLoot.CARD_HEIGHT / 2f;

            pos = rotQ * pos;

            // Add the base position of the player's hand (which will be at the
            // bottom-center of the fan of the cards)
            pos += handSlotDef.pos;
            pos.z = -0.5f * i;

            // If not the initial deal, start moving the card immediately.
            if(Loot.S.phase != TurnPhase.idle)
            {
                hand[i].timeStart = 0;
            }

            // Set the localPosition and rotation of the ith card in the hand
            hand[i].MoveTo(pos, rotQ); // Tell CardLoot to interpolate
            hand[i].state = CLState.toHand;
            // After the move, CardLoot will set the state to CLState.hand

            /* <= This begins a multiline comment
            hand[i].transform.localPosition = pos;
            hand[i].transform.rotation = rotQ;
            hand[i].state = CLState.hand; 
            This ends the multiline comment => */

            hand[i].faceUp = (type == PlayerType.human);

            // Set the SortOrder of the cards so that they overlap properly
            hand[i].eventualSortOrder = i * 4;
            //hand[i].SetSortOrder(i * 4);
        }
    }

    // The TakeTurn() function enables the AI of the computer Players
    public void TakeTurn()
    {
        Utils.tr("Player.TakeTurn");

        // Don't need to do anything if this is the human player.
        if (type == PlayerType.human) return;

        Loot.S.phase = TurnPhase.waiting;

        CardLoot cl;

        // If this is an AI player, need to make a choice about what to play
        // Find valid plays
        List<CardLoot> validCards = new List<CardLoot>();
        foreach (CardLoot tCL in hand)
        {
            if (Loot.S.ValidPlay(tCL))
            {
                validCards.Add(tCL);
            }
        }
        // If there are no valid cards
        if(validCards.Count == 0)
        {
            // ... then draw a card
            cl = AddCard(Loot.S.Draw());
            cl.callbackPlayer = this;
            return;
        }

        // So, there is a card or more to play, so pick one
        cl = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cl);
        Loot.S.MoveToTarget(cl);
        cl.callbackPlayer = this;
    }

    public void CLCallback(CardLoot tCL)
    {
        Utils.tr("Player.CLCallback()", tCL.name, "Player " + playerNum);
        // The card is done moving, so pass the turn
        Loot.S.PassTurn();
    }
}
