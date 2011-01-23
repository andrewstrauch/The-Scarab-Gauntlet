//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// An editable text field GUI control.
    /// </summary>
    class GUITextEdit : GUIText, ITickObject, IDisposable
    {
        /// <summary>
        /// A delegate called to allow a user to validate text entered into a text edit field.
        /// </summary>
        public delegate void TextValidateDelegate();


        #region Public properties, operators, constants, and enums

        public override string Text
        {
            get { return base.Text; }
            set
            {
                if (_text == value)
                    return;

                base.Text = value;

                // reset the cursor pos in case the text gets shorter
                _cursorPos = _text.Length;
            }
        }



        /// <summary>
        /// Delegate called when text is entered into this control.
        /// </summary>
        public TextValidateDelegate OnValidateText
        {
            get { return _onValidateText; }
            set { _onValidateText = value; }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUITextEdit.OnRender");
#endif

            base.OnRender(offset, updateRect);

            // only render the cursor if the control is focused
            if (this != GUICanvas.Instance.GetFocusControl())
            {
#if DEBUG
                Profiler.Instance.EndBlock("GUITextEdit.OnRender");
#endif
                return;
            }

            RectangleF cursorRect = new RectangleF();
            cursorRect.Point = offset;
            cursorRect.Y += 2;
            cursorRect.Width = 2;
            cursorRect.Height = updateRect.Height - 2;
            if (_cursorPos > 0)
                cursorRect.X += (Style as GUITextStyle).Font.Instance.MeasureString(_text.Substring(0, _cursorPos)).X + 3;

            DrawUtil.RectFill(cursorRect, Color.Gray);

#if DEBUG
            Profiler.Instance.EndBlock("GUITextEdit.OnRender");
#endif
        }



        // adltodo: There's a lot of functionality that isn't here, like highlighting for instance.
        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            if (_inputMap != null)
            {
                if (_inputMap.ProcessInput(data))
                    return true;
            }

            // only need keyboard events
            if (data.DeviceTypeId != TorqueInputDevice.GetDeviceTypeId("keyboard"))
                return base.OnInputEvent(ref data);

            // capture break events, but we don't need to use them
            if (data.EventAction == TorqueInputDevice.Action.Break)
            {
                if (data.ObjectId == (int)_heldKey)
                    _heldKey = Keys.None;

                return true;
            }

            // record the state of the shift modifier
            _shiftPressed = (data.Modifier & TorqueInputDevice.Action.Shift) != 0;

            // only handle make events otherwise!
            if (data.EventAction != TorqueInputDevice.Action.Make)
                return false;

            // let the parent handle it for control focusing
            if (data.ObjectId == (int)Keys.Tab)
                return base.OnInputEvent(ref data);

            // record this key as pressed
            _heldKey = (Keys)data.ObjectId;
            _keyPressedTime = TorqueEngineComponent.Instance.TorqueTime;

            return _ProcessKey(data.ObjectId);
        }



        public override void OnLoseFocus(GUIControl newFocusCtrl)
        {
            base.OnLoseFocus(newFocusCtrl);

            _heldKey = Keys.None;
        }



        public void ProcessTick(Move move, float dt)
        {
            if (_heldKey != Keys.None && TorqueEngineComponent.Instance.TorqueTime - _keyPressedTime > _keyPressRepeatDelay)
            {
                if (TorqueEngineComponent.Instance.RealTime - _keyProcessTime > _keyPressRepeatTimeaout)
                    _ProcessKey((int)_heldKey);
            }
        }

        public void InterpolateTick(float k) { }

        #endregion


        #region Private, protected, internal methods

        // this may fit better in InputEventData or utility
        protected char _GetCharacterFromKey(int key, bool shift)
        {
            switch ((Keys)key)
            {
                case Keys.OemSemicolon: return shift ? ':' : ';';
                case Keys.OemPlus: return shift ? '+' : '=';
                case Keys.OemComma: return shift ? '<' : ',';
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.OemQuestion: return shift ? '?' : '/';
                case Keys.OemTilde: return shift ? '~' : '`';
                case Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Keys.OemPipe: return shift ? '|' : '\\';
                case Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Keys.OemQuotes: return shift ? '\"' : '\'';

                case Keys.NumPad0: return '0';
                case Keys.NumPad1: return '1';
                case Keys.NumPad2: return '2';
                case Keys.NumPad3: return '3';
                case Keys.NumPad4: return '4';
                case Keys.NumPad5: return '5';
                case Keys.NumPad6: return '6';
                case Keys.NumPad7: return '7';
                case Keys.NumPad8: return '8';
                case Keys.NumPad9: return '9';
                case Keys.Multiply: return '*';
                case Keys.Add: return '+';
                case Keys.Subtract: return '-';
                case Keys.Decimal: return '.';
                case Keys.Divide: return '/';

                case Keys.D0: return shift ? ')' : '0';
                case Keys.D1: return shift ? '!' : '1';
                case Keys.D2: return shift ? '@' : '2';
                case Keys.D3: return shift ? '#' : '3';
                case Keys.D4: return shift ? '$' : '4';
                case Keys.D5: return shift ? '%' : '5';
                case Keys.D6: return shift ? '^' : '6';
                case Keys.D7: return shift ? '&' : '7';
                case Keys.D8: return shift ? '*' : '8';
                case Keys.D9: return shift ? '(' : '9';

                case Keys.Space: return ' ';
            }

            if (char.IsLetter((char)key))
                return shift ? char.ToUpper((char)key) : char.ToLower((char)key);

            return '\0';
        }



        protected void _ValidateText()
        {
            if (_onValidateText != null)
                _onValidateText();
        }



        protected override bool _OnWake()
        {
            _Ticking = true;
            ProcessList.Instance.AddTickCallback(this);
            return base._OnWake();
        }


        protected override void _OnSleep()
        {
            _Ticking = false;
            ProcessList.Instance.RemoveObject(this);
            base._OnSleep();
        }



        protected virtual bool _ProcessKey(int key)
        {
            // record the last time a key was processed!
            _keyProcessTime = TorqueEngineComponent.Instance.TorqueTime;

            // process they pressed key
            switch (key)
            {
                // validate the text
                case (int)Keys.Enter:
                    _ValidateText();
                    return true;

                // backspace
                case (int)Keys.Back:
                    if (_cursorPos > 0)
                    {
                        _cursorPos--;
                        _text = _text.Remove(_cursorPos, 1);
                    }
                    return true;

                // delete
                case (int)Keys.Delete:
                    if (_cursorPos < _text.Length)
                        _text = _text.Remove(_cursorPos, 1);

                    return true;

                // left arrow
                case (int)Keys.Left:
                    if (_cursorPos > 0)
                        _cursorPos--;

                    return true;

                // right arrow
                case (int)Keys.Right:
                    if (_cursorPos < _text.Length)
                        _cursorPos++;

                    return true;

                // up arrow and home
                case (int)Keys.Up:
                case (int)Keys.Home:
                    _cursorPos = 0;
                    return true;

                // down arrow and end
                case (int)Keys.Down:
                case (int)Keys.End:
                    _cursorPos = _text.Length;
                    return true;
            }

            char c = _GetCharacterFromKey(key, _shiftPressed);

            if (c != '\0')
            {
                if (_cursorPos == 0)
                    _text = c + _text;
                else if (_cursorPos == _text.Length)
                    _text = _text + c;
                else
                    _text = _text.Insert(_cursorPos, c.ToString());

                _cursorPos++;
            }

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        private TextValidateDelegate _onValidateText;
        private int _cursorPos;
        private Keys _heldKey;
        private bool _shiftPressed;
        private float _keyPressedTime = float.NegativeInfinity;
        private float _keyProcessTime = float.NegativeInfinity;
        private float _keyPressRepeatDelay = 500;
        private float _keyPressRepeatTimeaout = 30;
        private bool _Ticking = false;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (_Ticking)
                ProcessList.Instance.RemoveObject(this);

            base.Dispose();
        }

        #endregion
    }
}