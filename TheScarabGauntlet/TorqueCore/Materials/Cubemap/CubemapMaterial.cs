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
using System.Xml.Serialization;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Material for cubemaps. This can be dynamic or static.
    /// </summary>
    public class CubemapMaterial : RenderMaterial, IReflectionMaterial
    {

        #region Constructors

        public CubemapMaterial()
        {
            EffectFilename = "CubemapEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The cubemap object.
        /// </summary>
        public Cubemap Cubemap
        {
            get { return _cubemap; }
        }



        /// <summary>
        /// The camera used to render the cubemap.
        /// </summary>
        [XmlIgnore]
        public CubemapCamera Camera
        {
            get { return _camera; }
            set { _camera = value; }
        }



        /// <summary>
        /// Whether or not the cubemap is dynamic.
        /// </summary>
        public bool IsDynamic
        {
            get { return _isDynamic; }
            set { _isDynamic = value; }
        }



        /// <summary>
        /// The pixel size of each face of the cubemap.
        /// </summary>
        public int CubemapSize
        {
            get { return _cubemapSize; }
            set { _cubemapSize = value; }
        }



        public int Priority
        {
            get
            {
                if (IsDynamic)
                    return Cubemap.DynamicUpdatePriority;

                return -1;
            }
            set { Cubemap.DynamicUpdatePriority = value; }
        }



        /// <summary>
        /// Always true.
        /// </summary>
        public override bool IsReflective
        {
            get { return true; }
            set { }
        }

        #endregion


        #region Public methods

        public void Update(SceneRenderState srs, Matrix objectTransform)
        {
            Cubemap.Position = objectTransform.Translation;
            Cubemap.UpdateDynamic(srs);
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            _cubemap.Destroy();
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(GarageGames.Torque.SceneGraph.SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (!_cubemap.IsInitialized)
                _cubemap.Create(_isDynamic, _cubemapSize, _camera);

            return base._SetupEffect(srs, materialData);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _rotationParameter = EffectManager.GetParameter(Effect, "rotation");
            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
            _cubeTextureParameter = EffectManager.GetParameter(Effect, "cubeTexture");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _rotationParameter = null;
            _worldViewProjectionParameter = null;
            _cubeTextureParameter = null;
        }



        protected override void _SetupGlobalParameters(GarageGames.Torque.SceneGraph.SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            _cubeTextureParameter.SetValue(_cubemap.Texture);
        }



        protected override void _SetupObjectParameters(GarageGames.Torque.RenderManager.RenderInstance renderInstance, GarageGames.Torque.SceneGraph.SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            Matrix rotation = renderInstance.ObjectTransform;
            rotation.Translation = Vector3.Zero;

            _worldViewProjectionParameter.SetValue(renderInstance.ObjectTransform * srs.View * srs.Projection);
            _rotationParameter.SetValue(rotation);
        }

        #endregion


        #region Private, protected, internal fields

        Cubemap _cubemap = new Cubemap();
        CubemapCamera _camera = new CubemapCamera();
        bool _isDynamic = false;
        int _cubemapSize = 64;

        EffectParameter _worldViewProjectionParameter;
        EffectParameter _cubeTextureParameter;
        EffectParameter _rotationParameter;

        #endregion
    }
}
