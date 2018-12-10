using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LootDeck : MonoBehaviour
{

    [Header("Set in Inspector")]
    public bool startFaceUp = false;
    // Card Colors
    public Sprite cardColorBlue;
    public Sprite cardColorGreen;
    public Sprite cardColorPurple;
    public Sprite cardColorYellow;
    public Sprite[] cardSprites;
    public Sprite[] valueSprites;
    public Sprite cardBack;
    public Sprite cardFront;
    // Prefabs
    public GameObject prefabCard;
    public GameObject prefabSprite;

    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;
    public List<string> cardNames;
    public List<Card> cards;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictCardColors;
    public Color purple = new Color32(210 / 255, 0 / 255, 255 / 255, 255 / 255);

    // InitDeck is called by Prospector when it is ready
    public void InitDeck(string lootDeckXMLText)
    {
        // This creates an anchor for all the Card GameObjects in the Hierarchy
        if (GameObject.Find("_Deck") == null)
        {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }

        // Initialize the Dictionary of SuitSprites with necessary Sprites
        dictCardColors = new Dictionary<string, Sprite>()
        {
            {"B", cardColorBlue},
            {"G", cardColorGreen},
            {"P", cardColorPurple},
            {"Y", cardColorYellow}
        };

        ReadDeck(lootDeckXMLText);

        MakeCards();
    }

    // ReadDeck parses the XML file passed to it into CardDefinitions
    public void ReadDeck(string lootDeckXMLText)
    {
        xmlr = new PT_XMLReader(); // Create a new PT_XMLReader
        xmlr.Parse(lootDeckXMLText); // Use that PT_XMLReader to parse DeckXML

        // Read skull locations for each card number
        cardDefs = new List<CardDefinition>(); // Init the List of Cards
        // Grab an PT_XMLHashList of all the <card>s in the XML file
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefs.Count; i++)
        {
            // For each of the <card>s
            // Create a new CardDefinition
            CardDefinition cDef = new CardDefinition();
            // Parse the attribute values and add them to cDef
            cDef.value = int.Parse(xCardDefs[i].att("value"));
        }
    }

    // Get the proper CardDefinition based on Value (1 to 4 is one skull to four skulls)
    public CardDefinition GetCardDefinitionByValue(int val)
    {
        // Search through all of the CardDefinitions
        foreach (CardDefinition cd in cardDefs)
        {
            // If the rank is correct, return this definition
            if (cd.value == val)
            {
                return (cd);
            }
        }
        return (null);
    }

    // Make the Card GameObjects
    public void MakeCards()
    {
        // cardNames will be the names of cards to build
        // Each card color goes from 1 to 4 (e.g., B1 to B4 for Blues)
        cardNames = new List<string>();
        string[] letters = new string[] { "B", "G", "P", "Y" };
        foreach (string s in letters)
        {
            for (int i = 0; i < 4; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }

        // Make a list to hold all the cards
        cards = new List<Card>();

        // Iterate through all of the card names that were just made
        for (int i = 0; i < cardNames.Count; i++)
        {
            // Make the card and add it to the cards Deck
            cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum)
    {
        // Create a new Card GameObject
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        // Set the transform.parent of the new card to the anchor.
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>(); // Get the Card Component

        // This line stacks the cards so that they're all in nice rows
        cgo.transform.localPosition = new Vector3((cNum % 4) * 3, cNum / 4 * 4, 0);

        // Assign basic values to the Card
        card.name = cardNames[cNum];
        card.cardColor = card.name[0].ToString();
        card.value = int.Parse(card.name.Substring(1));
        if (card.cardColor == "G")
        {
            card.colS = "Green";
            card.color = Color.green;
        }
        if (card.cardColor == "P")
        {
            card.colS = "Purple";
            card.color = new Color32(210 / 255, 0 / 255, 255 / 255, 255 / 255);
        }
        if (card.cardColor == "Y")
        {
            card.colS = "Yellow";
            card.color = Color.yellow;
        }
        // Pull the CardDefinition for this card
        card.def = GetCardDefinitionByValue(card.value);

        AddCard(card);
        AddBack(card);

        return card;
    }

    // These private variables will be reused several times in helper methods
    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;

    private void AddCard(Card card)
    {
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        // Generate the right name and pass it to GetFace()
        _tSp = GetCard("merch_5");
        //_tSp = GetCard(card.def.value + card.cardColor);
        _tSR.sprite = _tSp; // Assign this Sprite to _tSR
        _tSR.sortingOrder = 1; // Set the sortingOrder
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "card";
    }

    // Find the proper face card Sprite
    private Sprite GetCard(string cardS)
    {
        foreach (Sprite _tSP in cardSprites)
        {
            // If this Sprite has the right name...
            if (_tSP.name == cardS)
            {
                // ...then return the Sprite
                return (_tSP);
            }
        }
        // If nothing can be found, return null
        return (null);
    }

    private void AddBack(Card card)
    {
        // Add Card Back
        // The Card Back will be able to cover everything else on the Card
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        // This is a higher sortingOrder than anything else
        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;
        // Default to face-up
        card.faceUp = startFaceUp; // Use the property faceUp of Card
    }

    static public void Shuffle(ref List<Card> oCards)
    {
        // Create a temporary List to hold the new shuffle order
        List<Card> tCards = new List<Card>();

        int ndx; // This will hold the index of the card to be moved
        tCards = new List<Card>(); // Initialize the temporary List
        // Repeat as long as there are cards in the original List
        while (oCards.Count > 0)
        {
            // Pick the index of a random card
            ndx = Random.Range(0, oCards.Count);
            // Add that card to the temporary List
            tCards.Add(oCards[ndx]);
            // And remove that card from the original List
            oCards.RemoveAt(ndx);
        }
        // Replace the original List with the temporary List
        oCards = tCards;
        // Because oCards is a reference (ref) parameter, the original argument
        // that was passed in is changed as well.
    }
}
