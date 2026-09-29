// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// A vertex or pixel shader on the console.
    ///
    /// The GPU runs vs.1.1 and ps.1.1. The bytecode in an .mgfxo built for this platform is the
    /// shader's assembly source rather than microcode: only the console has an assembler for the
    /// NV2A, so the translation cannot happen on the build machine, and the one in the title's own
    /// XGraphics runs it here instead. That is also why there is no HLSL step — see the generator
    /// in tools/RxdkEffectBuilder for where the stock effects' assembly comes from.
    ///
    /// A vertex shader also needs a vertex declaration describing the stream it reads, which
    /// Direct3D 8 wants at creation time rather than at draw time. The declaration comes from the
    /// vertex buffer bound when the shader is first applied, so creation is deferred until then.
    /// </summary>
    internal partial class Shader
    {
        byte[] _source;
        byte[] _microcode;
        int _handle;
        bool _created;

        /// <summary>
        /// Shader model 1. MonoGame compares this against the profile recorded in the .mgfxo so a
        /// shader built for another backend is rejected rather than fed to the GPU.
        /// </summary>
        private static int PlatformProfile()
        {
            return 2;
        }

        private void PlatformConstruct(ShaderStage stage, byte[] shaderBytecode)
        {
            if (shaderBytecode == null || shaderBytecode.Length == 0)
                throw new InvalidOperationException(
                    "The shader is empty. Build the effect with the Xbox profile.");

            _source = shaderBytecode;
        }

        /// <summary>
        /// Assembles the source the first time the shader is used, rather than in the constructor,
        /// so an effect that is loaded but never drawn with costs nothing.
        /// </summary>
        byte[] Microcode()
        {
            if (_microcode == null)
            {
                var assembled = Rxdk.XGraphics.AssembleShader(_source);
                if (assembled.Status != 0 || assembled.Shader == null || assembled.Shader.Length == 0)
                    throw new ShaderCompilerException(SourceFile, Entrypoint, Stage,
                        assembled.Errors, System.Text.Encoding.ASCII.GetString(_source));
                _microcode = assembled.Shader;
            }
            return _microcode;
        }

        internal void PlatformApply(GraphicsDevice device)
        {
            if (Stage == ShaderStage.Pixel)
            {
                if (!_created)
                {
                    _handle = XDevice.CreatePixelShader(Microcode());
                    _created = true;
                }
                XDevice.PixelShader = _handle;
                return;
            }

            if (!_created)
            {
                var declaration = XboxVertexDeclaration.Build(device, Attributes);
                _handle = XDevice.CreateVertexShader(declaration, Microcode());
                _created = true;
            }
            XDevice.VertexShader = _handle;
        }

        private void PlatformGraphicsDeviceResetting()
        {
            // The console never loses its device, so a created shader stays valid.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _created)
            {
                if (Stage == ShaderStage.Pixel)
                    XDevice.DeletePixelShader(_handle);
                else
                    XDevice.DeleteVertexShader(_handle);
                _created = false;
            }
            base.Dispose(disposing);
        }
    }
}
