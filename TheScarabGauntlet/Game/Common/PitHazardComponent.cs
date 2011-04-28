using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class PitHazardComponent : HazardComponent
    {
        #region Private Routines

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            PlayerActorComponent player = theirObject.Components.FindComponent<PlayerActorComponent>();

            if (player == null)
                return;
            else
                player.SwitchToFallDeath();

            base._onEnter(ourObject, theirObject, info);
        }
        #endregion

    }
}
