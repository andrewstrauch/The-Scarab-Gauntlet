//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Sim;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Base class for all materials. Creating a new material is pretty simple, and will
    /// generally need to be done each time a new effect is created. There are some exceptions
    /// to this. A material can be used for multiple effects if each effect has the same
    /// parameters and techniques. Also, many effects created using ATI's Render Monkey or
    /// Nvidia's FX Composer can be used directly with GenericMaterial.
    /// 
    /// To create a new material, derive from this class and any relevant material interfaces
    /// (IReflectionMaterial, IRefractionMaterial, ITextureMaterial, or IFogMaterial). Then,
    /// implement _LoadParameters, _ClearParameters, _SetupEffect, _SetupGlobalParameters, and
    /// _SetupObjectParameters. In most cases, this is all that will need to be done, although
    /// most of the other methods are virtual as well for the rare case that the base method
    /// needs to be extended or changed.
    /// 
    /// Rendering with a material is done by calling SetupEffect, followed by a SetupPass loop,
    /// followed by CleanupEffect. More than likely this will never have to be done, since the
    /// render manager already does it, but, for reference:
    /// 
    /// <code>
    /// material.SetupEffect(sceneRenderState, materialInstanceData);
    /// while (material.SetupPass())
    /// {
    ///    material.SetupObject(renderInstance, sceneRenderState);
    ///    // draw the geometry...
    /// }
    /// 
    /// material.CleanupEffect();
    /// </code>
    /// </summary>
    public abstract class RenderMaterial : TorqueBase, ICloneable, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// If non empty, the material will be mapped to the specified name in the MaterialManager. This is used
        /// when loading meshes to determine the material to use for a mesh. DTS meshes look for a material with
        /// name mapped to its texture name (using the full path from the project root). XNA meshes look for a
        /// material with name mapped to the name of the mesh appended to the path to the model file (this is the
        /// ModelMesh.Name property).
        /// </summary>
        public String NameMapping
        {
            get { return _nameMapping; }
            set { _nameMapping = value; }
        }



        /// <summary>
        /// The name of the effect to use with the material. This is most often set in
        /// the constructor of derived classes since, for the most part, materials are tied
        /// directly to a specific effect. However, it can be set at any time to any existing
        /// effect filename. Effects that are included in TorqueEngineData just need the name
        /// of the effect set, whereas custom effects will need to have the full path from
        /// the project root set.
        /// </summary>
        public string EffectFilename
        {
            get { return _effectFilename; }
            set
            {
                _ClearEffect();
                _effectFilename = value;
            }
        }



        /// <summary>
        /// The effect instance.
        /// </summary>
        public Resource<Effect> Effect
        {
            get { return _effect; }
        }



        /// <summary>
        /// Specifies the TextureDivider that will handle subdivisions of this material. This is only used
        /// in 2D right now.
        /// </summary>
        public TextureDivider TextureDivider
        {
            get { return _textureDivider; }
            set
            {
                if (_textureDivider != null)
                    _textureDivider.Destroy();

                _textureDivider = value;

                if (_textureDivider != null)
                    _textureDivider.Init(this);
            }
        }



        /// <summary>
        /// Specifies the number of regions on this material, as defined by the material's TextureDivider. If
        /// a texture divider wasn't set, then 1 is returned.
        /// </summary>
        public int TextureRegionCount
        {
            get { return TextureDivider != null ? TextureDivider.GetRegionCount() : 1; }
        }



        /// <summary>
        /// Whether or not to render the material in wireframe. Only works in debug.
        /// </summary>
        public bool IsWireframe
        {
            get { return _isWireframe; }
            set { _isWireframe = value; }
        }

        /// <summary>
        /// Whether or not the material is reflective. If this is true, the material must
        /// implement IReflectionMaterial. It will be added automatically to the reflection
        /// render manager.
        /// </summary>
        public virtual bool IsReflective
        {
            get { return _isReflective; }
            set { _isReflective = value; }
        }



        /// <summary>
        /// Whether or not the material is refractive. If this is true, the material must
        /// implement IRefractionMaterial. Objects using this material will be automatically
        /// added to the refraction render manager.
        /// </summary>
        public virtual bool IsRefractive
        {
            get { return _isRefractive; }
            set { _isRefractive = value; }
        }



        /// <summary>
        /// Whether or not the material is translucent. Setting this to true causes objects with this
        /// material to be rendered with the translucent render manager, which means they are blended
        /// and rendered back to front. In 3D, this should be set on any material that has any alpha in
        /// its texture. In 2D, this should be set on any material that has translucent parts. i.e an
        /// alpha value of something other than 0 or 255. For 2D, this is only relevant when using
        /// depth buffering.
        /// </summary>
        public bool IsTranslucent
        {
            get { return _isTranslucent; }
            set { _isTranslucent = value; }
        }



        /// <summary>
        /// Shortcut for setting additive blending (source = SourceAlpha, destination = One).
        /// </summary>
        public bool IsAdditive
        {
            get { return _sourceBlend == Blend.SourceAlpha && _destinationBlend == Blend.One; }
            set
            {
                _sourceBlend = Blend.SourceAlpha;
                if (value)
                    _destinationBlend = Blend.One;
                else
                    _destinationBlend = Blend.InverseSourceAlpha;
            }
        }



        /// <summary>
        /// The source blend factor to use when rendering with this material. This is only applied when
        /// IsTranslucent is true.
        /// </summary>
        public Blend SourceBlend
        {
            get { return _sourceBlend; }
            set { _sourceBlend = value; }
        }



        /// <summary>
        /// The destination blend factor to use when rendering with this material. This is only applied
        /// when IsTranslucent is true.
        /// </summary>
        public Blend DestinationBlend
        {
            get { return _destinationBlend; }
            set { _destinationBlend = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Prepares the material for rendering using the specified scene render state.
        /// </summary>
        /// <param name="srs">The scene render state to use when rendering.</param>
        /// <param name="materialData">Data spcific to this material that defines additional parameters
        /// for setting up the effect.</param>
        public virtual void SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            Assert.Fatal(!_isEffectActive, "RenderMaterial.SetupEffect - The effect is already active!");

            if (_effect.IsNull || _effect.IsInvalid)
            {
                _LoadEffect();
                Assert.Fatal(!(_effect.IsNull || _effect.IsInvalid), "RenderMaterial.SetupEffect - Failed to load effect!");
            }

#if DEBUG
            srs.Gfx.Device.RenderState.FillMode = _isWireframe ? FillMode.WireFrame : FillMode.Solid;
#endif

            _previousU = srs.Gfx.Device.SamplerStates[0].AddressU;
            _previousV = srs.Gfx.Device.SamplerStates[0].AddressV;

            // NOTE: IsTranslucent isn't tested here because its the render bins and
            // not the material itself which controls the alpha blend enabled state.
            //
            // So although its inefficient we must set the blend mode here all the 
            // time to ensure we get a consistent render state.            
            srs.Gfx.Device.RenderState.SourceBlend = _sourceBlend;
            srs.Gfx.Device.RenderState.DestinationBlend = _destinationBlend;

            string technique = _SetupEffect(srs, materialData);
            _effect.Instance.CurrentTechnique = _effect.Instance.Techniques[technique];

            if (_effect.Instance.Techniques["BlendWithLightMap_2_0"] != null)
                _effect.Instance.CurrentTechnique = _effect.Instance.Techniques["BlendWithLightMap_2_0"];

            if (!_hasLoadedParameters)
                _LoadParameters();

            _SetupGlobalParameters(srs, materialData);
        }



        /// <summary>
        /// Sets up the material state to render a pass. Draw primitive can be called after this. Generally this
        /// is called repeatedly in a loop until it returns false.
        /// </summary>
        /// <returns>False if no more passes are required.</returns>
        public virtual bool SetupPass()
        {
            Assert.Fatal(!_effect.IsNull, "XNAMaterial.SetupPass - Invalid effect!");

            // first pass - initialize everything
            if (!_isEffectActive)
            {
                _effect.Instance.Begin(SaveStateMode.None);
                _passCount = _effect.Instance.CurrentTechnique.Passes.Count;
                _nextPass = 0;
                _isEffectActive = true;
            }

            // second or later pass - end the previous pass
            if (_isPassActive)
            {
                _effect.Instance.CurrentTechnique.Passes[_nextPass - 1].End();
                _isPassActive = false;
            }

            // no more passes
            if (_nextPass >= _passCount)
                return false;

            // start the next pass
            _isPassActive = true;
            _effect.Instance.CurrentTechnique.Passes[_nextPass].Begin();
            _nextPass++;
            return true;
        }



        /// <summary>
        /// Sets up any object specific data on the material using the specified render instance.
        /// </summary>
        /// <param name="ri">The render instance that is about to be rendered.</param>
        /// <param name="srs">The current render state of the scene.</param>
        public virtual void SetupObject(RenderInstance ri, SceneRenderState srs)
        {
            if (_previousU != ri.UTextureAddressMode)
            {
                srs.Gfx.Device.SamplerStates[0].AddressU = ri.UTextureAddressMode;
                _previousU = ri.UTextureAddressMode;
            }

            if (_previousV != ri.VTextureAddressMode)
            {
                srs.Gfx.Device.SamplerStates[0].AddressV = ri.VTextureAddressMode;
                _previousV = ri.VTextureAddressMode;
            }

            _SetupObjectParameters(ri, srs);
            CommitChanges();
        }



        /// <summary>
        /// Commit any state changes to to the underlying material system. Materials are responsible for calling
        /// this when appropriate. SetupObject calls this already, so it is not necessary to call it from
        /// _SetupObjectParameters.
        /// </summary>
        public virtual void CommitChanges()
        {
            Assert.Fatal(!_effect.IsNull, "RenderMaterial.CommitChanges - Invalid effect!");

            if (_isEffectActive)
                _effect.Instance.CommitChanges();
        }



        /// <summary>
        /// Cleans up the material after rendering. Should be called after the SetupPass loop is complete.
        /// </summary>
        public virtual void CleanupEffect()
        {
            // end previous pass if applicable
            if (_isPassActive)
            {
                _effect.Instance.CurrentTechnique.Passes[_nextPass].End();
                _isPassActive = false;
            }

            // end effect
            if (_isEffectActive)
            {
                _effect.Instance.End();
                _isEffectActive = false;
            }
        }



        /// <summary>
        /// Adds the material to the material manager.
        /// </summary>
        public override void OnLoaded()
        {
            base.OnLoaded();
            MaterialManager.Add(_nameMapping, this);
        }



        /// <summary>
        /// Removes the material from the material manager.
        /// </summary>
        public override void OnUnloaded()
        {
            MaterialManager.Remove(this);
            base.OnUnloaded();
        }



        /// <summary>
        /// Get the texture coordinates associated with the specified region index.
        /// </summary>
        /// <param name="index">The region index for which to retrieve texture coordinates.</param>
        /// <param name="t0">The top left corner of the region specified.</param>
        /// <param name="t1">The top right corner of the region specified.</param>
        /// <param name="t2">The bottom right corner of the region specified.</param>
        /// <param name="t3">The bottom left corner of the region specified.</param>
        public void GetRegionCoords(int index, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3)
        {
            // if no textureDivider has been created, return the full texture space
            if (_textureDivider == null)
            {
                t0 = new Vector2(0.0f, 0.0f);
                t1 = new Vector2(1.0f, 0.0f);
                t2 = new Vector2(1.0f, 1.0f);
                t3 = new Vector2(0.0f, 1.0f);
            }
            else
            {
                // if we have a texture divider, get the texture coordinates for this region index
                _textureDivider.GetRegionCoords(index, out t0, out t1, out t2, out t3);
            }
        }



        /// <summary>
        /// Get the texture coordinates associated with the specified region index.
        /// </summary>
        /// <param name="index">The region index for which to retrieve texture coordinates.</param>
        /// <returns>A rectangle representing the texture coordinates of the specified region.</returns>
        public RectangleF GetRegionCoords(int index)
        {
            // if no textureDivider has been created, return the full texture space
            if (_textureDivider == null)
                return new RectangleF(0.0f, 0.0f, 1.0f, 1.0f);

            // if we have a texture divider, get the texture coordinates for this region index
            else
                return _textureDivider.GetRegionCoords(index);
        }

        //------------------------------------------------------
        #region ICloneable Members

        /// <summary>
        /// This function renders a memberwise clone of the material. Note that materials are cloneable but do not
        /// participate in TorqueObject style object pooling (since they are TorqueBase).
        /// </summary>
        /// <returns>A clone of the material.</returns>
        public virtual object Clone()
        {
            Assert.Fatal(!_isEffectActive, "XNAEffect.Clone - Cannot clone the effect while it is active!");
            return MemberwiseClone();
        }

        #endregion

        //------------------------------------------------------
        #region IDisposable Members

        /// <summary>
        /// Clears the effect and its parameters.
        /// </summary>
        public override void Dispose()
        {
            _IsDisposed = true;
            Name = String.Empty;
            ///_ClearEffect();
            _nameMapping = "";
            _textureDivider = null;
            _effectFilename = "";
            _ResetRefs();
            _ref = null;
            base.Dispose();
        }

        #endregion

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Called when the effect needs to be loaded/reloaded. Loads the effect from the resource
        /// manager.
        /// </summary>
        protected virtual void _LoadEffect()
        {
            if (!(_effect.IsNull || _effect.IsInvalid) || String.IsNullOrEmpty(_effectFilename))
                return;

            _effect = ResourceManager.Instance.LoadEffect(_effectFilename);
            _ClearParameters();
        }



        /// <summary>
        /// Called when the material is disposed to cleanup the effect.
        /// </summary>
        protected virtual void _ClearEffect()
        {
            if (!_effect.IsNull)
                _effect.Invalidate();
            _effectFilename = String.Empty;
            _ClearParameters();
        }



        /// <summary>
        /// This is called from SetupEffect prior to rendering with this material to setup any effect
        /// specific render states or properties and select the technique to use for the effect. It is
        /// not necessary to call the base method from derived classes as all it does is return the first
        /// technique in the effect's technique collection.
        /// </summary>
        /// <param name="srs">The current scene render state.</param>
        /// <param name="materialData">Object specific data for the material.</param>
        /// <returns>The name of the technique to use for the effect.</returns>
        protected virtual string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            return _effect.Instance.Techniques[0].Name;
        }

        /// <summary>
        /// This is called once on the material per render instance batch after SetupEffect, and before any
        /// calls to SetupPass. It should be used to set the values for any effect parameters that are the
        /// same across all objects using the material. The base method does nothing.
        /// </summary>
        /// <param name="srs">The current scene render state.</param>
        /// <param name="materialData">Object specific data for the material.</param>
        protected virtual void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
        }



        /// <summary>
        /// This is called once on the material for each render instance each pass. It should be used to set
        /// the values for any effect parameters that are unique to each object using the material. The base
        /// method does nothing.
        /// </summary>
        /// <param name="renderInstance">The next render instance to be rendered with this material.</param>
        /// <param name="srs">The current scene render state.</param>
        protected virtual void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
        }



        /// <summary>
        /// This is called whenever the effect parameters need to be loaded. This happens once at startup, and
        /// additionally after a device reset. The base method must be called for correct behavior. See the
        /// EffectManager for more information on loading effect parameters.
        /// </summary>
        protected virtual void _LoadParameters()
        {
            _hasLoadedParameters = true;
        }



        /// <summary>
        /// This is called whenever the effect parameters need to be cleared (like when a device reset happens).
        /// Every parameter that is loaded in _LoadParameters should be set to null here. Also, it is important
        /// that the base method be called.
        /// </summary>
        protected virtual void _ClearParameters()
        {
            _hasLoadedParameters = false;
        }

        #endregion


        #region Private, protected, internal fields

        string _nameMapping = String.Empty;
        TextureDivider _textureDivider;

        bool _isWireframe;
        bool _isTranslucent;
        bool _isReflective;
        bool _isRefractive;

        Blend _sourceBlend = Blend.SourceAlpha;
        Blend _destinationBlend = Blend.InverseSourceAlpha;

        string _effectFilename = String.Empty;
        protected Resource<Effect> _effect;

        /// <summary>
        /// Total number of passes on the effect.
        /// </summary>
        protected int _passCount;

        /// <summary>
        /// The next pass to render. The current pass is this value - 1.
        /// </summary>
        protected int _nextPass = 0;
        bool _isPassActive = false;
        bool _isEffectActive = false;

        bool _hasLoadedParameters = false;

        TextureAddressMode _previousU = TextureAddressMode.Clamp;
        TextureAddressMode _previousV = TextureAddressMode.Clamp;

        #endregion
    }
}
