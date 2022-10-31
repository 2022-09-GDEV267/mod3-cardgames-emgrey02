using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Golf : MonoBehaviour
{

    static public Golf S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset golfLayoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f;
    public Text gameOverText, roundResultText, highScoreText;
    public static int ROUND_NUM = 1;

    [Header("Set Dynamically")]
    public Deck deck;
    public GolfLayout golfLayout;
    public List<CardGolf> drawPile;
    public Transform layoutAnchor;
    public CardGolf target;
    public List<CardGolf> tableau;
    public List<CardGolf> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
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

    void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        golfLayout = GetComponent<GolfLayout>();
        golfLayout.ReadLayout(golfLayoutXML.text);

        drawPile = ConvertListCardsToListCardGolfs(deck.cards);
        flipCards();
        LayoutGame();
    }

    void flipCards()
    {
        foreach (CardGolf card in deck.cards)
        {
            card.faceUp = true;
        }
    }

    List<CardGolf> ConvertListCardsToListCardGolfs(List<Card> lCD)
    {
        List<CardGolf> lCG = new List<CardGolf>();
        CardGolf tCG;
        foreach (Card tCD in lCD)
        {
            tCG = tCD as CardGolf;
            lCG.Add(tCG);
        }
        return (lCG);
    }

    // the draw function will pull a single card from the drawPile and return it
    CardGolf Draw()
    {
        CardGolf cd = drawPile[0]; // pull the 0th CardProspector
        drawPile.RemoveAt(0);
        return (cd);
    }

    // LayoutGame() positions the initial tableau of cards
    void LayoutGame()
    {
        // create an empty GameObject to serve as an anchor for the tableau
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;

        }

        CardGolf cd;
        //follow layout
        foreach (GolfSlotDef tSD in golfLayout.golfSlotDefs)
        {
            cd = Draw(); //pull card from top of draw pile
            cd.faceUp = tSD.faceUp; // set faceUp to value in SlotDef
            cd.transform.parent = layoutAnchor; //make parent layoutAnchor
                                                // this replaces previous parent: deck.deckAnchor, which
                                                // appears as _Deck in the hierarchy when the scene is playing

            // set localPosition of card based on slotDef
            cd.transform.localPosition = new Vector3(golfLayout.multiplier.x * tSD.x,
                                        golfLayout.multiplier.y * tSD.y,
                                        -tSD.layerID);
            cd.layoutID = tSD.id;
            cd.golfSlotDef = tSD;

            //CardGolfs in the tableau have the state GolfCardState.tableau
            cd.state = eGolfCardState.tableau;
            cd.SetSortingLayerName(tSD.layerName);
            tableau.Add(cd); // add CardGolfs to the List<> tableau
        }

        // set which cards are hiding others
        foreach (CardGolf tCG in tableau)
        {
            foreach (int hid in tCG.golfSlotDef.hiddenBy)
            {
                cd = FindCardByLayoutID(hid);
                tCG.hiddenBy.Add(cd);
            }
        }
        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    // convert from layoutID int to the CardProspector with that ID
    CardGolf FindCardByLayoutID(int layoutID)
    {
        foreach (CardGolf tCG in tableau)
        {
            // search through all cards in tableau List<>
            if (tCG.layoutID == layoutID)
            {
                // if card has same ID, return it
                return tCG;
            }
        }
        // if not found, return null
        return null;
    }

    // this turns cards in the Mine face-up or face-down
    void SetTableauFaces()
    {
        foreach (CardGolf cd in tableau)
        {
            bool faceUp = true; //assume card will be face-up
            foreach (CardGolf cover in cd.hiddenBy)
            {
                //if either of the covering cards are in the tableau
                if (cover.state == eGolfCardState.tableau)
                {
                    faceUp = false; // then this card is face-down
                }
            }
            cd.faceUp = faceUp; // set the value on the card
        }

    }

    void MoveToDiscard(CardGolf cd)
    {
        //set state of card to discard
        cd.state = eGolfCardState.discard;
        discardPile.Add(cd);
        cd.transform.parent = layoutAnchor;

        // position this card on the discardPile
        cd.transform.localPosition = new Vector3(
            golfLayout.multiplier.x * golfLayout.discardPile.x,
            golfLayout.multiplier.y * golfLayout.discardPile.y,
            -golfLayout.discardPile.layerID + 0.5f);
        cd.faceUp = true;

        // place it on top of the pile for depth sorting
        cd.SetSortingLayerName(golfLayout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    void MoveToTarget(CardGolf cd)
    {
        // if there is currently a target card, move it to discardPile
        if (target != null) MoveToDiscard(target);
        target = cd; // cd is new target
        cd.state = eGolfCardState.target;
        cd.transform.parent = layoutAnchor;

        // move to target position
        cd.transform.localPosition = new Vector3(
            golfLayout.multiplier.x * golfLayout.discardPile.x,
            golfLayout.multiplier.y * golfLayout.discardPile.y,
            -golfLayout.discardPile.layerID);
        cd.faceUp = true;

        //set the depth sorting
        cd.SetSortingLayerName(golfLayout.discardPile.layerName);
        cd.SetSortOrder(0);

    }

    // arranges all the cards of the drawPile to show how many are left
    void UpdateDrawPile()
    {
        CardGolf cd;
        // go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            // position it correctly with the layout.drawPile.stagger
            Vector2 dpStagger = golfLayout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                golfLayout.multiplier.x * (golfLayout.drawPile.x + i * dpStagger.x),
                golfLayout.multiplier.y * (golfLayout.drawPile.y + i * dpStagger.y),
                -golfLayout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = eGolfCardState.drawpile;

            // set depth sorting
            cd.SetSortingLayerName(golfLayout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardGolf cd)
    {
        // the reaction is determined by the state of the clicked card
        switch (cd.state)
        {
            case eGolfCardState.target:
                break;

            case eGolfCardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                break;

            case eGolfCardState.tableau:
                bool validMatch = true;
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
                break;
        }
        CheckForRoundOver();
    }

    void CheckForRoundOver()
    {
        // if tableau empty, round over
        if (tableau.Count == 0)
        {
            RoundOver();
            return;
        }
        if (drawPile.Count > 0)
        {
            return;
        }
        // check for remaining valid plays
        foreach (CardGolf cd in tableau)
        {
            if (AdjacentRank(cd, target))
            {
                // if there is a valid play, the game's not over
                return;
            }
        }

        // since there are no valid plays, game is over
        RoundOver();
    }

    // called when round is over
    void RoundOver()
    {
        ROUND_NUM++;
        int roundScore = GolfScoreManager.ROUND_SCORE;
        int totalScore = GolfScoreManager.TOTAL_SCORE;
        gameOverText.text = "Round Over";
        roundResultText.text = "Round Score: " + roundScore;
        ShowResultsUI(true);
        GolfScoreManager.EVENT(eGolfScoreEvent.roundOver);

        if (ROUND_NUM > 9)
        {
            gameOverText.text = "Game Over";
            if (GolfScoreManager.BEST_SCORE > totalScore)
            {
                string str = "You got the high score!\nHigh score: " + totalScore;
                roundResultText.text = str;
            }
            else
            {
                roundResultText.text = "Your final score was: " + totalScore;
            }
            ShowResultsUI(true);
            GolfScoreManager.EVENT(eGolfScoreEvent.gameOver);

        }

        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        SceneManager.LoadScene("Golf_Solitaire");
    }

    // return true if the two cards are adjacent in rank
    public bool AdjacentRank(CardGolf c0, CardGolf c1)
    {

        // if they are 1 apart, they are adjacent
        if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return true;
        }

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
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }
}
