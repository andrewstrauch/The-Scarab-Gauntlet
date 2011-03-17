using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using GarageGames.Torque.T2D;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Materials;
using GarageGames.Torque.GameUtil;

using GarageGames.Torque.PlatformerFramework;
using PlatformerStarter.Player;

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class PlayerActorComponent : ActorComponent
    {
        #region Private Members
        private bool spawning = false;
        private bool isInvincible = false;
        private float maxGlideFallSpeed = 40;
        private float lightAttackCoolDown;
        private float heavyAttackCoolDown;
        private float baseShotCoolDown;
        private float invincibilityLength;
        private T2DAnimationData spawnAnim;
        private T2DAnimationData fallDeathAnim;
        private T2DAnimationData hitDeathAnim;
        private T2DAnimationData jumpPunchAnim;
        private T2DAnimationData jumpSwipeAnim;
        private T2DAnimationData punchAnim;
        private T2DAnimationData swipeAnim;
        private AttackCollisionComponent attackComponent;
        private T2DSceneObject punchMeleeTemplate;
        private T2DSceneObject swipeMeleeTemplate;
        private PlayerActions attackActions;
        private HealthBar_GUI healthbar;
        #endregion

        #region Public Properties
        public int LightAttackCoolDown
        {
            get { return (int)lightAttackCoolDown; }
            set { lightAttackCoolDown = (float)value; }
        }

        public int HeavyAttackCoolDown
        {
            get { return (int)heavyAttackCoolDown; }
            set { heavyAttackCoolDown = (float)value; }
        }

        public int BaseShotCoolDown
        {
            get { return (int)baseShotCoolDown; }
            set { baseShotCoolDown = (float)value; }
        }

        public int InvincibilityLength
        {
            get { return (int)invincibilityLength; }
            set { invincibilityLength = (int)value; }
        }

        public bool IsInvincible
        {
            get { return isInvincible; }
        }

        public T2DAnimationData SpawnAnim
        {
            get { return spawnAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "Spawn animation should not cycle.  Specify an animation that doesn't cycle.");

                spawnAnim = value;
            }
        }
        public T2DAnimationData PunchAnim
        {
            get { return punchAnim; }
            set 
            { 
                if(value != null)
                    Assert.Fatal(!value.AnimationCycle, "Punch animation should not cycle.  Specify an animation that doesn't cycle.");

                punchAnim = value;
            }
        }
        public T2DAnimationData JumpPunchAnim
        {
            get { return jumpPunchAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "Jump-Punch animation should not cycle.  Specify an animation that doesn't cycle.");

                jumpPunchAnim = value;
            }
        }
        public T2DAnimationData SwipeAnim
        {
            get { return swipeAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "Punch animation should not cycle.  Specify an animation that doesn't cycle.");

                swipeAnim = value;
            }
        }

        public T2DAnimationData JumpSwipeAnim
        {
            get { return jumpSwipeAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "Jump-Punch animation should not cycle.  Specify an animation that doesn't cycle.");

                jumpSwipeAnim = value;
            }
        }

        public T2DAnimationData FallDeathAnim
        {
            get { return fallDeathAnim; }
            set { fallDeathAnim = value; }
        }

        public T2DAnimationData HitDeathAnim
        {
            get { return hitDeathAnim; }
            set { hitDeathAnim = value; }
        }
        #endregion

        #region Public Routines
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            PlayerActorComponent obj2 = obj as PlayerActorComponent;

            obj2.SpawnAnim = SpawnAnim;
            obj2.PunchAnim = PunchAnim;
            obj2.SwipeAnim = SwipeAnim;
            obj2.JumpSwipeAnim = JumpSwipeAnim;
            obj2.JumpPunchAnim = JumpPunchAnim;
            obj2.FallDeathAnim = FallDeathAnim;
        }

        /// <summary>
        /// Makes the player shoot a projectile in the direction she is facing
        /// </summary>
        public void Shoot()
        {
            if (attackActions.GetAction("baseShot").ReadyToAct)
            {
                WeaponComponent weapon = SceneObject.Components.FindComponent<WeaponComponent>();

                if (weapon != null)
                {
                    Vector2 fireDirection;

                    ActionAnim = punchAnim;
                    FSM.Instance.SetState(_animationManager, "magicAttack");

                    if (SceneObject.FlipX)
                        fireDirection = new Vector2(-1, 0);
                    else
                        fireDirection = new Vector2(1, 0);

                    weapon.FireAt(fireDirection);

                    this.HorizontalStop();
                }
            }
        }


        /// <summary>
        /// Makes the player "swipe"
        /// </summary>
        public void Swipe()
        {
            if (attackActions.GetAction("heavyAttack").ReadyToAct)
            {
                if (!OnGround)
                    ActionAnim = jumpSwipeAnim;
                else
                    ActionAnim = swipeAnim;

                T2DSceneObject attackObject = swipeMeleeTemplate.Clone() as T2DSceneObject;

                Attack(attackObject, "heavyAttack");
            }
        }

        /// <summary>
        /// Makes the player "punch"
        /// </summary>
        public void Punch()
        {
            if (attackActions.GetAction("lightAttack").ReadyToAct)
            {
                // set the current action animation to punch
                if (!OnGround)
                    ActionAnim = jumpPunchAnim;
                else
                    ActionAnim = punchAnim;

                T2DSceneObject attackObject = punchMeleeTemplate.Clone() as T2DSceneObject;

                Attack(attackObject, "lightAttack");
            }
        }

        /// <summary>
        /// Mounts or dismounts the attack object based on the current frame of the animation.
        /// </summary> 
        public void CheckAttack()
        {
            if (!attackComponent.Mounted && AnimatedSprite.CurrentFrame >= attackComponent.MountFrame)
                attackComponent.MountAttack();
            else if (attackComponent.Mounted && AnimatedSprite.CurrentFrame >= attackComponent.DismountFrame)
                attackComponent.DismountAttack();
        }

        /// <summary>
        /// Updates the GUI to reflect the player's current health.
        /// </summary>
        public void UpdateHealthBar()
        {
            healthbar.DisplayedHealth = (int)Health;
        }

        public void ApplyDamageEffects()
        {
            isInvincible = true;
            //SceneObject.ObjectType -= PlatformerData.ActorObjectType;
            SceneObject.CollisionsEnabled = false;  // IT'S A BUG!!!!
            attackActions.GetAction("invincibility").Timer.Start();
            attackActions.GetAction("invincibility").ReadyToAct = false;
        }

        public override bool HealDamage(float damage, T2DSceneObject sourceObject)
        {
            bool healed = base.HealDamage(damage, sourceObject);

            UpdateHealthBar();

            return healed;
        }

        /// <summary>
        /// Switches the current death animation to the fall death animation.
        /// </summary>
        public void SwitchToFallDeath()
        {
            DieAnim = FallDeathAnim;
        }

        public void ToggleControl()
        {
            if (this.IsPossessed)
                Controller.UnpossessMover(this);
            else
                Controller.PossessMover(this);
        }

        public void AddGoldCrystal()
        {
            ++healthbar.NumCollectedCrystals;
        }

        #endregion

        #region Private Routines
        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            // update the melee timer
            TimerManager.Instance.Update(TorqueEngineComponent.Instance.GameTime);

            attackActions.CheckActionTimers();

            if (isInvincible)
            {
                int time = attackActions.GetRemainingActionTime("invincibility");
                if (time % 2 == 0)
                    SceneObject.Visible = false;
                else
                    SceneObject.Visible = true;
            }
            else
                SceneObject.CollisionsEnabled = true;
            //else
              //  SceneObject.ObjectType += PlatformerData.ActorObjectType;
                //SceneObject.CollisionsEnabled = true;
        }

        private void ToggleInvincibility()
        {
            SceneObject.Visible = true;
            isInvincible = false;
        }

        private void Attack(T2DSceneObject attackObject, string meleeState)
        {
            // switch to the "action" state
            FSM.Instance.SetState(_animationManager, meleeState);

            if (attackObject != null)
            {
                attackObject.Position = SceneObject.Position;

                // Flip the damage poly if the player is facing the other way
                attackObject.FlipX = AnimatedSprite.FlipX;

                TorqueObjectDatabase.Instance.Register(attackObject);
                attackComponent = attackObject.Components.FindComponent<AttackCollisionComponent>();
            }

            this.HorizontalStop();
            this._inheritedVelocity = Vector2.Zero;
            this._groundVelocity = Vector2.Zero;
        }



        protected override bool _OnRegister(GarageGames.Torque.Core.TorqueObject owner)
        {
            base._OnRegister(owner);

            // set the object type of the owner so other objects will know it's a player
            owner.SetObjectType(PlatformerData.PlayerObjectType, true);

            // create a new PlayerController and possess this actor with it
            PlayerController controller = new PlayerController();
            controller.PossessMover(this);
            //float coolDown = 1000;

            // Setup attack timers
            attackActions = new PlayerActions();
            attackActions.AddAction("lightAttack", lightAttackCoolDown);
            attackActions.AddAction("heavyAttack", heavyAttackCoolDown);
            attackActions.AddAction("baseShot", baseShotCoolDown);
            attackActions.AddAction("invincibility", invincibilityLength, new OnTimerEndDelegate(ToggleInvincibility));

            punchMeleeTemplate = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("punch");
            swipeMeleeTemplate = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("swipe");

            // Temp.  Add new collision types for the player and enemies
            //SceneObject.Collision.CollidesWith += PlatformerData.DamageTriggerObjecType;
            SceneObject.Collision.CollidesWith += ExtPlatformerData.EnemyDamageObjectType;
            SceneObject.Collision.CollidesWith += PlatformerData.EnemyObjectType;

            healthbar = new HealthBar_GUI();
            healthbar.DisplayedHealth = (int)Health;
            healthbar.NumCollectedCrystals = 0;
            GUICanvas.Instance.SetContentControl(healthbar);

#if TORQUE_CONSOLE
            CustomConsoleRoutinePool.Instance.RegisterMethod(HealPlayer);
            CustomConsoleRoutinePool.Instance.RegisterMethod(WarpToCheckpoint);
#endif

            return true;
        }

        #region Debug Routines

#if TORQUE_CONSOLE

        protected bool HealPlayer(out string error, string[] parameters)
        {
            error = null;

            if (parameters != null)
            {
                string sHealth = parameters[parameters.Length - 1];
                if (sHealth != null)
                {
                    int healAmt;
                    if (!int.TryParse(sHealth, out healAmt))
                    {
                        error = sHealth + " is not a valid input for this routine.";
                        return false;
                    }
                    else
                        HealDamage(healAmt, null);
                }
            }
            else
                HealDamage(MaxHealth, null);

            return true;
        }

        protected bool WarpToCheckpoint(out string error, string[] parameters)
        {
            error = null;

            if (parameters != null)
            {
                string checkpointName = parameters[0];
                T2DSceneObject checkpoint = TorqueObjectDatabase.Instance.FindObject(checkpointName) as T2DSceneObject;

                if (checkpoint != null)
                {
                    SceneObject.Position = checkpoint.Position;
                    return true;
                }

                error = checkpointName + " does not exist.";
            }
            else
                error = "Checkpoint name needed.  Please enter the name of a checkpoint.";

            return false;
        }

#endif
        #endregion
        protected override void _PostRegister()
        {
            base._PostRegister();

            // camera mounting
            T2DSceneCamera camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>("Camera");//_sceneObject.SceneGraph.Camera as T2DSceneCamera;
            if (camera != null)
            {
                if (camera.IsMounted)
                    camera.Dismount();

                camera.Position = _actor.Position + new Vector2(0, -1);
                camera.Mount(_actor, String.Empty, new Vector2(0, -1), 0.0f, true);
                camera.UseMountForce = true;
                camera.MountForce = 15;
                camera.UseCameraWorldLimits = false;
                camera.CameraWorldLimitMin = new Vector2(-1000, -1000);
                camera.CameraWorldLimitMax = new Vector2(1000, 1000);

                //jukeBox = new JukeBox("music.wav", Game.Instance.Content);

                //PlatformerStarter.Common.ParallaxManager.Instance.Target = SceneObject;
                ParallaxManager.Instance.ParallaxTarget = camera;
            }
        }

        protected override void _die(float damage, T2DSceneObject sourceObject)
        {
            UpdateHealthBar();

            base._die(damage, sourceObject);

            CheckpointManager.Instance.LoadCheckPoint();
        }

        protected override void _tookDamage(float damage, T2DSceneObject sourceObject)
        {
            base._tookDamage(damage, sourceObject);

            if (_alive)
            {
                Vector2 direction = _actor.Position - sourceObject.Position;
                direction.Normalize();

                _actor.Physics.VelocityY = -60;
                _moveSpeed = Vector2.Zero;
                _inheritedVelocity = new Vector2(direction.X * 90);
                _onGround = false;
            }

            UpdateHealthBar();
        }

        protected override void _respawn()
        {
            // makes sure we're allowed to respawn
            if (!_allowRespawn)
                return;

            // bring this player back to life and spawn it at it's spawn position
            // (specialized, we will wait until done spawning to turn collision and
            //  object type back on)

            _actor.FlipX = false;
            _actor.Physics.Velocity = Vector2.Zero;
            _moveSpeed = Vector2.Zero;
            _inheritedVelocity = Vector2.Zero;
            _actor.WarpToPosition(RespawnPosition, _actor.Rotation);
            _alive = true;
            _health = _maxHealth;

            UpdateHealthBar();
            // swith to the spawn anim state
            spawning = true;
            FSM.Instance.SetState(_animationManager, "spawn");
        }

        protected virtual void respawnAnimFinished()
        {
            // we're done respawning
            // turn collision and object type back on
            spawning = false;
            _actor.CollisionsEnabled = true;
            _actor.SetObjectType(PlatformerData.ActorObjectType, true);

            // tell the controller that this actor respawned
            if (Controller != null)
                (Controller as ActorController).ActorSpawned(this);
        }

        protected override void _registerPhysicsStates()
        {
            base._registerPhysicsStates();

            FSM.Instance.RegisterState<PlayerActorOnGroundState>(this, "onGround");
            FSM.Instance.RegisterState<PlayerActorInAirState>(this, "inAir");
            FSM.Instance.RegisterState<PlayerActorDeadState>(this, "dead");
        }

        protected override void _createAnimationManager()
        {
            _animationManager = new PlayerActorAnimationManager(this);
        }

        protected override void _initAnimationManager()
        {
            _soundBank = "amanda";
            _useAnimationManagerSoundEvents = true;
            _animationManager.SetSoundEvent(PunchAnim, "amanda_melee1");
            _animationManager.SetSoundEvent(SwipeAnim, "amanda_swipe1");
            _animationManager.SetSoundEvent(JumpAnim, "amanda_jump1");
            _animationManager.SetSoundEvent(RunJumpAnim, "amanda_jump1");
            _animationManager.SetSoundEvent(JumpPunchAnim, "amanda_melee1");
            _animationManager.SetSoundEvent(JumpSwipeAnim, "amanda_swipe1");

            _useAnimationStepSoundList = true;
            _animationManager.AddStepSoundFrame(RunAnim, 15, "amanda_run1");
            _animationManager.AddStepSoundFrame(RunAnim, 64, "amanda_run1");
        }
        #endregion

        #region Physics States
        protected class PlayerActorOnGroundState : OnGroundState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                base.UpdatePhysics(actor, elapsed);

                // cast the actor to a "player" actor
                PlayerActorComponent playerActor = actor as PlayerActorComponent;

                if (playerActor == null)
                    return;
            }
        }

        protected class PlayerActorInAirState : InAirState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                base.UpdatePhysics(actor, elapsed);

                // cast actor to a "player" actor
                PlayerActorComponent playerActor = actor as PlayerActorComponent;

                if (playerActor == null)
                    return;

                // set inherited velocity to 0 if we passed zero or we aren't moving
                if (!((playerActor._moveSpeed.X + playerActor._inheritedVelocity.X < 0) == (playerActor._previousTotalVelocityX < 0))
                    && playerActor._moveSpeed.X == 0)
                {
                    playerActor._moveSpeed.X += playerActor._inheritedVelocity.X;
                    playerActor._inheritedVelocity.X = 0;
                }

            }
        }

        protected class PlayerActorDeadState : DeadState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                base.UpdatePhysics(actor, elapsed);

                PlayerActorComponent playerActor = actor as PlayerActorComponent;

                if (playerActor == null)
                    return;

                if (playerActor.spawning)
                {
                    playerActor._actor.Physics.Velocity = Vector2.Zero;
                    return;
                }

                // we wanna wait for the animated sprite to disappear after animation
                if (!playerActor.AnimatedSprite.Visible)
                {
                    if (playerActor.Lives > 0)
                        playerActor._respawn();
                }

                // apply gravity to allow for the "jumpyness"
                playerActor._actor.Physics.VelocityX = 0;
                playerActor._actor.Physics.VelocityY += actor.Gravity * elapsed;
            }

            public override string Execute(IFSMObject obj)
            {
                PlayerActorComponent obj2 = obj as PlayerActorComponent;

                if (obj2.Alive && !obj2.spawning)
                    return "inAir";

                return null;
            }
        }
        #endregion

        #region AnimationManager
        public class PlayerActorAnimationManager : ActorAnimationManager
        {
            private PlayerActorComponent actorComponent;

            public PlayerActorAnimationManager(PlayerActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
            }

            protected override void _registerAnimStates()
            {
                base._registerAnimStates();

                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<JumpState>(this, "jump");
                FSM.Instance.RegisterState<FallState>(this, "fall");
                FSM.Instance.RegisterState<RunJumpState>(this, "runJump");
                FSM.Instance.RegisterState<RunFallState>(this, "runFall");
                FSM.Instance.RegisterState<ClimbJumpState>(this, "climbJump");
                FSM.Instance.RegisterState<SpawnState>(this, "spawn");
                FSM.Instance.RegisterState<LightAttackState>(this, "lightAttack");
                FSM.Instance.RegisterState<HeavyAttackState>(this, "heavyAttack");
                FSM.Instance.RegisterState<MagicAttackState>(this, "magicAttack");
               
                _currentState = FSM.Instance.GetState(this, "idle");
            }

            public class LightAttackState : ActorAnimationManager.ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.attackActions.GetAction("lightAttack").Timer.Start();
                    actorAnimMgr.actorComponent.attackActions.GetAction("lightAttack").ReadyToAct = false;
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    actorAnimMgr.actorComponent.CheckAttack();

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";                    

                    return null;
                }
            }

            public class HeavyAttackState : ActorAnimationManager.ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.attackActions.GetAction("heavyAttack").Timer.Start();
                    actorAnimMgr.actorComponent.attackActions.GetAction("heavyAttack").ReadyToAct = false;
                    
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    actorAnimMgr.actorComponent.CheckAttack();

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }

            public class MagicAttackState : ActorAnimationManager.ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.attackActions.GetAction("baseShot").Timer.Start();
                    actorAnimMgr.actorComponent.attackActions.GetAction("baseShot").ReadyToAct = false;                  
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    actorAnimMgr.actorComponent.CheckAttack();

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }

            new public class JumpState : ActorAnimationManager.JumpState
            {
                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.Alive)
                        return "die";

                    if (!actorAnimMgr.actorComponent._onGround)
                    {
                        if (actorAnimMgr.actorComponent._actor.Physics.VelocityX > 0)
                            return "fall";
                    }
                    else
                    {
                        if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                            return "run";
                        else
                            return "idle";
                    }

                    return null;
                }
            }

            new public class FallState : ActorAnimationManager.FallState
            {
                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if (actorAnimMgr.actorComponent.OnGround)
                    {
                        if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                            return "run";
                        else
                            return "idle";
                    }
                    return null;
                }
            }

            new public class RunJumpState : ActorAnimationManager.RunJumpState
            {
                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if (actorAnimMgr.actorComponent._Climbing)
                    {
                        if (actorAnimMgr.actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr.actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (!actorAnimMgr.actorComponent._onGround)
                    {
                        if (actorAnimMgr.actorComponent._actor.Physics.VelocityY > 0)
                        {
                            return "runFall";
                        }
                    }
                    else
                    {
                        if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                            return "run";
                        else
                            return "idle";
                    }

                    return null;
                }
            }

            new public class RunFallState : ActorAnimationManager.RunFallState
            {
                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if (actorAnimMgr.actorComponent.OnGround)
                    {
                        if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                            return "run";
                        else
                            return "idle";
                    }
                    return null;
                }
            }

            public class SpawnState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.SpawnAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    PlayerActorAnimationManager actorAnimMgr = obj as PlayerActorAnimationManager;

                    if (!actorAnimMgr.actorComponent._animatedSprite.IsAnimationPlaying)
                    {
                        actorAnimMgr.actorComponent.respawnAnimFinished();
                        return "fall";
                    }

                    return null;
                }
            }

            new public class DieState : ActorAnimationManager.DieState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    T2DSceneCamera camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>();

                    if (camera == null)
                        return;

                    camera.Dismount();
                }
            }
        }
        #endregion
    }
}
