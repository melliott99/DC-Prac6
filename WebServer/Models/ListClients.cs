using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServer.Models
{
	public class ListClients
	{
		public static List<Client> clients = new List<Client>();

		public static void addClient(Client client)
		{
			clients.Add(client);
		}

		public static void removeClient(Client client)
		{
			for(int i = 0; i < clients.Count; i++)
			{
				if (clients[i].port == client.port)
				{
					clients.RemoveAt(i);
				}
			}
		}

	}
}