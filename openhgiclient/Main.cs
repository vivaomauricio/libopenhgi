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
			
			
			this.openhgi.initTracking();
			
			Console.Out.WriteLine("'Wave' to start session");

		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{	
			Console.WriteLine(e.message);
		}
		
		private void navigationGesture(object sender, NavigationGestureEventArgs e)
		{
			Console.Out.Write(".");	
			
		}
		
		
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();
					
		}

		
	}
}
