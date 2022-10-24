using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Vector3 layoutCenter;

	[Header("Set Dynamically")]
	public Deck	deck;
	public Layout layout;
	public List<CardProspector> drawPile;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;

	void Awake(){
		S = this;
	}

	void Start() {
		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle(ref deck.cards);

		layout = GetComponent<Layout> ();
		layout.ReadLayout(layoutXML.text);

		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
	{
		List<CardProspector> lCP = new List<CardProspector>();
		CardProspector tCP;
		foreach(Card tCD in lCD)
		{
			tCP = tCD as CardProspector;
			lCP.Add(tCP);
		}
		return (lCP);
	}

	// the draw function will pull a single card from the drawPile and return it
	CardProspector Draw()
	{
		CardProspector cd = drawPile[0]; // pull the 0th CardProspector
		drawPile.RemoveAt(0);
		return (cd);
	}

	// LayoutGame() positions the initial tableau of cards, a.k.a. the "mine"
	void LayoutGame()
	{
		// create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null)
		{
			GameObject tGO = new GameObject("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;

		}

		CardProspector cp;
		//follow layout
		foreach(SlotDef tSD in layout.slotDefs)
		{
			cp = Draw(); //pull card from top of draw pile
			cp.faceUp = tSD.faceUp; // set faceUp to value in SlotDef
			cp.transform.parent = layoutAnchor; //make parent layoutAnchor
			// this replaces previous parent: deck.deckAnchor, which
			// appears as _Deck in the hierarchy when the scene is playing

			// set localPosition of card based on slotDef
			cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x,
										layout.multiplier.y * tSD.y,
										-tSD.layerID);
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;

			//CardProspectors in the tableau have the state CardState.tableau
			cp.state = eCardState.tableau;
			cp.SetSortingLayerName(tSD.layerName);
			tableau.Add(cp); // add CardProspector to the List<> tableau
		}
	}
}
