// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using MonoGame.Framework.Utilities;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDevice
    {
        /// <summary>
        /// The console has one device, created once and never lost: there is no other application
        /// to take it away and no window to resize. So the backend does not implement device reset,
        /// and PresentationParameters describes the mode the runtime negotiated rather than one the
        /// game asked for.
        /// </summary>
        static bool _deviceOpen;

        // User-primitive scratch buffers, the same trick the DirectX backend uses: SpriteBatch and
        // DrawUserPrimitives hand over managed arrays every frame, which have to land in a real
        // vertex buffer before the GPU can see them.
        readonly Dictionary<VertexDeclaration, DynamicVertexBuffer> _userVertexBuffers =
            new Dictionary<VertexDeclaration, DynamicVertexBuffer>();
        DynamicIndexBuffer _userIndexBuffer16;
        DynamicIndexBuffer _userIndexBuffer32;

        int _vertexBufferSlotsUsed;

        private void PlatformSetup()
        {
            MaxTextureSlots = 4;
            MaxVertexTextureSlots = 0;
            // One vertex stream, which a shader declaration reads all of its inputs from. Leaving
            // this at zero gives the device nowhere to record a bound buffer, and every draw fails.
            _maxVertexBufferSlots = 1;

            if (!_deviceOpen)
            {
                if (!XDevice.Open())
                    throw new InvalidOperationException(
                        "Could not open the Xbox display device (status " + XDevice.LastStatus + ").");
                _deviceOpen = true;
            }
        }

        private void PlatformInitialize()
        {
            // The mode is whatever the console settled on, not what was requested.
            PresentationParameters.BackBufferWidth = XDevice.Width;
            PresentationParameters.BackBufferHeight = XDevice.Height;
            PresentationParameters.BackBufferFormat = SurfaceFormat.Color;

            _viewport = new Viewport(0, 0, XDevice.Width, XDevice.Height);
            ApplyRenderTargets(null);
        }

        internal void OnPresentationChanged()
        {
            // Nothing to reset: the mode cannot change while the title runs.
        }

        /// <summary>The console does not multisample the back buffer.</summary>
        internal int PlatformGetMaxMultiSampleCount(SurfaceFormat format)
        {
            return 0;
        }

        private void PlatformClear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            Rxdk.ClearFlags flags = 0;
            if ((options & ClearOptions.Target) != 0)
                flags |= Rxdk.ClearFlags.Target;
            if ((options & ClearOptions.DepthBuffer) != 0)
                flags |= Rxdk.ClearFlags.Depth;
            if ((options & ClearOptions.Stencil) != 0)
                flags |= Rxdk.ClearFlags.Stencil;
            if (flags == 0)
                return;

            var packed = XboxFormat.PackColor(color);
            XDevice.Clear(flags, (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, depth, stencil);
        }

        private void PlatformDispose()
        {
            foreach (var buffer in _userVertexBuffers.Values)
                buffer.Dispose();
            _userVertexBuffers.Clear();

            if (_userIndexBuffer16 != null)
                _userIndexBuffer16.Dispose();
            if (_userIndexBuffer32 != null)
                _userIndexBuffer32.Dispose();
            _userIndexBuffer16 = null;
            _userIndexBuffer32 = null;
        }

        private void PlatformPresent()
        {
            XDevice.Present();
        }

        private void PlatformSetViewport(ref Viewport value)
        {
            var viewport = new Rxdk.Viewport();
            viewport.X = (uint)value.X;
            viewport.Y = (uint)value.Y;
            viewport.Width = (uint)value.Width;
            viewport.Height = (uint)value.Height;
            viewport.MinZ = value.MinDepth;
            viewport.MaxZ = value.MaxDepth;
            XDevice.Viewport = viewport;
        }

        private void PlatformApplyDefaultRenderTarget()
        {
            XboxRenderTargets.BindBackBuffer();
        }

        internal void PlatformResolveRenderTargets()
        {
            if (_currentRenderTargetCount == 0)
                return;

            // The GPU is a push buffer behind, so the texture is not readable until it catches up.
            XDevice.BlockUntilIdle();
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            // Only one colour target: the console has a single render target slot.
            var binding = _currentRenderTargetBindings[0];
            var target = (IRenderTarget)binding.RenderTarget;
            XboxRenderTargets.Bind(target);
            return target;
        }

        internal void PlatformBeginApplyState()
        {
        }

        private void PlatformApplyBlend()
        {
            if (!_blendStateDirty && !_blendFactorDirty)
                return;

            _actualBlendState.PlatformApplyState(this);
            _blendStateDirty = false;
            _blendFactorDirty = false;
        }

        internal void PlatformApplyState(bool applyShaders)
        {
            if (!applyShaders)
                return;

            if (_indexBufferDirty)
            {
                XDevice.SetIndexBuffer(_indexBuffer == null ? null : _indexBuffer.Buffer);
                _indexBufferDirty = false;
            }

            if (_vertexBuffersDirty)
            {
                if (_vertexBuffers.Count > 0)
                {
                    // Direct3D 8 on the console has one vertex stream that a shader declaration
                    // reads from, so only slot zero is bound and extra slots are a programming
                    // error rather than something to silently drop.
                    if (_vertexBuffers.Count > 1)
                        throw new NotSupportedException(
                            "The Xbox backend binds a single vertex buffer; multiple streams are not available.");

                    var binding = _vertexBuffers.Get(0);
                    var declaration = binding.VertexBuffer.VertexDeclaration;
                    XDevice.SetVertexBuffer(binding.VertexBuffer.Buffer, declaration.VertexStride);
                    _vertexBufferSlotsUsed = 1;
                }
                else if (_vertexBufferSlotsUsed > 0)
                {
                    XDevice.SetVertexBuffer(null, 0);
                    _vertexBufferSlotsUsed = 0;
                }
                _vertexBuffersDirty = false;
            }

            if (_vertexShader == null)
                throw new InvalidOperationException("A vertex shader must be set!");
            if (_pixelShader == null)
                throw new InvalidOperationException("A pixel shader must be set!");

            if (_vertexShaderDirty)
            {
                _vertexShader.PlatformApply(this);
                _vertexShaderDirty = false;
            }

            if (_pixelShaderDirty)
            {
                _pixelShader.PlatformApply(this);
                _pixelShaderDirty = false;
            }

            _vertexConstantBuffers.SetConstantBuffers(this);
            _pixelConstantBuffers.SetConstantBuffers(this);

            Textures.SetTextures(this);
            SamplerStates.PlatformSetSamplers(this);
        }

        /// <summary>
        /// Copies user vertices into a dynamic buffer and returns the vertex to start drawing from.
        /// Appends until the buffer is full, then wraps, so a frame of SpriteBatch calls costs one
        /// buffer rather than one per call.
        /// </summary>
        private int SetUserVertexBuffer<T>(T[] vertexData, int vertexOffset, int vertexCount, VertexDeclaration vertexDecl)
            where T : struct
        {
            DynamicVertexBuffer buffer;
            if (!_userVertexBuffers.TryGetValue(vertexDecl, out buffer) || buffer.VertexCount < vertexCount)
            {
                if (buffer != null)
                    buffer.Dispose();

                buffer = new DynamicVertexBuffer(this, vertexDecl, Math.Max(vertexCount, 2000), BufferUsage.WriteOnly);
                _userVertexBuffers[vertexDecl] = buffer;
            }

            var startVertex = buffer.UserOffset;
            if (vertexCount + buffer.UserOffset < buffer.VertexCount)
            {
                buffer.UserOffset += vertexCount;
                buffer.SetData(startVertex * vertexDecl.VertexStride, vertexData, vertexOffset, vertexCount,
                    vertexDecl.VertexStride, SetDataOptions.NoOverwrite);
            }
            else
            {
                buffer.UserOffset = vertexCount;
                buffer.SetData(vertexData, vertexOffset, vertexCount, SetDataOptions.Discard);
                startVertex = 0;
            }

            SetVertexBuffer(buffer);
            return startVertex;
        }

        private int SetUserIndexBuffer<T>(T[] indexData, int indexOffset, int indexCount)
            where T : struct
        {
            var indexSize = ReflectionHelpers.SizeOf<T>.Get();
            var indexElementSize = indexSize == 2 ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
            var requiredIndexCount = Math.Max(indexCount, 6000);

            DynamicIndexBuffer buffer;
            if (indexElementSize == IndexElementSize.SixteenBits)
            {
                if (_userIndexBuffer16 == null || _userIndexBuffer16.IndexCount < requiredIndexCount)
                {
                    if (_userIndexBuffer16 != null)
                        _userIndexBuffer16.Dispose();
                    _userIndexBuffer16 = new DynamicIndexBuffer(this, indexElementSize, requiredIndexCount, BufferUsage.WriteOnly);
                }
                buffer = _userIndexBuffer16;
            }
            else
            {
                if (_userIndexBuffer32 == null || _userIndexBuffer32.IndexCount < requiredIndexCount)
                {
                    if (_userIndexBuffer32 != null)
                        _userIndexBuffer32.Dispose();
                    _userIndexBuffer32 = new DynamicIndexBuffer(this, indexElementSize, requiredIndexCount, BufferUsage.WriteOnly);
                }
                buffer = _userIndexBuffer32;
            }

            var startIndex = buffer.UserOffset;
            if (indexCount + buffer.UserOffset < buffer.IndexCount)
            {
                buffer.UserOffset += indexCount;
                buffer.SetData(startIndex * indexSize, indexData, indexOffset, indexCount, SetDataOptions.NoOverwrite);
            }
            else
            {
                startIndex = 0;
                buffer.UserOffset = indexCount;
                buffer.SetData(indexData, indexOffset, indexCount, SetDataOptions.Discard);
            }

            Indices = buffer;
            return startIndex;
        }

        private void PlatformDrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            ApplyState(true);
            XDevice.DrawIndexedPrimitive(XboxFormat.Primitive(primitiveType), baseVertex, startIndex, primitiveCount);
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            ApplyState(true);
            XDevice.DrawPrimitive(XboxFormat.Primitive(primitiveType), vertexStart,
                GetPrimitiveCount(primitiveType, vertexCount));
        }

        private void PlatformDrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset,
            VertexDeclaration vertexDeclaration, int vertexCount) where T : struct
        {
            var startVertex = SetUserVertexBuffer(vertexData, vertexOffset, vertexCount, vertexDeclaration);
            ApplyState(true);
            XDevice.DrawPrimitive(XboxFormat.Primitive(primitiveType), startVertex,
                GetPrimitiveCount(primitiveType, vertexCount));
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset,
            int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
            where T : struct
        {
            var startVertex = SetUserVertexBuffer(vertexData, vertexOffset, numVertices, vertexDeclaration);
            var startIndex = SetUserIndexBuffer(indexData, indexOffset, GetElementCountArray(primitiveType, primitiveCount));
            PlatformDrawIndexedPrimitives(primitiveType, startVertex, startIndex, primitiveCount);
        }

        private void PlatformDrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset,
            int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
            where T : struct
        {
            var startVertex = SetUserVertexBuffer(vertexData, vertexOffset, numVertices, vertexDeclaration);
            var startIndex = SetUserIndexBuffer(indexData, indexOffset, GetElementCountArray(primitiveType, primitiveCount));
            PlatformDrawIndexedPrimitives(primitiveType, startVertex, startIndex, primitiveCount);
        }

        private void PlatformDrawInstancedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex,
            int primitiveCount, int baseInstance, int instanceCount)
        {
            throw new NotSupportedException(
                "The Xbox GPU has no instancing; draw the instances individually.");
        }

        private void PlatformGetBackBufferData<T>(Rectangle? rect, T[] data, int startIndex, int count) where T : struct
        {
            throw new NotSupportedException(
                "Reading the Xbox back buffer is not implemented; render to a RenderTarget2D and read that instead.");
        }

        /// <summary>
        /// Televisions overscan, so a title must keep anything the player has to read inside the
        /// inner 90%. XNA's Xbox 360 profile used the same margin.
        /// </summary>
        private static Rectangle PlatformGetTitleSafeArea(int x, int y, int width, int height)
        {
            var marginX = (int)(width * 0.05f);
            var marginY = (int)(height * 0.05f);
            return new Rectangle(x + marginX, y + marginY, width - marginX * 2, height - marginY * 2);
        }

        /// <summary>
        /// The layout of the vertex buffer on slot zero, which a vertex shader needs at creation
        /// time because Direct3D 8 binds a shader to the stream format it reads.
        /// </summary>
        internal VertexDeclaration GetBoundVertexDeclaration()
        {
            if (_vertexBuffers.Count == 0)
                return null;
            return _vertexBuffers.Get(0).VertexBuffer.VertexDeclaration;
        }

        /// <summary>Vertices to primitives, the inverse of GetElementCountArray.</summary>
        static int GetPrimitiveCount(PrimitiveType primitiveType, int vertexCount)
        {
            switch (primitiveType)
            {
                case PrimitiveType.TriangleList: return vertexCount / 3;
                case PrimitiveType.TriangleStrip: return vertexCount - 2;
                case PrimitiveType.LineList: return vertexCount / 2;
                case PrimitiveType.LineStrip: return vertexCount - 1;
                case PrimitiveType.PointList: return vertexCount;
                default: throw new NotSupportedException("Unsupported PrimitiveType." + primitiveType);
            }
        }
    }
}
