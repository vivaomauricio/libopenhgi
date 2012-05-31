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
				Console.WriteLine(".");
			}
			else
			{
				Console.WriteLine("\tplane: " + e.coordinate.plane + "\tquadrant: " + e.coordinate.quadrant);
			}
		}
		
		
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();
					
		}

		
	}
}
