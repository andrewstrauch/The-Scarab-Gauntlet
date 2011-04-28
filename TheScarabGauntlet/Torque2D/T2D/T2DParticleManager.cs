//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Particle.
    /// </summary>
    public struct T2DParticle : IIndexPoolerNode
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// IIndexPoolerNode: Previous Particle. 
        /// </summary>
        public int Previous
        {
            get { return _previousIndex; }
            set { _previousIndex = value; }
        }



        /// <summary>
        /// IIndexPoolerNode: Next Particle.
        /// </summary>
        public int Next
        {
            get { return _nextIndex; }
            set { _nextIndex = value; }
        }



        public float Lifetime
        {
            get
            {
                return _lifetime;
            }
            set
            {
                _lifetime = value;
            }
        }



        public float Age
        {
            get
            {
                return _age;
            }
            set
            {
                _age = value;
            }
        }



        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public float PositionX
        {
            get
            {
                return _position.X;
            }
            set
            {
                _position.X = value;
            }
        }



        public float PositionY
        {
            get
            {
                return _position.Y;
            }
            set
            {
                _position.Y = value;
            }
        }



        public Vector2 Velocity
        {
            get
            {
                return _velocity;
            }
            set
            {
                _velocity = value;
            }
        }



        public float VelocityX
        {
            get
            {
                return _velocity.X;
            }
            set
            {
                _velocity.X = value;
            }
        }



        public float VelocityY
        {
            get
            {
                return _velocity.Y;
            }
            set
            {
                _velocity.Y = value;
            }
        }



        public float RotationAngle
        {
            get
            {
                return _rotationAngle;
            }
            set
            {
                _rotationAngle = value;
            }
        }



        public Vector2 Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }



        public float SizeX
        {
            get
            {
                return _size.X;
            }
            set
            {
                _size.X = value;
            }
        }



        public float SizeY
        {
            get
            {
                return _size.Y;
            }

            set
            {
                _size.Y = value;
            }
        }



        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
            }
        }



        public float Spin
        {
            get
            {
                return _spin;
            }
            set
            {
                _spin = value;
            }
        }



        public float FixedForce
        {
            get
            {
                return _fixedForce;
            }
            set
            {
                _fixedForce = value;
            }
        }



        public float RandomMotion
        {
            get
            {
                return _randomMotion;
            }
            set
            {
                _randomMotion = value;
            }
        }

        #endregion


        #region Private, protected, internal fields

        // IIndexPoolerNode.
        private int _previousIndex;
        private int _nextIndex;

        // Particle Components.
        private float _lifetime;
        private float _age;
        internal Vector2 _position;
        internal Vector2 _velocity;
        internal float _rotationAngle;

        // Base Properties.
        internal Vector2 _size;
        internal float _speed;
        internal float _spin;
        internal float _fixedForce;
        internal float _randomMotion;

        #endregion
    }



    /// <summary>
    /// Singleton Particle Manager.  Used by particle emitters to generate particles.
    /// </summary>
    sealed public class T2DParticleManager
    {
        #region Public properties, operators, constants, and enums

        // Get Singleton Instance.
        public static T2DParticleManager Instance
        {
            get
            {
                return _instance = _instance ?? new T2DParticleManager();
            }
        }



        /// <summary>
        /// Pool of all particles.
        /// </summary>
        public T2DParticle[] Pool
        {
            get
            {
                return _particlePool.Pool;
            }
        }



        /// <summary>
        /// Current particle capacity.  This is not a hard limit.  If number of particles
        /// exceeds capacity then more space is allocated.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _particlePool.Capacity;
            }
        }



        /// <summary>
        /// Number of particle slots not currently in used.
        /// </summary>
        public int Free
        {
            get
            {
                return _particlePool.Free;
            }
        }



        /// <summary>
        /// Number of particle slots currently in use.
        /// </summary>
        public int Allocated
        {
            get
            {
                return _particlePool.Allocated;
            }
        }



        /// <summary>
        /// Number of new particles slots which will be created if 
        /// current Capacity is exceeded.
        /// </summary>
        public int ChunkSize
        {
            get
            {
                return IndexPooler<T2DParticle>.ChunkSize;
            }
            set
            {
                // Sanity!
                Assert.Fatal(value > 0, "Capacity chunk size must be >0!");

                IndexPooler<T2DParticle>.ChunkSize = value;
            }
        }



        /// <summary>
        /// Index indicating end of list of particles.
        /// </summary>
        public static int NodeEndMarker
        {
            get
            {
                return IndexPooler<T2DParticle>.NodeEndMarker;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Allocate a quantity of particles.  No initialization occurs.
        /// </summary>
        /// <param name="quantity">Number of particles to allocate.</param>
        /// <param name="poolerChain">Pooler to allocate from.</param>
        /// <returns>Index of first allocated particle.</returns>
        public int AllocateParticles(int quantity, IIndexPoolerChain poolerChain)
        {
            // Pass through to our internal index-pooler.
            int startFreeIndex = _particlePool.AllocateNodes(quantity, poolerChain);

            // Return start free index.
            return startFreeIndex;
        }



        /// <summary>
        /// Free all particles withing a given pool.
        /// </summary>
        /// <param name="poolerChain">Pool of particles to clear.</param>
        public void FreeAllParticles(IIndexPoolerChain poolerChain)
        {
            // Are any particles allocation?
            if (poolerChain.Allocated == 0)
            {
                // Sanity!
                Assert.Fatal(poolerChain.Head == T2DParticleManager.NodeEndMarker && poolerChain.Tail == T2DParticleManager.NodeEndMarker,
                                "Emitter particle pool has no allocated nodes but chain isn't empty!");

                // No, so return!
                return;
            }

            // Pass through to our internal index-pooler.
            _particlePool.FreeAllNodes(poolerChain);

            // Sanity!
            Assert.Fatal(poolerChain.Head == T2DParticleManager.NodeEndMarker && poolerChain.Tail == T2DParticleManager.NodeEndMarker,
                            "Emitter particle pool deallocated but chain isn't empty!");
        }



        /// <summary>
        /// Free a single particle, removing from linked list.
        /// </summary>
        /// <param name="index">Index of particle to free.</param>
        /// <param name="poolerChain">Particle pool from which originally allocated.</param>
        public void FreeParticle(int index, IIndexPoolerChain poolerChain)
        {
            // Pass through to our internal index-pooler.
            _particlePool.FreeNode(index, poolerChain);
        }

        #endregion


        #region Private, protected, internal fields

        IndexPooler<T2DParticle> _particlePool = new IndexPooler<T2DParticle>();
        private static T2DParticleManager _instance;

        #endregion
    }

}
