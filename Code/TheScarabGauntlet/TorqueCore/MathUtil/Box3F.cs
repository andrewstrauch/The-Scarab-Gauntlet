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
    /// Helper class for working with boxes.
    /// </summary>
    public struct Box3F
    {

        #region Static methods

        /// <summary>
        /// Transforms a box with a specified transform.
        /// </summary>
        /// <param name="inBox">The box to transform.</param>
        /// <param name="transform">The transform.</param>
        /// <returns>The transformed box.</returns>
        static public Box3F Transform(ref Box3F inBox, ref Matrix transform)
        {
            Vector3 center = 0.5f * (inBox.Min + inBox.Max);
            Vector3 extent = inBox.Max - center;

            Vector3 newExtent = new Vector3(
                    extent.X * Math.Abs(transform.M11) + extent.Y * Math.Abs(transform.M21) + extent.Z * Math.Abs(transform.M31),
                    extent.X * Math.Abs(transform.M12) + extent.Y * Math.Abs(transform.M22) + extent.Z * Math.Abs(transform.M32),
                    extent.X * Math.Abs(transform.M13) + extent.Y * Math.Abs(transform.M23) + extent.Z * Math.Abs(transform.M33));

            center = Vector3.Transform(center, transform);
            Box3F outBox = new Box3F(center - newExtent, center + newExtent);

#if extra_strict_check
            // extra strict check
            {
                // Cycle through all the corners of the box and transform them.  Then
                // extend test box by transformed corners.  This box should match the
                // one we computed above except for precision errors.
                Box3F testBox= new Box3F(10E10f,10E10f,10E10f,-10E10f,-10E10f,-10E10f);
                center = 0.5f * (inBox.Min + inBox.Max);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = center;
                    corner.X += ((i & 1) == 0) ? extent.X : -extent.X;
                    corner.Y += ((i & 2) == 0) ? extent.Y : -extent.Y;
                    corner.Z += ((i & 4) == 0) ? extent.Z : -extent.Z;
                    corner = Vector3.Transform(corner, transform);
                    testBox.Extend(corner);
                }

                Assert.Fatal(Math.Abs(testBox.Min.X - outBox.Min.X) < 0.001f && Math.Abs(testBox.Min.Y - outBox.Min.Y) < 0.001f && Math.Abs(testBox.Min.Z - outBox.Min.Z) < 0.001f, "doh");
                Assert.Fatal(Math.Abs(testBox.Max.X - outBox.Max.X) < 0.001f && Math.Abs(testBox.Max.Y - outBox.Max.Y) < 0.001f && Math.Abs(testBox.Max.Z - outBox.Max.Z) < 0.001f, "doh");
            }
#endif

            return outBox;
        }

        /// <summary>
        /// Scale a box.
        /// </summary>
        /// <param name="box">The box to scale.</param>
        /// <param name="scale">The amount to scale in each direction.</param>
        /// <returns>The scaled box.</returns>
        static public Box3F Scale(ref Box3F box, ref Vector3 scale)
        {
            Vector3 center = 0.5f * (box.Max + box.Min);
            Vector3 extent = 0.5f * (box.Max - box.Min);
            extent.X *= scale.X;
            extent.Y *= scale.Y;
            extent.Z *= scale.Z;
            Box3F outBox = new Box3F();
            outBox.Min = center - extent;
            outBox.Max = center + extent;
            return outBox;
        }

        #endregion


        #region Constructors

        public Box3F(float xmin, float ymin, float zmin, float xmax, float ymax, float zmax)
        {
            Min = new Vector3(xmin, ymin, zmin);
            Max = new Vector3(xmax, ymax, zmax);
        }

        public Box3F(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        #endregion


        #region Public properties and fields

        /// <summary>
        /// Returns the center point of this box.
        /// </summary>
        public Vector3 Center
        {
            get
            {
                return new Vector3(
                    (float)((Min.X + Max.X) * 0.5),
                    (float)((Min.Y + Max.Y) * 0.5),
                    (float)((Min.Z + Max.Z) * 0.5)
                    );
            }
        }

        /// <summary>
        /// The minimum point of the box.
        /// </summary>
        public Vector3 Min;

        /// <summary>
        /// The maximum point of the box.
        /// </summary>
        public Vector3 Max;

        #endregion


        #region Public methods

        /// <summary>
        /// Perform an intersection operation with this vector and store
        /// the results in this box.
        /// </summary>
        /// <param name="vec"></param>
        public void Extend(Vector3 vec)
        {
            if (vec.X < Min.X)
                Min.X = vec.X;
            if (vec.X > Max.X)
                Max.X = vec.X;

            if (vec.Y < Min.Y)
                Min.Y = vec.Y;
            if (vec.Y > Max.Y)
                Max.Y = vec.Y;

            if (vec.Z < Min.Z)
                Min.Z = vec.Z;
            if (vec.Z > Max.Z)
                Max.Z = vec.Z;
        }

        /// <summary>
        /// Check to see if a point is contained in this box.
        /// </summary>
        /// <param name="point">The position to check.</param>
        /// <returns>Returns true if the specified point is contained in this box.</returns>
        public bool Contains(Vector3 point)
        {
            return point.X <= Max.X && point.Y <= Max.Y && point.Z <= Max.Z &&
                   point.X >= Min.X && point.Y >= Min.Y && point.Z >= Min.Z;

        }

        /// <summary>
        /// Check to see if another box overlaps this box.
        /// </summary>
        /// <param name="inBox">The other box to check.</param>
        /// <returns>Returns true if the specified box overlaps this box.</returns>
        public bool Overlaps(ref Box3F inBox)
        {
            if (inBox.Min.X > Max.X ||
                inBox.Min.Y > Max.Y ||
                inBox.Min.Z > Max.Z)
                return false;

            if (inBox.Max.X < Min.X ||
                inBox.Max.Y < Min.Y ||
                inBox.Max.Z < Min.Z)
                return false;

            return true;
        }

        #endregion
    }
}
