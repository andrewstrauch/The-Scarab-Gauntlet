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
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A helper component to be placed on scrollers in a level. ParallaxXScrollerComponents register themselves with the ParallaxManager
    /// during _OnRegister. See the summary of the ParallaxFactorX property for details on setup. There must be a target set on the ParallaxManager
    /// for this to have any effect at all.
    /// </summary>
    [TorqueXmlSchemaType]
    public class ParallaxXScrollerComponent : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// This is the magnitude of the parallax effect. 0 means no movement; 1 means same movement as camera (not reccomended); less than 0 means opposite 
        /// direction from camera. Example: near-background: 0.7, far-background: 0.3, sky: 0.02. You can also add a foreground plane at around -0.3 or so to
        /// give the impression of objects close to the camera moving quickly. Play around with the numbers, it's not an exact effect in most games.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public float ParallaxFactorX
        {
            get { return _parallaxFactorX; }
            set { _parallaxFactorX = value; }
        }

        /// <summary>
        /// An interface for the ParallaxManager to directly assign the scroll speed of the scroller object that owns this component.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public T2DScroller Scroller
        {
            get { return _scroller; }
        }

        /// <summary>
        /// The initial X scroll offset of this scroller as it appears in the level.
        /// </summary>
        public float StartOffset
        {
            get { return _startOffset; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ParallaxXScrollerComponent obj2 = obj as ParallaxXScrollerComponent;

            obj2.ParallaxFactorX = ParallaxFactorX;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // store the scroller that owns this component
            _scroller = owner as T2DScroller;

            // return false if no scroller exists
            if (_scroller == null)
                return false;

            // enforce a zero X-axis scroll rate for parallax scrollers
            _scroller.ScrollRateX = 0;

            // grab the initial offset
            _startOffset = _scroller.TextureOffsetX;

            // register this scroller with the parallax manager
            ParallaxManager.Instance.RegisterParallaxScroller(this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected T2DScroller _scroller;
        protected float _parallaxFactorX;
        protected float _startOffset;

        protected float _preTickOffset;
        protected float _postTickOffset;

        /// <summary>
        /// The offset of this scroller at the begining of the current tick. Used by the parallax manager to interpolate offset of this scroller.
        /// </summary>
        internal float _PreTickOffset
        {
            get { return _preTickOffset; }
            set { _preTickOffset = value; }
        }

        /// <summary>
        /// The offset of this scroller at the end of the current tick. Used by the parallax manager to interpolate offset of this scroller.
        /// </summary>
        internal float _PostTickOffset
        {
            get { return _postTickOffset; }
            set { _postTickOffset = value; }
        }

        #endregion
    }

    public class ParallaxManager : ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors

        /// <summary>
        /// Static singleton instance of the ParallaxManager.
        /// </summary>
        static public ParallaxManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ParallaxManager();

                return _instance;
            }
        }

        static private ParallaxManager _instance;

        #endregion

        //======================================================
        #region Constructors

        public ParallaxManager()
        {
            ProcessList.Instance.AddTickCallback(new TorqueObject(), this);
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object that the parallax effect will be based on. In most cases, ParallaxTarget will be the scene camera.
        /// </summary>
        public T2DSceneObject ParallaxTarget
        {
            get { return _parallaxTarget; }
            set
            {
                _parallaxTarget = value;

                if (_parallaxTarget != null)
                    _parallaxStartPositionX = _parallaxTarget.Position.X;
                else
                    _parallaxStartPositionX = 0;
            }
        }

        /// <summary>
        /// A scalar to be applied to all parallax scroll speeds.
        /// </summary>
        public float ParallaxSpeedScale
        {
            get { return _parallaxSpeedScale; }
            set { _parallaxSpeedScale = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public void ProcessTick(Move move, float elapsed)
        {
            if (_parallaxTarget == null)
                return;

            if (_parallaxStartPositionX == 0)
                _parallaxStartPositionX = _parallaxTarget.Position.X;

            // get the offset from the starting position scaled by the parallax speed scale
            float offset = (_parallaxStartPositionX - _parallaxTarget.Position.X) * _parallaxSpeedScale;

            // if the engine is not interpolating, just slam the correct texture offset
            if (!ProcessList.Instance.UseInterpolation)
            {
                // set the exact texture offset of each scroller in our list
                foreach (ParallaxXScrollerComponent scroller in _parallaxScrollers)
                    scroller.Scroller.TextureOffsetX = scroller.StartOffset + (scroller.ParallaxFactorX * (offset / (scroller.Scroller.Size.X / scroller.Scroller.TextureRepeatX)));
            }
            else
            {
                // iterate over all the scrollers
                foreach (ParallaxXScrollerComponent scroller in _parallaxScrollers)
                {
                    // record the current offset
                    scroller._PreTickOffset = scroller.Scroller.TextureOffsetX;

                    // set the post tick offset to the desired offset
                    scroller._PostTickOffset = scroller.StartOffset + (scroller.ParallaxFactorX * (offset / (scroller.Scroller.Size.X / scroller.Scroller.TextureRepeatX)));
                }
            }


        }

        public void InterpolateTick(float k)
        {
            if (_parallaxTarget == null)
                return;

            // interpolate the offset of each of the parallax scrollers
            foreach (ParallaxXScrollerComponent scroller in _parallaxScrollers)
                scroller.Scroller.TextureOffsetX = (1.0f - k) * scroller._PreTickOffset + k * scroller._PostTickOffset;
        }

        /// <summary>
        /// Adds the ParallaxXScrollerComponent to the list of active parallax scrollers.
        /// </summary>
        /// <param name="scroller">The ParallaxXScrollerComponent to be added.</param>
        public void RegisterParallaxScroller(ParallaxXScrollerComponent scroller)
        {
            if (!_parallaxScrollers.Contains(scroller) && scroller.Owner as T2DScroller != null)
                _parallaxScrollers.Add(scroller);
        }

        /// <summary>
        /// Removes a ParallaxXScrollerComponent from the list of active parallax scrollers.
        /// </summary>
        /// <param name="scroller">The ParallaxXScrollerComponent to be added.</param>
        public void UnregisterParallaxScroller(ParallaxXScrollerComponent scroller)
        {
            if (_parallaxScrollers.Contains(scroller))
                _parallaxScrollers.Remove(scroller);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected T2DSceneObject _parallaxTarget;
        protected float _parallaxStartPositionX;
        protected List<ParallaxXScrollerComponent> _parallaxScrollers = new List<ParallaxXScrollerComponent>();
        protected float _parallaxSpeedScale = 1.0f;

        #endregion
    }
}
