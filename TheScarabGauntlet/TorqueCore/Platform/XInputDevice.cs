//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;



namespace GarageGames.Torque.Platform
{
    /// <summary>
    /// XNA Game pad device.
    /// </summary>
    public class XGamePadDevice : TorqueInputDevice
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Threshold for thumb stick up/down/left/right buttons.  If thumbstick pushed beyond this threshold
        /// in any direction then a button event is triggered.  Note that this is in addition to the standard
        /// thumb stick move events.
        /// </summary>
        public const float ThumbThreshold = 0.5f;

        /// <summary>
        /// Threshold for trigger buttons.  If trigger pushed beyond this threshold
        /// in any direction then a button event is triggered.  Note that this is in addition to the standard
        /// trigger move events.
        /// </summary>
        public const float TriggerThreshold = 0.12f;

        static XGamePadDevice()
        {
            // set up object name <--> id list
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Up", (int)GamePadObjects.Up));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Down", (int)GamePadObjects.Down));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Left", (int)GamePadObjects.Left));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Right", (int)GamePadObjects.Right));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Start", (int)GamePadObjects.Start));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Back", (int)GamePadObjects.Back));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbButton", (int)GamePadObjects.LeftThumbButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbButton", (int)GamePadObjects.RightThumbButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftShoulderButton", (int)GamePadObjects.LeftShoulder));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightShoulderButton", (int)GamePadObjects.RightShoulder));

            _xinputObjectIdList.Add(new KeyValuePair<string, int>("A", (int)GamePadObjects.A));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("B", (int)GamePadObjects.B));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("X", (int)GamePadObjects.X));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("Y", (int)GamePadObjects.Y));

            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbX", (int)GamePadObjects.LeftThumbX));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbY", (int)GamePadObjects.LeftThumbY));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbUpButton", (int)GamePadObjects.LeftThumbUpButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbDownButton", (int)GamePadObjects.LeftThumbDownButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbLeftButton", (int)GamePadObjects.LeftThumbLeftButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftThumbRightButton", (int)GamePadObjects.LeftThumbRightButton));

            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbX", (int)GamePadObjects.RightThumbX));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbY", (int)GamePadObjects.RightThumbY));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbUpButton", (int)GamePadObjects.RightThumbUpButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbDownButton", (int)GamePadObjects.RightThumbDownButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbLeftButton", (int)GamePadObjects.RightThumbLeftButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightThumbRightButton", (int)GamePadObjects.RightThumbRightButton));

            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftTrigger", (int)GamePadObjects.LeftTrigger));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightTrigger", (int)GamePadObjects.RightTrigger));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("LeftTriggerButton", (int)GamePadObjects.LeftTriggerButton));
            _xinputObjectIdList.Add(new KeyValuePair<string, int>("RightTriggerButton", (int)GamePadObjects.RightTriggerButton));
        }



        /// <summary>
        /// Create 4 XNA gamepads.  Should be called on program startup.  Automatically called by TorqueEngineComponent.
        /// </summary>
        static public void EnumerateGamepads()
        {
            // add devices for each of the game pads, they hook themselves in
            InputManager.Instance.AddDevice(new XGamePadDevice(PlayerIndex.One));
            InputManager.Instance.AddDevice(new XGamePadDevice(PlayerIndex.Two));
            InputManager.Instance.AddDevice(new XGamePadDevice(PlayerIndex.Three));
            InputManager.Instance.AddDevice(new XGamePadDevice(PlayerIndex.Four));
        }



        static protected List<KeyValuePair<String, int>> _xinputObjectIdList = new List<KeyValuePair<string, int>>();

        #endregion


        #region Constructors

        public XGamePadDevice(PlayerIndex player)
        {
            _controllerId = player;
            _deviceTypeId = TorqueInputDevice.GamePadId;
            _objectIdList = _xinputObjectIdList;

            _lowSpeedVibration = 0.0f;
            _highSpeedVibration = 0.0f;
        }

        #endregion


        #region Public properties, operators, constants, and enums


        /// <summary>
        /// Is the device still connected.
        /// </summary>
        public bool IsConnected
        {
            get { return _IsConnected; }
        }

        /// <summary>
        /// Was the deviced connected before, but is not connected now.
        /// </summary>
        public bool HasBeenDisconnected
        {
            get { return _WasConnected && !_IsConnected; }
        }

        /// <summary>
        /// Gamepad objects which produce events.
        /// </summary>
        public enum GamePadObjects
        {
            None = 0,
            Up = 1 << 0,
            Down = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3,
            Start = 1 << 4,
            Back = 1 << 5,
            LeftThumbButton = 1 << 6, // we append "Button" to this one to differentiate it from the stick X/Y values
            RightThumbButton = 1 << 7, // we append "Button" to this one to differentiate it from the stick X/Y values
            LeftShoulder = 1 << 8,
            RightShoulder = 1 << 9,
            A = 1 << 10,
            B = 1 << 11,
            X = 1 << 12,
            Y = 1 << 13,
            LeftThumbX = 1 << 14, // this represents the float x-axis value of the thumb stick
            LeftThumbY = 1 << 15, // this represents the float y-axis value of the thumb stick
            LeftThumbUpButton = 1 << 16, // this represents the float value of the thumb stick interpreted as a button press or release
            LeftThumbDownButton = 1 << 17, // this represents the float value of the thumb stick interpreted as a button press or release
            LeftThumbLeftButton = 1 << 18, // this represents the float value of the thumb stick interpreted as a button press or release
            LeftThumbRightButton = 1 << 19, // this represents the float value of the thumb stick interpreted as a button press or release
            RightThumbX = 1 << 20, // this represents the float x-axis value of the thumb stick
            RightThumbY = 1 << 21, // this represents the float y-axis value of the thumb stick
            RightThumbUpButton = 1 << 22, // this represents the float value of the thumb stick interpreted as a button press or release
            RightThumbDownButton = 1 << 23, // this represents the float value of the thumb stick interpreted as a button press or release
            RightThumbLeftButton = 1 << 24, // this represents the float value of the thumb stick interpreted as a button press or release
            RightThumbRightButton = 1 << 25, // this represents the float value of the thumb stick interpreted as a button press or release
            LeftTrigger = 1 << 26, // this represents the float value of the trigger 
            RightTrigger = 1 << 27, // this represents the float value of the trigger 
            LeftTriggerButton = 1 << 28, // this represents the float value of the trigger interpreted as a button press or release
            RightTriggerButton = 1 << 29 // this represents the float value of the trigger interpreted as a button press or release
        }

        #endregion


        #region Public methods

        override public void PumpDevice()
        {
            // Processing the Controller Input
            GamePadState oldState = _state;
            _state = GamePad.GetState(_controllerId);
            _IsConnected = _state.IsConnected;
            _WasConnected = oldState.IsConnected;

            if (!_WasConnected && !_IsConnected)
                return;

            // use GamePadButtons enum to test buttons...when defined in both, GamePadButtons = GamePadObjects
            _SignalButtonState(oldState.Buttons.A == ButtonState.Pressed, _state.Buttons.A == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.A);
            _SignalButtonState(oldState.Buttons.B == ButtonState.Pressed, _state.Buttons.B == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.B);
            _SignalButtonState(oldState.Buttons.X == ButtonState.Pressed, _state.Buttons.X == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.X);
            _SignalButtonState(oldState.Buttons.Y == ButtonState.Pressed, _state.Buttons.Y == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Y);
            _SignalButtonState(oldState.Buttons.Start == ButtonState.Pressed, _state.Buttons.Start == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Start);
            _SignalButtonState(oldState.Buttons.Back == ButtonState.Pressed, _state.Buttons.Back == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Back);
            _SignalButtonState(oldState.Buttons.LeftShoulder == ButtonState.Pressed, _state.Buttons.LeftShoulder == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.LeftShoulder);
            _SignalButtonState(oldState.Buttons.LeftStick == ButtonState.Pressed, _state.Buttons.LeftStick == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.LeftThumbButton);
            _SignalButtonState(oldState.Buttons.RightShoulder == ButtonState.Pressed, _state.Buttons.RightShoulder == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.RightShoulder);
            _SignalButtonState(oldState.Buttons.RightStick == ButtonState.Pressed, _state.Buttons.RightStick == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.RightThumbButton);
            _SignalButtonState(oldState.DPad.Up == ButtonState.Pressed, _state.DPad.Up == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Up);
            _SignalButtonState(oldState.DPad.Down == ButtonState.Pressed, _state.DPad.Down == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Down);
            _SignalButtonState(oldState.DPad.Left == ButtonState.Pressed, _state.DPad.Left == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Left);
            _SignalButtonState(oldState.DPad.Right == ButtonState.Pressed, _state.DPad.Right == ButtonState.Pressed, _WasConnected, _IsConnected, GamePadObjects.Right);

            _SignalThumbState(oldState.ThumbSticks.Left.X, _state.ThumbSticks.Left.X, XGamePadDevice.ThumbThreshold, _WasConnected, _IsConnected, GamePadObjects.LeftThumbX, GamePadObjects.LeftThumbLeftButton, GamePadObjects.LeftThumbRightButton);
            _SignalThumbState(oldState.ThumbSticks.Left.Y, _state.ThumbSticks.Left.Y, XGamePadDevice.ThumbThreshold, _WasConnected, _IsConnected, GamePadObjects.LeftThumbY, GamePadObjects.LeftThumbDownButton, GamePadObjects.LeftThumbUpButton);
            _SignalThumbState(oldState.ThumbSticks.Right.X, _state.ThumbSticks.Right.X, XGamePadDevice.ThumbThreshold, _WasConnected, _IsConnected, GamePadObjects.RightThumbX, GamePadObjects.RightThumbLeftButton, GamePadObjects.RightThumbRightButton);
            _SignalThumbState(oldState.ThumbSticks.Right.Y, _state.ThumbSticks.Right.Y, XGamePadDevice.ThumbThreshold, _WasConnected, _IsConnected, GamePadObjects.RightThumbY, GamePadObjects.RightThumbDownButton, GamePadObjects.RightThumbUpButton);

            _SignalTriggerState(oldState.Triggers.Left, _state.Triggers.Left, XGamePadDevice.TriggerThreshold, _WasConnected, _IsConnected, GamePadObjects.LeftTrigger, GamePadObjects.LeftTriggerButton);
            _SignalTriggerState(oldState.Triggers.Right, _state.Triggers.Right, XGamePadDevice.TriggerThreshold, _WasConnected, _IsConnected, GamePadObjects.RightTrigger, GamePadObjects.RightTriggerButton);
        }

        #endregion


        #region Vibration public methods

        /// <summary>
        /// Sets the vibration motor speeds.
        /// </summary>
        /// <param name="lowSpeed">The speed of the low-frequency motor, between 0.0 and 1.0</param>
        /// <param name="highSpeed">The speed of the high-frequency motor, between 0.0 and 1.0</param>
        public void SetVibration(float lowSpeed, float highSpeed)
        {
            _lowSpeedVibration = lowSpeed;
            _highSpeedVibration = highSpeed;

            Microsoft.Xna.Framework.Input.GamePad.SetVibration(_controllerId, _lowSpeedVibration, _highSpeedVibration);
        }



        /// <summary>
        /// Sets the low frequency motor vibration speed.
        /// </summary>
        /// <param name="lowSpeed">The speed of the low-frequency motor, between 0.0 and 1.0</param>
        public void SetLowFrequencyVibration(float lowSpeed)
        {
            SetVibration(lowSpeed, _highSpeedVibration);
        }



        /// <summary>
        /// Sets the high frequency motor vibration speed.
        /// </summary>
        /// <param name="highSpeed">The speed of the high-frequency motor, between 0.0 and 1.0</param>
        public void SetHighFrequencyVibration(float highSpeed)
        {
            SetVibration(_lowSpeedVibration, highSpeed);
        }



        /// <summary>
        /// Stop vibration of both motors.
        /// </summary>
        public void StopVibration()
        {
            SetVibration(0.0f, 0.0f);
        }

        #endregion


        #region Private, protected, internal methods

        void _SignalButtonState(bool wasDown, bool isDown, bool wasConnected, bool isConnected, GamePadObjects button)
        {
            // treat dis-connected as up (and assume ill-defined coming in)
            if (!isConnected)
                isDown = false;
            if (!wasConnected)
                wasDown = false;

            if (isDown != wasDown)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)button);

                // data fill in
                data.Value = isDown ? 1.0f : 0.0f;
                data.EventAction = isDown ? Action.Make : Action.Break;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }
        }



        void _SignalThumbState(float oldState, float newState, float threshold, bool wasConnected, bool isConnected, GamePadObjects thumb, GamePadObjects negButton, GamePadObjects posButton)
        {
            if (!wasConnected)
                oldState = 0;
            if (!isConnected)
                newState = 0;
            bool posWasDown = oldState > threshold;
            bool posIsDown = newState > threshold;
            bool negWasDown = oldState < -threshold;
            bool negIsDown = newState < -threshold;

            if (oldState != newState)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)thumb);

                // data fill in
                data.Value = newState;
                data.EventAction = Action.Move;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }

            // Positive axis direction button
            if (posWasDown != posIsDown)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)posButton);

                // data fill in
                data.Value = posIsDown ? 1.0f : 0.0f;
                data.EventAction = posIsDown ? Action.Make : Action.Break;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }

            // Negative axis direction button
            if (negWasDown != negIsDown)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)negButton);

                // data fill in
                data.Value = negIsDown ? 1.0f : 0.0f;
                data.EventAction = negIsDown ? Action.Make : Action.Break;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }
        }



        void _SignalTriggerState(float oldState, float newState, float threshold, bool wasConnected, bool isConnected, GamePadObjects trigger, GamePadObjects button)
        {
            if (!wasConnected)
                oldState = 0;
            if (!isConnected)
                newState = 0;
            bool wasDown = oldState > threshold;
            bool isDown = newState > threshold;

            if (oldState != newState)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)trigger);

                // data fill in
                data.Value = newState;
                data.EventAction = Action.Move;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }
            if (wasDown != isDown)
            {
                TorqueInputDevice.InputEventData data = _getEventData((int)button);

                // data fill in
                data.Value = isDown ? 1.0f : 0.0f;
                data.EventAction = isDown ? Action.Make : Action.Break;

                TorqueEventManager.PostEvent(GamepadEvent, data);
            }
        }

        #endregion


        #region Private, protected, internal fields

        GamePadState _state;
        PlayerIndex _controllerId;
        bool _IsConnected;
        bool _WasConnected;

        float _lowSpeedVibration;
        float _highSpeedVibration;

        #endregion
    }



    /// <summary>
    /// XNA Keyboard device.
    /// </summary>
    public class XKeyboardDevice : TorqueInputDevice
    {

        #region Static methods, fields, constructors

        static XKeyboardDevice()
        {
            // set up object name <--> id list
            Keys[] keys = TorqueUtil.GetEnumValues<Keys>();

            foreach (Keys key in keys)
                _xinputObjectIdList.Add(new KeyValuePair<String, int>(key.ToString(), (int)key));
        }

        /// <summary>
        /// Create XNA keyboard.  Should be called on program startup.  Automatically called by TorqueEngineComponent.
        /// </summary>
        static public void EnumerateKeyboards()
        {
            InputManager.Instance.AddDevice(new XKeyboardDevice());
        }

        static protected List<KeyValuePair<String, int>> _xinputObjectIdList = new List<KeyValuePair<string, int>>();

        #endregion


        #region Constructors

        public XKeyboardDevice()
        {
            _deviceTypeId = TorqueInputDevice.KeyboardId;
            _objectIdList = _xinputObjectIdList;
        }

        #endregion


        #region Public methods

        override public void PumpDevice()
        {
            // Processing the keyboard input
            KeyboardState currentState = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            Keys[] pressedKeys = currentState.GetPressedKeys();

            // search for break events
            for (int i = 0; i < _pressedKeys.Count; i++)
            {
                Keys key = _pressedKeys[i];
                if (!currentState.IsKeyDown(key))
                {
                    _pressedKeys.RemoveAt(i);
                    i--;

                    TorqueInputDevice.InputEventData data = _getEventData((int)key);

                    // data fill in
                    data.Value = 0.0f;
                    data.EventAction = Action.Break;

                    TorqueEventManager.PostEvent(KeyboardEvent, data);
                }
            }

            foreach (Keys key in pressedKeys)
            {
                if (!_pressedKeys.Contains(key))
                {
                    _pressedKeys.Add(key);

                    TorqueInputDevice.InputEventData data = _getEventData((int)key);

                    // data fill in
                    data.Value = 1.0f;
                    data.EventAction = Action.Make;
                    if (key != Keys.LeftAlt && key != Keys.RightAlt &&
                        key != Keys.RightAlt && key != Keys.RightShift &&
                        key != Keys.LeftControl && key != Keys.RightControl)
                    {
                        if (currentState.IsKeyDown(Keys.LeftControl) || currentState.IsKeyDown(Keys.RightControl))
                            data.Modifier |= Action.Ctrl;
                        if (currentState.IsKeyDown(Keys.LeftShift) || currentState.IsKeyDown(Keys.RightShift))
                            data.Modifier |= Action.Shift;
                    }

                    TorqueEventManager.PostEvent(KeyboardEvent, data);
                }
            }
            // cafTODO: need to produce ascii events too
        }

        #endregion


        #region Private, protected, internal fields

        List<Keys> _pressedKeys = new List<Keys>();

        #endregion
    }

#if !XBOX
    /// <summary>
    /// XNA Mouse device.
    /// </summary>
    public class XMouseDevice : TorqueInputDevice
    {

        #region Static methods, fields, constructors

        static XMouseDevice()
        {
            // set up object name <--> id list
            MouseObjects sample = new MouseObjects();
            MouseObjects[] mouseObjects = (MouseObjects[])Enum.GetValues(sample.GetType());

            foreach (MouseObjects mouseObject in mouseObjects)
                _xinputObjectIdList.Add(new KeyValuePair<String, int>(mouseObject.ToString(), (int)mouseObject));
        }



        /// <summary>
        /// Create XNA mouse.  Should be called on program startup.  Automatically called by TorqueEngineComponent.
        /// </summary>
        static public void EnumerateMouses()
        {
            InputManager.Instance.AddDevice(new XMouseDevice());
        }

        static protected List<KeyValuePair<String, int>> _xinputObjectIdList = new List<KeyValuePair<string, int>>();

        #endregion


        #region Constructors

        public XMouseDevice()
        {
            _deviceTypeId = TorqueInputDevice.MouseId;
            _objectIdList = _xinputObjectIdList;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Mouse objects which produce events.
        /// </summary>
        public enum MouseObjects
        {
            LeftButton, RightButton, MiddleButton, X, Y, Wheel
        }

        #endregion


        #region Public methods

        override public void PumpDevice()
        {
            MouseState newState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            if (newState.LeftButton != _state.LeftButton)
                _SignalButtonEvent(newState.LeftButton == ButtonState.Pressed, MouseObjects.LeftButton);
            if (newState.MiddleButton != _state.MiddleButton)
                _SignalButtonEvent(newState.MiddleButton == ButtonState.Pressed, MouseObjects.MiddleButton);
            if (newState.RightButton != _state.RightButton)
                _SignalButtonEvent(newState.RightButton == ButtonState.Pressed, MouseObjects.RightButton);

            if (newState.X != 0)
                _SignalMoveEvent(newState.X - _state.X, MouseObjects.X);
            if (newState.Y != 0)
                _SignalMoveEvent(newState.Y - _state.Y, MouseObjects.Y);
            if (newState.ScrollWheelValue != 0)
                _SignalMoveEvent(newState.ScrollWheelValue, MouseObjects.Wheel);

            _state = newState;
        }
        #endregion


        #region Private, protected, internal methods

        void _SignalButtonEvent(bool down, MouseObjects button)
        {
            TorqueInputDevice.InputEventData data = _getEventData((int)button);

            // data fill in
            data.Value = down ? 1.0f : 0.0f;
            data.EventAction = down ? Action.Make : Action.Break;
            TorqueEventManager.PostEvent(MouseEvent, data);
        }



        void _SignalMoveEvent(int val, MouseObjects axis)
        {
            TorqueInputDevice.InputEventData data = _getEventData((int)axis);

            // data fill in
            data.Value = (float)-val;
            data.EventAction = Action.Move;

            MouseState state = Microsoft.Xna.Framework.Input.Mouse.GetState();
            if (state.LeftButton == ButtonState.Pressed)
                data.Modifier |= Action.LeftClick;
            if (state.MiddleButton == ButtonState.Pressed)
                data.Modifier |= Action.MiddleClick;
            if (state.RightButton == ButtonState.Pressed)
                data.Modifier |= Action.RightClick;
            TorqueEventManager.PostEvent(MouseEvent, data);
        }

        #endregion


        #region Private, protected, internal fields

        MouseState _state;

        #endregion
    }
#endif
}
