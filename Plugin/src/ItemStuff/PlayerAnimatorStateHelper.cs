using UnityEngine;

namespace CodeRebirth.ItemStuff
{
    public class PlayerAnimatorStateHelper
    {
        private Animator animator;
        private AnimatorStateInfo currentStateInfo;
        private float currentAnimationTime;
        private bool isCrouching;
        private bool isJumping;
        private bool isWalking;
        private bool isSprinting;
        private RuntimeAnimatorController originalAnimatorController;

        public PlayerAnimatorStateHelper(Animator animator)
        {
            this.animator = animator;
            this.originalAnimatorController = animator.runtimeAnimatorController;
        }


        //We need to Save the important states due to how unity handles switching animator overrides (So stupid)
        public void SaveAnimatorStates()
        {
            if (animator != null)
            {
                isCrouching = animator.GetBool("crouching");
                isJumping = animator.GetBool("Jumping");
                isWalking = animator.GetBool("Walking");
                isSprinting = animator.GetBool("Sprinting");
                currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                currentAnimationTime = currentStateInfo.normalizedTime;

                //Debug.Log("Saved Animator States - Crouching: " + isCrouching +
                //          ", Jumping: " + isJumping +
                //          ", Walking: " + isWalking +
                //          ", Sprinting: " + isSprinting +
                //          ", State: " + currentStateInfo.fullPathHash +
                //          ", Time: " + currentAnimationTime);
            }
        }

        //We need to Restore the important states due to how unity handles switching animator overrides
        public void RestoreAnimatorStates()
        {
            if (animator != null)
            {
                animator.Play(currentStateInfo.fullPathHash, 0, currentAnimationTime);
                animator.SetBool("crouching", isCrouching);
                animator.SetBool("Jumping", isJumping);
                animator.SetBool("Walking", isWalking);
                animator.SetBool("Sprinting", isSprinting);

                //Debug.Log("Restored Animator States - Crouching: " + isCrouching +
                //          ", Jumping: " + isJumping +
                //          ", Walking: " + isWalking +
                //          ", Sprinting: " + isSprinting +
                //          ", State: " + currentStateInfo.fullPathHash +
                //          ", Time: " + currentAnimationTime);
            }
        }

        public void RestoreOriginalAnimatorController()
        {
            if (animator != null)
            {
                animator.runtimeAnimatorController = originalAnimatorController;
                RestoreAnimatorStates();
            }
        }

        public void SetAnimatorOverrideController(AnimatorOverrideController overrideController)
        {
            if (animator != null)
            {
                animator.runtimeAnimatorController = overrideController;
                RestoreAnimatorStates();
            }
        }

        public RuntimeAnimatorController GetOriginalAnimatorController()
        {
            return originalAnimatorController;
        }
    }
}
