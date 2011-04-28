//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;
using System.Xml.Serialization;
using GarageGames.Torque.Core;
using System.ComponentModel;



namespace GarageGames.Torque.Lighting
{
    public interface ILightObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The position of the light in world space.
        /// </summary>
        Vector3 Position
        {
            get;
        }

        #endregion
    }



    abstract public class Light
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The ambient color of the light.
        /// </summary>
        public Vector3 AmbientColor
        {
            get { return _ambientColor; }
            set { _ambientColor = value; }
        }

        /// <summary>
        /// The diffuse color of the light.
        /// </summary>
        public Vector3 DiffuseColor
        {
            get { return _diffuseColor; }
            set { _diffuseColor = value; }
        }

        /// <summary>
        /// The light object that owns this light.
        /// </summary>
        [XmlIgnore]
        [BrowsableAttribute(false)]
        public ILightObject Owner
        {
            get { return _owner; }
            internal set { _owner = value; }
        }



        /// <summary>
        /// The position of the light in world space.
        /// </summary>
        public virtual Vector3 Position
        {
            get { return _owner.Position; }
        }



        /// <summary>
        /// The constant falloff of the light. If this is set to 1 and the linear attenuation
        /// is 0, the light will be constant everywhere (useful for something like the sun).
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public float ConstantAttenuation
        {
            get { return _constantAttenuation; }
            set { _constantAttenuation = value; }
        }



        /// <summary>
        /// The linear falloff of the light. Lower numbers will have the light extending farther.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public float LinearAttenuation
        {
            get { return _linearAttenuation; }
            set { _linearAttenuation = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Creates a copy of this light.
        /// </summary>
        /// <returns>A new light with the same properties as this one.</returns>
        public Light Clone()
        {
            Light light = ObjectPooler.Construct(this) as Light;
            _CopyTo(light);
            return light;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Copies all the fields on this light to the specified light.
        /// </summary>
        /// <param name="light">The light to copy values to.</param>
        protected virtual void _CopyTo(Light light)
        {
            light.AmbientColor = AmbientColor;
            light.DiffuseColor = DiffuseColor;
            light.ConstantAttenuation = ConstantAttenuation;
            light.LinearAttenuation = LinearAttenuation;
        }

        #endregion


        #region Private, protected, internal fields

        ILightObject _owner;
        protected Vector3 _ambientColor = new Vector3(0.5f);
        protected Vector3 _diffuseColor = new Vector3(0.8f);
        protected float _constantAttenuation;
        protected float _linearAttenuation;

        #endregion
    }



    /// <summary>
    /// A point light.
    /// </summary>
    [TorqueXmlSchemaType]
    public class PointLight : Light
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Offset from the owner's position that the light is at.
        /// </summary>
        public Vector3 PositionOffset
        {
            get { return _positionOffset; }
            set { _positionOffset = value; }
        }

        [XmlIgnore]
        [BrowsableAttribute(false)]
        public override Vector3 Position
        {
            get { return base.Position + _positionOffset; }
        }

        public override string ToString()
        {
            return "Point Light";
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _CopyTo(Light light)
        {
            base._CopyTo(light);

            PointLight pointLight = (PointLight)light;
            pointLight.PositionOffset = PositionOffset;
        }

        public Vector3 _positionOffset = Vector3.Zero;

        #endregion
    }



    [TorqueXmlSchemaType]
    public class DirectionalLight : Light
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The direction of the light.
        /// </summary>
        public Vector3 Direction = -Vector3.UnitZ;

        /// <summary>
        /// The light's position. This is a large number in the opposite direction of the
        /// light's direction.
        /// </summary>
        public override Vector3 Position
        {
            get { return -Direction * 1000000.0f; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _CopyTo(Light light)
        {
            base._CopyTo(light);

            DirectionalLight directionalLight = (DirectionalLight)light;
            directionalLight.Direction = Direction;
        }

        public override string ToString()
        {
            return "Directional Light";
        }

        #endregion
    }



    /// <summary>
    /// Comparer that sorts lights based on the affect they will have on a point in the scene.
    /// </summary>
    public class LightComparer : Comparer<Light>
    {

        #region Public properties, operators, constants, and enums

        public Box3F WorldBox;

        #endregion


        #region Public methods

        public override int Compare(Light x, Light y)
        {
            if (x.Owner == null || y.Owner == null)
                return 0;

            // constant attenuation is always more important than linear
            if (x.ConstantAttenuation > y.ConstantAttenuation)
                return -1;
            else if (y.ConstantAttenuation > x.ConstantAttenuation)
                return 1;

            Vector3 xPosition = x.Position;
            Vector3 yPosition = y.Position;

            Vector3 xDistance = new Vector3(
                MathHelper.Max(Math.Abs(xPosition.X - WorldBox.Max.X), Math.Abs(xPosition.X - WorldBox.Min.X)),
                MathHelper.Max(Math.Abs(xPosition.Y - WorldBox.Max.Y), Math.Abs(xPosition.Y - WorldBox.Min.Y)),
                MathHelper.Max(Math.Abs(xPosition.Z - WorldBox.Max.Z), Math.Abs(xPosition.Z - WorldBox.Min.Z)));

            Vector3 yDistance = new Vector3(
                MathHelper.Max(Math.Abs(yPosition.X - WorldBox.Max.X), Math.Abs(yPosition.X - WorldBox.Min.X)),
                MathHelper.Max(Math.Abs(yPosition.Y - WorldBox.Max.Y), Math.Abs(yPosition.Y - WorldBox.Min.Y)),
                MathHelper.Max(Math.Abs(yPosition.Z - WorldBox.Max.Z), Math.Abs(yPosition.Z - WorldBox.Min.Z)));

            // higher values mean the light has less of an effect
            if ((xDistance.Length() * x.LinearAttenuation) < (yDistance.Length() * y.LinearAttenuation))
                return -1;
            else
                return 1;
        }

        #endregion
    }
}
