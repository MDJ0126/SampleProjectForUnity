using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Game
{
    /// <summary>
    /// 그래프의 개별 전투 단계를 실행하며 Target 블랙보드를 공유한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Combat Step", story: "[Step] enemy [Target]", category: "Action/Combat", id: "4f0b3ca8db0d4fbe824737ac56aa9261")]
    public partial class CombatAction : Action
    {
        [BlackboardEnum]
        public enum eStep { Find, Approach, Attack }
        [SerializeReference] public BlackboardVariable<eStep> Step = new BlackboardVariable<eStep>();
        [SerializeReference] public BlackboardVariable<GameObject> Target = new BlackboardVariable<GameObject>();
        private AIController _controller;

        protected override void OnSetup()
        {
            _controller = GameObject.GetComponent<AIController>();
        }

        protected override Status OnStart()
        {
            return Execute();
        }

        protected override Status OnUpdate()
        {
            return Execute();
        }

        protected override void OnEnd()
        {
            if (Step.Value != eStep.Find)
            {
                _controller?.StopMove();
            }
        }

        /// <summary>
        /// 선택·접근·공격의 순서와 재시도는 그래프가 결정한다.
        /// </summary>
        private Status Execute()
        {
            if (_controller == null)
            {
                return Status.Failure;
            }
            if (Step.Value == eStep.Find)
            {
                Pawn target = _controller.FindTarget();
                Target.Value = target != null ? target.gameObject : null;
                return target != null ? Status.Success : Status.Failure;
            }
            Pawn pawn = Target.Value != null ? Target.Value.GetComponent<Pawn>() : null;
            return Step.Value == eStep.Approach ? _controller.Approach(pawn) : _controller.Attack(pawn);
        }
    }
}
