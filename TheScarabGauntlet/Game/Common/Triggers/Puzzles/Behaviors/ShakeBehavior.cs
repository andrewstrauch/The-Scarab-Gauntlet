#region Using Directives
using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles.Behaviors
{
    class ShakeBehavior : PuzzleBehavior, ITickObject
    {
        #region Private Memmbers

        private float xMag;
        private float yMag;
        private float degrees;
        private T2DSceneObject puzzleObject;
        
        #endregion

        #region Public Properties

        /// <summary>
        /// The magnitude at which to shake the object in the x axis.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public float XMagnitude
        {
            get { return xMag; }
            set { xMag = value; }
        }

        /// <summary>
        /// The magnitude at which to shake the object in the y axis.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public float YMagnitude
        {
            get { return yMag; }
            set { yMag = value; }
        }

        #endregion
        public void InterpolateTick(float dt)
        { }

        public void ProcessTick(Move move, float dt)
        {
            degrees += MathHelper.ToDegrees(dt * 100);

            puzzleObject.Position += new Vector2(degrees * xMag, degrees * yMag); 
        }

        public override void Execute(GarageGames.Torque.T2D.T2DSceneObject puzzleObject)
        {
            this.puzzleObject = puzzleObject;
            ProcessList.Instance.AddTickCallback(puzzleObject, this);
            active = false;
            degrees = 0;
        }

    }
}
