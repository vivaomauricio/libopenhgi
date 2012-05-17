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
	public delegate void LeftHandPointUpdatedHandler(object sender, HandPointEventArgs e);
	public delegate void RightHandPointUpdatedHandler(object sender, HandPointEventArgs e);
	
	public delegate void NavigationGestureEventHandler(object sender, NavigationGestureEventArgs e);
	
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
		public event LeftHandPointUpdatedHandler LeftHandPointUpdatedEvent;
		public event RightHandPointUpdatedHandler RightHandPointUpdatedEvent;
		public event NavigationGestureEventHandler NavigationGestureEvent;
		
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
		
		private enum State {STEADY, MOVING, NONE};
		private State state;
		
		private int NiteUser;
		private MovementSpace movementSpace;
		
		private Point3D leftHand;
		private Point3D rightHand;
		
		private Point3D leftHip;
		private Point3D rightHip;
		
		
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
				this.log.setLevelDebug(true);
				
				this.context = Context.CreateFromXmlFile(this.configxml, out this.scriptNode);
				this.depth = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
				this.handsGenerator = this.context.FindExistingNode(NodeType.Hands) as HandsGenerator;
				this.handsGenerator.SetSmoothing(0.1f);
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
				this.sessionManager.SessionFocusProgress +=
					new EventHandler<SessionProgressEventArgs>(sessionManager_sessionFocusProgress);
				
				
				this.steadyDetector.Steady += 
					new EventHandler<SteadyEventArgs>(steadyDetector_steady);
				this.steadyDetector.NotSteady += 
					new EventHandler<SteadyEventArgs>(steadyDetector_moving);
				
				this.sessionManager.AddListener(this.steadyDetector);
				
				this.state = State.NONE;
				this.NiteUser = 1;
				
				this.shouldRun = true;
				this.readThread = new Thread(readerThread);
				this.readThread.Start();
			}
			catch (Exception e)
			{
				this.log.ERROR("libopenhgi", e.Message);
				OnMessageEvent(new MessageEventArgs(e.Message));
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
		
		private bool anyCalib = false;
		void skeletonCapability_calibrationComplete(object sender, CalibrationProgressEventArgs e)
		{
			
			if (e.Status == CalibrationStatus.OK) 
			{
				this.skeletonCapability.StartTracking(e.ID);
				this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
				
				this.log.DEBUG("TRACKER", "calibration OK --> user: " + e.ID);
				this.NiteUser = e.ID;
				
				if (!anyCalib)
				{
					OnMessageEvent(new MessageEventArgs("'Wave' to start session"));
				}
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
			this.poseDetectionCapability.StopPoseDetection(e.ID);
			this.skeletonCapability.RequestCalibration(e.ID, true);
		}
		
		void sessionManager_sessionStart(object sender, PositionEventArgs e)
		{
			this.log.DEBUG("NITE", "session started");	
		}
		
		void sessionManager_sessionEnd(object sender, EventArgs e)
		{
			this.log.DEBUG("NITE", "session ended");
		}
		
		void sessionManager_sessionFocusProgress(object sender, SessionProgressEventArgs e)
		{
			
		}
		
		void steadyDetector_steady(object sender, SteadyEventArgs e)
		{
			
			this.state = State.STEADY;
			
			if (!this.skeletonCapability.IsTracking(e.ID))
				this.log.DEBUG("STEADY", "skeletonCapability is not tracking the user " + e.ID);
			else
			{
				this.log.DEBUG("Tracker", "user: " + e.ID);
				this.NiteUser = e.ID;
			
			
				this.leftHand = updatePoint(joints[e.ID][SkeletonJoint.LeftHand].Position);
				this.rightHand = updatePoint(joints[e.ID][SkeletonJoint.RightHand].Position);
							
				this.leftHip = updatePoint(joints[e.ID][SkeletonJoint.LeftElbow].Position);
				this.rightHip = updatePoint(joints[e.ID][SkeletonJoint.RightElbow].Position);
				
				
				Console.WriteLine("<<<<<<<STEADY");
				
				if ((this.leftHand.Y > this.leftHip.Y) && (this.rightHand.Y > this.rightHip.Y))
				{
					this.movementSpace = new MovementSpace(this.leftHand, this.rightHand);
				} 
				else
				{
					this.movementSpace = null;
				}
			
			}
		}
		
		void steadyDetector_moving(object sender, SteadyEventArgs e)
		{
			Console.WriteLine("<<<<<<<MOVING");
			this.NiteUser = e.ID;
			
			this.state = State.MOVING;
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
						getJoints(user);
						
						this.leftHand = updatePoint(joints[user][SkeletonJoint.LeftHand].Position);
						this.rightHand = updatePoint(joints[user][SkeletonJoint.RightHand].Position);
						this.leftHip = updatePoint(joints[user][SkeletonJoint.LeftHip].Position);
						this.rightHip = updatePoint(joints[user][SkeletonJoint.RightHip].Position);
					
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
				
				bool gesturePose = (this.leftHand.Y > this.leftHip.Y) && (this.rightHand.Y > this.rightHip.Y);
				
				if (this.state == State.MOVING && this.movementSpace != null && gesturePose)
				{
					MovementSpaceCoordinate c = this.movementSpace.calcCoordinate(this.leftHand, this.rightHand);
					OnNavigationGestureEvent(new NavigationGestureEventArgs(c));
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
				log.OffTheRecord("EVENT", "user:" + e.user + " - Looking for pose");
				LookingForPoseEvent(this, e);
			}
		}
		
		protected virtual void OnCalibratingHGIUserEvent(HGIUserEventArgs e)
		{
			if (LookingForPoseEvent != null)
			{
				log.OffTheRecord("EVENT", "user:" + e.user + " - Calibrating");
				LookingForPoseEvent(this, e);
			}
		}
		
		protected virtual void OnUserIsSteady(HGIUserEventArgs e)
		{
			if (UserIsSteadyEvent != null)
			{
				log.DEBUG("EVENT", "user:" + e.user + " - is steady");
				UserIsSteadyEvent(this, e);
			}
		}
		
		protected virtual void OnUserIsNotSteady(HGIUserEventArgs e)
		{
			if (UserIsNotSteadyEvent != null)
			{
				log.DEBUG("EVENT", "user:" + e.user + " - is not steady");
				UserIsNotSteadyEvent(this, e);
			}
		}
		
		protected virtual void OnLeftHandPointUpdatedEvent(HandPointEventArgs e)
		{
			if (LeftHandPointUpdatedEvent != null)
			{
				LeftHandPointUpdatedEvent(this, e);
			}
		}
		
		protected virtual void OnRightHandPointUpdatedEvent(HandPointEventArgs e)
		{
			if (RightHandPointUpdatedEvent != null)
			{
				RightHandPointUpdatedEvent(this, e);
			}
		}
		
		protected virtual void OnNavigationGestureEvent(NavigationGestureEventArgs e)
		{
			if (NavigationGestureEvent != null)
			{
				NavigationGestureEvent(this, e);
			}
		}
		
		private void getJoint(int user, SkeletonJoint joint)
		{
			SkeletonJointPosition pos = this.skeletonCapability.GetSkeletonJointPosition(user, joint);
			if (pos.Position.Z == 0)
			{
				pos.Confidence = 0;
			}
			else
			{
				pos.Position = this.depth.ConvertRealWorldToProjective(pos.Position);
			}
			this.joints[user][joint] = pos;
		}
		
		private void getJoints(int user)
		{	
			getJoint(user, SkeletonJoint.Head);
			getJoint(user, SkeletonJoint.Neck);
			
			getJoint(user, SkeletonJoint.LeftShoulder);
			getJoint(user, SkeletonJoint.LeftElbow);
			getJoint(user, SkeletonJoint.LeftHand);
			
			getJoint(user, SkeletonJoint.RightShoulder);
			getJoint(user, SkeletonJoint.RightElbow);
			getJoint(user, SkeletonJoint.RightHand);
			
			getJoint(user, SkeletonJoint.Torso);
			
			getJoint(user, SkeletonJoint.LeftHip);
            getJoint(user, SkeletonJoint.LeftKnee);
            getJoint(user, SkeletonJoint.LeftFoot);

            getJoint(user, SkeletonJoint.RightHip);
            getJoint(user, SkeletonJoint.RightKnee);
            getJoint(user, SkeletonJoint.RightFoot);
		}
		
		private Point3D updatePoint(Point3D p)
		{
			//p.Y = -p.Y;
			return this.depth.ConvertRealWorldToProjective(p);
		}
	}
}

