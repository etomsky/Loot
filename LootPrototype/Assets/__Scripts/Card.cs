using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {
    [Header("Set Dynamically")]
    public string cardColor; // Color of the Card (B,G,P, or Y)
    public int value; // Value of the Card (1-4)
    public Color color = Color.blue; // Color to tint pips
    public string colS = "Blue"; // Or "Yellow", "Green", "Purple", Name of the Color

    public GameObject back; // The GameObject of the back of the card

    public CardDefinition def; // Parsed from DeckXML.xml

    // List of the SpriteRenderer Components of this GameObject and its children
    public SpriteRenderer[] spriteRenderers;

    private void Start()
    {
        SetSortOrder(0); // Ensures that the card starts properly depth sorted
    }

    //If SpriteRenderers is not yet defined, this function defines it
    public void PopulateSpriteRenderers()
    {
        // If spriteRenderers is null or empty
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            // Get SpriteRenderer Components of this GameObject and its children
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    // Sets the sortingLayerName on all SpriteRenderer Components
    public void SetSortingLayerName(string tSLN)
    {
        PopulateSpriteRenderers();

        foreach(SpriteRenderer tSR in spriteRenderers)
        {
            tSR.sortingLayerName = tSLN;
        }
    }

    // Sets the sortingOrder of all SpriteRenderer Components
    public void SetSortOrder(int sOrd)
    {
        PopulateSpriteRenderers();

        // Iterate through all the spriteRenderers as tSR
        foreach (SpriteRenderer tSR in spriteRenderers)
        {
            if (tSR.gameObject == this.gameObject)
            {
                // If the gameObject is this.gameObject, it's the background
                tSR.sortingOrder = sOrd; // Set its order to sOrd
                continue; // And continue to the next iteration of the loop
            }

            // Each of the children of this GameObject are named
            // switch based on the names
            switch (tSR.gameObject.name)
            {
                case "back": // If the name is "back"
                    // Set it to the highest layer to cover the other sprites
                    tSR.sortingOrder = sOrd + 2;
                    break;

                case "card": // If the name is "card"
                default: // or if it's anything else
                    // Set it to the middle layer to be above the background
                    tSR.sortingOrder = sOrd + 1;
                    break;
            }
        }
    }

    public bool faceUp
    {
        get
        {
            return (!back.activeSelf);
        }
        set
        {
            back.SetActive(!value);
        }
    }

    // Virtual methods can be overridden by subclass methods with the same name
    virtual public void OnMouseUpAsButton()
    {
        print(name); // When clicked, this outputs the card name
    }
} // class Card

[System.Serializable]
public class CardDefinition
{
    public string sprite; // Sprite to use for each card
    public int value; // The value (1-4) of this card
    public string cardColor; // The color (B, G, P, Y) of this card
}
