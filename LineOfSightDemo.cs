/*©
 * Version: 1.0
 *     $Id$
 *
 * Revisions:
 *     $Log$
 * 
 */
#region Using Statements

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

#endregion

namespace AllanBishop.XNA
{
    /// <summary>
    /// 
    /// This class is the test driver of the LineOfSight class.
    /// Four world entities are created of various shapes. Each shape can
    /// be dragged and rotated. Additionaly any entity can be selected to be
    /// 'the entity to detect'. A field of view camera is created and can also
    /// be moved and rotated. If the entity to detect (detectable entity) is in 
    /// the field of view and can be seen then the screen backround will turn red,
    /// else the screen background will remain blue
    /// 
    /// I have tried to optimise this code hence have minimised the use of new()
    /// in the update and draw methods as well as minimise the use of any loops.
    /// 
    /// Also note that when running iTunes 7.1.1.5 and this code and using the keyboard
    /// a loader lock error is thrown. This is a iTunes/XNA issue, for the meantime I have
    /// disabled loader locks from being thrown but a pause will occur initially.
    /// <list type="bullet">
    /// 
    /// <item>
    /// <term>Author</term>
    /// <description>Allan Bishop</description>
    /// <blog>xna4noobs@blogspot.com</blog>
    /// </item>
    /// 
    /// </list>
    /// 
    /// </summary>
    public class LineOfSightDemo : Microsoft.Xna.Framework.Game
    {
        #region Class Members

        const float FOV_ANGLE_DEGREES = 60f;
        const int FOV_CAMERA_SPEED = 1;
        const float SAFE_AREA_PORTION = 0.05f;

        GraphicsDeviceManager _graphics;
        ContentManager _content;

        Vector2 _fovCameraPosition;
        Vector2 _fovCameraDirectionFacing;
        Vector2 _drawPosition;
        Vector2 _currentlySelectedEntityPosition;
        Vector2 _origin;
        Vector2 _fovCameraLeftAngleBoundary;
        Vector2 _fovCameraRightAngleBoundary;
        Vector2 _cursorOrigin;
        Vector2 _cursorPosition;

        Texture2D greenSquareTexture;
        Texture2D triangleTexture;
        Texture2D whiteSquareTexture;
        Texture2D concaveCircleTexture;
        Texture2D lineTexture;
        Texture2D cursorCrosshairTexture;

        WorldEntity _currentlySelectedEntity;
        WorldEntity _currentDetectableEntity;
        WorldEntity _greenSquareEntity;
        WorldEntity triangleEntity;
        WorldEntity _purpleSquareEntity;
        WorldEntity _concaveCircleEntity;

        WorldEntity[] _worldEntites;

        Rectangle _safeBounds;
       
        LineOfSight _lineOfSight;

        SpriteBatch _spriteBatch;

        float _fovAngleRadians;

        private float _RotationAngle = 0f;

        float _elapsed = 0.01f;

        bool _leftMouseBtnHeld = false;
        bool _rightMouseBtnHeld = false;

        #endregion

        #region Constructors

        public LineOfSightDemo()
        {
            _graphics = new GraphicsDeviceManager(this);
            _content = new ContentManager(Services);

            _graphics.PreferredBackBufferWidth = 853;
            _graphics.PreferredBackBufferHeight = 480;
        }

        #endregion


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            base.Initialize();

            // Calculate safe bounds based on current resolution
            Viewport viewport = _graphics.GraphicsDevice.Viewport;
            _safeBounds = new Rectangle(
                (int)(viewport.Width * SAFE_AREA_PORTION),
                (int)(viewport.Height * SAFE_AREA_PORTION),
                (int)(viewport.Width * (1 - 2 * SAFE_AREA_PORTION)),
                (int)(viewport.Height * (1 - 2 * SAFE_AREA_PORTION)));
            // Start the player in the center along the bottom of the screen
            _fovCameraPosition.X = (_safeBounds.Width) / 2;
            _fovCameraPosition.Y = _safeBounds.Height / 2;

            //Set the direction facing to 50 along the Y axis
            _fovCameraDirectionFacing.X = _fovCameraPosition.X;
            _fovCameraDirectionFacing.Y = _fovCameraPosition.Y + 50.0f;

            //Create an initial point for the boundary of the FOV
         
            _fovCameraLeftAngleBoundary.Y = _fovCameraDirectionFacing.Y+10000f;
            _fovCameraLeftAngleBoundary.X = _fovCameraDirectionFacing.X;

            _fovCameraRightAngleBoundary.Y = _fovCameraDirectionFacing.Y+10000f;
            _fovCameraRightAngleBoundary.X = _fovCameraDirectionFacing.X;

            _fovAngleRadians = MathHelper.ToRadians(FOV_ANGLE_DEGREES);

            _fovCameraRightAngleBoundary = RotatePointOnZAxis(_fovCameraRightAngleBoundary, _fovAngleRadians *0.5f);
            _fovCameraLeftAngleBoundary = RotatePointOnZAxis(_fovCameraLeftAngleBoundary, (0.0f - (_fovAngleRadians *0.5f)));

            _currentlySelectedEntityPosition = new Vector2();
            _cursorPosition = Vector2.Zero;

            _drawPosition = new Vector2();
            _origin = new Vector2();
        }


        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// 
        /// We load the textures here, therefore we also load the verticies of the 
        /// world entities, and create the entities before adding them to a container
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        /// 
        protected override void LoadGraphicsContent(bool loadAllContent)
        {
            if (loadAllContent)
            {
                // Load textures
                greenSquareTexture = _content.Load<Texture2D>("Content/GreenSquare");
                whiteSquareTexture = _content.Load<Texture2D>("Content/WhiteSquare");
                triangleTexture = _content.Load<Texture2D>("Content/Triangle");
                concaveCircleTexture = _content.Load<Texture2D>("Content/CircularConcave");
                lineTexture = _content.Load<Texture2D>("Content/Line");
                cursorCrosshairTexture = _content.Load<Texture2D>("Content/CursorCrosshair");

                _cursorOrigin = new Vector2(cursorCrosshairTexture.Width * 0.5f, cursorCrosshairTexture.Height * 0.5f);

                //Sets up the vertices of the entity based on the dimensions of each texture
                //vertex code generated by Shape Vertices tool
                Vector2[] squareVertices = new Vector2[4];
                squareVertices[0] = new Vector2(0.0f, 50.0f);
                squareVertices[1] = new Vector2(50.0f, 50.0f);
                squareVertices[2] = new Vector2(50.0f, 0.0f);
                squareVertices[3] = new Vector2(0.0f, 00.0f);

                _greenSquareEntity = new WorldEntity(greenSquareTexture, squareVertices);
                _greenSquareEntity.Position = new Vector2(500.0f, 300.0f);
                _greenSquareEntity.Centroid = new Vector2(25.0f, 25.0f);
                //By default the green square will be the entity to detect
                _greenSquareEntity.IsDetectable = true;

                Vector2[] triangleVertices = new Vector2[3];
                triangleVertices[0] = new Vector2(0.0f, 49.0f);
                triangleVertices[1] = new Vector2(23.0f, 3.0f);
                triangleVertices[2] = new Vector2(49.0f, 49.0f);

                triangleEntity = new WorldEntity(triangleTexture, triangleVertices);
                triangleEntity.Centroid = new Vector2(23.0f, 34.0f);
                triangleEntity.Position = new Vector2(300.0f, 100.0f);

                Vector2[] concaveCircleVertices = new Vector2[27];
                concaveCircleVertices[0] = new Vector2(3.0f, 38.0f);
                concaveCircleVertices[1] = new Vector2(1.0f, 32.0f);
                concaveCircleVertices[2] = new Vector2(0.0f, 26.0f);
                concaveCircleVertices[3] = new Vector2(0.0f, 21.0f);
                concaveCircleVertices[4] = new Vector2(1.0f, 14.0f);
                concaveCircleVertices[5] = new Vector2(4.0f, 8.0f);
                concaveCircleVertices[6] = new Vector2(8.0f, 5.0f);
                concaveCircleVertices[7] = new Vector2(12.0f, 2.0f);
                concaveCircleVertices[8] = new Vector2(16.0f, 0.0f);
                concaveCircleVertices[9] = new Vector2(20.0f, 0.0f);
                concaveCircleVertices[10] = new Vector2(28.0f, 0.0f);
                concaveCircleVertices[11] = new Vector2(34.0f, 1.0f);
                concaveCircleVertices[12] = new Vector2(40.0f, 4.0f);
                concaveCircleVertices[13] = new Vector2(44.0f, 8.0f);
                concaveCircleVertices[14] = new Vector2(47.0f, 14.0f);
                concaveCircleVertices[15] = new Vector2(49.0f, 20.0f);
                concaveCircleVertices[16] = new Vector2(49.0f, 27.0f);
                concaveCircleVertices[17] = new Vector2(48.0f, 32.0f);
                concaveCircleVertices[18] = new Vector2(45.0f, 37.0f);
                concaveCircleVertices[19] = new Vector2(44.0f, 32.0f);
                concaveCircleVertices[20] = new Vector2(42.0f, 28.0f);
                concaveCircleVertices[21] = new Vector2(36.0f, 22.0f);
                concaveCircleVertices[22] = new Vector2(29.0f, 19.0f);
                concaveCircleVertices[23] = new Vector2(20.0f, 19.0f);
                concaveCircleVertices[24] = new Vector2(13.0f, 22.0f);
                concaveCircleVertices[25] = new Vector2(7.0f, 28.0f);
                concaveCircleVertices[26] = new Vector2(5.0f, 34.0f);

                _concaveCircleEntity = new WorldEntity(concaveCircleTexture, concaveCircleVertices);
                _concaveCircleEntity.Centroid = new Vector2(25.0f, 25.0f);
                _concaveCircleEntity.Position = new Vector2(100.0f, 100.0f);

                _purpleSquareEntity = new WorldEntity(whiteSquareTexture, squareVertices);
                _purpleSquareEntity.Centroid = new Vector2(25.0f, 25.0f);
                _purpleSquareEntity.Position = new Vector2(460.0f, 240.0f);

                //container to hold all world world entities
                _worldEntites = new WorldEntity[4];
                _worldEntites[0] = _purpleSquareEntity;
                _worldEntites[1] = triangleEntity;
                _worldEntites[2] = _concaveCircleEntity;
                _worldEntites[3] = _greenSquareEntity;

                //
                _currentlySelectedEntity = _greenSquareEntity;
                _currentlySelectedEntity.Selected = true;

                _currentDetectableEntity = _greenSquareEntity;
                _currentlySelectedEntity.IsDetectable = true;

                _spriteBatch = new SpriteBatch(_graphics.GraphicsDevice);

                _lineOfSight = new LineOfSight(_fovCameraPosition, _greenSquareEntity, _worldEntites, MathHelper.ToRadians(FOV_ANGLE_DEGREES/2), _fovCameraDirectionFacing);
            }
        }
        /// <summary>
        /// Rotates a specified vector.
        /// The rotation is applied to the model co-ordinates not world co-ordinates
        /// </summary>
        /// <param name="point">vertex to rotate</param>
        /// <param name="angle">angle to rotate</param>
        public Vector2 RotatePointOnZAxis(Vector2 point, float angle)
        {
            // Create a rotation matrix that represents a rotation of angle radians.
            Matrix rotationMatrix = Matrix.CreateRotationZ(angle);

            //Translate point to model co-ordinates
            point = Vector2.Transform(point, Matrix.CreateTranslation(-_fovCameraPosition.X, -_fovCameraPosition.Y, 0.0f));
            //Rotate the point
            Vector2 rotatedPoint = Vector2.Transform(point, rotationMatrix);
            //Translate the point back to world co-ordinates
            rotatedPoint = Vector2.Transform(rotatedPoint, Matrix.CreateTranslation(_fovCameraPosition.X, _fovCameraPosition.Y, 0.0f));

            return rotatedPoint;
        }


        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        protected override void UnloadGraphicsContent(bool unloadAllContent)
        {
            if (unloadAllContent)
            {
                _content.Unload();
            }

        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        protected override void Update(GameTime gameTime)
        {
            ControlUpdate();
            base.Update(gameTime);
        }

        /// <summary>
        /// Handles the mouse, gamepad and keyboard controls
        /// </summary>
        /// 
        public void ControlUpdate()
        {
            KeyboardState keyboard = Keyboard.GetState();
            GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
            MouseState mouse = Mouse.GetState();

            _elapsed = 0.0f;

            if (gamePad.Buttons.Back == ButtonState.Pressed ||
                keyboard.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            if (keyboard.IsKeyDown(Keys.Left) ||
                gamePad.DPad.Left == ButtonState.Pressed)
            {
                _fovCameraPosition.X -= FOV_CAMERA_SPEED;
                _fovCameraDirectionFacing.X -= FOV_CAMERA_SPEED;
                _fovCameraLeftAngleBoundary.X -= FOV_CAMERA_SPEED;
                _fovCameraRightAngleBoundary.X -= FOV_CAMERA_SPEED;

            }
            if (keyboard.IsKeyDown(Keys.Right) ||
                gamePad.DPad.Right == ButtonState.Pressed)
            {
                _fovCameraPosition.X += FOV_CAMERA_SPEED;
                _fovCameraDirectionFacing.X += FOV_CAMERA_SPEED;
                _fovCameraLeftAngleBoundary.X += FOV_CAMERA_SPEED;
                _fovCameraRightAngleBoundary.X += FOV_CAMERA_SPEED;
            }

            if (keyboard.IsKeyDown(Keys.Up) ||
             gamePad.DPad.Up == ButtonState.Pressed)
            {
                _fovCameraPosition.Y -= FOV_CAMERA_SPEED;
                _fovCameraDirectionFacing.Y -= FOV_CAMERA_SPEED;
                _fovCameraLeftAngleBoundary.Y -= FOV_CAMERA_SPEED;
                _fovCameraRightAngleBoundary.Y -= FOV_CAMERA_SPEED;
            }
            if (keyboard.IsKeyDown(Keys.Down) ||
                gamePad.DPad.Down == ButtonState.Pressed)
            {
                _fovCameraPosition.Y += FOV_CAMERA_SPEED;
                _fovCameraDirectionFacing.Y += FOV_CAMERA_SPEED;
                _fovCameraLeftAngleBoundary.Y += FOV_CAMERA_SPEED;
                _fovCameraRightAngleBoundary.Y += FOV_CAMERA_SPEED;
            }

            if (keyboard.IsKeyDown(Keys.Delete))
            {
                _elapsed += -0.01f;
            }

            if (keyboard.IsKeyDown(Keys.PageDown))
            {
                _elapsed = 0.01f;
            }

            _fovCameraLeftAngleBoundary = RotatePointOnZAxis(_fovCameraLeftAngleBoundary, _elapsed);
            _fovCameraRightAngleBoundary = RotatePointOnZAxis(_fovCameraRightAngleBoundary, _elapsed);

            _fovCameraDirectionFacing = RotatePointOnZAxis(_fovCameraDirectionFacing, _elapsed);

            _RotationAngle += _elapsed;
            float circle = MathHelper.Pi * 2;
            _RotationAngle = _RotationAngle % circle;

            _cursorPosition.X = (float)mouse.X;
            _cursorPosition.Y = (float)mouse.Y;

            if (!_leftMouseBtnHeld)
            {
                if (mouse.LeftButton == ButtonState.Pressed)
                {
                    //loop through each object and see if the cursor is on the object
                    //just using a basic square shape so it is not pixel perfect
                    foreach (WorldEntity entity in _worldEntites)
                    {
                        if (_cursorPosition.X > entity.Position.X && _cursorPosition.X < entity.Position.X + entity.Texture.Width)
                        {
                            if (_cursorPosition.Y > entity.Position.Y && _cursorPosition.Y < entity.Position.Y + entity.Texture.Height)
                            {
                                if (keyboard.IsKeyDown(Keys.S))
                                {
                                    //Set what was the detectable entity to false
                                    _currentDetectableEntity.IsDetectable = false;
                                    //Assign new entity and set detectable to true
                                    _currentDetectableEntity = entity;
                                    _currentDetectableEntity.IsDetectable = true;
                                    //update the line of sight to know which object to detect
                                    _lineOfSight.DetectableEntity = _currentDetectableEntity;

                                }
                                _currentlySelectedEntity.Selected = false;
                                _currentlySelectedEntity = entity;
                               
                                //these values are used later to to keep the cursur position relative to the oject when dragging
                                _currentlySelectedEntity.XDiff = _cursorPosition.X - _currentlySelectedEntity.Position.X;
                                _currentlySelectedEntity.YDiff = _cursorPosition.Y - _currentlySelectedEntity.Position.Y;

                                _currentlySelectedEntity.Selected = true;
                                _leftMouseBtnHeld = true;
                                break;
                            }
                        }
                    }
                }
            }
            //-------------------
            if (!_rightMouseBtnHeld)
            {
                if (mouse.RightButton == ButtonState.Pressed)
                {
                    foreach (WorldEntity entity in _worldEntites)
                    {

                        if (_cursorPosition.X > entity.Position.X &&
                                _cursorPosition.X < entity.Position.X + entity.Texture.Width)
                        {
                            if (_cursorPosition.Y > entity.Position.Y
                                    && _cursorPosition.Y < entity.Position.Y + entity.Texture.Height)
                            {
                                _currentlySelectedEntity.Selected = false;
                                _currentlySelectedEntity = entity;
                                _currentlySelectedEntity.Selected = true;
                                _rightMouseBtnHeld = true;
                                break;
                            }
                        }
                    }
                }
            }

            // y - entity.position.Y;
            if (_leftMouseBtnHeld)
            {
                //following required for allowing the user to hold down left then press 'S'
                if (keyboard.IsKeyDown(Keys.S))
                {
                    _currentDetectableEntity.IsDetectable = false;
                    _currentDetectableEntity = _currentlySelectedEntity;
                    _currentDetectableEntity.IsDetectable = true;
                    _lineOfSight.DetectableEntity = _currentDetectableEntity;

                }
                _currentlySelectedEntityPosition.X = _cursorPosition.X - _currentlySelectedEntity.XDiff;
                _currentlySelectedEntityPosition.Y = _cursorPosition.Y - _currentlySelectedEntity.YDiff;
                _currentlySelectedEntity.Position = _currentlySelectedEntityPosition;
            }
            if (_rightMouseBtnHeld)
            {
                _currentlySelectedEntity.Rotate = 0.01f;
            }
            if (mouse.LeftButton == ButtonState.Released)
            {
                _leftMouseBtnHeld = false;
            }

            if (mouse.RightButton == ButtonState.Released)
            {
                _rightMouseBtnHeld = false;
            }
            if (mouse.RightButton == ButtonState.Released && mouse.LeftButton == ButtonState.Released)
            {
                _currentlySelectedEntity.Selected = false;
            }
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = _graphics.GraphicsDevice;
      
            if (_lineOfSight.InLineOfSight(_fovCameraPosition, _fovCameraDirectionFacing, _fovCameraLeftAngleBoundary, _fovCameraRightAngleBoundary))
            {
                device.Clear(Color.Red);
            }
            else
            {
                device.Clear(Color.CornflowerBlue); 
            }

            _spriteBatch.Begin();

            //Draws the FOV boundary lines
            _spriteBatch.Draw(lineTexture, _fovCameraPosition, null, Color.White,
                             (_RotationAngle - _fovAngleRadians * 0.5f), Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
            _spriteBatch.Draw(lineTexture, _fovCameraPosition, null, Color.White,
                   (_RotationAngle + _fovAngleRadians * 0.5f), Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);

          
            foreach (WorldEntity entity in _worldEntites)
            {
                //We want the position to be in the centre of where the current entity is located
                _drawPosition.X = entity.Position.X + entity.Centroid.X;
                _drawPosition.Y = entity.Position.Y + entity.Centroid.Y;
                _origin.X = entity.Centroid.X;
                _origin.Y = entity.Centroid.Y;

                if (entity.IsDetectable)
                {
                    //draws the texture as the colour black
                    _spriteBatch.Draw(entity.Texture, _drawPosition, null, Color.Black, entity.Rotate, _origin, 1.0f, SpriteEffects.None, 0.0f);
                }
                else
                {
                    _spriteBatch.Draw(entity.Texture, _drawPosition, null, Color.White, entity.Rotate, _origin, 1.0f, SpriteEffects.None, 0.0f);
                }
            }

            _drawPosition.X = _cursorPosition.X - _cursorOrigin.X;
            _drawPosition.Y = _cursorPosition.Y - _cursorOrigin.Y;

            _spriteBatch.Draw(cursorCrosshairTexture, _drawPosition, null, Color.WhiteSmoke, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
