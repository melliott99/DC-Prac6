using APIClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{

	[ServiceContract]
	public interface PeerServerInterface
	{

		[OperationContract]
		Job DownloadJob();

		[OperationContract]
		void UploadAns(Job completedJob);

		[OperationContract]
		void UploadJob(Job newJob);

	}
}
