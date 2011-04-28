//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Query class for the container system.  To do container queries, create an instance of this class, populate its
    /// member fields according to how you want to do your query, and then pass the instance to the container's 
    /// FindObjects method.
    /// </summary>
    public class T2DSceneContainerQueryData : SceneContainerQueryData
    {
        #region Public properties, operators, constants, and enums

        public delegate void OnObjectFoundDelegate(ISceneContainerObject obj);



        /// <summary>
        /// If non-null, this delegate will be called for each object found.  Note that because this uses a delegate, 
        /// and is called once for each matching object, it is less efficient than the resultList interface.  
        /// Use with caution.
        /// </summary>
        public OnObjectFoundDelegate onObjectFound;



        /// <summary>
        /// World-space rectangle to search for objects.
        /// </summary>
        public RectangleF Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }



        /// <summary>
        /// Bitmask containing layers in range [0..31] that should be searched.  Layers outside of this range are not searched.
        /// </summary>
        public uint LayerMask
        {
            get { return _layerMask; }
            set { _layerMask = value; }
        }



        /// <summary>
        /// Whether to compare objects render bounds or physical bounds.  Note: currently always the same.
        /// </summary>
        public bool IgnorePhysics
        {
            get { return _ignorePhysics; }
            set { value = _ignorePhysics; }
        }

        #endregion

        #region Private, protected, internal fields

        internal RectangleF _rectangle;
        internal uint _layerMask;
        internal bool _ignorePhysics = false;

        #endregion
    }



    /// <summary>
    /// Scene container used by T2DSceneGraph.
    /// </summary>
    public class T2DSceneContainer : SceneContainer
    {
        #region Constructors

        public T2DSceneContainer()
            : base()
        {
        }



        public T2DSceneContainer(BaseSceneGraph sg)
            : base(sg, DefaultBinSize, DefaultBinCount)
        {
        }



        public T2DSceneContainer(BaseSceneGraph sg, float binSize, uint binCount)
            : base(sg, binSize, binCount)
        {
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Accelerated version of CheckSceneObjectBins for T2D.
        /// </summary>
        /// <param name="obj">Object to check.</param>
        public void CheckSceneObjectBinsT2D(T2DSceneObject obj)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "Invalid Object");

            // Get the object's SceneContainerData
            SceneContainerData scd = obj.SceneContainerData;

            // Find which bins we cover.
            uint minBinX, minBinY, maxBinX, maxBinY;
            _GetBins(obj.WorldClipRectangle, out minBinX, out minBinY, out maxBinX, out maxBinY);

            _CheckSceneObjectBins(obj, scd, minBinX, minBinY, maxBinX, maxBinY);
        }

        #endregion


        #region Private, protected, internal methods

        /// Found Object Tests
        override protected bool _FoundObject(ISceneContainerObject obj, SceneContainerQueryData queryData)
        {
            // Check type on objects
            Assert.Fatal(obj is ISceneObject2D, "Invalid object passed to _FoundObject");
            Assert.Fatal(queryData is T2DSceneContainerQueryData, "Invalid query data object passed to _FoundObject");
            ISceneObject2D sceneObject = (ISceneObject2D)obj; // this is slow due to not inlined properties below, would be better to use T2DSceneObject, but lights can be in the graph and are not scene objects

            T2DSceneContainerQueryData query = (T2DSceneContainerQueryData)queryData;

            if (sceneObject.Visible || query.FindInvisible)
            {
                // Check if the Group / Layer masks match.
                int layerMask = 1 << sceneObject.Layer;
                if ((layerMask & query._layerMask) != 0)
                {
                    // Yes, so fetch Clip Rectangle.
                    if (_IntersectsWith(sceneObject, query))
                        return true;
                }
            }

            return false;
        }



        override protected bool _IntersectsWith(ISceneContainerObject obj, SceneContainerQueryData iQueryData)
        {
            // Check type on objects
            Assert.Fatal(obj is ISceneObject2D, "Invalid object passed to _FoundObject");
            Assert.Fatal(iQueryData is T2DSceneContainerQueryData, "Invalid query data object passed to _FoundObject");
            ISceneObject2D sceneObject = (ISceneObject2D)obj;
            T2DSceneContainerQueryData query = (T2DSceneContainerQueryData)iQueryData;

            return _IntersectsWith(sceneObject, query);
        }



        protected bool _IntersectsWith(ISceneObject2D obj, T2DSceneContainerQueryData query)
        {
            Assert.Fatal(!(query.onObjectFound != null && query.ResultList != null), "Please specify a delegate or result list but not both");

            // JMQtodo: since the rectangle uses a different coordinate system,
            //      will they always intersect?
            //RectangleF intersectRect = query.IgnorePhysics ?
            //    obj.WorldClipRectangle :
            //    obj.WorldCollisionClipRectangle;
            // cafTODO: WorldClipRectangle and WorldCollisionClipRectangle are the same atm
            RectangleF intersectRect = obj.WorldClipRectangle;

            // Do the collision clip rectangles intersect?
            if (query._rectangle.IntersectsWith(intersectRect))
            {
                // Yes, so perform callback.
                if (query.onObjectFound != null)
                    query.onObjectFound(obj);
                if (query.ResultList != null)
                    query.ResultList.Add(obj);

                return true;
            }

            return false;
        }



        /// Get Bin Range
        protected void _GetBins(RectangleF rectangle, out uint minBinX, out uint minBinY, out uint maxBinX, out uint maxBinY)
        {
            _GetBinRange(rectangle.X, rectangle.X + rectangle.Width, out minBinX, out maxBinX);
            _GetBinRange(rectangle.Y, rectangle.Y + rectangle.Height, out minBinY, out maxBinY);
        }



        override protected void _GetBins(ISceneContainerObject obj, out uint minBinX, out uint minBinY, out uint maxBinX, out uint maxBinY)
        {
            // Check type on object
            Assert.Fatal(obj is ISceneObject2D, "Invalid object passed to _GetBins");
            RectangleF rectangle = ((ISceneObject2D)obj).WorldCollisionClipRectangle;

            _GetBinRange(rectangle.X, rectangle.X + rectangle.Width, out minBinX, out maxBinX);
            _GetBinRange(rectangle.Y, rectangle.Y + rectangle.Height, out minBinY, out maxBinY);
        }



        override protected void _GetBins(SceneContainerQueryData iQueryData, out uint minBinX, out uint minBinY, out uint maxBinX, out uint maxBinY)
        {
            // Check type on query data object
            Assert.Fatal(iQueryData is T2DSceneContainerQueryData, "Invalid query data object passed to _GetBins");
            T2DSceneContainerQueryData query = (T2DSceneContainerQueryData)iQueryData;

            _GetBinRange(query.Rectangle.Point.X, query.Rectangle.Point.X + query.Rectangle.Width,
                out minBinX, out maxBinX);

            _GetBinRange(query.Rectangle.Point.Y, query.Rectangle.Point.Y + query.Rectangle.Height,
                out minBinY, out maxBinY);
        }

        #endregion
    }
}
