#region Using Directives
using GarageGames.Torque.T2D;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles
{
    public abstract class PuzzleBehavior
    {
        #region Private Members

        protected bool active;

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

        public abstract void Execute(T2DSceneObject puzzleObject);

        public void Run(T2DSceneObject puzzleObject)
        {
            if (!active)
                Execute(puzzleObject);
        }
        #endregion

    }
}
