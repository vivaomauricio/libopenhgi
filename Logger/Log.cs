using System;
using System.IO;

namespace Logger
{
	public class Log
	{
		private static Log instance;
		
		
		private string logfilepath;
		private TextWriter textWriter;
		
		private int i;
		
		
		public static Log getInstance(String logfilepath)
		{
			if (instance == null)
				instance = new Log(logfilepath);
			return instance;
		}
		
		public static Log getInstance()
		{
			if (instance == null)
				instance = new Log();
			return instance;
		}
		
		private enum Level {NONE, INFO, DEBUG}
		private Level level;
		
		public Log ()
		{
			this.init();
		}
		
		public Log (string logfilepath)
		{
			this.init();
			this.logfilepath = Path.GetFullPath(logfilepath);
			this.textWriter = File.AppendText(this.logfilepath);
			this.writeHeader();
			this.INFO("Log", "writing on file: " + this.logfilepath);
		}
		
		private void init()
		{
			this.level = Level.INFO;
			this.i = 0;
		}
		
		public void setLevelDebug(bool debug)
		{
			if (debug)
				this.level = Level.DEBUG;
			else
				this.level = Level.INFO;
		}
		
		public void setLevelNone(bool none)
		{
			if (none)
				this.level = Level.NONE;
			else 
				this.level = Level.INFO;
		}
		
		private void writeFile(string str)
		{
			this.i += 1;
			str = "[" + this.i + "]" + str;
			this.textWriter.WriteLine(str);
			this.textWriter.Flush();
		}
		
		private void write(string str)
		{
			this.i += 1;
			str = "[" + this.i + "]" + str;
			Console.WriteLine(str);
			writeFile(str);
		}
		
		public void DEBUG(string module, string str)
		{
			str = "[DEBUG][" + module + "] " + str;
			if (this.level == Level.DEBUG)
			{
				write(str);
			}
			else
			{
				writeFile(str);	
			}
		}
		
		public void INFO(string module, string str)
		{
			str = "[INFO][" + module + "] " + str;
			if (this.level == Level.INFO || this.level == Level.DEBUG)
			{	
				write(str);
			}
			else
			{
				writeFile(str);
			}
		}
		
		public void ERROR(string module, string str)
		{
			str = "[ERROR][" + module + "] " + str;
			write (str);
		}
		
		public void OffTheRecord(string module, string str)
		{
			str = "[>][" + module + "] " + str;
			Console.WriteLine(str);
		}
		
		public void CloseTextWriter()
		{
			if (this.textWriter != null)
			{
				this.textWriter.Close();
			}
		}
		
		public void writeHeader()
		{
			this.textWriter.WriteLine("\n\n#\n#\n#\n#  " + DateTime.Now + "\n");
			this.textWriter.Flush();
		}
		
		public void printPoint(string label, int x, int y, int z)
		{
			Console.Out.WriteLine(label + ":\t\t\t\tx= " + x + "  y= " + y + "  z= " + z);
		}
	}
}

