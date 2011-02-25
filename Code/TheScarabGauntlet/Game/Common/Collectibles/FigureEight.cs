using System;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class FigureEight : IMovement
    {
        #region Private Members
        
        private Vector2 startPosition;
        private Vector2 position;
        private float xMagnitude;
        private float yMagnitude;
        private float theta;
        
        #endregion

        #region Public Properties
        
        public Vector2 StartingPosition
        {
            set { startPosition = value; }
        }
        public Vector2 Position
        {
            get { return position; }
        }

        public float XMagnitude
        {
            get { return xMagnitude; }
            set { xMagnitude = value; }
        }

        public float YMagnitude
        {
            get { return yMagnitude; }
            set { yMagnitude = value; }
        }

        #endregion

        #region Public Routines

        public void Initialize()
        {
            theta = -MathHelper.PiOver2;
            position = startPosition;
        }
               
        public void Update(float dt)
        {
            theta += dt * 10.0f;

            if (theta >= 3.0f * MathHelper.PiOver2)
                theta = -MathHelper.PiOver2;

            float xPos = position.X + xMagnitude * (float)Math.Cos(theta);
            float yPos =  yMagnitude * (float)(Math.Sin(theta) * Math.Cos(theta));

            position = new Vector2(xPos, yPos);
        }
        
        #endregion
    }
}
