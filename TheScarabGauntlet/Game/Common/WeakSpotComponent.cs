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
    public class WeakSpotComponent : TorqueComponent
    {
        protected int health;
        protected int armor;

        #region Properties
        public int Health
        {
            get { return health; }
            set { health = value; }
        }
        public int Armor
        {
            get { return armor; }
            set { armor = value; }
        }
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            WeakSpotComponent obj2 = obj as WeakSpotComponent;

            obj2.Health = Health;
            obj2.Armor = Armor;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            // Set the type of the melee objec to a damage region typ so it collides with damage
            // trigger objects 
            SceneObject.SetObjectType(ExtPlatformerData.DamageRegionObjectType, true);

            // Make sure we tell the melee object what to collide with, in this case
            // it will only collide with damage trigger objects (melee polygons)
            if(SceneObject.Collision.CollidesWith.Equals(TorqueObjectType.AllObjects))
                SceneObject.Collision.CollidesWith = ExtPlatformerData.MeleeDamageObjectType;

            if (SceneObject.IsMounted)
                SceneObject.Physics.ProcessCollisionsAtRest = true;
            //SceneObject.CollisionsEnabled = false;

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }

        public void TakeDamage(int damage, bool ignoreArmor)
        {
            int totalDamage = damage;

            if (!ignoreArmor)
                totalDamage -= armor;

            health -= totalDamage;

            if (health <= 0)
            {
                //ActorComponent actor = targetObject.Components.FindComponent<ActorComponent>();

               // if (actor != null)
                 //   actor.Die();

                SceneObject.MarkForDelete = true;
            }
        }

        /// <summary>
        /// Mounts the owner scene object to the target object.
        /// </summary>
        public void EnableCollision()
        {
            SceneObject.CollisionsEnabled = true;
        }

        /// <summary>
        /// Dismounts the owner scene object from the target object.
        /// </summary>
        public void DisableCollision()
        {
            SceneObject.CollisionsEnabled = false;
        }
    }
}