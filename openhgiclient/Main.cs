using System;
using System.IO;
using System.Xml;
using libopenhgi;

namespace openhgiclient
{
	class OpenhgiClient
	{
		private string configxml;
		public Openhgi openhgi;
		
		private int leftHand;
		private int rightHand;
		
		
		
		public OpenhgiClient(string xml)
		{
			this.configxml = Path.GetFullPath(xml);
		}
		
		public void start()
		{
			this.openhgi = new Openhgi(configxml);		
			this.openhgi.MessageEvent += new MessageEventHandler(this.printMessage);
			this.openhgi.NewHGIUserEvent += new NewHGIUserHandler(this.newUser);
			this.openhgi.LostHGIUserEvent += new LostHGIUserHandler(this.lostUser);
			this.openhgi.LookingForPoseEvent += new LookingForPoseHandler(this.lookingForPose);
			this.openhgi.CalibratingHGIUserEvent += new CalibratingHGIUserHandler(this.calibratingUser);
			this.openhgi.LeftHandPointUpdatedEvent += 
				new LeftHandPointUpdatedHandler(this.leftHandUpdated);
			this.openhgi.RightHandPointUpdatedEvent += 
				new RightHandPointUpdatedHandler(this.rightHandUpdated);
			
			this.leftHand = 0;
			this.rightHand = 0;
			
			
			this.openhgi.initTracking();
		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{	
			Console.WriteLine(e.message);
		}
		
		private void newUser(object sender, HGIUserEventArgs e)
		{
			int user = e.user;
		}
		
		private void lostUser(object sender, HGIUserEventArgs e)
		{
			int user = e.user;
		}
		
		private void userIsSteady(object sender, HGIUserEventArgs e)
		{
			
		}
		
		private void userIsNotSteady(object sender, HGIUserEventArgs e)
		{
			
		}
		
		private void lookingForPose(object sender, HGIUserEventArgs e)
		{
			
		}
		
		private void calibratingUser(object sender, HGIUserEventArgs e)
		{
			
		}
		
		private void leftHandUpdated(object sender, HandPointEventArgs e)
		{
			
		}
		
		private void rightHandUpdated(object sender, HandPointEventArgs e)
		{
			
		}
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();
					
		}

		
	}
}
