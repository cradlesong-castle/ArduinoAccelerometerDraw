using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreSystem : MonoBehaviour
{

    public int t1Score;
    public int t2Score;
    public Text t1ScoreText;
    public Text t2ScoreText;

    public int scoreToWin;
    public int currScore;

    public bool isNewGame = false;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        t1ScoreText.text = "Team 1: " + t1Score;
        t2ScoreText.text = "Team 2: " + t2Score;
    }
}
