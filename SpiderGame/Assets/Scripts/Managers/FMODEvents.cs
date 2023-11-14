using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FMODEvents : Singleton<FMODEvents>
{
    [field: Header("BG_Music")]
    [field: SerializeField] public EventReference BackgrpundMusic {  get; private set; }
}