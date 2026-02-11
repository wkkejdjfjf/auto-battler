using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    public bool isEnemy;
    public bool isAlive;

    public UnityEvent death;

    private bool deathInvoked;

    private void Start()
    {
        isAlive = true;
    }

    public void OnDestroy()
    {
        if (deathInvoked == false)
        {
            death.Invoke();
        }
        deathInvoked = false;
    }

    public void InvokeDeath()
    {
        if (deathInvoked == false)
        {
            death.Invoke();
        }
        deathInvoked = true;
    }
}
