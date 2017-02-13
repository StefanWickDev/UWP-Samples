    using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Navigation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ShapeRecognitionUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        InkAnalyzer inkAnalyzer = null;
        Random rnd = new Random(Environment.TickCount);
        Shape movingShape = null;
        Point offset;

        public MainPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            inkAnalyzer = new InkAnalyzer();
        }

        private async void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            inkAnalyzer.AddDataForStrokes(args.Strokes);
            InkAnalysisResult result = await inkAnalyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated && inkAnalyzer.AnalysisRoot.Children.Count > 0)
            {
                if (inkAnalyzer.AnalysisRoot.Children.Last().Kind == InkAnalysisNodeKind.InkDrawing)
                {
                    LinearGradientBrush newRandomBrush = GetRandomGradientBrush();

                    InkAnalysisInkDrawing drawing = inkAnalyzer.AnalysisRoot.Children.Last() as InkAnalysisInkDrawing;
                    if (drawing.DrawingKind == InkAnalysisDrawingKind.Circle ||
                        drawing.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                    {
                        Ellipse ellipse = new Ellipse();
                        AttachDragHandlers(ellipse);
                        ellipse.Fill = newRandomBrush;
                        ellipse.Stroke = new SolidColorBrush(Colors.Black);
                        ellipse.Width = drawing.BoundingRect.Width;
                        ellipse.Height = drawing.BoundingRect.Height;
                        CompositeTransform transform = new CompositeTransform();
                        transform.TranslateX = drawing.BoundingRect.X;
                        transform.TranslateY = drawing.BoundingRect.Y;
                        ellipse.RenderTransform = transform;
                        ellipse.RenderTransformOrigin = new Point(0.5, 0.5);
                        root.Children.Add(ellipse);
                        if (animationToggle.IsChecked == true)
                        {
                            AddAnimation(ellipse);
                        }
                    }
                    else
                    {
                        Polygon polygon = new Polygon();
                        AttachDragHandlers(polygon);
                        polygon.Fill = newRandomBrush;
                        polygon.Stroke = new SolidColorBrush(Colors.Black);
                        foreach (Point pt in drawing.Points)
                        {
                            polygon.Points.Add(pt);
                        }
                        CompositeTransform transform = new CompositeTransform();
                        transform.CenterX = drawing.Center.X;
                        transform.CenterY = drawing.Center.Y;
                        polygon.RenderTransform = transform;
                        root.Children.Add(polygon);
                        if (animationToggle.IsChecked == true)
                        {
                            AddAnimation(polygon);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(inkAnalyzer.AnalysisRoot.Children.Last().Kind.ToString());
                }
            }
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            inkAnalyzer.ClearDataForAllStrokes();
        }

        private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.IsInputEnabled = false;
            inkCanvas.SetValue(Canvas.ZIndexProperty, -1);
        }

        private void AppBarToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.IsInputEnabled = true;
            inkCanvas.SetValue(Canvas.ZIndexProperty, 1);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            root.Children.Clear();
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void shape_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            movingShape = sender as Shape;
            if (movingShape != null)
            {
                movingShape.CapturePointer(e.Pointer);
                PointerPoint currentPoint = e.GetCurrentPoint(root);
                offset = new Point((double)movingShape.GetValue(Canvas.LeftProperty) - currentPoint.Position.X, (double)movingShape.GetValue(Canvas.TopProperty) - currentPoint.Position.Y);
            }
        }

        private void shape_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (movingShape != null)
            {
                PointerPoint currentPoint = e.GetCurrentPoint(root);
                movingShape.SetValue(Canvas.LeftProperty, offset.X + currentPoint.Position.X);
                movingShape.SetValue(Canvas.TopProperty, offset.Y + currentPoint.Position.Y);
            }
        }

        private void shape_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            movingShape = null;
        }

        private LinearGradientBrush GetRandomGradientBrush()
        {
            byte[] bytes1 = new byte[3];
            rnd.NextBytes(bytes1);
            byte[] bytes2 = new byte[3];
            rnd.NextBytes(bytes2);
            GradientStopCollection gradientStops = new GradientStopCollection();
            gradientStops.Add(new GradientStop() { Color = Color.FromArgb(128, bytes1[0], bytes1[1], bytes1[2]), Offset = 0 });
            gradientStops.Add(new GradientStop() { Color = Color.FromArgb(192, bytes2[0], bytes2[1], bytes2[2]), Offset = 1 });
            return new LinearGradientBrush(gradientStops, rnd.Next() * 360);
        }

        private void AttachDragHandlers(Shape shape)
        {
            shape.PointerPressed += shape_PointerPressed;
            shape.PointerMoved += shape_PointerMoved;
            shape.PointerReleased += shape_PointerReleased;
        }

        private void animationToggle_Checked(object sender, RoutedEventArgs e)
        {
            foreach (UIElement element in root.Children)
            {
                Shape shape = element as Shape;
                if (shape != null)
                {
                    AddAnimation(shape);
                }
            }
        }

        private void AddAnimation(Shape shape)
        {
            Storyboard storyboard = shape.Tag as Storyboard;
            if (storyboard != null)
            {
                storyboard.Resume();
            }
            else
            {
                DoubleAnimation angleAnimation = new DoubleAnimation();
                if (rnd.NextDouble() > 0.5d)
                {
                    angleAnimation.From = 0d;
                    angleAnimation.To = 360d;
                }
                else
                {
                    angleAnimation.From = 360d;
                    angleAnimation.To = 0d;
                }
                angleAnimation.AutoReverse = false;
                angleAnimation.Duration = TimeSpan.FromSeconds(3d);
                angleAnimation.RepeatBehavior = RepeatBehavior.Forever;
                storyboard = new Storyboard();
                Storyboard.SetTargetProperty(angleAnimation, "(Shape.RenderTransform).(CompositionTransform.Rotation)");
                Storyboard.SetTarget(angleAnimation, shape);
                storyboard.Children.Add(angleAnimation);
                storyboard.Begin();
                shape.Tag = storyboard;
            }
        }

        private void animationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (UIElement element in root.Children)
            {
                Shape shape = element as Shape;
                if (shape != null)
                {
                    Storyboard storyboard = shape.Tag as Storyboard;
                    if (storyboard != null)
                    {
                        storyboard.Pause();
                    }
                }
            }
        }
    }
}
