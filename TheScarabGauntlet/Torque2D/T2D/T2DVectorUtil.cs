//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.T2D;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Static utility methods for converting between rotation (in degrees)
    /// and Vector2.  These methods take T2D conventions into consideration
    /// (e.g., up is 0 degrees rotation and up depends on whether screen 
    /// coordinates or input coordinates are being used).
    /// </summary>
    public static class T2DVectorUtil
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Calculate the clockwise angle from analog stick input.  Positive y-axis (0,1) is 0 
        /// degrees rotation, so an object drawn with the returned rotation will have it's top 
        /// pointing in the direction of the input stick (note: in T2D -y is up but on the 
        /// thumbstick +y is up).  Return value is in degrees.
        /// </summary>
        /// <param name="inputVector">The input vector.</param>
        /// <returns>Clockwise angle in degrees.</returns>
        public static float AngleFromInput(Vector2 inputVector)
        {
            float rotation = (float)Math.Atan2(inputVector.X, inputVector.Y);
            return ((MathHelper.ToDegrees(rotation) % 360.0f) + 360.0f) % 360.0f;
        }



        /// <summary>
        /// Calculate the clockwise angle from an offset vector assuming T2D coordinates.
        /// Negative y-axis (0,-1) is 0 degrees rotation, so an object drawn with the returned
        /// rotation will have it's top pointing along the vector (note: in T2D -y is up).  
        /// Return value is in degrees.
        /// </summary>
        /// <param name="vector">Offset vector.</param>
        /// <returns>Clockwise angle in degrees.</returns>
        public static float AngleFromVector(Vector2 vector)
        {
            float rotation = (float)Math.Atan2(vector.X, -vector.Y);
            return ((MathHelper.ToDegrees(rotation) % 360.0f) + 360.0f) % 360.0f;
        }



        /// <summary>
        /// Calculate clockwise angle of vector between two points.  Negative y-axis (0,-1) is 0
        /// degrees rotation, so an object drawn with the returned rotation will have it's top
        /// pointing from the source position to the target position (note: in T2D -y is up).
        /// Return value is in degrees.
        /// </summary>
        /// <param name="srcPos">Source position.</param>
        /// <param name="targetPos">Target position.</param>
        /// <returns>Clockwise angle in degrees.</returns>
        public static float AngleFromTarget(Vector2 srcPos, Vector2 targetPos)
        {
            return AngleFromVector(targetPos - srcPos);
        }



        /// <summary>
        /// Convert a vector from an input stick into a velocity.  Converts from
        /// "plus y-up" space of input controller to "minus y-up" screen space.
        /// </summary>
        /// <param name="inputVector">The input stick vector.</param>
        /// <param name="speedVector">Scale of velocity vector.</param>
        /// <returns>Velocity in direction of input stick.</returns>
        public static Vector2 VelocityFromInput(Vector2 inputVector, Vector2 speedScale)
        {
            // normalize vector safely
            float len = inputVector.Length();
            if (len > Epsilon.Value)
                inputVector *= 1.0f / len;

            // flip Y since the Y-Axis of our stick comes in "upside down"
            inputVector.Y = -inputVector.Y;

            // adjust normalized vector by speed
            inputVector *= speedScale;

            return inputVector;
        }



        /// <summary>
        /// Convert an offset vector to a velocity which will move toards the target
        /// at the given speed.
        /// </summary>
        /// <param name="srcPos">Source position.</param>
        /// <param name="targetPos">Target position.</param>
        /// <param name="speedScale">Target speed.</param>
        /// <returns>Velocity toward target.</returns>
        public static Vector2 VelocityFromTarget(Vector2 srcPos, Vector2 targetPos, float speed)
        {
            // calculate difference between target and src
            Vector2 velocity = targetPos - srcPos;

            // normalize vector safely
            float len = velocity.Length();
            if (len > Epsilon.Value)
                velocity *= 1.0f / len;

            // scale return value by speedScale
            return speed * velocity;
        }



        /// <summary>
        /// Calculate vector pointing in the given angle.  Angle is in clockwise
        /// degrees from "up" (negative y -- (0,-1) -- is up).
        /// </summary>
        /// <param name="rotation">Rotation in degrees.</param>
        /// <returns>Vector pointing at given rotation.</returns>
        public static Vector2 VectorFromAngle(float rotation)
        {
            float radians = MathHelper.ToRadians(rotation);
            return new Vector2((float)Math.Sin(radians), -(float)Math.Cos(radians));
        }

        #endregion
    }
}
