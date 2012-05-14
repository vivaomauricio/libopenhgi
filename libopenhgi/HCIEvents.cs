using System;

namespace libopenhgi
{
	
	public class MessageEventArgs : EventArgs
	{
		public string message;
		
		public MessageEventArgs(string message)
		{
			this.message = message;
		}
	}
	
	public class HGIUserEventArgs : EventArgs
	{
		public int ID;
		
		public HGIUserEventArgs(int ID)
		{
			this.ID = ID;
		}
	}
	
	
	
}

