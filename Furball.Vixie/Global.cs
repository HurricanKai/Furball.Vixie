using System;
using System.Collections.Generic;
using Furball.Vixie.Backends.OpenGLES;
using Furball.Vixie.Backends.Shared.Backends;
using Silk.NET.Windowing;

namespace Furball.Vixie {
    internal static class Global {
        internal static bool                             AlreadyInitialized;
        internal static Game                             GameInstance;
        internal static WindowManager                    WindowManager;
        
        public static Dictionary<string, FeatureLevel> GetFeatureLevels(Backend backend) {
            return backend switch {
                Backend.None         => new Dictionary<string, FeatureLevel>(),
                Backend.Direct3D11   => new Dictionary<string, FeatureLevel>(),
                Backend.LegacyOpenGL => new Dictionary<string, FeatureLevel>(),
                Backend.ModernOpenGL => new Dictionary<string, FeatureLevel>(),
                Backend.OpenGLES     => OpenGLESBackend.FeatureLevels,
                Backend.Veldrid      => new Dictionary<string, FeatureLevel>(),
                _                    => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
            };
        }
    }
}
