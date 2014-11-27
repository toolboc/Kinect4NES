//------------------------------------------------------------------------------
// <copyright file="GestureDetector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Kinect4NES
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Kinect.VisualGestureBuilder;
    using Arduino4Net.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Gesture Detector class which listens for VisualGestureBuilderFrame events from the service
    /// </summary>
    public class GestureDetector : IDisposable
    {
        Arduino board;

        Dictionary<string, Action> gestureActions = new Dictionary<string,Action>();

        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"Database\_PunchOut_.gbd";

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

        /// <summary>
        /// Initializes a new instance of the GestureDetector class along with the gesture frame source and reader
        /// </summary>
        /// <param name="kinectSensor">Active sensor to initialize the VisualGestureBuilderFrameSource object with</param>
        public GestureDetector(KinectSensor kinectSensor, Arduino arduino)
        {
            board = arduino;

            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

            // open the reader for the vgb frames
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                //this.vgbFrameReader.IsPaused = true;
                this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
            }

            // load the gestures from the gesture database
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
            {
                this.vgbFrameSource.AddGestures(database.AvailableGestures);
            }

            InitGestureActions();
        }

        private void Press(params int[] pin)
        {
            for (int i = 0; i < pin.Length; i++)
            {
                board.DigitalWrite(pin[i], DigitalPin.Low);
            }
            Thread.Sleep(60);

            for (int i = 0; i < pin.Length; i++)
            {
                board.DigitalWrite(pin[i], DigitalPin.High);
            }
            Thread.Sleep(60);
        }

        private void Hold(params int[] pin)
        {
            for ( int i = 0 ; i < pin.Length ; i++ )
            {
                board.DigitalWrite(pin[i], DigitalPin.Low);
            }
        }

        private void Release(params int[] pin)
        {
            for (int i = 0; i < pin.Length; i++)
            {
                board.DigitalWrite(pin[i], DigitalPin.High);
            }
        }
        
        private void InitGestureActions()
        {
            gestureActions.Add("Block", () => Hold(NesButtons.Down));
            gestureActions.Add("BodyBlow_Left", () => Press(NesButtons.B));
            gestureActions.Add("BodyBlow_Right", () => Press(NesButtons.A));
            gestureActions.Add("Dodge_Left", () => Press(NesButtons.Left));
            gestureActions.Add("Dodge_Right", () => Press(NesButtons.Right));
            gestureActions.Add("DrinkingWater", () => Press(NesButtons.Select));
            //Double check this later
            gestureActions.Add("Duck",  () => Press(NesButtons.Down, NesButtons.Down));
            gestureActions.Add("HeadBlow_Left", () => Press(NesButtons.Up, NesButtons.B));
            gestureActions.Add("HeadBlow_Right", () => Press(NesButtons.Up, NesButtons.A));
            gestureActions.Add("Uppercut", () => Press(NesButtons.Start));
        }

        /// <summary>
        /// Gets or sets the body tracking ID associated with the current detector
        /// The tracking ID can change whenever a body comes in/out of scope
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
            }
        }

        /// <summary>
        /// Handles gesture detection results arriving from the sensor for the associated body tracking Id
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                    if (discreteResults != null)
                    {
                        Parallel.ForEach(this.vgbFrameSource.Gestures, gesture =>
                        {

                            DiscreteGestureResult result = null;
                            discreteResults.TryGetValue(gesture, out result);

                            if (result.Detected)
                                gestureActions[gesture.Name].Invoke();
                            else if (gesture.Name == "Block")
                                Release(NesButtons.Down);
                        });
                    }
                }
            }
        }
        

        /// <summary>
        /// Handles the TrackingIdLost event for the VisualGestureBuilderSource object
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {

        }
    }
}
