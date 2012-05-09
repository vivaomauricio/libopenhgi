using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using OpenNI;


namespace libopenhgi
{
	public class Openhgi
	{
		
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
				this.configxml = configxml;
				this.context = Context.CreateFromXmlFile(this.configxml, out this.scriptNode);
				this.depth = this.context.FindExistingNode(NodeType.Depth) as DepthGenerator;
				this.depthMetaData = new DepthMetaData();
				
				if (this.depth == null)
				{
					throw new Exception(@"Error in "
					                    + Path.GetFullPath(this.configxml)
					                    + ". No depth node found.");
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
	}
}

