using AutomaticTypeMapper;
using EOLib.Domain.Character;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering
{
    /// <summary>
    /// A 2D camera for isometric tile-based rendering that caches transformation matrices
    /// and reduces per-frame calculations by tracking the viewport position in world space.
    /// </summary>
    [AutoMappedType(IsSingleton = true)]
    public class Camera2D : ICamera2D
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;

        private Vector2 _position;
        private Matrix _transformMatrix;
        private bool _isDirty = true;
        private int _lastCharacterX = -1;
        private int _lastCharacterY = -1;
        private int _lastCharacterOffsetX = 0;
        private int _lastCharacterOffsetY = 0;
        private int _lastViewportWidth = -1;
        private int _lastViewportHeight = -1;

        public Vector2 Position => _position;
        public Matrix TransformMatrix => _transformMatrix;

        public Camera2D(ICharacterProvider characterProvider,
                       IRenderOffsetCalculator renderOffsetCalculator,
                       IClientWindowSizeProvider clientWindowSizeProvider)
        {
            _characterProvider = characterProvider;
            _renderOffsetCalculator = renderOffsetCalculator;
            _clientWindowSizeProvider = clientWindowSizeProvider;
        }

        /// <summary>
        /// Updates the camera position based on the main character's position.
        /// Only recalculates when the character actually moves or viewport changes.
        /// </summary>
        public void Update()
        {
            var character = _characterProvider.MainCharacter;
            var renderProps = character.RenderProperties;

            var currentX = renderProps.MapX;
            var currentY = renderProps.MapY;
            var currentOffsetX = _renderOffsetCalculator.CalculateWalkAdjustX(renderProps);
            var currentOffsetY = _renderOffsetCalculator.CalculateWalkAdjustY(renderProps);
            var currentWidth = _clientWindowSizeProvider.Width;
            var currentHeight = _clientWindowSizeProvider.Height;

            // Only update if character moved or viewport changed
            if (_lastCharacterX != currentX || _lastCharacterY != currentY ||
                _lastCharacterOffsetX != currentOffsetX || _lastCharacterOffsetY != currentOffsetY ||
                _lastViewportWidth != currentWidth || _lastViewportHeight != currentHeight)
            {
                _lastCharacterX = currentX;
                _lastCharacterY = currentY;
                _lastCharacterOffsetX = currentOffsetX;
                _lastCharacterOffsetY = currentOffsetY;
                _lastViewportWidth = currentWidth;
                _lastViewportHeight = currentHeight;

                UpdatePosition();
                _isDirty = true;
            }

            if (_isDirty)
            {
                UpdateTransformMatrix();
                _isDirty = false;
            }
        }

        /// <summary>
        /// Forces a camera update on the next frame.
        /// Use when map changes or other significant events occur.
        /// </summary>
        public void ForceUpdate()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Converts world coordinates to screen coordinates using cached camera position.
        /// </summary>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return worldPosition - _position;
        }

        /// <summary>
        /// Converts world coordinates to screen coordinates using cached camera position.
        /// </summary>
        public Vector2 WorldToScreen(int worldX, int worldY)
        {
            return new Vector2(worldX, worldY) - _position;
        }

        /// <summary>
        /// Converts screen coordinates to world coordinates using cached camera position.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return screenPosition + _position;
        }

        /// <summary>
        /// Gets the camera offset for the main character (cached).
        /// This replaces the need to constantly recalculate GetMainCharacterOffsets().
        /// </summary>
        public Vector2 GetMainCharacterOffset()
        {
            var props = _characterProvider.MainCharacter.RenderProperties;
            return new Vector2(
                _renderOffsetCalculator.CalculateOffsetX(props),
                _renderOffsetCalculator.CalculateOffsetY(props));
        }

        private void UpdatePosition()
        {
            var props = _characterProvider.MainCharacter.RenderProperties;

            var widthFactor = _clientWindowSizeProvider.Width / 2;
            var heightFactor = _clientWindowSizeProvider.Resizable
                ? _clientWindowSizeProvider.Height / 2
                : _clientWindowSizeProvider.Height * 3 / 10 - 2;

            // Calculate the world position the camera should be centered on
            var characterWorldX = _renderOffsetCalculator.CalculateOffsetX(props);
            var characterWorldY = _renderOffsetCalculator.CalculateOffsetY(props);

            // Camera position is the offset needed to center the character
            _position = new Vector2(
                characterWorldX - widthFactor,
                characterWorldY - heightFactor);
        }

        private void UpdateTransformMatrix()
        {
            // For sprite batch, we create a translation matrix
            // This moves all world coordinates to screen space
            _transformMatrix = Matrix.CreateTranslation(-_position.X, -_position.Y, 0);
        }
    }

    public interface ICamera2D
    {
        /// <summary>
        /// Gets the camera's position in world space.
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// Gets the transformation matrix for rendering with SpriteBatch.
        /// </summary>
        Matrix TransformMatrix { get; }

        /// <summary>
        /// Updates the camera based on character movement.
        /// </summary>
        void Update();

        /// <summary>
        /// Forces a camera update on the next frame.
        /// </summary>
        void ForceUpdate();

        /// <summary>
        /// Converts world coordinates to screen coordinates.
        /// </summary>
        Vector2 WorldToScreen(Vector2 worldPosition);

        /// <summary>
        /// Converts world coordinates to screen coordinates.
        /// </summary>
        Vector2 WorldToScreen(int worldX, int worldY);

        /// <summary>
        /// Converts screen coordinates to world coordinates.
        /// </summary>
        Vector2 ScreenToWorld(Vector2 screenPosition);

        /// <summary>
        /// Gets the cached main character offset.
        /// </summary>
        Vector2 GetMainCharacterOffset();
    }
}
