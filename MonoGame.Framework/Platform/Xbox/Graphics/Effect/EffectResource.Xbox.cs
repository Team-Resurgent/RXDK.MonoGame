// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class EffectResource
    {
        /// <summary>
        /// The stock effects, embedded by scripts/build-monogame.sh from tools/mgfx-xbox.py. Each is
        /// an .mgfxo whose shaders are vs.1.1 and ps.1.1 assembly rather than DirectX or OpenGL
        /// bytecode; see Shader.Xbox.cs for why they are text.
        ///
        /// Only SpriteEffect and BasicEffect are built. The other four are named so that core's
        /// EffectResource still compiles, and constructing one of those effects reports the missing
        /// resource rather than failing somewhere less obvious.
        /// </summary>
        const string AlphaTestEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.AlphaTestEffect.xbox.mgfxo";
        const string BasicEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.BasicEffect.xbox.mgfxo";
        const string DualTextureEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.DualTextureEffect.xbox.mgfxo";
        const string EnvironmentMapEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.EnvironmentMapEffect.xbox.mgfxo";
        const string SkinnedEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.SkinnedEffect.xbox.mgfxo";
        const string SpriteEffectName = "Microsoft.Xna.Framework.Platform.Xbox.Graphics.Effect.Resources.SpriteEffect.xbox.mgfxo";
    }
}
