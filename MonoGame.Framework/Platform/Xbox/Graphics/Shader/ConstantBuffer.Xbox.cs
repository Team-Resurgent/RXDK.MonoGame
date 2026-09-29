// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Shader constants on the console.
    ///
    /// Direct3D 8 has no constant buffer object: constants are written straight into the shader's
    /// register file. So a "buffer" here is just the staging bytes the effect writes into, copied
    /// to registers when it is applied.
    ///
    /// An effect built for this platform lays its first constant buffer out so that byte offset 0
    /// is register c0 and every parameter sits at the register its shader reads, which is why the
    /// base register below is always zero. The slot is the buffer's position in the shader's own
    /// list, so a second buffer would have to start somewhere the file cannot tell us; effects for
    /// this platform use one per stage.
    /// </summary>
    internal partial class ConstantBuffer
    {
        float[] _registers;

        private void PlatformInitialize()
        {
            // Registers are four floats wide; a partial one still occupies a whole register.
            _registers = new float[(_buffer.Length + 15) / 16 * 4];
        }

        private void PlatformClear()
        {
            _dirty = true;
        }

        internal void PlatformApply(GraphicsDevice device, ShaderStage stage, int slot)
        {
            if (slot != 0)
                throw new NotSupportedException(
                    "An effect for this platform may only declare one constant buffer per shader "
                    + "stage, because the register file has no second base to place it at.");

            Buffer.BlockCopy(_buffer, 0, _registers, 0, _buffer.Length);

            const int baseRegister = 0;
            if (stage == ShaderStage.Vertex)
                XDevice.SetVertexShaderConstant(baseRegister, _registers);
            else
                XDevice.SetPixelShaderConstant(baseRegister, _registers);

            _dirty = false;
        }
    }
}
