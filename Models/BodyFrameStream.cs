using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics.Models
{
  internal class BodyFrameStreamService
  {
    private readonly IDisposable _bodyCountChanges;
    private readonly BodyFrameReader _bodyFrameReader;
    private readonly KinectSensor _kinect;
    private readonly IDisposable _kinectIsAvailable;
    private readonly IDisposable _propertyChanged;

    public BodyFrameStreamService(KinectSensor kinect)
    {
      if (kinect == null)
      {
        throw new ArgumentNullException("kinect");
      }

      _kinect = kinect;
      _bodyFrameReader = _kinect.BodyFrameSource.OpenReader();

      // stream of bools (true when enabled, false when off)
      var kinectIsAvailable =
        Observable.FromEventPattern<IsAvailableChangedEventArgs>(h => _kinect.IsAvailableChanged += h,
          h => _kinect.IsAvailableChanged -= h).Select(e => e.EventArgs.IsAvailable);

      var bodyFrameHasArrived =
        Observable.FromEventPattern<BodyFrameArrivedEventArgs>(h => _bodyFrameReader.FrameArrived += h,
          h => _bodyFrameReader.FrameArrived -= h).Select(e => e.EventArgs.FrameReference);

      _kinectIsAvailable = kinectIsAvailable.Subscribe(_ => Console.WriteLine("Available: {0}", _));

      FeetMovements = bodyFrameHasArrived.Select(ExtractBodiesFromFrame).Select(GetFeetFromBodies);
      HandMovements = bodyFrameHasArrived.Select(ExtractBodiesFromFrame).Select(GetHandsFromBodies);
      HandPairMovements = bodyFrameHasArrived.Select(ExtractBodiesFromFrame).Select(GetHandPairsFromBodies);

      if (_kinect.IsAvailable)
      {
        return;
      }

      if (!_kinect.IsOpen)
      {
        _kinect.Open();
      }
    }

    public IObservable<List<Tuple<Hand, Hand>>> HandPairMovements { get; set; }

    public IObservable<List<Hand>> HandMovements { get; set; }

    public IObservable<List<Foot>> FeetMovements { get; set; }

    private static List<Foot> GetFeetFromBodies(Body[] bodies)
    {

        List<Foot> ret = bodies.Select(
            body =>
                new Foot(Side.Left, body.Joints[JointType.FootLeft].Position,
                  body.Joints[JointType.FootLeft].TrackingState)).ToList();
        ret.AddRange(bodies.Select(
            body =>
                new Foot(Side.Right, body.Joints[JointType.FootRight].Position,
                  body.Joints[JointType.FootRight].TrackingState)).ToList());
        return ret;

    }

    private static List<Hand> GetHandsFromBodies(Body[] bodies)
    {

      List<Hand> ret =  bodies.Select(
          body =>
              new Hand(Side.Left, body.Joints[JointType.HandLeft].Position,
                body.Joints[JointType.HandLeft].TrackingState, body.HandLeftState)).ToList();
      ret.AddRange(bodies.Select(
          body =>
              new Hand(Side.Right, body.Joints[JointType.HandRight].Position,
                body.Joints[JointType.HandRight].TrackingState, body.HandRightState)).ToList());
      return ret;
        
    }

    private static List<Tuple<Hand, Hand>> GetHandPairsFromBodies(Body[] bodies)
    {
        return
          bodies.Select(
            body =>
              new Tuple<Hand, Hand>(
                new Hand(Side.Left, body.Joints[JointType.HandLeft].Position,
                  body.Joints[JointType.HandLeft].TrackingState, body.HandLeftState),
                new Hand(Side.Right, body.Joints[JointType.HandRight].Position,
                  body.Joints[JointType.HandRight].TrackingState, body.HandRightState))).ToList();
    }

    private static Body[] ExtractBodiesFromFrame(BodyFrameReference frameReference)
    {
      Body[] bodies;
      using (var frame = frameReference.AcquireFrame())
      {
        if (frame != null)
        {
          bodies = new Body[frame.BodyCount];
          frame.GetAndRefreshBodyData(bodies);
        }
        else
        {
          bodies = new Body[0];
        }
      }
      return bodies;
    }
  }
}