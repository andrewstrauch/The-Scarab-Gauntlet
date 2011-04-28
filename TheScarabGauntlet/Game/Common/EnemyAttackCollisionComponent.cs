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

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class EnemyAttackCollisionComponent : AttackCollisionComponent
    {
        #region Properties
        [TorqueXmlSchemaType]
        public static T2DOnCollisionDelegate EnemyCollision
        {
            get { return MeleeCollision; }
        }
        #endregion

        protected override void SetupCollision()
        {
            // Set the type of the melee object to a damage trigger type so it collides with enemies
            SceneObject.SetObjectType(ExtPlatformerData.EnemyDamageObjectType, true);//PlatformerData.DamageTriggerObjecType, true);//PlatformerData.DamageTriggerObjecType, true);
            //SceneObject.ObjectType -= ExtPlatformerData.MeleeDamageObjectType;

            // Make sure we tell the melee object what to collide with,  in this case
            // it will only collide with enemies (non-player actors)
            SceneObject.Collision.CollidesWith = PlatformerData.PlayerObjectType;
            //SceneObject.Collision.CollidesWith -= PlatformerData.EnemyObjectType;

        }

        /// <summary>
        /// T2DOnCollision delegate to handle damage between the melee scene object and enemies
        /// </summary>
        /// <param name="myObject">The melee scene object mounted on the player</param>
        /// <param name="theirObject">The enemy scene object</param>
        /// <param name="info">Collision information</param>
        /// <param name="resolve">How the collision will be resolved</param>
        /// <param name="physicsMaterial">The type of material that would affect the physics</param>
        public static void MeleeCollision(T2DSceneObject myObject, T2DSceneObject theirObject,
            T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            int damage = myObject.Components.FindComponent<AttackCollisionComponent>().Damage;

            if (theirObject.TestObjectType(PlatformerData.PlayerObjectType))
            {
                PlayerActorComponent actor = theirObject.Components.FindComponent<PlayerActorComponent>();

                // Deal damage to the enemy
                if (actor != null)
                    actor.TakeDamage(damage, myObject, true, true);
            }

            myObject.MarkForDelete = true;
        }
    }
}
