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
using GarageGames.Torque.Util;
using System.Xml.Serialization;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// The style properties for the GUIText control.
    /// </summary>
    public class GUITextStyle : GUIStyle, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether the GUIText control should resize itself to the 
        /// width and height of the rendered text.
        /// </summary>
        public bool SizeToText
        {
            get { return _sizeToText; }
            set { _sizeToText = value; }
        }



        /// <summary>
        /// The font resource used when rendering the text. The font resource
        /// contains glyph information and is used by the FontRenderer. The
        /// font resource is created when the FontType property is specified.
        /// </summary>
        public Resource<SpriteFont> Font
        {
            get
            {
                if (_font.IsInvalid)
                    _font = ResourceManager.Instance.LoadFont(_fontType);
                return _font;
            }
        }



        /// <summary>
        /// The font to use for rendering the text. The default engine fonts are
        /// specified in the project TorqueEngineData.
        /// <example>style.FontType = "Arial16";</example>
        /// </summary>
        public string FontType
        {
            get { return _fontType; }
            set
            {
                _fontType = value;

                if (_fontType != String.Empty)
                    _font = ResourceManager.Instance.LoadFont(_fontType);
            }
        }



        /// <summary>
        /// The color of the text when rendered.
        /// </summary>
        public ColorCollection TextColor
        {
            get { return _textColor; }
        }



        /// <summary>
        /// Defines the horizontal text justification.
        /// </summary>
        public TextAlignment Alignment
        {
            get { return _textAlignment; }
            set { _textAlignment = value; }
        }

        #endregion


        #region Public methods

        public override void OnLoaded()
        {
            base.OnLoaded();

            for (int i = 0; i < _textColorAsVector4.Count; i++)
            {
                Vector4 vec = _textColorAsVector4[i];
                Color col = new Color();
                col.R = (Byte)vec.X;
                col.G = (Byte)vec.Y;
                col.B = (Byte)vec.Z;
                col.A = (Byte)vec.W;
                TextColor[(CustomColor)i] = col;
            }
        }

        #endregion


        #region Private, protected, internal fields

        string _fontType;
        Resource<SpriteFont> _font;

        ColorCollection _textColor = new ColorCollection();
        TextAlignment _textAlignment = TextAlignment.JustifyLeft;

        bool _sizeToText = true;

        [XmlElement(ElementName = "TextColors")]
        [TorqueXmlDeserializeInclude]
        protected List<Vector4> _textColorAsVector4 = new List<Vector4>();

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_font.IsNull)
            {
                _font.Invalidate();
            }
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// Renders a string to the GUICanvas.
    /// </summary>
    public class GUIText : GUIControl
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The string to render to the screen.
        /// </summary>
        public virtual string Text
        {
            get { return _text; }
            set
            {
                _text = value;

                if (_style != null && _style.SizeToText)
                    _SizeToText();
            }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUIText.OnRender");
#endif

            DrawUtil.ClearBitmapModulation();

            ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill the update rect with the fill color
            if (_style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, _style.FillColor[CustomColor.ColorBase]);

            // if there's a border, draw the border
            if (_style.HasBorder)
                DrawUtil.Rect(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

            DrawUtil.JustifiedText(_style.Font, new Vector2(offset.X - updateRect.Point.X, offset.Y - updateRect.Point.Y), _bounds.Extent, _style.Alignment, _style.TextColor[CustomColor.ColorBase], _text);

#if DEBUG
            Profiler.Instance.EndBlock("GUIText.OnRender");
#endif

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIText obj2 = (GUIText)obj;
            obj2.Text = Text;
        }

        #endregion


        #region Private, protected, internal methods

        protected virtual void _SizeToText()
        {
            Size = new Vector2(_style.Font.Instance.MeasureString(_text).X, _style.Font.Instance.LineSpacing);
        }



        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUITextStyle);

            Assert.Fatal(!_style.Font.IsNull, "GUIText._OnNewStyle - Font resource is not valid!");

            if (_style.Font.IsNull)
                return false;

            Assert.Fatal(_style != null, "GUIText._OnNewStyle - Control was assigned an invalid style!");

            if (_style == null || !base._OnNewStyle(style))
                return false;

            if (_style.SizeToText && _text != String.Empty)
                _SizeToText();

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        protected string _text = String.Empty;
        GUITextStyle _style = null;
        RectangleF ctrlRect;

        #endregion
    }
}
