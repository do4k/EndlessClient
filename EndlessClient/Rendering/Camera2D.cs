using AutomaticTypeMapper;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering
{
    /// <summary>
    /// A 2D camera that transforms the viewport instead of recalculating positions for every entity.
    /// This approach relies on MonoGame's transformation matrix rather than manually offsetting every tile.
    /// </summary>
    [MappedType(BaseType = typeof(ICamera2D))]
    public class Camera2D : ICamera2D
    {
        private const int WidthFactor = 32;
        private const int HeightFactor = 16;
        private const int WalkWidthFactor = WidthFactor / 4;
        private const int WalkHeightFactor = HeightFactor / 4;

        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private Matrix _transformMatrix;
        private Vector2 _position;
        private float _zoom;

        public Vector2 Position
        {
            get => _position;
            private set
            {
                if (_position != value)
                {
                    _position = value;
                    UpdateTransformMatrix();
                }
            }
        }

        public float Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom != value)
                {
                    _zoom = value;
                    UpdateTransformMatrix();
                }
            }
        }

        public Matrix TransformMatrix => _transformMatrix;

        public Camera2D(IClientWindowSizeProvider clientWindowSizeProvider)
        {
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _zoom = 1.0f;
            _position = Vector2.Zero;
            UpdateTransformMatrix();
        }

        public void CenterOnCharacter(CharacterRenderProperties properties)
        {
            // Calculate the world position of the character using isometric projection
            var worldX = properties.MapX * WidthFactor - properties.MapY * WidthFactor;
            var worldY = properties.MapX * HeightFactor + properties.MapY * HeightFactor;

            // Add walk animation adjustments
            worldX += CalculateWalkAdjustX(properties);
            worldY += CalculateWalkAdjustY(properties);

            // Camera position is the character's world position
            // The transform will center this at the viewport center
            Position = new Vector2(worldX, worldY);
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, _transformMatrix);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(_transformMatrix));
        }

        public Vector2 GetWorldPositionFromGridCoordinates(int gridX, int gridY)
        {
            var worldX = gridX * WidthFactor - gridY * WidthFactor;
            var worldY = gridX * HeightFactor + gridY * HeightFactor;
            return new Vector2(worldX, worldY);
        }

        private void UpdateTransformMatrix()
        {
            var viewportCenter = new Vector3(
                _clientWindowSizeProvider.Width / 2f,
                _clientWindowSizeProvider.Resizable
                    ? _clientWindowSizeProvider.Height / 2f
                    : _clientWindowSizeProvider.Height * 3 / 10f - 2,
                0);

            // Transform: Translate to origin, scale, translate to viewport center, then offset by camera position
            _transformMatrix =
                Matrix.CreateTranslation(new Vector3(-_position, 0)) *  // Move camera position to origin
                Matrix.CreateScale(_zoom, _zoom, 1) *                    // Apply zoom
                Matrix.CreateTranslation(viewportCenter);                 // Move to viewport center
        }

        private int CalculateWalkAdjustX(CharacterRenderProperties properties)
        {
            var multiplier = properties.IsFacing(EOLib.EODirection.Left, EOLib.EODirection.Down) ? -1 : 1;
            var walkAdjust = properties.IsActing(CharacterActionState.Walking) ? WalkWidthFactor * properties.ActualWalkFrame : 0;
            return walkAdjust * multiplier;
        }

        private int CalculateWalkAdjustY(CharacterRenderProperties properties)
        {
            var multiplier = properties.IsFacing(EOLib.EODirection.Left, EOLib.EODirection.Up) ? -1 : 1;
            var walkAdjust = properties.IsActing(CharacterActionState.Walking) ? WalkHeightFactor * properties.ActualWalkFrame : 0;
            return walkAdjust * multiplier;
        }
    }

    public interface ICamera2D
    {
        /// <summary>
        /// The camera's position in world coordinates (follows the character).
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// The zoom level (1.0 = normal, >1.0 = zoomed in, &lt;1.0 = zoomed out).
        /// </summary>
        float Zoom { get; set; }

        /// <summary>
        /// The transformation matrix to apply to SpriteBatch.Begin().
        /// </summary>
        Matrix TransformMatrix { get; }

        /// <summary>
        /// Centers the camera on the given character, including walk animation offsets.
        /// </summary>
        void CenterOnCharacter(CharacterRenderProperties properties);

        /// <summary>
        /// Converts a world position to screen coordinates.
        /// </summary>
        Vector2 WorldToScreen(Vector2 worldPosition);

        /// <summary>
        /// Converts screen coordinates to world position.
        /// </summary>
        Vector2 ScreenToWorld(Vector2 screenPosition);

        /// <summary>
        /// Gets the world position from grid coordinates.
        /// </summary>
        Vector2 GetWorldPositionFromGridCoordinates(int gridX, int gridY);
    }
}
