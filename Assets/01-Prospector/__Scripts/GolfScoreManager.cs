using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum to handle all the possible scoring events
public enum eGolfScoreEvent
{
    tableau,
    roundOver,
    gameOver,
}

public class GolfScoreManager : MonoBehaviour
{
    static private GolfScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int BEST_SCORE = 1000;
    static public int TOTAL_SCORE = 0;

    [Header("Set Dynamically")]
    // fields to track score info
    public int roundScore = 0;

    void Awake()
    {
        if (S == null)
        {
            S = this;
        }
        else
        {
            Debug.LogError("ERROR: GolfScoreManager.Awake(): S is already set!");
        }

        // check for high score in playerprefs
        if (PlayerPrefs.HasKey("GolfBestScore"))
        {
            BEST_SCORE = PlayerPrefs.GetInt("GolfBestScore");
        }

        // add the score from last round, which will be >0 if it was a win
        //TOTAL_SCORE += SCORE_FROM_PREV_ROUND;

        roundScore = 35;

        // now reset it
        SCORE_FROM_PREV_ROUND = 0;
    }

    static public void EVENT(eGolfScoreEvent evt)
    {
        try
        {
            S.Event(evt);
        }
        catch (System.NullReferenceException nre)
        {
            Debug.LogError("GolfScoreManager:EVENT() called while S = null\n" + nre);
        }
    }

    void Event(eGolfScoreEvent evt)
    {
        switch (evt)
        {
            case eGolfScoreEvent.tableau:
                //update score whenever tableau changes
                roundScore = Golf.S.tableau.Count;
                break;

            case eGolfScoreEvent.roundOver:
            case eGolfScoreEvent.gameOver:
                //how many cards are in the tableau = score for this round
                roundScore = Golf.S.tableau.Count;          
                break;

        }

        switch (evt)
        {
            case eGolfScoreEvent.roundOver:
                // if round is over, add score to next round
                // static fields aren't reset by GolfSceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND = roundScore;
                TOTAL_SCORE += SCORE_FROM_PREV_ROUND;
                //print("You finished this round with " + roundScore + " points!");
                break;

            case eGolfScoreEvent.gameOver:
                // if game over, check against best score
                TOTAL_SCORE += roundScore;
                if (TOTAL_SCORE < BEST_SCORE)
                {
                    //print("You got the high score! High score: " + TOTAL_SCORE);
                    BEST_SCORE = TOTAL_SCORE;
                    PlayerPrefs.SetInt("GolfBestScore", TOTAL_SCORE);
                }
                else
                {
                    //print("Your final score for the game was: " + TOTAL_SCORE + " points");
                }
                TOTAL_SCORE = 0;
                break;

            default:
                print("score: " + TOTAL_SCORE);
                break;
        }
    }
    static public int ROUND_SCORE { get { return S.roundScore; } }
}
