using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    public void DeathAnimation()
    {
        animator.SetTrigger("death");
    }
}
