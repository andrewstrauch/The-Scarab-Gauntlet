//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GFX
{
    /// <summary>
    /// Base volatile manager class - supports arbitrary buffer types (vertex, index, ...).
    /// 
    /// Requested volatile buffers are drawn from the manager's pool of dynamic buffer chunks
    /// (think of these as memory pages).  Changes to the volatile buffer apply directly to
    /// the manager's cache (cache buffer size is equal to chunk size, and represents a view
    /// of the currently active chunk).
    /// 
    /// When a volatile buffer request exceeds the available chunk space or rendering begins,
    /// cache data is flushed to the current active chunk and a new chunk is made active.
    /// 
    /// After rendering volatile buffers are invalidated and the active chunk reset, however
    /// existing chunks are cached and reused.
    /// 
    /// Volatile buffer sizes cannot exceed the manager chunk size.
    /// </summary>
    /// <typeparam name="TGFXIntBufferType">chunk dynamic buffer type</typeparam>
    /// <typeparam name="TGFXExtBufferType">volatile request buffer type</typeparam>
    /// <typeparam name="TGFXCacheBufferType">cache buffer type</typeparam>
    public abstract class GFXVolatileBufferSourceManager<TGFXIntBufferType, TGFXExtBufferType, TGFXCacheBufferType>
    {

        #region Public methods

        public GFXVolatileBufferSourceManager(int chunkelementcount)
        {
            _chunkElementCount = chunkelementcount;

            _locked = false;

            // force alloc on first use.
            _Reset();
        }



        public void ReserveBuffer(int elementcount, TGFXExtBufferType buffer)
        {
            Assert.Fatal((elementcount <= _chunkElementCount), "Requesting too much data.");
            Assert.Fatal(!_locked, "Unable to request data from a locked buffer.");

            // will it fit?
            if ((_currentChunkElementOffset + elementcount) > _chunkElementCount)
            {
                // no then flush the cache.
                _FlushCache();

                // increase chunk index.
                _currentChunk++;
                _currentChunkElementOffset = 0;

                // make sure the chunk exists.
                if (_currentChunk >= _chunks.Count)
                    _Alloc();
            }

            _AssignBuffer(buffer);

            _currentChunkElementOffset += elementcount;
        }



        public void FlushAndLock()
        {
            Assert.Fatal(!_locked, "Buffer already locked.");

            _FlushCache();
            _Reset();

            _locked = true;
        }



        public void Unlock()
        {
            Assert.Fatal(_locked, "Buffer already unlocked.");

            _locked = false;
        }



        public virtual void Dispose()
        {
            _locked = false;

            _Reset();

            _cache = default(TGFXCacheBufferType);
            _chunks.Clear();
        }



        public bool AreBufferContentsFlushed()
        {
            return (_currentChunk == -1);
        }

        #endregion


        #region Private, protected, internal methods

        protected void _Reset()
        {
            _currentChunk = -1;
            _currentChunkElementOffset = _chunkElementCount + 1;
        }



        protected abstract void _Alloc();
        protected abstract void _AssignBuffer(TGFXExtBufferType buffer);
        protected abstract void _FlushCache();

        #endregion


        #region Private, protected, internal fields

        protected bool _locked;
        protected int _chunkElementCount;
        protected int _currentChunk;
        protected int _currentChunkElementOffset;
        protected TGFXCacheBufferType _cache;
        protected List<TGFXIntBufferType> _chunks = new List<TGFXIntBufferType>();

        #endregion
    }



    /// <summary>
    /// Volatile vertex buffer management class - supports user defined vertex formats.
    /// 
    /// See GFXVolatileBufferSourceManager for details.
    /// 
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public class GFXVolatileVertexBufferSourceManager<TGFXFormat> : GFXVolatileBufferSourceManager<GFXDynamicVertexBuffer<TGFXFormat>, GFXVolatileSharedVertexBuffer<TGFXFormat>, GFXCacheBuffer<TGFXFormat>> where TGFXFormat : struct
    {

        #region Public methods

        public GFXVolatileVertexBufferSourceManager(int chunkelementcount) : base(chunkelementcount) { }



        public override void Dispose()
        {
            if (_cache != null)
                _cache.Dispose();


            for (int index = 0; index < _chunks.Count; index++)
            {
                _chunks[index].Dispose();
            }

            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _Alloc()
        {
            if (_cache == null)
                _cache = new GFXCacheBuffer<TGFXFormat>(_chunkElementCount);

            GFXDynamicVertexBuffer<TGFXFormat> chunk = new GFXDynamicVertexBuffer<TGFXFormat>(_chunkElementCount);
            _chunks.Add(chunk);

            // verify buffer created.
            chunk.AreBufferContentsValid();
        }



        protected override void _AssignBuffer(GFXVolatileSharedVertexBuffer<TGFXFormat> buffer)
        {
            Assert.Fatal((_currentChunk >= 0), "GFXVolatileVertexBufferSourceManager<TGFXFormat>._AssignBuffer - Index out of range.");
            Assert.Fatal((_currentChunk < _chunks.Count), "GFXVolatileVertexBufferSourceManager<TGFXFormat>._AssignBuffer - Index out of range.");
            Assert.Fatal((_cache != null), "GFXVolatileVertexBufferSourceManager<TGFXFormat>._AssignBuffer - Invalid cache buffer.");

            buffer.WriteBuffer = _cache;
            buffer.RenderBuffer = _chunks[_currentChunk];
            buffer.OffsetIntoBuffer = _currentChunkElementOffset;
        }



        protected override void _FlushCache()
        {
            if (_currentChunk < 0)
                return;

            TGFXFormat[] data = _cache.InternalBuffer;
            _chunks[_currentChunk].SetData(data, 0, _currentChunkElementOffset);
        }

        #endregion
    }
}

