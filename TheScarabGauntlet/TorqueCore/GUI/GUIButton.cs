//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Util;
using System.Xml.Serialization;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// Called when the button has focus and is selected.
    /// </summary>
    public delegate void OnButtonSelected();



    /// <summary>
    /// The style properties for the GUIButton control.
    /// </summary>
    public class GUIButtonStyle : GUIStyle
    {

        #region Public properties, operators, constants, and enums

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

        [XmlElement(ElementName = "TextColors")]
        [TorqueXmlDeserializeInclude]
        protected List<Vector4> _textColorAsVector4 = new List<Vector4>();

        #endregion
    }



    /// <summary>
    /// Renders a skinnable button.
    /// </summary>
    public class GUIButton : GUIControl
    {

        #region Constructors

        public GUIButton()
        {
            _SetupInput(0);
        }

        public GUIButton(int playerIndex)
        {
            _SetupInput(playerIndex);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Defines the various states of a button.
        /// </summary>
        public enum ButtonState
        {
            Normal = 0, // regular look
            Selected,   // has focus
            Pushed,     // receiving input

            NumStates
        }



        /// <summary>
        /// The string to render on the button.
        /// </summary>
        public string ButtonText
        {
            get { return _buttonText; }
            set { _buttonText = value; }
        }



        /// <summary>
        /// This delegate is called when the button is selectd.
        /// </summary>
        public OnButtonSelected OnSelectedDelegate
        {
            get { return _selectedDelegate; }
            set { _selectedDelegate = value; }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUIButton.OnRender");
#endif

            bool highlight = _buttonState == ButtonState.Selected;
            bool depressed = _buttonState == ButtonState.Pushed;

            // clear bitmap modulation
            DrawUtil.ClearBitmapModulation();

            RectangleF ctrlRect = new RectangleF(offset, _bounds.Extent);
            CustomColor colorIndex = Active ? (depressed ? CustomColor.ColorSEL : (highlight ? CustomColor.ColorHL : CustomColor.ColorBase)) : CustomColor.ColorNA;

            // Fill in if appropriate
            if (Style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, Style.FillColor[colorIndex]);

            // Draw the border if appopriate
            if (Style.HasBorder)
                DrawUtil.Rect(ctrlRect, Style.BorderColor[colorIndex]);

            // render justified text
            Vector2 textPos = new Vector2(0.0f, 0.0f);

            if (_buttonState == ButtonState.Pushed)
                textPos += new Vector2(1.0f, 1.0f);

            DrawUtil.JustifiedText(_style.Font, textPos, _bounds.Extent, _style.Alignment, _style.TextColor[colorIndex], _buttonText);

#if DEBUG
            Profiler.Instance.EndBlock("GUIButton.OnRender");
#endif
        }



        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            // an inactive button is a disabled button
            if (Active)
            {
                if (_inputMap != null)
                {
                    if (_inputMap.ProcessInput(data))
                    {
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                        {
                            _buttonState = ButtonState.Pushed;
                        }
                        else if (data.EventAction == TorqueInputDevice.Action.Break)
                        {
                            if (GUICanvas.Instance.GetFocusControl() == this)
                                _buttonState = ButtonState.Selected;
                            else
                                _buttonState = ButtonState.Normal;
                        }

                        return true;
                    }
                }

                // if the button is currently pushed, eat input until it is released
                if (_buttonState == ButtonState.Pushed)
                    return false;
            }

            // if we got here, this control didn't handle the input,
            //  pass it up to the parent if we have one
            return (Parent != null ? Parent.OnInputEvent(ref data) : false);
        }



        public override void OnGainFocus(GUIControl oldFocusCtrl)
        {
            base.OnGainFocus(oldFocusCtrl);

            // only change state if the button is in normal mode
            if (_buttonState == ButtonState.Normal)
                _buttonState = ButtonState.Selected;
        }



        public override void OnLoseFocus(GUIControl newFocusCtrl)
        {
            base.OnLoseFocus(newFocusCtrl);

            // only change state if the button is selected
            if (_buttonState == ButtonState.Selected)
                _buttonState = ButtonState.Normal;
        }



        public void SetButtonPushed()
        {
            _buttonState = ButtonState.Pushed;
        }



        public void SetButtonSelected()
        {
            _buttonState = ButtonState.Selected;

            if (_selectedDelegate != null)
                _selectedDelegate();
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIButton obj2 = (GUIButton)obj;
            obj2.ButtonText = ButtonText;
            obj2.OnSelectedDelegate = OnSelectedDelegate;
        }

        #endregion


        #region Private, protected, internal methods

        private void _SetupInput(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex <= 3)
            {
                int gamepadId = InputManager.Instance.FindDevice("gamepad" + playerIndex.ToString());
                int keyboardId = InputManager.Instance.FindDevice("keyboard" + playerIndex.ToString());
                InputMap.BindCommand(gamepadId, (int)XGamePadDevice.GamePadObjects.A, SetButtonPushed, SetButtonSelected);
                InputMap.BindCommand(keyboardId, (int)Keys.Enter, SetButtonPushed, SetButtonSelected);
            }
        }

        protected override bool _OnNewStyle(GUIStyle style)
        {
            _style = (style as GUIButtonStyle);

            Assert.Fatal(_style != null, "GUIButton._OnNewStyle - control was assigned an invalid style!");

            if (_style == null || !base._OnNewStyle(style))
                return false;

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        string _buttonText = String.Empty;

        ButtonState _buttonState = ButtonState.Normal;
        GUIButtonStyle _style = null;
        OnButtonSelected _selectedDelegate;

        #endregion
    }
}
