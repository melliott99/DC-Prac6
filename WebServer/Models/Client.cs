using APIClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServer.Models
{
	public class Client
	{

		public int jobcount{get; set;}

		public string ipaddress{get;set;}
		public string port{get;set;}

		public void updateJobCount(int newCount)
		{
			jobcount++;
		}
	}
}