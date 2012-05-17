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
			this.openhgi.MessageEvent += new MessageEventHandler(this.printMessage);
			
			
			this.openhgi.initTracking();

		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{	
			Console.WriteLine(e.message);
		}
		
		private void navigationGesture(object sender, NavigationGestureEventArgs e)
		{
			Console.Out.Write(".");
			
			if (e.coordinate.plane == MovementSpacePlane.BACKWARD)
			{
				Console.WriteLine("Plane: Backward");
			}
			else if (e.coordinate.plane == MovementSpacePlane.POV)
			{
				Console.WriteLine("Plane: POV");
			}
			else if (e.coordinate.plane == MovementSpacePlane.FORWARD)
			{
				Console.WriteLine("Plane: Forward");
			}
			else 
			{
				Console.Out.WriteLine("No plane at all");
			}
			
		}
		
		
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();
					
		}

		
	}
}
