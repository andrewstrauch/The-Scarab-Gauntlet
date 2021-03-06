//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright � GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GameUtil;


namespace GarageGames.Torque.PlatformerFramework
{
    public partial class ActorComponent : MoveComponent, IFSMObject
    {
        /// <summary>
        /// A helper class to facilitate the extraction of animation transition data from the level file.
        /// </summary>
        public class XMLTransitionData
        {
            //======================================================
            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The name of the animation state the Actor is transitioning from.
            /// </summary>
            public string FromState
            {
                get { return _fromState; }
                set { _fromState = value; }
            }

            /// <summary>
            /// The name of the animation state the Actor is transitioning to.
            /// </summary>
            public string ToState
            {
                get { return _toState; }
                set { _toState = value; }
            }

            /// <summary>
            /// The animation the Actor should play when transitioning between FromState and ToState.
            /// </summary>
            public T2DAnimationData Animation
            {
                get { return _animation; }
                set { _animation = value; }
            }


            #endregion

            //======================================================
            #region Private, protected, internal fields

            private string _fromState;
            private string _toState;
            private T2DAnimationData _animation = new T2DAnimationData();

            #endregion
        }

        /// <summary>
        /// State-based animation manager for Actors. Allows the user to specify any number of animations and transition between them
        /// based on the state of the Actor. Allows the user to specify transitional animations to play between any two animation states.
        /// Also allows the user to specify a sound cue to play at the beginning of any animation using sound events. Users may also specify
        /// sound cues for specific frames of an animation using step sounds (named for their most likely use: footstep sounds).
        /// </summary>
        public class ActorAnimationManager : IFSMObject
        {
            //======================================================
            #region Constructors

            /// <summary>
            /// Constructor. Stores the ActorComponent that this animation manager is associated with and calls _registerAnimStates.
            /// </summary>
            /// <param name="actorComponent">The ActorComponent associated with this animation manager.</param>
            public ActorAnimationManager(ActorComponent actorComponent)
            {
                // get a reference to the actor component
                _actorComponent = actorComponent;

                // register animation states
                _registerAnimStates();
            }

            #endregion

            //======================================================
            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The current animation state.
            /// </summary>
            public FSMState CurrentState
            {
                get { return _currentState; }
                set { _currentState = value; }
            }

            /// <summary>
            /// The previous animation state.
            /// </summary>
            public FSMState PreviousState
            {
                get { return _previousState; }
                set { _previousState = value; }
            }

            /// <summary>
            /// The hash table of animation transitions used by this animation manager. Use the SetTransition method to add transitions.
            /// </summary>
            public Hashtable Transitions
            {
                get { return _transitions; }
                set { _transitions = value; }
            }

            /// <summary>
            /// The hash table of animation sound events used by this animation manager. Use the SetSoundEvent method to add sound events.
            /// </summary>
            public Hashtable SoundEvents
            {
                get { return _soundEvents; }
                set { _soundEvents = value; }
            }

            /// <summary>
            /// The hash table of step sounds used by this animation manager. Use the AddStepSoundFrame method to add step sounds.
            /// </summary>
            public Hashtable StepSoundAnimations
            {
                get { return _stepSoundAnimations; }
                set { _stepSoundAnimations = value; }
            }

            /// <summary>
            /// The ammount of time (seconds) this animation manager will wait after the Actor falls off a platform before FallingFromGround will evaluate true.
            /// This is used to buffer the transition time from standing animations (run, idle, etc.) to falling animations (fall, runFall, etc.) to avoid
            /// noticeable jitter when passing over negligible changes in platform surfaces.
            /// </summary>
            public float StandToFallThreshold
            {
                get { return _standToFallThreshold; }
                set { _standToFallThreshold = value; }
            }

            /// <summary>
            /// True if the time since the Actor's last positive ground check is greater than StandToFallThreshold.
            /// </summary>
            public bool FallingFromGround
            {
                get { return TorqueEngineComponent.Instance.TorqueTime - _actorComponent._lastOnGroundTime > _standToFallThreshold * 1000; }
            }

            #endregion

            //======================================================
            #region Public methods

            /// <summary>
            /// Add a transitional animation to be played between the two specified animation states.
            /// </summary>
            /// <param name="from">The name of the state to be transitioning from.</param>
            /// <param name="to">The name of the state to be transitioning to.</param>
            /// <param name="objectDBName">The name of the animation to be played when transitioning from FromState to ToState.</param>
            public void SetTransition(string from, string to, string objectDBName)
            {
                SetTransition(from, to, TorqueObjectDatabase.Instance.FindObject(objectDBName) as T2DAnimationData);
            }

            /// <summary>
            /// Add a transitional animation to be played between the two specified animation states.
            /// </summary>
            /// <param name="from">The name of the state to be transitioning from.</param>
            /// <param name="to">The name of the state to be transitioning to.</param>
            /// <param name="animationData">The animation to be played when transitioning from FromState to ToState.</param>
            public void SetTransition(string from, string to, T2DAnimationData animationData)
            {
                if (animationData == null)
                    return;

                Assert.Fatal(!animationData.AnimationCycle, "Transitional animations should not cycle! \n\nDo not add looping animations to an Actor's transition list.");

                ClearTransition(from, to);

                string hashKey = from + "_to_" + to;
                _transitions.Add(hashKey, animationData);
            }

            /// <summary>
            /// Remove an existing transition between the two specified animation states.
            /// </summary>
            /// <param name="from">The name of the state to be transitioning from.</param>
            /// <param name="to">The name of the state to be trensitioning to.</param>
            public void ClearTransition(string from, string to)
            {
                string hashKey = from + "_to_" + to;

                if (_transitions.Contains(hashKey))
                    _transitions.Remove(hashKey);
            }

            /// <summary>
            /// Get the transitional animation to be played between the two specified states.
            /// </summary>
            /// <param name="from">The name of the state transitioning from.</param>
            /// <param name="to">The name of the state transitioning to.</param>
            /// <returns>The animation to be played between the two given animation states.</returns>
            public T2DAnimationData GetTransition(string from, string to)
            {
                string hashKey = from + "_to_" + to;
                return _transitions[hashKey] as T2DAnimationData;
            }


            /// <summary>
            /// Set a sound cue to be played immediately when the specified animation is played.
            /// </summary>
            /// <param name="animation">The animation to attach the sound cue to.</param>
            /// <param name="soundCueIndex">The name of the sound cue.</param>
            public void SetSoundEvent(T2DAnimationData animation, string soundCueIndex)
            {
                if (animation == null)
                    return;

                ClearSoundEvent(animation);

                _soundEvents.Add(animation.Name, soundCueIndex);
            }

            /// <summary>
            /// Remove a sound event from the specified animation.
            /// </summary>
            /// <param name="animation">The animation to clear the sound event from.</param>
            public void ClearSoundEvent(T2DAnimationData animation)
            {
                if (_soundEvents.Contains(animation.Name))
                    _soundEvents.Remove(animation.Name);
            }

            /// <summary>
            /// Get the sound cue to be played when the specified animation is played.
            /// </summary>
            /// <param name="animation">The animation to check for.</param>
            /// <returns>The sound cue associated with the specified animation.</returns>
            public string GetSoundEvent(T2DAnimationData animation)
            {
                if (_soundEvents.Contains(animation.Name))
                    return _soundEvents[animation.Name] as string;

                return null;
            }


            /// <summary>
            /// Adds a sound event to the specified frame of the specified animation.
            /// </summary>
            /// <param name="animation">The animation to add the step sound to.</param>
            /// <param name="frameIndex">The 0-based frame number of the specified animation on which to play the specified sound cue.</param>
            /// <param name="soundCueIndex">The sound cue to play on the specified frame of the specified animation.</param>
            public void AddStepSoundFrame(T2DAnimationData animation, int frameIndex, string soundCueIndex)
            {
                if (animation == null)
                    return;

                if (!_stepSoundAnimations.Contains(animation.Name))
                    _stepSoundAnimations.Add(animation.Name, new StepSoundTable(animation));

                StepSoundTable table = _stepSoundAnimations[animation.Name] as StepSoundTable;

                if (table != null)
                    table.AddStepFrame(frameIndex, soundCueIndex);
                else
                    ClearAnimationStepSounds(animation);
            }

            /// <summary>
            /// Remove any sound event from the specified frame of the specified animation.
            /// </summary>
            /// <param name="animation">The animation to remove the sound event from.</param>
            /// <param name="frame">The frame to remove the sound event from.</param>
            public void ClearStepSoundFrame(T2DAnimationData animation, int frame)
            {
                if (_stepSoundAnimations.Contains(animation.Name))
                {
                    StepSoundTable table = _stepSoundAnimations[animation.Name] as StepSoundTable;

                    if (table != null)
                        table.RemoveStepFrame(frame);
                    else
                        ClearAnimationStepSounds(animation);
                }
            }

            /// <summary>
            /// Removes all step sounds from the specified animation.
            /// </summary>
            /// <param name="animation">The animation to clear step sounds from.</param>
            public void ClearAnimationStepSounds(T2DAnimationData animation)
            {
                if (_stepSoundAnimations.Contains(animation.Name))
                {
                    StepSoundTable table = _stepSoundAnimations[animation.Name] as StepSoundTable;

                    if (table != null)
                        table.ClearTable();

                    _stepSoundAnimations.Remove(animation.Name);
                }
            }

            /// <summary>
            /// Get the step sound table associated with the specified animation.
            /// </summary>
            /// <param name="animation">The animation for which to retrieve the step sound table.</param>
            /// <returns>The step sound table associated with the specified animation.</returns>
            public StepSoundTable GetStepSoundTable(T2DAnimationData animation)
            {
                if (_stepSoundAnimations.Contains(animation.Name))
                    return _stepSoundAnimations[animation.Name] as StepSoundTable;

                return null;
            }

            public virtual void OnFrameChange(int frame)
            {
                // get the current animation on the actor sprite
                T2DAnimationData animation = _actorComponent._animatedSprite.AnimationData;

                // if the animation is bogus
                // or we are no longer supposed to be handling sound events for whatever reason
                if (animation == null
                    || !_actorComponent._useAnimationStepSoundList)
                {
                    // null the delegate for now
                    _actorComponent._animatedSprite.OnFrameChange = null;
                    return;
                }

                // get the sound table for this animation
                StepSoundTable table = GetStepSoundTable(animation);

                // make sure the table for this animation exists and has frames
                if (table == null || !table.HasFrames)
                {
                    // if not, null the delegate and return
                    _actorComponent._animatedSprite.OnFrameChange = null;
                    return;
                }

                // if the table is enabled and there is a sound for this frame, play it!
                // (oh, and make sure we aren't playing ground-based sounds when we're not actually on the ground)
                if (table.StepSounds.Contains(_actorComponent._animatedSprite.CurrentFrame) && table.Enabled
                    && !((animation == _actorComponent.IdleAnim || animation == _actorComponent.RunAnim) && !_actorComponent._onGround))
                {
                    float distance = Vector2.Distance((_actorComponent.SceneObject.SceneGraph.Camera as T2DSceneCamera).Position, _actorComponent._animatedSprite.Position);
                    SoundManager.Instance.PlaySound(_actorComponent._soundBank,
                        table.StepSounds[_actorComponent._animatedSprite.CurrentFrame] as string,
                        distance);
                }
            }

            #endregion

            //======================================================
            #region Private, protected, internal methods

            // register animation states
            /// <summary>
            /// This is used to register various animation states for this animation manager. Override this method to insert or replace a physics state.
            /// </summary>
            protected virtual void _registerAnimStates()
            {
                // register all our states
                // (this function exists to allow you overide and 
                // register different animation states if you need to)
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<JumpState>(this, "jump");
                FSM.Instance.RegisterState<FallState>(this, "fall");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<RunJumpState>(this, "runJump");
                FSM.Instance.RegisterState<RunFallState>(this, "runFall");
                FSM.Instance.RegisterState<SlideState>(this, "slide");
                FSM.Instance.RegisterState<ClimbIdleState>(this, "climbIdle");
                FSM.Instance.RegisterState<ClimbUpState>(this, "climbUp");
                FSM.Instance.RegisterState<ClimbDownState>(this, "climbDown");
                FSM.Instance.RegisterState<ClimbJumpState>(this, "climbJump");
                FSM.Instance.RegisterState<ActionState>(this, "action");
                FSM.Instance.RegisterState<DamageState>(this, "damage");
                FSM.Instance.RegisterState<DieState>(this, "die");

                // set the initial state
                _currentState = FSM.Instance.GetState(this, "idle");
            }

            /// <summary>
            /// Plays the specified animation on the Actor starting at frame 0. Also responsible for playing sound events.
            /// </summary>
            /// <param name="animation">The animation to be played.</param>
            protected virtual void _playAnimation(T2DAnimationData animation)
            {
                _playAnimation(animation, 0);
            }

            /// <summary>
            /// Plays the specified animation on the Actor starting at the specified frame. Also responsible for playing sound events.
            /// </summary>
            /// <param name="animation">The animation to be played.</param>
            /// <param name="frameIndex">The frame at which to start the animation.</param>
            protected virtual void _playAnimation(T2DAnimationData animation, uint frameIndex)
            {
                // make sure the animation exists
                // (and that we actually have an actor component)
                if (_actorComponent != null && animation != null)
                {
                    uint targetFrame = frameIndex < animation.AnimationFramesList.Count ? frameIndex : 0;
                    _actorComponent._animatedSprite.PlayAnimation(animation);
                    _actorComponent._animatedSprite.SetAnimationFrame(targetFrame);


                    // if we're currently supposed to be managing sound events for this actor...
                    if (_actorComponent._useAnimationManagerSoundEvents)
                    {
                        // grab the sound event for this animation
                        string soundEvent = GetSoundEvent(animation);

                        // play the sound event if one exists
                        if (soundEvent != null)
                        {
                            float distance = Vector2.Distance((_actorComponent.SceneObject.SceneGraph.Camera as T2DSceneCamera).Position, _actorComponent._animatedSprite.Position);
                            _currentSoundCue = SoundManager.Instance.PlaySound(_actorComponent._soundBank, soundEvent, distance);
                        }
                    }

                    // if we're supposed to be doing step sounds...
                    if (_actorComponent._useAnimationStepSoundList)
                    {
                        // grab the sound table for this animation
                        StepSoundTable table = GetStepSoundTable(animation);

                        // table has frames, enable OnFrameChange callback
                        if (table != null && table.HasFrames)
                        {
                            _actorComponent._animatedSprite.OnFrameChange = OnFrameChange;

                            // special case: check for step sound on starting frame and play
                            if (table.StepSounds.Contains(frameIndex) && table.Enabled)
                            {
                                float distance = Vector2.Distance((_actorComponent.SceneObject.SceneGraph.Camera as T2DSceneCamera).Position, _actorComponent._animatedSprite.Position);
                                _currentSoundCue = SoundManager.Instance.PlaySound(_actorComponent._soundBank,
                                                                table.StepSounds[frameIndex] as string,
                                                                distance);
                            }
                        }
                    }
                }
            }

            #endregion

            //======================================================
            #region Private, protected, internal fields

            protected FSMState _currentState;
            protected FSMState _previousState;
            private ActorComponent _actorComponent;

            protected float _standToFallThreshold = 0.2f;
            protected Hashtable _transitions = new Hashtable();
            protected bool _transitioning;
            protected T2DAnimationData _transitioningTo;
            protected Hashtable _soundEvents = new Hashtable();
            protected Hashtable _stepSoundAnimations = new Hashtable();
            protected Cue _currentSoundCue;

            #endregion



            /// <summary>
            /// A container class to keep track of step sound cues and frame indeces on a particular animation.
            /// </summary>
            public class StepSoundTable
            {
                //======================================================
                #region Constructors

                /// <summary>
                /// Constructor. Records the animation this tabls is associated with.
                /// </summary>
                /// <param name="animation"></param>
                public StepSoundTable(T2DAnimationData animation)
                {
                    _animation = animation;
                }

                #endregion

                //======================================================
                #region Public properties, operators, constants, and enums

                /// <summary>
                /// The animation associated with this table.
                /// </summary>
                public T2DAnimationData Animation
                {
                    get { return _animation; }
                }

                /// <summary>
                /// Specifies whether or not this table is enabled. If false, the animation manager will not play sounds from this table.
                /// </summary>
                public bool Enabled
                {
                    get { return _enabled; }
                    set { _enabled = value; }
                }

                /// <summary>
                /// The hash table of sound cues indexed by frame number.
                /// </summary>
                public Hashtable StepSounds
                {
                    get { return _stepSounds; }
                }

                /// <summary>
                /// Specifies whether or not this table contains frames.
                /// </summary>
                public bool HasFrames
                {
                    get { return _stepSounds.Count > 0; }
                }

                #endregion

                //======================================================
                #region Public methods

                /// <summary>
                /// Associates the specified sound cue to the specified frame.
                /// </summary>
                /// <param name="frame"></param>
                /// <param name="sound"></param>
                public void AddStepFrame(int frame, string sound)
                {
                    if (frame >= _animation.AnimationFramesList.Count || frame < 0)
                        return;

                    if (_stepSounds.Contains(frame))
                        _stepSounds.Remove(frame);

                    _stepSounds.Add(frame, sound);
                }

                /// <summary>
                /// Removes the sound cue from the specified frame.
                /// </summary>
                /// <param name="frame"></param>
                public void RemoveStepFrame(int frame)
                {
                    if (_stepSounds.Contains(frame))
                        _stepSounds.Remove(frame);
                }

                /// <summary>
                /// Removes all sound cues from the table.
                /// </summary>
                public void ClearTable()
                {
                    _stepSounds.Clear();
                }

                #endregion

                //======================================================
                #region Private, protected, internal fields

                protected T2DAnimationData _animation;
                protected bool _enabled = true;
                protected Hashtable _stepSounds = new Hashtable();

                #endregion
            }

            //======================================================
            #region Actor animation states

            /// <summary>
            /// Base animation state used by ActorAnimationManager. Handles core animation transition functionality.
            /// </summary>
            public abstract class AnimationState : FSMState
            {
                protected uint _targetFrame;

                public override void Enter(IFSMObject obj)
                {
                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    T2DAnimationData transition = actorAnimMgr.GetTransition(obj.PreviousState.StateName, obj.CurrentState.StateName);

                    if (transition != null)
                    {
                        actorAnimMgr._transitioning = true;
                        actorAnimMgr._playAnimation(transition, _targetFrame);
                    }

                    _targetFrame = 0;

                    if (actorAnimMgr._actorComponent._alive)
                        actorAnimMgr._actorComponent._actor.Visible = true;
                }

                public override string Execute(IFSMObject obj)
                {
                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (actorAnimMgr._transitioning && !actorAnimMgr._actorComponent._animatedSprite.IsAnimationPlaying)
                    {
                        actorAnimMgr._transitioning = false;
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo, _targetFrame);
                    }

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioning = false;

                    _targetFrame = 0;
                }
            }

            // animation states
            public class IdleState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.IdleAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (actorAnimMgr.FallingFromGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                            return "fall";
                    }

                    if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                        return "run";

                    return null;
                }
            }

            public class JumpState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.JumpAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (!actorAnimMgr._actorComponent._onGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                            return "fall";
                    }
                    else
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }

                    return null;
                }
            }

            public class FallState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.FallAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (actorAnimMgr._actorComponent._onGround)
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }

                    return null;
                }
            }

            public class RunState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.RunAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    if (actorAnimMgr._actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr._actorComponent._animatedSprite.AnimationTimeScale = actorAnimMgr._actorComponent._runAnimSpeedScale / actorAnimMgr._actorComponent._maxMoveSpeed;
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (actorAnimMgr.FallingFromGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                            return "runFall";
                    }

                    if (actorAnimMgr._actorComponent._slideAnim != null
                        && ((actorAnimMgr._actorComponent._moveSpeed.X > 0 && actorAnimMgr._actorComponent._moveLeft)
                            || (actorAnimMgr._actorComponent._moveSpeed.X < 0 && actorAnimMgr._actorComponent._moveRight)))
                        return "slide";

                    if ((!actorAnimMgr._actorComponent._scaleRunAnimBySpeed && !(actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight))
                        || (actorAnimMgr._actorComponent._scaleRunAnimBySpeed && actorAnimMgr._actorComponent._runAnimSpeedScale <= (actorAnimMgr._actorComponent._minRunAnimSpeedScale * actorAnimMgr._actorComponent._maxMoveSpeed)))
                        return "idle";

                    if (actorAnimMgr._actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr._actorComponent._animatedSprite.AnimationTimeScale = actorAnimMgr._actorComponent._runAnimSpeedScale / actorAnimMgr._actorComponent._maxMoveSpeed;

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._actorComponent._animatedSprite.AnimationTimeScale = 1.0f;
                }
            }

            public class RunJumpState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.RunJumpAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (!actorAnimMgr._actorComponent._onGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                            return "runFall";
                    }
                    else
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }

                    return null;
                }
            }

            public class RunFallState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.FallAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (actorAnimMgr._actorComponent._onGround)
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }

                    return null;
                }
            }

            public class SlideState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.SlideAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (actorAnimMgr.FallingFromGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                            return "fall";
                    }
                    else if ((actorAnimMgr._actorComponent._moveSpeed.X >= 0) == (!actorAnimMgr._actorComponent._moveLeft))
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }
 
                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;
                }
            }

            public class ClimbIdleState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.ClimbIdleAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";
                    }
                    else
                    {
                        if (!actorAnimMgr._actorComponent._onGround)
                            return "fall";
                        else
                            return "idle";
                    }

                    return null;
                }
            }

            public class ClimbUpState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.ClimbUpAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y == 0)
                            return "climbIdle";
                    }
                    else
                    {
                        if (!actorAnimMgr._actorComponent._onGround)
                            return "fall";
                        else
                            return "idle";
                    }

                    return null;
                }
            }

            public class ClimbDownState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.ClimbDownAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y == 0)
                            return "climbIdle";
                    }
                    else
                    {

                        if (!actorAnimMgr._actorComponent._onGround)
                            return "fall";
                        else
                            return "idle";
                    }

                    return null;
                }
            }

            public class ClimbJumpState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.ClimbJumpAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (actorAnimMgr._actorComponent._Climbing)
                    {
                        if (actorAnimMgr._actorComponent._moveSpeed.Y < 0)
                            return "climbUp";
                        else if (actorAnimMgr._actorComponent._moveSpeed.Y > 0)
                            return "climbDown";

                        return "climbIdle";
                    }

                    if (!actorAnimMgr._actorComponent._onGround)
                    {
                        if (actorAnimMgr._actorComponent._actor.Physics.VelocityY > 0)
                        {
                            if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                                return "runFall";
                            else
                                return "fall";
                        }
                    }
                    else
                    {
                        if (actorAnimMgr._actorComponent._moveLeft || actorAnimMgr._actorComponent._moveRight)
                            return "run";

                        return "idle";
                    }

                    return null;
                }
            }

            public class ActionState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.ActionAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent.OnGround)
                        actorAnimMgr._actorComponent.Actor.Physics.VelocityX = 0;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (!actorAnimMgr._actorComponent._animatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }

            public class DamageState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.DamageAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if (!actorAnimMgr._actorComponent._alive)
                        return "die";

                    if (!actorAnimMgr._actorComponent._animatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }

            public class DieState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr._actorComponent.DieAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    ActorAnimationManager actorAnimMgr = obj as ActorAnimationManager;

                    if (actorAnimMgr._actorComponent == null)
                        return null;

                    if(!actorAnimMgr._actorComponent._animatedSprite.IsAnimationPlaying)
                        actorAnimMgr._actorComponent.Actor.Visible = false;

                    if (actorAnimMgr._actorComponent._alive)
                       return "idle";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);
                }
            }

            #endregion
        }
    }
}
