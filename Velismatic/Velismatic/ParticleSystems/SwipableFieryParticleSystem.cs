using System;
using System.Collections.Generic;
using DPSF;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Velismatic.ParticleSystems
{

#if (WINDOWS)
    [Serializable]
#endif
    public class SwipableParticleSystem : DefaultTexturedQuadParticleSystem
    {
        private HashSet<DefaultTexturedQuadParticle> swipedParticles;

        public SwipableParticleSystem(Game cGame) : base(cGame) { }

        public override void AutoInitialize(GraphicsDevice cGraphicsDevice, ContentManager cContentManager, SpriteBatch cSpriteBatch)
        {
            InitializeTexturedQuadParticleSystem(cGraphicsDevice, cContentManager, 1000, 50000,
                                                UpdateVertexProperties, "Textures/Particle");
            Name = "Messabout Particle System to Experiment and Learn DPSF with";
            LoadParticleSystem();
        }

        public void LoadParticleSystem()
        {
            this.swipedParticles = new HashSet<DefaultTexturedQuadParticle>();

            ParticleInitializationFunction = InitializeParticleUsingInitialProperties;

            Emitter.ParticlesPerSecond = 100;
            Emitter.BurstTime = 1;
            Emitter.PositionData.Position = new Vector3(0, -20, 0);

            InitialProperties.LifetimeMin = 2.5f;
            InitialProperties.LifetimeMax = 3.0f;
            InitialProperties.PositionMin = Emitter.PositionData.Position;
            InitialProperties.PositionMax = Emitter.PositionData.Position;
            InitialProperties.VelocityMin = new Vector3(5, 5, 0);
            InitialProperties.VelocityMax = new Vector3(0, 75, 0);
            InitialProperties.RotationalVelocityMin = new Vector3(0, 0, -MathHelper.Pi);
            InitialProperties.RotationalVelocityMax = new Vector3(0, 0, MathHelper.Pi);
            InitialProperties.EndSizeMax = 35;
            InitialProperties.EndSizeMin = 30;
            InitialProperties.StartSizeMax = 50;
            InitialProperties.StartSizeMin = 45;
            InitialProperties.StartColorMin = Color.DarkRed;
            InitialProperties.StartColorMax = Color.OrangeRed;
            InitialProperties.EndColorMin = Color.Yellow;
            InitialProperties.EndColorMax = Color.Red;

            ParticleEvents.RemoveAllEvents();
            ParticleSystemEvents.RemoveAllEvents();

            ParticleEvents.AddEveryTimeEvent(UpdateParticlePositionUsingVelocity);
            ParticleEvents.AddEveryTimeEvent(UpdateParticleRotationUsingRotationalVelocity);
            ParticleEvents.AddEveryTimeEvent(UpdateParticleWidthAndHeightUsingLerp);
            ParticleEvents.AddEveryTimeEvent(UpdateParticleColorUsingLerp);
            ParticleEvents.AddNormalizedTimedEvent(1, UpdateParticleCleanupAtEndOfLifecycle);
            ParticleEvents.AddEveryTimeEvent(UpdateParticleTransparencyToFadeOutUsingLerp, 100);

            ParticleEvents.AddEveryTimeEvent(UpdateParticleToFaceTheCamera, 200);

            ParticleSystemEvents.LifetimeData.EndOfLifeOption = CParticleSystemEvents.EParticleSystemEndOfLifeOptions.Repeat;
            ParticleSystemEvents.LifetimeData.Lifetime = 1.0f;
            ParticleSystemEvents.AddTimedEvent(0.0f, UpdateParticleSystemEmitParticlesAutomaticallyOn);
            ParticleSystemEvents.AddTimedEvent(0.5f, UpdateParticleSystemEmitParticlesAutomaticallyOff);
        }

        public void InitializeParticleProperties(DefaultTexturedQuadParticle cParticle)
        {
            cParticle.Lifetime = 2.0f;
            cParticle.Position = Emitter.PositionData.Position;
            Vector3 sVelocityMin = new Vector3(-50, 50, -50);
            Vector3 sVelocityMax = new Vector3(50, 100, 50);
            cParticle.Velocity = DPSFHelper.RandomVectorBetweenTwoVectors(sVelocityMin, sVelocityMax);
            cParticle.Velocity = Vector3.Transform(cParticle.Velocity, Emitter.OrientationData.Orientation);
            cParticle.Width = cParticle.StartWidth = cParticle.EndWidth =
            cParticle.Height = cParticle.StartHeight = cParticle.EndHeight = RandomNumber.Next(10, 50);
            cParticle.Color = cParticle.StartColor = cParticle.EndColor = DPSFHelper.RandomColor();
        }

        public void SwipeParticle(DefaultTexturedQuadParticle particle, float elapsedTimeInSecconds)
        {
            if (!this.swipedParticles.Contains(particle))
            {
                this.swipedParticles.Add(particle);
                particle.Velocity = particle.Velocity + new Vector3((particle.ElapsedTime * 1.5f / particle.Lifetime) * 5 * this.LastSwipeDeltaX, 0, 0);
            }
        }

        private void UpdateParticleCleanupAtEndOfLifecycle(DefaultTexturedQuadParticle particle, float elapsedTimeInSeconds)
        {
            this.swipedParticles.Remove(particle);
        }

        public float LastSwipeDeltaX { get; set; }
    }
}
