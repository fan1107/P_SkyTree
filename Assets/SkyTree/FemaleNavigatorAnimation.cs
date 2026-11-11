using UnityEngine;
using UnityEngine.Events;

namespace SkyTree
{
    [RequireComponent(typeof(Animator))]
    public sealed class FemaleNavigatorAnimation : MonoBehaviour
    {
        public UnityEvent onCannonFire = new UnityEvent();
        private Animator animator;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        public int ShotsFired { get; private set; }
        private Animator Rig => animator ? animator : (animator = GetComponent<Animator>());

        // 0 = breathe, 1 = walk, 2 = run. Does not translate the character.
        public void SetMovement(float speed) => Rig.SetFloat(Speed, Mathf.Clamp(speed, 0, 2));
        public void Attack()
        {
            if (Rig.GetCurrentAnimatorStateInfo(0).IsName("Attack") || Rig.IsInTransition(0)) return;
            Rig.ResetTrigger(AttackTrigger);
            Rig.SetTrigger(AttackTrigger);
        }

        // Animation event at the recoil frame. Connect the game's cannon firing here.
        public void OnCannonFire()
        {
            ShotsFired++;
            onCannonFire.Invoke();
        }
    }
}
