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
using GarageGames.Torque.SceneGraph;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Used by the T2DSceneGraph to store and execute the sort modes for layers.
    /// </summary>
    public class T2DLayerSortDictionary : Dictionary<int, IComparer<ISceneContainerObject>>
    {
        #region Static methods, fields, constructors

        public static readonly IComparer<ISceneContainerObject> LayerSort = new LayerComparer();



        /// <summary>
        /// Perform no actual sorting on layers.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> NoSort = new NoSortComparer();



        /// <summary>
        /// Sorts ISceneContainerObjects by layer order.  This normally resolves
        /// to the object id, which is essentially the add order.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> LayerOrderSort = new LayerOrderComparer();



        /// <summary>
        /// Sorts ISceneContainerObjects by the X axis sort point.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> XAxisSort = new XAxisComparer();



        /// <summary>
        /// Sorts ISceneContainerObjects by the Y axis sort point.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> YAxisSort = new YAxisComparer();



        /// <summary>
        /// Sorts ISceneContainerObjects by the X axis sort point in reverse order.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> NegativeXAxisSort = new NegativeXAxisComparer();



        /// <summary>
        /// Sorts ISceneContainerObjects by the Y axis sort point in reverse order.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> NegativeYAxisSort = new NegativeYAxisComparer();



        /// <summary>
        /// The sort method which will be used if none is specified.
        /// </summary>
        public static readonly IComparer<ISceneContainerObject> DefaultSort = LayerOrderSort;

        #endregion


        #region Private, protected, internal methods

        internal class SortMode
        {
            public int Layer = 0;
            public IComparer<ISceneContainerObject> Comparer = null;
        }



        // Special properties for use by deserializer.
        [XmlElement(ElementName = "SortModes")]
        [TorqueXmlDeserializeInclude]
        internal List<SortMode> XmlElements
        {
            set
            {
                foreach (SortMode mode in value)
                {
                    // Do not store invalid comparers!
                    if (mode.Comparer != null)
                        this[mode.Layer] = mode.Comparer;
                }

                // We don't save the input list, because we 
                // don't need it anymore.
            }
            get { return null; }
        }



        #region Private IComparer objects for sorting scene objects

        /// <summary>
        /// Perform no actual sorting on layers.
        /// </summary>
        private class NoSortComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                // Do nothing... this is a dummy object and is
                // never really called for sorting.  We just use
                // it to detect when we can skip sorting.
                return 0;
            }
        }



        private class LayerComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                ISceneObject2D so1 = x as ISceneObject2D;
                ISceneObject2D so2 = y as ISceneObject2D;

                return so1.Layer < so2.Layer ? 1 : so1.Layer > so2.Layer ? -1 : 0;
            }
        }



        /// <summary>
        /// Sorts ISceneContainerObjects by layer order.  This normally resolves
        /// to the object id, which is essentially the add order.
        /// </summary>
        private class LayerOrderComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                int lo1;
                if (x is T2DSceneObject)
                    lo1 = (x as T2DSceneObject).LayerOrder;
                else
                {
                    Assert.Fatal(x is ISceneObject2D, "Invalid object passed to LayerOrderSort!");
                    lo1 = (x as ISceneObject2D).LayerOrder;
                }

                int lo2;
                if (y is T2DSceneObject)
                    lo2 = (y as T2DSceneObject).LayerOrder;
                else
                {
                    Assert.Fatal(y is ISceneObject2D, "Invalid object passed to LayerOrderSort!");
                    lo2 = (y as ISceneObject2D).LayerOrder;
                }

                return lo1 < lo2 ? -1 : lo1 > lo2 ? 1 : 0;
            }
        }



        /// <summary>
        /// Sorts ISceneContainerObjects by the X axis sort point.
        /// </summary>
        private class XAxisComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                // In release builds sometimes the floating point math below can
                // return unequal values when the compare is passed the same object 
                // for x and y... presumably this is an imprecision in optimization.
                // This imprecision causes the following assertion to fire:
                // 
                // IComparer (or the IComparable methods it relies upon) did not 
                // return zero when Array.Sort called x.CompareTo(x).
                //
                // To fix this assertion we check at the start if we have the same
                // object for x and y and return 0.
                if (x == y)
                    return 0;

                float x1;
                if (x is T2DSceneObject)
                {
                    T2DSceneObject so = x as T2DSceneObject;
                    x1 = so.Position.X + (so.SortPoint.X * (so.Size.X / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    x1 = -System.Single.MaxValue;
                }

                float x2;
                if (y is T2DSceneObject)
                {
                    T2DSceneObject so = y as T2DSceneObject;
                    x2 = so.Position.X + (so.SortPoint.X * (so.Size.X / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    x2 = -System.Single.MaxValue;
                }

                return x1 < x2 ? -1 : x1 > x2 ? 1 : 0;
            }
        }



        /// <summary>
        /// Sorts ISceneContainerObjects by the Y axis sort point.
        /// </summary>
        private class YAxisComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                // In release builds sometimes the floating point math below can
                // return unequal values when the compare is passed the same object 
                // for x and y... presumably this is an imprecision in optimization.
                // This imprecision causes the following assertion to fire:
                // 
                // IComparer (or the IComparable methods it relies upon) did not 
                // return zero when Array.Sort called x.CompareTo(x).
                //
                // To fix this assertion we check at the start if we have the same
                // object for x and y and return 0.
                if (x == y)
                    return 0;

                float y1;
                if (x is T2DSceneObject)
                {
                    T2DSceneObject so = x as T2DSceneObject;
                    y1 = so.Position.Y + (so.SortPoint.Y * (so.Size.Y / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    y1 = -System.Single.MaxValue;
                }

                float y2;
                if (y is T2DSceneObject)
                {
                    T2DSceneObject so = y as T2DSceneObject;
                    y2 = so.Position.Y + (so.SortPoint.Y * (so.Size.Y / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    y2 = -System.Single.MaxValue;
                }

                return y1 < y2 ? -1 : y1 > y2 ? 1 : 0;
            }
        }



        /// <summary>
        /// Sorts ISceneContainerObjects by the X axis sort point in reverse order.
        /// </summary>
        private class NegativeXAxisComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                // In release builds sometimes the floating point math below can
                // return unequal values when the compare is passed the same object 
                // for x and y... presumably this is an imprecision in optimization.
                // This imprecision causes the following assertion to fire:
                // 
                // IComparer (or the IComparable methods it relies upon) did not 
                // return zero when Array.Sort called x.CompareTo(x).
                //
                // To fix this assertion we check at the start if we have the same
                // object for x and y and return 0.
                if (x == y)
                    return 0;

                float x1;
                if (x is T2DSceneObject)
                {
                    T2DSceneObject so = x as T2DSceneObject;
                    x1 = so.Position.X + (so.SortPoint.X * (so.Size.X / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    x1 = -System.Single.MaxValue;
                }

                float x2;
                if (y is T2DSceneObject)
                {
                    T2DSceneObject so = y as T2DSceneObject;
                    x2 = so.Position.X + (so.SortPoint.X * (so.Size.X / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    x2 = -System.Single.MaxValue;
                }

                return x1 > x2 ? -1 : x1 < x2 ? 1 : 0;
            }
        }



        /// <summary>
        /// Sorts ISceneContainerObjects by the Y axis sort point in reverse order.
        /// </summary>
        private class NegativeYAxisComparer : IComparer<ISceneContainerObject>
        {
            public int Compare(ISceneContainerObject x, ISceneContainerObject y)
            {
                // In release builds sometimes the floating point math below can
                // return unequal values when the compare is passed the same object 
                // for x and y... presumably this is an imprecision in optimization.
                // This imprecision causes the following assertion to fire:
                // 
                // IComparer (or the IComparable methods it relies upon) did not 
                // return zero when Array.Sort called x.CompareTo(x).
                //
                // To fix this assertion we check at the start if we have the same
                // object for x and y and return 0.
                if (x == y)
                    return 0;

                float y1;
                if (x is T2DSceneObject)
                {
                    T2DSceneObject so = x as T2DSceneObject;
                    y1 = so.Position.Y + (so.SortPoint.Y * (so.Size.Y / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    y1 = -System.Single.MaxValue;
                }

                float y2;
                if (y is T2DSceneObject)
                {
                    T2DSceneObject so = y as T2DSceneObject;
                    y2 = so.Position.Y + (so.SortPoint.Y * (so.Size.Y / 2));
                }
                else
                {
                    // We sort ISceneObject2D to the front of the list.
                    y2 = -System.Single.MaxValue;
                }

                return y1 > y2 ? -1 : y1 < y2 ? 1 : 0;
            }
        }

        #endregion

        #endregion
    }
}