using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
public class SmallRigo : MonoBehaviour
{
    public AudioSource audioSource = null!;
    public AudioClip[] imarigoSounds = [];
    public Animator animator = null!;
    public NavMeshAgent agent = null!;

    [HideInInspector] public bool jumping = false;
    private static readonly int Jumping = Animator.StringToHash("isJumping"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public void DoPathingToPosition(Vector3 position)
    {
        animator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
        agent.SetDestination(position);
    }

    public void SetJumping(bool _jumping)
    {
        jumping = _jumping;
        animator.SetBool(Jumping, _jumping);
    }
}