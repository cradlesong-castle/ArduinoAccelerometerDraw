using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameController : MonoBehaviour
{
    //UI Elements
    public Text wordToDraw;
    public Text hint;
    public Text timer;
    public Text currentDrawer;
    public Text finishtext;

    //Game Objects
    public GameObject EachRound;
    public GameObject turnAround;
    public GameObject ready;
    public GameObject wordtoDraw;
    public GameObject FinishRound;
    

    //Numbers
    private int randomNum;
    private int numofChars;
    public float turnTime;
    private int correctAns = 10;
    private int team;
    public int currentwordcount = 0;

    //Score
    public GameObject scoreTracker;

    //Words
    public string nameofItem;
    public string[] guessStrarry = new string[10] { "House", "Waterfall","Horse", "Python", "Staircase", "Tree", "Cat", "Banana", "Chair", "Monkey"};
    private string[] temparry = new string[10];
    public string currentword;

    //Booleans
    public bool isT1Draw = false;
    public bool isT2Draw = false;
    public bool startround = false;
    public bool gameover = false;


    //Misc
    public List<int> noDuplicateList = new List<int>();
    public GameObject MoveCube;



    // Start is called before the first frame update
    void Start()
    {
        team = 1;
        gameover = false;
        FinishRound.SetActive(false);
        turnTime = 60;
        timer.text = "Time Left: " + turnTime.ToString("F0");
        RandomWord();
        wordToDraw.text = "Draw: " + currentword;
        isT1Draw = true;
        isT2Draw = false;
        currentDrawer.text = "Team 1's Turn";
        startround = false;
        hint.gameObject.SetActive(false);
        StartCoroutine(DisableAfterTime(2.0f));

    }

    // Update is called once per frame
    void Update()
    {
        if(gameover == false)
        {
            if (turnTime >= 0 && startround == true)
            {
                turnTime -= Time.deltaTime;
                timer.text = "Time Left: " + turnTime.ToString("F1");
            }
            else if (turnTime <= 0)
            {
                MoveCube.GetComponent<MoveCube>().ClearLines();
                if (isT1Draw == true)
                {
                    isT2Draw = true;
                    isT1Draw = false;

                    turnTime = 60;
                    timer.text = "Time Left: " + turnTime.ToString("F1");                
                    RandomWord();
                    wordToDraw.text = "Draw: " + currentword;
                   
                    startround = false;
                    hint.gameObject.SetActive(false);
                    if (currentwordcount < 10)
                    {
                        StartCoroutine(DisableAfterTime(2.0f));
                    }

                }
                else if(isT2Draw == true)
                {
                    isT1Draw = true;
                    isT2Draw = false;

                    turnTime = 60;
                    timer.text = "Time Left: " + turnTime.ToString("F1");                
                    RandomWord();
                    wordToDraw.text = "Draw: " + currentword;
              
                    startround = false;
                    hint.gameObject.SetActive(false);
                    if (currentwordcount < 10)
                    {
                        StartCoroutine(DisableAfterTime(2.0f));
                    }

                }
            }

            if (isT1Draw == true)
            {
                currentDrawer.text = "Team 1's Turn";
                hint.text = "Hint: It is a " + currentword.Length + " letter word";

            }
            if (isT2Draw == true)
            {
                currentDrawer.text = "Team 2's Turn";
                hint.text = "Hint: It is a " + currentword.Length + " letter word";
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                turnTime = 60;
                timer.text = "Time Left: " + turnTime.ToString("F1");
                scoreTracker.GetComponent<ScoreSystem>().t1Score += correctAns;
                RandomWord();
                wordToDraw.text = "Draw: " + currentword;
                isT2Draw = true;
                isT1Draw = false;
                startround = false;
                hint.gameObject.SetActive(false);
                MoveCube.GetComponent<MoveCube>().ClearLines();
                if (currentwordcount < 10)
                {
                    StartCoroutine(DisableAfterTime(2.0f));
                }
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                turnTime = 60;
                timer.text = "Time Left: " + turnTime.ToString("F1");
                scoreTracker.GetComponent<ScoreSystem>().t2Score += correctAns;
                RandomWord();
                wordToDraw.text = "Draw: " + currentword;
                isT1Draw = true;
                isT2Draw = false;
                startround = false;
                hint.gameObject.SetActive(false);
                MoveCube.GetComponent<MoveCube>().ClearLines();
                if (currentwordcount < 10)
                {
                    StartCoroutine(DisableAfterTime(2.0f));
                }
            
                
            }

            

            if(currentwordcount >= 10)
            {
                gameover = true;
            }
        }

        else
        {
            FinishRound.SetActive(true);
            if(scoreTracker.GetComponent<ScoreSystem>().t1Score > scoreTracker.GetComponent<ScoreSystem>().t2Score)
            {
                finishtext.text = "Game Over! Team 1 has won!";
            }
            else if(scoreTracker.GetComponent<ScoreSystem>().t2Score > scoreTracker.GetComponent<ScoreSystem>().t1Score)
            {
                finishtext.text = "Game Over! Team 2 has won!";
            }
            else
            {
                finishtext.text = "Game Over! It's a Draw!";
            }
        }
       
    }


    IEnumerator DisableAfterTime(float sec)
    {
        EachRound.gameObject.SetActive(true);
        turnAround.gameObject.SetActive(true);
      
        yield return new WaitForSeconds(sec);
        turnAround.gameObject.SetActive(false);
        
        ready.SetActive(true);
        yield return new WaitForSeconds(sec);
        
        ready.SetActive(false);
        yield return new WaitForSeconds(sec);
       
        wordtoDraw.SetActive(true);
        yield return new WaitForSeconds(sec);
        wordtoDraw.SetActive(false);
        EachRound.gameObject.SetActive(false);
        hint.gameObject.SetActive(true);
        startround = true;
        
    }

    public void RandomWord()
    {
        
        if(currentwordcount < 11)
        {
            currentword = guessStrarry[currentwordcount];
            currentwordcount++;
        }
        
    }


    public void playagainbtnclick()
    {
        SceneManager.LoadScene("Game");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
