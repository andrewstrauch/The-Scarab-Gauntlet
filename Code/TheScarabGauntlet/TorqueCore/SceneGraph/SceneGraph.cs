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
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Lighting;
using GarageGames.Torque.Util;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Materials;
using System.ComponentModel;



namespace GarageGames.Torque.SceneGraph
{
    /// <summary>
    /// A stack of matrices. Used in the scene render state for heirarchically rendering objects.
    /// </summary>
    public class MatrixStack
    {

        #region Constructors

        public MatrixStack()
        {
            _stack = new List<Matrix>(8);
            _stack.Add(Matrix.Identity);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Gets the top matrix on the stack.
        /// </summary>
        public Matrix Top
        {
            get { return _stack[_stack.Count - 1]; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Loads identity matrix on top of the stack.
        /// </summary>
        public void LoadIdentity() { _stack[_stack.Count - 1] = Matrix.Identity; }



        /// <summary>
        /// Copies the specified matrix onto the top of the stack.
        /// </summary>
        /// <param name="mat">Matrix to load.</param>
        public void LoadMatrix(Matrix mat) { _stack[_stack.Count - 1] = mat; }



        /// <summary>
        /// Multiplies the specified matrix by the top of the stack and copies it there.
        /// Matrix is multiplied on the left of the stack top.
        /// </summary>
        /// <param name="mat">Matrix to multiply.</param>
        public void MultiplyMatrixLocal(Matrix mat) { _stack[_stack.Count - 1] = Matrix.Multiply(mat, _stack[_stack.Count - 1]); }



        /// <summary>
        /// Push another matrix onto the stack, copying the current top matrix.
        /// </summary>
        public void Push()
        {
            _stack.Add(_stack[_stack.Count - 1]);
        }



        /// <summary>
        /// Remove the top matrix, restoring the previous top value.
        /// </summary>
        public void Pop()
        {
            Assert.Fatal(_stack.Count > 0, "Popping off one too many matrices");
            _stack.RemoveAt(_stack.Count - 1);
        }

        #endregion


        #region Private, protected, internal fields

        private List<Matrix> _stack;

        #endregion
    }



    /// <summary>
    /// Structure for storing information about the current state of the scene during
    /// rendering.
    /// </summary>
    public class SceneRenderState
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// GFXDevice we are rendering with.
        /// </summary>
        public GFXDevice Gfx
        {
            get { return _gfx; }
            set { _gfx = value; }
        }



        /// <summary>
        /// Scene graph we are rendering with.
        /// </summary>
        public BaseSceneGraph SceneGraph
        {
            get { return _sg; }
            set { _sg = value; }
        }



        /// <summary>
        /// True if not actually rendering but simply pre loading materials.
        /// </summary>
        public bool IsPreloadPass
        {
            get { return _isPreload; }
            set { _isPreload = value; }
        }



        /// <summary>
        /// True if rendering zpass.
        /// </summary>
        public bool IsZPass
        {
            get { return _isZPass; }
            set { _isZPass = value; }
        }



        /// <summary>
        /// True if rendering a reflect pass.
        /// </summary>
        public bool IsReflectPass
        {
            get { return _isReflectPass; }
            set { _isReflectPass = value; }
        }



        /// <summary>
        /// Index of the current world view. WorldViewIndex corresponds to a camera or series of cameras and is used when 
        /// a material or object needs to render differently from different views. For example, the clip map uses this for 
        /// rendering from multiple views without drastic recenter operations (for split-screen). This should normally be
        /// left at it's default value of zero.
        /// </summary>
        public int WorldViewIndex
        {
            get { return _worldViewIndex; }
            set { _worldViewIndex = value; }
        }



        /// <summary>
        /// Transform of current camera.
        /// </summary>
        public Matrix CameraTransform
        {
            get { return _camTrans; }
            set { _camTrans = value; }
        }



        /// <summary>
        /// Position of current camera.
        /// </summary>
        public Vector3 CameraPosition
        {
            get { return _camTrans.Translation; }
        }



        /// <summary>
        /// Matrix stack representing object to world transfom.
        /// </summary>
        public MatrixStack World
        {
            get { return _world; }
            set { _world = value; }
        }



        /// <summary>
        /// Matrix representing view transform.
        /// </summary>
        public Matrix View
        {
            get { return _view; }
            set { _view = value; }
        }



        /// <summary>
        /// Matrix representing projection transform.
        /// </summary>
        public Matrix Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }



        /// <summary>
        /// 3D View frustum for scene.  Objects falling outside view
        /// frustum are culled from rendering.
        /// </summary>
        public Frustum Frustum
        {
            get { return _frustum; }
            set { _frustum = value; }
        }



        /// <summary>
        /// True if their are reflections in the scene.
        /// </summary>
        public bool HasReflections
        {
            get { return _hasReflections; }
            set { _hasReflections = value; }
        }

        #endregion


        #region Public methods

        public override string ToString()
        {
            return base.ToString() + ":\r\n" +
                "GFXDevice:" + this._gfx + "\r\n" +
                "SceneGraph:" + this._sg + "\r\n" +
                "CameraTransform:" + this._camTrans + " \r\n" +
                "World(Top):" + this._world.Top + "\r\n" +
                "View:" + this._view + "\r\n" +
                "Projection:" + this._projection + "\r\n" +
                "IsPreload:" + this._isPreload + "\r\n";
        }

        #endregion


        #region Private, protected, internal fields

        public Matrix _camTrans;
        internal GFXDevice _gfx;
        internal BaseSceneGraph _sg;
        internal int _worldViewIndex;
        internal MatrixStack _world = new MatrixStack();
        internal Matrix _view;
        internal Matrix _projection;
        internal bool _isPreload;
        internal bool _isZPass;
        internal bool _isStencilPass;
        internal bool _isReflectPass;
        internal Frustum _frustum;
        internal bool _hasReflections;

        #endregion

    }



    /// <summary>
    /// Interface for objects that can be added to a scenegraph. Actual scene objects
    /// use ISceneContainer, which derives from this.
    /// </summary>
    public interface ISceneObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scenegraph the scene object is in.
        /// </summary>
        BaseSceneGraph SceneGraph
        {
            get;
            set;
        }



        /// <summary>
        /// 3D transform of object.
        /// </summary>
        Matrix Transform
        {
            get;
        }



        /// <summary>
        /// True if object should be rendered.
        /// </summary>
        bool Visible
        {
            get;
        }



        /// <summary>
        /// Object type of this object.  This is used for container queries to cull out objects
        /// we are not interested in.
        /// </summary>
        TorqueObjectType ObjectType
        {
            get;
        }



        /// <summary>
        /// Returns true if the object has been disposed.
        /// </summary>
        bool IsDisposed
        {
            get;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Render this scene object.
        /// </summary>
        /// <param name="sceneRenderState">Scene render state helps determine how to render object.</param>
        void Render(SceneRenderState sceneRenderState);

        #endregion
    }



    /// <summary>
    /// Delegate for material preloading.
    /// </summary>
    /// <param name="srs">Scene render state.</param>
    public delegate void PreloadMaterialDelegate(SceneRenderState srs);



    /// <summary>
    /// Base class shared by both 2D and 3D scene graphs.
    /// </summary>
    public abstract class BaseSceneGraph : TorqueObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The camera used for rendering.  The camera determines the view of the world that is rendered,
        /// included position and field of view of the camera.
        /// </summary>
        [XmlIgnore]
        public virtual ISceneCamera Camera
        {
            set { _camera = value; }
            get { return _camera; }
        }



        /// <summary>
        /// Scene container which can be queried for objects in the scene.
        /// </summary>
        [BrowsableAttribute(false)]
        abstract public SceneContainer Container
        {
            get;
            set;
        }



        /// <summary>
        /// Delegate for preloading materials with.
        /// </summary>
        [BrowsableAttribute(false)]
        public PreloadMaterialDelegate PreloadMaterials
        {
            get { return _preloadMaterialsDelegate; }
            set { _preloadMaterialsDelegate = value; }
        }



        /// <summary>
        /// Event that is triggered the first time the scene is rendered.
        /// </summary>
        [BrowsableAttribute(false)]
        public TorqueEvent<int> FirstRenderEvent
        {
            get { return _firstRenderEvent; }
        }



        /// <summary>
        /// Whether or not to do a Z pre pass. This could speed things up in 2D in some
        /// cases, but it also doesn't work with all scene configurations. Most likely, it
        /// won't be used. In 3D, it could be beneficial if their are several large objects
        /// in the scene that occlude other objects. The terrain doesn't count as one of these
        /// large objects because the special case rendering it does already optimizes for
        /// early out depth testing.
        /// </summary>
        public virtual bool DoZPass
        {
            get { return _doZPass; }
            set { _doZPass = value; }
        }



        [BrowsableAttribute(false)]
        public SceneRenderState CurrentSceneRenderState
        {
            get { return _srs; }
        }



        /// <summary>
        /// The current frame that is being rendered.
        /// </summary>
        [XmlIgnore]
        public int CurrentFrame;



        /// <summary>
        /// The default material used for fog on all render managers using this scenegraph. 
        /// This can be overridden by setting a specific fog material on a render manager.
        /// </summary>
        [BrowsableAttribute(false)]
        public RenderMaterial FogMaterial
        {
            get { return _fogMaterial; }
            set
            {
                Assert.Fatal(value is IFogMaterial, "BaseSceneGraph.FogMaterial_set - Fog materials must implement IFogMaterial.");
                _fogMaterial = value;
            }
        }



        /// <summary>
        /// The render time of the current frame.
        /// </summary>
        [XmlIgnore]
        public float CurrentTime;



        /// <summary>
        /// The time at which the last frame was rendered.
        /// </summary>
        [XmlIgnore]
        public float LastTime;

        #endregion


        #region Public methods

        /// <summary>
        /// Add an object to the scene graph.  The object type must be compatible with the current scene
        /// graph or this call will fail.
        /// </summary>
        /// <param name="obj">Object to add.</param>
        abstract public void AddObject(ISceneObject obj);



        /// <summary>
        /// Remove an object from the scene graph.  If the scene object is not in the scene graph then
        /// no action is taken.
        /// </summary>
        /// <param name="obj">Object to remove.</param>
        abstract public void RemoveObject(ISceneObject obj);



        /// <summary>
        /// Update given object in the scene graph.  If the object is not in the scene graph then it is added
        /// (assuming it is compatible with the current scene graph).  This method must be called when an
        /// object's spatial information changes (i.e., position, size, rotation).
        /// </summary>
        /// <param name="obj">Object to update.</param>
        abstract public void UpdateObject(ISceneObject obj);



        /// <summary>
        /// Render the objects which have object types compatible with the render mask but not
        /// the noRenderMask.
        /// </summary>
        /// <param name="gfx">Graphics device with which to render</param>
        /// <param name="renderMask">Render objects of these types.</param>
        /// <param name="noRenderMask">Do not render objects of these types.</param>
        public void Render(GFXDevice gfx, TorqueObjectType renderMask, TorqueObjectType noRenderMask)
        {
#if DEBUG
            Profiler.Instance.StartBlock("SceneGraph.Render");
#endif

            SceneRenderer.RenderManager.Sort(_srs);
            SceneRenderer.RenderManager.PreRender(_srs);
            SceneRenderer.RenderManager.Render(_srs);
            SceneRenderer.RenderManager.Clear();

#if DEBUG
            Profiler.Instance.EndBlock("SceneGraph.Render");
#endif
        }



        /// <summary>
        /// Prepare to render objects which have object types compatible with the render mask but 
        /// not the noRenderMask.
        /// </summary>
        /// <param name="gfx">Graphics device with which to render</param>
        /// <param name="renderMask">Render objects of these types.</param>
        /// <param name="noRenderMask">Do not render objects of these types.</param>
        public void PreRender(GFXDevice gfx, TorqueObjectType renderMask, TorqueObjectType noRenderMask, float aspectRatio)
        {
#if DEBUG
            Profiler.Instance.StartBlock("SceneGraph.PreRender");
#endif

            Assert.Fatal(gfx != null, "SceneGraph.PreRender - Must have a GFX device in order to render");
            Assert.Fatal(_camera != null, "SceneGraph.PreRender - Must have a camera in order to render");

            UpdateFrameTime();

            // setup state
            _SetupSceneState(gfx);
            _SetupView(gfx);
            _SetupWorld(gfx);
            _SetupProjection(gfx, aspectRatio);

            if (_preloadMaterialsDelegate != null)
                _preloadMaterialsDelegate(_srs);

            _preloadMaterialsDelegate = null;

            // similar to the preload delegate, provide an event for first render
            // cafTODO: should preload delegate be moved into this?
            if (!_hasRendered)
            {
                TorqueEventManager.TriggerEvent(_firstRenderEvent, 0);
                _hasRendered = true;
            }

            // render
            _RenderObjects(renderMask - noRenderMask, aspectRatio);

#if DEBUG
            Profiler.Instance.EndBlock("SceneGraph.PreRender");
#endif
        }



        /// <summary>
        /// Called by derived classes to keep the time up to date, for use by various 
        /// shader effects.
        /// </summary>
        public void UpdateFrameTime()
        {
            CurrentFrame++;
            LastTime = CurrentTime;
            CurrentTime = ((float)Environment.TickCount) / 1000.0f;
        }



        /// <summary>
        /// Add a light to the scene. The light component calls this automatically.
        /// </summary>
        /// <param name="light">The light to add.</param>
        public void AddLight(Light light)
        {
            _lightList.Add(light);
        }



        /// <summary>
        /// Remove a light from the scene. The light component calls this automatically.
        /// </summary>
        /// <param name="light">The light to remove.</param>
        public void RemoveLight(Light light)
        {
            _lightList.Remove(light);
        }



        /// <summary>
        /// Gets the list of lights in the scene that would have the most affect on an object
        /// inside of a box, up to a maximum number.
        /// </summary>
        /// <param name="worldBox">The box to get lights for.</param>
        /// <param name="maxLights">The maximum number of lights to return.</param>
        /// <returns></returns>
        public List<Light> GetLights(Box3F worldBox, int maxLights)
        {
            _objectLights.Clear();

            _lightComparer.WorldBox = worldBox;
            _lightList.Sort(_lightComparer);

            int count = _lightList.Count;
            for (int i = 0; i < count && i < maxLights; i++)
            {
                Light light = _lightList[i];

                if (light.Owner != null)
                    _objectLights.Add(light);
            }

            return _objectLights;
        }

        #endregion


        #region Private, protected, internal methods

        abstract protected void _SetupProjection(GFXDevice gfx, float aspectRatio);

        protected virtual void _SetupView(GFXDevice gfx)
        {
            _srs.CameraTransform = _camera.Transform;
            _srs._worldViewIndex = _camera.WorldViewIndex;
            _srs.View = Matrix.Invert(_camera.Transform);
        }



        protected virtual void _SetupWorld(GFXDevice gfx)
        {
            _srs.World.LoadIdentity();
        }



        protected virtual void _SetupSceneState(GFXDevice gfx)
        {
            _srs.Gfx = gfx;
            _srs.SceneGraph = this;
        }



        abstract protected void _RenderObjects(TorqueObjectType renderMask, float aspectRatio);

        #endregion


        #region Private, protected, internal fields

        TorqueEvent<int> _firstRenderEvent = new TorqueEvent<int>("firstRender", false);
        bool _hasRendered;

        PreloadMaterialDelegate _preloadMaterialsDelegate;

        List<Light> _lightList = new List<Light>();
        List<Light> _objectLights = new List<Light>();
        LightComparer _lightComparer = new LightComparer();

        RenderMaterial _fogMaterial = null;

        protected SceneRenderState _srs = new SceneRenderState();
        protected ISceneCamera _camera;

        private bool _doZPass;

        protected List<ISceneContainerObject> _containerQueryResults = new List<ISceneContainerObject>();

        #endregion
    }
}

