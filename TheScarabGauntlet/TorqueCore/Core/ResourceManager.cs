//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework.Media;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Base class for all resources
    /// </summary>
    public abstract class BaseResource
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Get the associated profile for this resource.
        /// </summary>
        public BaseResourceProfile ResourceProfile
        {
            get { return _profile; }
        }



        /// <summary>
        /// Get whether or not this resource is invalid. An invalid resource should 
        /// be discarded, and if necessary, another loaded in its place. A resource
        /// may become invalid because of a graphics device reset.
        /// </summary>
        public bool IsInvalid
        {
            get { return _isInvalid; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Invalidates this resources. You should invalidate a resource when it
        /// is no longer needed. An invalidated resource is not necessarily
        /// disposed of right away.
        /// </summary>
        public virtual void Invalidate() { _isInvalid = true; }

        #endregion


        #region Private, protected, internal fields

        protected BaseResourceProfile _profile;
        protected bool _isInvalid;

        #endregion
    }



    /// <summary>
    /// Internal (to the resource manager) resource class.  Instances of this class are wrapped in a 
    /// Resource struct before being returned to the engine or game code.  The struct makes the resource
    /// easier to use for the calling code (don't need to check for both null resources and null objects within
    /// resources).  
    /// </summary>
    /// <typeparam name="T">Type of resource object.</typeparam>
    public class InternalResource<T> : BaseResource, IDisposable
    {
        #region Constructors

        public InternalResource(T obj, BaseResourceProfile profile)
            : base()
        {
            _obj = obj;
            _profile = profile;
        }



        public InternalResource(T obj)
            : base()
        {
            _obj = obj;
            _profile = null;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns whether the object instance that is wrapped inside this
        /// resource is a null reference.
        /// </summary>
        public bool IsNull
        {
            get { return _obj == null; }
        }



        /// <summary>
        /// Get the object instance that is wrapped inside the resource.
        /// </summary>
        public T Instance
        {
            get { return _obj; }
        }

        #endregion


        #region Public methods

        public override void Invalidate()
        {
            base.Invalidate();

            if (_profile.DisposeOnInvalidate)
                Dispose();

            _obj = default(T);

            // tell resource manager we are dead
            ResourceManager.Instance.OnResourceInvalidated(this);
        }



        /// <summary>
        /// Disposes this resource.
        /// </summary>
        public virtual void Dispose()
        {
            // we duplicate some Invalidate logic here in case someone calls Dispose without calling Invalidate.
            // can't call our Invalidate because we might get into a cycle if it calls us.
            base.Invalidate();

            if (_obj is IDisposable)
                (_obj as IDisposable).Dispose();

            _obj = default(T);

            // tell resource manager we are dead
            ResourceManager.Instance.OnResourceInvalidated(this);
        }

        #endregion


        #region Private, protected, internal fields

        protected T _obj;

        #endregion
    }



    /// <summary>
    /// Public wrapper object for ResourceInstances. 
    /// </summary>
    /// <typeparam name="T">Type of resource.</typeparam>
    public struct Resource<T>
    {
        #region Constructors

        /// <summary>
        /// Create a resource.
        /// </summary>
        /// <param name="resource">The internal resource for this resource.</param>
        public Resource(InternalResource<T> resource)
        {
            _resource = resource;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns true if this resource is null.
        /// </summary>
        public bool IsNull
        {
            get { return _resource == null || _resource.IsNull; }
        }



        /// <summary>
        /// Returns true if this resource has been invalidated.
        /// </summary>
        public bool IsInvalid
        {
            get { return _resource != null && _resource.IsInvalid; }
        }



        /// <summary>
        /// Returns the resource object.
        /// </summary>
        public T Instance
        {
            get
            {
                if (_resource != null)
                    return _resource.Instance;
                else
                    return default(T);
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Force this resource to be invalidated.  Ok to call this if resource is null.
        /// </summary>
        public void Invalidate()
        {
            if (!IsNull)
                _resource.Invalidate();
        }

        #endregion


        #region Private, protected, internal fields

        internal InternalResource<T> _resource;

        #endregion
    }



    /// <summary>
    /// The resource manager. Intended to be a "one-stop shop" for creating or loading any type of resource that 
    /// the engine or application might need.  I.e, a "vending machine".
    /// </summary>
    public class ResourceManager
    {
        #region Static methods, fields, constructors


        /// <summary>
        /// Get the resource manager singleton instance.  Will not be null.
        /// </summary>
        public static ResourceManager Instance
        {
            get { return _instance; }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Constructs the resource manager.
        /// </summary>
        public ResourceManager()
        {
#if XBOX
#if DEBUG
            _systemResourceManager = GarageGames.Torque.EngineData.EngineData_Xbox_Debug.ResourceManager;
#else
            _systemResourceManager = GarageGames.Torque.EngineData.EngineData_Xbox.ResourceManager;
#endif
#else
#if DEBUG
            _systemResourceManager = GarageGames.Torque.EngineData.EngineData_x86_Debug.ResourceManager;
#else
            _systemResourceManager = GarageGames.Torque.EngineData.EngineData_x86.ResourceManager;
#endif
#endif
            _systemResourceManager.IgnoreCase = true;

            _resourceContentManager = CreateResourceContentManager(_systemResourceManager);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns the content manager at the top of the content manager stack.
        /// </summary>
        public ContentManager CurrentContentManager
        {
            get
            {
                if (_contentManagerStack.Count > 0)
                    return _contentManagerStack.Peek();

                return _globalContentManager;
            }
        }



        /// <summary>
        /// Returns the Global Content Manager. It's not suggested to use the set method of this property.
        /// </summary>
        public ContentManager GlobalContentManager
        {
            get { return _globalContentManager; }
            set { _globalContentManager = value; } // not recommend that you call this

        }

        #endregion


        #region Public methods

        /// <summary>
        /// Push a content manager onto the stack. Does not check for duplicates.
        /// </summary>
        /// <param name="cm">The content manager to push onto the stack.</param>
        public void PushContentManager(ContentManager cm)
        {
            Assert.Fatal(cm != null, "ResourceManager.PushContentManager - Error: pushing null content manager.");

            if (cm == null)
                return;

            _contentManagerStack.Push(cm); // we don't care if caller pushes a duplicate
        }



        /// <summary>
        /// Pop a Content Manager from the stack.
        /// </summary>
        /// <returns>The content manager popped from the stack or null if none.</returns>
        public ContentManager PopContentManager()
        {
            Assert.Fatal(_contentManagerStack.Count > 0, "ResourceManager.PopContentManager - Error: content manager stack underflow.");

            if (_contentManagerStack.Count > 0)
                return _contentManagerStack.Pop();
            else
                return null;
        }



        /// <summary>
        /// Called by the GFX device when a reset or lost device state happens. This function will null 
        /// any resources that are using d3d default pool objects. It is the application's responsibility 
        /// to check for null and recreate these resources if necessary.  
        /// </summary>
        /// <param name="deviceDisposing">Specifies whether the device is disposing or just resetting.</param>
        public void OnDeviceResetting(bool deviceDisposing)
        {
            // if the device is not disposing, then it is just resetting.
            if (!deviceDisposing)
            {
                // walk copy of list because we will be removing items...
                for (int index = 0; index < _resources.Count; index++)
                {
                    if (_resources[index].ResourceProfile.InvalidateOnDeviceReset)
                        _resources[index].Invalidate();
                }

                // all done
                return;
            }

            // device is disposing.
            // walk copy of list because we will be removing items...

            BaseResourceProfile profile;
            for (int index = 0; index < _resources.Count; index++)
            {
                profile = _resources[index].ResourceProfile;

                if (profile.InvalidateOnDeviceDispose)
                    _resources[index].Invalidate();
            }

            // tell all of the content managers to unload their stuff
            _resourceContentManager.Instance.Unload();

            for (int index = 0; index < _contentManagerStack.Count; index++)
                _contentManagerStack.Pop().Unload();

            _globalContentManager.Unload();
        }



        /// <summary>
        /// Called when a resource is invalidated.  The ResourceManager then removes the resource from its list
        /// of internal resources.
        /// </summary>
        /// <param name="resource">The resource that was invalidated.</param>
        public void OnResourceInvalidated(BaseResource resource)
        {
            // remove the invalidated resource
            _resources.Remove(resource);
        }



        /// <summary>
        /// Create a ResourceContentManager resource.
        /// </summary>
        /// <param name="systemResourceManager">The resource manager to use when creating the ResourceContentManager resource.</param>
        /// <returns>The new resource.</returns>
        public Resource<ResourceContentManager> CreateResourceContentManager(System.Resources.ResourceManager systemResourceManager)
        {
            ResourceContentManager rcm = new Microsoft.Xna.Framework.Content.ResourceContentManager(TorqueEngineComponent.Instance.Game.Services, _systemResourceManager);
            InternalResource<ResourceContentManager> res = new InternalResource<ResourceContentManager>(rcm, ResourceProfiles.AutomaticGenericProfile);
            _resources.Add(res);
            return new Resource<ResourceContentManager>(res);
        }



        /// <summary>
        /// Create a Vertex Buffer resource, using the specified profile and size.
        /// </summary>
        /// <param name="profile">The vertex buffer profile of the vertex buffer.</param>
        /// <param name="sizeInBytes">The size in bytes of the vertex buffer.</param>
        /// <returns>A new vertex buffer resource.</returns>
        public Resource<VertexBuffer> CreateVertexBuffer(D3DVertexBufferProfile profile, int sizeInBytes)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateVertexBuffer - Null device: can't create vertex buffer!");
            VertexBuffer vb = new VertexBuffer(GFXDevice.Instance.Device, sizeInBytes, profile._bufferUsage);
            InternalResource<VertexBuffer> res = new InternalResource<VertexBuffer>(vb, profile);
            _resources.Add(res);
            return new Resource<VertexBuffer>(res);
        }



        /// <summary>
        /// Create a Dynamic Vertex Buffer resource, using the specified profile and size.
        /// </summary>
        /// <param name="profile">The vertex buffer profile of the vertex buffer.</param>
        /// <param name="sizeInBytes">The size in bytes of the vertex buffer.</param>
        /// <returns>A new vertex buffer resource.</returns>
        public Resource<DynamicVertexBuffer> CreateDynamicVertexBuffer(D3DVertexBufferProfile profile, int sizeInBytes)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateDynamicVertexBuffer - Null device: can't create vertex buffer!");
            DynamicVertexBuffer vb = new DynamicVertexBuffer(GFXDevice.Instance.Device, sizeInBytes, profile._bufferUsage);
            InternalResource<DynamicVertexBuffer> res = new InternalResource<DynamicVertexBuffer>(vb, profile);
            _resources.Add(res);
            return new Resource<DynamicVertexBuffer>(res);
        }



        /// <summary>
        /// Create an Index Buffer resource, using the specified profile, size, and index element size.
        /// </summary>
        /// <param name="profile">The index buffer profile of the index buffer.</param>
        /// <param name="sizeInBytes">The size in bytes of the index buffer.</param>
        /// <param name="indexElementSize">The size of index elements. Use the IndexElementSize enum.</param>
        /// <returns>A new index buffer resource.</returns>
        public Resource<IndexBuffer> CreateIndexBuffer(D3DIndexBufferProfile profile, int sizeInBytes, IndexElementSize indexElementSize)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateIndexBuffer - Null device: can't create index buffer!");
            IndexBuffer ib = new IndexBuffer(GFXDevice.Instance.Device, sizeInBytes, profile._bufferUsage, indexElementSize);
            InternalResource<IndexBuffer> res = new InternalResource<IndexBuffer>(ib, profile);
            _resources.Add(res);
            return new Resource<IndexBuffer>(res);
        }



        /// <summary>
        /// Create an Dynamic Index Buffer resource, using the specified profile, size, and index element size.
        /// </summary>
        /// <param name="profile">The index buffer profile of the index buffer.</param>
        /// <param name="sizeInBytes">The size in bytes of the index buffer.</param>
        /// <param name="indexElementSize">The size of index elements. Use the IndexElementSize enum.</param>
        /// <returns>A new index buffer resource.</returns>
        public Resource<DynamicIndexBuffer> CreateDynamicIndexBuffer(D3DIndexBufferProfile profile, int sizeInBytes, IndexElementSize indexElementSize)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateDynamicIndexBuffer - Null device: can't create index buffer!");
            DynamicIndexBuffer ib = new DynamicIndexBuffer(GFXDevice.Instance.Device, sizeInBytes, profile._bufferUsage, indexElementSize);
            InternalResource<DynamicIndexBuffer> res = new InternalResource<DynamicIndexBuffer>(ib, profile);
            _resources.Add(res);
            return new Resource<DynamicIndexBuffer>(res);
        }



        /// <summary>
        /// Create a texture cube resource.
        /// </summary>
        /// <param name="profile">Profile to use.</param>
        /// <param name="edgeLength">Length of an edge of the cube in pixels.</param>
        /// <param name="numberLevels">Number of mip levels for cube textures.</param>
        /// <param name="surfaceFormat">Format of cube surfaces.</param>
        /// <returns>A new texture cube resource.</returns>
        public Resource<TextureCube> CreateTextureCube(D3DTextureCubeProfile profile, int edgeLength, int numberLevels, SurfaceFormat surfaceFormat)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateTextureCube - Null device: can't create texture cube!");
            TextureCube tc = new TextureCube(GFXDevice.Instance.Device, edgeLength, numberLevels, profile._textureUsage, surfaceFormat);
            InternalResource<TextureCube> res = new InternalResource<TextureCube>(tc, profile);
            _resources.Add(res);
            return new Resource<TextureCube>(res);
        }



        /// <summary>
        /// Create a TextureCube Resource.
        /// </summary>
        /// <param name="rtCube">Cube Render target resource from which the texture should be created.</param>
        /// <returns>A new texture cube resource.</returns>
        public Resource<TextureCube> CreateTextureCube(Resource<RenderTargetCube> rtCube)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateTextureCube - Null device: can't create texture cube!");

            // the texture cube has the same resource mode as the render target
            D3DTextureCubeProfile p = null;
            if (rtCube._resource.ResourceProfile == ResourceProfiles.ManualRenderTargetCubeProfile)
                p = ResourceProfiles.ManualTextureCubeProfile;
            else if (rtCube._resource.ResourceProfile == ResourceProfiles.AutomaticTextureCubeProfile)
                p = ResourceProfiles.AutomaticTextureCubeProfile;
            else
                Assert.Fatal(false, "ResourceManager.CreateTextureCube - Unknown render target profile.");

            TextureCube tc = rtCube.Instance.GetTexture();
            InternalResource<TextureCube> res = new InternalResource<TextureCube>(tc, p);
            _resources.Add(res);
            return new Resource<TextureCube>(res);
        }



        /// <summary>
        /// Create a RenderTargetCube resource.
        /// </summary>
        /// <param name="edgeLength">Length of an edge of the cube in pixels.</param>
        /// <returns>A new render target cube resource.</returns>
        public Resource<RenderTargetCube> CreateRenderTargetCube(int edgeLength)
        {
            Assert.Fatal(GFXDevice.Instance.Device != null, "ResourceManager.CreateRenderTargetCube - Null device: can't create texture cube!");
            // we don't seem to have control anymore over the resource pool of the cube render target.  it seems to be created in the default
            // ("manual") pool, so we'll specify that and then assert if in fact it was created somewhere else.
            D3DRenderTargetCubeProfile profile = ResourceProfiles.ManualRenderTargetCubeProfile;

            int msQuality = GFXDevice.Instance.Device.PresentationParameters.MultiSampleQuality;
            MultiSampleType msType = GFXDevice.Instance.Device.PresentationParameters.MultiSampleType;

            RenderTargetCube rt = new RenderTargetCube(GFXDevice.Instance.Device, edgeLength, 1, SurfaceFormat.Bgr32, msType, msQuality);
            InternalResource<RenderTargetCube> res = new InternalResource<RenderTargetCube>(rt, profile);
            _resources.Add(res);
            return new Resource<RenderTargetCube>(res);
        }



        /// <summary>
        /// Create a RenderTarget2D resource.
        /// </summary>
        /// <param name="width">Width of render target</param>
        /// <param name="height">Height of render target</param>
        /// <param name="numberLevels">Number of mip levels of render target</param>
        /// <param name="format">SurfaceFormat of render target</param>
        /// <returns>A new 2D render target resource.</returns>
        public Resource<RenderTarget2D> CreateRenderTarget2D(int width, int height, int numberLevels, SurfaceFormat format)
        {
            int msQuality = GFXDevice.Instance.Device.PresentationParameters.MultiSampleQuality;
            MultiSampleType msType = GFXDevice.Instance.Device.PresentationParameters.MultiSampleType;
            RenderTarget2D rt = new RenderTarget2D(GFXDevice.Instance.Device, width, height, numberLevels, format, msType, msQuality, RenderTargetUsage.DiscardContents);
            InternalResource<RenderTarget2D> res = new InternalResource<RenderTarget2D>(rt, ResourceProfiles.ManualGenericProfile);
            _resources.Add(res);
            return new Resource<RenderTarget2D>(res);
        }



        /// <summary>
        /// Create a resource for the given type.  These resources are not tracked by the resource manager; i.e., they won't
        /// be invalidated on a device reset (even if they should be).  The main purpose of this function is to create
        /// an intermediate resource for APIs or objects that require a resource type.
        /// </summary>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="obj">Instance from which to create the resource.</param>
        /// <returns>A new resource of the given type.</returns>
        public Resource<T> CreateResource<T>(T obj)
        {
            // we don't add these resources to our list of tracked resources
            InternalResource<T> res = new InternalResource<T>(obj, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);
            return new Resource<T>(res);
        }



        /// <summary>
        /// Load an effect and return it. If the asset name does not contain any slashes ("\\" or "/"), 
        /// it is assumed to be a TorqueEngineData asset and is loaded from the internal resource content manager.
        /// </summary>
        /// <param name="asset">Path and filename of the effect.</param>
        /// <returns>A new resource created using the newly loaded effect.</returns>
        public Resource<Effect> LoadEffect(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded effect - content manager will return cached effect if it already loaded it.
            Effect newEffect = cm.Load<Effect>(loadpath);
            InternalResource<Effect> res = new InternalResource<Effect>(newEffect, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);
            return new Resource<Effect>(res);
        }



        /// <summary>
        /// Load a model and return it. If the asset name does not contain any slashes ("\\" or "/"), 
        /// it is assumed to be a TorqueEngineData asset and is loaded from the internal resource content manager.
        /// </summary>
        /// <param name="asset">Path and filename of the model.</param>
        /// <returns>A new resource created using the newly loaded model.</returns>
        public Resource<Model> LoadModel(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded model - content manager will return cached model if it already loaded it.
            Model newModel = cm.Load<Model>(loadpath);
            InternalResource<Model> res = new InternalResource<Model>(newModel, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);
            return new Resource<Model>(res);
        }



        /// <summary>
        /// Load a font and return it. If the asset name does not contain any slashes ("\\" or "/"), 
        /// it is assumed to be a TorqueEngineData asset and is loaded from the internal resource content manager.
        /// </summary>
        /// <param name="asset">Path and filename of the font.</param>
        /// <returns>A new resource created using the newly loaded font.</returns>
        public Resource<SpriteFont> LoadFont(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded font - content manager will return cached font if it already loaded it.
            SpriteFont newFont = cm.Load<SpriteFont>(loadpath);
            InternalResource<SpriteFont> res = new InternalResource<SpriteFont>(newFont, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);
            return new Resource<SpriteFont>(res);
        }



        /// <summary>
        /// Load a Texture and return it. If the asset name does not contain any slashes ("\\" or "/"), 
        /// it is assumed to be a TorqueEngineData asset and is loaded from the internal resource content manager.
        /// </summary>
        /// <param name="asset">Path and filename of the texture.</param>
        /// <returns>A new resource created using the newly loaded texture.</returns>
        public Resource<Texture> LoadTexture(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded texture - content manager will return cached texture if it already loaded it.

            try
            {
                Texture tex = cm.Load<Texture>(loadpath);
                InternalResource<Texture> res = new InternalResource<Texture>(tex, ResourceProfiles.DefaultContentManagerProfile);
                _resources.Add(res);
                return new Resource<Texture>(res);
            }
            catch (Exception)
            {
                // Failed to load the texture... return null instead
                // of crashing out.
                return new Resource<Texture>(null);
            }

        }



        public Resource<Video> LoadVideo(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded texture - content manager will return cached texture if it already loaded it.
            Video tex = cm.Load<Video>(loadpath);
            InternalResource<Video> res = new InternalResource<Video>(tex, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);
            return new Resource<Video>(res);
        }



        /// <summary>
        /// Load a Texture and return it. If the asset name does not contain any slashes ("\\" or "/"), 
        /// it is assumed to be a TorqueEngineData asset and is loaded from the internal resource content manager.
        /// </summary>
        /// <param name="asset">Path and filename of the texture.</param>
        /// <returns>A new resource created using the newly loaded texture.</returns>
        public Resource<Texture2D> LoadTexture2D(string asset)
        {
            // remove extension
            string loadpath = TorqueUtil.ChopFileExtension(asset.Trim());

            bool isEngineResource = !(asset.Contains("\\") || asset.Contains("/"));
            ContentManager cm = isEngineResource ? _resourceContentManager.Instance : CurrentContentManager;

            // return loaded texture - content manager will return cached texture if it already loaded it.
            Texture2D tex = cm.Load<Texture2D>(loadpath);
            InternalResource<Texture2D> res = new InternalResource<Texture2D>(tex, ResourceProfiles.DefaultContentManagerProfile);
            _resources.Add(res);

            return new Resource<Texture2D>(res);
        }

        #endregion


        #region Private, protected, internal fields

        List<BaseResource> _resources = new List<BaseResource>();
        static ResourceManager _instance = new ResourceManager();

        System.Resources.ResourceManager _systemResourceManager;
        Resource<ResourceContentManager> _resourceContentManager;

        ContentManager _globalContentManager;
        Stack<ContentManager> _contentManagerStack = new Stack<ContentManager>();

        #endregion
    }
}
