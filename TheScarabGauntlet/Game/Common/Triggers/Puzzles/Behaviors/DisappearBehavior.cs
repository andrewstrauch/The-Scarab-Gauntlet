#region Using Directives
using System;

using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles
{
    class DisappearBehavior : PuzzleBehavior, ITickObject
    {
        #region Private Members
        private T2DSceneObject obj;
        #endregion

        #region Public Routines

        /// <summary>
        /// Turns the object invisible (if it isn't already) and disables all collision.
        /// </summary>
        /// <param name="puzzleObject">The object to make disappear.</param>
        public override void Execute(T2DSceneObject puzzleObject)
        {
            obj = puzzleObject;
            puzzleObject.CollisionsEnabled = false;
            //puzzleObject.Visible = false;
            active = false;
            ProcessList.Instance.AddTickCallback(puzzleObject, this);
        }

        public void ProcessTick(Move move, float dt)
        {
            obj.Position += new Vector2((float)Math.Sin(dt * 100), 0);
        }

        public void InterpolateTick(float dt)
        {
        }
        #endregion
    }
}
