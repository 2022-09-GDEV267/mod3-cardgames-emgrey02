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
	public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
	public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
	public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
	public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
	public float reloadDelay = 2f;
	public Text gameOverText, roundResultText, highScoreText;

    public Sprite cardBack;
	public Sprite cardBackGold;
    public Sprite cardFront;
	public Sprite cardFrontGold;

    [Header("Set Dynamically")]
	public Deck	deck;
	public Layout layout;
	public List<CardProspector> drawPile;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;
	public FloatingScore fsRun;

	void Awake(){
		S = this;
		SetUpUITexts();
	}

	void SetUpUITexts()
	{
		// set up the higScore UI Text
		GameObject go = GameObject.Find("HighScore");
		if (go != null)
		{
			highScoreText = go.GetComponent<Text>();
		}
		int highScore = ScoreManager.HIGH_SCORE;
		string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
		go.GetComponent<Text>().text = hScore;

		// set up the UI Texts that show at the end of the round
		go = GameObject.Find("GameOver");
		if (go != null)
		{
			gameOverText = go.GetComponent<Text>();
		}

		go = GameObject.Find("RoundResult");
		if (go != null)
		{
			roundResultText = go.GetComponent<Text>();
		}

		ShowResultsUI(false);
	}

	void ShowResultsUI(bool show)
	{
		gameOverText.gameObject.SetActive(show);
		roundResultText.gameObject.SetActive(show);
	}

	void Start() {
		Scoreboard.S.score = ScoreManager.SCORE;
		deck = GetComponent<Deck> ();
		deck.InitDeck(deckXML.text);
		Deck.Shuffle(ref deck.cards);

		layout = GetComponent<Layout> ();
		layout.ReadLayout(layoutXML.text);

		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		flipCards();
		//LayoutGame();
	}

	void flipCards()
    {
		foreach (CardProspector card in deck.cards)
        {
			card.faceUp = true;
        }
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

		// set which cards are hiding others
		foreach (CardProspector tCP in tableau)
		{
			foreach (int hid in tCP.slotDef.hiddenBy)
			{
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
		}
		SetGoldCards();
		MoveToTarget(Draw());
		UpdateDrawPile();
	}

	// convert from layoutID int to the CardProspector with that ID
	CardProspector FindCardByLayoutID(int layoutID)
	{
		foreach (CardProspector tCP in tableau)
		{
			// search through all cards in tableau List<>
			if (tCP.layoutID == layoutID)
			{
				// if card has same ID, return it
				return tCP;
			}
		}
		// if not found, return null
		return null;
	}

	// this turns cards in the Mine face-up or face-down
	void SetTableauFaces()
	{
		foreach(CardProspector cd in tableau)
		{
			bool faceUp = true; //assume card will be face-up
			foreach(CardProspector cover in cd.hiddenBy)
			{
				//if either of the covering cards are in the tableau
				if (cover.state == eCardState.tableau)
				{
					faceUp = false; // then this card is face-down
				}
			}
			cd.faceUp = faceUp; // set the value on the card
		}
		
	}

	void SetGoldCards()
	{
		foreach(CardProspector cd in tableau)
		{
            bool isGold = Random.value < 0.1f; // 10% chance this is a gold card
			GameObject card = cd.gameObject;
			card.GetComponent<SpriteRenderer>().sprite = isGold ? cardFrontGold : cardFront;
			GameObject cardBackgo = card.transform.GetChild(card.transform.childCount - 1).gameObject;
            cardBackgo.GetComponent<SpriteRenderer>().sprite = isGold ? cardBackGold : cardBack;
            cd.isGold = isGold;
        }
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
		bool isGold = cd.isGold;
		// the reaction is determined by the state of the clicked card
		switch(cd.state)
		{
			case eCardState.target:
				break;

			case eCardState.drawpile:
				MoveToDiscard(target);
				MoveToTarget(Draw());
				UpdateDrawPile();
				ScoreManager.EVENT(eScoreEvent.draw);
				FloatingScoreHandler(eScoreEvent.draw);
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
				SetTableauFaces();
				if (isGold)
				{
					ScoreManager.EVENT(eScoreEvent.mineGold);
				} else
				{
					ScoreManager.EVENT(eScoreEvent.mine);
				}
				FloatingScoreHandler(eScoreEvent.mine);
				break;
		}
		CheckForGameOver();
	}

	void CheckForGameOver()
	{
		// if tableau empty, game over
		if (tableau.Count == 0)
		{
			GameOver(true);
			return;
		}
		if (drawPile.Count > 0)
		{
			return;
		}
		// check for remaining valid plays
		foreach (CardProspector cd in tableau)
		{
			if (AdjacentRank(cd, target))
			{
				// if there is a valid play, the game's not over
				return;
			}
		}

		// since there are no valid plays, game is over
		GameOver(false);
	}

	// called when game is over
	void GameOver(bool won)
	{
		int score = ScoreManager.SCORE;
		if (fsRun != null) score += fsRun.score;
		if (won)
		{
			gameOverText.text = "Round Over";
			roundResultText.text = "You won this round!\nRound Score: " + score;
			ShowResultsUI(true);
			ScoreManager.EVENT(eScoreEvent.gameWin);
			FloatingScoreHandler(eScoreEvent.gameWin);
		} else
		{
			gameOverText.text = "Game Over";
			if (ScoreManager.HIGH_SCORE <= score)
			{
				string str = "You got the high score!\nHigh score: " + score;
				roundResultText.text = str;
			} else
			{
				roundResultText.text = "Your final score was: " + score;
			}
			ShowResultsUI(true);
            ScoreManager.EVENT(eScoreEvent.gameLoss);
			FloatingScoreHandler(eScoreEvent.gameLoss);
        }

		Invoke("ReloadLevel", reloadDelay);
	}

	void ReloadLevel()
	{
		SceneManager.LoadScene("__Prospector");
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

    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {
            // Same things need to happen whether it's a draw, a win, or a loss
            case eScoreEvent.draw:     // Drawing a card
            case eScoreEvent.gameWin:  // Won the round
            case eScoreEvent.gameLoss: // Lost the round
                // Add fsRun to the Scoreboard score
                if (fsRun != null)
                {
                    // Create points for the Bézier curve1
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    // Also adjust the fontSize
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null; // Clear fsRun so it's created again
                }
                break;

            case eScoreEvent.mine: // Remove a mine card
                // Create a FloatingScore for this score
                FloatingScore fs;
                // Move it from the mousePosition to fsPosRun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
				
				if (fsRun == null)
				{
					fsRun = fs;
					fsRun.reportFinishTo = null;
				} else
				{
					fs.reportFinishTo = fsRun.gameObject;
				}
                break;
        }
    }
}
