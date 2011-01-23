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



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// Called when the GUISplash has finished the fade in - fade out routine.
    /// </summary>
    public delegate void OnSplashFinished();



    /// <summary>
    /// The style properties for the GUISplash control.
    /// </summary>
    public class GUISplashStyle : GUIStyle
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The number of seconds to fade the splash image from black.
        /// </summary>
        public float FadeInSec
        {
            get { return _fadeTimeIn; }
            set { _fadeTimeIn = value; }
        }



        /// <summary>
        /// The number of seconds to fade the splash image to black.
        /// </summary>
        public float FadeOutSec
        {
            get { return _fadeTimeOut; }
            set { _fadeTimeOut = value; }
        }



        /// <summary>
        /// The number of seconds to wait between a fade in and fade out.
        /// </summary>
        public float FadeWaitSec
        {
            get { return _waitTime; }
            set { _waitTime = value; }
        }

        #endregion


        #region Private, protected, internal fields

        float _waitTime = 2;
        float _fadeTimeIn = 1;
        float _fadeTimeOut = 1;

        #endregion
    }



    /// <summary>
    /// Fades an image in and out from black. Typically used for promotional
    /// ads that are shown at the start of an application.
    /// </summary>
    public class GUISplash : GUIControl, IAnimatedObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// This delegate is called after this control has finished fading the image.
        /// </summary>
        public OnSplashFinished OnFadeFinished
        {
            get { return _finishedDelegate; }
            set { _finishedDelegate = value; }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUISplash.OnRender");
#endif

            // clear bitmap modulation
            DrawUtil.ClearBitmapModulation();

            if (_style.Bitmap != null)
            {
                // draw the bitmap
                RectangleF rect = new RectangleF(offset, _bounds.Extent);
                DrawUtil.BitmapStretch(_style.Material, rect, BitmapFlip.None);
            }

            // draw the fade quad
            Color fadeColor = new Color(0, 0, 0, _currentAlpha);
            DrawUtil.RectFill(offset, _bounds.Extent + offset, fadeColor);

            // make sure control has time to render finished state
            if (_doneFading && !_finished)
                _finished = true;

#if DEBUG
            Profiler.Instance.EndBlock("GUISplash.OnRender");
#endif

            base.OnRender(offset, updateRect);
        }



        public void UpdateAnimation(float dt)
        {
            _totalTime += dt;

            if (_totalTime < _style.FadeInSec)
            {
                // fade in
                _currentAlpha = (byte)(255.0f - (255.0f * (_totalTime / _style.FadeInSec)));
            }
            else if (_totalTime < (_style.FadeInSec + _style.FadeWaitSec))
            {
                // wait
                _currentAlpha = 0;
            }
            else if (_totalTime < (_style.FadeInSec + _style.FadeWaitSec + _style.FadeOutSec))
            {
                // fade out
                float elapsed = _totalTime - (_style.FadeInSec + _style.FadeWaitSec);
                _currentAlpha = (byte)(255.0f * elapsed / _style.FadeOutSec);
            }
            else
            {
                // done state
                _currentAlpha = 255;
                _doneFading = true;

                if (_finished && _finishedDelegate != null)
                    _finishedDelegate();
            }
        }



        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            if (data.EventAction == TorqueInputDevice.Action.Break)
            {
                // ignore mouse movement...
                if (data.DeviceTypeId == TorqueInputDevice.KeyboardId || data.DeviceTypeId == TorqueInputDevice.GamePadId)
                {
                    if (_finishedDelegate != null)
                        _finishedDelegate();

                    return true;
                }
            }

            return false;
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUISplash obj2 = (GUISplash)obj;
            obj2.OnFadeFinished = OnFadeFinished;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnWake()
        {
            if (!base._OnWake())
                return false;

            _totalTime = 0;
            _doneFading = false;
            _finished = false;
            _currentAlpha = 255;

            ProcessList.Instance.AddAnimationCallback(this);

            return true;
        }



        protected override void _OnSleep()
        {
            ProcessList.Instance.RemoveObject(this);
            base._OnSleep();
        }



        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUISplashStyle);

            Assert.Fatal(_style != null, "GUISplash._OnNewStyle - Control was assigned an invalid style!");

            if (_style == null || !base._OnNewStyle(style))
                return false;

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        float _totalTime;

        bool _doneFading;
        byte _currentAlpha;

        bool _finished;
        OnSplashFinished _finishedDelegate;

        GUISplashStyle _style = null;

        #endregion
    }
}
