using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace openhgiclient
{
	public class XmlMessage : XmlDocument 
	{
		
		
		public XmlMessage ()
		{
		
			this.AppendChild(this.CreateElement("joint", "urn:1"));	
			
			
		}
		
	}
}

