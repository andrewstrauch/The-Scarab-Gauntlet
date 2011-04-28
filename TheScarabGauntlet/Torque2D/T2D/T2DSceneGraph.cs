//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;
using GarageGames.Torque.RenderManager;



namespace GarageGames.Torque.T2D
{

    public interface ISceneObject2D : ISceneObject, ISceneContainerObject
    {
        #region Public properties, operators, constants, and enums

        int Layer
        {
            get;
        }



        float LayerDepth
        {
            get;
            set;
        }



        int LayerOrder
        {
            get;
        }



        uint LayerMask
        {
            get;
        }



        RectangleF WorldCollisionClipRectangle
        {
            get;
        }



        RectangleF WorldClipRectangle
        {
            get;
        }



        bool PickingAllowed
        {
            get;
        }



        Vector2 SortPoint
        {
            get;
        }

        #endregion

    }



    /// <summary>
    /// SceneGraph used by T2D.
    /// </summary>
    public class T2DSceneGraph : BaseSceneGraph
    {
        #region Constructors

        /// <summary>
        /// Constructs the scene graph, but does not set up the container systemh. The scene graph will be unusable until that step is completed. 
        /// This constructor is primarily for use when deserializing from XML.
        /// </summary>
        public T2DSceneGraph()
            : this(false)
        {
        }



        /// <summary>
        /// Constructs the scene graph, and optionally creates the container system. If createSceneContainer is false, the scene graph will not be 
        /// usable until a container is installed. This is primarily a legacy constructor and may be removed in a future release.
        /// </summary>
        /// <param name="createSceneContainer">Whether to create the scene container</param>
        public T2DSceneGraph(bool createSceneContainer)
        {
            if (createSceneContainer)
            {
                _container = new T2DSceneContainer(this);
                _container.Init();
            }
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The T2DLayerSortDictionary can be used to control how the objects in each layer
        /// are sorted for rendering.  See T2DLayerSortDictionary for pre-defined methods for
        /// sorting layers or provide a custom method.  Add a sort method to a layer using the
        /// Add method on the dictionary with the layer as a key and the sort method as the value.
        /// </summary>
        public T2DLayerSortDictionary LayerSortDictionary
        {
            get { return _layerSortDictionary; }
            internal set { _layerSortDictionary = value; }
        }



        public bool UseLayerSorting
        {
            get { return _useLayerSorting; }
            set { _useLayerSorting = value; }
        }



        /// <summary>
        /// This will not work correctly with T2DShape3D objects.
        /// </summary>
        public bool UseDepthBuffer
        {
            get { return _useDepthBuffer; }
            set { _useDepthBuffer = value; }
        }



        public override bool DoZPass
        {
            get { return base.DoZPass; }
            set
            {
                base.DoZPass = value;
                _useDepthBuffer = true;
            }
        }



        public override ISceneCamera Camera
        {
            set
            {
                Assert.Fatal(value is T2DSceneCamera, "A SceneCamera2D is required to render this scenegraph.");
                base.Camera = value;
                _t2dCamera = (T2DSceneCamera)value;
            }
        }



        [XmlElement(ElementName = "T2DSceneContainer")]
        public override SceneContainer Container
        {
            get { return _container; }
            set
            {
                _container = (T2DSceneContainer)value;
                _container.SceneGraph = this;
            }
        }

        #endregion


        #region Public Methods

        public override void AddObject(ISceneObject obj)
        {
            Assert.Fatal(obj is ISceneObject2D, "Only ISceneContainerObject objects can be added to this SceneGraph");
            ISceneObject2D o = (ISceneObject2D)obj;

            _container.CheckSceneObjectBins(o);
        }



        public override void RemoveObject(ISceneObject obj)
        {
            Assert.Fatal(obj is ISceneObject2D, "Only ISceneContainerObject objects can be removed from this SceneGraph");
            ISceneObject2D o = (ISceneObject2D)obj;

            _container.RemoveSceneObject(o);
        }



        public override void UpdateObject(ISceneObject obj)
        {
            Assert.Fatal(obj is ISceneObject2D, "Only ISceneContainerObject objects can be removed from this SceneGraph");
            ISceneObject2D o = (ISceneObject2D)obj;

            _container.CheckSceneObjectBins(o);
        }



        /// <summary>
        /// An accelerated version of UpdateUpdate for T2DSceneObjects.
        /// </summary>
        /// <param name="obj">T2DSceneObject to update.</param>
        public void UpdateObjectT2D(T2DSceneObject obj)
        {
            _container.CheckSceneObjectBinsT2D(obj);
        }



        /// <summary>
        /// Find objects in scene container within the given search rectangle and matching the specified object types and layers.
        /// </summary>
        /// <param name="searchRect">World rectangle to search.</param>
        /// <param name="findTypes">Object types to match.</param>
        /// <param name="layerMask">Layers to match.</param>
        /// <param name="list">List which will contain results.  Note: list is not cleared.</param>
        public void FindObjects(RectangleF searchRect, TorqueObjectType findTypes, uint layerMask, List<ISceneContainerObject> list)
        {
            // prepare container system query
            _queryData.Rectangle = searchRect;
            _queryData.ObjectTypes = findTypes;
            _queryData.LayerMask = layerMask;
            _queryData.IgnoreObject = null;
            _queryData.IgnoreObjects = null;
            _queryData.FindInvisible = false;
            _queryData.IgnorePhysics = false;
            _queryData.ResultList = list;

            // do the query
            _container.FindObjects(_queryData);
        }



        /// <summary>
        /// Find object in scene container within the given search radius and matching the specified object type and layers.
        /// </summary>
        /// <param name="pos">Center of search radius.</param>
        /// <param name="radius">Radius of search.</param>
        /// <param name="findTypes">Object types to match.</param>
        /// <param name="layerMask">Layers to match.</param>
        /// <param name="list">List which will contain results.  Note: list is not cleared.</param>
        public void FindObjects(Vector2 pos, float radius, TorqueObjectType findTypes, uint layerMask, List<ISceneContainerObject> list)
        {
            RectangleF searchRect = new RectangleF(pos.X - radius, pos.Y - radius, 2.0f * radius, 2.0f * radius);
            FindObjects(searchRect, findTypes, layerMask, list);

            for (int i = 0; i < list.Count; i++)
            {
                T2DSceneObject obj = list[i] as T2DSceneObject;
                if (obj == null)
                    continue;
                RectangleF objRect = obj.WorldCollisionClipRectangle;
                Vector2 objRad = 0.5f * objRect.Extent;
                Vector2 objPos = objRect.Point + objRad;

                // find closest x coord of object rect to pos
                if (pos.X < objPos.X - objRad.X)
                    objPos.X = objPos.X - objRad.X;
                else if (pos.X > objPos.X + objRad.X)
                    objPos.X = objPos.X + objRad.X;
                else
                    objPos.X = pos.X;

                // find closest y coord of object rect to pos
                if (pos.Y < objPos.Y - objRad.Y)
                    objPos.Y = objPos.Y - objRad.Y;
                else if (pos.Y > objPos.Y + objRad.Y)
                    objPos.Y = objPos.Y + objRad.Y;
                else
                    objPos.Y = pos.Y;

                // if object further than radius away, get rid of it
                if ((pos - objPos).LengthSquared() > radius * radius)
                {
                    list[i] = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    i--;
                }
            }
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            Assert.Fatal(_container != null, "T2DSceneGraph must have a container after load");
            // init container
            _container.Init();
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _SetupProjection(GFXDevice gfx, float aspectRatio)
        {
            // the far distance needs to be greater than the total number of objects on screen at once
            // since each object is assigned a depth value based on it's sort order in the scene
            _srs.Projection = gfx.SetOrtho(false, -0.5f * _t2dCamera.Extent.X, 0.5f * _t2dCamera.Extent.X, 0.5f * _t2dCamera.Extent.Y, -0.5f * _t2dCamera.Extent.Y, 0.1f, _camera.FarDistance);
        }



        protected override void _RenderObjects(TorqueObjectType renderMask, float aspectRatio)
        {
            // clear results from previous container query
            _containerQueryResults.Clear();

            // find objects to render
            RectangleF sceneRect = new RectangleF(_t2dCamera.SceneMin.X, _t2dCamera.SceneMin.Y, _t2dCamera.SceneMax.X - _t2dCamera.SceneMin.X, _t2dCamera.SceneMax.Y - _t2dCamera.SceneMin.Y);
            FindObjects(sceneRect, renderMask, 0xFFFFFFFF, _containerQueryResults);

            // sort by layer
            if (_useLayerSorting && _containerQueryResults.Count > 0)
            {
                _containerQueryResults.Sort(T2DLayerSortDictionary.LayerSort);

                // sort contents of each layer
                int start = 0;
                int lastLayer = (_containerQueryResults[0] as ISceneObject2D).Layer;
                for (int end = 0; end < _containerQueryResults.Count; end++)
                {
                    ISceneObject2D sceneObject = _containerQueryResults[end] as ISceneObject2D;
                    int layer = sceneObject.Layer;

                    // We reached the last object in a layer, or, the end of the
                    // list. Sort this subsection.
                    if (layer == lastLayer && end < _containerQueryResults.Count - 1)
                        continue;

                    IComparer<ISceneContainerObject> comparer = null;
                    if (!_layerSortDictionary.TryGetValue(lastLayer, out comparer) || comparer == null)
                        comparer = T2DLayerSortDictionary.DefaultSort;

                    if (comparer != T2DLayerSortDictionary.NoSort && (end - start) > 1)
                        _containerQueryResults.Sort(start, end - start, comparer);

                    start = end;
                    lastLayer = layer;
                }
            }

            if (!_useDepthBuffer)
                SceneRenderer.RenderManager.BinOverride = RenderInstance.RenderInstanceType.Mesh2D;

            // Number of objects we are about to render.
            int count = _containerQueryResults.Count;

            // This is the greatest depth value that will still be render as within
            // the camera frustum. Because the objects LayerDepth is a world
            // space value we must account for any camera offset in z.
            float maxDepth = _t2dCamera.FarDistance - _t2dCamera.Transform.Translation.Z;

            // If the FarDistance happens to be rediculously close, lets at 
            // least not crash.
            if (maxDepth <= 0.0f)
                maxDepth = 1.0f;

            // Give each object a sliver of our available depth.
            // Given enough objects it is still possible to have z-fighting            
            // but this is better than losing objects by pushing them past the FarDistance.
            float step = -maxDepth / (float)count;

            for (int i = 0; i < count; i++)
            {
                ISceneObject2D sceneObject = _containerQueryResults[i] as ISceneObject2D;
                sceneObject.LayerDepth = _useLayerSorting ? step * (float)i : -(float)sceneObject.Layer;
                _containerQueryResults[i].Render(_srs);
            }

            SceneRenderer.RenderManager.BinOverride = RenderInstance.RenderInstanceType.UndefinedType;
        }

        #endregion


        #region Private, protected, internal fields

        T2DSceneCamera _t2dCamera;

        T2DSceneContainer _container;
        T2DSceneContainerQueryData _queryData = new T2DSceneContainerQueryData();

        T2DLayerSortDictionary _layerSortDictionary = new T2DLayerSortDictionary();
        bool _useLayerSorting = true;
        bool _useDepthBuffer = false;

        #endregion
    }
}
