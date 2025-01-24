using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
    public TimerScript tim; 

    private void OnTriggerEnter(Collider player)
    {
        // check collision tag to be "Player"
        if (player.CompareTag("Player"))
        {
            tim.StopTimer(); 
        }
    }
}
