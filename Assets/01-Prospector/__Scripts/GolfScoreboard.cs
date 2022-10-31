using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GolfScoreboard : MonoBehaviour
{
    public static GolfScoreboard S; // the singleton for Scoreboard

    [Header("Set in Inspector")]
    public GameObject prefabFloatingScore;

    [Header("Set Dynamically")]
    [SerializeField] private int _score = 0;
    [SerializeField] private string _scoreString;

    // the score property also sets the scoreString
    public int score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            scoreString = _score.ToString("N0");
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
            Debug.LogError("ERROR: Scoreboard.Awake(): S is already set!");
        }
    }
}
