// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace MonoGame.Framework.Utilities
{
    partial class PlatformInfo
    {
        private static MonoGamePlatform PlatformGetMonoGamePlatform()
        {
            return MonoGamePlatform.Xbox;
        }

        /// <summary>
        /// Direct3D 8. GraphicsBackend does not distinguish versions, and every other value would
        /// be less true than this one.
        /// </summary>
        private static GraphicsBackend PlatformGetGraphicsBackend()
        {
            return GraphicsBackend.DirectX;
        }
    }
}
