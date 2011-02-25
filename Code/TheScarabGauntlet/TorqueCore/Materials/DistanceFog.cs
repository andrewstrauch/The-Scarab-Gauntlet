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
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using System.Xml.Serialization;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Material for fogging objects based on distance. A fog material is set on the scenegraph and
    /// rendered with every object as a second pass to slowly fade them to a solid color as they get
    /// farther away.
    /// 
    /// Parameters
    /// 
    /// worldMatrix: The object to world space matrix for the object being rendered
    /// worldViewProjection: The object to screen space matrix for the object being rendered
    /// fogColor: The color of the fog
    /// fogNearDist: The distance at which objects are just starting to fog
    /// fogFarDist: The distance at which objects are fully fogged
    /// camPos: The position of the camera
    /// 
    /// Techniques
    /// 
    /// DistanceFog
    /// </summary>
    public class DistanceFog : RenderMaterial, IFogMaterial
    {

        #region Constructors

        public DistanceFog()
        {
            EffectFilename = "DistanceFog";
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The color of fog this material will render as a Vector3.
        /// </summary>
        public Vector3 FogColorAsVector3
        {
            get { return _fogColor; }
            set { _fogColor = value; }
        }



        /// <summary>
        /// The color of fog this material will render as a Color object.
        /// </summary>
        [XmlIgnore]
        public Color FogColorAsColor
        {
            get { return new Color(_fogColor); }
            set { _fogColor = value.ToVector3(); }
        }



        /// <summary>
        /// The distance from the camera to start drawing fog.
        /// </summary>
        public float FogNearDistance
        {
            get { return _fogNearDistance; }
            set { _fogNearDistance = value; }
        }



        /// <summary>
        /// The distance from the camera at which fog will be fully visible.
        /// </summary>
        public float FogFarDistance
        {
            get { return _fogFarDistance; }
            set { _fogFarDistance = value; }
        }

        #endregion


        #region Public methods

        public bool IsObjectObscured(Vector3 camPos, Box3F bounds)
        {
            // do a fast radius check to see if any part of the object could be 
            // within the fog far distance
            _objBoundsMin = bounds.Min;
            _objBoundsMax = bounds.Max;

            float radius = Math.Max(_objBoundsMax.X - _objBoundsMin.X,
                                Math.Max(_objBoundsMax.Y - _objBoundsMin.Y,
                                        _objBoundsMax.Z - _objBoundsMin.Z));

            if (Vector3.Distance(camPos, bounds.Center) - radius < _fogFarDistance)
                return false;

            return true;
        }



        public bool IsObjectFogged(Vector3 camPos, Box3F bounds)
        {
            // do a fast radius check to see if any part of the object could be completely
            // inside the unfogged area close to the camera
            _objBoundsMin = bounds.Min;
            _objBoundsMax = bounds.Max;

            float radius = Math.Max(_objBoundsMax.X - _objBoundsMin.X,
                                Math.Max(_objBoundsMax.Y - _objBoundsMin.Y,
                                        _objBoundsMax.Z - _objBoundsMin.Z));

            if (Vector3.Distance(camPos, bounds.Center) + radius < _fogNearDistance)
                return false;

            return true;
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            srs.Gfx.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            srs.Gfx.Device.RenderState.SourceBlend = Blend.SourceAlpha;

            return base._SetupEffect(srs, materialData);
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            EffectManager.SetParameter(_fogColorParameter, _fogColor);
            EffectManager.SetParameter(_fogNearDistanceParameter, _fogNearDistance);
            EffectManager.SetParameter(_fogFarDistanceParameter, _fogFarDistance);
            EffectManager.SetParameter(_camPosParameter, srs.CameraPosition);
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldMatrixParameter, renderInstance.ObjectTransform);
            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _worldMatrixParameter = EffectManager.GetParameter(Effect, "worldMatrix");
            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
            _fogColorParameter = EffectManager.GetParameter(Effect, "fogColor");
            _fogNearDistanceParameter = EffectManager.GetParameter(Effect, "fogNearDist");
            _fogFarDistanceParameter = EffectManager.GetParameter(Effect, "fogFarDist");
            _camPosParameter = EffectManager.GetParameter(Effect, "camPos");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _worldMatrixParameter = null;
            _worldViewProjectionParameter = null;
            _fogColorParameter = null;
            _fogNearDistanceParameter = null;
            _fogFarDistanceParameter = null;
            _camPosParameter = null;
        }

        #endregion


        #region Private, protected, internal fields

        Vector3 _fogColor = Color.Salmon.ToVector3();
        float _fogNearDistance = 100;
        float _fogFarDistance = 600;

        EffectParameter _worldMatrixParameter;
        EffectParameter _worldViewProjectionParameter;
        EffectParameter _fogColorParameter;
        EffectParameter _fogNearDistanceParameter;
        EffectParameter _fogFarDistanceParameter;
        EffectParameter _camPosParameter;

        Vector3 _objBoundsMin = Vector3.Zero;
        Vector3 _objBoundsMax = Vector3.Zero;

        #endregion
    }
}
