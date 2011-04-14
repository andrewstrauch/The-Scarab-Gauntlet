using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    /// <summary>
    /// This controller defines the AI used by actors that simply chase the player.  They will 
    /// basically chase the player after that player has come within a certain distance and will turn
    /// when they "sense" the player is on the other side.
    /// </summary>
    public class AIChaseController : BaseAIController
    {
        protected bool bounces;

        #region Properties
        public bool Bounces
        {
            get { return bounces; }
            set { bounces = value; }
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
                AIChaseController chaseAI = AI as AIChaseController;

                if (chaseAI == null)
                    return;

                chaseAI._horizontalStop();
            }

            public override string Execute(IFSMObject obj)
            {
                AIChaseController chaseAI = obj as AIChaseController;

                if (chaseAI == null)
                    return null;

                if (chaseAI.InAlertRange && !chaseAI.InAttackRange)// && chaseAI.onSameLevel)
                    return "move";

                if (chaseAI.Attacks)
                    if (chaseAI.InAttackRange)
                        if (!chaseAI.ReadyToAttack)
                            return null; 
                        else
                            return "attack";

                return null;
            }
        }

        protected class MoveState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIChaseController chaseAI = AI as AIChaseController;

                if (chaseAI == null)
                    return;

                if (chaseAI.OnLeft)
                    chaseAI._moveLeft();
                else
                    chaseAI._moveRight();

                if (chaseAI.Bounces)
                    chaseAI._jump();
            }

            public override string Execute(IFSMObject obj)
            {
                AIChaseController chaseAI = obj as AIChaseController;

                if (chaseAI == null)
                    return null;

                if (/*!chaseAI.onSameLevel ||*/ !chaseAI.InAlertRange)
                    return "idle";

                if (chaseAI.Attacks)
                    if (chaseAI.InAttackRange)
                        if (!chaseAI.ReadyToAttack)
                            return "idle";
                        else
                            return "attack";

                return null;
            }
        }

        protected class AttackState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIChaseController chaseAI = AI as AIChaseController;

                if (chaseAI == null)
                    return;

                chaseAI.actor.Attack();
            }

            public override string Execute(IFSMObject obj)
            {
                AIChaseController chaseAI = obj as AIChaseController;

                if (chaseAI == null)
                    return null;

               // if (!chaseAI.onSameLevel)
               //     return "idle";

                if (!chaseAI.ReadyToAttack)
                {
                    if (!chaseAI.InAttackRange)
                        return "move";
                    else
                        return "idle";
                }

                return null;
            }
        }
        #endregion
    }
}
