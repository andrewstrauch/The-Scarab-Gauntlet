#region Using Directives

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

using Scripting;

#endregion

namespace PlatformerStarter.Enemies
{
    public class LuaAIController : BaseAIController
    {
        #region Private Members

        private string initScript;
        private string updateScript;

        #endregion

        #region Public Properties

        public string InitScript
        {
            get { return initScript; }
            set { initScript = value; }
        }

        public string UpdateScript
        {
            get { return updateScript; }
            set { updateScript = value; }
        }

        #endregion

        public void Init(object registerObj)
        {
            ScriptingEngine.Instance.RegisterObject(this);
            ScriptingEngine.Instance.RegisterObject(registerObj);

            ScriptingEngine.Instance.RunScript("data/scripts/" + initScript);
        }

        #region Lua Exposed Routines
        
        [LuaFuncAttr("RegisterState", "Registers a state from script in the FSM.")]
        public void RegisterState(string stateName)
        {
            FSM.Instance.RegisterState<LuaAIState>(this, stateName);
            CurrentState = FSM.Instance.GetState(this, "attack");

        }

        [LuaFuncAttr("MoveLeft", "Moves the enemy left, across the screen.")]
        public void MoveLeft()
        {
            this._moveLeft();
        }

        [LuaFuncAttr("Attack", "Tells the enemy to attack.")]
        public void Attack()
        {
            actor.Attack();
        }

        [LuaFuncAttr("OnLeft", "Checks whether the player is on the left or not.")]
        public bool OnLeft()
        {
            return this.onLeft;
        }
         
        [LuaFuncAttr("Stop", "Stops all horizontal movement.")]
        public void Stop()
        {
            this._horizontalStop();
        }

        [LuaFuncAttr("MoveRight", "Moves the enemy right, across the screen.")]
        public void MoveRight()
        {
            this._moveRight();
        }

        [LuaFuncAttr("PlayerInRange", "Checks whether the player is in range.")]
        public bool PlayerInRange(float dist)
        {
            return (Math.Abs(_getDistanceToPlayer().X) < dist);
        }

        #endregion


        private class LuaAIState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                LuaAIController luaAI = AI as LuaAIController;

                if (luaAI == null)
                    return;

                ScriptingEngine.Instance.RunScript("data/scripts/" + luaAI.UpdateScript);
            }

            public override string Execute(IFSMObject obj)
            {
                return null;
            }
        }
    }
}
