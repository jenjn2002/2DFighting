using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Data/Stats")]
public class Stats : ScriptableObject
{
    [Header("LAYERS")]
    public LayerMask playerLayer;
    [Header("INPUT")]
    public bool snapInput = true;

    [Range(0.01f, 0.99f)]
    public float verticalDeadzone = 0.3f;
    [Range(0.01f, 0.99f)]
    public float horizontalDeadzone = 0.1f;
    [Header("MOVEMENT")]
    public float speed = 14f;
    public float accelration = 120f;

    //The pace at which the player comes to a stop
    public float groundDeceleration = 60f;
    //Deceleration in air only after stopping input mid-air
    public float airDeceleration = 30f;

    //"A constant downward force applied while grounded. Helps on slopes"
    [Range(0f,-10f)]
    public float groundingForce = -1.5f;

    //The detection distance for grounding and roof detection
    [Range(0f, 0.5f)]
    public float grounderDistance = 0.05f;

    [Header("JUMP")]
    public float jumpPower = 36f;
    public float maxFallSpeed = 40f;
    public float fallDeceleration = 100f;
    public float jumpEndEarlyGravityModifier = 3f;

    //The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge
    public float coyoteTime = .15f;
    //TThe amount of time we buffer a jump. This allows jump input before actually hitting the ground
    public float jumpBuffer = .2f;

    [Header("DASH")]
    public float dashPower = 5f;
}
