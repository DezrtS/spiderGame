using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : Singleton<Drone>
{
    [SerializeField] private float timeToReachEndWaypoint = 120;
    [SerializeField] private Vector3 endWaypoint;

    private Vector3 diff;

    private float distanceToTravel;

    public bool isMoving;

    public bool hasArrived;

    private void Start()
    {
        diff = endWaypoint - transform.position;
        distanceToTravel = diff.magnitude;
        diff = diff.normalized;

        Activate();
    }

    public void Activate()
    {
        isMoving = true;
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            diff = endWaypoint - transform.position;

            transform.forward = new Vector3(diff.x, 0, diff.y).normalized;

            transform.position += diff.normalized * GetSpeed() * Time.deltaTime;

            if (diff.magnitude <= GetSpeed() * Time.deltaTime)
            {
                transform.position = endWaypoint;
                isMoving = false;
                hasArrived = true;
                Debug.Log("Reached End Waypoint");
            }
        }   
    }

    public float GetSpeed()
    {
        return distanceToTravel / timeToReachEndWaypoint;
    }
}