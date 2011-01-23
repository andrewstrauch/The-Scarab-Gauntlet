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

namespace PlatformerStarter
{
    public enum SHOT_TYPE
    {
        STRAIGHT = 0, ARC, SHOTGUN,
    }

    [TorqueXmlSchemaType]
    public class WeaponComponent : TorqueComponent
    {
        #region Private members

        private int damage;
        private float shotSpeed;
        private float shotAngle;
        private string linkPointName;
        private T2DSceneObject projectileTemplate;
        private T2DSceneObject mountObject;

        #endregion

        #region Properties

        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "20")]
        public float ShotSpeed
        {
            get { return shotSpeed; }
            set { shotSpeed = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "45.0")]
        public float ShotAngle
        {
            get { return shotAngle; }
            set { shotAngle = value; }
        }

        public string LinkPointName
        {
            get { return linkPointName; }
            set { linkPointName = value; }
        }

        public T2DSceneObject ProjectileTemplate
        {
            get { return projectileTemplate; }
            set { projectileTemplate = value; }
        }

        public T2DSceneObject MountObject
        {
            get { return mountObject; }
            set { mountObject = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public routines

        /// <summary>
        /// Changes the direction that the projectile will be shot.
        /// </summary>
        public void ChangeHeading()
        {
            if (shotAngle <= 0)
                shotAngle += 180f;
            else
                shotAngle -= 180f;
        }

        /// <summary>
        /// Fires off a projectile in a direction based on given angle
        /// </summary>
        public void Fire()
        {
            if (projectileTemplate.IsTemplate)
            {
                T2DSceneObject projectile = projectileTemplate.Clone() as T2DSceneObject;
                projectile.Position = SceneObject.Position;              
                projectile.Physics.Velocity = T2DVectorUtil.VectorFromAngle(shotAngle) * shotSpeed;
                
                TorqueObjectDatabase.Instance.Register(projectile);
            }
        }

        /// <summary>
        /// Fires off a projectile in the given direction
        /// </summary>
        /// <param name="direction">The direction the projectile is to be shot at</param>
        public void FireAt(Vector2 direction)
        {
            if (projectileTemplate != null)
            {
                T2DSceneObject projectile = projectileTemplate.Clone() as T2DSceneObject;
                projectile.Position = SceneObject.Position;
                projectile.Physics.Velocity = direction * shotSpeed;

                TorqueObjectDatabase.Instance.Register(projectile);
            }
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            WeaponComponent obj2 = obj as WeaponComponent;
            obj2.Damage = Damage;
            obj2.ShotAngle = ShotAngle;
            obj2.ShotSpeed = ShotSpeed;
            obj2.ProjectileTemplate = ProjectileTemplate;
            obj2.LinkPointName = LinkPointName;
            obj2.MountObject = MountObject;
        }

        #endregion

        #region Private routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            if (linkPointName != "" && mountObject != null)
                SceneObject.Mount(mountObject, linkPointName, true);

            return true;
        }

        protected override void _OnUnregister()
        {
            SceneObject.MarkForDelete = true;

            base._OnUnregister();
        }
        #endregion

    }
}
