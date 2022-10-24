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

		MoveToTarget(Draw());
		UpdateDrawPile();
	}

	void MoveToDiscard(CardProspector cd)
	{
		//set state of card to discard
		cd.state = eCardState.discard;
		discardPile.Add(cd);
		cd.transform.parent = layoutAnchor;

		// position this card on the discardPile
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID + 0.5f);
		cd.faceUp = true;

		// place it on top of the pile for depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(-100 + discardPile.Count);
	}

	void MoveToTarget(CardProspector cd)
	{
		// if there is currently a target card, move it to discardPile
		if (target != null) MoveToDiscard(target);
		target = cd; // cd is new target
		cd.state = eCardState.target;
		cd.transform.parent = layoutAnchor;

		// move to target position
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID);
		cd.faceUp = true;

		//set the depth sorting
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(0);

	}

	// arranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile()
	{
		CardProspector cd;
		// go through all the cards of the drawPile
		for (int i = 0; i < drawPile.Count; i++)
		{
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;

			// position it correctly with the layout.drawPile.stagger
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
				layout.multiplier.x * (layout.drawPile.x + i*dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i*dpStagger.y),
				-layout.drawPile.layerID + 0.1f * i);
			cd.faceUp = false;
			cd.state = eCardState.drawpile;

			// set depth sorting
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder(-10 * i);
		}
	}

	public void CardClicked(CardProspector cd)
	{
		// the reaction is determined by the state of the clicked card
		switch(cd.state)
		{
			case eCardState.target:
				break;

			case eCardState.drawpile:
				MoveToDiscard(target);
				MoveToTarget(Draw());
				UpdateDrawPile();
				break;

			case eCardState.tableau:
				bool validMatch = true;
				if (!cd.faceUp)
				{
					// if card is face-down, it's not valid
					validMatch = false;
				}
				if (!AdjacentRank(cd, target))
				{
					// if it's not an adjacent rank, it's not valid
					validMatch = false;
				}
				if (!validMatch) return; // return if not valid

				// if we got here then its a valid card
				tableau.Remove(cd);
				MoveToTarget(cd);
				break;
		}
	}

	// return true if the two cards are adjacent in rank
	public bool AdjacentRank(CardProspector c0, CardProspector c1)
	{
		// if either card is face-down, it's not adjacent.
		if (!c0.faceUp || !c1.faceUp) return false;

		// if they are 1 apart, they are adjacent
		if (Mathf.Abs(c0.rank - c1.rank) == 1)
		{
			return true;
		}

		// if one is Ace and the other King, they are adjacent
		if (c0.rank == 1 && c1.rank == 13) return true;
		if (c0.rank == 13 && c1.rank == 1) return true;

		// otherwise, return false
		return false;
	}
}
