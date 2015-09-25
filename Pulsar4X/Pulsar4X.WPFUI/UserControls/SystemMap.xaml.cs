﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.ViewModels;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemWindow.xaml
    /// </summary>
    public partial class SystemMap : ITabControl
    {
        public string Title { get; set; }
        private SystemVM systemVM;
        private Canvas _canvas;

        private Dictionary<string, int> _canvasItemIndexes = new Dictionary<string, int>(); 

        private double canvasCenterH
        {
            get { return _canvas.ActualHeight / 2; }
        }

        private double canvasCenterW
        {
            get { return _canvas.ActualWidth / 2; }
        }

        private double zoom = 100;

        public SystemMap()
        {
            InitializeComponent();
            Title = "System Map";
            _canvas = MapCanvas;
            SystemSelection.ItemsSource = App.Current.GameVM.StarSystems;
            SystemSelection.DisplayMemberPath = "Name";
            MapCanvas.Background = new SolidColorBrush(Brushes.DarkBlue.Color);

        }

        private void SystemSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            systemVM = (SystemVM)SystemSelection.SelectedItem;
            //systemVM.PropertyChanged += system_PropertyChanged;
            DrawSystem();
        }

        private void planet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PlanetVM planet = (PlanetVM)sender;
            var planetItem = MapCanvas.Children[_canvasItemIndexes[planet.Name]];
            int size = 10;
            Point newPos = GetPosition(planet);
            Canvas.SetLeft(planetItem, newPos.X - size / 2);
            Canvas.SetTop(planetItem, newPos.Y - size / 2);
            MapCanvas.UpdateLayout();
        }

        private Point GetPosition(PlanetVM planet)
        {

            Point parentPos;
            if (planet.ParentPlanet != null)
                parentPos = GetPosition(planet.ParentPlanet);
            else
                parentPos = GetPosition(planet.ParentStar);

            double planetLeftPos = zoom * (parentPos.X + planet.Position.X) + canvasCenterW;
            double planetTopPos = zoom * (parentPos.Y + planet.Position.Y) + canvasCenterH;

            return new Point(planetLeftPos, planetTopPos);
        }

        private Point GetPosition(StarVM star)
        {
            double leftPos = zoom * star.Position.X + canvasCenterW;
            double topPos = zoom * star.Position.Y + canvasCenterH;
            return new Point(leftPos, topPos);
        }

        private void system_PropertyChanged(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            DrawSystem();
        }

        private void DrawSystem()
        {
            if (systemVM == null)
                return;
            MapCanvas.Children.Clear();
            foreach (var star in systemVM.Stars)
            {

                Point starPos = GetPosition(star);
                MapCanvas.Children.Add(DrawBody(20, Brushes.DarkOrange, starPos));
                _canvasItemIndexes.Add(star.Name,MapCanvas.Children.Count);
                foreach (var planet in star.ChildPlanets)
                {

                    Point planetPos = GetPosition(planet);
                    MapCanvas.Children.Add(DrawBody(10, Brushes.DarkGreen, planetPos));
                    _canvasItemIndexes.Add(planet.Name, MapCanvas.Children.Count);
                    DrawOrbit(starPos, planet);
                    planet.PropertyChanged += planet_PropertyChanged;
                    DrawDebugLines(starPos, planetPos, planet);
                }
            }
        }


        private void DrawOrbit(Point parentPosition, PlanetVM planet)
        {

            double arcRotAngle = Angle.ToRadians(planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode); // if inclination is 0
            Point periapsis = new Point(parentPosition.X + Math.Sin(arcRotAngle) * zoom * planet.Periapsis, parentPosition.Y + Math.Cos(arcRotAngle) * zoom * planet.Periapsis);
            Vector tangent = new Vector(Math.Cos(arcRotAngle), -Math.Sin(arcRotAngle));
            Point arcStart = periapsis-tangent;
            Point arcEnd = periapsis+tangent;

            double majorAxis = planet.Periapsis + planet.Apoapsis;
            double minorAxis = Math.Sqrt(1-planet.Eccentricity*planet.Eccentricity) * majorAxis;
            Size arcSize = new Size(zoom * majorAxis / 2, zoom * minorAxis / 2);

            SweepDirection sweepDirection = SweepDirection.Clockwise;

            ArcSegment orbitArc = new ArcSegment(arcEnd, arcSize, planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode, true, sweepDirection, true);

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = arcStart;
            pathFigure.Segments.Add(orbitArc);

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            Path orbitPath = new Path();
            orbitPath.Stroke = Brushes.Cornsilk;
            orbitPath.StrokeThickness = 1;
            orbitPath.Data = pathGeometry;

            MapCanvas.Children.Add(orbitPath);
        }

        private Ellipse DrawBody(int size, Brush color, Point position)
        {
            Ellipse bodyEllipse = new Ellipse();
            //bodyEllipse.Name 
            bodyEllipse.Height = size;
            bodyEllipse.Width = size;
            bodyEllipse.Fill = color;

            
            Canvas.SetLeft(bodyEllipse, position.X - size / 2);
            Canvas.SetTop(bodyEllipse, position.Y - size / 2);

            return bodyEllipse;
        }

        private void DrawDebugLines(Point parentPos, Point position, PlanetVM planet)
        {
            Line trueAnomoly = new Line();
            trueAnomoly.Stroke = Brushes.Magenta;
            trueAnomoly.X1 = parentPos.X;
            trueAnomoly.Y1 = parentPos.Y;
            trueAnomoly.X2 = position.X;
            trueAnomoly.Y2 = position.Y;
            trueAnomoly.StrokeThickness = 1;
            //MapCanvas.Children.Add(trueAnomoly);


            Line periapsis = new Line();
            periapsis.Stroke = Brushes.Cyan;
            periapsis.X1 = parentPos.X;
            periapsis.Y1 = parentPos.Y;
            double arcRotAngle = Angle.ToRadians(planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode);

            periapsis.X2 = parentPos.X + Math.Sin(arcRotAngle) * zoom * planet.Periapsis;
            periapsis.Y2 = parentPos.Y + Math.Cos(arcRotAngle) * zoom * planet.Periapsis;
            periapsis.StrokeThickness = 1;
            MapCanvas.Children.Add(periapsis);


        }
    }
}
