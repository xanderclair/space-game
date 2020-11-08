using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    public PlayerMovement movement;
    private void OnAnimatorMove()
    {
        movement.animatorMoved();
    }
}
