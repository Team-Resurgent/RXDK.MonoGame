// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.IO;

namespace Microsoft.Xna.Framework
{
    partial class TitleContainer
    {
        /// <summary>
        /// D:\ is the disc the title booted from, which is where the packer put Content. There is
        /// no notion of a current directory or a base directory to fall back on.
        /// </summary>
        static partial void PlatformInit()
        {
            Location = "D:\\";
        }

        private static Stream PlatformOpenStream(string safeName)
        {
            var absolutePath = Path.Combine(Location, safeName);
            return File.Exists(absolutePath) ? File.OpenRead(absolutePath) : null;
        }
    }
}
