//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// The base TextureDivider class that defines how a RenderMaterial is subdivided. Allows user to 
    /// specify regions and then later access those regions' texture coordinates by index.
    /// </summary>
    public abstract class TextureDivider
    {

        #region Public Methods

        /// <summary>
        /// This is called by the associated RenderMaterial once it's been loaded.
        /// </summary>
        public virtual void Init(RenderMaterial material)
        {
            // keep a reference to the render material for later use
            _material = material;
        }



        /// <summary>
        /// This is called by the associated RenderMaterial when it's unloaded.
        /// </summary>
        public virtual void Destroy()
        {
            _material = null;
        }



        /// <summary>
        /// Returns the total number of regions on this TextureDivider.
        /// </summary>
        /// <returns></returns>
        public abstract int GetRegionCount();



        /// <summary>
        /// Get the texture coordinates associated with the specified region index.
        /// </summary>
        /// <param name="index">The region index for which to retrieve texture coordinates.</param>
        /// <param name="t0">The top left corner of the region specified.</param>
        /// <param name="t1">The top right corner of the region specified.</param>
        /// <param name="t2">The bottom right corner of the region specified.</param>
        /// <param name="t3">The bottom left corner of the region specified.</param>
        public abstract void GetRegionCoords(int index, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3);



        /// <summary>
        /// Get the texture coordinates associated with the specified region index.
        /// </summary>
        /// <param name="index">The region index for which to retrieve texture coordinates.</param>
        /// <returns>A rectangle representing the texture coordinates of the specified region.</returns>
        public abstract RectangleF GetRegionCoords(int index);

        #endregion


        #region Private, protected, internal fields

        protected RenderMaterial _material;

        #endregion
    }

    /// <summary>
    /// Divides a texture based on user defined texture regions. These are RectangleF objects and can
    /// be set in XML with the RegionList tag.
    /// </summary>
    public class GenericTextureDivider : TextureDivider
    {

        #region Public Methods

        public override void Init(RenderMaterial material)
        {
            base.Init(material);

            // make sure our list isn't null
            if (_regionList == null)
                _regionList = new List<RectangleF>();
        }



        public override int GetRegionCount()
        {
            // either return the region list count, or 1 if it's empty
            // (return 1 because GetRegionCoords will still return the full texture space if 
            // no regions are defined. in other words, this divider will always provide at least
            // one region)
            int count = _regionList.Count;
            return count > 0 ? count : 1;
        }



        public override void GetRegionCoords(int index, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _regionList.Count && index >= 0)
            {
                RectangleF rect = _regionList[index];

                t0 = new Vector2(rect.Point.X, rect.Point.Y);
                t1 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y);
                t2 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y + rect.Extent.Y);
                t3 = new Vector2(rect.Point.X, rect.Point.Y + rect.Extent.Y);
            }
            else
            {
                t0 = new Vector2(0.0f, 0.0f);
                t1 = new Vector2(1.0f, 0.0f);
                t2 = new Vector2(1.0f, 1.0f);
                t3 = new Vector2(0.0f, 1.0f);
            }
        }



        public override RectangleF GetRegionCoords(int index)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _regionList.Count && index >= 0)
                return _regionList[index];

            return new RectangleF(0.0f, 0.0f, 1.0f, 1.0f);
        }

        #endregion


        #region Private, protected, internal fields

        [XmlElement(ElementName = "RegionList")]
        [TorqueXmlDeserializeInclude]
        private List<RectangleF> _regionList;

        #endregion
    }



    /// <summary>
    /// Divide a texture based on a number of cells.
    /// </summary>
    public class CellCountDivider : TextureDivider
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The number of vertical columns by which to subdivide the texture space.
        /// </summary>
        public int CellCountX
        {
            get { return _cellCountX; }
            set
            {
                if (value != _cellCountX)
                {
                    _cellCountX = value >= 1 ? value : 1;
                    _CalculateCells();
                }
            }
        }



        /// <summary>
        /// The number of horizontal rows by which to subdivide the texture space.
        /// </summary>
        public int CellCountY
        {
            get { return _cellCountY; }
            set
            {
                if (value != _cellCountY)
                {
                    _cellCountY = value >= 1 ? value : 1;
                    _CalculateCells();
                }
            }
        }

        #endregion


        #region Public Methods

        public override int GetRegionCount()
        {
            // either return the cells list count, or 1 if it's empty
            // (return 1 because GetRegionCoords will still return the full texture space if 
            // no regions are defined. in other words, this divider will always provide at least
            // one region)
            return _cellsList.Count > 0 ? _cellsList.Count : 1;
        }



        public override void GetRegionCoords(int index, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _cellsList.Count && index >= 0)
            {
                RectangleF rect = _cellsList[index];

                t0 = new Vector2(rect.Point.X, rect.Point.Y);
                t1 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y);
                t2 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y + rect.Extent.Y);
                t3 = new Vector2(rect.Point.X, rect.Point.Y + rect.Extent.Y);
            }
            else
            {
                t0 = new Vector2(0.0f, 0.0f);
                t1 = new Vector2(1.0f, 0.0f);
                t2 = new Vector2(1.0f, 1.0f);
                t3 = new Vector2(0.0f, 1.0f);
            }
        }



        public override RectangleF GetRegionCoords(int index)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _cellsList.Count && index >= 0)
                return _cellsList[index];

            return new RectangleF(0.0f, 0.0f, 1.0f, 1.0f);
        }

        #endregion


        #region Private, protected, internal methods

        private void _CalculateCells()
        {
            // make sure we have valid cell counts to work with
            if (_cellCountX <= 0 || _cellCountY <= 0)
                return;

            // create the new cells list
            _cellsList = new List<RectangleF>();

            // get the size per cell in local coordinates
            float sizePerCellX = 1f / _cellCountX;
            float sizePerCellY = 1f / _cellCountY;

            float offsetX = 0;
            float offsetY = 0;
            RectangleF cell;

            // calculate the different regions and populate the cells list
            for (int y = 0; y < _cellCountY; y++)
            {
                for (int x = 0; x < _cellCountX; x++)
                {
                    offsetX = x * sizePerCellX;
                    offsetY = y * sizePerCellY;
                    cell = new RectangleF(offsetX, offsetY, sizePerCellX, sizePerCellY);
                    _cellsList.Add(cell);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        private int _cellCountX = 1;
        private int _cellCountY = 1;

        protected List<RectangleF> _cellsList = new List<RectangleF>();

        #endregion
    }



    /// <summary>
    /// Divide a texture based on a cell width and height.
    /// </summary>
    public class CellSizeDivider : TextureDivider
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The pixel width of each cell.
        /// </summary>
        public int CellWidth
        {
            get { return _cellWidth; }
            set
            {
                _cellWidth = value >= 1 ? value : 1;
                _CalculateCells();
            }
        }



        /// <summary>
        /// The pixel height of each cell.
        /// </summary>
        public int CellHeight
        {
            get { return _cellHeight; }
            set
            {
                _cellHeight = value >= 1 ? value : 1;
                _CalculateCells();
            }
        }

        #endregion


        #region Public Methods

        public override void Init(RenderMaterial material)
        {
            base.Init(material);

            // call calculate cells now 
            // (there is a chance that it wasn't able to load earlier due to the texture
            // dimensions not being available yet)
            _CalculateCells();
        }



        public override int GetRegionCount()
        {
            // either return the cells list count, or 1 if it's empty
            // (return 1 because GetRegionCoords will still return the full texture space if 
            // no regions are defined. in other words, this divider will always provide at least
            // one region)
            return _cellsList.Count > 0 ? _cellsList.Count : 1;
        }



        public override void GetRegionCoords(int index, out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _cellsList.Count && index >= 0)
            {
                RectangleF rect = _cellsList[index];

                t0 = new Vector2(rect.Point.X, rect.Point.Y);
                t1 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y);
                t2 = new Vector2(rect.Point.X + rect.Extent.X, rect.Point.Y + rect.Extent.Y);
                t3 = new Vector2(rect.Point.X, rect.Point.Y + rect.Extent.Y);
            }
            else
            {
                t0 = new Vector2(0.0f, 0.0f);
                t1 = new Vector2(1.0f, 0.0f);
                t2 = new Vector2(1.0f, 1.0f);
                t3 = new Vector2(0.0f, 1.0f);
            }
        }



        public override RectangleF GetRegionCoords(int index)
        {
            // return the region specified
            // if the specified region doesn't exist, return the full texture space
            if (index < _cellsList.Count && index >= 0)
                return _cellsList[index];

            return new RectangleF(0.0f, 0.0f, 1.0f, 1.0f);
        }

        #endregion


        #region Private, protected, internal methods

        private void _CalculateCells()
        {
            // make sure we have two valid dimensions to work with
            if (_material as ITextureMaterial == null || _cellWidth <= 0 || _cellHeight <= 0)
                return;

            // grab the texture so we can query its dimensions
            ITextureMaterial tm = _material as ITextureMaterial;
            Resource<Texture> res = ResourceManager.Instance.LoadTexture((_material as ITextureMaterial).TextureFilename);
            Texture2D tex2d = res.Instance as Texture2D;

            // figure out the number of cells to process
            int cellCountX = tex2d.Width / _cellWidth;
            int cellCountY = tex2d.Height / _cellHeight;

            // avoid 'divide by zero' problems ahead
            if (cellCountX <= 0 || cellCountY <= 0)
                return;

            // as far as we know, we've got valid numbers
            // clear the list to repopulate
            _cellsList = new List<RectangleF>();

            // get the size per cell
            float sizePerCellX = _cellWidth / (float)tex2d.Width;
            float sizePerCellY = _cellHeight / (float)tex2d.Height;

            // declare stuff for the loop
            RectangleF cell;
            float offsetX, offsetY;

            // calculate regions and populate the cells list
            for (int y = 0; y < cellCountY; y++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    offsetX = x * sizePerCellX;
                    offsetY = y * sizePerCellY;
                    cell = new RectangleF(offsetX, offsetY, sizePerCellX, sizePerCellY);
                    _cellsList.Add(cell);
                }
            }

            // invalidate the texture resource
            res.Invalidate();
        }

        #endregion


        #region Private, protected, internal fields

        private int _cellWidth;
        private int _cellHeight;

        protected List<RectangleF> _cellsList = new List<RectangleF>();

        #endregion
    }
}