using System;
using AutomaticTypeMapper;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;
using DomainNPC = EOLib.Domain.NPC.NPC;

namespace EndlessClient.Rendering
{
    [AutoMappedType]
    public class GridDrawCoordinateCalculator : IGridDrawCoordinateCalculator
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private readonly ICamera2D _camera;

        public GridDrawCoordinateCalculator(ICharacterProvider characterProvider,
                                            ICurrentMapProvider currentMapProvider,
                                            IRenderOffsetCalculator renderOffsetCalculator,
                                            IClientWindowSizeProvider clientWindowSizeProvider,
                                            ICamera2D camera)
        {
            _characterProvider = characterProvider;
            _currentMapProvider = currentMapProvider;
            _renderOffsetCalculator = renderOffsetCalculator;
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _camera = camera;
        }

        public Vector2 CalculateRawRenderCoordinatesFromGridUnits(int gridX, int gridY, int tileWidth = 64, int tileHeight = 32)
        {
            var widthFactor = tileWidth / 2;
            var heightFactor = tileHeight / 2;

            return new Vector2((gridX * widthFactor) - (gridY * widthFactor),
                               (gridY * heightFactor) + (gridX * heightFactor));
        }

        public Vector2 CalculateDrawCoordinatesFromGridUnits(int gridX, int gridY)
        {
            // Camera-based approach: Return world position directly
            // Previously, this method subtracted main character offsets from viewport center
            // Now the camera encapsulates that logic, making this cleaner
            return _camera.GetWorldPositionFromGridCoordinates(gridX, gridY);
        }

        public Vector2 CalculateDrawCoordinatesFromGridUnits(MapCoordinate mapCoordinate)
        {
            return CalculateDrawCoordinatesFromGridUnits(mapCoordinate.X, mapCoordinate.Y);
        }

        public Vector2 CalculateBaseLayerDrawCoordinatesFromGridUnits(int gridX, int gridY)
        {
            return CalculateDrawCoordinatesFromGridUnits(gridX, gridY) -
                new Vector2(IGridDrawCoordinateCalculator.DefaultGridWidth / 2, 0);
        }

        public Vector2 CalculateBaseLayerDrawCoordinatesFromGridUnits(MapCoordinate mapCoordinate)
        {
            return CalculateBaseLayerDrawCoordinatesFromGridUnits(mapCoordinate.X, mapCoordinate.Y);
        }

        public Vector2 CalculateGroundLayerRenderTargetDrawCoordinates(bool isMiniMap = false, int tileWidth = 64, int tileHeight = 32)
        {
            var ViewportWidthFactor = _clientWindowSizeProvider.Width / 2 - 1; // 640 * (1/2) - 1
            var ViewportHeightFactor = _clientWindowSizeProvider.Resizable
                ? _clientWindowSizeProvider.Height / 2
                : _clientWindowSizeProvider.Height * 3 / 10 - 2; // 480 * (3/10) - 2

            var rp = _characterProvider.MainCharacter.RenderProperties;
            var cx = isMiniMap ? _characterProvider.MainCharacter.X : rp.MapX;
            var cy = isMiniMap ? _characterProvider.MainCharacter.Y : rp.MapY;

            var mapHeightPlusOne = _currentMapProvider.CurrentMap.Properties.Height + 1;

            var tileWidthFactor = tileWidth / 2;
            var tileHeightFactor = tileHeight / 2;

            var walkAdjustOffsets = isMiniMap ? Vector2.Zero : GetMainCharacterWalkAdjustOffsets();

            // opposite of the algorithm for rendering the base layers
            return new Vector2(ViewportWidthFactor - (mapHeightPlusOne * tileWidthFactor) + (cy * tileWidthFactor) - (cx * tileWidthFactor),
                               ViewportHeightFactor - (cy * tileHeightFactor) - (cx * tileHeightFactor)) - walkAdjustOffsets;
        }

        public Vector2 CalculateDrawCoordinates(DomainNPC npc)
        {
            // With camera system, NPC renders at its world position
            // The camera transform handles the relative positioning
            var worldX = _renderOffsetCalculator.CalculateOffsetX(npc);
            var worldY = _renderOffsetCalculator.CalculateOffsetY(npc) + 16;

            return new Vector2(worldX, worldY);
        }

        public MapCoordinate CalculateGridCoordinatesFromDrawLocation(Vector2 drawLocation)
        {
            // Camera-based approach: Convert screen to world, then reverse isometric projection
            // Previously: complex manual calculations with viewport factors and character offsets
            // Now: camera handles the viewport transformation, we just reverse the isometric math
            var worldPos = _camera.ScreenToWorld(drawLocation);

            // Reverse isometric projection: solve for grid X,Y from world X,Y
            // Given: worldX = gridX * 32 - gridY * 32
            //        worldY = gridX * 16 + gridY * 16
            // Solution: gridX = (worldX + 2 * worldY) / 64
            //           gridY = (2 * worldY - worldX) / 64
            var gridX = (int)Math.Round((worldPos.X + 2 * worldPos.Y) / 64.0);
            var gridY = (int)Math.Round((2 * worldPos.Y - worldPos.X) / 64.0);

            return new MapCoordinate(gridX, gridY);
        }

        private Vector2 GetMainCharacterOffsets()
        {
            var props = _characterProvider.MainCharacter.RenderProperties;
            return new Vector2(_renderOffsetCalculator.CalculateOffsetX(props),
                               _renderOffsetCalculator.CalculateOffsetY(props));
        }

        private Vector2 GetMainCharacterWalkAdjustOffsets()
        {
            var props = _characterProvider.MainCharacter.RenderProperties;
            return new Vector2(_renderOffsetCalculator.CalculateWalkAdjustX(props),
                               _renderOffsetCalculator.CalculateWalkAdjustY(props));
        }
    }

    public interface IGridDrawCoordinateCalculator
    {
        const int DefaultGridWidth = 64;
        const int DefaultGridHeight = 32;

        Vector2 CalculateRawRenderCoordinatesFromGridUnits(int gridX, int gridY, int tileWidth = DefaultGridWidth, int tileHeight = DefaultGridHeight);

        Vector2 CalculateDrawCoordinatesFromGridUnits(int gridX, int gridY);

        Vector2 CalculateDrawCoordinatesFromGridUnits(MapCoordinate mapCoordinate);

        Vector2 CalculateBaseLayerDrawCoordinatesFromGridUnits(int gridX, int gridY);

        Vector2 CalculateBaseLayerDrawCoordinatesFromGridUnits(MapCoordinate mapCoordinate);

        Vector2 CalculateGroundLayerRenderTargetDrawCoordinates(bool isMiniMap = false, int tileWidth = DefaultGridWidth, int tileHeight = DefaultGridHeight);

        Vector2 CalculateDrawCoordinates(DomainNPC npc);

        MapCoordinate CalculateGridCoordinatesFromDrawLocation(Vector2 drawLocation);
    }
}
