//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// An object responsible for controlling any number of MoveComponents. There is only basic functionality for performing
    /// handshakes with MoveComponents on this object. See ActorController and derived classes for intended use.
    /// </summary>
    public abstract class MoveController : TorqueObject
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// List of currently possessed movers.
        /// </summary>
        public List<MoveComponent> Movers
        {
            get { return _movers; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Attempt to find and possess a MoveComponent on the specified scene object.
        /// </summary>
        /// <param name="mover">The scene object on which to search for and possess a MoveComponent.</param>
        public void PossessMover(T2DSceneObject mover)
        {
            PossessMover(mover.Components.FindComponent<MoveComponent>());
        }

        /// <summary>
        /// Attempt to possess a MoveComponent.
        /// </summary>
        /// <param name="mover">The MoveComponent to possess.</param>
        public void PossessMover(MoveComponent mover)
        {
            if (mover != null && !_movers.Contains(mover))
            {
                _movers.Add(mover);

                if (mover.Controller != this)
                    mover.Possess(this);

                _possessedMover(mover);
            }
        }

        /// <summary>
        /// Unpossess the MoveComponent on the specified scene object. Removes the MoveComponent from our list of controlled movers.
        /// </summary>
        /// <param name="mover">The scene object on which to search for and unpossess a MoveComponent currently possessed by this MoveController.</param>
        public void UnpossessMover(T2DSceneObject mover)
        {
            UnpossessMover(mover.Components.FindComponent<MoveComponent>());
        }

        /// <summary>
        /// Unpossess the specified MoveComponent. Removes the specified MoveComponent from our list of controlled movers.
        /// </summary>
        /// <param name="mover">The MoveComponent to be unpossessed by this MoveController.</param>
        public void UnpossessMover(MoveComponent mover)
        {
            if (mover != null && _movers.Contains(mover))
            {
                _movers.Remove(mover);

                if (mover.Controller == this)
                    mover.Unpossess();

                _unpossessedMover(mover);
            }
        }

        /// <summary>
        /// Check if this MoveController is currently possessing the specified MoveComponent.
        /// </summary>
        /// <param name="mover">The MoveComponent to check for.</param>
        /// <returns>True if the specified MoveComponent is in our list of controlled movers.</returns>
        public bool IsPossessing(MoveComponent mover)
        {
            return _movers.Contains(mover);
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Callback after the successful possession of a MoveComponent. Override this callback to verify proper MoveComponent types, if desired.
        /// Use UnpossessMover in the case of an undesired MoveComponent.
        /// </summary>
        /// <param name="mover">The MoveComponent that has just been possessed.</param>
        protected virtual void _possessedMover(MoveComponent mover) { }

        /// <summary>
        /// Callback after a MoveComponent is successfully unpossessed by this MoveController.
        /// </summary>
        /// <param name="mover">The MoveComponent that was just unposessed.</param>
        protected virtual void _unpossessedMover(MoveComponent mover) { }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private List<MoveComponent> _movers = new List<MoveComponent>();

        #endregion
    }
}
