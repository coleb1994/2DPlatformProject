using UnityEngine;

[CreateAssetMenu(fileName = "NewJumpState", menuName = "ScriptableObjects/PlayerStats/JumpState")]
public class PlayerJumpStateSO : ScriptableObject
{
    [Tooltip("The amount of upwards force the player experiences when jumping.")]
    public float jumpForce = 600f;
    [Tooltip("The amount of gravity the player experiences when jumping.")]
    public float jumpGravity = 1.5f;
    [Tooltip("A number to quantify how long the player will 'hang' at the apex of their jump (not specifically seconds).")]
    public float airHangThreshold = 0.35f;
    [Tooltip("The amount of gravity the player experiences when at the apex of their jump. A low number here will allow the player to 'hang' in the air.")]
    public float airHangGravity = 1.7f;
}
