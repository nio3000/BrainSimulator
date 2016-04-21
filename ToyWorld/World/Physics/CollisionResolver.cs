﻿using System;
using System.Diagnostics;
using VRageMath;

namespace World.Physics
{
    public interface ICollisionResolver
    {
        void ResolveCollision(IForwardMovablePhysicalEntity physicalEntity);
    }

    public class CollisionResolver : ICollisionResolver
    {
        private readonly ICollisionChecker m_collisionChecker;
        private readonly IMovementPhysics m_movementPhysics;

        private const int BINARY_SEARCH_ITERATIONS = 16;
        private const float X_DIRECTION = (float) Math.PI / 2;
        private const float Y_DIRECTION = 0;

        private Random random = new Random(79);

        public CollisionResolver(ICollisionChecker collisionChecker, IMovementPhysics movementPhysics)
        {
            m_collisionChecker = collisionChecker;
            m_movementPhysics = movementPhysics;
        }

        public void ResolveCollision(IForwardMovablePhysicalEntity physicalEntity)
        {
            if (!m_collisionChecker.CollidesWithTile(physicalEntity))
            {
                return;
            }
            else
            {
                // move to previous position
                physicalEntity.Position = Utils.Move(physicalEntity.Position, physicalEntity.Direction, -physicalEntity.ForwardSpeed);
            }
            FindTileFreePosition(physicalEntity);
        }

        private void FindTileFreePosition(IForwardMovablePhysicalEntity physicalEntity)
        {
            float speed = physicalEntity.ForwardSpeed;
            Vector2 previousPosition = physicalEntity.Position;
            // get back to last free position in direction counter to original direction
            TileFreePositionBinarySearch(physicalEntity, physicalEntity.ForwardSpeed, physicalEntity.Direction);

            Vector2 freePosition = physicalEntity.Position;
            float directionRads = physicalEntity.Direction;
            float residueSpeed = speed - Vector2.Distance(previousPosition, freePosition);
            float xSpeed = (float)Math.Sin(directionRads) * residueSpeed;
            float ySpeed = (float)Math.Cos(directionRads) * residueSpeed;
            // position before move
            
            // try to move orthogonally left/right and up/down
            TileFreePositionBinarySearch(physicalEntity, xSpeed, X_DIRECTION);
            TileFreePositionBinarySearch(physicalEntity, ySpeed, Y_DIRECTION);
            Vector2 xFirstPosition = new Vector2(physicalEntity.Position.X, physicalEntity.Position.Y);

            // try to move orthogonally up/down and left/right; reset position first
            physicalEntity.Position = freePosition;
            TileFreePositionBinarySearch(physicalEntity, ySpeed, Y_DIRECTION);
            TileFreePositionBinarySearch(physicalEntity, xSpeed, X_DIRECTION);
            Vector2 yFirstPosition = new Vector2(physicalEntity.Position.X, physicalEntity.Position.Y);

            // farther position is chosen
            if (Vector2.Distance(freePosition, xFirstPosition) > Vector2.Distance(freePosition, yFirstPosition))
            {
                physicalEntity.Position = xFirstPosition;
            }
            else
            {
                physicalEntity.Position = yFirstPosition;
            }
        }

        /// <summary>
        /// Search for position close to obstacle in given direction. Must be on stable position when starting search.
        /// </summary>
        /// <param name="physicalEntity"></param>
        /// <param name="initialSpeed"></param>
        /// <param name="direction"></param>
        /// <param name="goForward">If true make step forward. Otherwise start with half step back.</param>
        private void TileFreePositionBinarySearch(IForwardMovablePhysicalEntity physicalEntity, float initialSpeed, float direction)
        {
            float speed = initialSpeed;
            bool goForward = false;

            Vector2 lastNotColliding = physicalEntity.Position;

            m_movementPhysics.Shift(physicalEntity, initialSpeed, direction);

            for (int i = 0; i < BINARY_SEARCH_ITERATIONS; i++)
            {
                if (goForward)
                {
                    m_movementPhysics.Shift(physicalEntity, speed, direction);
                }
                else
                {
                    m_movementPhysics.Shift(physicalEntity, -speed, direction);
                }
                bool colliding = m_collisionChecker.CollidesWithTile(physicalEntity);
                if (!colliding)
                {
                    lastNotColliding = physicalEntity.Position;
                }
                speed = speed / 2;
                goForward = !colliding;
            }

            physicalEntity.Position = lastNotColliding;
        }
    }
}
