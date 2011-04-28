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
    /// GFXBuffer Generic Interface
    /// 
    /// Unfortunately C# doesn't support global typedefs, and deriving from
    /// IGFXBuffer creates an interface from a different branch of the class
    /// hierarchy than GFXBuffer descendants (ie: can't be directly assigned).
    /// 
    /// Instead use the following to interface arbitrary buffer objects based
    /// on type and usage (assuming same vertex format, currently only one
    /// - PCTTBN - exists):
    /// 
    ///  IGFXBuffer<GFXVertexFormat.PCTTBN>
    ///  

    /// <summary>
    /// User static vertex buffer - supports the PCTTBN vertex format.
    /// 
    /// Capabilities:
    /// -can only be filled once
    /// -can render multiple times
    /// 
    /// </summary>
    public class GFXStaticVertexBufferPCTTBN : GFXStaticVertexBuffer<GFXVertexFormat.PCTTBN>
    {
        public GFXStaticVertexBufferPCTTBN(int count)
            : base(count)
        {
        }
    }



    /// <summary>
    /// User dynamic vertex buffer - supports the PCTTBN vertex format.
    /// 
    /// Capabilities:
    /// -can be filled multiple times
    /// -can render multiple times
    /// 
    /// </summary>
    public class GFXDynamicVertexBufferPCTTBN : GFXDynamicVertexBuffer<GFXVertexFormat.PCTTBN>
    {
        public GFXDynamicVertexBufferPCTTBN(int count)
            : base(count)
        {
        }
    }



    /// <summary>
    /// User volatile-shared vertex buffer - supports the PCTTBN vertex format.
    /// 
    /// Capabilities:
    /// -can be filled multiple times
    /// -can render only once (must be refilled after rendering)
    /// -shares a vertex buffer with other volatile objects (potential for batching)
    /// 
    /// Useful for rendering objects in a semi-immediate-mode - avoids cluttering
    /// memory with infrequently rendered or short lived objects, by sharing a
    /// common memory pool between objects.
    /// 
    /// Avoid requesting large volatile buffers - objects with a large number of
    /// verts should use non-shared static or dynamic buffers.
    /// 
    /// </summary>
    public class GFXVolatileSharedVertexBufferPCTTBN : GFXVolatileSharedVertexBuffer<GFXVertexFormat.PCTTBN>
    {
        public GFXVolatileSharedVertexBufferPCTTBN(int count)
            : base(count)
        {
        }

        protected override void _CreateBuffer()
        {
            GFXDevice.Instance.ReserveVolatileBufferPCTTBN(_count, this);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }



    /// <summary>
    /// User static vertex buffer – supports user defined vertex formats.
    /// 
    /// Capabilities:
    /// -can only be filled once
    /// -can render multiple times
    /// 
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public class GFXStaticVertexBuffer<TGFXFormat> : GFXVertexBuffer<TGFXFormat> where TGFXFormat : struct
    {
        public GFXStaticVertexBuffer(int count)
            : base(count, GFXBufferResourceProfiles.ManualStaticWriteOnlyVBProfile)
        {
        }
    }



    /// <summary>
    /// User dynamic vertex buffer - supports user defined vertex formats.
    /// 
    /// Capabilities:
    /// -can be filled multiple times
    /// -can render multiple times
    /// 
    /// </summary>
    /// <typeparam name="TGFXFormat">user defined vertex format</typeparam>
    public class GFXDynamicVertexBuffer<TGFXFormat> : GFXVertexBuffer<TGFXFormat> where TGFXFormat : struct
    {
        public GFXDynamicVertexBuffer(int count)
            : base(count, GFXBufferResourceProfiles.ManualDynamicWriteOnlyVBProfile)
        {
        }
    }



    public class GFXBufferResourceProfiles
    {
        public static D3DVertexBufferProfile ManualStaticWriteOnlyVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
        public static D3DVertexBufferProfile ManualDynamicWriteOnlyVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
    };
}

