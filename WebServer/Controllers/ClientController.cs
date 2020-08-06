using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web;
using System.Web.Http;
using WebServer.Models;


namespace WebServer.Controllers
{
    public class ClientController : ApiController
    {
        Log logger = Log.GetInstance();
        [Route("api/client/registerclient/")]
        [HttpPost]
        public void RegisterClient([FromBody]Client client)
        {
            try
            {
                ListClients.addClient(client);
            }
            catch (Exception)//Catching all exceptions here as I don't know the specifics
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Failed to add the client with the port {client.port}")
                };
                logger.LogFunc($"Failed to add the client with the port {client.port}");
                throw new HttpResponseException(resMessage);
                
            }
        }


        [Route("api/client/getclientlist")]
        [HttpGet]
        public List<Client> GetClientList()
        {
            try
            {
                return ListClients.clients;
            }
            catch (Exception)
            {

                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Failed to return the client list")
                };
                logger.LogFunc("Failed to return the client list");
                throw new HttpResponseException(resMessage);
            }
        }

        [Route("api/client/unregisterclient/")]
        [HttpPost]
        public void UnRegisterClient([FromBody]Client client)
        {
            try
            {
                ListClients.removeClient(client);
            }
            catch (Exception)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Failed unregister this client with the port num {client.port}")
                };
                logger.LogFunc($"Failed to remove the client with the port {client.port}");
                throw new HttpResponseException(resMessage);
            }
        }


        [Route("api/client/updatescores/")]
        [HttpPost]
        public void UpdateScores([FromBody]Client client)
        {
            try
            {
                foreach (Client c in ListClients.clients)
                {
                    if (c.port == client.port)
                    {
                        c.updateJobCount(client.jobcount);
                    }
                }
            }
            catch (Exception)
            {
                HttpResponseMessage resMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to update scores")
                };
                logger.LogFunc("Failed to update scores");
                throw new HttpResponseException(resMessage);
            }
        }

    }
}