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
	
	
	public delegate void NavigationSessionStartHandler(object sender, HGIUserEventArgs e);
	public delegate void NavigationSessionEndHandler(object sender, HGIUserEventArgs e);
	public delegate void NavigationGestureEventHandler(object sender, NavigationGestureEventArgs e);
	
	public delegate void PointingCoordinatesHandler(object sender, HandPointEventArgs e);
	
	
	
	
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
		
		public event NavigationGestureEventHandler NavigationGestureEvent;
		public event NavigationSessionStartHandler NavigationSessionStartEvent;
		public event NavigationSessionEndHandler NavigationSessionEndEvent;
		
		public event PointingCoordinatesHandler PointingCoordinatesEvent;
		
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
		
		public enum GestureSessionState {NAVIGATION, POINTING, NONE}
		public GestureSessionState gestureSessionState;
		public GestureSessionState lastGestureSessionState;
		
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
				this.gestureSessionState = GestureSessionState.NONE;
				this.lastGestureSessionState = GestureSessionState.NONE;
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
		
		Boolean niteSession = false;
		void sessionManager_sessionStart(object sender, PositionEventArgs e)
		{
			this.log.DEBUG("NITE", "session started");
			this.niteSession = true;
		}
		
		void sessionManager_sessionEnd(object sender, EventArgs e)
		{
			this.log.DEBUG("NITE", "session ended");
			this.niteSession = false;
		}
		
		void sessionManager_sessionFocusProgress(object sender, SessionProgressEventArgs e)
		{
			
		}
		
		void updateGestureSessionState()
		{
			
			
			if (this.leftHand.Y > this.leftHip.Y && this.rightHand.Y > this.leftHip.Y)
			{
				this.gestureSessionState = GestureSessionState.NAVIGATION;
			}
			else if (this.leftHand.Y < this.leftHip.Y && this.rightHand.Y > this.leftHip.Y)
			{
				this.gestureSessionState = GestureSessionState.POINTING;
			}
			else
			{
				this.gestureSessionState = GestureSessionState.NONE;
			}
		}
		
		void steadyDetector_steady(object sender, SteadyEventArgs e)
		{
			
			this.state = State.STEADY;
			
			log.DEBUG("STEADY", "\t\t\t <> " + this.gestureSessionState);		
			
			if (this.gestureSessionState == GestureSessionState.NAVIGATION)
			{
				if (this.movementSpace == null)
				{
					this.movementSpace = new MovementSpace(this.leftHand, this.rightHand);
					OnNavigationSessionStartEvent(new HGIUserEventArgs(1));
				}
				this.lastGestureSessionState = GestureSessionState.NAVIGATION;			
			} 
			else if (this.gestureSessionState == GestureSessionState.POINTING)
			{
				this.movementSpace = null;
				this.lastGestureSessionState = GestureSessionState.POINTING;
				
			}
			else {
				
				this.lastGestureSessionState = GestureSessionState.NONE;
				this.movementSpace = null;
			}
		}
		
		void steadyDetector_moving(object sender, SteadyEventArgs e)
		{
			this.state = State.MOVING;
			log.DEBUG("MOVING","\t\t\t <> " + this.gestureSessionState);	
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
						
						this.NiteUser = user;
						
						getJoints(user);
						
						this.leftHand = updatePoint(joints[1][SkeletonJoint.LeftHand].Position);
						this.rightHand = updatePoint(joints[1][SkeletonJoint.RightHand].Position);
						this.leftHip = updatePoint(joints[1][SkeletonJoint.LeftHip].Position);
						this.rightHip = updatePoint(joints[1][SkeletonJoint.RightHip].Position);
					
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
				
				if (this.niteSession)
				{
					this.updateGestureSessionState();
					if (this.gestureSessionState == GestureSessionState.NAVIGATION)
					{
						if (this.movementSpace != null)
						{
							MovementSpaceCoordinate c;
							c = this.movementSpace.calcCoordinate(this.leftHand, this.rightHand);
						
							if (c == null)
							{
								this.movementSpace = null;
								OnNavigationSessionEndEvent(new HGIUserEventArgs(1));
							}
							else
							{
								OnNavigationGestureEvent(new NavigationGestureEventArgs(c));
							}
						}
						
					}
					else if (this.gestureSessionState == GestureSessionState.POINTING)
					{
						OnPointingCoordinatesEvent(new HandPointEventArgs(1, (int) this.rightHand.X, (int) this.rightHand.Y, (int) this.rightHand.Z));
					}
					
					
					if (this.leftHand.X > this.rightHip.X
					    && this.rightHand.X < this.leftHip.X)
					{
						Environment.Exit(0);
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
		
		protected virtual void OnNavigationGestureEvent(NavigationGestureEventArgs e)
		{
			if (NavigationGestureEvent != null)
			{
				NavigationGestureEvent(this, e);
			}
		}
		
		protected virtual void OnNavigationSessionStartEvent(HGIUserEventArgs e)
		{
			if (NavigationSessionStartEvent != null)
			{
				NavigationSessionStartEvent(this, e);
				log.toFile("NAVIGATION", "Session Started");
			}
		}
		
		protected virtual void OnNavigationSessionEndEvent(HGIUserEventArgs e)
		{
			if (NavigationSessionEndEvent != null)
			{
				NavigationSessionEndEvent(this, e);
				log.toFile("NAVIGATION", "Session Ended");
			}
		}
		
		protected virtual void OnPointingCoordinatesEvent(HandPointEventArgs e)
		{
			if (PointingCoordinatesEvent != null)
			{
				PointingCoordinatesEvent(this, e);
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

