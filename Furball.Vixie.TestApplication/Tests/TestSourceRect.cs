using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Graphics.Renderers.OpenGL;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestSourceRect : GameComponent {
        private ImmediateRenderer _immediateRenderer;
        private BatchedRenderer   _batchedRenderer;
        private Texture           _whiteTexture;

        private ImGuiController _imGuiController;

        public override void Initialize() {
            this._immediateRenderer = new ImmediateRenderer();
            this._batchedRenderer   = new BatchedRenderer();
            this._whiteTexture      = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private float _rotation = 1f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._immediateRenderer.Begin();
            this._immediateRenderer.Draw(this._whiteTexture, new Vector2(1280 /2, 720 /2), new Vector2(371/2, 326), null, 0, Color.White, new Rectangle(371/2, 0, 371, 326));
            this._immediateRenderer.End();



            #region ImGui menu

            this._imGuiController.Update((float) deltaTime);

            ImGui.Text($"Frametime: {Math.Round(1000.0f / ImGui.GetIO().Framerate, 2).ToString(CultureInfo.InvariantCulture)} " +
                       $"Framerate: {Math.Round(ImGui.GetIO().Framerate,           2).ToString(CultureInfo.InvariantCulture)}"
            );

            ImGui.DragFloat("Rotation", ref this._rotation, 0.01f, 0f, 8f);

            if (ImGui.Button("Go back to test selector")) {
                this.BaseGame.Components.Add(new BaseTestSelector());
                this.BaseGame.Components.Remove(this);
            }

            this._imGuiController.Render();

            #endregion

            base.Draw(deltaTime);
        }
    }
}