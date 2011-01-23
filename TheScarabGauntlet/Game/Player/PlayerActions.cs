using System;
using System.Collections.Generic;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Player
{
    public delegate void OnTimerEndDelegate();

    public class PlayerAction
    {
        public Timer Timer;
        public float CoolDown;
        public bool ReadyToAct;
        public OnTimerEndDelegate OnTimerEnd;

        public PlayerAction(string actionName, float coolDown)
        {
            CoolDown = coolDown;
            ReadyToAct = true;
            Timer = new Timer(actionName);
            Timer.MillisecondsUntilExpire = CoolDown;
        }
    };

    public class PlayerActions
    {
        #region Private Members
        private Dictionary<string, PlayerAction> actions;
        #endregion

        #region Public Properties
        #endregion

        #region Public Routines
        /// <summary>
        /// Constructor.
        /// </summary>
        public PlayerActions()
        {
            actions = new Dictionary<string, PlayerAction>();
        }

        /// <summary>
        /// Adds the action name and corresponding cool-down value to the data structure.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="coolDown">The time it takes the action to "cool down".</param>
        public void AddAction(string actionName, float coolDown)
        {
            if (!actions.ContainsKey(actionName))
                actions.Add(actionName, new PlayerAction(actionName, coolDown));
        }

        /// <summary>
        /// Adds the action names, cool-down, and applicable delegate to the data structure.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="coolDown">The time it takes the action to "cool-down".</param>
        /// <param name="function">The function we wish to call when the timer expires.</param>
        public void AddAction(string actionName, float coolDown, OnTimerEndDelegate function)
        {
            AddAction(actionName, coolDown);

            actions[actionName].OnTimerEnd = function;
        }

        /// <summary>
        /// Returns the action of the given name.
        /// </summary>
        /// <param name="name">The name of the action wanted.</param>
        /// <returns>The action corresponding to the given name.</returns>
        public PlayerAction GetAction(string name)
        {
            return actions[name];
        }

        public void CheckActionTimers()
        {
            foreach (KeyValuePair<string, PlayerAction> action in actions)
            {
                if (action.Key == "invincibility")
                    Game.Instance.Window.Title = action.Value.Timer.Delta.ToString();
                if (action.Value.Timer.Expired)
                {
                    action.Value.ReadyToAct = true;
                    action.Value.Timer.Reset();
                    
                    if(action.Value.OnTimerEnd != null)
                        action.Value.OnTimerEnd();
                }
            }
        }
        #endregion
    }
}
