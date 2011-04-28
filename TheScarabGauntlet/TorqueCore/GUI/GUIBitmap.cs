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
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Materials;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// The style properties for the GUIBitmap control.
    /// </summary>
    public class GUIBitmapStyle : GUIStyle, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether the GUIBitmap control should resize itself to the same
        /// dimensions of the specified bitmap.
        /// </summary>
        public bool SizeToBitmap
        {
            get { return _sizeToBitmap; }
            set { _sizeToBitmap = value; }
        }



        /// <summary>
        /// Whether the GUIBitmap control should wrap the rendering of the 
        /// specified bitmap if the control dimensions are larger.
        /// </summary>
        public bool TextureWrap
        {
            get { return _textureWrap; }
            set { _textureWrap = value; }
        }

        #endregion


        #region Private, protected, internal fields

        bool _sizeToBitmap = true;
        bool _textureWrap = false;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// Draws an image to the GUICanvas.
    /// </summary>
    public class GUIBitmap : GUIControl, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The image file to render to the screen.
        /// </summary>
        public string Bitmap
        {
            get { return _bitmapName; }
            set
            {
                _bitmapName = value;
                if (_bitmapName != String.Empty)
                {
                    if (_material != null)
                        _material.Dispose();

                    _material = new Materials.SimpleMaterial();
                    _material.TextureFilename = _bitmapName;
                    _material.IsTranslucent = true;
                    _material.IsColorBlended = true;

                    // rdbnote: the render material should really handle this for me
                    Resource<Texture> res = ResourceManager.Instance.LoadTexture(_material.TextureFilename);
                    Texture2D texture = (Texture2D)res.Instance;

                    _material.SetTexture(res.Instance);

                    _bitmapSize = new Vector2(texture.Width, texture.Height);

                    if (_style != null && _style.SizeToBitmap)
                        Size = _bitmapSize;

                    // invalidate the temp. resource
                    res.Invalidate();
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
        /// Determines if the bitmap should be drawn mirrored along the Y axis.
        /// </summary>
        public BitmapFlip BitmapFlip
        {
            get { return _flip; }
            set { _flip = value; }
        }



        /// <summary>
        /// Returns the size in pixels of the bitmap.
        /// </summary>
        public Vector2 BitmapSize
        {
            get { return _bitmapSize; }
        }



        /// <summary>
        /// Adjusts the scale of the bitmap.
        /// </summary>
        public Vector2 BitmapScale
        {
            get { return _bitmapScale; }
            set { _bitmapScale = value; }
        }



        /// <summary>
        /// The material the bitmap is drawn with.
        /// </summary>
        public Materials.SimpleMaterial Material
        {
            get { return _material; }
            set { _material = value; }
        }



        /// <summary>
        /// If texture wrapping is enabled, this specifies the x,y coordinates of the
        /// area of the texture to begin rendering.
        /// </summary>
        public Vector2 WrapStart
        {
            get { return _wrapStart; }
            set { _wrapStart = value; }
        }



        /// <summary>
        /// The opacity of the bitmap. 1.0f is fully visible. 0.0f is fully transparent.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUIBitmap.OnRender");
#endif

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
                if (_material.Texture.IsNull)
                    Bitmap = _bitmapName;

                if (_style.TextureWrap)
                {
                    xDone = (_bounds.Extent.X / (_bitmapSize.X * _bitmapScale.X)) + 1;
                    yDone = (_bounds.Extent.Y / (_bitmapSize.Y * _bitmapScale.Y)) + 1;

                    xShift = (int)_wrapStart.X % (int)(_bitmapSize.X * _bitmapScale.X);
                    yShift = (int)_wrapStart.Y % (int)(_bitmapSize.Y * _bitmapScale.Y);

                    for (int y = 0; y < yDone; ++y)
                    {
                        for (int x = 0; x < xDone; ++x)
                        {
                            srcRegion.X = 0;
                            srcRegion.Y = 0;
                            srcRegion.Width = _bitmapSize.X * _bitmapScale.X;
                            srcRegion.Height = _bitmapSize.Y * _bitmapScale.Y;

                            dstRegion.X = (((_bitmapSize.X * _bitmapScale.X) * x) + offset.X) - xShift;
                            dstRegion.Y = (((_bitmapSize.Y * _bitmapScale.Y) * y) + offset.Y) - yShift;
                            dstRegion.Width = _bitmapSize.X * _bitmapScale.X;
                            dstRegion.Height = _bitmapSize.Y * _bitmapScale.Y;

                            // draw the bitmap
                            DrawUtil.BitmapStretchSR(_material, dstRegion, srcRegion, _flip);
                        }
                    }
                }
                else
                {
                    // draw the bitmap
                    if (_bitmapScale.X != 1.0f || _bitmapScale.Y != 1.0f)
                    {
                        srcRegion.X = 0;
                        srcRegion.Y = 0;
                        srcRegion.Width = _bitmapSize.X * _bitmapScale.X;
                        srcRegion.Height = _bitmapSize.Y * _bitmapScale.Y;

                        dstRegion.X = offset.X;
                        dstRegion.Y = offset.Y;
                        dstRegion.Width = Size.X * _bitmapScale.X;
                        dstRegion.Height = Size.Y * _bitmapScale.Y;

                        DrawUtil.BitmapStretchSR(_material, dstRegion, srcRegion, _flip);
                    }
                    else DrawUtil.BitmapStretch(_material, ctrlRect, _flip);
                }
            }

            // draw a border, if appropriate
            if (_style.HasBorder)
                DrawUtil.Rect(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

#if DEBUG
            Profiler.Instance.EndBlock("GUIBitmap.OnRender");
#endif

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIBitmap obj2 = (GUIBitmap)obj;

            obj2.Bitmap = Bitmap;
            obj2.WrapStart = WrapStart;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUIBitmapStyle);

            Assert.Fatal(_style != null, "GUIBitmap._OnNewStyle - Control was assigned an invalid style!");

            if (_style == null || !base._OnNewStyle(style))
                return false;

            if (_style.SizeToBitmap && _material != null)
                Size = new Vector2(_bitmapSize.X, _bitmapSize.Y);

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        string _bitmapName = String.Empty;
        Materials.SimpleMaterial _material = null;

        float _opacity = 1.0f;

        RectangleF srcRegion = new RectangleF();
        RectangleF dstRegion = new RectangleF();

        float xDone;
        float yDone;
        int xShift;
        int yShift;

        Vector2 _bitmapSize;
        Vector2 _wrapStart = new Vector2(0.0f, 0.0f);

        GUIBitmapStyle _style = null;

        Vector2 _bitmapScale = new Vector2(1.0f, 1.0f);
        BitmapFlip _flip = BitmapFlip.None;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            _material = null;
            base.Dispose();
        }

        #endregion
    }
}
