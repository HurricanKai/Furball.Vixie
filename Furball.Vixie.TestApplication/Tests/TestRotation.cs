using System;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Furball.Vixie.Graphics;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Furball.Vixie.ImGuiHelpers;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Furball.Vixie.TestApplication.Tests {
    public class TestRotation : GameComponent {
        private InstancedRenderer _instancedRenderer;
        private BatchedRenderer   _batchedRenderer;
        private Texture           _whiteTexture;

        private ImGuiController _imGuiController;

        public override void Initialize() {
            this._instancedRenderer = new InstancedRenderer();
            this._batchedRenderer   = new BatchedRenderer();
            this._whiteTexture      = new Texture(ResourceHelpers.GetByteResource("Resources/pippidonclear0.png"));

            this._imGuiController = ImGuiCreator.CreateController();

            base.Initialize();
        }

        private float _rotation = 0f;

        public override void Draw(double deltaTime) {
            this.GraphicsDevice.GlClear();

            this._instancedRenderer.Begin();
            this._instancedRenderer.Draw(this._whiteTexture, new Vector2(0, 0), new Vector2(371, 356), Vector2.Zero, _rotation);
            this._instancedRenderer.End();



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