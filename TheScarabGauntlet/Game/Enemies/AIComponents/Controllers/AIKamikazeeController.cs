using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    public class AIKamikazeeController : BaseAIController
    {
        #region Properties
        public bool InXAttackRange
        {
            get { return InAttackRange; }
        }
        /*public bool InYAttackRange
        {
            get { return (Math.Abs(_getDistanceToPlayer().Y) >= attackDist); }
        }*/
        #endregion

        protected override void _registerAIStates()
        {
            FSM.Instance.RegisterState<IdleState>(this, "idle");
            FSM.Instance.RegisterState<MoveState>(this, "move");
            //FSM.Instance.RegisterState<MoveRightState>(this, "moveRight");
            FSM.Instance.RegisterState<AttackState>(this, "attack");


            CurrentState = FSM.Instance.GetState(this, "idle");
        }

        #region AI States
        protected class IdleState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIKamikazeeController killAI = AI as AIKamikazeeController;

                if (killAI == null)
                    return;

                killAI._horizontalStop();
            }

            public override string Execute(IFSMObject obj)
            {
                AIKamikazeeController killAI = obj as AIKamikazeeController;

                if (killAI == null)
                    return null;

                if (killAI.InAlertRange && !killAI.InXAttackRange)
                    return "move";

                if (killAI.Attacks)
                    if (killAI.InXAttackRange)// && killAI.onSameLevel)//killAI.InYAttackRange)
                        return "attack";

                return null;
            }
        }

        protected class MoveState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIKamikazeeController killAI = AI as AIKamikazeeController;

                if (killAI == null)
                    return;
                if (killAI.OnLeft)
                    killAI._moveLeft();
                else
                    killAI._moveRight();
            }

            public override string Execute(IFSMObject obj)
            {
                AIKamikazeeController killAI = obj as AIKamikazeeController;

                if (killAI == null)
                    return null;


                if (!killAI.InAlertRange)
                    return "idle";
                if (killAI.Attacks)
                    if (killAI.InXAttackRange)// && killAI.onSameLevel)
                        return "attack";

                return null;
            }
        }

        protected class AttackState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIKamikazeeController killAI = AI as AIKamikazeeController;

                if (killAI == null)
                    return;

                killAI._horizontalStop();
                killAI.actor.Attack();
            }

            public override string Execute(IFSMObject obj)
            {
                return null;
            }
        }
        #endregion
    }
}
