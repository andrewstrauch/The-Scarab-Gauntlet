//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Simple struct for creating and applying 2D rotations.
    /// </summary>
    public struct Rotation2D
    {

        #region Constructors

        /// <summary>
        /// Create rotation of given angle in radians.
        /// </summary>
        /// <param name="radians">Angle to rotate by.</param>
        public Rotation2D(float radians)
        {
            float s = (float)Math.Sin(radians);
            float c = (float)Math.Cos(radians);
            M11 = M22 = c;
            M12 = s;
            M21 = -s;

            // This code makes sure we get the same matrix as full matrix class.
            //Matrix mat = Matrix.CreateRotationZ(radians);
            //Assert.Fatal(Math.Abs(mat.M11 - M11) < 0.0001f, "doh");
            //Assert.Fatal(Math.Abs(mat.M12 - M12) < 0.0001f, "doh");
            //Assert.Fatal(Math.Abs(mat.M21 - M21) < 0.0001f, "doh");
            //Assert.Fatal(Math.Abs(mat.M22 - M22) < 0.0001f, "doh");
        }

        /// <summary>
        /// create rotation using 2x2 matrix.  No verification is performed to
        /// make sure matrix is a true rotation.
        /// </summary>
        /// <param name="m11">Row 1, Column 1.</param>
        /// <param name="m21">Row 2, Column 1.</param>
        /// <param name="m12">Row 1, Column 2.</param>
        /// <param name="m22">Row 1, Column 2.</param>
        public Rotation2D(float m11, float m21, float m12, float m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// First row of matrix representing this rotation.
        /// </summary>
        public Vector2 X
        {
            get { return new Vector2(M11, M12); }
        }

        /// <summary>
        /// Second row of matrix representing this rotation.
        /// </summary>
        public Vector2 Y
        {
            get { return new Vector2(M21, M22); }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Transform given vector by this rotation.
        /// </summary>
        /// <param name="vec">Vector to rotate.</param>
        /// <returns>Resulting vector.</returns>
        public Vector2 Rotate(Vector2 vec)
        {
            return new Vector2(M11 * vec.X + M21 * vec.Y, M12 * vec.X + M22 * vec.Y);
        }

        /// <summary>
        /// Transform given vector by the inverse of this rotation.
        /// </summary>
        /// <param name="vec">Vector to rotate.</param>
        /// <returns>Resulting vector.</returns>
        public Vector2 Unrotate(Vector2 vec)
        {
            return new Vector2(M11 * vec.X + M12 * vec.Y, M21 * vec.X + M22 * vec.Y);
        }

        /// <summary>
        /// Find the inverse of this rotation which rotates in the other direction.
        /// </summary>
        /// <returns>The inverted rotation.</returns>
        public Rotation2D Invert()
        {
            return new Rotation2D(M11, M12, M21, M22);
        }

        /// <summary>
        /// Concatenate two rotations and form a new one.
        /// </summary>
        /// <param name="a">First rotation.</param>
        /// <param name="b">Second rotation.</param>
        /// <returns>Resulting rotation.</returns>
        public static Rotation2D operator *(Rotation2D a, Rotation2D b)
        {
            return new Rotation2D(b.M11 * a.M11 + b.M21 * a.M12, b.M11 * a.M21 + b.M21 * a.M22, b.M12 * a.M11 + b.M22 * a.M12, b.M12 * a.M21 + b.M22 * a.M22);
        }

        #endregion


        #region Private, protected, internal fields

        internal float M11;
        internal float M12;
        internal float M21;
        internal float M22;

        #endregion
    }
}
