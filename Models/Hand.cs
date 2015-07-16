using System.Windows;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics.Models
{
  public class Hand
  {
    public Hand(Side side, CameraSpacePoint position, TrackingState trackingState, HandState state)
    {
      Side = side;
      Position = position;
      TrackingState = trackingState;
      State = state;
    }

    public Side Side { get; private set; }
    public CameraSpacePoint Position { get; private set; }
    public TrackingState TrackingState { get; private set; }
    public HandState State { get; private set; }
  }
}