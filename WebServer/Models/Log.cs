using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace WebServer.Models
{
	public class Log
	{
		private static uint logNum;
		private static Log instance;

		private Log()
		{
			logNum = 1;
		}

		public static Log GetInstance()
		{
			if (instance == null)
			{
				instance = new Log();
			}
			return instance;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void LogFunc(string logString)
		{
			logNum++;
			System.IO.File.AppendAllText(@"C:\Users\Michael (Work)\source\repos\Prac6\Logs\LogFile.txt", "\n" + logNum + ": " + logString);
		}
	}
}