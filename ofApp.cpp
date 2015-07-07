#include "ofApp.h"

using namespace WindowsPreview::Kinect;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::Storage::Streams;



static byte* GetPointerToPixelData(IBuffer^ pBuffer)
{
	// Query the IBufferByteAccess interface.
	Microsoft::WRL::ComPtr<IBufferByteAccess> spBufferByteAccess;
	reinterpret_cast<IInspectable*>(pBuffer)->QueryInterface(IID_PPV_ARGS(&spBufferByteAccess));

	// Retrieve the buffer data.
	byte* pixels = nullptr;
	spBufferByteAccess->Buffer(&pixels);

	return pixels;
}
//--------------------------------------------------------------

void ofApp::setup(){

	// get the kinectSensor object
	sensor = KinectSensor::GetDefault();


	// open the sensor
	sensor->Open();


	//open the readers
	depthReader = sensor->DepthFrameSource->OpenReader();
	colorReader = sensor->ColorFrameSource->OpenReader();
	bodyReader = sensor->BodyFrameSource->OpenReader();
	infraReader = sensor->InfraredFrameSource->OpenReader();

	//Frame description for the color..
	FrameDescription^ colorFrameDescription = sensor->ColorFrameSource->FrameDescription;
	txtColor.allocate(colorFrameDescription->Width, colorFrameDescription->Height, GL_RGBA);

	//Allocate Color data frame array
	colorDataFrame = ref new Platform::Array<unsigned char>(colorFrameDescription->Width * colorFrameDescription->Height * 4);

	//Allocate InfraRed data
	IRDataFrame = ref new Platform::Array<unsigned short>(520 * 424);

	//framedescription for the depth..
	depthFrameDescription = sensor->DepthFrameSource->FrameDescription;
	//allocate the OFimage for depth
	imgDepth.allocate(depthFrameDescription->Width, depthFrameDescription->Height, ofImageType::OF_IMAGE_GRAYSCALE);

	//ofGetWindowWidth() don't run well on Surface, 
	//windowDepthRatio = (float)ofGetWindowWidth() / (float)depthFrameDescription->Width;
	windowDepthRatio = (float)colorFrameDescription->Width / (float)depthFrameDescription->Width;
	//windowDepthRatio = 3;



	//the body array of bodies in the bodyFrame
	bdyArray = ref new Platform::Collections::Vector <Kinect::Body^>(sensor->BodyFrameSource->BodyCount);

	//sensor-screen mapper
	coordMapper = sensor->CoordinateMapper;

}
//--------------------------------------------------------------

void ofApp::update(){
	if (mode != 4){
		ofSetBackgroundAuto(true);
	}
	if ((mode == 0) || (mode == 2))
		UpdateColorFrame();

	if ((mode == 1) || (mode == 3))
	{
		zoomFactor = windowDepthRatio;
		UpdateDepthFrame();
	}
	if ((mode == 2) || (mode == 4)){
		UpdateBodyFrameColor();
	}

	if ((mode == 3))
	{
		zoomFactor = windowDepthRatio;
		UpdateBodyFrameDepth();
	}
	/*
	if (mode == 10)
	{
		UpdateInfraRedFrame();

	}
	*/
}
//--------------------------------------------------------------

void ofApp::draw(){
	zoomFactor = 1;
	

	if (!sensor->IsAvailable)
	{
		ofDrawBitmapString("No Kinect found :-(", 100, 100);
		return;
	}

	if ((mode == 0) || (mode == 2))
		txtColor.draw(0, 0);

	if ((mode == 1) || (mode == 3))
		imgDepth.draw(0, 0, depthFrameDescription->Width*zoomFactor, depthFrameDescription->Height*zoomFactor);

	if ((mode == 2) || (mode == 3))
	{
		drawJointCircles();
	}
	if (mode == 4){
		drawAlphaCircles(head);
		drawAlphaLines(leftHand, rightHand);
	}
	/*
	if (mode ==10
	{
		txtInfraRed.draw(0, 0);

	}
	*/
	ofDrawBitmapString(ofToString(ofGetFrameRate()) + "fps", 10, 15);
}

//--------------------------------------------------------------
void ofApp::drawJointCircles(){
	ofCircle(rightHand.position.x*zoomFactor, rightHand.position.y*zoomFactor, 20);
	ofSetColor(colorLeftHand);
	ofCircle(leftHand.position.x*zoomFactor, leftHand.position.y*zoomFactor, 20);
	ofSetColor(ofColor::white);
	ofCircle(head.position.x*zoomFactor, head.position.y*zoomFactor, 20);
	ofSetColor(ofColor::black);
	ofDrawBitmapString("you look good :-)", (head.position.x - 70)* zoomFactor, head.position.y*zoomFactor);

	ofSetColor(ofColor::blue);
	ofCircle(thumbRight.position.x*zoomFactor, thumbRight.position.y*zoomFactor, 5);
	ofCircle(handTipRight.position.x*zoomFactor, handTipRight.position.y*zoomFactor, 5);
	ofSetColor(ofColor::white);
}

void ofApp::drawAlphaCircles(MyJoint joint){
	//Todo: Transparente fläche einmal zentral malen
	ofSetColor(0, 0, 0, 5);
	ofRect(0, 0, ofGetWidth(), ofGetHeight());
	ofSetColor(255, 200, 100);
	ofCircle(joint.position.x, joint.position.y, (3 - joint.position.z) * 20);
	ofDrawBitmapString("z: " + ofToString(leftHand.position.z), 10, 35);
}
void ofApp::drawAlphaLines(MyJoint jointA, MyJoint jointB){
	ofSetColor(0, 0, 0, 5);
	ofRect(0, 0, ofGetWidth(), ofGetHeight());
	ofSetColor(255, 200, 100);
	ofLine(jointA.position.x, jointA.position.y, jointB.position.x, jointB.position.y);
	ofDrawBitmapString("z: " + ofToString(leftHand.position.z), 10, 35);
}
//--------------------------------------------------------------
void ofApp::UpdateDepthFrame(){
	DepthFrame^ frame = depthReader->AcquireLatestFrame();
	if (nullptr != frame)
	{
		int nMinDepth = frame->DepthFrameSource->DepthMinReliableDistance;
		int nMaxDepth = frame->DepthFrameSource->DepthMaxReliableDistance;

		IBuffer^ pBuffer = frame->LockImageBuffer();
		UINT16* pSrc = reinterpret_cast<UINT16*>(GetPointerToPixelData(pBuffer));
		const UINT16* pBufferEnd = pSrc + (depthFrameDescription->Width * depthFrameDescription->Height);

		ofColor c;
		for (unsigned int y = 0; y < depthFrameDescription->Height; y++)
		{
			unsigned int x = 0;

			while (x < depthFrameDescription->Width)
			{
				USHORT depth = pSrc[x];
				BYTE intensity = static_cast<BYTE>((depth >= nMinDepth) && (depth <= nMaxDepth) ? (depth % 256) : 0);
				c.r = intensity;
				c.g = intensity;
				c.b = intensity;

				imgDepth.setColor(x, y, c);

				x++;
			}
			pSrc += depthFrameDescription->Width;
		}

		imgDepth.update();
	}
}
//--------------------------------------------------------------

void ofApp::UpdateDepthFrameOld(){
	DepthFrame^ frame = depthReader->AcquireLatestFrame();
	if (nullptr != frame)
	{
		int nMinDepth = frame->DepthFrameSource->DepthMinReliableDistance;
		int nMaxDepth = frame->DepthFrameSource->DepthMaxReliableDistance;

		IBuffer^ pBuffer = frame->LockImageBuffer();
		UINT16* pSrc = reinterpret_cast<UINT16*>(GetPointerToPixelData(pBuffer));
		const UINT16* pBufferEnd = pSrc + (depthFrameDescription->Width * depthFrameDescription->Height);

		int i = 0;
		int x = 0;
		int y = 0;
		ofColor c;

		while (pSrc < pBufferEnd)
		{
			x = i;
			i++;

			if (i >= depthFrameDescription->Width)
			{
				y++;
				x = 0;
				i = 0;
			}
			USHORT depth = *pSrc;
			BYTE intensity = static_cast<BYTE>((depth >= nMinDepth) && (depth <= nMaxDepth) ? (depth % 256) : 0);
			c.r = intensity;
			c.g = intensity;
			c.b = intensity;

			imgDepth.setColor(x, y, c);
			++pSrc;
		}


		imgDepth.update();
	}
}
//--------------------------------------------------------------

void ofApp::UpdateColorFrame(){
	//update the color Frame
	ColorFrame^ frame = colorReader->AcquireLatestFrame();
	if (nullptr != frame)
	{
		frame->CopyConvertedFrameDataToArray(colorDataFrame, ColorImageFormat::Rgba);
		txtColor.loadData(colorDataFrame->Data, frame->FrameDescription->Width, frame->FrameDescription->Height, GL_RGBA);
	}

}
//--------------------------------------------------------------

void ofApp::UpdateBodyFrameDepth()
{
	BodyFrame^ bdyFrame = bodyReader->AcquireLatestFrame();
	if (nullptr != bdyFrame)
	{
		bdyFrame->GetAndRefreshBodyData(bdyArray);
		for each (Body^ body in bdyArray)
		{
			if (body->IsTracked)
			{
				WFC::IMapView<JointType, Joint>^ joints = body->Joints;
				for each(auto joint in joints)
				{
					if (joint->Key == JointType::HandRight)
					{
						CameraSpacePoint position = joint->Value.Position;
						DepthSpacePoint depthSpacePoint = coordMapper->MapCameraPointToDepthSpace(position);
						//body
						
						//ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						rightHand.position.set(depthSpacePoint.X, depthSpacePoint.Y);

					}
					else if (joint->Key == JointType::HandLeft)
					{
						CameraSpacePoint position = joint->Value.Position;
						DepthSpacePoint depthSpacePoint = coordMapper->MapCameraPointToDepthSpace(position);
						leftHand.position.set(depthSpacePoint.X, depthSpacePoint.Y);

					}

					else if (joint->Key == JointType::Head)
					{
						CameraSpacePoint position = joint->Value.Position;
						DepthSpacePoint depthSpacePoint = coordMapper->MapCameraPointToDepthSpace(position);
						head.position.set(depthSpacePoint.X, depthSpacePoint.Y);
					}
				}
			}
		}
	}

}
//--------------------------------------------------------------

void ofApp::UpdateBodyFrameColor()
{
	BodyFrame^ bdyFrame = bodyReader->AcquireLatestFrame();
	if (nullptr != bdyFrame)
	{
		bdyFrame->GetAndRefreshBodyData(bdyArray);
		for each (Body^ body in bdyArray)
		{
			if (body->IsTracked)
			{
				WFC::IMapView<JointType, Joint>^ joints = body->Joints;
				for each(auto joint in joints)
				{
					if (joint->Key == JointType::HandRight)
					{
						CameraSpacePoint position = joint->Value.Position;
						ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						//body
						rightHand.position.set(colorSpacePoint.X, colorSpacePoint.Y);


					}
					else if (joint->Key == JointType::HandLeft)
					{
						CameraSpacePoint position = joint->Value.Position;
						ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						leftHand.position.set(colorSpacePoint.X, colorSpacePoint.Y, position.Z);

					}

					else if (joint->Key == JointType::Head)
					{
						CameraSpacePoint position = joint->Value.Position;
						ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						head.position.set(colorSpacePoint.X, colorSpacePoint.Y, position.Z);

					}
					else if (joint->Key == JointType::HandTipRight)
					{
						CameraSpacePoint position = joint->Value.Position;
						ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						handTipRight.position.set(colorSpacePoint.X, colorSpacePoint.Y);

					}
					else if (joint->Key == JointType::ThumbRight)
					{
						CameraSpacePoint position = joint->Value.Position;
						ColorSpacePoint colorSpacePoint = coordMapper->MapCameraPointToColorSpace(position);
						thumbRight.position.set(colorSpacePoint.X, colorSpacePoint.Y);

					}
					//Karmine fix :-)
					if (body->HandLeftConfidence == TrackingConfidence::High)
					{
						switch (body->HandLeftState)
						{

						case HandState::Open:
							colorLeftHand = ofColor::green;
							break;
						case HandState::Closed:
							colorLeftHand = ofColor::red;
							break;
						case HandState::Lasso:
							colorLeftHand = ofColor::yellow;
							break;
						default:
							colorLeftHand = ofColor::white;
						}
					}
				}
			}
		}
	}

}
//--------------------------------------------------------------
void ofApp::UpdateInfraRedFrame()
{
	//512 x 424, 30 fps
	InfraredFrame^ frame = infraReader->AcquireLatestFrame();
	if (nullptr != frame)
	{
		//frame->CopyFrameDataToArray(IRDataFrame);
		//txtInfraRed.loadData(IRDataFrame->Data, 512, 424, GL_RGBA);

		IBuffer^ buffer = frame->LockImageBuffer();
		UINT16* pSrc = reinterpret_cast<UINT16*>(GetPointerToPixelData(buffer));
		//txtInfraRed.loadData(pSrc, 512, 424, ofImageType::OF_IMAGE_GRAYSCALE);
		txtInfraRed.loadData(pSrc, 512, 424, GL_RG);
	}

}

void ofApp::featureSetup(int _mode){
	switch (_mode){
	case 4:
		ofSetBackgroundAuto(false);
		ofBackground(0, 0, 0);
		ofSetFrameRate(40);
		break;
	default:
		ofSetBackgroundAuto(true);
		
	}
}



void ofApp::keyPressed(int key){

	if (key == ' ')
	{
		//switch
		mode++;

		if (mode >= 5)
			mode = 0;

		featureSetup(mode);

	}

}
//--------------------------------------------------------------

void ofApp::keyReleased(int key){

}

//--------------------------------------------------------------
void ofApp::mouseMoved(int x, int y){

}

//--------------------------------------------------------------
void ofApp::mouseDragged(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::mousePressed(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::mouseReleased(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::windowResized(int w, int h){

}

//--------------------------------------------------------------
void ofApp::gotMessage(ofMessage msg){

}

//--------------------------------------------------------------
void ofApp::dragEvent(ofDragInfo dragInfo){

}