using System;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;

namespace PlatformerStarter.Common.Triggers.Puzzles.Behaviors
{
    class MovementBehavior : PuzzleBehavior, ITickObject
    {
        #region Private Members

        private float xDistance;
        private float yDistance;
        private float xSpeed;
        private float ySpeed;
        private T2DSceneObject puzzleObject;
        private Vector2 origPosition;

        #endregion

        #region Public Properties

        public float XDistance
        {
            get { return xDistance; }
            set { xDistance = value; }
        }

        public float YDistance
        {
            get { return yDistance; }
            set { yDistance = value; }
        }

        public float XSpeed
        {
            get { return xSpeed; }
            set { xSpeed = value; }
        }

        public float YSpeed
        {
            get { return ySpeed; }
            set { ySpeed = value; }
        }

        #endregion

        public override void Execute(T2DSceneObject puzzleObject)
        {
            this.puzzleObject = puzzleObject;
            origPosition = this.puzzleObject.Position;
            this.puzzleObject.Physics.VelocityX = xSpeed * (xDistance / Math.Abs(xDistance));
            this.puzzleObject.Physics.VelocityY = ySpeed * (yDistance / Math.Abs(yDistance));

            ProcessList.Instance.AddTickCallback(puzzleObject, this);
            active = false;
        }

        public void ProcessTick(Move move, float dt)
        {
            if (puzzleObject.Position - origPosition == new Vector2(xDistance, yDistance))
            {
                puzzleObject.Physics.Velocity = Vector2.Zero;
                ProcessList.Instance.RemoveObject(puzzleObject);
            }
        }

        public void InterpolateTick(float dt)
        {
        }                                                                                                                                                                                                                                                                                                                                            
    }
}
