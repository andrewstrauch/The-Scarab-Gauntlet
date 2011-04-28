using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

namespace PlatformerStarter.Common
{
    [TorqueXmlSchemaType]
    public class CustomCollision
    {
        #region Public Properties
        /// <summary>
        /// OnCollision delegate that forces two objects to stay still when colliding.
        /// </summary>
        public static T2DResolveCollisionDelegate ImmovableCollision
        {
            get { return ResolveCollision; }
        }
        /// <summary>
        /// OnCollision delegate that forces the player to go into a "ghost-like" state 
        /// that disables all collision for a certain amount of time.
        /// </summary>
        public static T2DResolveCollisionDelegate GhostCollision
        {
            get { return EtherealCollision; }
        }
        #endregion

        #region Private Routines
        public static void ResolveCollision(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info,
            T2DCollisionMaterial material, bool handleBoth)
        {
            theirObject.Physics.Velocity = Vector2.Zero;
            ourObject.Physics.Velocity = Vector2.Zero;
            ourObject.MarkForDelete = true;
        }

        public static void EtherealCollision(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info,
            T2DCollisionMaterial material, bool handleBoth)
        {
            // Start player's flashing animation to indicate she was hit.
            PlayerActorComponent actor = theirObject.Components.FindComponent<PlayerActorComponent>();

            if (actor != null && !actor.IsInvincible)
                actor.ApplyDamageEffects();
        }
        #endregion
    }
}
