using System.Linq;
using DPSF;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNI;
using Velismatic.OpenNI;
using Velismatic.ParticleSystems;
using System;

namespace Velismatic
{
    public class Visualizer : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont arialFont;
        ParticleSystemManager particleManager = new ParticleSystemManager();
        SwipableParticleSystem particleSystem = null;

        Vector3 cameraPosition = new Vector3(0, 0, -200);
        UserTracker userTracker;

        float? lastX = null;

        public Visualizer()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            this.userTracker = new UserTracker();
            this.userTracker.UpdateInBackground();
            this.userTracker.JointsUpdatedCallback = this.DetectRightHandSwipe;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(GraphicsDevice);
            this.arialFont = Content.Load<SpriteFont>("Fonts/Arial");
            this.particleSystem = new SwipableParticleSystem(this);
            this.particleSystem.AutoInitialize(this.GraphicsDevice, this.Content, null);
            this.particleManager.AddParticleSystem(this.particleSystem);
            Matrix sViewMatrix = Matrix.CreateLookAt(this.cameraPosition, new Vector3(0, 0, 0), Vector3.Up);
            Matrix sProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height, 1, 10000);
            this.particleSystem.SetWorldViewProjectionMatrices(Matrix.Identity, sViewMatrix, sProjectionMatrix);
            this.particleSystem.SetCameraPosition(this.cameraPosition);
        }

        protected override void UnloadContent()
        {
            this.particleManager.DestroyAllParticleSystems();
            this.userTracker.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                this.Exit();
            }
            this.particleSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        private void DetectRightHandSwipe()
        {
            float? currentX = null;
            if (this.userTracker.Joints.Keys.Count > 0)
            {
                var userJoints = this.userTracker.Joints.First().Value;
                if (userJoints.ContainsKey(SkeletonJoint.RightHand))
                {
                    currentX = userJoints[SkeletonJoint.RightHand].Position.X;
                }
            }
            var swiped = false;
            if (lastX != null && currentX != null)
            {
                float deltaX = currentX.Value - lastX.Value;
                if (Math.Abs(deltaX) > 30)
                {
                    this.particleSystem.LastSwipeDeltaX = -1 * deltaX;
                    this.particleSystem.ParticleEvents.AddOneTimeEvent(this.particleSystem.SwipeParticle);
                    swiped = true;
                }
            }
            lastX = swiped ? null : currentX;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            this.particleManager.DrawAllParticleSystems();
            this.DrawText();
            base.Draw(gameTime);
        }

        private void DrawText()
        {
            this.spriteBatch.Begin();
            this.spriteBatch.DrawString(this.arialFont, String.Format("Users: {0}", this.userTracker.Joints.Keys.Count), new Vector2(2, 2), Color.White);
            this.spriteBatch.DrawString(this.arialFont, String.Format("Right Hand: {0}", this.IsRightHandDetected), new Vector2(2, 22), Color.White);
            this.spriteBatch.End();
        }

        private bool IsRightHandDetected
        {
            get
            {
                var result = false;
                if (this.userTracker.Joints.Keys.Count > 0)
                {
                    if (this.userTracker.Joints.First().Value.ContainsKey(SkeletonJoint.RightHand))
                    {
                        result = true;
                    }
                }
                return result;
            }
        }
    }
}
