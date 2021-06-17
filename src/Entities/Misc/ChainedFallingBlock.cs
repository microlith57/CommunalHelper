﻿using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/ChainedFallingBlock")]
    class ChainedFallingBlock : Solid {

        private char tileType;
        private TileGrid tiles;

        private bool triggered;
        private bool climbFall;
        private bool held;
        private bool chainShattered;

        private float chainStopY, startY;
        private bool centeredChain;
        private bool chainOutline;

        private bool indicator;
        private float pathLerp;

        private EventInstance rattle;

        private static MTexture chain, chainEnd;

        public ChainedFallingBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'), data.Bool("climbFall", true), data.Bool("behind"), data.Int("fallDistance"), data.Bool("centeredChain"), data.Bool("chainOutline", true), data.Bool("trajectoryIndicator")) { }

        public ChainedFallingBlock(Vector2 position, int width, int height, char tileType, bool climbFall, bool behind, int maxFallDistance, bool centeredChain, bool chainOutline, bool indicator)
            : base(position, width, height, safe: false) {
            this.climbFall = climbFall;
            this.tileType = tileType;

            startY = Y;
            chainStopY = startY + maxFallDistance;
            this.centeredChain = centeredChain || Width <= 8;
            this.chainOutline = chainOutline;
            this.indicator = indicator;

            Calc.PushRandom(Calc.Random.Next());
            Add(tiles = GFX.FGAutotiler.GenerateBox(tileType, width / 8, height / 8).TileGrid);
            Calc.PopRandom();

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            Add(new TileInterceptor(tiles, highPriority: false));

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            if (behind)
                Depth = Depths.SolidsBelow;
        }

        public override void OnShake(Vector2 amount) {
            base.OnShake(amount);
            tiles.Position += amount;
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            triggered = true;
        }

        private bool PlayerFallCheck() {
            if (climbFall) {
                return HasPlayerRider();
            }
            return HasPlayerOnTop();
        }

        private bool PlayerWaitCheck() {
            if (triggered)
                return true;

            if (PlayerFallCheck())
                return true;

            if (climbFall) {
                if (!CollideCheck<Player>(Position - Vector2.UnitX))
                    return CollideCheck<Player>(Position + Vector2.UnitX);
                return true;
            }
            return false;
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(rattle);
        }

        private IEnumerator Sequence() {
            while (!triggered && !PlayerFallCheck()) {
                yield return null;
            }
            triggered = true;

            Vector2 rattleSoundPos = new Vector2(Center.X, startY);
            while (true) {
                ShakeSfx();
                StartShaking();
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                yield return 0.2f;

                float timer = 0.4f;
                while (timer > 0f && PlayerWaitCheck()) {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }

                StopShaking();
                FallParticles();

                rattle = Audio.Play(CustomSFX.game_chainedFallingBlock_chain_rattle, rattleSoundPos);

                float speed = 0f;
                //float maxSpeed = 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, 160f, 500f * Engine.DeltaTime);
                    if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true)) {
                        held = Y == chainStopY;
                        break;
                    } else if (Y > chainStopY && !chainShattered) {
                        held = true;
                        MoveToY(chainStopY, LiftSpeed.Y);
                        break;
                    }
                    Audio.Position(rattle, rattleSoundPos);
                    yield return null;
                }

                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();
                Audio.Stop(rattle);
                if (held) {
                    Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, TopCenter);
                    Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_ceiling, rattleSoundPos);
                }
                yield return 0.2f;

                StopShaking();
                if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
                    break;
                }

                while (held) {
                    yield return null;
                }

                while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
                    yield return 0.1f;
                }
            }
            Safe = true;
        }

        private void LandParticles() {
            for (int i = 2; i <= Width; i += 4) {
                if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                    float direction = (!(i < Width / 2f)) ? 0f : ((float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, direction);;
                }
            }
        }

        private void FallParticles() {
            for (int i = 2; i < Width; i += 4) {
                if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, (float) Math.PI / 2f);
                }
                SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
            }
        }

        private void ShakeSfx() {
            Audio.Play(tileType switch {
                '3' => SFX.game_01_fallingblock_ice_shake,
                '9' => SFX.game_03_fallingblock_wood_shake,
                'g' => SFX.game_06_fallingblock_boss_shake,
                _ => SFX.game_gen_fallblock_shake,
            }, Center);
        }

        private void ImpactSfx() {
            // Some impacts weren't as attenuated like the game_gen_fallblock_impact event,
            // and it was inconsistent with the fact that you can hear the chain tighten but not the block impact.
            // So custom impact sounds for all specific variants with matching distance attenuation effects were added.
            Audio.Play(tileType switch {
                '3' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_ice_impact,
                '9' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_wood_impact,
                'g' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_boss_impact,
                _ => SFX.game_gen_fallblock_impact,
            }, Center);
        }

        private void RenderChain(float x) {
            Vector2 top = new Vector2(x, startY);

            if (chainOutline) {
                for (float y = Y - 6f; y > startY - 8f; y -= 8f) {
                    chain.DrawOutlineOnly(new Vector2(x, y));
                }
                chainEnd.DrawOutlineOnly(top);
            }

            for (float y = Y - 6f; y > startY - 8f; y -= 8f) {
                chain.Draw(new Vector2(x, y));
            }
            chainEnd.Draw(top);
        }

        public override void Update() {
            base.Update();

            if (triggered && indicator)
                pathLerp = Calc.Approach(pathLerp, 1f, Engine.DeltaTime * 2f);
        }

        public override void Render() {
            if (triggered && indicator && !held && !chainShattered) {
                float toY = startY + (chainStopY + Height - startY) * Ease.ExpoOut(pathLerp);
                Draw.Rect(X, Y, Width, toY - Y, Color.Black * 0.75f);
            }

            if (centeredChain) {
                RenderChain(Center.X - 4f);
            } else {
                RenderChain(X);
                RenderChain(X + Width - 8f);
            }

            base.Render();
        }

        public static void InitializeTextures() {
            MTexture texture = GFX.Game["objects/hanginglamp"];
            chainEnd = texture.GetSubtexture(0, 0, 8, 8);
            chain = texture.GetSubtexture(0, 8, 8, 8);
        }
    }
}
