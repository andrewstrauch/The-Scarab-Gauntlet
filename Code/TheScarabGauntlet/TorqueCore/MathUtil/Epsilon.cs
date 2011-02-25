//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Use this class to test for values close to zero.  If you use the default
    /// epsilon value then use the static version of this (Epsilon) instead.
    /// </summary>
    public struct EpsilonTester
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Common Epsilon.
        /// </summary>
        public float Value;

        #endregion


        #region Public methods

        /// <summary>
        /// Initialize tester with given epsilon.  If default constructor
        /// used then epsilon will be 0.
        /// </summary>
        /// <param name="epsilon">Initial epsilon.</param>
        public EpsilonTester(float epsilon)
        {
            Value = epsilon;
        }

        /// <summary>
        /// Test if  given float is near zero.
        /// </summary>
        /// <param name="f">Float to test.</param>
        /// <returns>True if not close to zero.</returns>
        public bool FloatIsNotZero(float f)
        {
            return (Math.Abs(f) >= Value);
        }

        /// <summary>
        /// Test if  given float is near zero.
        /// </summary>
        /// <param name="f">Float to test.</param>
        /// <returns>True if close to zero.</returns>
        public bool FloatIsZero(float f)
        {
            return (Math.Abs(f) < Value);
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public bool VectorIsZero(Vector2 v)
        {
            return Vector2.Dot(v, v) <= Value;
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public bool VectorIsZero(Vector3 v)
        {
            return Vector3.Dot(v, v) <= Value;
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public bool VectorIsZero(Vector4 v)
        {
            return Vector4.Dot(v, v) <= Value;
        }

        #endregion
    }

    /// <summary>
    /// Static version of EpsilonTester.  Epsilon value is fixed at 0.001.
    /// </summary>
    public class Epsilon
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Common Epsilon.
        /// </summary>
        public const float Value = 0.001f;

        /// <summary>
        /// Test if  given float is near zero.
        /// </summary>
        /// <param name="f">Float to test.</param>
        /// <returns>True if not close to zero.</returns>
        public static bool FloatIsNotZero(float f)
        {
            return (Math.Abs(f) >= Value);
        }

        /// <summary>
        /// Test if  given float is near zero.
        /// </summary>
        /// <param name="f">Float to test.</param>
        /// <returns>True if close to zero.</returns>
        public static bool FloatIsZero(float f)
        {
            return (Math.Abs(f) < Value);
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public static bool VectorIsZero(Vector2 v)
        {
            return Vector2.Dot(v, v) <= Value;
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public static bool VectorIsZero(Vector3 v)
        {
            return Vector3.Dot(v, v) <= Value;
        }

        /// <summary>
        /// Test if given vector is close to zero.
        /// </summary>
        /// <param name="v">Vector to test.</param>
        /// <returns>True if close to zero.</returns>
        public static bool VectorIsZero(Vector4 v)
        {
            return Vector4.Dot(v, v) <= Value;
        }

        #endregion
    }
}
