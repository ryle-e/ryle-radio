
using System;
using System.Collections.Generic;
using UnityEngine;

// a component that references a RadioData
public abstract class RadioComponent : MonoBehaviour
{
    // delegate allows us to universally initialize these components
    public static Action InitAllComponents { get; private set; } = new(() => { });

    // the data this component is linked to
    [SerializeField] protected RadioData data;

    public RadioData Data => data;


    // initialize this component
    public abstract void Init();


    // hook up the component to the delegate
    private void Awake()
    {
        InitAllComponents += Init;
    }

    private void OnDestroy()
    {
        InitAllComponents -= Init;
    }
}