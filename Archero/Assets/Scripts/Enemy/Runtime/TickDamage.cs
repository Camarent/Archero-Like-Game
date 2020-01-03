using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct TickDamage : IComponentData
{
    public float Time;
    public float CurrentTime;
    public float Value;
}