//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;


namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// The base AI controller for Actors. Inherits from ActorController. Establishes the protected _update method which 
    /// calls on the individual AI states' public Update method. This class should be used as a parent class for specific 
    /// AI controllers. 
    /// </summary>
    public class ActorAIController : ActorController, ITickObject, IFSMObject
    {
        //======================================================
        #region Constructors

        /// <summary>
        /// Constructor. Adds a tick callback for this ActorAIController and calls _registerAIStates.
        /// </summary>
        public ActorAIController()
        {
            // register for a tick callback
            ProcessList.Instance.AddTickCallback(this, this);

            // register AI states
            _registerAIStates();
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The current AI state of this ActorAIController
        /// </summary>
        public FSMState CurrentState
        {
            get { return _currentState; }
            set { _currentState = value; }
        }

        /// <summary>
        /// The previous AI state of this ActorAIController
        /// </summary>
        public FSMState PreviousState
        {
            get { return _previousState; }
            set { _previousState = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public void ProcessTick(Move move, float elapsed)
        {
            // call update on this AI controller
            _update();
        }

        public void InterpolateTick(float k) { }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Registers the AI states that this ActorAIController will used to manipulate possessed Actors.
        /// Override this method to add desired states to the actor.
        /// </summary>
        protected virtual void _registerAIStates() { }

        /// <summary>
        /// Updates the current AIState and calls the Update method on that state.
        /// </summary>
        protected virtual void _update()
        {
            FSM.Instance.Execute(this);

            if (_currentState as AIState != null)
                (_currentState as AIState).Update(this);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected FSMState _currentState;

        protected FSMState _previousState;

        #endregion

        //======================================================
        #region Actor AI states

        /// <summary>
        /// The base AI state class. Derive all AI states from this class and put any AI code into the Update method of your states.
        /// </summary>
        protected abstract class AIState : FSMState
        {
            /// <summary>
            /// Use the Update method to run state-specific AI code. This is called once for each ProcessTick that this 
            /// AIState is the ActorAIController's CurrentState.
            /// </summary>
            /// <param name="AI">The ActorAIController object on which this is the current state.</param>
            public abstract void Update(ActorAIController AI);
        }

        #endregion
    }
}
