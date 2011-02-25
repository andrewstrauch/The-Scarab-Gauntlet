//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Rectangle structure with several useful rectangle related methods. This is
    /// for floating point rectangles.
    /// </summary>
    public struct RectangleF
    {

        #region Constructors

        /// <summary>
        /// Creates a rectangle from another rectangle.
        /// </summary>
        /// <param name="rectangle"></param>
        public RectangleF(RectangleF rectangle)
        {
            _point = rectangle.Point;
            _extent = rectangle.Extent;
        }

        /// <summary>
        /// Create a rectangle based on a point and size.
        /// </summary>
        /// <param name="point">The upper left point of the rectangle.</param>
        /// <param name="extent">The size of the rectangle.</param>
        public RectangleF(Vector2 point, Vector2 extent)
        {
            _point = point;
            _extent = extent;
        }

        /// <summary>
        /// Create a rectangle based on the top left corner, width, and height.
        /// </summary>
        /// <param name="left">The left most point.</param>
        /// <param name="top">The top most point.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height</param>
        public RectangleF(float left, float top, float width, float height)
        {
            _point = new Vector2(left, top);
            _extent = new Vector2(width, height);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public float Width
        {
            get { return _extent.X; }
            set { _extent.X = value; }
        }

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public float Height
        {
            get { return _extent.Y; }
            set { _extent.Y = value; }
        }

        /// <summary>
        /// The left most point of the rectangle.
        /// </summary>
        public float X
        {
            get { return _point.X; }
            set { _point.X = value; }
        }

        /// <summary>
        /// The top most point of the rectangle.
        /// </summary>
        public float Y
        {
            get { return _point.Y; }
            set { _point.Y = value; }
        }

        /// <summary>
        /// The upper left corner of the rectangle.
        /// </summary>
        public Vector2 Point
        {
            get { return _point; }
            set { _point = value; }
        }

        /// <summary>
        /// The size of the rectangle.
        /// </summary>
        public Vector2 Extent
        {
            get { return _extent; }
            set { _extent = value; }
        }

        /// <summary>
        /// The center point of the rectangle.
        /// </summary>
        public Vector2 Center
        {
            get
            {
                return new Vector2(
                    _point.X + (_extent.X * 0.5f),
                    _point.Y + (_extent.Y * 0.5f));
            }
        }

        /// <summary>
        /// Whether or not the rectangle is valid (size > 0 in both dimensions).
        /// </summary>
        public bool IsValid
        {
            get { return (_extent.X > 0 && _extent.Y > 0); }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Shrink the rectangle by a specified amount in each direction.
        /// </summary>
        /// <param name="x">The amount to inset in x.</param>
        /// <param name="y">The amount to inset in y.</param>
        public void Inset(float x, float y)
        {
            _point.X += x;
            _point.Y += y;
            _extent.X -= 2.0f * x;
            _extent.Y -= 2.0f * y;
        }

        /// <summary>
        /// Move the rectangle by a specified amount in each direction.
        /// </summary>
        /// <param name="x">The amount to move in x.</param>
        /// <param name="y">The amount to move in y.</param>
        public void Offset(float x, float y)
        {
            _point.X += x;
            _point.Y += y;
        }

        /// <summary>
        /// Set this rectangle to the intersection of it and another rectangle.
        /// </summary>
        /// <param name="clipRect">The rectangle to intersect with.</param>
        /// <returns>Whether or not the resulting rectangle is valid.</returns>
        public bool Intersect(RectangleF clipRect)
        {
            Vector2 bottomL;
            bottomL.X = MathHelper.Min(_point.X + _extent.X, clipRect.Point.X + clipRect.Extent.X);
            bottomL.Y = MathHelper.Min(_point.Y + _extent.Y, clipRect.Point.Y + clipRect.Extent.Y);

            _point.X = MathHelper.Max(_point.X, clipRect.Point.X);
            _point.Y = MathHelper.Max(_point.Y, clipRect.Point.Y);

            _extent.X = bottomL.X - _point.X;
            _extent.Y = bottomL.Y - _point.Y;

            return IsValid;
        }

        /// <summary>
        /// Extend the rectangle so that it contains a point.
        /// </summary>
        /// <param name="point">The point to envelop.</param>
        public void Union(Vector2 point)
        {
            Vector2 end = _point + _extent;

            _point.X = MathHelper.Min(_point.X, point.X);
            _point.Y = MathHelper.Min(_point.Y, point.Y);

            if (_extent.X < 0)
                _extent.X = 0;
            else
                _extent.X = MathHelper.Max(end.X, point.X) - _point.X;

            if (_extent.Y < 0)
                _extent.Y = 0;
            else
                _extent.Y = MathHelper.Max(end.Y, point.Y) - _point.Y;
        }

        /// <summary>
        /// Extend the rectangle so that it contains another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to envelop.</param>
        public void Union(RectangleF rect)
        {
            Union(rect._point);
            Union(rect._point + rect._extent);
        }

        /// <summary>
        /// Returns whether or not this rectangle intersects with another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test intersections with.</param>
        /// <returns>True if the rectangles intersect.</returns>
        public bool IntersectsWith(RectangleF rect)
        {
            if (_point.X + _extent.X < rect._point.X)
                return false;

            if (_point.Y + _extent.Y < rect._point.Y)
                return false;

            if (rect._point.X + rect._extent.X < _point.X)
                return false;

            if (rect._point.Y + rect._extent.Y < _point.Y)
                return false;

            return true;
        }

        /// <summary>
        /// Tests if this rectangle overlaps another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test against.</param>
        /// <returns></returns>
        public bool Overlaps(RectangleF rect)
        {
            RectangleF test = new RectangleF(this);
            return test.Intersect(rect);
        }

        /// <summary>
        /// Returns whether or not this rectangle entirely contains another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test against.</param>
        /// <returns>True if the rectangle is entirely contained.</returns>
        public bool Contains(RectangleF rect)
        {
            if (_point.X <= rect.Point.X && _point.Y <= rect.Point.Y)
            {
                if (rect.Point.X + rect.Extent.X <= _point.X + _extent.X)
                {
                    if (rect.Point.Y + rect.Extent.Y <= _point.Y + _extent.Y)
                        return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RectangleF))
                return false;
            RectangleF other = (RectangleF)obj;

            return _point.Equals(other._point) && _extent.Equals(other._extent);
        }

        public static bool operator ==(RectangleF x, RectangleF y)
        {
            return x._extent == y._extent && x._point == y._point;
        }

        public static bool operator !=(RectangleF x, RectangleF y)
        {
            return !(x._extent == y._extent && x._point == y._point);
        }

        public override int GetHashCode()
        {
            return _point.GetHashCode() ^ _extent.GetHashCode();
        }

        #endregion


        #region Private, protected, internal fields

        Vector2 _point;
        Vector2 _extent;

        #endregion
    }



    /// <summary>
    /// Rectangle structure with several useful rectangle related methods. This is for integer
    /// rectangles.
    /// </summary>
    public struct RectangleI
    {

        #region Constructors

        /// <summary>
        /// Creates a rectangle from another rectangle.
        /// </summary>
        /// <param name="rectangle"></param>
        public RectangleI(RectangleI rectangle)
        {
            _point = rectangle.Point;
            _extent = rectangle.Extent;
        }

        /// <summary>
        /// Create a rectangle based on a point and size.
        /// </summary>
        /// <param name="point">The upper left point of the rectangle.</param>
        /// <param name="extent">The size of the rectangle.</param>
        public RectangleI(Point point, Point extent)
        {
            _point = point;
            _extent = extent;
        }

        /// <summary>
        /// Create a rectangle based on the top left corner, width, and height.
        /// </summary>
        /// <param name="left">The left most point.</param>
        /// <param name="top">The top most point.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height</param>
        public RectangleI(int x, int y, int width, int height)
        {
            _point = new Point(x, y);
            _extent = new Point(width, height);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The left most point of the rectangle.
        /// </summary>
        public int X
        {
            get { return _point.X; }
            set { _point.X = value; }
        }

        /// <summary>
        /// The top most point of the rectangle.
        /// </summary>
        public int Y
        {
            get { return _point.Y; }
            set { _point.Y = value; }
        }

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public int Width
        {
            get { return _extent.X; }
            set { _extent.X = value; }
        }

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public int Height
        {
            get { return _extent.Y; }
            set { _extent.Y = value; }
        }

        /// <summary>
        /// The upper left corner of the rectangle.
        /// </summary>
        public Point Point
        {
            get { return _point; }
            set { _point = value; }
        }

        /// <summary>
        /// The size of the rectangle.
        /// </summary>
        public Point Extent
        {
            get { return _extent; }
            set { _extent = value; }
        }

        /// <summary>
        /// Whether or not the rectangle is valid (size > 0 in both dimensions).
        /// </summary>
        public bool IsValid
        {
            get { return (_extent.X > 0 && _extent.Y > 0); }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Shrink the rectangle by a specified amount in each direction.
        /// </summary>
        /// <param name="x">The amount to inset in x.</param>
        /// <param name="y">The amount to inset in y.</param>
        public void Inset(int x, int y)
        {
            _point.X += x;
            _point.Y += y;
            _extent.X -= 2 * x;
            _extent.Y -= 2 * y;
        }

        /// <summary>
        /// Move the rectangle by a specified amount in each direction.
        /// </summary>
        /// <param name="x">The amount to move in x.</param>
        /// <param name="y">The amount to move in y.</param>
        public void Offset(int x, int y)
        {
            _point.X += x;
            _point.Y += y;
        }

        /// <summary>
        /// Set this rectangle to the intersection of it and another rectangle.
        /// </summary>
        /// <param name="clipRect">The rectangle to intersect with.</param>
        /// <returns>Whether or not the resulting rectangle is valid.</returns>
        public bool Intersect(RectangleI clipRect)
        {
            int bottomLX = (int)MathHelper.Min(_point.X + _extent.X, clipRect.X + clipRect.Width);
            int bottomLY = (int)MathHelper.Min(_point.Y + _extent.Y, clipRect.Y + clipRect.Height);

            _point.X = (int)MathHelper.Max(_point.X, clipRect.X);
            _point.Y = (int)MathHelper.Max(_point.Y, clipRect.Y);

            _extent.X = bottomLX - _point.X;
            _extent.Y = bottomLY - _point.Y;

            return IsValid;
        }

        /// <summary>
        /// Returns whether or not this rectangle intersects with another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test intersections with.</param>
        /// <returns>True if the rectangles intersect.</returns>
        public bool IntersectsWith(RectangleI rect)
        {
            if (_point.X + _extent.X < rect._point.X)
                return false;
            if (_point.Y + _extent.Y < rect._point.Y)
                return false;
            if (rect._point.X + rect._extent.X < _point.X)
                return false;
            if (rect._point.Y + rect._extent.Y < _point.Y)
                return false;
            return true;
        }

        /// <summary>
        /// Tests if this rectangle overlaps another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test against.</param>
        /// <returns></returns>
        public bool Overlaps(RectangleI rect)
        {
            RectangleI test = new RectangleI(this);
            return test.Intersect(rect);
        }

        /// <summary>
        /// Returns whether or not this rectangle entirely contains another rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to test against.</param>
        /// <returns>True if the rectangle is entirely contained.</returns>
        public bool Contains(RectangleI rect)
        {
            if (_point.X <= rect.Point.X && _point.Y <= rect.Point.Y)
                if (rect.Point.X + rect.Extent.X <= _point.X + _extent.X)
                    if (rect.Point.Y + rect.Extent.Y <= _point.Y + _extent.Y)
                        return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RectangleI))
                return false;

            RectangleI other = (RectangleI)obj;

            return _point.Equals(other._point) && _extent.Equals(other._extent);
        }

        public static bool operator ==(RectangleI x, RectangleI y)
        {
            return x._extent == y._extent && x._point == y._point;
        }

        public static bool operator !=(RectangleI x, RectangleI y)
        {
            return !(x._extent == y._extent && x._point == y._point);
        }

        public override int GetHashCode()
        {
            return _point.GetHashCode() ^ _extent.GetHashCode();
        }

        #endregion


        #region Private, protected, internal fields

        Point _point;
        Point _extent;

        #endregion
    }
}

