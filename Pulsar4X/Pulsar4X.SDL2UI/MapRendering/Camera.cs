﻿using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using Point = SDL2.SDL.SDL_Point;
namespace Pulsar4X.SDL2UI
{


    public class Camera
    {
        internal bool IsGrabbingMap = false;
        internal int MouseFrameIncrementX;
        internal int MouseFrameIncrementY;

        bool IsPinnedToEntity;
        PositionDB _entityPosDB;
        Vector4 _camWorldPos = new Vector4();
        public Vector4 CameraWorldPosition
        {
            get
            {
                if (IsPinnedToEntity && _entityPosDB != null)
                    return _camWorldPos - _entityPosDB.AbsolutePosition;
                else
                    return _camWorldPos;
            }
            set
            {
                if (IsPinnedToEntity)
                {
                    IsPinnedToEntity = false;
                }
                _camWorldPos = value;
            }
        }
                
        //public ImVec2 WorldPosition { get { return _cameraWorldPosition; } }

        public ImVec2 ViewPortCenter { get { return new ImVec2(_viewPort.Size.X * 0.5f, _viewPort.Size.Y * 0.5f); }}

        public ImVec2 ViewPortSize { get { return _viewPort.Size; } }
        public float ZoomLevel { get; set; } = 200;
        public float zoomSpeed { get; set; } = 1.25f;

        public ImGuiSDL2CSWindow _viewPort;

        double MAX_ZOOMLEVEL = 1.496e+11;

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera(ImGuiSDL2CSWindow viewPort)
        {
            _viewPort = viewPort;
            //_viewPort.SizeChanged += _viewPort_SizeChanged;

        }

        public void PinToEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _entityPosDB = entity.GetDataBlob<PositionDB>();
                _camWorldPos = new Vector4(); //zero on it. 
                IsPinnedToEntity = true;
            }
        }
        public void CenterOnEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _camWorldPos = entity.GetDataBlob<PositionDB>().AbsolutePosition;
            }
        }

        public Point CameraViewCoordinate()
        {
            Point point = new Point();
            point.x = (int)(CameraWorldPosition.X * (ZoomLevel) + ViewPortCenter.x);
            point.y = (int)(CameraWorldPosition.Y * (ZoomLevel) + ViewPortCenter.y);
            return point;
        }
        /// <summary>
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(Vector4 worldCoord)
        {
            int x = (int)(worldCoord.X * (ZoomLevel) + ViewPortCenter.x);
            int y = (int)(worldCoord.Y * (ZoomLevel) + ViewPortCenter.y);
            Point viewCoord = new Point() {x = x ,y = y  };

            return viewCoord;
        }



        /// <summary>
        /// returns the worldCoordinate of a given View Coordinate 
        /// </summary>
        /// <param name="viewCoordinate"></param>
        /// <returns></returns>
        public Vector4 WorldCoordinate(int viewCoordinateX, int viewCoordinateY)
        {
            double x = ((viewCoordinateX - ViewPortCenter.X) / ZoomLevel);
            double y = ((viewCoordinateY - ViewPortCenter.Y) / ZoomLevel);
            return new Vector4(x, y, 0, 0);
        }


        /// <summary>
        /// Returns the size of an object in view-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public ImVec2 ViewSize(ImVec2 worldSize)
        {
            ImVec2 viewSize = new ImVec2( worldSize.X * ZoomLevel, worldSize.Y * ZoomLevel);
            return viewSize;
        }

        /// <summary>
        /// Returns the Distance in view-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public float ViewDistance(float dist)
        {
            return dist * ZoomLevel;
        }

        /// <summary>
        /// Returns the Distance in World-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public float WorldDistance(float dist)
        {
            return dist / ZoomLevel;
        }

        /// <summary>
        /// Returns the size of an object in world-Coordinates
        /// </summary>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public ImVec2 WorldSize(ImVec2 viewSize)
        {
            return new ImVec2(viewSize.x / ZoomLevel, viewSize.y / ZoomLevel);
        }


        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// <param name="xOffset">Pans the camera horizontaly relative to offset</param>
        /// <param name="yOffset">Pans the camera verticaly relative to offset</param>
        /// </summary>
        public void WorldOffset(double xOffset, double yOffset)
        {
            _camWorldPos.X -= (float)(xOffset * 1.0f / ZoomLevel);
            _camWorldPos.Y -= (float)(yOffset * 1.0f / ZoomLevel);
        }


        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.x - (mouseX - ViewPortCenter.x) * zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.y - (mouseY - ViewPortCenter.y) * zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }


        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
        public void ZoomOut(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);

            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.x - (mouseX - ViewPortCenter.x) / zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.y - (mouseY - ViewPortCenter.y) / zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }

        /// <summary>
        /// Returns the translation matrix for a world position, relative to the camera position
        /// </summary>
        /// <param name="position">Position in World Units</param>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix(ImVec2 position)
        {
            var transformMatrix = new Matrix();
            double x = CameraWorldPosition.X + position.x;
            double y = CameraWorldPosition.Y + position.y;
            transformMatrix.Translate(x, y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }

        /// <summary>
        /// Returns the translation matrix for 0,0, relative to the camera position
        /// </summary>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix()
        {
            var transformMatrix = new Matrix();

            transformMatrix.Translate(CameraWorldPosition.X, CameraWorldPosition.Y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }
    }
}