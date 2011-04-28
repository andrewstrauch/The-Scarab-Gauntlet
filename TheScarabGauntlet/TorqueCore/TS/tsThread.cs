//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Information about a sequence that is currently playing on a shape instance.
    /// </summary>
    public class Thread : IComparable
    {
        /// <summary>
        /// Stores data about the transition between two sequences.
        /// </summary>
        public struct TransitionData
        {
            // whether or not the thread is in transition
            internal bool _inTransition;

            // length of the transition
            internal float _duration;

            // transition position
            internal float _pos;

            // transition direction
            internal float _direction;

            // time Scale for _sequence we are transitioning to (during transition only)
            internal float _targetScale;

            // nodes controlled by this thread before the transition
            internal BitVector _oldRotationNodes;
            internal BitVector _oldTranslationNodes;
            internal BitVector _oldScaleNodes;

            // _sequence that was Set before transition began
            internal Sequence _oldSequence;

            // position of _sequence before transition began
            internal float _oldPos;
        }



        /// <summary>
        /// Stores path information used in determining when to fire triggers.
        /// </summary>
        struct Path
        {
            internal float _start;
            internal float _end;
            internal int _loop;
        }


        #region Constructors

        public Thread(ShapeInstance si)
        {
            _pos = 0;
            _timeScale = 1;
            _shapeInstance = si;

            if (si.GetShape().Sequences != null && si.GetShape().Sequences.Length != 0)
                _SetSequence(si.GetShape().Sequences[0], 0.0f);

            _priority = _sequence.Priority;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The current time of the thread.
        /// </summary>
        public float Time
        {
            get { return _transitionData._inTransition ? _transitionData._pos * _transitionData._duration : _pos * _sequence.Duration; }
            set { Position = _timeScale * value / Duration; }
        }



        /// <summary>
        /// The current position of the thread.
        /// </summary>
        public float Position
        {
            get { return _transitionData._inTransition ? _transitionData._pos : _pos; }
            set { AdvancePos(value - Position); }
        }



        /// <summary>
        /// Scalar value to slow down or speed up the playback of the thread.
        /// </summary>
        public float TimeScale
        {
            get { return _timeScale; }
            set { _timeScale = value; }
        }



        /// <summary>
        /// Whether or not the thread is currently transitioning between two sequences.
        /// </summary>
        public bool IsInTransition
        {
            get { return _transitionData._inTransition; }
        }



        /// <summary>
        /// The total amount of time the current sequence or transition plays.
        /// </summary>
        public float Duration
        {
            get { return _transitionData._inTransition ? _transitionData._duration : _sequence.Duration; }
        }



        /// <summary>
        /// The duration scaled by the time scale.
        /// </summary>
        public float ScaledDuration
        {
            get { return Duration / Math.Abs(_timeScale); }
        }



        /// <summary>
        /// The number of keyframes in the current sequence.
        /// </summary>
        public int KeyframeCount
        {
            get
            {
                Assert.Fatal(!_transitionData._inTransition, "TSThread.KeyframeCount - Not while in transition.");
                return _sequence.KeyframeCount + 1;
            }
        }



        /// <summary>
        /// The keyframe that is currently being played on the sequence.
        /// </summary>
        public int KeyframeNumber
        {
            get
            {
                Assert.Fatal(!_transitionData._inTransition, "TSThread.KeyframeNumber - Not while in transition.");
                return _keyNum1;
            }
            set
            {
                Assert.Fatal(value >= 0 && value <= _sequence.KeyframeCount, "TSThread.KeyframeNumber - Invalid frame specified.");
                Assert.Fatal(!_transitionData._inTransition, "TSThread.KeyframeNumber - Not while in transition.");

                _keyNum1 = _keyNum2 = value;
                _keyPos = 0;
                _pos = 0;
            }
        }



        /// <summary>
        /// The current sequence set on the thread.
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
        }



        /// <summary>
        /// The shape instance this thread is animating.
        /// </summary>
        public ShapeInstance ShapeInstance
        {
            get { return _shapeInstance; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets a new sequence on the thread by transitioning to it.
        /// </summary>
        /// <param name="seq">The sequence to transition to.</param>
        /// <param name="toPos">The position of the new sequence to start at.</param>
        /// <param name="duration">The length of the transition.</param>
        /// <param name="continuePlay">Whether or not to continue playing the thread.</param>
        public void TransitionToSequence(Sequence seq, float toPos, float duration, bool continuePlay)
        {
            Assert.Fatal(duration > 0.0f, "Thread.TransitionToSequence - Must pass a positive non zero duration.");

            Shape shape = _shapeInstance.GetShape();

            // make sure these nodes are smoothly interpolated to new positions...
            // basically, any node we controlled just prior to transition, or at any stage
            // of the transition is interpolated.  If we _start to transtion from A to B,
            // but before reaching B we transtion to C, we Interpolate all nodes controlled
            // by A, B, or C to their new position.
            if (_transitionData._inTransition)
            {
                _transitionData._oldRotationNodes.Overlap(_sequence.DoesRotationMatter);
                _transitionData._oldTranslationNodes.Overlap(_sequence.DoesTranslationMatter);
                _transitionData._oldScaleNodes.Overlap(_sequence.DoesScaleMatter);
            }
            else
            {
                _transitionData._oldRotationNodes.Copy(ref _sequence.DoesRotationMatter);
                _transitionData._oldTranslationNodes.Copy(ref _sequence.DoesTranslationMatter);
                _transitionData._oldScaleNodes.Copy(ref _sequence.DoesScaleMatter);
            }

            // Set time characteristics of transition
            _transitionData._oldSequence = _sequence;
            _transitionData._oldPos = _pos;
            _transitionData._duration = duration;
            _transitionData._pos = 0.0f;
            _transitionData._direction = _timeScale > 0.0f ? 1.0f : -1.0f;
            _transitionData._targetScale = continuePlay ? 1.0f : 0.0f;

            // in transition...
            _transitionData._inTransition = true;

            // Set target _sequence data
            _sequence = seq;
            _priority = _sequence.Priority;
            _pos = toPos;
            _makePath = _sequence.MakePath();

            // 1.0f doesn't exist on cyclic sequences
            if (_pos > 0.9999f && _sequence.IsCyclic())
                _pos = 0.9999f;

            // select keyframes
            _SelectKeyframes(_pos, _sequence, out _keyNum1, out _keyNum2, out _keyPos);
        }



        /// <summary>
        /// Advance the animation by an amount of time.
        /// </summary>
        /// <param name="delta">The time to advance.</param>
        public void AdvanceTime(float delta)
        {
            AdvancePos(_timeScale * delta / Duration);
        }



        /// <summary>
        /// Advance the animation by a position amount.
        /// </summary>
        /// <param name="delta">The amount to advance.</param>
        public void AdvancePos(float delta)
        {
            if (Math.Abs(delta) > 0.00001f)
            {
                // make dirty what this thread changes
                DirtyFlags dirtyFlags = _sequence.DirtyFlags;
                if (_transitionData._inTransition)
                    dirtyFlags |= DirtyFlags.TransformDirty;

                for (int i = 0; i < _shapeInstance.GetShape().SubShapeFirstNode.Length; i++)
                    _shapeInstance._dirtyFlags[i] |= dirtyFlags;
            }

            if (_transitionData._inTransition)
            {
                _transitionData._pos += _transitionData._direction * delta;
                if (_transitionData._pos < 0 || _transitionData._pos >= 1.0f)
                {
                    _shapeInstance.ClearTransition(this);
                    if (_transitionData._pos < 0.0f)
                        // return to old _sequence
                        _shapeInstance.SetSequence(this, _transitionData._oldSequence, _transitionData._oldPos);
                }

                // re-adjust delta to be correct time-wise
                delta *= _transitionData._targetScale * _transitionData._duration / _sequence.Duration;
            }

            // even if we are in a transition, keep playing the _sequence

            if (_makePath)
            {
                _path._start = _pos;
                _pos += delta;
                if (!_sequence.IsCyclic())
                {
                    _pos = MathHelper.Clamp(_pos, 0.0f, 1.0f);
                    _path._loop = 0;
                }
                else
                {
                    _path._loop = (int)_pos;
                    if (_pos < 0.0f)
                        _path._loop--;

                    _pos -= _path._loop;
                    // following necessary because of floating point roundoff errors
                    if (_pos < 0.0f) _pos += 1.0f;
                    if (_pos >= 1.0f) _pos -= 1.0f;
                }

                _path._end = _pos;
                _AnimateTriggers(); // do this automatically...no need for user to call it

                Assert.Fatal(_pos >= 0.0f && _pos <= 1.0f, "Thread.AdvancePos - Invalid thread position.");
                Assert.Fatal(!_sequence.IsCyclic() || _pos < 1.0f, "Thread.AdvancePos - Invalid thread position.");
            }
            else
            {
                _pos += delta;
                if (!_sequence.IsCyclic())
                {
                    _pos = MathHelper.Clamp(_pos, 0.0f, 1.0f);
                }
                else
                {
                    _pos -= (int)_pos;
                    // following necessary because of floating point roundoff errors
                    if (_pos < 0.0f) _pos += 1.0f;
                    if (_pos >= 1.0f) _pos -= 1.0f;
                }

                Assert.Fatal(_pos >= 0.0f && _pos <= 1.0f, "Thread.AdvancePos - Invalid thread position.");
                Assert.Fatal(!_sequence.IsCyclic() || _pos < 1.0f, "Thread.AdvancePos - Invalid thread position.");
            }

            // select keyframes
            _SelectKeyframes(_pos, _sequence, out _keyNum1, out _keyNum2, out _keyPos);
        }

        #endregion


        #region Private, protected, internal methods

        int IComparable.CompareTo(object o)
        {
            Thread other = o as Thread;
            if (other != null)
            {
                if (_sequence.IsBlend() == other._sequence.IsBlend())
                {
                    // both blend or neither blend, sort based on _priority only -- higher _priority first
                    return other._priority - _priority;
                }
                else
                {
                    // one is blend, the other is not...sort based on blend -- non-blended first
                    return _sequence.IsBlend() ? 1 : -1;
                }
            }

            return 0;
        }



        internal void _SetSequence(Sequence seq, float toPos)
        {
            Shape shape = _shapeInstance.GetShape();

            Assert.Fatal(shape != null && toPos >= 0.0f && toPos <= 1.0f, "Thread.SetSequence - Invalid shape handle, sequence number, or position.");

            _shapeInstance.ClearTransition(this);

            _sequence = seq;
            _priority = _sequence.Priority;
            _pos = toPos;
            _makePath = _sequence.MakePath();

            // 1.0f doesn't exist on cyclic sequences
            if (_pos > 0.9999f && _sequence.IsCyclic())
                _pos = 0.9999f;

            // select keyframes
            _SelectKeyframes(_pos, _sequence, out _keyNum1, out _keyNum2, out _keyPos);
        }



        void _SelectKeyframes(float pos, Sequence seq, out int k1, out int k2, out float kpos)
        {
            Shape shape = _shapeInstance.GetShape();
            int numKF = seq.KeyframeCount;
            float kf;

            if (seq.IsCyclic())
            {
                // cyclic _sequence:
                // _pos=0 and _pos=1 are equivalent, so we don't have a keyframe at _pos=1
                // last keyframe corresponds to _pos=n/(n-1) up to (not including) _pos=1
                // (where n == num keyframes)

                Assert.Fatal(pos >= 0.0f && pos < 1.0f, "Thread.SelectKeyframes - Invalid thread position.");

                kf = pos * (float)(numKF);

                // Set _keyPos
                kpos = kf - (int)kf;

                // make sure compiler doing what we want...
                Assert.Fatal(kpos >= 0.0f && kpos < 1.0f, "Thread.SelectKeyframes - Invalid thread position.");

                int kfIdx1 = (int)kf;

                // following assert could happen if pos1<1 && pos1==1...paradoxically...
                Assert.Fatal(kfIdx1 <= seq.KeyframeCount, "Thread.SelectKeyframes - Invalid keyframe.");

                int kfIdx2 = (kfIdx1 == seq.KeyframeCount - 1) ? 0 : kfIdx1 + 1;

                k1 = kfIdx1;
                k2 = kfIdx2;
            }
            else
            {
                // one-shot _sequence:
                // _pos=0 and _pos=1 are now different, so we have a keyframe at _pos=1
                // last keyframe corresponds to _pos=1
                // rest of the keyframes are equally spaced (so 1/(n-1) _pos units long)
                // (where n == num keyframes)

                Assert.Fatal(pos >= 0.0f && pos <= 1.0f, "Thread.SelectKeyframes - Invalid thread position.");

                if (pos == 1.0f)
                {
                    kpos = 0.0f;
                    k1 = seq.KeyframeCount - 1;
                    k2 = seq.KeyframeCount - 1;
                }
                else
                {
                    kf = pos * (float)(numKF - 1);

                    // Set _keyPos
                    kpos = kf - (int)kf;

                    int kfIdx1 = (int)kf;

                    // following assert could happen if pos1<1 && pos1==1...paradoxically...
                    Assert.Fatal(kfIdx1 < seq.KeyframeCount, "TSThread.SelectKeyFrames - Invalid keyframe.");

                    int kfIdx2 = kfIdx1 + 1;

                    k1 = kfIdx1;
                    k2 = kfIdx2;
                }
            }
        }



        void _AnimateTriggers()
        {
            if (_sequence.TriggerCount == 0)
                return;

            switch (_path._loop)
            {
                case -1:
                    _ActivateTriggers(_path._start, 0);
                    _ActivateTriggers(1, _path._end);
                    break;

                case 0:
                    _ActivateTriggers(_path._start, _path._end);
                    break;

                case 1:
                    _ActivateTriggers(_path._start, 1);
                    _ActivateTriggers(0, _path._end);
                    break;

                default:
                    {
                        if (_path._loop > 0)
                        {
                            _ActivateTriggers(_path._end, 1);
                            _ActivateTriggers(0, _path._end);
                        }
                        else
                        {
                            _ActivateTriggers(_path._end, 0);
                            _ActivateTriggers(1, _path._end);
                        }

                        break;
                    }
            }
        }



        void _ActivateTriggers(float a, float b)
        {
            Shape shape = _shapeInstance.GetShape();
            int firstTrigger = _sequence.FirstTrigger;
            int numTriggers = _sequence.TriggerCount;

            // first find triggers at position a and b
            // we assume there aren't many triggers, so
            // search is linear
            float lastPos = -1.0f;
            int aIndex = numTriggers + firstTrigger; // initialized to handle case where _pos past all triggers
            int bIndex = numTriggers + firstTrigger; // initialized to handle case where _pos past all triggers
            for (int i = firstTrigger; i < numTriggers + firstTrigger; i++)
            {
                // is a between this trigger and previous one...
                if (a > lastPos && a <= shape.Triggers[i].Pos)
                    aIndex = i;

                // is b between this trigger and previous one...
                if (b > lastPos && b <= shape.Triggers[i].Pos)
                    bIndex = i;

                lastPos = shape.Triggers[i].Pos;
            }

            // activate triggers between aIndex and bIndex (depends on _direction)
            if (aIndex <= bIndex)
            {
                for (int i = aIndex; i < bIndex; i++)
                {
                    int state = shape.Triggers[i].State;
                    bool on = (state & (int)Trigger.TriggerStates.StateOn) != 0;
                    _shapeInstance._SetTriggerStateBit(state & (int)Trigger.TriggerStates.StateMask, on);
                }
            }
            else
            {
                for (int i = aIndex - 1; i >= bIndex; i--)
                {
                    int state = shape.Triggers[i].State;
                    bool on = (state & (int)Trigger.TriggerStates.StateOn) != 0;
                    if ((state & (int)Trigger.TriggerStates.InvertOnReverse) != 0)
                        on = !on;

                    _shapeInstance._SetTriggerStateBit(state & (int)Trigger.TriggerStates.StateMask, on);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        int _priority;

        ShapeInstance _shapeInstance;
        Sequence _sequence;

        bool _makePath;
        Path _path;

        internal float _pos;
        internal float _timeScale;

        // Keyframe at or before current position
        internal int _keyNum1;
        // Keyframe at or after current position
        internal int _keyNum2;
        // Where between 2 keyframes we are
        internal float _keyPos;

        internal bool _blendDisabled = false;
        internal TransitionData _transitionData;

        #endregion
    }
}
