using Furball.Vixie;
using Furball.Vixie.Backends.Shared.Backends;
using Furball.Vixie.Backends.Veldrid;
using Furball.Vixie.TestApplication;
using Silk.NET.Windowing;

var options = WindowOptions.DefaultVulkan;

options.VSync = false;

// GraphicsBackend.PrefferedBackends = Backend.OpenGL41 | Backend.OpenGLES32;

// VeldridBackend.PrefferedBackend = Veldrid.GraphicsBackend.Vulkan;

new TestGame(options).Run(options, Backend.Vulkan);