
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RadioComponent : MonoBehaviour
{
    public static Action InitAllComponents { get; private set; } = new(() => { });


    [SerializeField] protected RadioData data;

    public RadioData Data => data;


    protected abstract void Init();


    private void Awake()
    {
        InitAllComponents += Init;
    }

    private void OnDestroy()
    {
        InitAllComponents -= Init;
    }
}