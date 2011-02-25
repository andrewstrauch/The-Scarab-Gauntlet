//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Add this component to a T2DSceneObject to animate it's size property.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DSizeAnimComponent : TorqueComponent, ITickObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Rate of animation in X and Y
        /// </summary>
        public Vector2 AnimationRate
        {
            get { return _animationRate; }
            set
            {
                _animationRate = value;
                _isAnimatingSize = !Epsilon.VectorIsZero(_animationRate);
            }
        }



        /// <summary>
        /// Maximum size of animation.
        /// </summary>
        [TorqueXmlSchemaType]
        public Vector2 MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; _HasMaxSize = !Epsilon.VectorIsZero(_maxSize); }
        }



        /// <summary>
        /// Minimum size of animation.  The minimum size of any object is zero.
        /// </summary>
        [TorqueXmlSchemaType]
        public Vector2 MinSize
        {
            get { return _minSize; }
            set { _minSize = value; }
        }



        /// <summary>
        /// True if animation is a ping pong animation (goes back and forth between minimum and maximum).
        /// </summary>
        public bool PingPongAnimation
        {
            get { return _pingPong; }
            set { _pingPong = value; }
        }

        #endregion


        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DSizeAnimComponent obj2 = (T2DSizeAnimComponent)obj;
            obj2.AnimationRate = AnimationRate;
            obj2.MaxSize = MaxSize;
            obj2.MinSize = MinSize;
            obj2.PingPongAnimation = PingPongAnimation;
        }



        public void ProcessTick(Move move, float dt)
        {
            // update size if animation is enabled - note that this doesn't updated mounted objects
            if (_isAnimatingSize)
            {
                T2DSceneObject sceneObj = Owner as T2DSceneObject;
                if (sceneObj != null)
                {
                    sceneObj.Size += _animationRate * dt;

                    if (_HasMaxSize && (sceneObj.Size.X >= _maxSize.X || sceneObj.Size.Y >= _maxSize.Y))
                    {
                        Vector2 newSize = sceneObj.Size;

                        if (sceneObj.Size.X >= _maxSize.X)
                        {
                            newSize.X = _maxSize.X;
                            if (PingPongAnimation && _animationRate.X > 0.0f)
                                _animationRate.X *= -1;
                        }
                        if (sceneObj.Size.Y >= _maxSize.Y)
                        {
                            newSize.Y = _maxSize.Y;
                            if (PingPongAnimation && _animationRate.Y > 0.0f)
                                _animationRate.Y *= -1;
                        }

                        sceneObj.Size = newSize;
                    }

                    if (sceneObj.Size.X <= _minSize.X || sceneObj.Size.Y <= _minSize.Y)
                    {
                        Vector2 newSize = sceneObj.Size;

                        if (sceneObj.Size.X <= _minSize.X)
                        {
                            newSize.X = _minSize.X;
                            if (PingPongAnimation && _animationRate.X < 0.0f)
                                _animationRate.X *= -1;
                        }
                        if (sceneObj.Size.Y <= _minSize.Y)
                        {
                            newSize.Y = _minSize.Y;
                            if (PingPongAnimation && _animationRate.Y < 0.0f)
                                _animationRate.Y *= -1;
                        }

                        sceneObj.Size = newSize;
                    }
                }
            }
        }



        public void InterpolateTick(float k)
        {
        }



        /// <summary>
        /// Start animation over, setting size to the original starting size.
        /// </summary>
        public virtual void ResetAnimation()
        {
            if (_isAnimatingSize && (Owner is T2DSceneObject))
                (Owner as T2DSceneObject).Size = _initialSize;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            if (_isAnimatingSize && (Owner is T2DSceneObject))
                _initialSize = (Owner as T2DSceneObject).Size;

            _initialSize = (owner as T2DSceneObject).Size;

            ProcessList.Instance.AddTickCallback(Owner, this);

            return true;
        }

        #endregion


        #region Private, protected, internal fields
        bool _pingPong;
        bool _isAnimatingSize;
        bool _HasMaxSize;
        Vector2 _minSize;
        Vector2 _maxSize;
        Vector2 _animationRate;
        Vector2 _initialSize;
        #endregion

    }
}
