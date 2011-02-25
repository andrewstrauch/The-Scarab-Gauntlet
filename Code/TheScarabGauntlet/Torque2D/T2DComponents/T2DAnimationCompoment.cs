//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Add this component to a T2DSceneObject in order to animate particular
    /// float interfaces.  Float interfaces are exposed by other components in
    /// the RegisterInterfaces call and can be looked up by name and type.  This
    /// component animates ValueInterfaces which are exposed as "float" type.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DAnimationComponent : TorqueComponent, IAnimatedObject
    {
        /// <summary>
        /// Describes the animation of a float ValueInterface.
        /// </summary>
        public class Animation
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Name of interface to animate.
            /// </summary>
            public string ParameterName
            {
                get { return _parameterName; }
                set { _parameterName = value; }
            }



            /// <summary>
            /// Duration of animation.
            /// </summary>
            public float Duration
            {
                get { return _duration; }
                set { _duration = value; }
            }



            /// <summary>
            /// Minimum value of the animation.
            /// </summary>
            public float MinValue
            {
                get { return _minValue; }
                set { _minValue = value; }
            }



            /// <summary>
            /// Maximum value of the animation.
            /// </summary>
            public float MaxValue
            {
                get { return _maxValue; }
                set { _maxValue = value; }
            }



            /// <summary>
            /// If true this will be a ping pong animation (animates back and forth between 
            /// minimum and maximum value.
            /// </summary>
            public bool PingPong
            {
                get { return _pingPong; }
                set { _pingPong = value; }
            }



            /// <summary>
            /// If true this will be a cyclic animation (starts over at the beginning when done).
            /// </summary>
            public bool Cyclic
            {
                get { return _cyclic; }
                set { _cyclic = value; }
            }



            /// <summary>
            /// If true the animation will start at a random location within the sequence.
            /// </summary>
            public bool RandomStart
            {
                get { return _randomStart; }
                set { _randomStart = value; }
            }



            /// <summary>
            /// Used in conjunction with CurveValue to determine the animation curve.  CurveValue
            /// provides keyframe data for the animation to pass through.  CurveRange determines
            /// when those keyframes will occur.  Note that a polynomial is fit to all the data
            /// so interpolation will not be linear.  If CurveRange is supplied then all values
            /// must be between 0 and duration.  If CurveRange is NOT supplied, an even
            /// distribution is assumed between 0 and duration.
            /// </summary>
            public float[] CurveRange
            {
                get { return _curveRange; }
                set { _curveRange = value; _curveDirty = true; }
            }



            /// <summary>
            /// Used in conjunction with CurveRange to determine the animation curve.  CurveValue
            /// provides keyframe data for the animation to pass through.  CurveRange determines
            /// when those keyframes will occur.  Note that a polynomial is fit to all the data
            /// so interpolation will not be linear.
            /// </summary>
            public float[] CurveValue
            {
                get { return _curveValue; }
                set { _curveValue = value; _curveDirty = true; }
            }

            #endregion


            #region Private, protected, internal methods

            internal float[] _CurveRange
            {
                get { if (_curveDirty) _computeCurve(); return _curveRange; }
                set { _curveRange = value; _curveDirty = true; }
            }



            internal float[] _CurveValue
            {
                get { if (_curveDirty) _computeCurve(); return _curveValue; }
                set { _curveValue = value; _curveDirty = true; }
            }



            void _verifyRange()
            {
                // Make sure we're in order
                Assert.Fatal(_curveRange != null, "should not get in here with null curve range");
                for (int i = 1; i < _curveRange.Length; i++)
                {
                    if (_curveRange[i - 1] > _curveRange[i])
                    {
                        Assert.Fatal(false, "curve range not in order");
                    }
                }
                // Note: technically ok if these are out of order, but it is likely the case that
                // we'll get better fits if they are, so we choose to be strict here.
            }



            void _computeCurve()
            {
                if (_curveValue == null || _curveValue.Length == 0)
                {
                    // interpolate linearly from min to max between 0 to duration
                    _curveRange = null;
                    _curveValue = new float[2];
                    _curveValue[0] = MinValue;
                    _curveValue[1] = MaxValue;
                    _curveFit = new float[2];
                    PolyFit.FitData(0.0f, Duration, _curveValue, _curveFit);
                }
                else if (_curveRange == null)
                {
                    // generate curve based on equal steps
                    _curveFit = new float[_curveValue.Length];
                    PolyFit.FitData(0.0f, Duration, _curveValue, _curveFit);
                }
                else if (_curveRange.Length == _curveValue.Length)
                {
                    // deluxe case
                    _verifyRange();

                    _curveFit = new float[_curveValue.Length];
                    PolyFit.FitData(_curveRange, _curveValue, _curveFit);
                }
                else
                {
                    // mis-match between range and value...assert so they know it's a bug but
                    // go along happily just using value
                    Assert.Fatal(false, "Animation range and value have different element counts");
                    _curveRange = null;
                    _computeCurve();
                }
                _curveDirty = false;
            }

            #endregion


            #region Private, protected, internal fields

            internal string _parameterName;
            internal float _duration;
            internal float _minValue;
            internal float _maxValue;
            internal bool _pingPong;
            internal bool _cyclic;
            internal bool _randomStart;
            internal float[] _curveValue;
            internal float[] _curveRange;
            internal float[] _curveFit;
            internal bool _curveDirty = true;

            #endregion
        }


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// List of animations this component can play.
        /// </summary>
        [TorqueCloneIgnore] // we copy these manually in CopyTo
        public List<Animation> Animations
        {
            get { return _animations; }
            internal set { _animations = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Start indexed animation.
        /// </summary>
        /// <param name="animationIdx">Animation index.</param>
        public void PlayAnimation(int animationIdx)
        {
            Assert.Fatal(animationIdx >= 0 && animationIdx < _animations.Count, "Animation index out of range");
            _UpdateAnimationCounts();

            AnimationInstance inst = _animationInstance[animationIdx];
            if (inst.Started && !inst.Finished)
                inst.Paused = false;
            else
                inst.Started = false;

            _animationInstance[animationIdx] = inst;
        }



        /// <summary>
        /// Pause indexed animation.  A paused animation which is re-started will not
        /// be initialized again (so if it was at the end of the sequence and was neither
        /// cyclic or ping pong it will not play again, it will also not choose a random
        /// start location even if RandomStart property is true).
        /// </summary>
        /// <param name="animationIdx">Animation index.</param>
        public void PauseAnimation(int animationIdx)
        {
            Assert.Fatal(animationIdx >= 0 && animationIdx < _animations.Count, "Animation index out of range");
            _UpdateAnimationCounts();

            AnimationInstance inst = _animationInstance[animationIdx];
            inst.Paused = true;
            _animationInstance[animationIdx] = inst;
        }



        /// <summary>
        /// Stop indexed animation.  A stopped animation which is re-started will be
        /// initialized (it will start at a random place in the sequence if RandomStart is true
        /// and it will continue playing even if was previously stopped for being at the end).
        /// </summary>
        /// <param name="animationIdx">Animation index.</param>
        public void StopAnimation(int animationIdx)
        {
            Assert.Fatal(animationIdx >= 0 && animationIdx < _animations.Count, "Animation index out of range");
            _UpdateAnimationCounts();

            AnimationInstance inst = _animationInstance[animationIdx];
            inst.Finished = true;
            _animationInstance[animationIdx] = inst;
        }



        /// <summary>
        /// Set the time scale for the index animation.  Time scale is multiplied by time when updating,
        /// so a scale of 0 is similar to pausing the animation and a scale of 2 will play it twice as
        /// fast.
        /// </summary>
        /// <param name="animationIdx">Animation index.</param>
        /// <param name="scale">Time scale.</param>
        public void SetAnimationScale(int animationIdx, float scale)
        {
            Assert.Fatal(animationIdx >= 0 && animationIdx < _animations.Count, "Animation index out of range");
            _UpdateAnimationCounts();

            AnimationInstance inst = _animationInstance[animationIdx];
            inst.Scale = scale != 0.0f ? scale : 1.0f;
            _animationInstance[animationIdx] = inst;
        }



        public void UpdateAnimation(float dt)
        {
            _UpdateAnimationCounts();

            for (int i = 0; i < _animations.Count; i++)
            {
                Animation animData = _animations[i];
                AnimationInstance animInst = _animationInstance[i];
                if (!animInst.Started)
                    _InitAnimation(animData, ref animInst);
                if (animInst.Finished || animInst.Paused)
                    continue;

                float time = animInst.Time;
                time += dt * animInst.Scale;

                // flip and shift time if going backwards...saves us from duplicating wrap logic
                float flipScale = 1.0f;
                float flipAdd = 0.0f;
                if (animInst.Scale < 0.0f)
                {
                    flipScale = -1.0f;
                    flipAdd = animData.Duration;
                }
                time = time * flipScale + flipAdd;

                if (time > animData.Duration)
                {
                    if (animData.Cyclic)
                    {
                        time = time % animData.Duration;
                    }
                    else if (animData.PingPong)
                    {
                        time = time % (2.0f * animData.Duration);
                        if (time > animData.Duration)
                            time = 2.0f * animData.Duration - time;
                        animInst.Scale *= -1.0f;
                    }
                    else
                        animInst.Finished = true;
                }

                time = time * flipScale + flipAdd;
                animInst.Time = time;
                _animationInstance[i] = animInst;
                if (animInst.Finished)
                    continue;

                // compute the new value and clamp to range
                float val;
                if (animData._CurveRange == null)
                    val = PolyFit.ComputeData(time, 0.0f, animData.Duration, animData._curveFit);
                else
                    val = PolyFit.ComputeData(time, animData._CurveRange, animData._curveFit);
                val = MathHelper.Clamp(val, animData.MinValue, animData.MaxValue);

                // Set parameter value -- note special case for vector2's
                if (animInst.Parameter != null)
                    animInst.Parameter.Value = val;
                else if (animInst.ParameterX != null)
                    animInst.ParameterX.Value = new Vector2(val, animInst.ParameterX.Value.Y);
                else
                    animInst.ParameterY.Value = new Vector2(animInst.ParameterY.Value.X, val);
            }
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DAnimationComponent obj2 = (T2DAnimationComponent)obj;
            obj2._animations.Clear();
            for (int i = 0; i < _animations.Count; i++)
                obj2._animations.Add(_animations[i]);
            obj2._animationInstance.Clear();
            for (int i = 0; i < _animationInstance.Count; i++)
            {
                AnimationInstance inst = _animationInstance[i];

                inst.Parameter = null;
                if (inst.Started && !inst.Finished)
                    // pause is ok, but mid-play is converted to restart
                    inst.Started = false;

                obj2._animationInstance.Add(inst);
            }
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            ProcessList.Instance.AddAnimationCallback(Owner, this);

            return true;
        }



        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }



        protected void _UpdateAnimationCounts()
        {
            if (_animations.Count != _animationInstance.Count)
            {
                if (_animations.Count < _animationInstance.Count)
                    _animationInstance.RemoveRange(_animations.Count, _animationInstance.Count - _animations.Count);
                else
                    while (_animations.Count != _animationInstance.Count)
                        _animationInstance.Add(new AnimationInstance());
            }
        }



        protected void _InitAnimation(Animation data, ref AnimationInstance instance)
        {
            instance.Started = true;
            instance.Finished = false;

            // set parameter value -- note special case for vector2's
            instance.Parameter = Owner.Components.GetInterface<ValueInterface<float>>("float", data.ParameterName);
            if (instance.Parameter == null && data.ParameterName.EndsWith(".x", StringComparison.CurrentCultureIgnoreCase))
            {
                String param = data.ParameterName.Substring(0, data.ParameterName.Length - 2);
                instance.ParameterX = Owner.Components.GetInterface<ValueInterface<Vector2>>("vector2", param);
            }
            else if (instance.Parameter == null && data.ParameterName.EndsWith(".y", StringComparison.CurrentCultureIgnoreCase))
            {
                String param = data.ParameterName.Substring(0, data.ParameterName.Length - 2);
                instance.ParameterY = Owner.Components.GetInterface<ValueInterface<Vector2>>("vector2", param);
            }

            if (instance.Parameter == null && instance.ParameterX == null && instance.ParameterY == null)
            {
                instance.Finished = true;
                return;
            }

            if (instance.Scale == 0.0f)
                instance.Scale = 1.0f;

            if (data.RandomStart)
            {
                instance.Time = TorqueUtil.GetRandomFloat(data.Duration);
                if (data.PingPong && TorqueUtil.GetRandomFloat() < 0.5f)
                    instance.Scale *= -1.0f;
            }
            else if (instance.Scale > 0.0f)
                instance.Time = 0.0f;
            else
                instance.Time = data.Duration;

            instance.Started = true;
            instance.Finished = false;
            instance.Paused = false;
        }

        #endregion


        #region Private, protected, internal fields

        protected struct AnimationInstance
        {
            public ValueInterface<float> Parameter;
            public ValueInterface<Vector2> ParameterX;
            public ValueInterface<Vector2> ParameterY;
            public float Time;
            public float Scale;
            public bool Started;
            public bool Finished;
            public bool Paused;
        }

        List<Animation> _animations = new List<Animation>();
        List<AnimationInstance> _animationInstance = new List<AnimationInstance>();

        #endregion
    }
}
