//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.Sim
{
    /// <summary>
    /// Move component representing a button.  MoveButtons are either pushed or not.
    /// </summary>
    public struct MoveButton
    {

        #region Constructors

        public MoveButton(bool pushed)
        {
            _pushed = pushed;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether or not the button is pushed.
        /// </summary>
        public bool Pushed
        {
            get { return _pushed; }
        }

        #endregion


        #region Private, protected, internal methods

        internal bool _pushed;

        #endregion
    }



    /// <summary>
    /// Move component representing a trigger.  MoveTriggers have values between 0 and 1.
    /// </summary>
    public struct MoveTrigger
    {

        #region Constructors

        public MoveTrigger(float value)
        {
            _value = value;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public float Value
        {
            get { return _value; }
        }

        #endregion


        #region Private, protected, internal methods

        internal float _value; // 0 to 1

        #endregion
    }



    /// <summary>
    /// Move component representing a single-axis of a joystick.  MoveLevers have values between -1 and 1.
    /// </summary>
    public struct MoveLever
    {

        #region Constructors

        public MoveLever(float value)
        {
            _value = value;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public float Value
        {
            get { return _value; }
        }

        #endregion


        #region Private, protected, internal methods

        internal float _value; // -1 to 1

        #endregion
    }



    /// <summary>
    /// Move component representing a joystick or gamepad stick.  MoveSticks have an X and a Y axis,
    /// each having values between -1 and 1.
    /// </summary>
    public struct MoveStick
    {

        #region Constructors

        public MoveStick(float x, float y)
        {
            _x = x;
            _y = y;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public float X
        {
            get { return _x; }
        }

        public float Y
        {
            get { return _y; }
        }

        #endregion


        #region Private, protected, internal methods

        internal float _x; // -1 to 1
        internal float _y; // -1 to 1

        #endregion
    }



#if USE_MOUSE_LOOK
    // This (might) be useful when we go to 3D.  It would let move manager
    // handle look direction and allow mouse look or stick input.
    public struct MoveMouseLook
    {
        //======================================================
    #region Constructors
        public MoveMouseLook(Quaternion rot) { _rot = rot; }
    #endregion

        //======================================================
    #region Public properties, operators, constants, and enums
        public Quaternion Rotation { get { return _rot; } }
    #endregion

        //======================================================
    #region Private, protected, internal methods
        internal Quaternion _rot;
    #endregion
    }
#endif



    /// <summary>
    /// An object representing a single move of a game object.  A move is abstracted from the
    /// input driving a game object in the sense that a MoveStick, for example, can be driven
    /// by either gamepad input or keyboard input.  Moves are comprised of a variable number of 
    /// move components: MoveButtons, MoveTriggers, MoveLevers, and MoveSticks.  The move manager 
    /// can be configured to produce moves with the desired number of move components.  
    /// </summary>
    public class Move
    {

        #region Constructors

        public Move() { }

        public Move(MoveButton[] buttons, MoveTrigger[] triggers, MoveLever[] levers, MoveStick[] sticks)
        {
            _buttons = buttons;
            _triggers = triggers;
            _levers = levers;
            _sticks = sticks;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// List of buttons on the move.
        /// </summary>
        public ReadOnlyArray<MoveButton> Buttons
        {
            get { return new ReadOnlyArray<MoveButton>(_buttons); }
        }

        /// <summary>
        /// List of triggers.
        /// </summary>
        public ReadOnlyArray<MoveTrigger> Triggers
        {
            get { return new ReadOnlyArray<MoveTrigger>(_triggers); }
        }

        /// <summary>
        /// List of levers on the move.
        /// </summary>
        public ReadOnlyArray<MoveLever> Levers
        {
            get { return new ReadOnlyArray<MoveLever>(_levers); }
        }

        /// <summary>
        /// List of sticks on the move.
        /// </summary>
        public ReadOnlyArray<MoveStick> Sticks
        {
            get { return new ReadOnlyArray<MoveStick>(_sticks); }
        }

        #endregion


        #region Private, protected, internal fields

        internal MoveButton[] _buttons;
        internal MoveTrigger[] _triggers;
        internal MoveLever[] _levers;
        internal MoveStick[] _sticks;

        #endregion
    }



    internal struct ControlWidget
    {

        #region Public methods

        public void Init(MoveManager moveManager)
        {
            ConfigureTracking(moveManager.DefaultRampUpTime, moveManager.DefaultRampUpFunction, moveManager.DefaultRampDownTime, moveManager.DefaultRampDownFunction);
            ConfigureScaling(moveManager.DefaultScaleFunction);
        }

        public void ConfigureTracking(float rampUpTime, float[] rampUpValue, float rampDownTime, float[] rampDownValue)
        {
            _rampUpTime = rampUpTime;
            _rampDownTime = rampDownTime;
            _SetRampData(rampUpTime, rampUpValue, out _rampUp);
            _SetRampData(rampDownTime, rampDownValue, out _rampDown);

            // Do some error checking to make sure ramp functions are appropriate.
            if (rampUpValue != null)
            {
                Assert.Fatal(rampUpValue.Length > 1 && Math.Abs(rampUpValue[0]) < 0.001f && Math.Abs(rampUpValue[rampUpValue.Length - 1] - 1.0f) < 0.001f, "ramp up should go from 0 to 1");
                for (int i = 1; i < rampUpValue.Length; i++)
                    Assert.Fatal(rampUpValue[i - 1] <= rampUpValue[i], "ramp up should be monotonic increasing");
            }
            if (rampDownValue != null)
            {
                Assert.Fatal(rampDownValue.Length > 1 && Math.Abs(rampDownValue[rampDownValue.Length - 1]) < 0.001f && Math.Abs(rampDownValue[0] - 1.0f) < 0.001f, "ramp down should go from 1 to 0");
                for (int i = 1; i < rampDownValue.Length; i++)
                    Assert.Fatal(rampDownValue[i - 1] >= rampDownValue[i], "ramp up should be monotonic decreasing");
            }
        }

        public void ConfigureScaling(float[] scaleValues)
        {
            _scale = null;

            if (scaleValues == null || scaleValues.Length < 2)
                return;

            Assert.Fatal(scaleValues.Length <= MoveManager.MAX_FIT_POINTS, "Value array exceeds maximum allowed length of " + MoveManager.MAX_FIT_POINTS);
            if (scaleValues.Length > MoveManager.MAX_FIT_POINTS)
                return;

            _scale = new float[scaleValues.Length];
            PolyFit.FitData(0.0f, 1.0f, scaleValues, _scale);

            // Do some error checking to make sure scale function is appropriate.
            if (scaleValues != null)
            {
                Assert.Fatal(scaleValues.Length > 1 && Math.Abs(scaleValues[0]) < 0.001f && Math.Abs(scaleValues[scaleValues.Length - 1] - 1.0f) < 0.001f, "scale should go from 0 to 1");
                for (int i = 1; i < scaleValues.Length; i++)
                    Assert.Fatal(scaleValues[i - 1] <= scaleValues[i], "scale should be monotonic increasing");
            }
        }

        public void SetDirectly(float val)
        {
            val = MathHelper.Clamp(val, 0.0f, 1.0f);
            if (_scale != null)
                // scale it...
                val = PolyFit.ComputeData(val, 0.0f, 1.0f, _scale);
            _target = val;
            _direct = true;
        }

        public void SetTarget(float val)
        {
            val = MathHelper.Clamp(val, 0.0f, 1.0f);
            _target = val;
            _direct = false;
        }

        public float Update(float currentValue, float dt)
        {
            if (_direct)
                return _target;

            // make sure range is right (could be outside range if input is from
            // combination of two widgets).
            currentValue = MathHelper.Clamp(currentValue, 0.0f, 1.0f);

            // Note: ramp up or ramp down?
            bool useRampDown = _target < 0.5f;

            // determine direction of target (make dt negative if direction is down)
            if (useRampDown)
                dt *= -1.0f;

            if (_time * dt >= 0.0f)
                // continue in the same direction
                _time += dt;
            else
                // starting in other direction
                _time = dt;

            // keep in range
            _time = MathHelper.Clamp(_time, -_rampDownTime, _rampUpTime);

            if (useRampDown)
            {
                // ramping down
                float rampdown = _rampDown != null ? PolyFit.ComputeData(-_time, 0.0f, _rampDownTime, _rampDown) : 0.0f;
                currentValue = Math.Min(rampdown, currentValue);
            }
            else
            {
                // ramping up
                float rampup = _rampUp != null ? PolyFit.ComputeData(_time, 0.0f, _rampUpTime, _rampUp) : 1.0f;
                currentValue = Math.Max(rampup, currentValue);
            }
            return currentValue;
        }

        #endregion


        #region Private, protected, internal methods

        bool _SetRampData(float time, float[] value, out float[] poly)
        {
            poly = null;

            if (value == null)
                return true;

            Assert.Fatal(value.Length <= MoveManager.MAX_FIT_POINTS, "Value array exceeds maximum allowed length of " + MoveManager.MAX_FIT_POINTS);
            if (value.Length > MoveManager.MAX_FIT_POINTS)
                return false;

            poly = new float[value.Length];
            PolyFit.FitData(0.0f, time, value, poly);

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        float _target;
        float _time;
        bool _direct;
        float[] _scale;
        float[] _rampUp;
        float[] _rampDown;
        float _rampUpTime;
        float _rampDownTime;

        #endregion
    }



    /// <summary>
    /// Enumeration of mappings between input and move components.
    /// </summary>
    public enum MoveMapTypes
    {
        /// <summary>
        /// Map a button to a button.
        /// </summary>
        Button,
        /// <summary>
        /// Map an analog input onto a move trigger.  Scale function is used to scale input.
        /// </summary>
        TriggerAnalog,
        /// <summary>
        /// Map a digital input onto a move trigger.  Ramp functions are used to change trigger value over time.
        /// </summary>
        TriggerDigital,
        /// <summary>
        /// Map an analog input onto a move lever.  Scale function is used to scale input.
        /// </summary>
        LeverAnalog,
        /// <summary>
        /// Map a digital input onto a move lever in the negative direction.  Ramp functions are used to change the lever value over time.
        /// </summary>
        LeverDigitalMinus,
        /// <summary>
        /// Map a digital input onto a move lever in the positive direction.  Ramp functions are used to change the lever value over time.
        /// </summary>
        LeverDigitalPlus,
        /// <summary>
        /// Map an analog input onto a move stick horizontal component.  Scale function is used to scale input.
        /// </summary>
        StickAnalogHorizontal,
        /// <summary>
        /// Map a digital input onto left on a move stick.  Ramp functions are used to change the stick value over time.
        /// </summary>
        StickDigitalLeft,
        /// <summary>
        /// Map a digital input onto right on a move stick.  Ramp functions are used to change the stick value over time.
        /// </summary>
        StickDigitalRight,
        /// <summary>
        /// Map an analog input onto a move stick vertical component.  Scale function is used to scale input.
        /// </summary>
        StickAnalogVertical,
        /// <summary>
        /// Map a digital input onto down on a move stick.  Ramp functions are used to change the stick value over time.
        /// </summary>
        StickDigitalDown,
        /// <summary>
        /// Map a digital input onto up on a move stick.  Ramp functions are used to change the stick value over time.
        /// </summary>
        StickDigitalUp,
    }



    public delegate Move GenerateMoveDelegate();



    /// <summary>
    /// Manages the moves for a single player.  MoveManager can be used to configure what components will be included
    /// in in a move for a given player and how that move is affected by input over time.
    /// </summary>
    public class MoveManager
    {

        #region Constructors
        public MoveManager()
        {
            // set defaults, and make sure arrays exist
            NumButtons = 4;
            NumTriggers = 2;
            NumLevers = 0;
            NumSticks = 2;
        }
        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// If true, the number of move components cannot be changed (until lock set to false).
        /// This can be used to prevent InputMaps from changing the number of move components
        /// when new move binds are created.
        /// </summary>
        public bool LockMoveCounts
        {
            get { return _lockMoveCounts; }
            set { _lockMoveCounts = value; }
        }

        /// <summary>
        /// Number of MoveButtons produced for each Move.  Configure this to match what your game object
        /// expects (or let input map do it when you call BindMove).
        /// </summary>
        public int NumButtons
        {
            get { return _buttons.Length; }
            set
            {
                if (LockMoveCounts)
                    return;
                int old = _buttons == null ? 0 : _buttons.Length;
                TorqueUtil.ResizeArray(ref _buttonRepeat, value);
                TorqueUtil.ResizeArray(ref _buttons, value);
                TorqueUtil.ResizeArray(ref _previousButtons, value);
                for (int i = old; i < _buttons.Length; i++)
                    _buttonRepeat[i] = true;
            }
        }

        /// <summary>
        /// Number of MoveTriggers produced for each Move.  Configure this to match what your game object
        /// expects (or let input map do it when you call BindMove).
        /// </summary>
        public int NumTriggers
        {
            get { return _triggers.Length; }
            set
            {
                if (LockMoveCounts)
                    return;

                int old = _triggers == null ? 0 : _triggers.Length;
                TorqueUtil.ResizeArray(ref _triggers, value);
                TorqueUtil.ResizeArray(ref _triggerControls, value);
                for (int i = old; i < _triggers.Length; i++)
                    _triggerControls[i].Init(this);
            }
        }

        /// <summary>
        /// Number of MoveLevers produced for each Move.  Configure this to match what your game object
        /// expects (or let input map do it when you call BindMove).
        /// </summary>
        public int NumLevers
        {
            get { return _levers.Length; }
            set
            {
                if (LockMoveCounts)
                    return;
                int old = _levers == null ? 0 : _levers.Length;
                TorqueUtil.ResizeArray(ref _levers, value);
                TorqueUtil.ResizeArray(ref _leverControlsPositive, value);
                TorqueUtil.ResizeArray(ref _leverControlsNegative, value);
                for (int i = old; i < _levers.Length; i++)
                {
                    _leverControlsPositive[i].Init(this);
                    _leverControlsNegative[i].Init(this);
                }
            }
        }

        /// <summary>
        /// Number of MoveSticks produced for each Move.  Configure this to match what your game object
        /// expects (or let input map do it when you call BindMove).
        /// </summary>
        public int NumSticks
        {
            get { return _sticks.Length; }
            set
            {
                if (LockMoveCounts)
                    return;
                int old = _sticks == null ? 0 : _sticks.Length;
                TorqueUtil.ResizeArray(ref _sticks, value);
                TorqueUtil.ResizeArray(ref _stickControlsUp, value);
                TorqueUtil.ResizeArray(ref _stickControlsDown, value);
                TorqueUtil.ResizeArray(ref _stickControlsLeft, value);
                TorqueUtil.ResizeArray(ref _stickControlsRight, value);
                for (int i = old; i < _sticks.Length; i++)
                {
                    _stickControlsUp[i].Init(this);
                    _stickControlsDown[i].Init(this);
                    _stickControlsLeft[i].Init(this);
                    _stickControlsRight[i].Init(this);
                }
            }
        }

        /// <summary>
        /// Default ramp up curve for all digital input.  A ramp up curve is defined
        /// by an array of floats which trace out a function which increases from 0 to
        /// 1 monotonically.  Points in the array are assumed to be equally spaced 
        /// over the ramp up time.  Set DefaultRampUpTime to vary the interval over which
        /// the function ramps up.
        /// 
        /// Examples:
        ///   {0, 1} will produce a linear ramp (if no curve supplied this will be used).
        ///   {0, 0.25, 1} will produce a function which ramps slowly at first and faster later on.
        ///   {0, 0.75, 1} will produce a function which ramps quickly at first but slowly after that. 
        /// </summary>
        public float[] DefaultRampUpFunction
        {
            get
            {
                return _defaultRampupFunction;
            }
            set
            {
                _defaultRampupFunction = value;
            }
        }

        /// <summary>
        /// Default ramp down curve for all digital input.  A ramp down curve is defined
        /// by an array of floats which trace out a function which decreases from 1 to
        /// 0 monotonically.  Points in the array are assumed to be equally spaced 
        /// over the ramp down time.  Set DefaultRampDownTime to vary the interval over which
        /// the function ramps down.
        /// 
        /// Examples:
        ///   {1, 0} will produce a linear ramp (if no curve supplied this will be used).
        ///   {1, 0.75, 0} will produce a function which ramps slowly at first and faster later on.
        ///   {1, 0.25, 0} will produce a function which ramps quickly at first but slowly after that. 
        /// </summary>
        public float[] DefaultRampDownFunction
        {
            get
            {
                return _defaultRampdownFunction;
            }
            set
            {
                _defaultRampdownFunction = value;
            }
        }

        /// <summary>
        /// Default scale function for all analog input.  A scale function is defined by an
        /// array of floats which trace out a function which increases from 0 to 1 monotonically.
        /// Points in the array are assumed to be equally spaced over the input range.
        /// 
        /// Examples:
        ///    {0, 1} will produce a linear scale (if no curve supplied this will be assumed).
        ///    {0, 0.25, 1.0} will square the input value (increasing slowly at first and faster later on).
        ///    {0, 0.75, 1.0} will scale the input so that it increases fast at first but slowly after that.
        /// </summary>
        public float[] DefaultScaleFunction
        {
            get
            {
                return _defaultScaleFunction;
            }
            set
            {
                _defaultScaleFunction = value;
            }
        }

        /// <summary>
        /// Default time it takes digital input to ramp up non-button move components.
        /// </summary>
        public float DefaultRampUpTime
        {
            get { return _defaultRampUpTime; }
            set { _defaultRampUpTime = value; }
        }

        /// <summary>
        /// Default time it takes digital input to ramp down non-button move components.
        /// </summary>
        public float DefaultRampDownTime
        {
            get { return _defaultRampDownTime; }
            set { _defaultRampDownTime = value; }
        }

        public GenerateMoveDelegate GenerateMove { get { return _generateMoveDelegate; } set { _generateMoveDelegate = value; } }

        #endregion


        #region Public methods

        /// <summary>
        /// Produce a new move.  This method is not typically called directly by the user.
        /// </summary>
        /// <param name="dt">Time since last move.</param>
        /// <returns>New move.</returns>
        public Move Update(float dt)
        {
            for (int i = 0; i < _buttonRepeat.Length; i++)
            {
                // track previous button state for when button repeat is false
                bool prev = _previousButtons[i].Pushed;
                _previousButtons[i]._pushed = _buttons[i]._pushed;
                if (!_buttonRepeat[i] && prev)
                    // button repeat is false and was down last time
                    _buttons[i]._pushed = false;
            }
            for (int i = 0; i < _triggers.Length; i++)
                _triggers[i]._value = _triggerControls[i].Update(_triggers[i]._value, dt);
            for (int i = 0; i < _levers.Length; i++)
            {
                float val = _leverControlsPositive[i].Update(_levers[i]._value, dt);
                val -= _leverControlsNegative[i].Update(-_levers[i]._value, dt);
                _levers[i]._value = val;
            }
            for (int i = 0; i < _sticks.Length; i++)
            {
                float val = _stickControlsRight[i].Update(_sticks[i]._x, dt);
                val -= _stickControlsLeft[i].Update(-_sticks[i]._x, dt);
                _sticks[i]._x = val;
                val = _stickControlsUp[i].Update(_sticks[i]._y, dt);
                val -= _stickControlsDown[i].Update(-_sticks[i]._y, dt);
                _sticks[i]._y = val;
            }

            Move move = null;
            if (_generateMoveDelegate != null)
                move = _generateMoveDelegate();

            move = _GenerateMove(move);

            return move;
        }

        #region Button interface

        /// <summary>
        /// Set indexed button to pushed or not.
        /// </summary>
        /// <param name="idx">Button index.</param>
        /// <param name="value">True if button pushed, false if not.</param>
        public void SetButton(int idx, bool value)
        {
            Assert.Fatal(idx >= 0 && idx < _buttons.Length, "Index out of range");
            _buttons[idx]._pushed = value;
        }

        /// <summary>
        /// Configure indexed button to either continuously signal when pushed down
        /// or to only signal on make event.  Default is true.
        /// </summary>
        /// <param name="idx">Button index.</param>
        /// <param name="value">True for repeating button, false for signal on make event only.</param>
        public void ConfigureButtonRepeat(int idx, bool value)
        {
            Assert.Fatal(idx >= 0 && idx < _buttonRepeat.Length, "Index out of range");
            _buttonRepeat[idx] = value;
        }

        #endregion

        #region Trigger interface

        /// <summary>
        /// Set indexed trigger value directly (analog input).
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        /// <param name="value">Input value.</param>
        public void SetTriggerDirect(int idx, float value)
        {
            Assert.Fatal(idx >= 0 && idx < _triggers.Length, "Index out of range");
            _triggerControls[idx].SetDirectly(value);
        }

        /// <summary>
        /// Set indexed trigger target position (digital input).
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        /// <param name="pushed">True to set to fully pushed position, false to set to fully released position.</param>
        public void SetTriggerTarget(int idx, bool pushed)
        {
            Assert.Fatal(idx >= 0 && idx < _triggers.Length, "Index out of range");
            _triggerControls[idx].SetTarget(pushed ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Set ramp up and ramp down function for indexed trigger.  See default ramp functions for more details.
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        /// <param name="rampUpTime">Time in milliseconds it takes to fully set the trigger with digital input.</param>
        /// <param name="rampUpValue">Ramp up function.</param>
        /// <param name="rampDownTime">Time in milliseconds it takes to fully clear the trigger with digital input.</param>
        /// <param name="rampDownValue">Ramp down function.</param>
        public void ConfigureTriggerTracking(int idx, float rampUpTime, float[] rampUpValue, float rampDownTime, float[] rampDownValue)
        {
            Assert.Fatal(idx >= 0 && idx < _triggers.Length, "Index out of range");
            _triggerControls[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
        }

        /// <summary>
        /// Set scaling function for indexed trigger.  See default scale function for more details.
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        /// <param name="scaleValues">Scale function.</param>
        public void ConfigureTriggerScaling(int idx, float[] scaleValues)
        {
            Assert.Fatal(idx >= 0 && idx < _triggers.Length, "Index out of range");
            _triggerControls[idx].ConfigureScaling(scaleValues);
        }

        /// <summary>
        /// Set ramp and scale functions to default values for indexed trigger.
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        public void ResetTriggerConfiguration(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _triggers.Length, "Index out of range");
            _triggerControls[idx].Init(this);
        }

        #endregion

        #region Lever interface

        /// <summary>
        /// Set indexed lever value directly (analog input).
        /// </summary>
        /// <param name="idx">Lever index.</param>
        /// <param name="value">Input value.</param>
        public void SetLeverDirect(int idx, float value)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsPositive[idx].SetDirectly(value > 0.0f ? value : 0.0f);
            _leverControlsNegative[idx].SetDirectly(value < 0.0f ? -value : 0.0f);
        }

        /// <summary>
        /// Set indexed lever target position to positive (digital input).
        /// </summary>
        /// <param name="idx">Lever index.</param>
        /// <param name="pushed">True to set to fully positive position.</param>
        public void SetLeverPositive(int idx, bool plus)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsPositive[idx].SetTarget(plus ? 1.0f : 0.0f);

        }

        /// <summary>
        /// Set indexed lever target position to negative (digital input).
        /// </summary>
        /// <param name="idx">Lever index.</param>
        /// <param name="pushed">True to set to fully negative position.</param>
        public void SetLeverNegative(int idx, bool negative)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsNegative[idx].SetTarget(negative ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Set ramp up and ramp down function for indexed lever.  See default ramp functions for more details.
        /// </summary>
        /// <param name="idx">Lever index.</param>
        /// <param name="rampUpTime">Time in milliseconds it takes to fully set the lever with digital input.</param>
        /// <param name="rampUpValue">Ramp up function.</param>
        /// <param name="rampDownTime">Time in milliseconds it takes to fully clear the lever with digital input.</param>
        /// <param name="rampDownValue">Ramp down function.</param>
        public void ConfigureLeverTracking(int idx, float rampUpTime, float[] rampUpValue, float rampDownTime, float[] rampDownValue)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsPositive[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
            _leverControlsNegative[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
        }

        /// <summary>
        /// Set scaling function for indexed lever.  See default scale function for more details.
        /// </summary>
        /// <param name="idx">Lever index.</param>
        /// <param name="scaleValues">Scale function.</param>
        public void ConfigureLeverScaling(int idx, float[] scaleValues)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsPositive[idx].ConfigureScaling(scaleValues);
            _leverControlsNegative[idx].ConfigureScaling(scaleValues);
        }

        /// <summary>
        /// Set ramp and scale functions to default values for indexed lever.
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        public void ResetLeverConfiguration(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _levers.Length, "Index out of range");
            _leverControlsNegative[idx].Init(this);
            _leverControlsPositive[idx].Init(this);
        }

        #endregion

        #region Stick interface

        /// <summary>
        /// Set indexed stick horizontal value directly (analog input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="value">Input value.</param>
        public void SetStickHorizontalDirect(int idx, float value)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsRight[idx].SetDirectly(value > 0.0f ? value : 0.0f);
            _stickControlsLeft[idx].SetDirectly(value < 0.0f ? -value : 0.0f);
        }

        /// <summary>
        /// Set indexed stick vertical value directly (analog input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="value">Input value.</param>
        public void SetStickVerticalDirect(int idx, float value)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsUp[idx].SetDirectly(value > 0.0f ? value : 0.0f);
            _stickControlsDown[idx].SetDirectly(value < 0.0f ? -value : 0.0f);
        }

        /// <summary>
        /// Set indexed stick target position to up (digital input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="pushed">True to set to fully up position.</param>
        public void SetStickUp(int idx, bool plus)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsUp[idx].SetTarget(plus ? 1.0f : 0.0f);

        }

        /// <summary>
        /// Set indexed stick target position to down (digital input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="pushed">True to set to fully down position.</param>
        public void SetStickDown(int idx, bool plus)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsDown[idx].SetTarget(plus ? 1.0f : 0.0f);

        }

        /// <summary>
        /// Set indexed stick target position to left (digital input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="pushed">True to set to fully left position.</param>
        public void SetStickLeft(int idx, bool plus)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsLeft[idx].SetTarget(plus ? 1.0f : 0.0f);

        }

        /// <summary>
        /// Set indexed stick target position to right (digital input).
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="pushed">True to set to fully right position.</param>
        public void SetStickRight(int idx, bool plus)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsRight[idx].SetTarget(plus ? 1.0f : 0.0f);

        }

        /// <summary>
        /// Set ramp up and ramp down function for indexed stick horizontal axis.  See default ramp functions for more details.
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="rampUpTime">Time in milliseconds it takes to fully set the stick with digital input.</param>
        /// <param name="rampUpValue">Ramp up function.</param>
        /// <param name="rampDownTime">Time in milliseconds it takes to fully clear the stick with digital input.</param>
        /// <param name="rampDownValue">Ramp down function.</param>
        public void ConfigureStickHorizontalTracking(int idx, float rampUpTime, float[] rampUpValue, float rampDownTime, float[] rampDownValue)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsLeft[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
            _stickControlsRight[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
        }

        /// <summary>
        /// Set ramp up and ramp down function for indexed stick vertical axis.  See default ramp functions for more details.
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="rampUpTime">Time in milliseconds it takes to fully set the stick with digital input.</param>
        /// <param name="rampUpValue">Ramp up function.</param>
        /// <param name="rampDownTime">Time in milliseconds it takes to fully clear the stick with digital input.</param>
        /// <param name="rampDownValue">Ramp down function.</param>
        public void ConfigureStickVerticalTracking(int idx, float rampUpTime, float[] rampUpValue, float rampDownTime, float[] rampDownValue)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsUp[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
            _stickControlsDown[idx].ConfigureTracking(rampUpTime, rampUpValue, rampDownTime, rampDownValue);
        }

        /// <summary>
        /// Set scaling function for indexed stick horizontal axis.  See default scale function for more details.
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="scaleValues">Scale function.</param>
        public void ConfigureStickHorizontalScaling(int idx, float[] scaleValues)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsLeft[idx].ConfigureScaling(scaleValues);
            _stickControlsRight[idx].ConfigureScaling(scaleValues);
        }

        /// <summary>
        /// Set scaling function for indexed stick vertical axis.  See default scale function for more details.
        /// </summary>
        /// <param name="idx">Stick index.</param>
        /// <param name="scaleValues">Scale function.</param>
        public void ConfigureStickVerticalScaling(int idx, float[] scaleValues)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsUp[idx].ConfigureScaling(scaleValues);
            _stickControlsDown[idx].ConfigureScaling(scaleValues);
        }

        /// <summary>
        /// Set ramp and scale functions to default values for indexed stick.
        /// </summary>
        /// <param name="idx">Trigger index.</param>
        public void ResetStickConfiguration(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _sticks.Length, "Index out of range");
            _stickControlsUp[idx].Init(this);
            _stickControlsDown[idx].Init(this);
            _stickControlsLeft[idx].Init(this);
            _stickControlsRight[idx].Init(this);
        }

        #endregion

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Generates move for the move manager.  In order to generate a new move type one would
        /// derive a new class from Move and a new move manager type from MoveManager.  Then one
        /// would override this method to generate moves of the desired type.  Call base._GenerateMove
        /// with a move of the desired type to have base class fill in the buttons, triggers,
        /// levers, and sticks.  If simply using stock moves, there is no need to override this
        /// method.
        /// </summary>
        /// <param name="move">The move to fill in if already generated (by derived class) or null if creating move is our responsibility.</param>
        /// <returns>New move.</returns>
        virtual internal protected Move _GenerateMove(Move move)
        {
            // we may or may not supply our own move
            if (move == null)
                move = _move;

            // but we will always set the move data we know about ourself
            move._buttons = _buttons;
            move._triggers = _triggers;
            move._levers = _levers;
            move._sticks = _sticks;
            return move;
        }

        /// <summary>
        /// The TorqueObject who consumes moves from this move manager.  Moves are fed to
        /// objects in the process list via the ProcessTick callback.  This is internal
        /// because it is managed by ProcessList not by user.
        /// </summary>
        internal TorqueObject _Consumer
        {
            get { return _consumer; }
            set { _consumer = value; }
        }

        #endregion


        #region Private, protected, internal fields

        internal MoveButton[] _buttons;
        internal MoveButton[] _previousButtons;
        internal MoveTrigger[] _triggers;
        internal MoveLever[] _levers;
        internal MoveStick[] _sticks;

        // Don't allow changes to move counts (button counts, trigger counts, lever counts, stick counts)
        bool _lockMoveCounts;

        // the object which receives the moves
        TorqueObject _consumer;

        // button configuration -- on make only or continuously down
        bool[] _buttonRepeat;

        // control widgets for trigger
        ControlWidget[] _triggerControls;

        // control widgets for levers (lever value is sum of widgets)
        ControlWidget[] _leverControlsPositive;
        ControlWidget[] _leverControlsNegative;

        // control widgets for sticks (value on each axis is sum for that axis)
        ControlWidget[] _stickControlsUp;
        ControlWidget[] _stickControlsDown;
        ControlWidget[] _stickControlsLeft;
        ControlWidget[] _stickControlsRight;

        Move _move = new Move();
        GenerateMoveDelegate _generateMoveDelegate;

        internal const int MAX_FIT_POINTS = 8;
        float _defaultRampUpTime = 0.15f;
        float _defaultRampDownTime = 0.1f;
        float[] _defaultRampupFunction = new float[] { 0.0f, 0.5f, 0.75f, 1.0f };
        float[] _defaultRampdownFunction = new float[] { 1.0f, 0.0f };
        float[] _defaultScaleFunction = null; // identity, i.e. no scale

        #endregion
    }
}
