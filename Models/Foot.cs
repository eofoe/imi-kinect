using System.Windows;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics.Models
{
    public class Foot
  {
    public Foot(Side side, CameraSpacePoint position, TrackingState trackingState)
    {
      Side = side;
      Position = position;
      TrackingState = trackingState;
    }

    public Side Side { get; private set; }
    public CameraSpacePoint Position { get; private set; }
    public TrackingState TrackingState { get; private set; }
  }
}