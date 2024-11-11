using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerScript : MonoBehaviour
{
    public TMP_Text timerText; // Reference to the UI Text to display the timer
    private float timer = 0f; // Variable to store the time
    private bool isTiming = false; // Flag to control if the timer is running
    public GameObject WinTxt;
    public GameObject TimerObj;
    void Start()
    {
        WinTxt.SetActive(false);
        TimerObj.SetActive(false);
    }
    void Update()
    {
        // If the timer is active, increment and display it
        if (isTiming)
        {
            timer += Time.deltaTime;
            DisplayTime(timer);
        }
    }

    // Method to display the time in minutes and seconds format
    void DisplayTime(float timeToDisplay)
    {
        int minutes = Mathf.FloorToInt(timeToDisplay / 60); // Get minutes
        int seconds = Mathf.FloorToInt(timeToDisplay % 60); // Get seconds

        // Format and display the time
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Method to stop the timer, can be called by the trigger event
    public void StopTimer()
    {
        isTiming = false;
        WinTxt.SetActive(true);
    }
    public void StartTimer()
    {
        isTiming = true;
        TimerObj.SetActive(true);
        
    }

}
