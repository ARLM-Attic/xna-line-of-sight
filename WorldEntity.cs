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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace AllanBishop.XNA
{
    /// <summary>
    /// 
    /// This class creates a world entity
    /// 
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
    public class WorldEntity
    {
        #region Member Variables

        private Texture2D _texture;
        private Vector2[] _vertices;
        private bool _isDetectable;
        private Vector2 _position;
        private bool _selected;
        private float _rotation;
        private float _xDiff;
        private float _yDiff;
        private Vector2 _centroid;//The middle mass of the entity

        #endregion

        #region Properties

        public Vector2[] Vertices
        {
            get
            {
                return _vertices;
            }
            set
            {
                _vertices = value;
            }
        }


        public Texture2D Texture
        {
            get
            {
                return _texture;
            }
            set
            {
                _texture = value;
            }
        }


        public float XDiff
        {
            get
            {
                return _xDiff;
            }
            set
            {
                _xDiff = value;
            }
        }

        public float YDiff
        {
            get
            {
                return _yDiff;
            }
            set
            {
                _yDiff = value;
            }
        }


        public Vector2 Centroid
        {
            get
            {
                return _centroid;
            }
            set
            {
                _centroid = value;
            }
        }
        
        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                Vector2 tempPosition = value;

                _position = tempPosition - _position;

                //updates the object's vertices positions based on the objects new position
                for (int i = 0; i < _vertices.Length; i++)
                {
                    _vertices[i] += _position;
                }
                _position = tempPosition;
            }
        }
     

        public bool Selected
        {
            get
            {
                return _selected;
            }

            set
            {
                _selected = value;
            }
        }

        public bool IsDetectable
        {
            get
            {
                return _isDetectable;
            }
            set
            {
                _isDetectable = value;
            }
        }

        public float Rotate
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation += value;
                float circle = MathHelper.Pi * 2;
                _rotation = _rotation % circle;

                //updates the object's vertices positions based on the objects new position
                for (int i = 0; i < _vertices.Length; i++)
                {
                    _vertices[i] = this.RotatePointOnZAxis(_vertices[i], .01f);
                }
            }
        }

        #endregion

        #region Constructors

        public WorldEntity()
        {


        }

        public WorldEntity(Texture2D texture, Vector2[] squareVerticesertices)
        {
            _isDetectable = false;
            _rotation = 0f;
            _xDiff = 0.0f;
            _yDiff = 0.0f;
            _position = Vector2.Zero;
            _selected = false;
            _texture = texture;
            _vertices = new Vector2[squareVerticesertices.Length];

            //Performs a deep copy of the vertices array
            for (int i = 0; i < squareVerticesertices.Length; i++)
            {
                _vertices[i] = squareVerticesertices[i];
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Rotates a specified vertex belonging to this entity.
        /// The rotation is applied to the model co-ordinates not world co-ordinates
        /// </summary>
        /// <param name="point">vertex to rotate</param>
        /// <param name="angle">angle to rotate</param>
        /// <returns>Vector rotated by an angle</returns>
        //Based on http://www.nfostergames.com/xnaTipsAndLinks.htm
        private Vector2 RotatePointOnZAxis(Vector2 point, float angle)
        {
            // Create a rotation matrix that represents a rotation of angle radians.
            Matrix rotationMatrix = Matrix.CreateRotationZ(angle);

            // Apply the rotation matrix to the point.
            point = Vector2.Transform(point, Matrix.CreateTranslation(-(_position.X + _centroid.X), -(_position.Y + _centroid.Y), 0.0f));
            Vector2 rotatedPoint = Vector2.Transform(point, rotationMatrix);
            rotatedPoint = Vector2.Transform(rotatedPoint, Matrix.CreateTranslation(_position.X + _centroid.X, _position.Y + _centroid.Y, 0.0f));

            return rotatedPoint;
        }

        #endregion
    }
}
