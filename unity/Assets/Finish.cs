using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
    public TimerScript timerScript; // Reference to the TimerScript component

    private void OnTriggerEnter(Collider other)
    {
        // Ensure the collision is with the player
        if (other.CompareTag("Player"))
        {
            timerScript.StopTimer(); // Stop the timer
        }
    }
}
