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
			
			this.openhgi.MessageEvent += new MessageEventHandler(this.printMessage);
		}
		
		
		private void printMessage(object sender, MessageEventArgs e)
		{
			Console.WriteLine("yyyyyyyyyyyyyyyyyyyyyyyyyyyy");
			Console.WriteLine(e.message);
		}
		
		public static void Main (string[] args)
		{
			OpenhgiClient client = new OpenhgiClient(@"../../config/data.xml");
			client.start();
					
		}

		
	}
}
