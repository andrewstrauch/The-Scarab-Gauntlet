//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// A node in an index pool chain.
    /// </summary>
    public interface IIndexPoolerNode
    {
        int Previous { get; set; }
        int Next { get; set; }
    }

    /// <summary>
    /// Chain of objects that are pooled.
    /// </summary>
    public interface IIndexPoolerChain
    {
        int Head { get; set; }
        int Tail { get; set; }
        int Allocated { get; set; }
    }

    /// <summary>
    /// Pooler for pooling objects with the ability to specify a capacity and automatically create
    /// a number of objects. By default, 1000 nodes are allocated.
    /// </summary>
    /// <typeparam name="PoolType"></typeparam>
    public class IndexPooler<PoolType> where PoolType : IIndexPoolerNode
    {
        #region Constructors

        public IndexPooler()
        {
            _IncreaseCapacity(1000);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The pool.
        /// </summary>
        public PoolType[] Pool
        {
            get { return _poolArray; }
        }

        /// <summary>
        /// The total number of objects in the pool, free and allocated.
        /// </summary>
        public int Capacity
        {
            get { return Free + Allocated; }
        }

        /// <summary>
        /// The number of free objects in the pool.
        /// </summary>
        public int Free
        {
            get { return _freePoolCount; }
        }

        /// <summary>
        /// The number of allocated objects in the pool.
        /// </summary>
        public int Allocated
        {
            get { return _allocatedPoolCount; }
        }

        /// <summary>
        /// The minimum number of nodes to allocate during each allocation.
        /// </summary>
        public static int ChunkSize
        {
            get { return _minimumChunkSize; }
            set { _minimumChunkSize = Math.Max(0, value); }
        }

        /// <summary>
        /// The index of the last node.
        /// </summary>
        public static int NodeEndMarker
        {
            get { return _nodeEndMarker; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Allocate a specific number of nodes in the pool.
        /// </summary>
        /// <param name="quantity">The number of nodes to allocate.</param>
        /// <param name="poolerChain">The chain to allocate nodes in.</param>
        /// <returns>The index of the first node that was allocated.</returns>
        public int AllocateNodes(int quantity, IIndexPoolerChain poolerChain)
        {
            // Sanity!
            Assert.Fatal(quantity > 0, "IndexPooler.AllocateNodes - Node request must be greater than 0!");

            // Do we have enough nodes free?
            if (Free < quantity)
            {
                // No, so increase capacity to at least what we need.
                _IncreaseCapacity(quantity);
            }

            // Sanity!
            Assert.Fatal(Free >= quantity, "IndexPooler.AllocateNodes - Capacity increase but still not enough nodes free!");

            // Get Start/End free index.
            int startFreeIndex = _freeHeadIndex;
            int endFreeIndex = _freeHeadIndex;

            // Do we exactly what we require?
            if (Free == quantity)
            {
                // Yes, so we know the end without scanning!
                endFreeIndex = _freeTailIndex;

                // Sanity!
                Assert.Fatal(_poolArray[endFreeIndex].Next == _nodeEndMarker, "IndexPooler.AllocateNodes - All nodes should be allocated!");

                // Mark head/tail as end.
                _freeHeadIndex = _freeTailIndex = _nodeEndMarker;
            }
            else
            {
                // No, we have more than we require so scan for end index.
                for (int n = 1; n < quantity; ++n)
                {
                    endFreeIndex = _poolArray[endFreeIndex].Next;
                }

                // Adjust new head index.
                _freeHeadIndex = _poolArray[endFreeIndex].Next;

                // Did we reach the end?
                if (_freeHeadIndex == _nodeEndMarker)
                {
                    // Sanity!
                    Assert.Fatal(quantity == Free, "IndexPooler.AllocateNodes - Encountered end node but nodes still free!");

                    // Yes, so mark tail as end.
                    _freeTailIndex = _nodeEndMarker;
                }
                else
                {
                    // No, so mark start of new list.
                    _poolArray[_freeHeadIndex].Previous = _nodeEndMarker;
                }

                // Mark end of allocated list.
                // NOTE:-   The start was the head so it'll already be marked.
                _poolArray[endFreeIndex].Next = _nodeEndMarker;
            }

            // Is the pooler-chain empty?
            if (poolerChain.Head == _nodeEndMarker)
            {
                // Yes, so chain becomes our new free indice.
                poolerChain.Head = startFreeIndex;
                poolerChain.Tail = endFreeIndex;
            }
            else
            {
                // No, so add our new free indice to chain.
                _poolArray[poolerChain.Tail].Next = startFreeIndex;
                _poolArray[startFreeIndex].Previous = poolerChain.Tail;
                poolerChain.Tail = endFreeIndex;
            }

            // Increase Chain Allocation.
            poolerChain.Allocated += quantity;

            // Decrease free pool count.
            _freePoolCount -= quantity;
            // Increase allocated pool count.
            _allocatedPoolCount += quantity;

            // Return start of free nodes.
            return startFreeIndex;
        }

        /// <summary>
        /// Free every object in the pool.
        /// </summary>
        /// <param name="poolerChain">The pool to be cleared.</param>
        public void FreeAllNodes(IIndexPoolerChain poolerChain)
        {
            // Sanity!
            Assert.Fatal(poolerChain.Head != _nodeEndMarker, "IndexPooler.FreeAllNodes - Cannot free nodes from empty chain!");

            // Is Pool empty?
            if (_freePoolCount == 0)
            {
                // Yes, so simply use free nodes.
                _freeHeadIndex = poolerChain.Head;
                _freeTailIndex = poolerChain.Tail;
            }
            else
            {
                // No, so link into free pool.
                _poolArray[_freeTailIndex].Next = poolerChain.Head;
                _poolArray[poolerChain.Head].Previous = _freeTailIndex;
                _freeTailIndex = poolerChain.Tail;
            }

            // Increase free pool count.
            _freePoolCount += poolerChain.Allocated;
            // Decrease allocated pool count.
            _allocatedPoolCount -= poolerChain.Allocated;

            // Reset Pooler Chain.
            poolerChain.Head = poolerChain.Tail = _nodeEndMarker;
            poolerChain.Allocated = 0;
        }

        /// <summary>
        /// Release the node at the specified index.
        /// </summary>
        /// <param name="index">The index of the node to free.</param>
        /// <param name="poolerChain">The pool chain that contains the index.</param>
        public void FreeNode(int index, IIndexPoolerChain poolerChain)
        {
            // Sanity!
            Assert.Fatal(poolerChain.Head != _nodeEndMarker, "IndexPooler.FreeNode - Cannot free nodes from empty chain!");
            Assert.Fatal(index < Capacity, "IndexPooler.FreeNode - Cannot free invalid node!");

            // Is this the last node in the pooler-chain?
            if (poolerChain.Allocated == 1)
            {
                // Yes, so reset pooler-chain head/tail.
                poolerChain.Head = poolerChain.Tail = _nodeEndMarker;
            }
            else
            {
                // No, so is the index the pooler-chain head?
                if (poolerChain.Head == index)
                {
                    // Yes, so move head to next node.
                    poolerChain.Head = _poolArray[index].Next;
                    _poolArray[poolerChain.Head].Previous = _nodeEndMarker;
                }
                // No, so is the index the pooler-chain tail?
                else if (poolerChain.Tail == index)
                {
                    // Yes, so move the tail to the previous node.
                    poolerChain.Tail = _poolArray[index].Previous;
                    _poolArray[poolerChain.Tail].Next = _nodeEndMarker;
                }
                // No, so link adjacent nodes.
                else
                {
                    int previousIndex = _poolArray[index].Previous;
                    int nextIndex = _poolArray[index].Next;
                    _poolArray[previousIndex].Next = nextIndex;
                    _poolArray[nextIndex].Previous = previousIndex;
                }
            }

            // Is Pool empty?
            if (_freePoolCount == 0)
            {
                // Yes, so simply use free.
                _freeHeadIndex = index;
                _freeTailIndex = index;
                _poolArray[index].Previous = _nodeEndMarker;
                _poolArray[index].Next = _nodeEndMarker;
            }
            else
            {
                // No, so link into free pool.
                _poolArray[_freeTailIndex].Next = index;
                _poolArray[index].Previous = _freeTailIndex;
                _poolArray[index].Next = _nodeEndMarker;
                _freeTailIndex = index;
            }

            // Increase free pool count.
            _freePoolCount++;
            // Decrease allocated pool count.
            _allocatedPoolCount--;
            // Decrease allocated pooler-chain.
            poolerChain.Allocated--;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Increases the number of free objects in the pool to the specified number.
        /// </summary>
        /// <param name="minimumFree">The number of free objects to make sure are allocated.</param>
        private void _IncreaseCapacity(int minimumFree)
        {
            int chunkFrame;

            // Is the chunk-size adequate for the request?
            if (minimumFree <= _minimumChunkSize)
                chunkFrame = _minimumChunkSize;

            else
                chunkFrame = minimumFree;

            // Is the pool valid?
            if (_poolArray != null)
            {
                // Yes, so note next logical new start/end indice.
                int newStartIndex = _poolArray.GetLength(0);
                int newEndIndex = newStartIndex + chunkFrame - 1;

                // Increase Capacity.
                TorqueUtil.ResizeArray<PoolType>(ref _poolArray, newEndIndex + 1);

                // Chain nodes together.
                for (int n = newStartIndex; n <= newEndIndex; ++n)
                {
                    _poolArray[n].Previous = n - 1;
                    _poolArray[n].Next = n + 1;
                }

                // Mark start/end nodes.
                // NOTE:-   If the pool was empty them the tail-index will have
                //          an end-marker here else it'll correctly reference the
                //          tail of any previously free nodes.
                _poolArray[newStartIndex].Previous = _freeTailIndex;
                _poolArray[newEndIndex].Next = _nodeEndMarker;

                // Was the pool previously empty?
                if (_freeHeadIndex == _nodeEndMarker)
                {
                    // Yes, so set free head to start of new chunk.
                    _freeHeadIndex = newStartIndex;
                }
                else
                {
                    // No, so attach new chain to old one.
                    _poolArray[_freeTailIndex].Next = newStartIndex;
                }

                // Setup Tail.
                _freeTailIndex = newEndIndex;

                // Increase free pool count.
                _freePoolCount += chunkFrame;
            }
            else
            {
                // No, so setup initial pool.
                _poolArray = new PoolType[chunkFrame];

                // Setup Free Head/Tail.
                _freeHeadIndex = 0;
                _freeTailIndex = chunkFrame - 1;

                // Chain nodes together.
                for (int n = 0; n < chunkFrame; ++n)
                {
                    _poolArray[n].Previous = n - 1;
                    _poolArray[n].Next = n + 1;
                }

                // Mark start/end nodes.
                _poolArray[0].Previous = _nodeEndMarker;
                _poolArray[chunkFrame - 1].Next = _nodeEndMarker;

                // Increase free pool count.
                _freePoolCount += chunkFrame;
            }
        }

        #endregion


        #region Private, protected, internal fields

        // Pool Array.
        private PoolType[] _poolArray;

        // Head/Tail Indice.
        private int _freeHeadIndex;
        private int _freeTailIndex;

        // Minimum Chunk Size.
        private static int _minimumChunkSize = 250;

        // Pool Metrics.
        private int _freePoolCount = 0;
        private int _allocatedPoolCount = 0;

        // Node End Marker.
        private const int _nodeEndMarker = -1;

        #endregion
    }

}
