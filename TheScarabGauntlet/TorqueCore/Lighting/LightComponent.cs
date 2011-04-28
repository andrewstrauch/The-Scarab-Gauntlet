//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using System.Xml.Serialization;
using System.ComponentModel;



namespace GarageGames.Torque.Lighting
{
    /// <summary>
    /// Component for adding lights to objects. Each LightComponent can store a list of any
    /// number of lights. The first light in the list can be looked up via the "light" interface.
    /// Each light in the list is added to the scenegraph of its owner scene object. Lights are
    /// then picked for each material that receives lighting based on how much it is affected
    /// and how many the material supports (usually hardware dependent).
    /// 
    /// This base light component should not be used directly. Instead use T3DLightComponent for
    /// 3D scenes and T2DLightComponent for 2D scenes.
    /// </summary>
    public abstract class LightComponent : TorqueComponent, ILightObject, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The first light in the light list.
        /// </summary>
        [TorqueCloneIgnore]
        [BrowsableAttribute(false)]
        public Light Light
        {
            get { return _light.Value; }
        }



        /// <summary>
        /// The list of all lights on this component.
        /// </summary>
        [TorqueCloneIgnore]
        [EditorAttribute("TorqueXBuilder3D.LightConverter, TorqueXBuilder3D", typeof(System.Drawing.Design.UITypeEditor))]
        public List<Light> LightList
        {
            get { return _lightList; }
            set
            {
                _UnregisterLights();
                _lightList = value;
                _RegisterLights();

                if (_lightList.Count > 0)
                    _light.Value = _lightList[0];
            }
        }



        [BrowsableAttribute(false)]
        public virtual Vector3 Position
        {
            get { return _sceneGroup.Transform.Translation; }
        }



        [TorqueXmlSchemaType(DefaultValue = "true")]
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value)
                    return;

                _UnregisterLights();
                _isEnabled = value;
                _RegisterLights();
            }
        }

        #endregion


        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            LightComponent obj2 = (LightComponent)obj;
            obj2.IsEnabled = IsEnabled;
            List<Light> lightList = new List<Light>();
            for (int i = 0; i < _lightList.Count; i++)
                lightList.Add(_lightList[i].Clone());

            obj2.LightList = lightList;
        }

        #endregion


        #region Private, protected, internal methods

        protected internal override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            _RegisterLights();

            if (_lightList.Count > 0)
                _light.Value = _lightList[0];

            return true;
        }



        protected internal override void _OnUnregister()
        {
            _UnregisterLights();

            base._OnUnregister();
        }



        protected internal override void _RegisterInterfaces(TorqueObject owner)
        {
            base._RegisterInterfaces(owner);

            Owner.RegisterCachedInterface("light", "", this, _light);
        }



        protected void _RegisterLights()
        {
            if (!_isEnabled ||
                _sceneGroup == null ||
                _sceneGroup.SceneGraph == null)
                return;

            for (int i = 0; i < _lightList.Count; i++)
            {
                // If its a directional light then transform it by
                // the scene components transform.
                DirectionalLight light = _lightList[i] as DirectionalLight;
                if (light != null)
                    light.Direction = _sceneGroup.Transform.Forward;

                _sceneGroup.SceneGraph.AddLight(_lightList[i]);
                _lightList[i].Owner = this;
            }
        }



        protected void _UnregisterLights()
        {
            if (!_isEnabled ||
                _sceneGroup == null ||
                _sceneGroup.SceneGraph == null)
                return;

            for (int i = 0; i < _lightList.Count; i++)
            {
                _sceneGroup.SceneGraph.RemoveLight(_lightList[i]);
                _lightList[i].Owner = null;
            }
        }

        #endregion


        #region Private, protected, internal fields

        ValueInPlaceInterface<Light> _light = new ValueInPlaceInterface<Light>(null);
        List<Light> _lightList = new List<Light>();

        bool _isEnabled = true;

        protected ISceneObject _sceneGroup;

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (_lightList != null)
                this._lightList.Clear();
            _lightList = null;
        }

        #endregion
    }

}
