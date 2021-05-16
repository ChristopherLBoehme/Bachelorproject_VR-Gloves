using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopWatch : MonoBehaviour
{
    private bool running = false;
    private bool paused = false;
    private int lapID = 0;

    private float elapsedRunningTime = 0f;
    private float runningStartTime = 0f;
    private float pauseStartTime = 0f;
    private float elapsedPausedTime = 0f;
    private float totalElapsedPausedTime = 0f;
    private float lapStartTime = 0f;
    private float elapsedLapTime = 0f;
    private float currentLapPauseStartTime = 0f;
    private float currentLapElapsedPauseTime = 0f;
    private float currentLapTotalPauseTime = 0f;
    
    public List<float> lapValues = new List<float>();


    //Continuous Time
    private void Update()
    {
        if (running)
        {
            elapsedRunningTime = Time.time - runningStartTime - totalElapsedPausedTime;
            elapsedLapTime = Time.time - lapStartTime - currentLapTotalPauseTime;
        }
        else if (paused)
        {
            elapsedPausedTime = Time.time - pauseStartTime;
            currentLapElapsedPauseTime = Time.time - currentLapPauseStartTime;
        }
    }

    public void Begin()
    {
        if (!running && !paused)
        {
            runningStartTime = Time.time;
            lapStartTime = Time.time;
            running = true;
        }
    }

    public void Pause()
    {
        if (running && !paused)
        {
            running = false;
            pauseStartTime = Time.time;
            currentLapPauseStartTime = Time.time;
            paused = true;
        }
    }

    public void Unpause()
    {
        if (!running && paused)
        {
            totalElapsedPausedTime += elapsedPausedTime;
            currentLapTotalPauseTime += currentLapElapsedPauseTime;
            running = true;
            paused = false;
        }
    }

    public void NextLap()
    {
        lapValues.Add(elapsedLapTime);
        lapID += 1;
        lapStartTime = Time.time;
        elapsedLapTime = 0f;

        currentLapElapsedPauseTime = 0f;
        currentLapTotalPauseTime = 0f;
        
        if (running && !paused) currentLapPauseStartTime = 0f;
        else if (!running && paused) currentLapPauseStartTime = Time.time;

    }

    public void Reset()
    {
        running = false;
        paused = false;
        lapID = 0;

        elapsedRunningTime = 0f;
        runningStartTime = 0f;
        pauseStartTime = 0f;
        elapsedPausedTime = 0f;
        totalElapsedPausedTime = 0f;
        lapStartTime = 0f;
        elapsedLapTime = 0f;
        currentLapPauseStartTime = 0f;
        currentLapElapsedPauseTime = 0f;
        currentLapTotalPauseTime = 0f;
       
        lapValues = new List<float>();
    }

/* Additional functionality if needed
    public int GetMinutes()
    {
        return (int) (elapsedRunningTime / 60f);
    }

    public int GetSeconds()
    {
        return (int) (elapsedRunningTime);
    }

    public float GetMilliSeconds()
    {
        return (float) (elapsedRunningTime - System.Math.Truncate(elapsedRunningTime));
    }
    
    */

}
