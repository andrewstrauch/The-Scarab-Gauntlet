using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class AIRangedAttackController : BaseAIController
    {
        #region Properties
        public bool ReadyToAttack
        {
            get { return actor.ReadyToAttack; }
        }
        #endregion

        protected override void _registerAIStates()
        {
            FSM.Instance.RegisterState<IdleState>(this, "idle");
            FSM.Instance.RegisterState<AttackState>(this, "attack");

            CurrentState = FSM.Instance.GetState(this, "idle");
        }

        #region AI States
        protected class IdleState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIRangedAttackController rangedAI = AI as AIRangedAttackController;

                if (rangedAI == null)
                    return;
            }

            public override string Execute(IFSMObject obj)
            {
                AIRangedAttackController AI = obj as AIRangedAttackController;

                if (AI == null)
                    return null;

                if (AI.InAttackRange && AI.ReadyToAttack)
                    return "attack";

                return null;
            }
        }

        protected class AttackState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                AIRangedAttackController rangedAI = AI as AIRangedAttackController;

                if (AI == null)
                    return;

                rangedAI.actor.Attack();
            }

            public override string Execute(IFSMObject obj)
            {
                AIRangedAttackController AI = obj as AIRangedAttackController;

                if (AI == null)
                    return null;

                if (!AI.ReadyToAttack)
                    return "idle";

                return null;
            }
        }
        #endregion
    }
}
