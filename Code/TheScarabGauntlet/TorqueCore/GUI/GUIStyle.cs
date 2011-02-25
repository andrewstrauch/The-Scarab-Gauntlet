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
using System.Xml.Serialization;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// A GUIStyle is used by every GUIControl. It is used to control information
    /// that does not change or is unlikely to change during the execution of the
    /// application. Helps define the look and behavior of a GUIControl. Derive
    /// from GUIStyle to further specialize a style for your custom controls.
    /// </summary>
    public class GUIStyle : TorqueBase, IDisposable
    {

        #region Constructors

        static GUIStyle()
        {
            DefaultFillColor[CustomColor.ColorBase] = Color.LightBlue;
            DefaultFillColor[CustomColor.ColorHL] = Color.LightPink;
            DefaultFillColor[CustomColor.ColorSEL] = Color.LightGreen;
            DefaultFillColor[CustomColor.ColorNA] = Color.LightGray;
            DefaultBorderColor[CustomColor.ColorBase] = Color.DarkBlue;
            DefaultBorderColor[CustomColor.ColorHL] = Color.DarkRed;
            DefaultBorderColor[CustomColor.ColorSEL] = Color.DarkGreen;
            DefaultBorderColor[CustomColor.ColorNA] = Color.DarkGray;
        }



        static public ColorCollection DefaultFillColor = new ColorCollection();



        static public ColorCollection DefaultBorderColor = new ColorCollection();



        public GUIStyle()
        {
            for (int i = 0; i < (int)CustomColor.NumColors; i++)
            {
                // provide some default fill colors
                FillColor[(CustomColor)i] = DefaultFillColor[(CustomColor)i];

                // provide some default border colors
                BorderColor[(CustomColor)i] = DefaultBorderColor[(CustomColor)i];
            }
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specify an image used frequently for this style. Typically the image
        /// is used to skin a GUIControl by using sub-regions of the image to draw
        /// over various parts of a control. Sub-regions can be defined automatically
        /// by placing single pixel lines of a solid color in the image, or manually
        /// by specifying the rectangular coordinates making up the sub-regions.
        /// </summary>
        public string Bitmap
        {
            get { return _bitmapTexture; }
            set { SetBitmap(value); }
        }



        /// <summary>
        /// Sets or Gets the material used when rendering this style. Will return a null
        /// material if no Bitmap texture is specified. Used internally by the GUI
        /// system.
        /// </summary>
        public Materials.SimpleMaterial Material
        {
            get { return _material; }
            set { _material = value; }
        }



        /// <summary>
        /// If AutoSkin is enabled and GUIStyle::ConstructBitmapCoords was called this
        /// list will contain the rectangular coordinates of the sub-regions found in the
        /// specified image. Coordinates can also be manually set using this property.
        /// </summary>
        public List<RectangleF> Coords
        {
            get { return _bitmapCoordRects; }
            set { _bitmapCoordRects = value; }
        }



        /// <summary>
        /// Enable / Disable auto skinning. If auto skinning is enabled the control will
        /// try to generate the skinning regions from the bitmap based on pre-placed 
        /// pixels in the texture when GUIStyle::ConstructBitmapCoords is called.
        /// </summary>
        public bool AutoSkin
        {
            get { return _autoSkin; }
            set { _autoSkin = value; }
        }



        /// <summary>
        /// Used to fill the bounds of a GUIControl with the specified color, if opaque. If
        /// a control uses FillColor, it must support at least the CustomColor.BaseColor value.
        /// </summary>
        public ColorCollection FillColor
        {
            get { return _fillColor; }
        }



        /// <summary>
        /// Used to draw a border around the bounds of a GUIControl with the specified color. If
        /// a control uses BorderColor, it must support at least the CustomColor.BaseColor value.
        /// </summary>
        public ColorCollection BorderColor
        {
            get { return _borderColor; }
        }



        /// <summary>
        /// Whether a GUIControl should keep its aspect ratio. Mostly affects the
        /// content control (the bottom level control) during GUIControl::OnPreRender.
        /// </summary>
        public bool PreserveAspectRatio
        {
            get { return _preserveAspect; }
            set { _preserveAspect = value; }
        }



        /// <summary>
        /// If the GUIControl is not translucent.
        /// </summary>
        public bool IsOpaque
        {
            get { return _opaque; }
            set { _opaque = value; }
        }



        /// <summary>
        /// If the GUIControl has a border.
        /// </summary>
        public bool HasBorder
        {
            get { return _hasBorder; }
            set { _hasBorder = value; }
        }



        /// <summary>
        /// If the GUIControl can receive focus.
        /// Does the GUIControl respond to input events.
        /// </summary>
        public bool Focusable
        {
            get { return _canFocus; }
            set { _canFocus = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Specify an image used frequently for this style. Typically the image
        /// is used to skin a GUIControl by using sub-regions of the image to draw
        /// over various parts of a control. Sub-regions can be defined automatically
        /// by placing single pixel lines of a solid color in the image, or manually
        /// by specifying the rectangular coordinates making up the sub-regions.
        /// </summary>
        /// <param name="fileName">The image to be used by the style.</param>
        public void SetBitmap(string fileName)
        {
            if (fileName == String.Empty)
                return;

            // temp. load the texture
            _bitmapTexture = fileName;

            if (_bitmapTexture != String.Empty)
            {
                if (_material != null)
                    _material.Dispose();

                _material = new Materials.SimpleMaterial();
                _material.TextureFilename = _bitmapTexture;
                _material.IsTranslucent = true;
            }
            else
            {
                if (_material != null)
                    _material.Dispose();

                _material = null;
            }
        }



        /// <summary>
        /// Creates an array of bitmaps from one single bitmap using a seperator color.
        /// The seperator color is whatever color is in pixel 0,0 of the bitmap. It
        /// stores the the coordinates of each piece in _bitmapCoordRects.
        /// </summary>
        /// <returns>The number of sub-regions found in the specified image.</returns>
        public int ConstructBitmapCoords()
        {
            if (_bitmapTexture == String.Empty)
                return 0;

            if (!_autoSkin || _bitmapCoordRects.Count > 0)
                return _bitmapCoordRects.Count;

            // rdbnote: the render material should really handle this for me
            Resource<Texture> res = ResourceManager.Instance.LoadTexture(_material.TextureFilename);
            Texture2D bitmap = (Texture2D)res.Instance;

            int texWidth = bitmap.Width;
            int texHeight = bitmap.Height;

            uint[] rgba = new uint[texWidth * texHeight];
            bitmap.GetData<uint>(rgba);

            // get the seperator color
            Color sepColor = new Color();
            sepColor.PackedValue = rgba[0 + 0 * texWidth];

            // now loop through the bitmap and find the bounding rectangle for each piece
            _bitmapCoordRects.Clear();

            int curY = 0;
            Color color = new Color();

            while (curY < texHeight)
            {
                // grab the pixel color
                color.PackedValue = rgba[0 + curY * texWidth];

                // skip any sep colors
                if (color == sepColor)
                {
                    curY++;
                    continue;
                }

                // process left to right, grabbing bitmaps as we go
                int curX = 0;

                while (curX < texWidth)
                {
                    // grab the pixel color
                    color.PackedValue = rgba[curX + curY * texWidth];

                    // skip any sep colors
                    if (color == sepColor)
                    {
                        curX++;
                        continue;
                    }

                    int startX = curX;

                    while (curX < texWidth)
                    {
                        // grab the pixel color
                        color.PackedValue = rgba[curX + curY * texWidth];

                        // skip any sep colors
                        if (color == sepColor)
                            break;

                        curX++;
                    }

                    int stepY = curY;

                    while (stepY < texHeight)
                    {
                        // grab the pixel color
                        color.PackedValue = rgba[startX + stepY * texWidth];

                        // skip any sep colors
                        if (color == sepColor)
                            break;

                        stepY++;
                    }

                    // add these coordinates to the list
                    _bitmapCoordRects.Add(new RectangleF(startX, curY, curX - startX, stepY - curY));
                }

                // now skip to the next seperation color on column 0
                while (curY < texHeight)
                {
                    // grab the pixel color
                    color.PackedValue = rgba[0 + curY * texWidth];

                    if (color == sepColor)
                        break;

                    curY++;
                }
            }

            // invalidate the temp. resource
            res.Invalidate();

            // return the number of regions we found
            return _bitmapCoordRects.Count;
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            for (int i = 0; i < _borderColorAsVector4.Count; i++)
                BorderColor[(CustomColor)i] = new Color(_borderColorAsVector4[i]);

            for (int i = 0; i < _fillColorAsVector4.Count; i++)
                FillColor[(CustomColor)i] = new Color(_fillColorAsVector4[i]);
        }

        #endregion


        #region Private, protected, internal fields

        protected bool _opaque = false;
        protected bool _hasBorder = false;
        protected bool _canFocus = false;

        protected string _bitmapTexture = String.Empty;
        protected Materials.SimpleMaterial _material = null;

        protected List<RectangleF> _bitmapCoordRects = new List<RectangleF>();
        protected bool _autoSkin = false;

        protected ColorCollection _fillColor = new ColorCollection();
        protected ColorCollection _borderColor = new ColorCollection();

        protected bool _preserveAspect = false;

        [XmlElement(ElementName = "BorderColors")]
        [TorqueXmlDeserializeInclude]
        protected List<Vector4> _borderColorAsVector4 = new List<Vector4>();

        [XmlElement(ElementName = "FillColors")]
        [TorqueXmlDeserializeInclude]
        protected List<Vector4> _fillColorAsVector4 = new List<Vector4>();

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            _material = null;
            _borderColorAsVector4.Clear();
            _borderColorAsVector4 = null;
            _fillColorAsVector4.Clear();
            _fillColorAsVector4 = null;
            _fillColor = null;
            _borderColor = null;
            _bitmapCoordRects.Clear();
            _bitmapCoordRects = null;
            _ResetRefs();
            base.Dispose();
        }

        #endregion
    }
}
