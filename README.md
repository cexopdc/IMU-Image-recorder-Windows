# IMU-image-recorder-windows

## The program to run
Please unzip the attached “IMUFrameRecorderRelease.zip” file. The IMUFrameRecorder.exe is a Windows Form Application that records the time-stamped IMU sensor data and camera frames. We need to run the “IMUFrameRecorder.exe” to run the program on a X1 tablet with RealSense Camera. Double click the EXE file, select the desired camera (Intel RealSense Camera RGB in our case) from the pull down menu, and at the bottom the text box should be showing “IMUs are available on the device ” in green. Click “start/stop” to start and stop the recording. 

## The calibration to conduct
We are creating a list of image files and a CSV file containing the IMU measurements. The calibration target is fixed in this calibration and the camera-imu system is moved in front of the target to excite all IMU axes. One sample image is as below:

 

The recording should be 60-80 second sequences for the cam-imu calibration.

The typical motion is (all together is around 40-60 secs):
1.	accelerations: UP-DOWN, LEFT-RIGHT, FORWARD-BACK, 3x each
2.	rotations: roll LEFT-RIGHT, pitch UP-DOWN, pan LEFT-RIGHT, 3x each
3.	some smooth motion with all DOG, e.g. infinity sign while rotating a bit
What is important:
1.	Pattern is always visible
2.	try to excite all IMU axes (rotation and translation)
3.	No shocks to avoid accelerometer spikes; just smooth motion (don’t worry about the beginning/end though, I will crop out the first and the last 5 secs.)

## The calibration target to use

Please print out the attached “april_6x6_50x50cm.pdf” with a 38% percent scale, which will result 0.02m of apriltag size, from edge to edge, and 0.06m of space between adjacent tags. If the scale of the printout is not set to 38%, we need to measure the tag size and the tag space, and update accordingly. 
