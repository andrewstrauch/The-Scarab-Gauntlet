//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Quaternion structure that is stored with less precision than the default XNA
    /// Quaternion, thus taking up less memory. Mostly used by the TS library.
    /// </summary>
    public struct Quat16
    {

        #region Public properties, operators, constants, and enums

        enum Constants
        {
            MAX_VAL = 0x7fff
        };

        public short X, Y, Z, W;

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the quaternion to an identity.
        /// </summary>
        public void SetIdentity()
        {
            X = Y = Z = 0;
            W = (short)Constants.MAX_VAL;
        }

        /// <summary>
        /// Gets the quaternion as an XNA quaternion.
        /// </summary>
        /// <param name="q">The quaternion to receive the data.</param>
        public void Get(out Quaternion q)
        {
            q = new Quaternion((float)X / (float)Constants.MAX_VAL,
                    (float)Y / (float)Constants.MAX_VAL,
                    (float)Z / (float)Constants.MAX_VAL,
                    (float)W / (float)Constants.MAX_VAL);
        }

        /// <summary>
        /// Sets the quaternion from an XNA quaternion.
        /// </summary>
        /// <param name="q">The quaternion to set this to.</param>
        public void Set(Quaternion q)
        {
            X = (short)(q.X * (float)Constants.MAX_VAL);
            Y = (short)(q.Y * (float)Constants.MAX_VAL);
            Z = (short)(q.Z * (float)Constants.MAX_VAL);
            W = (short)(q.W * (float)Constants.MAX_VAL);
        }

        #endregion
    }
}
