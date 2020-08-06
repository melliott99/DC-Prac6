using APIClasses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
	public static class Messenger
	{
		//Shared between the main window and the peerserver so they can both add and remove
		public static ConcurrentQueue<Job> jobList = new ConcurrentQueue<Job>();

		public static Job finishedJob = new Job();

	}
}
