﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using XNAVERGE;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DemonDoor
{
    class Level1Screen : Screen
    {
        public McGrenderStack mcg;
        private World _world;
        private Gun _gun;


        internal override void Load()
        {
            Game1 game1 = (Game1)Game1.game;
            //Vector2[] verts = new [] {
            //    new Vector2 { X = -100, Y = 0 },
            //    new Vector2 { X = -70, Y = 0 },
            //    new Vector2 { X = -100, Y = 30 }
            //};

            //Wall _wallTri = new Wall(_world, verts);
            McgNode rendernode;
            Vector2 floor = Coords.Screen2Physics(new Vector2 { X = 0, Y = 220 });
            _world = new World(new Vector2 { X = 0, Y = -10 }, floor.Y);

            Vector2 _666pos = Coords.Screen2Physics(new Vector2 { X = 300, Y = 218 });
            Vector2 _666size = Coords.Screen2Physics(new Vector2 { X = 100, Y = 210 }, true);

            Wall _wall0 = new Wall(_world, -100, 1);
            Wall _wall1 = new Wall(_world, _666pos.X, -1);

            SpriteBasis civSpriteBasis = new SpriteBasis(16, 16, 7, 7);
            civSpriteBasis.image = game1.im_civvie;

            mcg = new McGrenderStack();
            Game1.game.setMcGrender(mcg);

            mcg.AddLayer( "skybox" );
            mcg.AddLayer( "clouds" );
            mcg.AddLayer( "background" );
            mcg.AddLayer("corpses");

            McgLayer l = mcg.GetLayer( "skybox" );
            /// this is wrong.
            Rectangle rectTitle = new Rectangle( 0, 0, 320, 240 );
            rendernode = l.AddNode(
                new McgNode( game1.im_skybox, rectTitle, l, 0, 0 )
            );

            l = mcg.GetLayer("background");
            rendernode = l.AddNode(
                new McgNode(game1.im_stage, rectTitle, l, 0, 0)
            );

            l = mcg.GetLayer( "clouds" );

            for( int i = 0; i < 40; i++ ) {
                int x = VERGEGame.rand.Next( -400, 350 );
                int y = VERGEGame.rand.Next(0,150);
                int d = VERGEGame.rand.Next(50000,200000);
                rendernode = l.AddNode(
                    new McgNode( game1.im_clouds[i%9], null, l, x,y ,600,y,d )
                );
            }
            /// this all should be encapsulated eventually.  CORPSEMAKER.
            l = mcg.GetLayer("corpses");

            var doorSpriteBasis = new SpriteBasis(38, 24, 5, 5);
            doorSpriteBasis.image = game1.im_door;
            var doorSprite = new DoorSprite(doorSpriteBasis);
            _gun = new Gun(_world,
                            Coords.Screen2Physics(new Vector2 { X = 32, Y = 206 }),
                            Coords.Screen2Physics(new Vector2 { X = 19, Y = 12 }, true),
                            doorSprite);
            _gun.Impulse = new Vector2 { X = -10, Y = 10 };

            rendernode = l.AddNode(
                new McgNode(_gun, l, 60, 200)
            );

            for (int i = 0; i < 20; i++)
            {
                var civvieSprite = new CivvieSprite(civSpriteBasis);

                Sprite sprite = new Sprite(civSpriteBasis, new Filmstrip(new Point(16, 16), new[] { 1, 2, 3, 4, 5 }, 100));
                CivvieController myCorpse = new CivvieController(
                    _world,
                    new Vector2 { X = 0, Y = 20 },
                    civvieSprite
                );

                civvieSprite.SetAnimationState(CivvieSprite.AnimationState.WalkingLeft);

                rendernode = l.AddNode(
                    new McgNode(myCorpse, l, Game1.rand.Next(0, 310), Game1.rand.Next(0, 50))
                );
            }
            
            
        }

        private const float MaxGunImpulse = 2000;
        private const float MinGunImpulse = 0;
        private const float GunImpulseKick = 1000;
        private const float GunImpulseDecayTime = 4;

        private float GunImpulse { get; set; }
        private TimeSpan _gunLastGameTime = TimeSpan.Zero;
        private bool _gunLatch = false;

        private void UpdateGunImpulse(GameTime gameTime)
        {
            // check gun key, kick if newly pressed
            {
                bool revGun = Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.E);

                if (revGun && !_gunLatch)
                    GunImpulse += GunImpulseKick;
            }

            // apply a bit of decay
            {
                float decayPerSecond = MaxGunImpulse / GunImpulseDecayTime;
                GunImpulse -= (float)(gameTime.TotalGameTime - _gunLastGameTime).TotalSeconds * decayPerSecond;
                _gunLastGameTime = gameTime.TotalGameTime;
            }

            // and limit to range
            GunImpulse = Math.Max(MinGunImpulse, GunImpulse);
            GunImpulse = Math.Min(MaxGunImpulse, GunImpulse);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        internal override void Update(GameTime gameTime)
        {
            {
                // update gun impulse.
                UpdateGunImpulse(gameTime);

                Vector2 dir = new Vector2 { X = 1, Y = 1 };
                dir.Normalize();

                _gun.Impulse = dir * GunImpulse;
            }

            _world.Simulate(gameTime);
            mcg.Update(gameTime);

            //Console.Out.WriteLine("@{3}: ({0}, {1}), {2}", _test.Position.X, _test.Position.Y, _test.Theta, gameTime.TotalGameTime);
        }

        private string DoorSpeedDescription
        {
            get
            {
                if (GunImpulse < 0.1 * MaxGunImpulse)
                {
                    return "mild";
                }
                else if (GunImpulse < 0.2 * MaxGunImpulse)
                {
                    return "moderate";
                }
                else if (GunImpulse < 0.3 * MaxGunImpulse)
                {
                    return "a little much";
                }
                else if (GunImpulse < 0.4 * MaxGunImpulse)
                {
                    return "way too much";
                }
                else if (GunImpulse < 0.5 * MaxGunImpulse)
                {
                    return "worrisome";
                }
                else if (GunImpulse < 0.6 * MaxGunImpulse)
                {
                    return "crazy";
                }
                else if (GunImpulse < 0.7 * MaxGunImpulse)
                {
                    return "warranty-voiding";
                }
                else if (GunImpulse < 0.8 * MaxGunImpulse)
                {
                    return "¡picante!";
                }
                else
                {
                    return "¡muy picante!";
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        internal override void Draw(SpriteBatch batch, GameTime gameTime)
        {
            Game1 game1 = (Game1)Game1.game;
            string doorSpeedDesc = string.Format("door speed: {0}", DoorSpeedDescription);
            Vector2 size = game1.ft_hud24.MeasureString(doorSpeedDesc);

            batch.DrawString(game1.ft_hud24, doorSpeedDesc, new Vector2 { X = (640 - size.X) / 2, Y = 10 }, Color.White);
        }
    }
}