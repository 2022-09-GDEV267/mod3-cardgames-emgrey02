using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum to handle all the possible scoring events
public enum eGolfScoreEvent
{
    roundOver,
    gameOver,
}

public class GolfScoreManager : MonoBehaviour
{
    static private GolfScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int BEST_SCORE = 0;

    [Header("Set Dynamically")]
    // fields to track score info
    public int roundScore = 0;
    public int totalScore = 0;

    void Awake()
    {
        if (S == null)
        {
            S = this;
        }
        else
        {
            Debug.LogError("ERROR: ScoreManager.Awake(): S is already set!");
        }

        // check for high score in playerprefs
        if (PlayerPrefs.HasKey("GolfBestScore"))
        {
            BEST_SCORE = PlayerPrefs.GetInt("GolfBestScore");
        }

        // add the score from last round, which will be >0 if it was a win
        roundScore += SCORE_FROM_PREV_ROUND;

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
            Debug.LogError("ScoreManager:EVENT() called while S = null\n" + nre);
        }
    }

    void Event(eGolfScoreEvent evt)
    {
        switch (evt)
        {
            case eGolfScoreEvent.roundOver:
            case eGolfScoreEvent.gameOver:
                roundScore = Golf.S.tableau.Count;          
                break;

        }

        switch (evt)
        {
            case eGolfScoreEvent.roundOver:
                // if round is over, add score to next round
                // static fields aren't reset by SceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND += roundScore;
                print("You finished this round with " + roundScore + " points!");
                break;

            case eGolfScoreEvent.gameOver:
                // if game over, check against high score
                totalScore = SCORE_FROM_PREV_ROUND;
                if (BEST_SCORE > totalScore)
                {
                    print("You got the high score! High score: " + totalScore);
                    BEST_SCORE = totalScore;
                    PlayerPrefs.SetInt("GolfBestScore", totalScore);
                }
                else
                {
                    print("Your final score for the game was: " + totalScore + " points");
                }
                break;

            default:
                print("score: " + totalScore);
                break;
        }
    }
    static public int ROUND_SCORE { get { return S.roundScore; } }
    static public int TOTAL_SCORE { get { return S.totalScore; } }
}
