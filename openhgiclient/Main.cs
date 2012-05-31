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
			
			this.openhgi.NavigationGestureEvent += 
				new NavigationGestureEventHandler(this.navigationGesture);
			
			this.openhgi.NavigationSessionStartEvent += 
				new NavigationSessionStartHandler(this.navigationSessionStart);
			
			this.openhgi.NavigationSessionEndEvent += 
				new NavigationSessionEndHandler(this.navigationSessionEnd);
			
			this.openhgi.PointingCoordinatesEvent += 
				new PointingCoordinatesHandler(this.pointingCoordinates);
			
			this.openhgi.MessageEvent += new MessageEventHandler(this.printMessage);
			
			
			this.openhgi.initTracking();

		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{	
			Console.WriteLine(e.message);
		}
		
		private void navigationSessionStart(object sender, HGIUserEventArgs e)
		{
			Console.WriteLine("Navigation Session started for user " + e.user);
		}
		
		private void navigationSessionEnd(object sender, HGIUserEventArgs e)
		{
			Console.WriteLine("Navigation Session ended for user " + e.user);
		}
		
		private void navigationGesture(object sender, NavigationGestureEventArgs e)
		{
			if (e.coordinate.plane == MovementSpacePlane.POV 
			    && e.coordinate.quadrant == MovementSpaceQuadrant.CENTER)
			{
				Console.WriteLine("[NAVIGATION]");
			}
			else
			{
				
				if (e.coordinate.plane == MovementSpacePlane.POV)
				{
					Console.WriteLine("[NAVIGATION]\t\t\t" + e.coordinate.quadrant);
				}
				else
				{
					Console.WriteLine("[NAVIGATION]\t\t\t" + e.coordinate.plane);
				}
			}
		}
		
		private void pointingSessionStart(object sender, HGIUserEventArgs e)
		{
			Console.WriteLine("[POINTING] session started");
		}
		
		private void pointingSessionEnd(object sender, HGIUserEventArgs e)
		{
			Console.WriteLine("[POINTING] session ended");
		}
		
		private void pointingCoordinates(object sender, HandPointEventArgs e)
		{
			Console.WriteLine("[POINTING] \t\t\tx: " + e.X + "\ty: " + e.Y);
		}
		
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();			
		}
	}
}
