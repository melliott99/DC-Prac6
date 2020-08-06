using APIClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ClientApp;
using WebServer.Models;

namespace ClientApp
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = true)]
	class PeerServer : PeerServerInterface
	{
		Log logger = Log.GetInstance();
		public void UploadJob(Job newJob)
		{
			Messenger.jobList.Enqueue(newJob);
			logger.LogFunc("Just uploaded a new job");

		}


		public Job DownloadJob()
		{
			Job job = null;
			if (Messenger.jobList.TryDequeue(out job))//gets a job out the queue if there is one
			{
				logger.LogFunc("Just downloaded a job from this client " + job.portID);
				
			}
			return job;
		}

		public void UploadAns(Job completedJob)
		{
			Messenger.finishedJob = completedJob;

		}

	}
}
