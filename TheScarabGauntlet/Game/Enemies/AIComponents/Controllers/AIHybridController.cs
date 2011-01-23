using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    [TorqueXmlSchemaType]
    public class AIHybridController : BaseAIController
    {
        protected float maxRangedDist;
        protected float minRangedDist;

        #region Properties
        public float MaxRangedDist
        {
            get { return maxRangedDist; }
            set { maxRangedDist = value; }
        }
        public float MinRangedDist
        {
            get { return minRangedDist; }
            set { minRangedDist = value; }
        }
        public bool InProjectileRange
        {
            get 
            {
                float distance = Math.Abs(_getDistanceToPlayer().X);
                return (distance < maxRangedDist) && (distance > minRangedDist); 
            }
        }
        public bool ReadyToAttack
        {
            get { return actor.ReadyToAttack; }
        }
        #endregion

        protected override void _registerAIStates()
        {
            FSM.Instance.RegisterState<IdleState>(this, "idle");
            FSM.Instance.RegisterState<MoveState>(this, "move");
            FSM.Instance.RegisterState<AttackState>(this, "attack");

            CurrentState = FSM.Instance.GetState(this, "idle");
        }

        #region AI States
        protected class IdleState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIHybridController hybridAI = AI as AIHybridController;

                if (hybridAI == null)
                    return;

                hybridAI._horizontalStop();
            }

            public override string Execute(IFSMObject obj)
            {
                AIHybridController AI = obj as AIHybridController;

                if (AI == null)
                    return null;

                if (AI.InAttackRange)
                {
                    if (!AI.ReadyToAttack)
                        return "idle";
                    else
                        return "attack";
                }

                if (AI.InAlertRange)
                {
                    if (!AI.InProjectileRange)
                        return "move";
                    else
                        return "attack";
                }

                return null;
            }
        }

        protected class MoveState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIHybridController hybridAI = AI as AIHybridController;

                if (AI == null)
                    return;

                if (hybridAI.OnLeft)
                    hybridAI._moveLeft();
                else
                    hybridAI._moveRight();
            }

            public override string Execute(IFSMObject obj)
            {
                AIHybridController AI = obj as AIHybridController;

                if (AI == null)
                    return null;

                if (!AI.InAlertRange)
                    return "idle";

                if (AI.Attacks && AI.ReadyToAttack)
                     if (AI.InAttackRange || AI.InProjectileRange)
                        return "attack";

                if (!AI.ReadyToAttack && !AI.InAlertRange)
                    return "idle";

                return null;
            }
        }

        protected class AttackState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIHybridController hybridAI = AI as AIHybridController;

                if (AI == null)
                    return;
                
                hybridAI._horizontalStop();
                hybridAI.actor.Attack();
            }

            public override string Execute(IFSMObject obj)
            {
                AIHybridController AI = obj as AIHybridController;

                if (AI == null)
                    return null;

                if (!AI.ReadyToAttack)
                {
                    if (!AI.InAlertRange || AI.InAttackRange)
                        return "idle";
                    else
                        return "move";
                }

                return null;
            }
        }
        #endregion
    }
}
