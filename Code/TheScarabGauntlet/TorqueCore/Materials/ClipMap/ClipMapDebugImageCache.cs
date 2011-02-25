//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.Materials.ClipMap
{
    /// <summary>
    /// An image cache that procedurally generates texture data on the fly to illustrate
    /// when and how the regional updates are happening aswell as where the clip level
    /// boundaries are. This will draw a gradient checkerboard pattern with alternating
    /// colors for each individual draw call done when updating the clip stack textures.
    /// </summary>
    public class ClipMapDebugImageCache : IClipMapImageCache
    {

        #region Public methods

        public void Initialize(int clipMapSize, int clipMapDepth) { }



        public void BeginRectUpdates(int mipLevel, ClipStackEntry stackEntry) { }



        public void DoRectUpdate(int mipLevel, ClipStackEntry stackEntry, RectangleI srcRegion, RectangleI dstRegion)
        {
            // toggle updateToggle
            _updateToggle = (_updateToggle + 1) % 3;

            // find the index to start at
            int index = (stackEntry.Texture.Width * dstRegion.Point.Y) + dstRegion.Point.X;

            // get an array of texture data
            int elementCount = dstRegion.Width * dstRegion.Height;
            uint[] dstData = new uint[elementCount];

            // make local copies of the rectangle components for quick access
            int srcPointX = srcRegion.Point.X;
            int srcPointY = srcRegion.Point.Y;
            int srcExtentX = srcRegion.Extent.X;
            int srcExtentY = srcRegion.Extent.Y;
            float scaleFactor = stackEntry.ScaleFactor;

            for (int y = 0; y < srcExtentY; y++)
            {
                for (int x = 0; x < srcExtentX; x++)
                {
                    uint color = 0xFF000000;

                    int realX = x + srcPointX;
                    int realY = y + srcPointY;

                    int xFlag = realX & 4;
                    int yFlag = realY & 4;

                    // checker pattern
                    if ((xFlag ^ yFlag) != 0)
                        color += 0xFF;

                    // gradient based on x position across master texture
                    color += (uint)((float)realX / (float)(512f * scaleFactor) * 255) << 8;

                    // toggle colors
                    switch (_updateToggle)
                    {
                        case 0:
                            color += 0xFF0000;
                            break;
                        case 1:
                            color += 0xA00000;
                            break;
                    }

                    //(uint)(_updateToggle ? 0xFF0000 : 0x00);
                    //color += 0xFF;

                    dstData[(srcExtentX * y) + x] = color;
                }
            }

            // send the new data to the stack entry texture
            Rectangle dstTextureRect = new Rectangle(dstRegion.X, dstRegion.Y, dstRegion.Width, dstRegion.Height);
            stackEntry.Texture.SetData<uint>(0, dstTextureRect, dstData, 0, elementCount, SetDataOptions.None);
        }



        public void FinishRectUpdates(int mipLevel, ClipStackEntry stackEntry) { }



        public IClipMapImageCache GetCopyOfInstance()
        {
            return this;
        }

        #endregion


        #region Private, protected, internal fields

        int _updateToggle;

        #endregion
    }
}
