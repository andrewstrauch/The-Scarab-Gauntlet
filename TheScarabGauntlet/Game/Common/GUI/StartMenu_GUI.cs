using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GUI;
using GarageGames.Torque.MathUtil;
using System.Collections;

namespace PlatformerStarter
{
    public class StartMenu_GUI : GUIBitmap, IGUIScreen
    {
        #region Private Variables

        private ArrayList _buttons = new ArrayList();
        private int _currentSelection = 0;

        #endregion

        #region Intialisation

        public StartMenu_GUI()
        {
            //create the style for the main menu background
            GUIBitmapStyle bitmapStyle = new GUIBitmapStyle();
            Style = bitmapStyle;
            Bitmap = @"data\images\startMenu"; // Background image of menu

            //create the style for the menu buttons
            GUIButtonStyle buttonStyle = new GUIButtonStyle();
            buttonStyle.FontType = "Arial22"; //@"data\images\MyFont";
            buttonStyle.TextColor[CustomColor.ColorBase] = new Color(100, 100, 100, 255); //normal menu text color
            buttonStyle.TextColor[CustomColor.ColorHL] = Color.Red; //highlighter color
            buttonStyle.TextColor[CustomColor.ColorNA] = Color.Silver; //disabled color
            buttonStyle.TextColor[CustomColor.ColorSEL] = Color.DarkRed; //select color
            buttonStyle.Alignment = TextAlignment.JustifyLeft;
            buttonStyle.Focusable = true;

            GUITextStyle textStyle = new GUITextStyle();
            textStyle.FontType = "Arial22";
            textStyle.TextColor[CustomColor.ColorBase] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            textStyle.Alignment = TextAlignment.JustifyCenter;
            

            // Based on 1024 * 768
            float positionX = 350;
            float positionY = 300;

            #region Buttons
            GUIText text = new GUIText();
            text.Style = textStyle;
            text.Size = new Vector2(800, 100);
            text.Position = new Vector2(400, 150);
            text.Visible = true;
            text.Folder = this;
            text.Text = "The Scarab Gauntlet";

            GUIButton playButton = new GUIButton();
            playButton.Style = buttonStyle;
            playButton.Size = new Vector2(500, 100);
            playButton.Position = new Vector2(positionX - (playButton.Size.X / 2), positionY);
            playButton.Visible = true;
            playButton.Folder = this;
            playButton.ButtonText = "Start Game";
            playButton.OnSelectedDelegate = On_Play;
            playButton.OnGainFocus(null);
            _buttons.Add(playButton);

            GUIButton optionsButton = new GUIButton();
            optionsButton.Style = buttonStyle;
            optionsButton.Size = new Vector2(500, 100);
            optionsButton.Position = new Vector2(positionX - (playButton.Size.X / 2), positionY + 50);
            optionsButton.Visible = true;
            optionsButton.Folder = this;
            optionsButton.ButtonText = "Options";
            optionsButton.OnSelectedDelegate = On_Options;
            _buttons.Add(optionsButton);

            GUIButton exitButton = new GUIButton();
            exitButton.Style = buttonStyle;
            exitButton.Size = new Vector2(500, 100);
            exitButton.Position = new Vector2(positionX - (playButton.Size.X / 2), positionY + 100);
            exitButton.Visible = true;
            exitButton.Folder = this;
            exitButton.ButtonText = "Exit";
            exitButton.OnSelectedDelegate = On_Exit;
            _buttons.Add(exitButton);

            #endregion

            //setup the input map
            SetupInputMap();
        }

        private void SetupInputMap()
        {
            // Hardcoded to playerIndex = 0
            //#######################################################
            // 
            InputMap.BindCommand(0, (int)XGamePadDevice.GamePadObjects.A, null, Select);
            InputMap.BindCommand(0, (int)XGamePadDevice.GamePadObjects.Up, null, MoveUp);
            InputMap.BindCommand(0, (int)XGamePadDevice.GamePadObjects.Down, null, MoveDown);
            InputMap.BindCommand(0, (int)XGamePadDevice.GamePadObjects.Back, null, On_Exit);

            int keyboardId = InputManager.Instance.FindDevice("keyboard");
            InputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Down, null, MoveDown);
            InputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Up, null, MoveUp);
            InputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Enter, null, Select);
            InputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Escape, null, On_Exit);
        }

        #endregion

        #region Events

        #region Input Events

        private void MoveDown()
        {
            if (_currentSelection + 1 < _buttons.Count)
            {
                //clear all other options
                for (int index = 0; index < _buttons.Count; index++)
                    ((GUIButton)_buttons[index]).OnLoseFocus(null);

                _currentSelection++;
                ((GUIButton)_buttons[_currentSelection]).OnGainFocus(null);
            }
        }

        private void MoveUp()
        {
            if (_currentSelection - 1 >= 0)
            {
                //clear all other options
                for (int index = 0; index < _buttons.Count; index++)
                    ((GUIButton)_buttons[index]).OnLoseFocus(null);

                _currentSelection--;
                ((GUIButton)_buttons[_currentSelection]).OnGainFocus(null);
            }
        }

        private void Select()
        {
            ((GUIButton)_buttons[_currentSelection]).OnSelectedDelegate();
        }

#endregion

        #region Screen Events

        private void On_Play()
        {
            HealthBar_GUI playGUI = new HealthBar_GUI();
            GUICanvas.Instance.SetContentControl(playGUI);

            // Load game
            Game.Instance.SceneLoader.Load(@"data\levels\Level1.txscene");
        }



        private void On_Options()
        {
            //show the help screen
            //GuiHelpScreen helpScreen = new GuiHelpScreen();
            //GUICanvas.Instance.SetContentControl(helpScreen);
        }


        private void On_Exit()
        {
            //shutdown the game
            GarageGames.Torque.XNA.TorqueEngineComponent.Instance.Exit();
        }

        #endregion

        #endregion
    }
}
