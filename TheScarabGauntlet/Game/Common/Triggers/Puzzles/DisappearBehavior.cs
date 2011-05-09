#region Using Directives
using System;

using GarageGames.Torque.T2D;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles
{
    class DisappearBehavior : IPuzzleBehavior
    {
        #region Private Members
        private bool active;
        #endregion

        #region Public Properties

        /// <summary>
        /// Flag indicating whether or not this behavior should be active.
        /// </summary>
        public bool Active
        {
            get { return active; }
            set { active = value; }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Turns the object invisible (if it isn't already) and disables all collision.
        /// </summary>
        /// <param name="puzzleObject">The object to make disappear.</param>
        public void Execute(T2DSceneObject puzzleObject)
        {
            puzzleObject.CollisionsEnabled = false;
            puzzleObject.Visible = false;
            active = false;
        }

        #endregion
    }
}
