using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using OpenNI;
using NITE;
using Logger;


namespace libopenhgi
{
	
	public delegate void MessageEventHandler(object sender, MessageEventArgs e);
	public delegate void NewHGIUserHandler(object sender, HGIUserEventArgs e);
	public delegate void LostHGIUserHandler(object sender, HGIUserEventArgs e);
	
	
	public class Openhgi
	{
		
		private Logger.Log log;
		
		public event MessageEventHandler MessageEvent;
		public event NewHGIUserHandler NewHGIUserEvent;
		public event LostHGIUserHandler LostHGIUserEvent;
		
		private string configxml;
		private OpenNI.Context context;
		private ScriptNode scriptNode;
		private DepthGenerator depth;
		private DepthMetaData depthMetaData;
		
		private UserGenerator userGenerator;
		private SkeletonCapability skeletonCapability;
		private PoseDetectionCapability poseDetectionCapability;
		private Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;
		private string calibPose;
		
		private SessionManager sessionManager;
		private HandsGenerator handsGenerator;
		private SteadyDetector steadyDetector;
		
		
		public int xRes, yRes;
		
		private Thread readThread;
		private bool shouldRun;
		
		
		public Openhgi (string configxml)
		{
			this.log = Logger.Log.getInstance(@"../../../log");			
			this.configxml = configxml;
		}
		
		public void initTracking()
		{
			try
			{
				this.log.INFO("libopenhgi", "loading config:" + this.configxml);
				
				this.context = Context.CreateFromXmlFile(this.configxml, out this.scriptNode);
				this.depth = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
				this.depthMetaData = new DepthMetaData();
				
				if (this.depth == null)
				{		
					string info = @"Error in "
					                    + Path.GetFullPath(this.configxml)
					                    + ". No depth node found.";
					
					throw new Exception(info);
				}
				
				this.userGenerator = new UserGenerator(this.context);
				this.skeletonCapability = this.userGenerator.SkeletonCapability;
				this.poseDetectionCapability = this.userGenerator.PoseDetectionCapability;
				this.calibPose = this.skeletonCapability.CalibrationPose;
				
				this.userGenerator.NewUser += userGenerator_newUser;
				this.userGenerator.LostUser	+= userGenerator_lostUser;
				this.poseDetectionCapability.PoseDetected
					+= poseDetectionCapability_poseDetected;
				this.skeletonCapability.CalibrationComplete
					+= skeletonCapability_calibrationComplete;
				
				this.skeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
				this.joints = new Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>>();
				this.userGenerator.StartGenerating();
				
				this.xRes = this.depth.MapOutputMode.XRes;
				this.yRes = this.depth.MapOutputMode.YRes;
				
				
				this.shouldRun = true;
				this.readThread = new Thread(readerThread);
				this.readThread.Start();
			}
			catch (Exception e)
			{
				OnMessageEvent(new MessageEventArgs("ITS ALIVE"));
				this.log.ERROR("libopenhgi", e.ToString());
				this.shouldRun = false;
			}
			
		}
		
		void userGenerator_newUser(object sender, NewUserEventArgs e)
		{
			OnNewHGIUserEvent(new HGIUserEventArgs(e.ID));
			if (this.skeletonCapability.DoesNeedPoseForCalibration)
			{
				this.poseDetectionCapability.StartPoseDetection(this.calibPose, e.ID);
			}
			else
			{
				this.skeletonCapability.RequestCalibration(e.ID, true);
			}
			
		}
		
		void userGenerator_lostUser(object sender, UserLostEventArgs e) 
		{
			OnLostHGIUserEvent(new HGIUserEventArgs(e.ID));
			this.joints.Remove(e.ID);
		}
		
		void skeletonCapability_calibrationComplete(object sender, CalibrationProgressEventArgs e)
		{
			this.log.DEBUG("libopenhgi", "calibration complete");
			if (e.Status == CalibrationStatus.OK) 
			{
				this.skeletonCapability.StartTracking(e.ID);
				this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
			}
			else if (e.Status != CalibrationStatus.ManualAbort)
			{
				if (this.skeletonCapability.DoesNeedPoseForCalibration) 
				{
					this.poseDetectionCapability.StartPoseDetection(calibPose, e.ID);
				} 
				else
				{
					this.skeletonCapability.RequestCalibration(e.ID, true);
				}
			}
		}
		
		void poseDetectionCapability_poseDetected(object sender, PoseDetectedEventArgs e)
		{
			this.log.DEBUG("libopenhgi", "pose detected");
			this.poseDetectionCapability.StopPoseDetection(e.ID);
			this.skeletonCapability.RequestCalibration(e.ID, true);
		}
		
		void sessionManager_sessionStart(object sender, PositionEventArgs e)
		{
			
		}
		
		void sessionManager_sessionEnd(object sender, EventArgs e)
		{
			
		}
		
		void steadyDetector_steady(object sender, SteadyEventArgs e)
		{
			
		}
		
		void steadyDetector_notSteady(object sender, SteadyEventArgs e)
		{
			
		}
		
		private unsafe void readerThread()
		{
			while (this.shouldRun)
			{
				try
				{
					
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					this.shouldRun = false;
				}
			}
		}
		
		
		protected virtual void OnMessageEvent(MessageEventArgs e)
		{
			if (MessageEvent != null)
			{
				log.DEBUG("EVENT", "raised Message");
				MessageEvent(this, e);
			} else log.DEBUG("libopenhgi","MessageEvent is null");
		}
		
		protected virtual void OnNewHGIUserEvent(HGIUserEventArgs e)
		{
			if (NewHGIUserEvent != null)
			{
				log.DEBUG("EVENT", "new user");
				NewHGIUserEvent(this, e);
			} else log.DEBUG("libopenhgi","NewHGIUserEvent is null");
		}
		
		protected virtual void OnLostHGIUserEvent(HGIUserEventArgs e)
		{
			if (LostHGIUserEvent != null)
			{
				log.DEBUG("EVENT", "lost user");
				LostHGIUserEvent(this, e);
			} else log.DEBUG("libopenhgi","LostHGIUserEvent is null");
		}
		
	}
}

