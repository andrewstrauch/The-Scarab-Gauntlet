//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
// Author: Jason Cahill
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Materials;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GUI
{
    public delegate bool OnVideoEndDelegate(string video);

    /// <summary>
    /// The style properties for the GUIVideo control.
    /// </summary>
    public class GUIVideoStyle : GUIStyle
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether the GUIVideo control should resize itself to the same
        /// dimensions of the specified bitmap.
        /// </summary>
        public bool SizeToBitmap
        {
            get { return _sizeToBitmap; }
            set { _sizeToBitmap = value; }
        }

        #endregion


        #region Private, protected, internal fields

        bool _sizeToBitmap = true;

        #endregion
    }



    /// <summary>
    /// Draws an image to the GUICanvas.
    /// </summary>
    public class GUIVideo : GUIControl
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The image file to render to the screen.
        /// </summary>
        public string VideoFilename
        {
            get { return _bitmapName; }
            set
            {
                _bitmapName = value;

                if (_bitmapName != String.Empty)
                {
                    if (_material != null)
                        _material.Dispose();

                    _material = new Materials.VideoMaterial();
                    _material.VideoFilename = _bitmapName;
                    _material.IsTranslucent = true;
                    _material.IsColorBlended = true;
                    _material.IsLooped = _isLooped;

                    // rdbnote: the render material should really handle this for me
                    Texture2D texture = (Texture2D)(_material.CurrentVideoFrameTexture.Instance);
                    _bitmapSize = new Vector2(texture.Width, texture.Height);

                    if (_style != null && _style.SizeToBitmap)
                        Size = _bitmapSize;
                }
                else
                {
                    if (_material != null)
                        _material.Dispose();
                    _material = null;
                }
            }
        }



        /// <summary>
        /// The opacity of the bitmap. 1.0f is fully visible. 0.0f is fully transparent.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }



        public bool IsLooped
        {
            get { return _isLooped; }
            set { _isLooped = value; if (_material != null) _material.IsLooped = value; }
        }



        public OnVideoEndDelegate OnVideoEnd
        {
            get { return _onVideoEnd; }
            set { _onVideoEnd = value; }
        }

        #endregion


        #region Public methods

        public void Play()
        {
            _material.Play();
        }



        public void Stop()
        {
            _material.Stop();
        }



        public void Pause()
        {
            _material.Pause();
        }



        public void Resume()
        {
            _material.Resume();
        }



        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUIVideo.OnRender");
#endif

            if (_material.IsStopped)
            {
                if (_onVideoEnd != null)
                {
                    if (!_onVideoEnd.Invoke(VideoFilename))
                    {
                        _onVideoEnd = null;
#if DEBUG
                        Profiler.Instance.EndBlock("GUIVideo.OnRender");
#endif
                        return;
                    }
                    _onVideoEnd = null;
                }
            }

            // clear bitmap modulation
            DrawUtil.ClearBitmapModulation();

            RectangleF ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill in, if appropriate
            if (_style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, _style.FillColor[CustomColor.ColorBase]);

            // set the opacity of the material
            _material.Opacity = _opacity;

            if (_material != null)
            {
                // draw the bitmap
                DrawUtil.BitmapStretch(_material, ctrlRect, BitmapFlip.None);
            }

            // draw a border, if appropriate
            if (_style.HasBorder)
                DrawUtil.Rect(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

#if DEBUG
            Profiler.Instance.EndBlock("GUIVideo.OnRender");
#endif

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIVideo obj2 = (GUIVideo)obj;

            obj2.VideoFilename = VideoFilename;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUIVideoStyle);

            Assert.Fatal(_style != null, "GUIVideo._OnNewStyle - Control was assigned an invalid style!");

            if (_style == null || !base._OnNewStyle(style))
                return false;

            if (_style.SizeToBitmap && _material != null)
                Size = new Vector2(_bitmapSize.X, _bitmapSize.Y);

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        string _bitmapName = String.Empty;

        Materials.VideoMaterial _material = null;
        float _opacity = 1.0f;
        bool _isLooped = true;

        Vector2 _bitmapSize;
        GUIVideoStyle _style = null;

        OnVideoEndDelegate _onVideoEnd;

        #endregion
    }
}