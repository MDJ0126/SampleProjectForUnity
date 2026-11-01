using UnityEngine;

public static class AnimHash
{
    public static int IsDead = Animator.StringToHash("IsDead");
    public static int PosX = Animator.StringToHash("PosX");
    public static int PosY = Animator.StringToHash("PosY");
    public static int Speed = Animator.StringToHash("Speed");
    public static int IsJumping = Animator.StringToHash("IsJumping");
    public static int IsFalling = Animator.StringToHash("IsFalling");
    public static int IsGrounded = Animator.StringToHash("IsGrounded");
    public static int Attack = Animator.StringToHash("Attack");
    public static int IsSprint = Animator.StringToHash("IsSprint");
    public static int Hit = Animator.StringToHash("Hit");
    public static int IsMelee = Animator.StringToHash("IsMelee");
}
