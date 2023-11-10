using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayLevelUnder : MonoBehaviour
{
    [SerializeField] private Transform stayUnder;

    private void Update()
    {
        transform.position = new Vector3(stayUnder.position.x, transform.position.y, stayUnder.position.z);
        transform.eulerAngles = new Vector3(0, stayUnder.eulerAngles.y, 0);
    }
}