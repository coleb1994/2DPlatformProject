using UnityEngine;

[CreateAssetMenu(fileName = "NewWallJumpState", menuName = "ScriptableObjects/PlayerStats/WallJumpState")]
public class PlayerWallJumpStateSO : PlayerJumpStateSO
{
    [Tooltip("The amount of outwards force the player experiences when jumping while sliding on a wall. Use a high number to encourage left/right wall jumping, or a low number to encourage wall climbing.")]
    public float horizontalJumpForce = 500f;
}
