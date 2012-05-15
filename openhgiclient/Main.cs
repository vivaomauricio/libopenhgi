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
		
		public OpenhgiClient(string xml)
		{
			this.configxml = Path.GetFullPath(xml);
		}
		
		public void start()
		{
			this.openhgi = new Openhgi(configxml);
			this.openhgi.initTracking();
			Console.Out.WriteLine("'Wave' to start session");

		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{	
			Console.WriteLine(e.message);
		}
		
		private void newUser(object sender, HGIUserEventArgs e)
		{
		
		}
		
		private void lostUser(object sender, HGIUserEventArgs e)
		{
		
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
