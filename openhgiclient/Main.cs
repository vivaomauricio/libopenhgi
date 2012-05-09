using System;
using System.IO;
using libopenhgi;


namespace openhgiclient
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			
			string configxml = @"../../config/data.xml";
			configxml = Path.GetFullPath(configxml);
			
			Openhgi openhgi = new Openhgi();
		}
	}
}
