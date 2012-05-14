using System;
using OpenNI;

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
		public int user;
		
		public HGIUserEventArgs(int user)
		{
			this.user = user;
		}
	}
	
	public class HandPointEventArgs : EventArgs
	{
		public int user;
		public int X;
		public int Y;
		public int Z;
		
		public HandPointEventArgs(int user, int X, int Y, int Z)
		{
			this.user = user;
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}
	}
	
}

