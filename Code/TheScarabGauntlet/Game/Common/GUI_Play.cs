using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.GUI;

namespace PlatformerStarter
{
    public class GUI_Play : GUISceneview, IGUIScreen
    {
        #region Private Variables

        private GUIControl healthBar;
        private GUIText crystalText;

        private int health;             // currentHealth
        private int crystals;           // current crystals
        private float maxHealthBarSize; // used to calculate size of health

        #endregion

        #region Public Properties

        public int DisplayedHealth
        {
            get { return health; }
            set
            {
                // bounds checking
                health = (int)MathHelper.Clamp(value, 0, 100);
            }
        }

        public int NumCollectedCrystals
        {
            get { return crystals; }
            set
            {
                if (value < 0)
                    value = 0;

                crystals = value;
            }
        }
        #endregion

        #region Initialisation
        public GUI_Play()
        {
            #region Styles

            // Create Base Style
            GUIStyle playStyle = new GUIStyle();
            Name = "GuiPlay";
            Style = playStyle;
            Size = new Microsoft.Xna.Framework.Vector2(1024, 768);
            Folder = GUICanvas.Instance;

            // Create Text Style
            GUITextStyle styleText = new GUITextStyle();
            styleText.FontType = "Arial20"; // Change to desired
            styleText.TextColor[CustomColor.ColorBase] = Color.White; // Change to desired
            styleText.SizeToText = true;
            styleText.Alignment = TextAlignment.JustifyLeft;
            styleText.PreserveAspectRatio = true;

            // Create Pic Style
            GUIBitmapStyle bitmapStyle = new GUIBitmapStyle();
            bitmapStyle.SizeToBitmap = false;

            // Modify the bitmap Style to draw the health
            GUIStyle healthStyle = new GUIStyle();
            healthStyle.FillColor[CustomColor.ColorBase] = Color.Red;
            healthStyle.IsOpaque = true;

            #endregion

            #region GUIObjects

            // Create health text object
            GUIText healthText = new GUIText();
            healthText.Style = styleText; // Created above
            healthText.Text = "Health"; // Change to desired
            healthText.Size = new Vector2(75, 30); // If smaller than text, will be cut off
            healthText.Position = new Vector2(20, 20); // Change to desired
            healthText.Folder = this; // needed to draw
            healthText.Visible = true; // needed to draw

            // Create cyrstal text object
            crystalText = new GUIText();
            crystalText.Style = styleText;
            crystalText.Text = ""; 
            crystalText.Size = new Vector2(75, 30); // If smaller than text, will be cut off
            crystalText.Position = new Vector2(920, 57); // Change to desired
            crystalText.Folder = this; 
            crystalText.Visible = true; 

            // Create "x" text object, to make independent of the no. of crystals
            GUIText crossText = new GUIText();
            crossText.Style = styleText; 
            crossText.Text = " x "; 
            crossText.Size = new Vector2(75, 30); // If smaller than text, will be cut off
            crossText.Position = new Vector2(crystalText.Position.X + 30, crystalText.Position.Y); // offset position from crystalText
            crossText.Folder = this; 
            crossText.Visible = true; 

            // Create picture object
            GUIBitmap healthBarBorder = new GUIBitmap();
            healthBarBorder.Style = bitmapStyle; 
            healthBarBorder.Size = new Vector2(300, 25); // The size of the health bar
            healthBarBorder.Bitmap = @"data\images\lifebar";
            healthBarBorder.Folder = this;
            healthBarBorder.Visible = true;
            healthBarBorder.Position = new Vector2(20, 60);

            // Create picture object
            GUIBitmap crystalBitmap = new GUIBitmap();
            crystalBitmap.Style = bitmapStyle; 
            crystalBitmap.Size = new Vector2(25, 60); // The size of the crystal
            crystalBitmap.Bitmap = @"data\images\crystal";
            crystalBitmap.Folder = this;
            crystalBitmap.Visible = true;
            crystalBitmap.Position = new Vector2(crystalText.Position.X + 55, crystalText.Position.Y - 15); // offset position from crystalText

            // Create base object to fill with colour
            healthBar = new GUIControl();
            healthBar.Style = healthStyle;
            //healthBar.Size = new Vector2(healthBarBorder.Size.X - 8, healthBarBorder.Size.Y - 5);
            healthBar.Size = new Vector2(292, 20); // offset size to fit inside health bar
            healthBar.Folder = this;
            healthBar.Visible = true;
            //healthBar.Position = new Vector2(healthBarBorder.Position.X + 5, healthBarBorder.Position.Y + 3);
            healthBar.Position = new Vector2(25, 63); // offset pos to fit inside health bar
            
            // used calculate health bar size (see draw)
            maxHealthBarSize = healthBar.Size.X;

            #endregion
        }
        #endregion

        #region Draw

        public override void OnRender(Microsoft.Xna.Framework.Vector2 offset, GarageGames.Torque.MathUtil.RectangleF updateRect)
        {
            // Calculate size of health
            Vector2 tempSize = healthBar.Size;
            tempSize.X = maxHealthBarSize * ((float)health / 100);
            healthBar.Size = tempSize;

            // If dead dont draw anything
            if (tempSize.X == 0)
                healthBar.Style.IsOpaque = false;
            else
                healthBar.Style.IsOpaque = true;

            // update crystal count
            crystalText.Text = crystals.ToString();

            base.OnRender(offset, updateRect);

        }

        #endregion
    }
}
