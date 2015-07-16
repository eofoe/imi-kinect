//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.BodyBasics.Models;

namespace Microsoft.Samples.Kinect.BodyBasics
{
  /// <summary>
  ///   Interaction logic for MainWindow
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    /// <summary>
    ///   Radius of drawn hand circles
    /// </summary>
    private const double HandSize = 30;

    /// <summary>
    ///   Thickness of drawn joint lines
    /// </summary>
    private const double JointThickness = 3;

    /// <summary>
    ///   Thickness of clip edge rectangles
    /// </summary>
    private const double ClipBoundsThickness = 10;

    /// <summary>
    ///   Constant for clamping Z values of camera space points from being negative
    /// </summary>
    private const float InferredZPositionClamp = 0.1f;

    private readonly List<FlashLine> _lines = new List<FlashLine>();
    private readonly List<DrawingPoint> _handPoints = new List<DrawingPoint>();
    private readonly List<List<DrawingPoint>> _handPointsSeperated = new List<List<DrawingPoint>>();
    private readonly List<DrawingPoint> _footPoints = new List<DrawingPoint>();
    private readonly Random _random = new Random();

    /// <summary>
    ///   List of colors for each body tracked
    /// </summary>
    private readonly List<Pen> bodyColors;

    /// <summary>
    ///   definition of bones
    /// </summary>
    private readonly List<Tuple<JointType, JointType>> bones;

    /// <summary>
    ///   Coordinate mapper to map one type of point to another
    /// </summary>
    private readonly CoordinateMapper coordinateMapper;

    /// <summary>
    ///   Height of display (depth space)
    /// </summary>
    private readonly int displayHeight;

    /// <summary>
    ///   Width of display (depth space)
    /// </summary>
    private readonly int displayWidth;

    /// <summary>
    ///   Drawing group for body rendering output
    /// </summary>
    private readonly DrawingGroup drawingGroup;

    /// <summary>
    ///   Brush used for drawing hands that are currently tracked as closed
    /// </summary>
    private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

    /// <summary>
    ///   Brush used for drawing hands that are currently tracked as in lasso (pointer) position
    /// </summary>
    private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

    /// <summary>
    ///   Brush used for drawing hands that are currently tracked as opened
    /// </summary>
    private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

    /// <summary>
    ///   Drawing image that we will display
    /// </summary>
    private readonly DrawingImage imageSource;

    /// <summary>
    ///   Pen used for drawing bones that are currently inferred
    /// </summary>
    private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

    /// <summary>
    ///   Brush used for drawing joints that are currently inferred
    /// </summary>
    private readonly Brush inferredJointBrush = Brushes.Yellow;

    /// <summary>
    ///   Brush used for drawing joints that are currently tracked
    /// </summary>
    private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

    private BodyFrameStreamService _bodyFrameStreamService;

    /// <summary>
    ///   Array for the bodies
    /// </summary>
    private Body[] bodies;

    /// <summary>
    ///   Reader for body frames
    /// </summary>
    private BodyFrameReader bodyFrameReader;

    /// <summary>
    ///   Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor;

    /// <summary>
    ///   Current status text to display
    /// </summary>
    private string statusText;

    public MainWindow()
    {
      // one sensor is currently supported
      kinectSensor = KinectSensor.GetDefault();

      // get the coordinate mapper
      coordinateMapper = kinectSensor.CoordinateMapper;

      // get the depth (display) extents
      var frameDescription = kinectSensor.DepthFrameSource.FrameDescription;

      // get size of joint space
      displayWidth = frameDescription.Width;
      displayHeight = frameDescription.Height;

      // open the reader for the body frames
      bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

      // a bone defined as a line between two joints
      bones = new List<Tuple<JointType, JointType>>();

      // Torso
      bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
      bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

      // Right Arm
      bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

      // Left Arm
      bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

      // Right Leg
      bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
      bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

      // Left Leg
      bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
      bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

      // populate body colors, one for each BodyIndex
      bodyColors = new List<Pen>();

      bodyColors.Add(new Pen(Brushes.Red, 6));
      bodyColors.Add(new Pen(Brushes.Orange, 6));
      bodyColors.Add(new Pen(Brushes.Green, 6));
      bodyColors.Add(new Pen(Brushes.Blue, 6));
      bodyColors.Add(new Pen(Brushes.Indigo, 6));
      bodyColors.Add(new Pen(Brushes.Violet, 6));

      // set IsAvailableChanged event notifier
      kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

      // open the sensor
      kinectSensor.Open();

      // set the status text
      StatusText = kinectSensor.IsAvailable
        ? Properties.Resources.RunningStatusText
        : Properties.Resources.NoSensorStatusText;

      // Create the drawing group we'll use for drawing
      drawingGroup = new DrawingGroup();

      // Create an image source that we can use in our image control
      imageSource = new DrawingImage(drawingGroup);

      // use the window object as the view model in this simple example
      DataContext = this;

      // initialize the components (controls) of the window
      InitializeComponent();
    }

    public ImageSource ImageSource
    {
      get { return imageSource; }
    }

    public string StatusText
    {
      get { return statusText; }

      set
      {
        if (statusText != value)
        {
          statusText = value;

          // notify any bound elements that the text has changed
          if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
          }
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      //if (bodyFrameReader != null)
      //{
      //  bodyFrameReader.FrameArrived += Reader_FrameArrived;
      //}

      _bodyFrameStreamService = new BodyFrameStreamService(kinectSensor);
      //_bodyFrameStreamService.HandPairMovements.Subscribe(DrawHandPairScene);
      //_bodyFrameStreamService.FeetMovements.Subscribe(DrawFootScene);
      _bodyFrameStreamService.HandMovements.Subscribe(DrawHandScene);
    }

    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
      if (bodyFrameReader != null)
      {
        // BodyFrameReader is IDisposable
        bodyFrameReader.Dispose();
        bodyFrameReader = null;
      }

      if (kinectSensor != null)
      {
        kinectSensor.Close();
        kinectSensor = null;
      }
    }

    private void DrawHandPairScene(IEnumerable<Tuple<Hand, Hand>> hands)
    {
      if (!hands.Any())
      {
        return;
      }


      using (var dc = drawingGroup.Open())
      {
        // Draw a transparent background to set the render size
        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, displayWidth, displayHeight));
        foreach (var pair in hands)
        {
          var leftHand = ProjectTo2D(pair.Item1.Position);
          var rightHand = ProjectTo2D(pair.Item2.Position);

          if (BothHandsAreClosed(pair.Item1.State, pair.Item2.State))
          {
            DrawLineBetweenHandsWhenVisible(leftHand, rightHand, dc);

            var otherPair =
              hands.Except(new[] {pair}).FirstOrDefault(p => BothHandsAreClosed(p.Item1.State, p.Item2.State));
            if (otherPair != null)
            {
              DrawLineBetweenHandsWhenVisible(leftHand, ProjectTo2D(otherPair.Item1.Position), dc);
              DrawLineBetweenHandsWhenVisible(rightHand, ProjectTo2D(otherPair.Item2.Position), dc);
            }
          }
        }

        foreach (var line in _lines.ToArray())
        {
          line.Alpha -= 10;


          var pen = new Pen
          {
            Brush = new SolidColorBrush(new Color {A = line.Alpha, R = 255, G = 255, B = 255})
          };


          line.Left = new Point(line.Left.X, line.Left.Y + _random.Next(1, 3));
          line.Right = new Point(line.Right.X, line.Right.Y + _random.Next(1, 3));

          dc.DrawLine(pen, line.Left, line.Right);


          if (line.Alpha < 11)
          {
            _lines.Remove(line);
          }
        }
      }
      // prevent drawing outside of our render area
      drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
    }

    private void DrawHandScene(IEnumerable<Hand> hands)
    {
        if (!hands.Any())
        {
            return;
        }


        using (var dc = drawingGroup.Open())
        {
            // Draw a transparent background to set the render size
            dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, displayWidth, displayHeight));
            int handIndex = 0;
            foreach (var hand in hands)
            {
                var currHand = ProjectTo2D(hand.Position);

                DrawDotOnHandWhenVisible(currHand, dc, handIndex);
                handIndex++;
            }

            foreach (var handPoints in _handPointsSeperated)
            {

                DrawingPoint[] d_arr = handPoints.ToArray();

                foreach (var point in d_arr)
                {
                    if (point.Point.X>0 && point.Point.Y>0)
                    {
                        //----------------------get kinetic energy
                        if (d_arr[d_arr.Length - 1].Point.X > 0 
                            && d_arr[d_arr.Length - 1].Point.Y > 0
                            && d_arr[d_arr.Length - 1].Point.X < Double.PositiveInfinity
                            && d_arr[d_arr.Length - 1].Point.Y < Double.PositiveInfinity
                            && d_arr[d_arr.Length - 1].Point.X > Double.NegativeInfinity
                            && d_arr[d_arr.Length - 1].Point.Y > Double.NegativeInfinity)
                        {
                            double energy_x = 0;
                            double energy_y = 0;

                            Point p_last = d_arr[d_arr.Length - 1].Point;

                            energy_x = Math.Abs(p_last.X - point.Point.X);
                            energy_y = Math.Abs(p_last.Y - point.Point.Y);
                            if (energy_x > energy_y)
                            {
                                point.Energy = energy_x;
                                point.Direction = 'x';
                            }
                            else
                            {
                                point.Energy = energy_y;
                                point.Direction = 'y';
                            }
                        }

                        //---------------------------------------------

                        point.Alpha -= 10;

                        Brush br = new SolidColorBrush(point.Color);

                        var pen = new Pen();


                        point.Point = new Point(point.Point.X, point.Point.Y + _random.Next(1, 3));
                        dc.DrawEllipse(br, pen, point.Point, point.Thickness, point.Thickness);
                        //point.Point = new Point(point.Point.X, point.Point.Y);

                        //EllipseGeometry ellipse = new EllipseGeometry

                        if (point.Direction == 'x' && point.Energy > 2 && point.Energy < 100) dc.DrawEllipse(br, pen, point.Point, point.Energy, point.Thickness);
                        else if (point.Direction == 'y' && point.Energy > 2 && point.Energy < 100) dc.DrawEllipse(br, pen, point.Point, point.Thickness, point.Energy);
                        else dc.DrawEllipse(br, pen, point.Point, point.Thickness, point.Thickness);
                


                        if (point.Alpha < 11)
                        {
                            handPoints.Remove(point);
                        }
                    }
                
                }
            }
        }
        // prevent drawing outside of our render area
        drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
    }

    private void DrawFootScene(IEnumerable<Foot> feet)
    {
        if (!feet.Any())
        {
            return;
        }


        using (var dc = drawingGroup.Open())
        {
            // Draw a transparent background to set the render size
            dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, displayWidth, displayHeight));
            foreach (var foot in feet)
            {
                var currFoot = ProjectTo2D(foot.Position);

                DrawDotOnFootWhenVisible(currFoot, dc);

            }

            foreach (var point in _footPoints.ToArray())
            {
                point.Alpha -= 10;

                Brush br = new SolidColorBrush(point.Color);

                var pen = new Pen();


                point.Point = new Point(point.Point.X, point.Point.Y + _random.Next(1, 3));

                dc.DrawEllipse(br, pen, point.Point, point.Thickness, point.Thickness);


                if (point.Alpha < 11)
                {
                    _footPoints.Remove(point);
                }
            }
        }
        // prevent drawing outside of our render area
        drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
    }

    private Point ProjectTo2D(CameraSpacePoint point3D)
    {
      var position = point3D;
      if (position.Z < 0)
      {
        position.Z = InferredZPositionClamp;
      }

      var depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
      var point2D = new Point(depthSpacePoint.X, depthSpacePoint.Y);
      return point2D;
    }

    private bool BothHandsAreClosed(HandState left, HandState right)
    {
      return (left == HandState.Closed && right == HandState.Closed);
    }

    private void DrawDotOnHandWhenVisible(Point currentHand, DrawingContext dc, int handIndex)
    {
        Random rnd = new Random();
        //---------------------random Thickness
        double thickness = rnd.Next(5);
        //---------------------random Color
        byte r = (byte)rnd.Next(255);
        byte g = (byte)rnd.Next(255);
        byte b = (byte)rnd.Next(255);

        Color rndColor = new Color { A = 255, R = r, G = g, B = b };

        var point = new DrawingPoint(currentHand, rndColor, thickness);

        List<DrawingPoint>[] temp = _handPointsSeperated.ToArray(); 
        if (currentHand.X > Double.NegativeInfinity && currentHand.Y > Double.NegativeInfinity
            && currentHand.X < Double.PositiveInfinity && currentHand.Y < Double.PositiveInfinity)
        {
            if (temp.Length <= handIndex)
            {
                List<DrawingPoint> tempList = new List<DrawingPoint>();
                tempList.Add(point);
                _handPointsSeperated.Add(tempList);
            }
            else
            {

                temp[handIndex].Add(point);
            }
            //_handPoints.Add(point);
        }

        Brush br = new SolidColorBrush(rndColor);

        var pen = new Pen(br, 1.0);

        dc.DrawEllipse(br, pen, currentHand, thickness, thickness);
    }

    private void DrawDotOnFootWhenVisible(Point currentFoot, DrawingContext dc)
    {
        Random rnd = new Random();
        //---------------------random Thickness
        double thickness = rnd.Next(5);
        thickness += 3;
        //---------------------random Color
        byte r = (byte)rnd.Next(255);
        byte g = (byte)rnd.Next(255);
        byte b = (byte)rnd.Next(255);

        Color rndColor = new Color { A = 255, R = r, G = g, B = b };

        var point = new DrawingPoint(currentFoot, rndColor, thickness);

        _footPoints.Add(point);

        Brush br = new SolidColorBrush(rndColor);

        var pen = new Pen(br, 1.0);

        dc.DrawEllipse(br, pen, currentFoot, thickness, thickness);
    }

    private void DrawLineBetweenHandsWhenVisible(Point leftHand, Point rightHand, DrawingContext dc)
    {
      _lines.Add(new FlashLine(leftHand, rightHand));

      var pen = new Pen(new SolidColorBrush(Colors.Azure), 1.0);

      dc.DrawLine(pen, leftHand, rightHand);
    }

    private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints,
      DrawingContext drawingContext, Pen drawingPen)
    {
      // Draw the bones
      foreach (var bone in bones)
      {
        DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
      }

      // Draw the joints
      foreach (var jointType in joints.Keys)
      {
        Brush drawBrush = null;

        var trackingState = joints[jointType].TrackingState;

        if (trackingState == TrackingState.Tracked)
        {
          drawBrush = trackedJointBrush;
        }
        else if (trackingState == TrackingState.Inferred)
        {
          drawBrush = inferredJointBrush;
        }

        if (drawBrush != null)
        {
          drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
        }
      }
    }

    private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints,
      JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
    {
      var joint0 = joints[jointType0];
      var joint1 = joints[jointType1];

      // If we can't find either of these joints, exit
      if (joint0.TrackingState == TrackingState.NotTracked ||
          joint1.TrackingState == TrackingState.NotTracked)
      {
        return;
      }

      // We assume all drawn bones are inferred unless BOTH joints are tracked
      var drawPen = inferredBonePen;
      if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
      {
        drawPen = drawingPen;
      }

      drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
    }

    private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
    {
      switch (handState)
      {
        case HandState.Closed:
          drawingContext.DrawEllipse(handClosedBrush, null, handPosition, HandSize, HandSize);
          break;

        case HandState.Open:
          drawingContext.DrawEllipse(handOpenBrush, null, handPosition, HandSize, HandSize);
          break;

        case HandState.Lasso:
          drawingContext.DrawEllipse(handLassoBrush, null, handPosition, HandSize, HandSize);
          break;
      }
    }

    private void DrawClippedEdges(Body body, DrawingContext drawingContext)
    {
      var clippedEdges = body.ClippedEdges;

      if (clippedEdges.HasFlag(FrameEdges.Bottom))
      {
        drawingContext.DrawRectangle(
          Brushes.Red,
          null,
          new Rect(0, displayHeight - ClipBoundsThickness, displayWidth, ClipBoundsThickness));
      }

      if (clippedEdges.HasFlag(FrameEdges.Top))
      {
        drawingContext.DrawRectangle(
          Brushes.Red,
          null,
          new Rect(0, 0, displayWidth, ClipBoundsThickness));
      }

      if (clippedEdges.HasFlag(FrameEdges.Left))
      {
        drawingContext.DrawRectangle(
          Brushes.Red,
          null,
          new Rect(0, 0, ClipBoundsThickness, displayHeight));
      }

      if (clippedEdges.HasFlag(FrameEdges.Right))
      {
        drawingContext.DrawRectangle(
          Brushes.Red,
          null,
          new Rect(displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, displayHeight));
      }
    }

    private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
    {
      // on failure, set the status text
      StatusText = kinectSensor.IsAvailable
        ? Properties.Resources.RunningStatusText
        : Properties.Resources.SensorNotAvailableStatusText;
    }

    private class FlashLine
    {
      public FlashLine(Point left, Point right)
      {
        Left = left;
        Right = right;
        Alpha = byte.MaxValue;
      }

      public Point Left { get; set; }
      public Point Right { get; set; }
      public byte Alpha { get; set; }
      public double Thickness { get; set; }
    }

    private class DrawingPoint
    {
        public DrawingPoint(Point point, Color color, double thickness)
        {
            Point = point;
            Alpha = byte.MaxValue;
            Color = color;
            Thickness = thickness;
        }

        public Point Point { get; set; }
        public byte Alpha { get; set; }
        public double Thickness { get; set; }
        public Color Color { get; set; }
        public double Energy { get; set; }
        public char Direction { get; set; }
    }
  }
}