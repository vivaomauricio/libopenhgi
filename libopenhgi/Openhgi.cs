using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using OpenNI;
using Logger;


namespace libopenhgi
{

	
	public delegate void MessageEventHandler(object sender, MessageEventArgs e);
	
	
	
	
	public class Openhgi
	{
		
		private Logger.Log log;
		
		
		public event MessageEventHandler MessageEvent;
		
		
		
		
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
		
		
		public int xRes, yRes;
		
		private Thread readThread;
		private bool shouldRun;
		
		
		public Openhgi (string configxml)
		{
			try
			{
				this.log = Logger.Log.getInstance(@"../../../log");
				
				this.configxml = configxml;
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
				
				this.xRes = this.depth.MapOutputMode.XRes;
				this.yRes = this.depth.MapOutputMode.YRes;
				
				
				this.shouldRun = true;
				this.readThread = new Thread(readerThread);
				this.readThread.Start();
			}
			catch (Exception e)
			{
				
				Console.WriteLine(e);
				this.log.ERROR("libopenhgi", e.ToString());
				this.shouldRun = false;
			}
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
				MessageEvent(this, e);
			} else log.DEBUG("libopenhgi","MessageEvent is null");
		}
		
	}
}

