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



namespace GarageGames.Torque.Lighting
{
    /// <summary>
    /// A light component specific to 2D scenes.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DLightComponent : LightComponent
    {
        #region Public properties, operators, constants, and enums

        public override Vector3 Position
        {
            get
            {
                Vector3 position = base.Position;
                position.Z = 0.0f;
                return position;
            }
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            _sceneGroup = Owner as ISceneObject;

            if (!base._OnRegister(owner))
                return false;

            return true;
        }

        #endregion
    }


}
