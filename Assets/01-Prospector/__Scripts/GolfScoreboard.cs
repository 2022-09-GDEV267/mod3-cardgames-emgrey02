using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GolfScoreboard : MonoBehaviour
{
    public static GolfScoreboard S; // the singleton for Scoreboard

    [Header("Set Dynamically")]
    [SerializeField] private int _roundScore = 0;
    [SerializeField] private string _scoreString;

    [SerializeField] private int _totalScore = 0;
    [SerializeField] private string _totalString;

    public Text Score;
    public Text TotalScore;

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
            Score.text = "Score: " + _scoreString;
        }
        
    }

    public int totalScore
    {
        get
        {
            return _totalScore;
        }
        set
        {
            _totalScore = value;
            totalString = _totalScore.ToString("N0");
        }
    }

    public string totalString
    {
        get
        {
            return _totalString;
        }
        set
        {
            _totalString = value;
            TotalScore.text = "Total Score: " + _totalString;
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
}

