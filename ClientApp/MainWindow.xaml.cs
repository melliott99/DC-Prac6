using APIClasses;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebServer.Models;
using IronPython.Hosting;
using System.Security.Cryptography;
using System.ComponentModel;
using System.ServiceModel;




//They didn't put python code in
//someone leaves the swarm
namespace ClientApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//private PeerServerInterface channel;
		private RestClient RestClient;

		private List<Client> clientList;

		private PeerServerInterface channel;

		private Client me;//Tracking my own details 

		private int jobCount = 0;//The total jobs this client has done
		
		//takes turns whether to start at the beginning or end 
		private bool fairSwarm = true;

		private Job currentJob = null;

		private Log logger = Log.GetInstance();



		public MainWindow()
		{
			InitializeComponent();

			string URL = "https://localhost:44341/";
			RestClient = new RestClient(URL);

			LoadingText.Visibility = Visibility.Hidden;
			ProgressBar.Visibility = Visibility.Hidden;

			//Registering myself
			me = new Client();
			me.ipaddress = "localhost";//this IP SHOULD work for any computer
			me.port = GeneratePort();//port number is randomised 
			me.jobcount = 0;
			PortBox.Text = me.port;
			JobCount.Text = JobCount.Text + jobCount;
			ServerThread();

			
			RegisterClient();

			CheckForAns();

			
		}
		//If someone closes then they are removed from the swarm
		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			RemoveClient();
		}

		public void RemoveClient()
		{
			RestRequest request = new RestRequest("api/client/unregisterclient/");
			request.AddJsonBody(me);
			IRestResponse resp = RestClient.Post(request);
			if(!resp.IsSuccessful)
			{
				logger.LogFunc("Failed to unregister client");
			}
		}

		/*Client checking to see if their request job has been completed*/
		public async void CheckForAns()
		{
			await Task.Run(() =>
			{
				while (true)
				{
					if (Messenger.finishedJob != null)
					{
						//Used to access the gui while not being on that thread
						Dispatcher.BeginInvoke(new Action(() =>
						{
							if (!Messenger.finishedJob.error)
							{
								if (string.IsNullOrEmpty(Messenger.finishedJob.code))
								{
									logger.LogFunc("Code was empty when checking for answers");
								}
								else
								{

									byte[] eoncodedBytes = Convert.FromBase64String(Messenger.finishedJob.code);
									string code = System.Text.Encoding.UTF8.GetString(eoncodedBytes);
									AnswerBox.Text = "Code: \n" + code + "\n\nAnswer is: " + Messenger.finishedJob.answer;
									logger.LogFunc("Successfully evaluated python script with the answer " + Messenger.finishedJob.answer);
								}
							}
							else
							{
								MessageBox.Show("Your input was not valid please use proper\npython code, refer to the readme for more info");
								Messenger.finishedJob = null;
								logger.LogFunc("User input invalid python script");
							}
						}));
					}
					UpdateScore();//change the scoreboard
					Thread.Sleep(10000);//May take a little bit for other clients to upload ans
				}
				
			});
		}


		public String GeneratePort()
		{
			Random rnd = new Random();
			string portnum = rnd.Next(8000, 8999).ToString();//generate random number from 8000-9000
			return portnum;
		}

		//Registering myself
		public void RegisterClient()
		{
			RestRequest request = new RestRequest("api/client/registerclient/");
			request.AddJsonBody(me);
			IRestResponse resp = RestClient.Post(request);
			if (resp.IsSuccessful)
			{
				logger.LogFunc("Successfully registered client");
			}
			else
			{
				logger.LogFunc("Failed to register client");
			}

		}

		/*-------------------------------------Network Thread------------------------------------------*/

		/* ---------------------------------------WARNING----------------------------------------------*/
		/* The code you are about to witness is a brainwave and if given more time it would be seperated
		 * out into different functions and made more clear but I didn't have enough time				*/
		public async Task NetworkThread()
		{
			await Task.Run(() =>
			{
				try
				{
					while (true)
					{
						RestRequest request = new RestRequest("api/client/getclientlist");
						IRestResponse resp = RestClient.Get(request);

						clientList = JsonConvert.DeserializeObject<List<Client>>(resp.Content);
						//Used to make the swarm more fair, start from the begginning and then start from the end
						if (fairSwarm)
						{
							fairSwarm = false;
							foreach (Client client in clientList)
							{
								if (me.port != client.port)
								{
									ChannelFactory<PeerServerInterface> channelFactory;
									NetTcpBinding tcp = new NetTcpBinding();
									tcp.MaxReceivedMessageSize = 2147483647;
									string clientURL = String.Format("net.tcp://{0}:{1}/DataService", client.ipaddress, client.port);
									channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
									channel = channelFactory.CreateChannel();

									currentJob = channel.DownloadJob();
									if (currentJob != null)
									{
										byte[] hash = CreateHash(currentJob.code);
										if (hash.SequenceEqual(currentJob.hash))
										{
											/*Sourced from 
											*https://stackoverflow.com/questions/7053172/how-can-i-call-ironpython-code-from-a-c-sharp-app
											*/
											Job doJob = currentJob;
											if (string.IsNullOrEmpty(doJob.code))
											{
												throw new EmptyCodeException();
											}
											else
											{
												byte[] eoncodedBytes = Convert.FromBase64String(doJob.code);
												string code = System.Text.Encoding.UTF8.GetString(eoncodedBytes);
												ChangeUIElements(true);
												Microsoft.Scripting.Hosting.ScriptEngine pythonEngine = IronPython.Hosting.Python.CreateEngine();
												Microsoft.Scripting.Hosting.ScriptSource pythonScript = pythonEngine.CreateScriptSourceFromString(code);
												var answer = pythonScript.Execute();
												Dispatcher.BeginInvoke(new Action(() =>
												{
													try
													{
														doJob.answer = answer;
														channel.UploadAns(doJob);
														jobCount++;
														ChangeJobCount();
													}
													catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)//Converting the answer to a string 
													{
														doJob.answer = answer.ToString();
														channel.UploadAns(doJob);
														jobCount++;
														ChangeJobCount();
													}
													ChangeUIElements(false);
													Thread.Sleep(2000);//Just so it doesn't constantly keep checking it sleeps for 2 seconds to chill for a bit
												}));
											}

											currentJob = null;
										}
									}
								}
							}
						}
						else
						{
							fairSwarm = true;
							for (int i = clientList.Count - 1; i >= 0; i--)
							{
								Client client = clientList[i];
								if (me.port != client.port)
								{
									ChannelFactory<PeerServerInterface> channelFactory;
									NetTcpBinding tcp = new NetTcpBinding();
									tcp.MaxReceivedMessageSize = 2147483647;
									string clientURL = String.Format("net.tcp://{0}:{1}/DataService", client.ipaddress, client.port);
									channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
									channel = channelFactory.CreateChannel();


									currentJob = channel.DownloadJob();
									if (currentJob != null)
									{
										byte[] hash = CreateHash(currentJob.code);
										if (hash.SequenceEqual(currentJob.hash))
										{
											/*Sourced from 
											*https://stackoverflow.com/questions/7053172/how-can-i-call-ironpython-code-from-a-c-sharp-app
											*/
											Job doJob = currentJob;
											if (string.IsNullOrEmpty(doJob.code))
											{
												throw new EmptyCodeException();
											}
											byte[] eoncodedBytes = Convert.FromBase64String(doJob.code);
											string code = System.Text.Encoding.UTF8.GetString(eoncodedBytes);
											ChangeUIElements(true);
											Microsoft.Scripting.Hosting.ScriptEngine pythonEngine = IronPython.Hosting.Python.CreateEngine();
											Microsoft.Scripting.Hosting.ScriptSource pythonScript = pythonEngine.CreateScriptSourceFromString(code);
											var answer = pythonScript.Execute();
											Dispatcher.BeginInvoke(new Action(() =>
											{
												//If have time encode ans but who cares for now
												try
												{

													doJob.answer = answer;
													channel.UploadAns(doJob);
													jobCount++;
													ChangeJobCount();
												}
												catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)//Converting the answer to a string 
												{
													doJob.answer = answer.ToString();
													channel.UploadAns(doJob);
													jobCount++;
													ChangeJobCount();
												}
												ChangeUIElements(false);
												Thread.Sleep(5000);
											}));

											currentJob = null;
										}
									}
								}
							}
						}

						UpdateScore();
						Thread.Sleep(10000);
					}
				}
				catch (System.ServiceModel.EndpointNotFoundException e)
				{
					//Occurs when someone leaves the swarm
					UpdateScore();
					logger.LogFunc("Error occured in the Networking Thread, someone has left the swarm abruptly");

				}
				catch (IronPython.Runtime.UnboundNameException e)
				{
					//When the input something not code
					MessageBox.Show("It appears were given a job that was in the wrong format");
					ChangeUIElements(false);
					currentJob.error = true;
					channel.UploadAns(currentJob);
					logger.LogFunc("Error occured in the Networking Thread, the users input code is invalid");

				}
				catch (EmptyCodeException e)
				{
					//when the input is empty
					logger.LogFunc("Error occured in the Networking Thread, the user didn't enter any code");
				}
			});

			
		}

		/*Posting to the scoreboard that a new job has been completed by a client*/
		public void UpdateScore()
		{
			RestRequest request = new RestRequest("api/client/getclientlist");//retrieving the client list
			IRestResponse resp = RestClient.Get(request);

			if (resp.IsSuccessful)
			{
				List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(resp.Content);
				if (clients != null)
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						Scoreboard.Items.Clear();
						foreach (Client client in clients)
						{
							String format = client.port + ": " + client.jobcount;
							Scoreboard.Items.Add(format);//adding to the item list in the xaml
						}
					}));
				}
			}
			else
			{
				logger.LogFunc("There was an error retrieving the client list in the UpdateScore function");
			}
		}


		public void ChangeUIElements(bool change)
		{
			//Alters the GUI Thread for when completing a job
			Dispatcher.BeginInvoke(new Action(() =>
			{
				if (change)
				{
					AnswerBox.Visibility = Visibility.Hidden;
					PortBox.Visibility = Visibility.Hidden;
					DownloadJob.Visibility = Visibility.Hidden;
					SubmitCode.Visibility = Visibility.Hidden;
					CodeTextBox.Visibility = Visibility.Hidden;
					CodeBox.Visibility = Visibility.Hidden;
					JobCount.Visibility = Visibility.Hidden;
					ScoreboardTitle.Visibility = Visibility.Hidden;
					Scoreboard.Visibility = Visibility.Hidden;
					ProgressBar.Visibility = Visibility.Visible;
					LoadingText.Visibility = Visibility.Visible;

				}
				else
				{
					AnswerBox.Visibility = Visibility.Visible;
					PortBox.Visibility = Visibility.Visible;
					DownloadJob.Visibility = Visibility.Visible;
					SubmitCode.Visibility = Visibility.Visible;
					CodeTextBox.Visibility = Visibility.Visible;
					CodeBox.Visibility = Visibility.Visible;
					JobCount.Visibility = Visibility.Visible;
					ScoreboardTitle.Visibility = Visibility.Visible;
					Scoreboard.Visibility = Visibility.Visible;
					ProgressBar.Visibility = Visibility.Hidden;
					LoadingText.Visibility = Visibility.Hidden;
				}
			}));
		}

		/*Updating the scores for all other clients*/
		public void ChangeJobCount()
		{
			JobCount.Text = "Job Count: " + jobCount;
			RestRequest request = new RestRequest("api/client/updatescores/");
			request.AddJsonBody(me);
			IRestResponse resp = RestClient.Post(request);
			if (resp.IsSuccessful)
			{
				UpdateScore();
			}
			else
			{
				logger.LogFunc("Could not tell other clients about a new completed job");
			}
		}


		/*-------------------------------------Server Thread------------------------------------------*/

		public async void ServerThread()
		{

			await Task.Run(() =>
			{

				Debug.WriteLine("Connecting to the server");

				//This is the actual host service system
				ServiceHost host;

				//This represents a tcp/ip binding in the windows network stack
				NetTcpBinding tcp = new NetTcpBinding();

				//Bind server to the implementation of peerServer
				host = new ServiceHost(typeof(PeerServer));

				string setupURL = String.Format("net.tcp://{0}:{1}/DataService", me.ipaddress, me.port);

				Debug.WriteLine(setupURL);
				host.AddServiceEndpoint(typeof(PeerServerInterface), tcp, setupURL);

				//We are on
				host.Open();

				Debug.WriteLine("Connection established for: " + me.port);

				while (true)
				{
					
				}
			});

		}


		
		
		public void SubmitCode_Click(object sender, RoutedEventArgs e)
		{
			//do nothing for now
			Job newJob = new Job();
			newJob.portID = me.port;
			newJob.error = false;
			if(String.IsNullOrEmpty(CodeBox.Text))
			{
				MessageBox.Show("Sorry No Code was input");
				logger.LogFunc("User input empty code and has been notified of this");
			}
			else
			{
				byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(CodeBox.Text);
				newJob.code = Convert.ToBase64String(textBytes);
				newJob.hash = CreateHash(newJob.code);

				Messenger.jobList.Enqueue(newJob);
				CodeBox.Text = ""; 
			}
		}


		public void DownloadJob_Click(object sender, RoutedEventArgs e)
		{
			//Used to start the networking thread 
			NetworkThread();
		}


		/*Creates the hash for encrypting*/
		public byte[] CreateHash(string code)
		{
			byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(code);
			SHA256 sha256Hash = SHA256.Create();
			return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(Convert.ToBase64String(textBytes)));
		}

	}
}
