using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// This enum contains the different phases of a game turn
public enum TurnPhase
{
    idle,
    pre,
    waiting,
    post,
    gameOver
}

public class Loot : MonoBehaviour {
    static public Loot S;
    static public LootPlayer CURRENT_PLAYER;

    [Header("Set in Inspector")]
    public TextAsset lootDeckXML;
    public TextAsset lootLayoutXML;
    public Vector3 layoutCenter = Vector3.zero;
    public float handFanDegrees = 10f;
    public int numStartingCards = 6;
    public float drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck lootDeck;
    public List<CardLoot> drawPile;
    public List<CardLoot> discardPile;
    public List<LootPlayer> players;
    public CardLoot targetCard;
    public TurnPhase phase = TurnPhase.idle;

    private LootLayout layout;
    private Transform layoutAnchor;

    private void Awake()
    {
        S = this;
    }

    private void Start()
    {
        lootDeck = GetComponent<Deck>(); // Get the Deck
        lootDeck.InitDeck(lootDeckXML.text);
        Deck.Shuffle(ref lootDeck.cards);

        layout = GetComponent<LootLayout>(); // Get the Layout
        layout.ReadLayout(lootLayoutXML.text);

        drawPile = UpgradeCardsList(lootDeck.cards);
        LayoutGame();
    }

    List<CardLoot> UpgradeCardsList(List<Card> lCD)
    {
        List<CardLoot> lCL = new List<CardLoot>();
        foreach (Card tCD in lCD)
        {
            lCL.Add(tCD as CardLoot);
        }
        return (lCL);
    }

    // Position all the cards in the drawPile properly
    public void ArrangeDrawPile()
    {
        CardLoot tCL;

        for (int i = 0; i < drawPile.Count; i++)
        {
            tCL = drawPile[i];
            tCL.transform.SetParent(layoutAnchor);
            tCL.transform.localPosition = layout.drawPile.pos;
            // Rotation should start at 0
            tCL.faceUp = false;
            tCL.SetSortingLayerName(layout.drawPile.layerName);
            tCL.SetSortOrder(-i * 4); // Order them front-to-back
            tCL.state = CLState.drawpile;
        }
    }

    //Perform the initial game layout
    void LayoutGame()
    {
        // Create an empty GameObject to serve as the tableau's anchor
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        // Position the drawPile cards
        ArrangeDrawPile();

        // Set up the players
        LootPlayer pl;
        players = new List<LootPlayer>();
        foreach (SlotDef tSD in layout.slotDefs)
        {
            pl = new LootPlayer();
            pl.handSlotDef = tSD;
            players.Add(pl);
            pl.playerNum = tSD.player;
        }
        players[0].type = PlayerType.human; // Make only the 0th player human

        CardLoot tCL;
        // Deal six cards to each player
        for (int i = 0; i < numStartingCards; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tCL = Draw(); // Draw a card
                // Stagger the draw time a bit.
                tCL.timeStart = Time.time + drawTimeStagger * (i * 4 + j);

                players[(j + 1) % 4].AddCard(tCL);
            }
        }

        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
    }

    public void DrawFirstTarget()
    {
        // Flip up the first target card from the DrawPile
        CardLoot tCL = MoveToTarget(Draw());
        // Set the CardLoot to call CLCallback on this Loot when it is done
        tCL.reportFinishTo = this.gameObject;
    }

    // This makes a new card the target
    public CardLoot MoveToTarget (CardLoot tCL)
    {
        tCL.timeStart = 0;
        tCL.MoveTo(layout.discardPile.pos + Vector3.back);
        tCL.state = CLState.toTarget;
        tCL.faceUp = true;

        targetCard = tCL;

        return (tCL);
    }

    // This callback is used by the last card to be dealt at the beginning
    public void CLCallback(CardLoot cl)
    {
        // You sometimes want to have reporting of method calls like this
        Utils.tr("Loot:CLCallback()", cl.name);
        StartGame(); // Start the Game
    }

    public void StartGame()
    {
        // Pick the player to the left of the human to go first.
        PassTurn(1);
    }

    public void PassTurn(int num = -1)
    {
        // If no number was passed in, pick the next player
        if (num == -1)
        {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % 4;
        }
        int lastPlayerNum = -1;
        if (CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            // Check for Game Over and need to reshuffle discards
            if (CheckGameOver())
            {
                return;
            }
        }
        CURRENT_PLAYER = players[num];
        phase = TurnPhase.pre;

        CURRENT_PLAYER.TakeTurn();

        // Report the turn passing
        Utils.tr("Loot:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
    }

    public bool CheckGameOver()
    {
        // See if we need to reshuffle the discard pile into the draw pile
        if (drawPile.Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach (CardLoot cl in discardPile)
            {
                cards.Add(cl);
            }
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardsList(cards);
            ArrangeDrawPile();
        }

        // Check to see if the current player has won
        if (CURRENT_PLAYER.hand.Count == 0)
        {
            // The player that just played has won!
            phase = TurnPhase.gameOver;
            Invoke("RestartGame", 1);
            return (true);
        }
        return (false);
    }

    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Loot_Scene_0");
    }

    // ValidPlay verifies that the card chosen can be played in a battle
    public bool ValidPlay(CardLoot cl)
    {
        // It's a valid play if the color is not the same
        if (cl.color != targetCard.color) return (true);

        // It's a valid play if the 
        if (cl.suit == targetCard.suit)
        {
            return (true);
        }

        // Otherwise, return false
        return (false);
    }

    // This makes a new card the target
    public CardLoot MoveToBattle(CardLoot tCL)
    {
        tCL.timeStart = 0;
        tCL.MoveTo(layout.discardPile.pos + Vector3.back);
        tCL.state = CLState.toBattle;
        tCL.faceUp = true;

        tCL.SetSortingLayerName("10");
        tCL.eventualSortLayer = layout.battle.layerName;
        if (targetCard != null)
        {
            MoveToDiscard(targetCard);
        }

        targetCard = tCL;

        return tCL;
    }

    public CardLoot MoveToDiscard(CardLoot tCL)
    {
        tCL.state = CLState.discard;
        discardPile.Add(tCL);
        tCL.SetSortingLayerName(layout.discardPile.layerName);
        tCL.SetSortOrder(discardPile.Count * 4);
        tCL.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

        return tCL;
    }

    // The Draw function will pull a single card from the drawPile and return it
    public CardLoot Draw()
    {
        CardLoot cd = drawPile[0]; // Pull the 0th CardLoot

        if (drawPile.Count == 0)
        {
            // If the drawPile is now empty
            // We need to shuffle the discards into the drawPile
            int ndx;
            while (discardPile.Count > 0)
            {
                // Pull a random card from the discard pile
                ndx = Random.Range(0, discardPile.Count);
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }
            ArrangeDrawPile();
            // Show the cards moving to the drawPile
            float t = Time.time;
            foreach (CardLoot tCL in drawPile)
            {
                tCL.transform.localPosition = layout.discardPile.pos;
                tCL.callbackPlayer = null;
                tCL.MoveTo(layout.drawPile.pos);
                tCL.timeStart = t;
                t += 0.02f;
                tCL.state = CLState.toDrawpile;
                tCL.eventualSortLayer = "0";
            }
        }

        drawPile.RemoveAt(0); // Then remove it from List<> drawPile
        return (cd); // And return it
    }

    public void CardClicked(CardLoot tCL)
    {
        if (CURRENT_PLAYER.type != PlayerType.human) return;
        if (phase == TurnPhase.waiting) return;

        switch (tCL.state)
        {
            case CLState.drawpile:
                // Draw the top card, not necessarily the one clicked.
                CardLoot cl = CURRENT_PLAYER.AddCard(Draw());
                cl.callbackPlayer = CURRENT_PLAYER;
                Utils.tr("Loot:CardClicked()", "Draw", cl.name);
                phase = TurnPhase.waiting;
                break;

            case CLState.hand:
                // Check to see whether the card is valid
                if (ValidPlay(tCL))
                {
                    CURRENT_PLAYER.RemoveCard(tCL);
                    MoveToTarget(tCL);
                    tCL.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr("Loot:CardClicked()", "Play", tCL.name, targetCard.name + " is target");
                    phase = TurnPhase.waiting;
                }
                else
                {
                    // Just ignore it but report what the player tried
                    Utils.tr("Loot:CardClicked()", "Attempted to Play", tCL.name, targetCard.name + " is target");
                }
                break;
        }
    }
}
