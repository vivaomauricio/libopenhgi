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
	public delegate void LookingForPoseHandler(object sender, HGIUserEventArgs e);
	public delegate void CalibratingHGIUserHandler(object sender, HGIUserEventArgs e);
	public delegate void UserIsSteadyHandler(object sender, HGIUserEventArgs e);
	public delegate void UserIsNotSteadyHandler(object sender, HGIUserEventArgs e);
	
	public class Openhgi
	{
		
		private Logger.Log log;
		
		public event MessageEventHandler MessageEvent;
		public event NewHGIUserHandler NewHGIUserEvent;
		public event LostHGIUserHandler LostHGIUserEvent;
		public event LookingForPoseHandler LookingForPoseEvent;
		public event CalibratingHGIUserHandler CalibratingHGIUserEvent;
		public event UserIsSteadyHandler UserIsSteadyEvent;
		public event UserIsNotSteadyHandler UserIsNotSteadyEvent;
		
		
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
				
				this.sessionManager = new SessionManager(this.context, "Wave", "RaiseHand");
				this.steadyDetector = new SteadyDetector();
				this.steadyDetector.DetectionDuration = 200;
				
				this.sessionManager.SessionStart += 
					new EventHandler<PositionEventArgs>(sessionManager_sessionStart);
				this.sessionManager.SessionEnd +=
					new EventHandler(sessionManager_sessionEnd);
				this.steadyDetector.Steady += 
					new EventHandler<SteadyEventArgs>(steadyDetector_steady);
				this.steadyDetector.NotSteady += 
					new EventHandler<SteadyEventArgs>(steadyDetector_notSteady);
				
				this.sessionManager.AddListener(this.steadyDetector);
				
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
			OnUserIsSteady(new HGIUserEventArgs(e.ID));
		}
		
		void steadyDetector_notSteady(object sender, SteadyEventArgs e)
		{
			OnUserIsNotSteady(new HGIUserEventArgs(e.ID));
		}
		
		private unsafe void readerThread()
		{
			while (this.shouldRun)
			{
				try
				{
					this.context.WaitAndUpdateAll();
					this.sessionManager.Update(this.context);
					this.depthMetaData = this.depth.GetMetaData();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					this.shouldRun = false;
				}
				
				int[] users = this.userGenerator.GetUsers();
				foreach (int user in users)
				{
					if (this.skeletonCapability.IsTracking(user))
					{
						
					} 
					else if (this.skeletonCapability.IsCalibrating(user))
					{
						OnLookingForPoseEvent(new HGIUserEventArgs(user));
					} 
					else
					{
						OnLookingForPoseEvent(new HGIUserEventArgs(user));
					}	
				}
			}
		}
		
		
		protected virtual void OnMessageEvent(MessageEventArgs e)
		{
			if (MessageEvent != null)
			{
				log.DEBUG("EVENT", "raised Message");
				MessageEvent(this, e);
			}
		}
		
		protected virtual void OnNewHGIUserEvent(HGIUserEventArgs e)
		{
			if (NewHGIUserEvent != null)
			{
				log.DEBUG("EVENT", "new user");
				NewHGIUserEvent(this, e);
			}
		}
		
		protected virtual void OnLostHGIUserEvent(HGIUserEventArgs e)
		{
			if (LostHGIUserEvent != null)
			{
				log.DEBUG("EVENT", "lost user");
				LostHGIUserEvent(this, e);
			}
		}
		
		protected virtual void OnLookingForPoseEvent(HGIUserEventArgs e)
		{
			if (LookingForPoseEvent != null)
			{
				log.OffTheRecord("EVENT", "user:" + e.ID + " - Looking for pose");
				LookingForPoseEvent(this, e);
			}
		}
		
		protected virtual void OnCalibratingHGIUserEvent(HGIUserEventArgs e)
		{
			if (LookingForPoseEvent != null)
			{
				log.OffTheRecord("EVENT", "user:" + e.ID + " - Calibrating");
				LookingForPoseEvent(this, e);
			}
		}
		
		protected virtual void OnUserIsSteady(HGIUserEventArgs e)
		{
			if (UserIsSteadyEvent != null)
			{
				log.DEBUG("EVENT", "user:" + e.ID + " - is steady");
				UserIsSteadyEvent(this, e);
			}
		}
		
		protected virtual void OnUserIsNotSteady(HGIUserEventArgs e)
		{
			if (UserIsNotSteadyEvent != null)
			{
				log.DEBUG("EVENT", "user:" + e.ID + " - is not steady");
				UserIsNotSteadyEvent(this, e);
			}
		}
	}
}

