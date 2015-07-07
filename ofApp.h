#pragma once

#include "ofMain.h"

namespace Kinect = WindowsPreview::Kinect;

using namespace Kinect;
namespace WFC = Windows::Foundation::Collections;

struct MyJoint{
	ofPoint position;
	ofColor color;
};

class ofApp : public ofBaseApp{

public:
	void setup();
	void update();
	void draw();

	void keyPressed(int key);
	void keyReleased(int key);
	void mouseMoved(int x, int y);
	void mouseDragged(int x, int y, int button);
	void mousePressed(int x, int y, int button);
	void mouseReleased(int x, int y, int button);
	void windowResized(int w, int h);
	void dragEvent(ofDragInfo dragInfo);
	void gotMessage(ofMessage msg);

	Kinect::KinectSensor^						sensor;
	Kinect::DepthFrameReader^					depthReader;
	Kinect::ColorFrameReader^					colorReader;
	Kinect::InfraredFrameReader^				infraReader;
	Kinect::BodyFrameReader^					bodyReader;
	Kinect::CoordinateMapper^					coordMapper;
	WFC::IVector<Kinect::Body^>^				bdyArray;
	Platform::Array<unsigned char>^				colorDataFrame;
	FrameDescription^							depthFrameDescription;
	Platform::Array<unsigned short>^			IRDataFrame;

	ofTexture									txtColor;
	ofTexture									txtInfraRed;
	ofImage										imgDepth;

	float										windowDepthRatio;
	int											zoomFactor = 1;
	int											mode = 0;


	// should be changed in future to store all of the joints in a list
	ofColor colorLeftHand = ofColor::white;
	MyJoint leftHand;
	MyJoint rightHand;
	MyJoint head;
	MyJoint handTipRight;
	MyJoint thumbRight;


	void UpdateColorFrame();
	void UpdateDepthFrame();
	void UpdateBodyFrameColor();
	void UpdateBodyFrameDepth();
	void UpdateInfraRedFrame();

	// rustic version :-)
	void UpdateDepthFrameOld();

	void featureSetup(int mode);

	void drawJointCircles();
	void drawAlphaCircles(MyJoint);
	void drawAlphaLines(MyJoint, MyJoint);
};