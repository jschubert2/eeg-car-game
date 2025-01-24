using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerScript : MonoBehaviour
{
    public TMP_Text timerText; //timer UI element
    private float timer = 0f; 
    private bool isTiming = false; //is the timer running?
    public GameObject WinTxt;
    public GameObject TimerObj;
    void Start()
    {
        WinTxt.SetActive(false);
        TimerObj.SetActive(false);
    }
    void Update()
    {
       
        if (isTiming)
        {
            timer += Time.deltaTime;
            DisplayTime(timer);
        }
    }

    //format time 
    void DisplayTime(float timeToDisplay)
    {
        int minutes = Mathf.FloorToInt(timeToDisplay / 60); // Get minutes
        int seconds = Mathf.FloorToInt(timeToDisplay % 60); // Get seconds

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    //Stop the timer and display the win text
    public void StopTimer()
    {
        isTiming = false;
        WinTxt.SetActive(true);
    }
    //start timer
    public void StartTimer()
    {
        isTiming = true;
        TimerObj.SetActive(true);
    }

}
