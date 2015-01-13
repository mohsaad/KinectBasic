using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Kinect;
using System.Diagnostics;

namespace KinectCameraBasic
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // sees if depth or not
        private bool isDepth;

        // initialize sensor
        private KinectSensor sensor;

        // bitmap that holds color info
        private WriteableBitmap colorBitmap;

        private WriteableBitmap depthBitmap;

        // intermediate storage for color data
        private byte[] colorPixels;

        private byte[] depthColorPixels;

        // intermediate storage for depth data
        private DepthImagePixel[] depthPixels;

        // when window loads, what happens?
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Sensor count: " + KinectSensor.KinectSensors.Count);
            Debug.WriteLine("Sensor status: " + KinectSensor.KinectSensors[0].Status);
            // go through each potential sensor, assign to sensor
            foreach (var aSensor in KinectSensor.KinectSensors)
            {
                if (aSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = aSensor;
                    break;
                }
                
            }

            if (this.sensor != null)
            {
                enableDepthStream();
                enableRGBStream();
                
                
                // start
                try
                {
                    this.sensor.Start();
                    this.status.Text = "Ready";
                }
                catch (IOException)
                {
                    this.sensor = null;
                }

            }

            if (null == this.sensor)
            {
                this.status.Text = "Not Ready";
            }



        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }



        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame cF = e.OpenColorImageFrame())
            {
                if (cF != null)
                {
                    // copy pixel data to temp array
                    cF.CopyPixelDataTo(this.colorPixels);

                    // write data to bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                            this.colorPixels,
                            this.colorBitmap.PixelWidth * sizeof(int),
                            0);
                }
            }
        }

        

        private void enableRGBStream()
        {
            // turn on color stream, recieve color frames
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            // allocate space for pixels from camera
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            // bitmap to display
            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set image we display to point to the bitmap where we will put 
            this.disp.Source = colorBitmap;

            // add an event handler to be called when there is new color frame data
            this.sensor.ColorFrameReady += this.SensorColorFrameReady;

            // not a depth stream
            this.isDepth = false;

        }

        

        private void enableDepthStream()
        {
            // turn on depth stream
            this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            
            // allocate space to put depth pixels
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            // allocate space to put color pixels
            this.depthColorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];

            // This is the bitmap we'll display on-screen
            this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Set the image we display to point to the bitmap where we'll put the image data
            this.dispDepth.Source = this.depthBitmap;

            // Add an event handler to be called whenever there is new depth frame data
            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;

            // is a depth stream
            this.isDepth = true;

      
        }

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame dF = e.OpenDepthImageFrame())
            {
                // copy pixel data to temp array
                dF.CopyDepthImagePixelDataTo(this.depthPixels);

                // max and min depths
                int minDepth = dF.MinDepth;
                int maxDepth = dF.MaxDepth;

                // convert depth to RGB
                int colorPixIndex = 0;
                for (int i = 0; i < this.depthPixels.Length; ++i)
                {
                    // get depth for pixel
                    short depth = depthPixels[i].Depth;
                    
                    // blah blah blah, throw away samples
                    byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                    // write out blue byte
                    this.depthColorPixels[colorPixIndex++] = intensity;
                    // write out green byte
                    this.depthColorPixels[colorPixIndex++] = intensity;
                    // write out red byte
                    this.depthColorPixels[colorPixIndex++] = intensity;

                    // last byte unused, so iterate over it
                    ++colorPixIndex;

                }

                // write to bitmap
                this.depthBitmap.WritePixels(
                    new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                    this.depthColorPixels,
                    this.depthBitmap.PixelWidth * sizeof(int),
                    0);

            }
        }

    }
}
