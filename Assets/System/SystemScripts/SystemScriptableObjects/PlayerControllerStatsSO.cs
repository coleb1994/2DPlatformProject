using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerControllerStats", menuName = "ScriptableObjects/PlayerStats/ControllerStats")]
public class PlayerControllerStatsSO : ScriptableObject
{
    [Space]
    [Header("Movement")]
    [Tooltip("The player's movement physics when on the ground.")]
    public PlayerMovementStateSO groundMovementValues;
    [Tooltip("The player's movement physics when in the air.")]
    public PlayerMovementStateSO airMovementValues;
    [Tooltip("The player's movement physics when sliding on a wall.")]
    public PlayerMovementStateSO wallMovementValues;
    [Tooltip("Whether or not the player can 'swim', i.e. jump while in water.")]
    public bool canSwim = true;

    [Space]
    [Header("Jumping")]
    [Tooltip("The default stats for performing a jump.")]
    public PlayerJumpStateSO jumpValues;
    [Tooltip("How many seconds the player can be in the air after running off a ledge, and still jump.")]
    public float coyoteTime = 0.5f;
    [Tooltip("How many seconds the player can press the jump button before they touch the ground, and have it still count as a jump.")]
    public float jumpQueueTime = 0.1f;
    [Tooltip("The maximum upwards speed a player can achieve.")]
    public float maxJumpSpeed = 100f;

    [Space]
    [Header("Double Jumping")]
    [Tooltip("The number of jumps the player can perform while already in the air.")]
    public int doubleJumps = 1;
    [Tooltip("The physics of each subsequent double jump the player does. If the player can perform more double jumps than this list has, the player will continuously use the last value in the list.")]
    public PlayerJumpStateSO[] doubleJumpValues = new PlayerJumpStateSO[1];

    [Space]
    [Header("Sequence Jumping")]
    [Tooltip("The number of different jumps the player can perform in succession. Each jump can have different jump forces associated with it.")]
    public int sequenceJumps = 3;
    [Tooltip("The amount of time between the player landing and jumping again that will still count as a sequence jump.")]
    public float sequenceJumpTime = 0.15f;
    [Tooltip("The physics of each sequence jump the player does. If the player can perform more sequence jumps than this list has, the player will continuously use the last value in the list.")]
    public PlayerJumpStateSO[] sequenceJumpValues = new PlayerJumpStateSO[3];

    [Space]
    [Header("Wall Jumping")]
    [Tooltip("Whether or not the player can jump while sliding down walls.")]
    public bool allowWallJump = true;
    [Tooltip("Whether or not wall jumping shoudl replenish the player's wall jumps.")]
    public bool resetDoubleJumpsOnWall = true;
    [Tooltip("The stats for performing a jump while sliding on a wall.")]
    public PlayerWallJumpStateSO wallJumpValues;

    [Space]
    [Header("Crouching")]
    [Tooltip("Whether or not the player can crouch.")]
    public bool allowCrouch = true;
    [Tooltip("How high the player's collider is when crouching. This is used to slide under objects.")]
    public float crouchColliderHeight = 0.25f;
}
