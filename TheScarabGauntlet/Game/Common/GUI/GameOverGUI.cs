using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GUI;
using GarageGames.Torque.MathUtil;

namespace PlatformerStarter.Common.GUI
{
    class GameOver_GUI : GUIControl, IGUIScreen
    {
        private Color fadeColour = new Color(0,0,0);

        #region Intialisation

        public GameOver_GUI()
        {
            //create the style for the main menu background
            GUIStyle gameOverStyle = new GUIStyle();
            gameOverStyle.IsOpaque = true;
            Style = gameOverStyle;

            //create the style for the menu buttons
            GUITextStyle textStyle = new GUITextStyle();
            textStyle.FontType = "Arial22";
            textStyle.TextColor[CustomColor.ColorBase] = Color.White;
            textStyle.Alignment = TextAlignment.JustifyCenter;

            // Based on 1024 * 768
            float positionX = 512;
            float positionY = 400;

            GUIText pauseText = new GUIText();
            pauseText.Style = textStyle;
            pauseText.Text = "Thanks for playing!!";
            pauseText.Size = new Vector2(300, 100);
            pauseText.Position = new Vector2(positionX - (pauseText.Size.X / 2), positionY);
            pauseText.Visible = true;
            pauseText.Folder = this;


        }

        #endregion

        protected override bool _OnWake()
        {
            // reset the fade colour
            fadeColour.A = 255;

            return base._OnWake();
        }

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
            if (fadeColour.A > 150)
            {
                fadeColour.A -= 1;
                Style.FillColor[CustomColor.ColorBase] = fadeColour;
            }

            base.OnRender(offset, updateRect);
        }
    }
}
