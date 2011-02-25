//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{

    /// <summary>
    /// Collection of render instances used to pool derived render instance types when using AllocateInstance&lt;T&gt;.
    /// </summary>
    public class RenderInstanceCollection
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The list of allocated render instances.
        /// </summary>
        public List<RenderInstance> Instances = new List<RenderInstance>();



        /// <summary>
        /// The number of render instances to allocate initially.
        /// </summary>
        public int NumInitialRenderInstances = 2;



        /// <summary>
        /// The index of the next available render instance.
        /// </summary>
        public int NextAllocInstance = 0;



        /// <summary>
        /// The index of the last render instance that is reset.
        /// </summary>
        public int LastResetInstance = 0;

        #endregion
    }



    /// <summary>
    /// Manages rendering of all render instances. Mostly this just holds all the render managers and
    /// dispatches method calls to each of them. It also hands-off render instances to the manager that
    /// is responsible for it. The mapping of type to manager is:
    /// 
    /// Sky - SkyRenderManager
    /// Mesh2D - T2DRenderManager
    /// Terrain - TerrainRenderManager
    /// Mesh3D - T3DRenderManager
    /// Shadow - ShadowRenderManager
    /// Translucent - TranslucentRenderManager
    /// Refraction - RefractionRenderManager
    /// 
    /// The above list is also sorted in the order at which the managers are rendered. The list is passed
    /// through once to render opaque instances, then a second time to render translucent instances with
    /// the proper render states set up for each. See BaseRenderManager for more info.
    /// 
    /// There is also a ReflectionRenderManager which handles updating the reflections on materials. This
    /// is not specific to any instance type, though. Any instance that has a material with IsReflective
    /// set will be added to both it's normal render manager and the reflection manager. The reflections
    /// are updated first, so by the time the instance is rendered, the texture associated with the
    /// reflection will be up to date.
    /// 
    /// The process for setting up vertex data to actually be rendered is as follows:
    /// 
    /// Allocate a render instance with one of the following methods
    ///    - SceneRenderer.RenderManager.AllocateInstance
    ///    - SceneRenderer.RenderManager.CreateInstance (automatically calls FillRenderInstance so skip the next step)
    /// 
    /// Fill the render instance with one of the following methods
    ///    - SceneRenderer.RenderManager.FillInstance
    ///    - Manually by setting the public fields on the RenderInstance object
    /// 
    /// Add the render instance to the scene renderer
    ///    - SceneRenderer.RenderManager.AddInstance
    /// 
    /// FreeInstance is called automatically on all instances that were added to the scene renderer. However,
    /// if an instance was allocated but not added, it is necessary to call FreeInstance.
    /// </summary>
    public class SceneRenderer
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Returns the global render manager singleton.
        /// </summary>
        public static SceneRenderer RenderManager
        {
            get
            {
                if (_sceneRenderer == null)
                {
                    _sceneRenderer = new SceneRenderer();
                    _sceneRenderer._Initialize();
                }

                return _sceneRenderer;
            }
        }

        private static SceneRenderer _sceneRenderer;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The number of render instances to allocate initially.
        /// </summary>
        public int NumInitialRenderInstances
        {
            get { return _numInitialRenderInstances; }
            set { _numInitialRenderInstances = value; }
        }



        /// <summary>
        /// Set this to force all future render instances into a specific bin. To disable the
        /// bin override, set it to instance type Undefined.
        /// </summary>
        public RenderInstance.RenderInstanceType BinOverride
        {
            get { return _binOverride; }
            set { _binOverride = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Allocate a render instance of the type specified for one time use.  Render instances created 
        /// using AllocateInstance will automatically be recycled at the end of the frame.  If you need to 
        /// hold onto the render instance, use 'new RenderInstance'.
        /// </summary>
        /// <typeparam name="T">The type of the render instance to create.</typeparam>
        /// <returns>A RenderInstance of the type specified.</returns>
        public T AllocateInstance<T>() where T : RenderInstance, new()
        {
            if (typeof(T).Equals(typeof(RenderInstance)))
                return AllocateInstance() as T;

            if (_riDictionary == null)
                _riDictionary = new Dictionary<Type, RenderInstanceCollection>();

            if (!_riDictionary.ContainsKey(typeof(T)))
                _riDictionary.Add(typeof(T), new RenderInstanceCollection());

            RenderInstanceCollection riCollection = _riDictionary[typeof(T)];
            List<RenderInstance> thisList = riCollection.Instances;

            if (thisList.Count <= riCollection.NextAllocInstance)
            {
                // allocate at least initial amount, but increase by 50%
                int size = Math.Max(riCollection.NumInitialRenderInstances, thisList.Count + (thisList.Count >> 1));
                thisList.Capacity = size;
                while (thisList.Count < size)
                    thisList.Add(new T());
            }

            RenderInstance ret = thisList[riCollection.NextAllocInstance];
            if (riCollection.NextAllocInstance < riCollection.LastResetInstance)
                ret.Reset();

            riCollection.NextAllocInstance++;

            // make sure we aren't getting unreset instance
            Assert.Fatal(ret.IsReset, "doh");
            ret.IsReset = false;

            return ret as T;
        }



        /// <summary>
        /// Allocate a render instance for one time use.  Render instances created using AllocateInstance
        /// will automatically be recycled at the end of the frame.  If you need to hold onto the render
        /// instance, use 'new RenderInstance'.
        /// </summary>
        /// <returns>RenderInstance</returns>
        public RenderInstance AllocateInstance()
        {
            if (_shortList.Count != 0)
            {
                // someone returned one recently, use that one
                RenderInstance reuse = _shortList[_shortList.Count - 1];
                _shortList.RemoveAt(_shortList.Count - 1);
                reuse.Reset();
                Assert.Fatal(reuse.IsReset, "AllocateInstance() Error");
                reuse.IsReset = false;
                return reuse;
            }

            if (_renderInstances.Count <= _nextAllocInstance)
            {
                // allocate at least initial amount, but increase by 50%
                int size = Math.Max(_numInitialRenderInstances, _renderInstances.Count + (_renderInstances.Count >> 1));
                _renderInstances.Capacity = size;

                while (_renderInstances.Count < size)
                    _renderInstances.Add(new RenderInstance());
            }

            RenderInstance ret = _renderInstances[_nextAllocInstance];
            if (_nextAllocInstance < _lastResetInstance)
                ret.Reset();
            _nextAllocInstance++;

            // make sure we aren't getting unreset instance
            Assert.Fatal(ret.IsReset, "AllocateInstance() Error");
            ret.IsReset = false;

            return ret;
        }



        /// <summary>
        /// Return an allocated render instance which was unused this frame.  Only required if allocated but not
        /// actually used.
        /// </summary>
        /// <param name="inst">Unused render instance.</param>
        public void FreeInstance(RenderInstance inst)
        {
            _shortList.Add(inst);
        }



        /// <summary>
        /// Create a render instance and fill it with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        /// <returns>The new render instance.</returns>
        public RenderInstance CreateInstance(DynamicVertexBuffer VertexBuffer, DynamicIndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                             int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex,
                                             int PrimitiveCount, float Opacity, RenderMaterial Material, MaterialInstanceData MaterialInstanceData,
                                             TextureAddressMode TexU, TextureAddressMode TexV, Matrix ObjectTransform, Box3F WorldBox,
                                             RenderInstance.RenderInstanceType Type)
        {
            RenderInstance rInst = AllocateInstance();

            FillInstance(rInst, VertexBuffer, IndexBuffer, VertexDeclaration, VertexSize, PrimitiveType, BaseVertex, NumVerts, StartIndex,
                         PrimitiveCount, Material, MaterialInstanceData, TexU, TexV, ref ObjectTransform, ref WorldBox, Type);

            AddInstance(rInst);

            return rInst;
        }



        /// <summary>
        /// Create a render instance and fill it with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        /// <returns>The new render instance.</returns>
        public RenderInstance CreateInstance(VertexBuffer VertexBuffer, IndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                             int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex,
                                             int PrimitiveCount, float Opacity, RenderMaterial Material, MaterialInstanceData MaterialInstanceData,
                                             TextureAddressMode TexU, TextureAddressMode TexV, Matrix ObjectTransform, Box3F WorldBox,
                                             RenderInstance.RenderInstanceType Type)
        {
            RenderInstance rInst = AllocateInstance();

            FillInstance(rInst, VertexBuffer, IndexBuffer, VertexDeclaration, VertexSize, PrimitiveType, BaseVertex, NumVerts, StartIndex,
                         PrimitiveCount, Material, MaterialInstanceData, TexU, TexV, ref ObjectTransform, ref WorldBox, Type);

            AddInstance(rInst);

            return rInst;
        }



        /// <summary>
        /// Create a render instance of a specific type and fill it with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        /// <param name="Type">The type of render instance to create.</param>
        /// <returns>The new render instance.</returns>
        public T CreateInstance<T>(DynamicVertexBuffer VertexBuffer, DynamicIndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                   int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex,
                                   int PrimitiveCount, float Opacity, RenderMaterial Material, MaterialInstanceData MaterialInstanceData,
                                   TextureAddressMode TexU, TextureAddressMode TexV, ref Matrix ObjectTransform, ref Box3F WorldBox,
                                   RenderInstance.RenderInstanceType Type) where T : RenderInstance, new()
        {
            RenderInstance rInst = AllocateInstance<T>() as RenderInstance;

            FillInstance(rInst, VertexBuffer, IndexBuffer, VertexDeclaration, VertexSize, PrimitiveType, BaseVertex, NumVerts, StartIndex,
                         PrimitiveCount, Material, MaterialInstanceData, TexU, TexV, ref ObjectTransform, ref WorldBox, Type);

            AddInstance(rInst);

            return rInst as T;
        }



        /// <summary>
        /// Create a render instance of a specific type and fill it with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        /// <param name="Type">The type of render instance to create.</param>
        /// <returns>The new render instance.</returns>
        public T CreateInstance<T>(VertexBuffer VertexBuffer, IndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                   int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex,
                                   int PrimitiveCount, float Opacity, RenderMaterial Material, MaterialInstanceData MaterialInstanceData,
                                   TextureAddressMode TexU, TextureAddressMode TexV, ref Matrix ObjectTransform, ref Box3F WorldBox,
                                   RenderInstance.RenderInstanceType Type) where T : RenderInstance, new()
        {
            RenderInstance rInst = AllocateInstance<T>() as RenderInstance;

            FillInstance(rInst, VertexBuffer, IndexBuffer, VertexDeclaration, VertexSize, PrimitiveType, BaseVertex, NumVerts, StartIndex,
                         PrimitiveCount, Material, MaterialInstanceData, TexU, TexV, ref ObjectTransform, ref WorldBox, Type);

            AddInstance(rInst);

            return rInst as T;
        }



        /// <summary>
        /// Fill a render instance with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        public void FillInstance(RenderInstance rInst, DynamicVertexBuffer VertexBuffer, DynamicIndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                 int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex, int PrimitiveCount,
                                 RenderMaterial Material, MaterialInstanceData MaterialInstanceData, TextureAddressMode TexU,
                                 TextureAddressMode TexV, ref Matrix ObjectTransform, ref Box3F WorldBox, RenderInstance.RenderInstanceType Type)
        {
            rInst.Type = Type;

            // geometry
            rInst.PrimitiveType = PrimitiveType;

            // vertices
            rInst.VertexSize = VertexSize;
            rInst.VertexDeclaration = VertexDeclaration;
            rInst.VertexBuffer = VertexBuffer;
            rInst.BaseVertex = BaseVertex;
            rInst.VertexCount = NumVerts;

            // indices
            rInst.IndexBuffer = IndexBuffer;
            rInst.StartIndex = StartIndex;
            rInst.PrimitiveCount = PrimitiveCount;

            // transform
            rInst.ObjectTransform = ObjectTransform;
            rInst.WorldBox = WorldBox;

            // material
            rInst.UTextureAddressMode = TexU;
            rInst.VTextureAddressMode = TexV;
            rInst.Material = Material;
            rInst.MaterialInstanceData = MaterialInstanceData;
        }



        /// <summary>
        /// Fill a render instance with the specified data. See RenderInstance for what each parameter does.
        /// </summary>
        public void FillInstance(RenderInstance rInst, VertexBuffer VertexBuffer, IndexBuffer IndexBuffer, VertexDeclaration VertexDeclaration,
                                 int VertexSize, PrimitiveType PrimitiveType, int BaseVertex, int NumVerts, int StartIndex, int PrimitiveCount,
                                 RenderMaterial Material, MaterialInstanceData MaterialInstanceData, TextureAddressMode TexU,
                                 TextureAddressMode TexV, ref Matrix ObjectTransform, ref Box3F WorldBox, RenderInstance.RenderInstanceType Type)
        {
            rInst.Type = Type;

            // geometry
            rInst.PrimitiveType = PrimitiveType;

            // vertices
            rInst.VertexSize = VertexSize;
            rInst.VertexDeclaration = VertexDeclaration;
            rInst.VertexBuffer = VertexBuffer;
            rInst.BaseVertex = BaseVertex;
            rInst.VertexCount = NumVerts;

            // indices
            rInst.IndexBuffer = IndexBuffer;
            rInst.StartIndex = StartIndex;
            rInst.PrimitiveCount = PrimitiveCount;

            // transform
            rInst.ObjectTransform = ObjectTransform;
            rInst.WorldBox = WorldBox;

            // material
            rInst.UTextureAddressMode = TexU;
            rInst.VTextureAddressMode = TexV;
            rInst.Material = Material;
            rInst.MaterialInstanceData = MaterialInstanceData;
        }



        /// <summary>
        /// Add an instance to be rendered. This automatically filters it to the correct render manager.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        public void AddInstance(RenderInstance instance)
        {
            // validate instance
            Assert.Fatal(instance.VertexBuffer == null || instance.VertexDeclaration != null, "SceneRenderer.AddInstance - Vertex buffer and declaration is required for render instances!");
            Assert.Fatal(instance.Type != RenderInstance.RenderInstanceType.UndefinedType, "SceneRenderer.AddInstance - Undefined render instance type!");
            Assert.Fatal(instance.Material != null, "SceneRenderer.AddInstance - RenderInstace submitted with no material!");

            // For release builds... don't render if we don't
            // have a material instance.
            if (instance.Material == null)
                return;

            // Materials that reflect are added to the reflection manager so the material can be updated. But,
            // the instance is still added to the manager it was supposed to be in so the object renders.
            if (instance.Material.IsReflective)
                _reflectionManager.AddElement(instance);

            // Refraction types are determined by the material so any object can have refraction without
            // having to check for it every time a render instance is created.
            else if (instance.Material.IsRefractive)
            {
                // refractions don't respect bin override
                _renderBins[(int)RenderInstance.RenderInstanceType.Refraction].AddElement(instance);
                return;
            }

            // Same deal as refraction, but additionally we need to set the sort point.
            else if (instance.Material.IsTranslucent)
            {
                if (_renderBins[(int)RenderInstance.RenderInstanceType.Translucent2D] != null)
                    instance.Type = RenderInstance.RenderInstanceType.Translucent2D;

                if (_renderBins[(int)RenderInstance.RenderInstanceType.Translucent3D] != null)
                    instance.Type = RenderInstance.RenderInstanceType.Translucent3D;

                if (!instance.IsSortPointSet)
                {
                    instance.SortPoint = instance.ObjectTransform.Translation;
                    instance.IsSortPointSet = true;
                }
            }

            int binType = _binOverride == RenderInstance.RenderInstanceType.UndefinedType ? (int)instance.Type : (int)_binOverride;

            if (_renderBins[binType] != null)
                _renderBins[binType].AddElement(instance);
        }



        /// <summary>
        /// Addd an instance to the specified render manager.
        /// </summary>
        /// <param name="instance">The render instance.</param>
        /// <param name="binType">The instance type of the manager to add it to.</param>
        public void AddInstance(RenderInstance instance, RenderInstance.RenderInstanceType binType)
        {
            _renderBins[(int)binType].AddElement(instance);
        }



        /// <summary>
        /// Sort all of the render managers. This just passes execution onto each of the render managers sort methods.
        /// </summary>
        /// <param name="srs">The current scene render state.</param>
        public void Sort(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_sortProfileBlock);
#endif

            for (int i = 0; i < _renderBins.Count; i++)
            {
                if (_renderBins[i] != null)
                    _renderBins[i].Sort(srs);
            }

#if DEBUG
            Profiler.Instance.EndBlock(_sortProfileBlock);
#endif
        }



        /// <summary>
        /// Remove all render instances from all render managers.
        /// </summary>
        public void Clear()
        {
            _reflectionManager.Clear();

            for (int i = 0; i < _renderBins.Count; i++)
            {
                if (_renderBins[i] != null)
                    _renderBins[i].Clear();
            }

            _FreeInstances();
        }



        /// <summary>
        /// Does any necessary setup before actual rendering.
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public void PreRender(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_preRenderProfileBlock);
#endif
            _reflectionManager.Update(srs);

#if DEBUG
            Profiler.Instance.EndBlock(_preRenderProfileBlock);
#endif
        }



        /// <summary>
        /// Renders all the render instances via their respective render managers.
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public void Render(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_renderProfileBlock);
#endif

            GFXDevice.Instance.FlushAndLockVolatileBuffers();

            Assert.Fatal(srs.Gfx.Device != null, "BaseRenderManager.Render - Invalid D3D device!");

            _SetupCommonRenderStates(srs);

            if (srs.SceneGraph.DoZPass)
            {
                _SetupZPassRenderStates(srs);

                for (int i = 0; i < _renderBins.Count; i++)
                {
                    if (_renderBins[i] != null)
                        _renderBins[i].RenderZPass(srs);
                }
            }

            _SetupOpaquePassRenderStates(srs);

            for (int i = 0; i < _renderBins.Count; i++)
            {
                if (_renderBins[i] != null)
                    _renderBins[i].RenderOpaquePass(srs);
            }

            _SetupTranslucentPassRenderStates(srs);

            for (int i = 0; i < _renderBins.Count; i++)
            {
                if (_renderBins[i] != null)
                    _renderBins[i].RenderTranslucentPass(srs);
            }

            _CleanupRenderStates(srs);

            GFXDevice.Instance.UnlockVolatileBuffers();

#if DEBUG
            Profiler.Instance.EndBlock(_renderProfileBlock);
#endif
        }



        /// <summary>
        /// Renders a quad at the specified position and size on the screen. This is used in post processing.
        /// </summary>
        /// <param name="_material">The material to render with.</param>
        /// <param name="position">The screen position to render to.</param>
        /// <param name="size">The size in pixels to render.</param>
        public void RenderQuad(RenderMaterial _material, Vector2 position, Vector2 size)
        {
            _quadRenderState.Gfx = GFXDevice.Instance;
            _quadRenderState.Gfx.Device.RenderState.CullMode = CullMode.None;

            _quadViewport.X = (int)position.X;
            _quadViewport.Y = (int)position.Y;
            _quadViewport.Width = (int)size.X;
            _quadViewport.Height = (int)size.Y;

            oldViewport = GFXDevice.Instance.Device.Viewport;
            GFXDevice.Instance.Device.Viewport = _quadViewport;

            _renderQuad.CreateAndFillVB();
            _renderQuad.FillRenderInstance(_quadRenderInstance, 1.0f, _quadRenderState);

            GFXDevice.Instance.Device.VertexDeclaration = _quadRenderInstance.VertexDeclaration;

            GFXDevice.Instance.Device.Vertices[0].SetSource(_quadRenderInstance.VertexBuffer, 0, _quadRenderInstance.VertexSize);

            _material.SetupEffect(_quadRenderState, null);
            _material.SetupObject(_quadRenderInstance, _quadRenderState);

            while (_material.SetupPass())
                GFXDevice.Instance.Device.DrawPrimitives(_quadRenderInstance.PrimitiveType, _quadRenderInstance.StartIndex, _quadRenderInstance.PrimitiveCount);

            _material.CleanupEffect();

            GFXDevice.Instance.Device.Viewport = oldViewport;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Clear out all the render instances allocated for this frame, reset them as needed.
        /// </summary>
        private void _FreeInstances()
        {
            // Everything allocated this frame will be marked as non-reset (_lastResetInstance=_nextAllocInstance).
            // Everything above that will be assumed reset, so reset everything between _nextAllocInstance and _lastResetInstance.
            // This makes sure we don't leave a bunch of dangling pointers that could otherwise be gc, but doesn't force us to
            // walk the list each time unless the number of rendered items changes dramatically frame to frame.
            while (_nextAllocInstance < _lastResetInstance)
                _renderInstances[--_lastResetInstance].Reset();

            _shortList.Clear();
            _lastResetInstance = _nextAllocInstance;
            _nextAllocInstance = 0;

            if (_riDictionary != null)
            {
                foreach (RenderInstanceCollection riCollection in _riDictionary.Values)
                {
                    while (riCollection.NextAllocInstance < riCollection.LastResetInstance)
                        riCollection.Instances[--riCollection.LastResetInstance].Reset();

                    riCollection.LastResetInstance = riCollection.NextAllocInstance;
                    riCollection.NextAllocInstance = 0;
                }
            }
        }



        private void _Initialize()
        {
            Assert.Fatal(!_isInitialized, "Render Manager already initialized!");

            _reflectionManager = new ReflectionManager();

            _renderBins = new List<BaseRenderManager>();
            _CreateRenderBins();

            _renderQuad = new RenderQuad();
            _renderQuad.SetupUVs(0.0f, 0.0f, 1.0f, 1.0f, false, true);
            _renderQuad.CreateAndFillVB();

            _quadRenderState.World.LoadMatrix(Matrix.Identity);
            _quadRenderState.View = Matrix.Identity;
            _quadRenderState.Projection = Matrix.Identity;

            _quadRenderInstance.Opacity = 1.0f;

            _isInitialized = true;
        }



        private void _CreateRenderBins()
        {
        }



        public void AddRenderBinToSceneRenderer(object NewRenderManager)
        {
            _renderBins.Add((BaseRenderManager)NewRenderManager);
        }



        private void _SetupCommonRenderStates(SceneRenderState srs)
        {
            if (srs.IsReflectPass)
                srs.Gfx.Device.RenderState.CullMode = CullMode.CullClockwiseFace;
            else
                srs.Gfx.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            srs.Gfx.Device.RenderState.AlphaBlendEnable = false;
            srs.Gfx.Device.RenderState.AlphaFunction = CompareFunction.GreaterEqual;

            srs.Gfx.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            srs.Gfx.Device.RenderState.DepthBias = 0.0f;
        }



        private void _SetupZPassRenderStates(SceneRenderState srs)
        {
            srs.Gfx.Device.RenderState.ColorWriteChannels = ColorWriteChannels.None;

            srs.Gfx.Device.RenderState.AlphaTestEnable = true;
            srs.Gfx.Device.RenderState.ReferenceAlpha = 250;

            srs.Gfx.Device.RenderState.DepthBufferEnable = true;
            srs.Gfx.Device.RenderState.DepthBufferWriteEnable = true;
        }



        private void _SetupOpaquePassRenderStates(SceneRenderState srs)
        {
            srs.Gfx.Device.RenderState.ColorWriteChannels = ColorWriteChannels.All;

            srs.Gfx.Device.RenderState.AlphaTestEnable = false;

            // The depth buffer is disabled since the sky is the first thing rendered. The sky render
            // manager turns it on when it is done.
            srs.Gfx.Device.RenderState.DepthBufferEnable = false;
            srs.Gfx.Device.RenderState.DepthBufferWriteEnable = false;
        }



        private void _SetupTranslucentPassRenderStates(SceneRenderState srs)
        {
            srs.Gfx.Device.RenderState.AlphaBlendEnable = true;
            srs.Gfx.Device.RenderState.DepthBufferEnable = true;
            srs.Gfx.Device.RenderState.DepthBufferWriteEnable = false;
        }



        private void _CleanupRenderStates(SceneRenderState srs)
        {
            srs.Gfx.Device.RenderState.DepthBufferEnable = false;
            srs.Gfx.Device.RenderState.CullMode = CullMode.None;
        }

        #endregion


        #region Private, protected, internal fields

        Viewport oldViewport;

        int _numInitialRenderInstances = 256;
        int _nextAllocInstance = 0;
        int _lastResetInstance = 0;
        List<RenderInstance> _renderInstances = new List<RenderInstance>();
        List<RenderInstance> _shortList = new List<RenderInstance>();
        RenderInstance.RenderInstanceType _binOverride = RenderInstance.RenderInstanceType.UndefinedType;
        Dictionary<Type, RenderInstanceCollection> _riDictionary;

        List<BaseRenderManager> _renderBins;
        bool _isInitialized;
        ReflectionManager _reflectionManager;

        Viewport _quadViewport = new Viewport();
        SceneRenderState _quadRenderState = new SceneRenderState();
        RenderInstance _quadRenderInstance = new RenderInstance();
        RenderQuad _renderQuad;

#if DEBUG
        ProfilerCodeBlock _sortProfileBlock = new ProfilerCodeBlock("SceneRenderer.Sort");
        ProfilerCodeBlock _preRenderProfileBlock = new ProfilerCodeBlock("SceneRenderer.PreRender");
        ProfilerCodeBlock _renderProfileBlock = new ProfilerCodeBlock("SceneRenderer.Render");
#endif

        #endregion
    }
}
