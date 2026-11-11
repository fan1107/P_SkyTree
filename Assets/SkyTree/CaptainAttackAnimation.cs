using UnityEngine;
using UnityEngine.Events;

namespace SkyTree
{
    [RequireComponent(typeof(Animator))]
    public sealed class CaptainAttackAnimation : MonoBehaviour
    {
        public UnityEvent onCannonFire = new UnityEvent();
        public int ShotsFired { get; private set; }
        public void Attack()
        {
            var animator=GetComponent<Animator>();
            if(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") || animator.IsInTransition(0)) return;
            animator.ResetTrigger("Attack"); animator.SetTrigger("Attack");
        }
        public void OnCannonFire() { ShotsFired++; onCannonFire.Invoke(); }
    }
}
