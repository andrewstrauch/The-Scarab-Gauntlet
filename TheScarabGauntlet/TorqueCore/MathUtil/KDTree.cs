//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// A KD tree implementation.
    /// </summary>
    /// <typeparam name="T">The type of data to store in the tree.</typeparam>
    public class KDTree<T> : IDisposable
    {
        /// <summary>
        /// A node in the KD tree
        /// </summary>
        public struct KDTreeNode
        {

            #region Public properties, operators, constants, and enums


            /// <summary>
            /// 0 for x axis, 1 for y axis, 2 for z axis
            /// </summary>
            public int Axis;

            /// <summary>
            /// The distance along the axis of this node.
            /// </summary>
            public float D;

            /// <summary>
            /// Data stored in the node.
            /// </summary>
            public T Data;

            /// <summary>
            /// Index of the node to the left of this one.
            /// </summary>
            public int LeftIndex;

            /// <summary>
            /// Index of the node to the right of this one.
            /// </summary>
            public int RightIndex;

            #endregion
        }

        #region Comparers...

        class XComp : IComparer<Vector3>
        {
            public int Compare(Vector3 a, Vector3 b)
            {
                return a.X < b.X ? -1 : 1;
            }
        }

        class YComp : IComparer<Vector3>
        {
            public int Compare(Vector3 a, Vector3 b)
            {
                return a.Y < b.Y ? -1 : 1;
            }
        }

        class ZComp : IComparer<Vector3>
        {
            public int Compare(Vector3 a, Vector3 b)
            {
                return a.Z < b.Z ? -1 : 1;
            }
        }

        class XBoxMinComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Min.X < b.Min.X ? -1 : 1;
            }
        }

        class YBoxMinComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Min.Y < b.Min.Y ? -1 : 1;
            }
        }

        class ZBoxMinComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Min.Z < b.Min.Z ? -1 : 1;
            }
        }

        class XBoxMaxComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Max.X < b.Max.X ? -1 : 1;
            }
        }

        class YBoxMaxComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Max.Y < b.Max.Y ? -1 : 1;
            }
        }

        class ZBoxMaxComp : IComparer<Box3F>
        {
            public int Compare(Box3F a, Box3F b)
            {
                return a.Max.Z < b.Max.Z ? -1 : 1;
            }
        }

        #endregion


        #region Static methods, fields, constructors

        static KDTree()
        {
            _comparers.Add(new XComp());
            _comparers.Add(new YComp());
            _comparers.Add(new ZComp());
            _boxMinComparers.Add(new XBoxMinComp());
            _boxMinComparers.Add(new YBoxMinComp());
            _boxMinComparers.Add(new ZBoxMinComp());
            _boxMaxComparers.Add(new XBoxMaxComp());
            _boxMaxComparers.Add(new YBoxMaxComp());
            _boxMaxComparers.Add(new ZBoxMaxComp());
        }

        static List<IComparer<Vector3>> _comparers = new List<IComparer<Vector3>>(3);
        static List<IComparer<Box3F>> _boxMinComparers = new List<IComparer<Box3F>>(3);
        static List<IComparer<Box3F>> _boxMaxComparers = new List<IComparer<Box3F>>(3);
        static List<int> _nodeStack = new List<int>();
        static List<int> _resultNodes = new List<int>();

        // track some stats
        static int _maxDepthAchieved;
        static int _minDepthAchieved;
        static int _avgDepth;
        static int _avgPerBin;
        static int _avgCount;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Number of boxes that can be stored in each bin.
        /// </summary>
        public int BoxesPerBin = 7;

        /// <summary>
        /// Number of points that can be stored in each bin.
        /// </summary>
        public int PointsPerBin = 10;

        /// <summary>
        /// Maximum depth of the tree.
        /// </summary>
        public int MaxDepth = 12;

        #endregion


        #region Public methods

        /// <summary>
        /// Finds all of the node values within a specified box.
        /// </summary>
        /// <param name="minExtent">The minimum point on the search box.</param>
        /// <param name="maxExtent">The maximum point on the search box.</param>
        /// <param name="leavesOnly">Whether or not to only include data from leaf nodes.</param>
        /// <param name="result">A list to be filled with the results.</param>
        public void QueryBox(Vector3 minExtent, Vector3 maxExtent, bool leavesOnly, List<T> result)
        {
            _resultNodes.Clear();
            QueryBox(minExtent, maxExtent, leavesOnly, _resultNodes);

            for (int idx = 0; idx < _resultNodes.Count; idx++)
                result.Add(_nodes[idx].Data);
        }

        /// <summary>
        /// Finds all of the node values within a specified box.
        /// </summary>
        /// <param name="minExtent">The minimum point on the search box.</param>
        /// <param name="maxExtent">The maximum point on the search box.</param>
        /// <param name="leavesOnly">Whether or not to only include data from leaf nodes.</param>
        /// <param name="result">A list to be filled with the indices of the nodes that are found.</param>
        public void QueryBox(Vector3 minExtent, Vector3 maxExtent, bool leavesOnly, List<int> result)
        {
            if (_nodes.Count == 0)
                return;

            _nodeStack.Clear();
            _nodeStack.Add(0);

            while (_nodeStack.Count != 0)
            {
                // pop one off
                int idx = _nodeStack[_nodeStack.Count - 1];
                _nodeStack.RemoveAt(_nodeStack.Count - 1);
                KDTreeNode node = _nodes[idx];

                if (node.LeftIndex == node.RightIndex)
                {
                    // both must be -1, meaning we reached a leaf
                    result.Add(idx);
                    continue;
                }
                if (!leavesOnly)
                    result.Add(idx);

                float minVal = _GetVal(minExtent, node.Axis);
                float maxVal = _GetVal(maxExtent, node.Axis);
                if (minVal <= node.D)
                    _nodeStack.Add(node.LeftIndex);
                if (maxVal >= node.D)
                    _nodeStack.Add(node.RightIndex);
            }
        }

        /// <summary>
        /// Constructs a tree out of the specified points.
        /// </summary>
        /// <param name="points">The points to add to the tree.</param>
        public void BuildTree(List<Vector3> points)
        {
            _ResetStats();

            _nodes.Clear();
            _AddNodes(points, 0, points.Count, 0);

            _DumpStats("Point bins");
        }

        /// <summary>
        /// Build a tree out of the specified boxes.
        /// </summary>
        /// <param name="boxes">The boxes to add to the tree.</param>
        public void BuildTree(List<Box3F> boxes)
        {
            _ResetStats();

            _nodes.Clear();
            _AddNodes(boxes, 0, boxes.Count, 0);

            _DumpStats("Box bins");
        }

        /// <summary>
        /// Find the first node contained within the specified region.
        /// </summary>
        /// <param name="minExtent">The minimum point on the search box.</param>
        /// <param name="maxExtent">The maximum point on the search box.</param>
        /// <returns>The index of the found node.</returns>
        public int FindFirstContainNode(Vector3 minExtent, Vector3 maxExtent)
        {
            int idx = 0;
            int depth = 0;
            while (idx < _nodes.Count)
            {
                KDTreeNode node = _nodes[idx];
                if (node.LeftIndex == node.RightIndex)
                    return idx;

                float minVal = _GetVal(minExtent, depth % 3);
                float maxVal = _GetVal(maxExtent, depth % 3);

                if (minVal <= node.D && maxVal <= node.D)
                    idx = node.LeftIndex;
                else if (minVal >= node.D && maxVal >= node.D)
                    idx = node.RightIndex;
                else
                    return idx;

                depth++;
            }

            return -1;
        }

        /// <summary>
        /// Gets the data from the node at the specified index.
        /// </summary>
        /// <param name="idx">The node index.</param>
        /// <returns>The node value.</returns>
        public T GetNodeData(int idx)
        {
            if (idx < 0 || idx > _nodes.Count - 1)
                return default(T);

            return _nodes[idx].Data;
        }

        /// <summary>
        /// Sets the data on the node at the specified index.
        /// </summary>
        /// <param name="idx">The node index.</param>
        /// <param name="data">The value to set on the node.</param>
        /// <returns>True if the data was set.</returns>
        public bool SetNodeData(int idx, T data)
        {
            if (idx < 0 || idx > _nodes.Count - 1)
                return false;

            KDTreeNode node = _nodes[idx];
            node.Data = data;
            _nodes[idx] = node;

            return true;
        }

        #endregion


        #region Private, protected, internal methods

        int _AddNodes(List<Vector3> points, int start, int end, int depth)
        {
            KDTreeNode node = new KDTreeNode();
            if (end - start < PointsPerBin || depth == MaxDepth)
            {
                // leaf node
                node.Axis = -1;
                node.LeftIndex = -1;
                node.RightIndex = -1;
                _nodes.Add(node);

                // save off some stats
                _maxDepthAchieved = Math.Max(_maxDepthAchieved, depth);
                _minDepthAchieved = Math.Min(_minDepthAchieved, depth);
                _avgDepth += depth;
                _avgPerBin += end - start;
                _avgCount++;

                return _nodes.Count - 1;
            }

            _SortOnAxis(points, start, end, depth % 3);
            int splitIndex = (start + end) >> 1;
            float splitVal = 0.5f * (_GetVal(points[splitIndex], depth % 3) + _GetVal(points[splitIndex + 1], depth % 3));

            // gotta add the node now even though we'll just copy it up again later
            int ret = _nodes.Count;
            _nodes.Add(node);

            // now split
            node.Axis = depth % 3;
            node.D = splitVal;
            node.LeftIndex = _AddNodes(points, start, splitIndex + 1, depth + 1);
            node.RightIndex = _AddNodes(points, splitIndex + 1, end, depth + 1);
            _nodes[ret] = node;
            return ret;
        }

        int _AddNodes(List<Box3F> boxes, int start, int end, int depth)
        {
            KDTreeNode node = new KDTreeNode();
            if (end - start < BoxesPerBin || depth == MaxDepth)
            {
                // leaf node
                node.Axis = -1;
                node.LeftIndex = -1;
                node.RightIndex = -1;
                _nodes.Add(node);

                // save off some stats
                _maxDepthAchieved = Math.Max(_maxDepthAchieved, depth);
                _minDepthAchieved = Math.Min(_minDepthAchieved, depth);
                _avgDepth += depth;
                _avgPerBin += end - start;
                _avgCount++;

                return _nodes.Count - 1;
            }

            // gotta add the node now even though we'll just copy it up again later
            int ret = _nodes.Count;
            _nodes.Add(node);

            _SortOnMinAxis(boxes, start, end, depth % 3);
            int minSplitIndex = (start + end) >> 1;
            float minSplitVal = 0.5f * (_GetMinVal(boxes[minSplitIndex], depth % 3) + _GetMinVal(boxes[minSplitIndex + 1], depth % 3));
            _SortOnMaxAxis(boxes, start, end, depth % 3);
            int maxSplitIndex = (start + end) >> 1;
            float maxSplitVal = 0.5f * (_GetMaxVal(boxes[maxSplitIndex], depth % 3) + _GetMaxVal(boxes[maxSplitIndex + 1], depth % 3));
            float splitVal = 0.5f * (minSplitVal + maxSplitVal);

            // find first box which extends to the left
            int splitIndex;
            for (splitIndex = start; splitIndex < end; splitIndex++)
                if (_GetMaxVal(boxes[splitIndex], depth % 3) > splitVal)
                    break;
            // all boxes from splitIndex and up extend greater than split
            // value, so they overlap right node
            node.RightIndex = _AddNodes(boxes, splitIndex, end, depth + 1);

            // find first box which doesn't go right
            _SortOnMinAxis(boxes, start, end, depth % 3);
            for (splitIndex = start; splitIndex < end; splitIndex++)
                if (_GetMinVal(boxes[splitIndex], depth % 3) > splitVal)
                    break;
            // all boxes up to splitIndex extend less than split
            // value, so they overlap left node
            node.LeftIndex = _AddNodes(boxes, start, splitIndex, depth + 1);

            // now add node for real
            node.Axis = depth % 3;
            node.D = splitVal;
            _nodes[ret] = node;

            return ret;
        }

        void _SortOnAxis(List<Vector3> points, int start, int end, int axis)
        {
            points.Sort(start, end - start, _comparers[axis]);
        }

        void _SortOnMinAxis(List<Box3F> boxes, int start, int end, int axis)
        {
            boxes.Sort(start, end - start, _boxMinComparers[axis]);
        }

        void _SortOnMaxAxis(List<Box3F> boxes, int start, int end, int axis)
        {
            boxes.Sort(start, end - start, _boxMaxComparers[axis]);
        }

        float _GetVal(Vector3 point, int axis)
        {
            return axis == 0 ? point.X : (axis == 1 ? point.Y : point.Z);
        }

        float _GetMinVal(Box3F box, int axis)
        {
            return _GetVal(box.Min, axis);
        }

        float _GetMaxVal(Box3F box, int axis)
        {
            return _GetVal(box.Max, axis);
        }

        void _ResetStats()
        {
            _maxDepthAchieved = -1;
            _minDepthAchieved = MaxDepth + 1;
            _avgDepth = 0;
            _avgPerBin = 0;
            _avgCount = 0;
        }

        void _DumpStats(String technique)
        {
            float avgDepth = (float)_avgDepth;
            float avgPerBin = (float)_avgPerBin;
            if (_avgCount != 0)
            {
                avgDepth *= 1.0f / (float)_avgCount;
                avgPerBin *= 1.0f / (float)_avgCount;
            }

            TorqueConsole.Echo("\nKDTree using {4}\n   - Min depth: {0}\n   - Max depth: {1}\n   - Average Depth: {2}\n   - Average Per Bin: {3}",
                               _minDepthAchieved, _maxDepthAchieved, avgDepth, avgPerBin, technique);
        }

        #endregion


        #region Private, protected, internal fields

        List<KDTreeNode> _nodes = new List<KDTreeNode>();

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _nodes.Clear();
            _nodes = null;
        }

        #endregion
    }
}
