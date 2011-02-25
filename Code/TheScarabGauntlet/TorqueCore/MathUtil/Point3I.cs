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
    /// Three dimensional points structure stored as integers.
    /// </summary>
    public struct Point3I : IEquatable<Point3I>
    {

        #region Static methods, fields, constructors

        public static bool operator !=(Point3I a, Point3I b)
        {
            return (a.X != b.X) || (a.Y != b.Y) || (a.Z != b.Z);
        }

        public static bool operator ==(Point3I a, Point3I b)
        {
            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z);
        }

        public static Point3I Zero
        {
            get { return new Point3I(0, 0, 0); }
        }

        #endregion


        #region Constructors

        public Point3I(Point point, int z)
        {
            X = point.X;
            Y = point.Y;
            Z = z;
        }

        public Point3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public int X;
        public int Y;
        public int Z;

        #endregion


        #region Public methods

        public override bool Equals(object obj)
        {
            if (obj is Point)
                return Equals((Point3I)obj);

            return false;
        }

        public bool Equals(Point3I other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(_format, X, Y, Z);
        }

        #endregion


        #region Private, protected, internal fields

        static string _format = "X:{0} Y:{1} Z:{2}";

        #endregion
    }
}
