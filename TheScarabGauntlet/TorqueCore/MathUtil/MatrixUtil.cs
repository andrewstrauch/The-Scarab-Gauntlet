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
    /// Handy matrix utilities.  These methods duplicate much of what 
    /// is already in XNA matrix and vector classes and thus should probably be
    /// fazed out. They are left over from earlier versions of XNA that didn't
    /// include this functionality.
    /// </summary>
    public class MatrixUtil
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Gets a row from a matrix.
        /// </summary>
        /// <param name="row">The row to grab.</param>
        /// <param name="m">The matrix to pull from.</param>
        /// <returns>The data from the specified row.</returns>
        public static Vector3 MatrixGetRow(int row, ref Matrix m)
        {
            switch (row)
            {
                case 0:
                    return new Vector3(m.M11, m.M12, m.M13);
                case 1:
                    return new Vector3(m.M21, m.M22, m.M23);
                case 2:
                    return new Vector3(m.M31, m.M32, m.M33);
                case 3:
                    return new Vector3(m.M41, m.M42, m.M43);
                default:
                    Assert.Fatal(false, "MatrixGetRow: Row " + row + " out of range.");
                    return new Vector3();
            }
        }

        /// <summary>
        /// Sets a row in a matrix.
        /// </summary>
        /// <param name="row">The row to set.</param>
        /// <param name="m">The matrix to set.</param>
        /// <param name="vec">The vector to set.</param>
        public static void MatrixSetRow(int row, ref Matrix m, ref Vector3 vec)
        {
            switch (row)
            {
                case 0:
                    m.M11 = vec.X;
                    m.M12 = vec.Y;
                    m.M13 = vec.Z;
                    return;
                case 1:
                    m.M21 = vec.X;
                    m.M22 = vec.Y;
                    m.M23 = vec.Z;
                    return;
                case 2:
                    m.M31 = vec.X;
                    m.M32 = vec.Y;
                    m.M33 = vec.Z;
                    return;
                case 3:
                    m.M41 = vec.X;
                    m.M42 = vec.Y;
                    m.M43 = vec.Z;
                    return;
                default:
                    Assert.Fatal(false, "MatrixSetRow: Row " + row + " out of range.");
                    break;
            }
        }

        /// <summary>
        /// Scale a vector by a matrix.
        /// </summary>
        /// <param name="scale">The vector to scale.</param>
        /// <param name="mat">The matrix to use to scale with.</param>
        public static void ApplyPreScale(ref Vector3 scale, ref Matrix mat)
        {
            mat.M11 *= scale.X; mat.M12 *= scale.Y; mat.M13 *= scale.Z;
            mat.M21 *= scale.X; mat.M22 *= scale.Y; mat.M23 *= scale.Z;
            mat.M31 *= scale.X; mat.M32 *= scale.Y; mat.M33 *= scale.Z;
        }

        /// <summary>
        /// Scale a vector by a matrix.
        /// </summary>
        /// <param name="scale">The vector to scale.</param>
        /// <param name="mat">The matrix to use to scale with.</param>
        public static void ApplyPostScale(ref Vector3 scale, ref Matrix mat)
        {
            mat.M11 *= scale.X; mat.M12 *= scale.X; mat.M13 *= scale.X;
            mat.M21 *= scale.Y; mat.M22 *= scale.Y; mat.M23 *= scale.Y;
            mat.M31 *= scale.Z; mat.M32 *= scale.Z; mat.M33 *= scale.Z;
        }

        /// <summary>
        /// Get the scale value of a matrix
        /// </summary>
        /// <param name="mat">The matrix.</param>
        /// <param name="scale">Vector to receive the data.</param>
        public static void GetPreScale(ref Matrix mat, out Vector3 scale)
        {
            float x = (float)Math.Sqrt(mat.M11 * mat.M11 + mat.M21 * mat.M21 + mat.M31 * mat.M31);
            float y = (float)Math.Sqrt(mat.M12 * mat.M12 + mat.M22 * mat.M22 + mat.M32 * mat.M32);
            float z = (float)Math.Sqrt(mat.M13 * mat.M13 + mat.M23 * mat.M23 + mat.M33 * mat.M33);
            scale = new Vector3(x, y, z);
        }

        /// <summary>
        /// Get the scale value of a matrix
        /// </summary>
        /// <param name="mat">The matrix.</param>
        /// <param name="scale">Vector to receive the data.</param>
        public static void GetPostScale(ref Matrix mat, out Vector3 scale)
        {
            float x = (float)Math.Sqrt(mat.M11 * mat.M11 + mat.M12 * mat.M12 + mat.M13 * mat.M13);
            float y = (float)Math.Sqrt(mat.M21 * mat.M21 + mat.M22 * mat.M22 + mat.M23 * mat.M23);
            float z = (float)Math.Sqrt(mat.M31 * mat.M31 + mat.M32 * mat.M32 + mat.M33 * mat.M33);
            scale = new Vector3(x, y, z);
        }

        /// <summary>
        /// Get the first row from a matrix.
        /// </summary>
        /// <param name="m">The matrix to pull from.</param>
        /// <param name="p">The vector that receives the data.</param>
        public static void GetX(ref Matrix m, out Vector3 x)
        {
            x = new Vector3(m.M11, m.M12, m.M13);
        }

        /// <summary>
        /// Get the second row from a matrix.
        /// </summary>
        /// <param name="m">The matrix to pull from.</param>
        /// <param name="p">The vector that receives the data.</param>
        public static void GetY(ref Matrix m, out Vector3 y)
        {
            y = new Vector3(m.M21, m.M22, m.M23);
        }

        /// <summary>
        /// Get the third row from a matrix.
        /// </summary>
        /// <param name="m">The matrix to pull from.</param>
        /// <param name="p">The vector that receives the data.</param>
        public static void GetZ(ref Matrix m, out Vector3 z)
        {
            z = new Vector3(m.M31, m.M32, m.M33);
        }

        /// <summary>
        /// Get the translation from a matrix.
        /// </summary>
        /// <param name="m">The matrix to pull from.</param>
        /// <param name="p">The vector that receives the data.</param>
        public static void GetP(ref Matrix m, out Vector3 p)
        {
            p = new Vector3(m.M41, m.M42, m.M43);
        }

        /// <summary>
        /// Transform a position with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <returns>The transformed point.</returns>
        public static Vector4 MatMulP(ref Vector4 pnt, ref Matrix mat)
        {
            return new Vector4(
                    pnt.X * mat.M11 + pnt.Y * mat.M21 + pnt.Z * mat.M31 + mat.M41,
                    pnt.X * mat.M12 + pnt.Y * mat.M22 + pnt.Z * mat.M32 + mat.M42,
                    pnt.X * mat.M13 + pnt.Y * mat.M23 + pnt.Z * mat.M33 + mat.M43,
                    pnt.X * mat.M14 + pnt.Y * mat.M24 + pnt.Z * mat.M33 + mat.M44);
        }

        /// <summary>
        /// Transform a position with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <returns>The transformed point.</returns>
        public static Vector3 MatMulP(ref Vector3 pnt, ref Matrix mat)
        {
            return new Vector3(
                    pnt.X * mat.M11 + pnt.Y * mat.M21 + pnt.Z * mat.M31 + mat.M41,
                    pnt.X * mat.M12 + pnt.Y * mat.M22 + pnt.Z * mat.M32 + mat.M42,
                    pnt.X * mat.M13 + pnt.Y * mat.M23 + pnt.Z * mat.M33 + mat.M43);
        }

        /// <summary>
        /// Transform a position with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <param name="mat">The output transformed point.</param>
        public static void MatMulP(ref Vector3 pnt, ref Matrix mat, out Vector3 outPnt)
        {
#if XBOX
              outPnt = Vector3.Zero;
#endif
            outPnt.X = pnt.X * mat.M11 + pnt.Y * mat.M21 + pnt.Z * mat.M31 + mat.M41;
            outPnt.Y = pnt.X * mat.M12 + pnt.Y * mat.M22 + pnt.Z * mat.M32 + mat.M42;
            outPnt.Z = pnt.X * mat.M13 + pnt.Y * mat.M23 + pnt.Z * mat.M33 + mat.M43;
        }

        /// <summary>
        /// Transform a position with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <returns>The transformed point.</returns>
        public static Vector2 MatMulP(ref Vector2 pnt, ref Matrix mat)
        {
            return new Vector2(
                    pnt.X * mat.M11 + pnt.Y * mat.M21 + mat.M31 + mat.M41,
                    pnt.X * mat.M12 + pnt.Y * mat.M22 + mat.M32 + mat.M42);
        }

        /// <summary>
        /// Transform a vector with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <returns>The transformed point.</returns>
        public static Vector3 MatMulV(ref Vector3 pnt, ref Matrix mat)
        {
            return new Vector3(
                    pnt.X * mat.M11 + pnt.Y * mat.M21 + pnt.Z * mat.M31,
                    pnt.X * mat.M12 + pnt.Y * mat.M22 + pnt.Z * mat.M32,
                    pnt.X * mat.M13 + pnt.Y * mat.M23 + pnt.Z * mat.M33);
        }

        /// <summary>
        /// Transform a vector with a matrix.
        /// </summary>
        /// <param name="pnt">The point to transform.</param>
        /// <param name="mat">The transform matrix.</param>
        /// <returns>The transformed point.</returns>
        public static Vector2 MatMulV(ref Vector2 pnt, ref Matrix mat)
        {
            return new Vector2(
                    pnt.X * mat.M11 + pnt.Y * mat.M21,
                    pnt.X * mat.M12 + pnt.Y * mat.M22);
        }

        /// <summary>
        /// Creates a rotation matrix that is oriented the same way as a vector.
        /// </summary>
        /// <param name="mat">The matrix to orient.</param>
        /// <param name="upDir">The direction to snap the matrix to.</param>
        public static void SnapToSurface(ref Matrix mat, ref Vector3 upDir)
        {
            Vector3 y = MatrixGetRow(1, ref mat);
            Vector3 x = Vector3.Cross(y, upDir);
            if (x.LengthSquared() < 0.01f)
            {
                // y and z almost parallel, let's use x instead
                x = MatrixGetRow(0, ref mat);
                y = Vector3.Cross(upDir, x);
                y.Normalize();
                x = Vector3.Cross(y, upDir);
            }
            else
            {
                x.Normalize();
                y = Vector3.Cross(upDir, x);
            }

            MatrixSetRow(0, ref mat, ref x);
            MatrixSetRow(1, ref mat, ref y);
            MatrixSetRow(2, ref mat, ref upDir);
        }

        /// <summary>
        /// Creates a rotation matrix that will aim from one point to another. The resulting
        /// matrix can be used to make objects face a certain direction.
        /// </summary>
        /// <param name="from">The source point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The aimed matrix.</returns>
        public static Matrix CreateAimedTransform(Vector3 from, Vector3 to)
        {
            // get a normalized vector from 'from' to 'to'
            Vector3 aimVector = to - from;
            aimVector.Normalize();

            // get a vector to the left (no roll)
            Vector3 leftVector = new Vector3(-aimVector.Y, aimVector.X, 0);
            if (!leftVector.Equals(Vector3.Zero))
                leftVector.Normalize();
            else
                leftVector = Vector3.Backward;

            // get the up vector
            Vector3 upVector = Vector3.Cross(aimVector, leftVector);
            upVector.Normalize();

            // create the matrix
            Matrix transform = Matrix.Identity;
            transform.Up = aimVector;
            transform.Backward = upVector;
            transform.Left = leftVector;

            // set the translation to the from position
            transform.Translation = from;

            // return the new matrix
            return transform;
        }

        #endregion
    }
}
