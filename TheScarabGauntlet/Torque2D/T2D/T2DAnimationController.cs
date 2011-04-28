//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Used for controlling animations. Determines when frame changes should occur, looping and ending of animations.
    /// </summary>
    public class T2DAnimationController
    {

        #region Public Properties

        /// <summary>
        /// Specifies whether or not this animation controller has been fully initialized.
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }



        /// <summary>
        /// Specifies the total duration of the animation. This time is divided up amongst the frames. If there are more frames 
        /// in the animation the time spent on each frame will be less.
        /// </summary>
        public float AnimationDuration
        {
            get { return _animationDuration; }
            set
            {
                _animationDuration = value > 0.0f ? value : 0.1f;
                _isDirty = true;
            }
        }



        /// <summary>
        /// Specifies the animation's time scale. This is not cumulative: it will only affect subsequent calls to 
        /// AdvanceAnimation, not previous ones.
        /// </summary>
        public float AnimationTimeScale
        {
            get { return _animationTimeScale; }
            set { _animationTimeScale = value; }
        }



        /// <summary>
        /// Specifies the total number of frames in the animation.
        /// </summary>
        public int AnimationFrameCount
        {
            get { return _animationFrameCount; }
            set
            {
                _animationFrameCount = value;
                _isDirty = true;
            }
        }



        /// <summary>
        /// Specifies whether or not to repeat the animation. If true, the animation will cycle.
        /// </summary>
        public bool AnimationCycle
        {
            get { return _animationCycle; }
            set { _animationCycle = value; }
        }



        /// <summary>
        /// Specifies to choose a random starting frame rather than using the StartFrame property.
        /// </summary>
        public bool RandomStart
        {
            get { return _randomStart; }
            set { _randomStart = value; }
        }



        /// <summary>
        /// Specifies the desired frame of the animation to begin. If the frame is beyond the FinalFrame, zero will be used.
        /// This property is ignored if RandomStart is set to true.
        /// </summary>
        public int StartFrame
        {
            get { return _startFrame; }
            set { _startFrame = value; }
        }



        /// <summary>
        /// Returns the current animation frame index.
        /// </summary>
        public int CurrentFrame
        {
            get { return _currentFrameIndex; }
        }



        /// <summary>
        /// Returns the final frame index.
        /// </summary>
        public int FinalFrame
        {
            get
            {
                if (_isDirty)
                    _CalculateAnimationTime();

                return _maxFrameIndex;
            }
        }



        /// <summary>
        /// Returns the current animation time.
        /// </summary>
        public float CurrentTime
        {
            // just a quick note on this wackyness (this is not a mistake, although it might look weird):
            // this property returns current time because that represents a time within the current animation's
            // duration. the set method assigns to _totalTimeElapsed because that's what _currentTime is derived 
            // from from in AdvanceAnimation (_currentTime = _totalTimeElapsed % _animationDuration;). Assigning
            // directly to _currentTime wouldn't do jack, as it would be overwritten during the next update. 
            // we still update _currentTime anyway just so people don't freak out when they set it and see that
            // the value has apparently not changed. :)
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                _totalTimeElapsed = _currentTime;
            }
        }



        /// <summary>
        /// Returns true if the animation has completed.
        /// </summary>
        public bool AnimationFinished
        {
            get { return _animationFinished; }
            set { _animationFinished = value; }
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Sets the current animation frame and updates the animation.
        /// </summary>
        /// <param name="frameIndex">frame index to set the animation to</param>
        public bool SetAnimationFrame(uint frameIndex)
        {
            // make sure the frame is valid
            if (frameIndex < 0 || frameIndex > FinalFrame)
            {
                Assert.Fatal(false, "doh! Animation frame index invalid!");
                return false;
            }

            // calculate current time
            _totalTimeElapsed = frameIndex * _frameDuration + System.Single.Epsilon;

            // do an immediate animation update.
            UpdateAnimation();

            return true;
        }



        /// <summary>
        /// Resets the current time and animation finished status.
        /// </summary>
        public void ResetAnimation()
        {
            // call InitAnimation
            // (this wasn't really useful before, but leaving it here so as not to break anything)
            InitAnimation();
        }



        /// <summary>
        /// Initializes the animation and prepares it to recieve updates.
        /// </summary>
        /// <returns>Returns true if it was able to play the animation successfully</returns>
        public bool InitAnimation()
        {
            // make sure there is at least one frame specified
            if (_animationFrameCount <= 0)
            {
                Assert.Fatal(false, "Invalid animation data.");
                return false;
            }

            // make sure we have the most recent animation time
            if (_isDirty)
                _CalculateAnimationTime();

            // are we using a random start time?
            if (_randomStart)
            {
                Random r = new Random();

                // set the current time based on a random percent of the total time
                _totalTimeElapsed = (float)(_animationDuration * r.NextDouble());
            }
            else
            {
                // make sure we have a valid start frame
                if (_startFrame < 0 || _startFrame > _maxFrameIndex)
                    _startFrame = 0;

                // assign the current time based on the starting frame
                _totalTimeElapsed = _startFrame * _frameDuration;
            }

            // reset animation finished flag and last frame index
            _lastFrameIndex = -1;
            _animationFinished = false;

            // do an immediate update so currentFrame get's a proper value
            UpdateAnimation();

            // set the initialized flag to true
            _isInitialized = true;

            return true;
        }

        /// <summary>
        /// Updates the current frame, current time, and animation finished fields based on the current data without advancing the animation.
        /// </summary>
        /// <returns>True if the update resulted in a change of the current frame index.</returns>
        public bool UpdateAnimation()
        {
            return AdvanceAnimation(0.0f);
        }



        /// <summary>
        /// Updates and determines the current frame based on the time delta.
        /// </summary>
        /// <param name="dt">The change in time to impart on the animation.</param>
        /// <returns>True if there has been a frame change, false otherwise.</returns>
        public bool AdvanceAnimation(float dt)
        {
            Assert.Fatal(_animationFrameCount > 0, "doh! Invalid animation data.");

            // check if our animation time data is stale and update if necessary
            if (_isDirty)
                _CalculateAnimationTime();

            // update the current time based on the elapsed time and the animation time scale property
            _totalTimeElapsed += dt * _animationTimeScale;

            if (_totalTimeElapsed >= _animationDuration)
            {
                // Animation has finished
                _animationFinished = true;

                if (!_animationCycle)
                    _totalTimeElapsed = _animationDuration - (_frameDuration * 0.5f);
            }

            // update current mod time
            _currentTime = _totalTimeElapsed % _animationDuration;

            // figure out which frame we think we're on
            int currentFrame = (int)(_currentTime / _frameDuration);

            // if it's a valid frame, assign it
            if (currentFrame >= 0 && currentFrame <= _maxFrameIndex)
                _currentFrameIndex = currentFrame;

            // check if frame changed
            bool frameChanged = _currentFrameIndex != _lastFrameIndex;

            // set last frame index for next update
            _lastFrameIndex = _currentFrameIndex;

            // Return Frame-Changed Flag.
            return frameChanged;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Calculates the per frame integration time.
        /// </summary>
        private void _CalculateAnimationTime()
        {
            // make sure we have at leats one frame
            if (_animationFrameCount <= 0)
                return;

            // update the last frame index
            _maxFrameIndex = _animationFrameCount - 1;

            // calculate time per frame
            _frameDuration = _animationDuration / (float)_animationFrameCount;

            // reset total elapsed time to _currentTime
            _totalTimeElapsed = _currentTime;

            // reset dirty flag
            _isDirty = false;
        }

        #endregion


        #region Private, protected, internal fields

        private bool _isInitialized;
        private int _animationFrameCount;
        private bool _animationCycle;
        private bool _randomStart;
        private Int32 _startFrame = -1;
        private float _animationDuration;
        private float _animationTimeScale = 1.0f;

        private int _lastFrameIndex;
        private int _currentFrameIndex;
        private int _maxFrameIndex;
        private float _frameDuration;
        private float _totalTimeElapsed = 0.0f;
        private float _currentTime;
        private bool _animationFinished;
        private bool _isDirty;

        #endregion
    }
}
