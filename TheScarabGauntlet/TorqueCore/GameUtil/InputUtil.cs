//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Platform;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.GameUtil
{
    /// <summary>
    /// A utility to store commonly useful input bindings.
    /// </summary>
    public class InputUtil
    {
        #region Static methods

        public static void BindStartBackQuickExit()
        {
            // bind the start+back exit hotkey in the default gui
            int gamepadId = InputManager.Instance.FindDevice("gamepad");
            InputMap.Global.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, InputUtil._OnStartButton);
            InputMap.Global.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Back, InputUtil._OnBackButton);
        }



        // global exit hotkey functions
        static void _CheckExit()
        {
            if (_backPressed && _startPressed)
            {
                TorqueEngineComponent.Instance.Exit();
            }
        }



        static void _OnStartButton(float val)
        {
            _startPressed = val > 0.0f;
            _CheckExit();
        }



        static void _OnBackButton(float val)
        {
            _backPressed = val > 0.0f;
            _CheckExit();
        }



        static bool _startPressed;
        static bool _backPressed;

        #endregion
    }
}
