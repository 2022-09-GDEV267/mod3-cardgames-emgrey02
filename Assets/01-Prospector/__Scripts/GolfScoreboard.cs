using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GolfScoreboard : MonoBehaviour
{
    public static GolfScoreboard S; // the singleton for Scoreboard

    [Header("Set Dynamically")]
    [SerializeField] private int _roundScore = 0;
    [SerializeField] private string _scoreString;

    // the score property also sets the scoreString
    public int roundScore
    {
        get
        {
            return _roundScore;
        }
        set
        {
            _roundScore = value;
            scoreString = _roundScore.ToString("N0");
        }
    }

    public string scoreString
    {
        get
        {
            return _scoreString;
        }
        set
        {
            _scoreString = value;
            GetComponent<Text>().text = _scoreString;
        }
        
    }

    void Awake()
    {
        if (S == null)
        {
            S = this;
        }
        else
        {
            Debug.LogError("ERROR: GolfScoreboard.Awake(): S is already set!");
        }
    }

    public void UpdateScore()
    {
        roundScore = GolfScoreManager.ROUND_SCORE;
        Text scoreText = gameObject.GetComponent<Text>();
        scoreText.text= "Score: " + scoreString;
    }
}

