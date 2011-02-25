//**btp_replace(namespace GarageGames.VCSTemplates.ItemTemplates,namespace PlatformerStarter.Enemies.ActorComponents.Level1)
//**btp_replace(class TestClass,public class BairneActorComponent)
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

namespace PlatformerStarter.Enemies.ActorComponents.Level1
{
    public class BairneAnimations
    {
        public T2DAnimationData LeftRun;
        public T2DAnimationData RightRun;
        public T2DAnimationData LeftLaunch;
        public T2DAnimationData RightLaunch;
        public T2DAnimationData LeftDie;
        public T2DAnimationData RightDie;
        public T2DAnimationData LeftSwipe;
        public T2DAnimationData RightSwipe;
    }

    [TorqueXmlSchemaType]
    public class BairneActorComponent : EnemyActorComponent
    {
        #region Private Members
        private BairneAnimations animations = new BairneAnimations();
        private Timer attackTimer;
        private float meleeCoolDown;
        private float projectileCoolDown;
        #endregion

        #region Public Properties
        public BairneAnimations Animations
        {
            get { return animations; }
            set { animations = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BairneActorComponent obj2 = obj as BairneActorComponent;
            obj2.Animations = Animations;
        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }
        #endregion

    }
}
