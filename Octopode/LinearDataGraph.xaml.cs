using System;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;

namespace Octopode {
    public partial class LinearDataGraph : Window {
        public LinearDataGraph() {
            InitializeComponent();
            Width = 800;
            Height = 600;
            this.DataContext = new MainViewModel();
            Title = "Octopode - RPM by Temperature";
        }
    }


    /// <summary>
    /// Represents the view-model for the main window.
    /// </summary>
    public class MainViewModel {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel() {
            // Create the plot model
            var model = new PlotModel { Title = "Pump RPM by Temperature" };
            model.IsLegendVisible = false;


            // Create two line series (markers are hidden by default)
            var s1 = new LineSeries {
                Title = "Series 1",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromArgb(0, 15, 35, 100),
                MarkerStroke = OxyColor.FromArgb(255, 15, 35, 100),
                MarkerStrokeThickness = 2,
                MarkerSize = 4,
                Color = OxyColor.FromArgb(255, 15, 35, 100)
            };

            s1.Points.Add(new DataPoint(00, 00));
            s1.Points.Add(new DataPoint(10, 10));
            s1.Points.Add(new DataPoint(20, 20));
            s1.Points.Add(new DataPoint(30, 30));
            s1.Points.Add(new DataPoint(40, 40));
            s1.Points.Add(new DataPoint(50, 50));
            s1.Points.Add(new DataPoint(60, 60));
            s1.Points.Add(new DataPoint(70, 70));
            s1.Points.Add(new DataPoint(80, 80));
            s1.Points.Add(new DataPoint(90, 90));
            s1.Points.Add(new DataPoint(100, 100));
            // Add the series to the plot model
            model.Series.Add(s1);

            int indexOfPointToMove = -1;

            // Subscribe to the mouse down event on the line series
            s1.MouseDown += (s, e) => {
                if(e.HitTestResult == null) {
                    return;
                }
                    
                if(e.ChangedButton == OxyMouseButton.Right) {
                    int indexOfNearestPoint = (int) Math.Round(e.HitTestResult.Index);
                    var nearestPoint = s1.Transform(s1.Points[indexOfNearestPoint]);
                    if((nearestPoint - e.Position).Length < 10) {
                        s1.Points.RemoveAt(indexOfNearestPoint);
                    }

                    indexOfPointToMove = -1;
                    model.InvalidatePlot(false);
                    e.Handled = true;
                    
                }

                // only handle the left mouse button (right button can still be used to pan)
                if(e.ChangedButton == OxyMouseButton.Left) {
                    int indexOfNearestPoint = (int) Math.Round(e.HitTestResult.Index);
                    var nearestPoint = s1.Transform(s1.Points[indexOfNearestPoint]);

                    // Check if we are near a point
                    if((nearestPoint - e.Position).Length < 10) {
                        // Start editing this point
                        indexOfPointToMove = indexOfNearestPoint;
                    } else if(s1.Points.Count <= 64) {
                        // otherwise create a point on the current line segment
                        int i = (int) e.HitTestResult.Index + 1;
                        s1.Points.Insert(i, s1.InverseTransform(e.Position));
                        s1.Points.Sort((a, b) => a.X.CompareTo(b.X));
                        indexOfPointToMove = i;
                    }

                    // Change the linestyle while editing
                    s1.LineStyle = LineStyle.DashDot;

                    // Remember to refresh/invalidate of the plot
                    model.InvalidatePlot(false);

                    // Set the event arguments to handled - no other handlers will be called.
                    e.Handled = true;
                }
            };

            s1.MouseMove += (s, e) => {
                if(indexOfPointToMove >= 0) {
                    // Move the point being edited.
                    s1.Points[indexOfPointToMove] = s1.InverseTransform(e.Position);
                    model.InvalidatePlot(false);
                    e.Handled = true;
                }
            };

            s1.MouseUp += (s, e) => {
                // Stop editing
                if(indexOfPointToMove >= 0) {
                    var pt = s1.Points[indexOfPointToMove];
                    var x = Math.Round(pt.X);
                    var y = Math.Round(pt.Y);
                    s1.Points[indexOfPointToMove] = new DataPoint(x, y);
                }

                s1.Points.Sort((a, b) => a.X.CompareTo(b.X));
                indexOfPointToMove = -1;
                s1.LineStyle = LineStyle.Solid;
                model.InvalidatePlot(false);
                e.Handled = true;
            };

            model.MouseDown += (s, e) => {
                if(e.ChangedButton == OxyMouseButton.Left) {
                    // Add a point to the line series.
                    s1.Points.Add(s1.InverseTransform(e.Position));
                    s1.Points.Sort((a, b) => a.X.CompareTo(b.X));
                    indexOfPointToMove = s1.Points.Count - 1;

                    model.InvalidatePlot(false);
                    e.Handled = true;
                }
            };

            // Axes are created automatically if they are not defined

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.Model = model;
        }

        /// <summary>
        /// Gets the plot model.
        /// </summary>
        public PlotModel Model { get; private set; }
    }
}