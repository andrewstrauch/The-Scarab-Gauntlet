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



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// A GUI style used by GUIMLText controls.
    /// </summary>
    public class GUIMLTextStyle : GUITextStyle
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not to size the width of this control aswell as the height.
        /// </summary>
        public bool AutoSizeHeightOnly
        {
            get { return _autoSizeHeightOnly; }
            set { _autoSizeHeightOnly = value; }
        }



        /// <summary>
        /// The extra height in pixels to add or subtract between lines of text.
        /// </summary>
        public float LineSpacing
        {
            get { return _lineSpacing; }
            set { _lineSpacing = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private bool _autoSizeHeightOnly;
        private float _lineSpacing = 0.0f;

        #endregion
    }



    /// <summary>
    /// Renders a string to the GUICanvas.
    /// </summary>
    public class GUIMLText : GUIText
    {

        #region Public properties, operators, constants, and enums

        public override string Text
        {
            get { return base.Text; }
            set
            {
                if (_text == value)
                    return;

                _splitText = value.Split('\n');
                base.Text = value;
            }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUIMLText.OnRender");
#endif

            DrawUtil.ClearBitmapModulation();

            RectangleF ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill the update rect with the fill color
            if (_style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, _style.FillColor[CustomColor.ColorBase]);

            // if there's a border, draw the border
            if (_style.HasBorder)
                DrawUtil.Rect(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

            if (_splitText != null)
            {
                Vector2 pos = offset - updateRect.Point;
                Vector2 size = new Vector2(Bounds.Width, _style.Font.Instance.LineSpacing + _style.LineSpacing);
                for (int i = 0; i < _splitText.Length; i++)
                {
                    DrawUtil.JustifiedText(_style.Font, pos, size, _style.Alignment, _style.TextColor[CustomColor.ColorBase], _splitText[i]);
                    pos.Y += size.Y;
                }
            }

#if DEBUG
            Profiler.Instance.EndBlock("GUIMLText.OnRender");
#endif

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _SizeToText()
        {
            if (_text == null)
                return;

            float width = Size.X;
            float height = (_style.Font.Instance.LineSpacing + _style.LineSpacing) * _splitText.Length;

            if (!_style.AutoSizeHeightOnly)
            {
                foreach (string s in _splitText)
                {
                    float newWidth = _style.Font.Instance.MeasureString(s).X;
                    if (newWidth > width)
                        width = newWidth;
                }
            }

            Size = new Vector2(width, height);
        }



        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUIMLTextStyle);

            Assert.Fatal(_style != null, "GUIMLText._OnNewStyle - Control was assigned an invalid style!\nRequires a ");
            if (_style == null || !base._OnNewStyle(style))
                return false;

            if (_style.SizeToText && _text != String.Empty)
                _SizeToText();

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        protected string[] _splitText;
        protected GUIMLTextStyle _style = null;

        #endregion
    }
}
