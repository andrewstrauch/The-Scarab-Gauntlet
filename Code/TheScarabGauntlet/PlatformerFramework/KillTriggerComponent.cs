//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A component to be added to a scene object. The trigger will instantly kill Actors that enter the scene object's boundaries.
    /// For dealing damage in increments (such as with 'spikes' or similar objects), use a HazardComponent rather than a KillTriggerComponent.
    /// </summary>
    [TorqueXmlSchemaType]
    public class KillTriggerComponent : DirectionalTriggerComponent
    {
        //======================================================
        #region Private, protected, internal methods

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            if (actor == null)
                return;

            actor.Kill(Owner as T2DSceneObject);
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            SceneObject.SetObjectType(PlatformerData.DamageTriggerObjecType, true);

            return true;
        }

        #endregion
    }
}