using UnityEngine;

[CreateAssetMenu(fileName = "NewMovementState", menuName = "ScriptableObjects/PlayerStats/MovementState")]
public class PlayerMovementStateSO : ScriptableObject
{
    [Space]
    [Header("Moving")]
    [Tooltip("How quickly the player accelerates.")]
    public float moveAcceleration = 40f;
    [Tooltip("How much friction the player experiences when actively moving left/right.")]
    public float moveFriction = 0f;
    [Tooltip("How much friction the player experiences when not actively moving.")]
    public float stopFriction = 3f;
    [Tooltip("How much air drag the player experiences.")]
    public float airDrag = 0.4f;
    [Tooltip("The maximum left/right speed the player can achieve.")]
    public float maxSpeed = 16f;

    [Space]
    [Header("Falling")]
    [Tooltip("How much gravity the player experiences.")]
    public float fallGravity = 3f;
    [Tooltip("The maximum downwards speed the player can achieve.")]
    public float maxFallSpeed = 50f;

    [Space]
    [Header("Crouching")]
    [Tooltip("The friction the player experiences when crouching. Use a low number to slide.")]
    public float crouchFriction = 0.01f;
    [Tooltip("The gravity the player experiences when crouching. Use a high number to fast fall while holding down.")]
    public float crouchGravity = 12f;
}
