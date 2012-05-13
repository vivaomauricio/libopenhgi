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
	
	
	
	
}

