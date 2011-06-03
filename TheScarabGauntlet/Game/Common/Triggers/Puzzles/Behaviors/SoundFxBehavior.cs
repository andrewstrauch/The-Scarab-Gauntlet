#region Using Directives
using System;

using GarageGames.Torque.T2D;
using GarageGames.Torque.GameUtil;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles
{
    class SoundFxBehavior : PuzzleBehavior
    {
        #region Private Members
        private string sfxName;
        #endregion

        #region Public Properties

        /// <summary>
        /// The name of the sound effect to play.
        /// </summary>
        public string SFXName
        {
            get { return sfxName; }
            set { sfxName = value; }
        }

        #endregion

        #region Public Routines

        public override void Execute(T2DSceneObject puzzleObject)
        {
            if (sfxName != "")
                SoundManager.Instance.PlaySound("sounds", sfxName);
                
            active = false;
        }

        #endregion
    }
}
