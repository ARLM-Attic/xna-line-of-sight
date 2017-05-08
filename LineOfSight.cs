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

#endregion

namespace AllanBishop.XNA
{
    /// <summary>
    /// 
    /// This class creates a two dimensional line of sight system.
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
    public class LineOfSight
    {
        #region class members

        private Vector2 _fovCameraPosition;
        private WorldEntity[] _worldEntities;
        private float _fovAngleInRadians;
        private Vector2 _fovCameraDirection;
        private Vector2 _result;
        private WorldEntity _detectableEntity;
        private Vector2 _newPixelPosition;

        #endregion

        #region Parameters

        public WorldEntity DetectableEntity
        {
            get
            {
                return _detectableEntity;
            }
            set
            {
                _detectableEntity = value;
            }
        }

        #endregion

        #region Constructors

        public LineOfSight(Vector2 fovCameraPostion, WorldEntity detectableEntity, WorldEntity[] worldEntites, float fovAngleInRadians, Vector2 fovCameraDirection)
        {
            _fovCameraPosition = fovCameraPostion;
            _detectableEntity = detectableEntity;
            _worldEntities = worldEntites;
            _fovAngleInRadians = fovAngleInRadians;
            _fovCameraDirection = fovCameraDirection;
            _result = new Vector2();
            _newPixelPosition = new Vector2();

        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns true if the detectable entity is in anyway visible to the fov camera in the world
        /// </summary>
        /// <param name="fovCameraPostion">position of the camera</param>
        /// <param name="fovCameraDirection">direction camera is facing</param>
        /// <param name="_fovCameraLeftAngleBoundary">a vector the lies on the edge of the FOV left boundary</param>
        /// <param name="_fovCameraRightAngleBoundary">a vector the lies on the edge of the FOV right boundary</param>  

        public bool InLineOfSight(Vector2 fovCameraPosition, Vector2 fovCameraDirection, Vector2 _fovCameraLeftAngleBoundary, Vector2 _fovCameraRightAngleBoundary)
        {
            _fovCameraPosition = fovCameraPosition;
  
            for (int i = 0; i < _detectableEntity.Vertices.Length; i++)
            {
                Vector2 vertex = _detectableEntity.Vertices[i];
                Vector2 nextVertex;

                if (i != _detectableEntity.Vertices.Length - 1) // if not last vertex
                {
                    nextVertex = _detectableEntity.Vertices[i + 1];
                }
                else
                {
                    nextVertex = _detectableEntity.Vertices[0]; //must be last so we go back to the start
                }

                //we check to see if a lines end points are in FOV to determine if either all, partial or no parts of the
                //line or in the Field of View
                bool isVertexInFOV = isSecondInFOVofFirst(fovCameraPosition, fovCameraDirection, vertex, _fovAngleInRadians);

                bool isNextVertexInFOV = isSecondInFOVofFirst(fovCameraPosition, fovCameraDirection, nextVertex, _fovAngleInRadians);

                //whole line section in FOV
                if (isVertexInFOV && isNextVertexInFOV)
                {
                    if (IterateAlongLine(vertex, nextVertex))
                    {
                        return true;
                    }
                }
                else
                {
                    //if start of line only in FOV
                    if (isVertexInFOV)
                    {
                        //as we know its only a partial line we need to find where its been intersected
                        //and use that point as our new end point

                        //we must test both FOV lines as we do not know which has intersected
                        //we will also get the new end point in _result
                        if (!IntersectionOfTwoLines(fovCameraPosition, _fovCameraLeftAngleBoundary, vertex, nextVertex, ref  _result))
                        {
                            IntersectionOfTwoLines(fovCameraPosition, _fovCameraRightAngleBoundary, vertex, nextVertex, ref  _result);
                        }

                        //if a point along the line is visible
                        if (IterateAlongLine(vertex, _result))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //if end of line only in FOV
                        if (isNextVertexInFOV)
                        {
                            if (!IntersectionOfTwoLines(fovCameraPosition, _fovCameraLeftAngleBoundary, vertex, nextVertex, ref _result))
                            {
                                IntersectionOfTwoLines(fovCameraPosition, _fovCameraRightAngleBoundary, vertex, nextVertex, ref _result);
                            }
                            if (IterateAlongLine(_result, nextVertex))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            //to get here must mean that object is not in _lineOfSight because at no point do we return true 
            return false;
        }

        /// <summary>
        /// Returns true if the detectable entity is in the FOV regardless if other world entities are blocking it from sight
        /// </summary>
        /// <param name="posFirst">position of the camera</param>
        /// <param name="_fovCameraDirectionFacing">direction camera is facing</param>
        /// <param name="posSecond">position of entity</param>
        /// <param name="fov">angle of the field of view (in radians)</param>  
        private bool isSecondInFOVofFirst(Vector2 posFirst, Vector2 _fovCameraDirectionFacing, Vector2 posSecond, double fov)
        {
            //the creation of the folowing vectors is too make the vectors a location from another vector
            //rather than a location from the world origin
            Vector2 positionDifference = posSecond - posFirst;

            _fovCameraDirectionFacing = _fovCameraDirectionFacing - posFirst;
            _fovCameraDirectionFacing.Normalize();
            positionDifference.Normalize();

            return Vector2.Dot(_fovCameraDirectionFacing, positionDifference) >= Math.Cos(fov);
        }

        /// <summary>
        /// Returns true if two lines intersect and returns the location of where they intersect
        /// </summary>
        /// <param name="a">start of first line</param>
        /// <param name="b">end of first line</param>
        /// <param name="c">start of second line</param>
        /// <param name="d">end of second line</param>  
        /// <param name="_result">returned vector of where (if) the two lines intersect</param>  
        /// 
        ///Method from http://www.ziggyware.com/readarticle.php?article_id=78
        ///********************************************************************
        ///Desc: Taken from comp.graphics.algorithms FAQ
        ///*********************************************************************/

        private static bool IntersectionOfTwoLines(Vector2 a, Vector2 b, Vector2 c,
                                       Vector2 d, ref Vector2 _result)
        {
            double r, s;

            double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            // If the denominator in above is zero, AB & CD are colinear
            if (denominator == 0)
                return false;

            double numeratorR = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            //  If the numerator above is also zero, AB & CD are collinear.
            //  If they are collinear, then the segments may be projected to the x- 
            //  or y-axis, and overlap of the projected intervals checked.

            r = numeratorR / denominator;

            double numeratorS = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            s = numeratorS / denominator;

            //  If 0<=r<=1 & 0<=s<=1, intersection exists
            //  r<0 or r>1 or s<0 or s>1 line segments do not intersect
            if (r < 0 || r > 1 || s < 0 || s > 1)
                return false;

            ///*
            //    Note:
            //    If the intersection point of the 2 lines are needed (lines in this
            //    context mean infinite lines) regardless whether the two line
            //    segments intersect, then
            //
            //        If r>1, P is located on extension of AB
            //        If r<0, P is located on extension of BA
            //        If s>1, P is located on extension of CD
            //        If s<0, P is located on extension of DC
            //*/

            // Find intersection point
            _result.X = (float)(a.X + (r * (b.X - a.X)));
            _result.Y = (float)(a.Y + (r * (b.Y - a.Y)));

            return true;
        }

        /// <summary>
        /// Method iterates along a line (of an entity), at each point along the line a 'ray' is fired towards the fov camera,
        /// it is then calculated if this ray intersects with any other lines of any other entity. If the ray
        /// does intersect then that point on the line of the entity is blocked from view. If any of the points on 
        /// the line are not intersected then we return true as a segment of the line can be seen. Otherwise if all
        /// points on the line are blocked we return false
        /// </summary>
        /// <param name="start">start of the line</param>
        /// <param name="end">end of the line</param>
        /// 
        /// Bresenham's line algorithm portion of method from http://www.gamedev.net/reference/articles/article1275.asp
        /// 
        private bool IterateAlongLine(Vector2 start, Vector2 end)
        {
            int deltax = (int)Math.Abs(end.X - start.X);
            int deltay = (int)Math.Abs(end.Y - start.Y);
            int x = (int)start.X;
            int y = (int)start.Y;
            int xinc1;
            int xinc2;
            int yinc1;
            int yinc2;
            int den;
            int num;
            int numadd;
            int numpixels;

            if (end.X >= start.X)
            {
                xinc1 = 1;
                xinc2 = 1;
            }
            else
            {
                xinc1 = -1;
                xinc2 = -1;
            }
            if (end.Y >= start.Y)
            {
                yinc1 = 1;
                yinc2 = 1;

            }
            else
            {
                yinc1 = -1;
                yinc2 = -1;
            }
            if (deltax >= deltay)
            {
                xinc1 = 0;
                yinc2 = 0;
                den = deltax;
                num = deltax / 2;
                numadd = deltay;
                numpixels = deltax;
            }
            else
            {
                xinc2 = 0;
                yinc1 = 0;
                den = deltay;
                num = deltay / 2;
                numadd = deltax;
                numpixels = deltay;

            }
            for (int curpixel = 0; curpixel <= numpixels; curpixel++)
            {
                num += numadd;
                if (num >= den)
                {
                    num -= den;
                    x += xinc1;
                    y += yinc1;

                }
                x += xinc2;
                y += yinc2;

                _newPixelPosition.X = (float)x;
                _newPixelPosition.Y = (float)y;

                bool intersects = false;
                bool pointIntersected = false;
              
                //we now fire a line from this position that lies somewhere on the entities line to the FOV camera, 
                //we then check to see if it intersects with anything else on the way. If it does then we know that this pixel
                //is blocked from sight. 
                foreach (WorldEntity entity in _worldEntities)
                {
                    if (!entity.IsDetectable)
                    {
                        for (int i = 0; i < entity.Vertices.Length; i++)
                        {
                            //checks to see if we are on the final line comprising an object (so the lines endpoint is the first vertex)
                            if (i == entity.Vertices.Length - 1)
                            {
                                intersects = IntersectionOfTwoLines(_fovCameraPosition, _newPixelPosition, entity.Vertices[i], entity.Vertices[0], ref _result);
                            }
                            else
                            {
                                intersects = IntersectionOfTwoLines(_fovCameraPosition, _newPixelPosition, entity.Vertices[i], entity.Vertices[i + 1], ref _result);
                            }

                            if (intersects)
                            {
                                pointIntersected = true;
                                break;
                            }
                        }
                    }
                    if (pointIntersected)
                    {
                        break; //as its intersected we need no futher testing, one intersection is enough to block its view
                    }
                }

                if (!pointIntersected)// the point did not intersect with any line in any obstacle
                {
                    return true; //thus the point must be in the FOV and is not being blocked so its in the _lineOfSight
                }
            }

            return false;
        }

        #endregion
    }
}