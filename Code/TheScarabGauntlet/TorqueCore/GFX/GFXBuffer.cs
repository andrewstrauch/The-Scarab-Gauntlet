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
    /// Interface common to all GFXBuffer objects of the same
    /// vertex format.
    /// 
    /// For instance, to create a common interface to all PCTTBN
    /// format buffers, use the following:
    /// 
    /// IGFXBuffer&lt;GFXVertexFormat.PCTTBN&gt;
    /// 
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public interface IGFXBuffer<TGFXFormat>
    {
        #region Public properties, operators, constants, and enums

        GraphicsResource Buffer
        {
            get;
        }



        int StartIndex
        {
            get;
        }



        int Count
        {
            get;
        }



        int ElementSize
        {
            get;
        }

        #endregion

        #region Public methods

        TGFXFormat[] GetScratchArray(int count);
        void SetData(TGFXFormat[] data);
        void SetData(TGFXFormat[] data, int startindex, int count);
        bool AreBufferContentsValid();
        void Dispose();

        #endregion
    }



    /// <summary>
    /// Base GFXBuffer class - introduces buffer type
    /// awareness.
    /// 
    /// It's best to use GraphicsResource derived types,
    /// most commonly VertexBuffer and IndexBuffer).
    /// 
    /// </summary>
    /// <typeparam name="TD3DBufferType">buffer type</typeparam>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    /// <typeparam name="TGFXUsage">resource usage specifier</typeparam>
    public abstract class GFXBuffer<TD3DBufferType, TGFXFormat, TGFXUsage> : IGFXBuffer<TGFXFormat>
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The index into the wrapped VertexBuffer at which the vertex elements begin.
        /// </summary>
        public virtual int StartIndex
        {
            get { return 0; }
        }



        /// <summary>
        /// The number of elements in the wrapped VertexBuffer.
        /// </summary>
        public virtual int Count
        {
            get { return _count; }
        }



        public abstract GraphicsResource Buffer
        {
            get;
        }



        /// <summary>
        /// The size in bytes of a vertex element.
        /// </summary>
        public abstract int ElementSize
        {
            get;
        }

        #endregion


        #region Constructors

        public GFXBuffer(int count, TGFXUsage usage)
        {
            _count = count;
            _usage = usage;
        }

        #endregion


        #region Public methods

        public TGFXFormat[] GetScratchArray(int count)
        {
            return TorqueUtil.GetScratchArray<TGFXFormat>(count);
        }



        public void SetData(TGFXFormat[] data)
        {
            SetData(data, 0, data.Length);
        }



        public abstract void SetData(TGFXFormat[] data, int startindex, int count);



        public abstract bool AreBufferContentsValid();


        #endregion


        #region Private, protected, internal methods

        protected abstract void _CreateBuffer();

        #endregion


        #region Private, protected, internal fields

        protected int _count;
        protected TD3DBufferType _buffer;
        protected TGFXUsage _usage;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
        }

        #endregion
    }



    /// <summary>
    /// Wraps the defined buffer type in a Resource&lt;&gt; template
    /// and adds resource aware functionality, including reload
    /// on lost device, which allows user transparent resource
    /// reload and disposal.
    /// 
    /// </summary>
    /// <typeparam name="TD3DBufferType">buffer type</typeparam>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    /// <typeparam name="TGFXUsage">resource usage specifier</typeparam>
    public abstract class GFXResourceBuffer<TD3DBufferType, TGFXFormat, TGFXUsage> : GFXBuffer<Resource<TD3DBufferType>, TGFXFormat, TGFXUsage>
    {

        #region Public methods

        public GFXResourceBuffer(int count, TGFXUsage usage) : base(count, usage) { }



        public override void SetData(TGFXFormat[] data, int startindex, int count)
        {
            if (_buffer.IsNull)
                _CreateBuffer();
        }



        public override bool AreBufferContentsValid()
        {
            if (!_buffer.IsNull)
                return true;

            _CreateBuffer();

            return false;
        }

        #endregion
    }



    /// <summary>
    /// Defines the buffer type as VertexBuffer and adds corresponding functionality.
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public class GFXVertexBuffer<TGFXFormat> : GFXResourceBuffer<DynamicVertexBuffer, TGFXFormat, D3DVertexBufferProfile> where TGFXFormat : struct
    {

        #region Public properties, operators, constants, and enums

        public override GraphicsResource Buffer
        {
            get { return _buffer.Instance; }
        }



        public override int ElementSize
        {
            get
            {
                // this should really be based on the format!
                return GFXVertexFormat.VertexSize;
            }
        }

        #endregion


        #region Public methods

        public GFXVertexBuffer(int count, D3DVertexBufferProfile usage) : base(count, usage) { }



        public override void SetData(TGFXFormat[] data, int startindex, int count)
        {
            base.SetData(data, startindex, count);

            Assert.Fatal((!_buffer.IsNull), "GFXVertexBuffer<TGFXFormat>.SetData - Graphics buffer invalid.");
            Assert.Fatal(((startindex + count) <= _count), "Out of range.");

            // unfortunately direct vertex buffer writes only support starting at index 0...
            // based on the XNA docs I assume this is a bug that will be resolved later...
            Assert.Fatal((startindex == 0), "GFXVertexBuffer<TGFXFormat>.SetData - Unsupported start index.");

            GFXDevice.Instance.Device.Vertices[0].SetSource(null, 0, 0);
            _buffer.Instance.SetData<TGFXFormat>(data, 0, count, SetDataOptions.NoOverwrite);
        }



        public override void Dispose()
        {
            if (!_buffer.IsNull)
            {
                _buffer.Instance.ContentLost -= new EventHandler(Buffer_ContentLost);
                _buffer.Instance.Dispose();
                _buffer.Invalidate();
            }
            this._usage = null;
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _CreateBuffer()
        {
            _buffer = ResourceManager.Instance.CreateDynamicVertexBuffer(_usage, _count * ElementSize);
            _buffer.Instance.ContentLost += new EventHandler(Buffer_ContentLost);
        }



        void Buffer_ContentLost(object sender, EventArgs e)
        {
            _buffer.Invalidate();
        }

        #endregion
    }



    /// <summary>
    /// Derived directly from GFXBuffer - provides shared volatile
    /// memory references.
    /// 
    /// Instead of using a buffer type derived from GraphicsResource,
    /// the type is of the GFXBuffer descendant GFXCacheBuffer, which
    /// is used for writing data to the shared buffer manager's cache.
    /// 
    /// The property RenderBuffer defines the buffer used for
    /// rendering, which is re-routed through the Buffer property
    /// for interface compatibility.
    /// 
    /// The properties OffsetIntoBuffer and Count define the section
    /// of the shared buffer manager's chunk pool this buffer references.
    /// 
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public abstract class GFXVolatileSharedVertexBuffer<TGFXFormat> : GFXBuffer<GFXCacheBuffer<TGFXFormat>, TGFXFormat, D3DVertexBufferProfile> where TGFXFormat : struct
    {

        #region Public properties, operators, constants, and enums

        public override GraphicsResource Buffer
        {
            get
            {
                if (_renderBuffer == null)
                    return null;
                GraphicsResource buf = _renderBuffer.Buffer;

                // volatile - after getting this kill the connection.
                Dispose();

                return buf;
            }
        }



        public override int ElementSize
        {
            get
            {
                // this should really be based on the format!
                return GFXVertexFormat.VertexSize;
            }
        }



        public override int StartIndex
        {
            get { return _offsetIntoBuffer; }
        }



        public GFXCacheBuffer<TGFXFormat> WriteBuffer
        {
            set { _buffer = value; }
        }



        public GFXDynamicVertexBuffer<TGFXFormat> RenderBuffer
        {
            set { _renderBuffer = value; }
        }



        public int OffsetIntoBuffer
        {
            get { return _offsetIntoBuffer; }
            set { _offsetIntoBuffer = value; }
        }

        #endregion


        #region Public methods

        public GFXVolatileSharedVertexBuffer(int count) : base(count, ResourceProfiles.ManualDynamicVBProfile) { }



        public override void SetData(TGFXFormat[] data, int startindex, int count)
        {
            // volatile.
            Dispose();
            _CreateBuffer();

            Assert.Fatal((_buffer != null), "GFXVolatileSharedVertexBuffer<TGFXFormat>.SetData - Graphics buffer invalid.");
            Assert.Fatal(((startindex + count) <= _count), "GFXVolatileSharedVertexBuffer<TGFXFormat>.SetData - Out of range.");

            // add the index into the shared buffer.
            int start = startindex + _offsetIntoBuffer;
            _buffer.SetData(data, start, count);
        }



        public override void Dispose()
        {
            _buffer = null;
            _renderBuffer = null;
        }



        public override bool AreBufferContentsValid()
        {
            // volatile.
            Dispose();

            return false;
        }

        #endregion


        #region Private, protected, internal fields

        protected int _offsetIntoBuffer;
        protected GFXDynamicVertexBuffer<TGFXFormat> _renderBuffer;

        #endregion
    }



    /// <summary>
    /// Derived directly from GFXBuffer - provides memory
    /// in the same vertex format as a GraphicsResource
    /// based GFXBuffer descendant, acting as a cache
    /// mechanism between a user object and the buffer.
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public class GFXCacheBuffer<TGFXFormat> : GFXBuffer<TGFXFormat[], TGFXFormat, int> where TGFXFormat : struct
    {

        #region Public properties, operators, constants, and enums

        public override GraphicsResource Buffer
        {
            get { return null; }
        }



        public override int ElementSize
        {
            get { return 0; }
        }



        public TGFXFormat[] InternalBuffer
        {
            get { return _buffer; }
        }

        #endregion


        #region Public methods

        public GFXCacheBuffer(int count) : base(count, 0) { }



        public override void SetData(TGFXFormat[] data, int startindex, int count)
        {
            if (_buffer == null)
                _CreateBuffer();

            Assert.Fatal((_buffer != null), "GFXCacheBuffer<TGFXFormat>.SetData - Invalid buffer.");
            Assert.Fatal(((startindex + count) <= _count), "GFXCacheBuffer<TGFXFormat>.SetData - Out of range.");

            for (int i = 0; i < count; i++)
                _buffer[i + startindex] = data[i];
        }



        public override bool AreBufferContentsValid()
        {
            if (_buffer != null)
                return true;

            _CreateBuffer();

            return false;
        }

        public override void Dispose()
        {
            _buffer = null;
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _CreateBuffer()
        {
            _buffer = new TGFXFormat[_count];
        }

        #endregion
    }
}

