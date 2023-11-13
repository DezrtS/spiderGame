using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : Singleton<Timer>
{
    Drone drone;
    private TextMeshProUGUI timer;

    private float startingTime;
    private bool started;
    private bool gameStarted;

    protected override void Awake()
    {
        base.Awake();
        
        timer = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        drone = Drone.Instance;
    }

    public void Activate()
    {
        startingTime = Time.timeSinceLevelLoad;
        gameStarted = true;
        started = true;
    }

    public void Deactivate()
    {
        started = false;
    }

    private void Update()
    {
        if (started)
        {
            float elapsedTime = Time.timeSinceLevelLoad - startingTime;
            timer.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(elapsedTime / 60), Mathf.FloorToInt(elapsedTime % 60));
        }
    }

    private void FixedUpdate()
    {
        if (!gameStarted && drone.isMoving)
        {
            Activate();
        }
    }
}
