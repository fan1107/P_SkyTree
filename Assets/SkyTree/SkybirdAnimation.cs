using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    /// <summary>Three-pose wing cycle at rest; the original spread wings during a strike.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage), typeof(Animator))]
    public sealed class SkybirdAnimation : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float playbackSpeed = 1f;
        private static readonly int Attacking = Animator.StringToHash("IsAttacking");
        private static readonly int Idle = Animator.StringToHash("Idle_Flap");
        private Animator animator;

        public bool IsAttacking { get; private set; }
        public bool IsStopped { get; private set; }

        private void Awake() { animator = GetComponent<Animator>(); }
        private void OnEnable() { PlayIdle(); }

        public void PlayIdle(float normalizedPhase = 0)
        {
            if (!animator) animator = GetComponent<Animator>();
            IsAttacking = false;
            IsStopped = false;
            if (!animator || !animator.runtimeAnimatorController) return;
            animator.speed = playbackSpeed;
            animator.SetBool(Attacking, false);
            animator.Play(Idle, 0, Mathf.Repeat(normalizedPhase, 1));
        }

        public void SetAttacking(bool attacking)
        {
            if (IsStopped || IsAttacking == attacking) return;
            IsAttacking = attacking;
            if (animator && animator.runtimeAnimatorController)
                animator.SetBool(Attacking, attacking);
        }

        public void StopAnimation()
        {
            IsStopped = true;
            if (animator) animator.speed = 0;
        }
    }
}
