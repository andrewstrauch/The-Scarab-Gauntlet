using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents
{
    [TorqueXmlSchemaType]
    public class HulkActorComponent : EnemyActorComponent
    {
        protected T2DAnimationData meleeAnim;
        protected T2DAnimationData launchAnim;
        protected T2DAnimationData introAnim;
        protected Timer attackTimer;
        protected float meleeCoolDown;
        protected float projectileCoolDown;
        protected bool launchingProjectile = false;
        private T2DSceneObject meleeRotationController;
        private T2DSceneObject shoulderCannonObject;
        protected SwipeAttackComponent meleeComponent;
        protected WeaponComponent shoulderCannon;

        #region Properties
        public T2DAnimationData MeleeAnim
        {
            get { return meleeAnim; }
            set { meleeAnim = value; }
        }
        public T2DAnimationData LaunchAnim
        {
            get { return launchAnim; }
            set { launchAnim = value; }
        }
        public T2DAnimationData IntroAnim
        {
            get { return introAnim; }
            set { introAnim = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "500")]
        public float MeleeCoolDown
        {
            get { return meleeCoolDown; }
            set { meleeCoolDown = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "2000")]
        public float ProjectileCoolDown
        {
            get { return projectileCoolDown; }
            set { projectileCoolDown = value; }
        }
        public T2DSceneObject MeleeRotationController
        {
            get { return meleeRotationController; }
            set { meleeRotationController = value; }
        }
        public T2DSceneObject ShoulderCannonObject
        {
            get { return shoulderCannonObject; }
            set { shoulderCannonObject = value; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            HulkActorComponent obj2 = obj as HulkActorComponent;

            obj2.MeleeAnim = MeleeAnim;
            obj2.LaunchAnim = LaunchAnim;
            obj2.MeleeCoolDown = MeleeCoolDown;
            obj2.ProjectileCoolDown = ProjectileCoolDown;
        }

        public override void Attack()
        {
            if (!Alive)
                return;

            if(readyToAttack)
            {
                string animationState;

                if ((((AIHybridController)Controller).InAttackRange))
                {
                    ActionAnim = MeleeAnim;
                    animationState = "swipe";
                }
                else
                {
                    ActionAnim = LaunchAnim;
                    animationState = "shoot";
                }

                if(!AnimatedSprite.IsAnimationPlaying)
                    FSM.Instance.SetState(_animationManager, animationState);
            }
                        
            //if(launchingProjectile)
            //    if (AnimatedSprite.CurrentFrame == AnimatedSprite.FinalFrame)
            //        ; // Fire off projectiles

            this.HorizontalStop();
        }

        public void PlayIntro()
        {
            ActionAnim = IntroAnim;
            FSM.Instance.SetState(_animationManager, "intro");
        }

        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            if (!Alive && !AnimatedSprite.IsAnimationPlaying)
            {
                ReplacementComponent replacment = _actor.Components.FindComponent<ReplacementComponent>();

                if (replacment != null)
                {
                    replacment.ReplaceObject();
                }
            }

            if (attackTimer.Expired)
            {
                readyToAttack = true;
                attackTimer.Reset();
            }
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            attackTimer = new Timer("hulkAttackTimer");

            attackTimer.MillisecondsUntilExpire = meleeCoolDown;

            if(meleeRotationController != null)
                meleeComponent = meleeRotationController.Components.FindComponent<SwipeAttackComponent>();

            if (shoulderCannonObject != null)
                shoulderCannon = shoulderCannonObject.Components.FindComponent<WeaponComponent>();

            //SceneObject.Collision.CollidesWith -= ExtPlatformerData.MeleeDamageObjectType;

            return true;
        }

        protected override void _createAnimationManager()
        {
            _animationManager = new HulkActorAnimationManager(this);
        }

        public class HulkActorAnimationManager : ActorAnimationManager
        {
            private HulkActorComponent actorComponent;

            public HulkActorAnimationManager(HulkActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
            }

            protected override void _registerAnimStates()
            {
                base._registerAnimStates();

                FSM.Instance.RegisterState<MeleeAttackState>(this, "swipe");
                FSM.Instance.RegisterState<ProjectileAttackState>(this, "shoot");
                FSM.Instance.RegisterState<IntroState>(this, "intro");
            }

            public class IntroState : ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.HorizontalStop();
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }
            public class AttackState : ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;


                    actorAnimMgr.actorComponent.HorizontalStop();
                }
                public override string Execute(IFSMObject obj)
                {
                    return base.Execute(obj); 
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr == null)
                        return;

                    actorAnimMgr.actorComponent.attackTimer.Start();
                    
                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        actorAnimMgr.actorComponent.ReadyToAttack = false;
                }
            }

            public class MeleeAttackState : AttackState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);
                }

                public override string Execute(IFSMObject obj)
                {
                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr.actorComponent.AnimatedSprite.CurrentFrame == 9)
                        //if (((AIHybridController)actorAnimMgr.actorComponent.Controller).InAttackRange)
                        actorAnimMgr.actorComponent.meleeComponent.StartAttack();

                    return base.Execute(obj);
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);
                }
            }

            public class ProjectileAttackState : AttackState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);
                }

                public override string Execute(IFSMObject obj)
                {
                    HulkActorAnimationManager actorAnimMgr = obj as HulkActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (actorAnimMgr.actorComponent.AnimatedSprite.CurrentFrame == 8)
                        actorAnimMgr.actorComponent.shoulderCannon.Fire();

                    return base.Execute(obj);
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);
                }
            }
        }
    }
}
