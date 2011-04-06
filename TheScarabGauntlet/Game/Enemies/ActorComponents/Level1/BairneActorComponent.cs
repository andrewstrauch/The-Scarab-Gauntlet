//**btp_replace(namespace GarageGames.VCSTemplates.ItemTemplates,namespace PlatformerStarter.Enemies.ActorComponents.Level1)
//**btp_replace(class TestClass,public class BairneActorComponent)
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents.Level1
{
    public class BairneAnimations
    {
        public T2DAnimationData KushLaunchAnim;
        public T2DAnimationData DeathAnim;
        public T2DAnimationData SwipeAnim;
        public T2DAnimationData VineLaunchAnim;
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

        public override void Attack()
        {
        }

        public void Swipe()
        {
            if (!Alive)
                return;

            ActionAnim = Animations.SwipeAnim;

            FSM.Instance.SetState(_animationManager, "swipe");


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


        #region Animation Manager
        public class BairneActorAnimationManager : ActorAnimationManager
        {
            private BairneActorComponent actorComponent;
            private LuaAIController controller;
            //private Cue currentSound;

            public BairneActorAnimationManager(BairneActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
                this.controller = actorComponent.Controller as LuaAIController;
            }

            protected override void _registerAnimStates()
            {
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<AttackState>(this, "swipe");
                FSM.Instance.RegisterState<ActionState>(this, "kush_launch");
                FSM.Instance.RegisterState<ActionState>(this, "vine_launch");
                FSM.Instance.RegisterState<DieState>(this, "die");

                _currentState = FSM.Instance.GetState(this, "idle");
            }

            new public class IdleState : ActorAnimationManager.IdleState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;
                }

                public override string Execute(IFSMObject obj)
                {
                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    // Uncomment if we include a fall animation for the grunt
                    /* if(actorAnimMgr.FallingFromGround)
                         if(actorAnimMgr.actorComponent.Actor.Physics.VelocityY > 0)
                             return "fall";
                     */
                    if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                        return "run";

                    return null;
                }
            }

            new public class RunState : ActorAnimationManager.RunState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.RunAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    if (actorAnimMgr.actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr.actorComponent.AnimatedSprite.AnimationTimeScale = actorAnimMgr.actorComponent._runAnimSpeedScale / actorAnimMgr.actorComponent._maxMoveSpeed;

                    //actorAnimMgr.currentSound = SoundManager.Instance.PlaySound("grunt", "walk");
                }

                public override string Execute(IFSMObject obj)
                {
                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if ((!actorAnimMgr.actorComponent._scaleRunAnimBySpeed && !(actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight))
                        || (actorAnimMgr.actorComponent._scaleRunAnimBySpeed && actorAnimMgr.actorComponent._runAnimSpeedScale <= (actorAnimMgr.actorComponent._minRunAnimSpeedScale * actorAnimMgr.actorComponent._maxMoveSpeed)))
                        return "idle";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    ////if (actorAnimMgr.currentSound.IsPlaying)
                    //    actorAnimMgr.currentSound.Stop(AudioStopOptions.Immediate);
                }
            }

            public class AttackState : ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.Alive)
                        return "die";

                   // actorAnimMgr.actorComponent.CheckAttackFrame();

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                   // actorAnimMgr.actorComponent.meleeTimer.Start();
                }
            }
        }

        #endregion

    }
}
