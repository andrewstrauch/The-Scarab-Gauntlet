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

        public void Init()
        {

            //ScriptingEngine.Instance.RegisterObject(this);
            ScriptingEngine.Instance.RegisterFunction("RegisterState", this);
            ScriptingEngine.Instance.RegisterFunction("MoveLeft", this);
            ScriptingEngine.Instance.RegisterFunction("MoveRight", this);
            ScriptingEngine.Instance.RegisterFunction("PlayerInRange", this);
            ScriptingEngine.Instance.RegisterFunction("Stop", this);
            ScriptingEngine.Instance.RegisterFunction("OnLeft", this);
            //ScriptingEngine.Instance.RunScript("data/scripts/" + initScript + ".lua");
        }

        protected override void _registerAIStates()
        {

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

        [LuaFuncAttr("MoveRight", "Moves the enemy right, across the screen.")]
        public void MoveRight()
        {
            this._moveRight();
        }

        [LuaFuncAttr("Attack", "Tells the enemy to attack.")]
        public void Attack()
        {
            actor.Attack();
        }

        public bool OnLeft()
        {
            return this.onLeft;
        }

        [LuaFuncAttr("PlayerInRange", "Checks whether the player is in range.")]
        public bool PlayerInRange(float dist)
        {
            return (Math.Abs(_getDistanceToPlayer().X) < dist);
        }

        public void Stop()
        {
            this._horizontalStop();
        }

        #endregion

        class LuaAIState : AIState
        {
            public override void Update(ActorAIController AI)
            {
                LuaAIController luaAI = AI as LuaAIController;

                if (luaAI == null)
                    return;

                ScriptingEngine.Instance.RunScript("data/scripts/" + luaAI.UpdateScript + ".lua");
            }

            public override string Execute(IFSMObject obj)
            {
                return null;
            }
        }
    }
}
