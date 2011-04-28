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
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Common.GUI
{
    public class Checkpoint_GUI : GUISceneview, IGUIScreen
    {
        #region Private Members
        private Timer displayTimer;
        private GUIText text;
        #endregion

        public Checkpoint_GUI(Vector2 position)
        {
            displayTimer = new Timer();
            displayTimer.MillisecondsUntilExpire = 2000;

            Initialize(position);
        }

        private void Initialize(Vector2 textPosition)
        {
            GUITextStyle textStyle = new GUITextStyle();
            textStyle.FontType = "Arial22";
            textStyle.TextColor[CustomColor.ColorBase] = Color.Bisque;
            textStyle.Alignment = TextAlignment.JustifyCenter;

            text = new GUIText();
            text.Style = textStyle;
            text.Text = "Checkpoint Reached.";
            text.Size = new Vector2(100, 200);
            text.Position = textPosition;
            text.Folder = this;
            text.Visible = true;

        }

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
            if (!displayTimer.Running && !displayTimer.Expired)
                displayTimer.Start();
            else if (displayTimer.Expired)
            {
                displayTimer.Reset();
                this.MarkForDelete = true;
            }

            base.OnRender(offset, updateRect);
        }
    }
}
