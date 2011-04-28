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
using GarageGames.Torque.XNA;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class ProjectileComponent : TorqueComponent, ITickObject
    {
        #region Private members
        private Timer deathTimer;
        private int mSecondsToLive;
        private float gravity;
        private int damage;
        #endregion

        #region Properties

        [TorqueXmlSchemaType(DefaultValue = "2000")]
        public int MilliSecondsToLive
        {
            get { return mSecondsToLive; }
            set { mSecondsToLive = value; }
        }

        public float Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }

        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        public static T2DOnCollisionDelegate ProjectileCollision
        {
            get { return ProjCollision; }
        }

        #endregion

        //======================================================
        #region Public routines

        public virtual void ProcessTick(Move move, float dt)
        {
            if (deathTimer.Expired)
            {
                SceneObject.MarkForDelete = true;
            }
            else
                SceneObject.Physics.VelocityY += gravity * (dt * 100);
        }

        public virtual void InterpolateTick(float k)
        {
            // todo: interpolate between ticks as needed here
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ProjectileComponent obj2 = obj as ProjectileComponent;

            obj2.Gravity = Gravity;
            obj2.MilliSecondsToLive = MilliSecondsToLive;
            obj2.Damage = Damage;
        
        }

        public static void ProjCollision(T2DSceneObject projectile, T2DSceneObject targetObject,
            T2DCollisionInfo into, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial material)
        {
            int damage = projectile.Components.FindComponent<ProjectileComponent>().Damage;

            if (targetObject.TestObjectType(PlatformerData.ActorObjectType))
            {
                PlayerActorComponent actor = targetObject.Components.FindComponent<PlayerActorComponent>();

                // Deal damage to the enemy
                if (actor != null)
                    actor.TakeDamage(damage, projectile, true, true);
            }

            projectile.CollisionsEnabled = false;
            projectile.MarkForDelete = true;
        }
        #endregion

        //======================================================
        #region Private routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            SceneObject.SetObjectType(ExtPlatformerData.ProjectileObjectType, true);

            SceneObject.Collision.CollidesWith = PlatformerData.PlayerObjectType;

            deathTimer = new Timer("projectile");
            deathTimer.MillisecondsUntilExpire = mSecondsToLive;
            
            deathTimer.Start();
            
            return true;
        }

        protected override void _OnUnregister()
        {
            TimerManager.Instance.Remove(deathTimer);

            base._OnUnregister();
        }
        #endregion

    }
}
