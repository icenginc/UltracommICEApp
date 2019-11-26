using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace UltraCommunications.Service
{
	// NOTE: Unable to find assembly 'ServicePublisher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
	//       See DeserializationHack class for details.

	// the following two lines makes this class work as a regular DLL and an activex server (loadable into Matlab).
    [Guid("E2D415CD-6053-4B8C-AD46-BAE52382D07D"), ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
	//[ComVisible(true)]
    
	public class ServiceQuery
	{
		Info[] m_info;
		public Info[] INFO { get { return m_info; } }

		/// <summary>
		/// Parameterless constructor for activex invocation.
		/// </summary>
		public ServiceQuery()
		{
            System.Diagnostics.Debug.WriteLine("ServiceQuery() - Duplicate response received: " );
		}
		public ServiceQuery(string service_class, string identity, int port)
		{
			Initialize(service_class, identity, port);
		}
		public void Initialize(string service_class, string identity, int port)
		{
			UdpClient udp = new UdpClient();
			udp.EnableBroadcast = true;
			IPEndPoint ep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), port);
			UltraCommunications.Service.Query query = new Query(service_class, identity);
			byte[] buf = query.Serialize();

			udp.Send(buf, buf.Length, ep);
			udp.Client.ReceiveTimeout = 2000;

			Dictionary<string, Info> service_providers = new Dictionary<string, Info>();
			try
			{
				while (true)
				{
					Info info = Info.Deserialize(udp.Receive(ref ep));
					string id = info.hostname + ":" + info.identity;

					if (!service_providers.ContainsKey(id))
					{
						service_providers.Add(id, info);
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("ServiceQuery() - Duplicate response received: " + id);
					}
				}
			}
			catch (SocketException)
			{
				// executes after timeout
				m_info = new Info[service_providers.Count];
				service_providers.Values.CopyTo(m_info, 0);
			}
			udp.Close();
		}
		/// <summary>
		/// Function to enable Matlab to retrieve info.
		/// </summary>
		/// <returns></returns>
		public string[,] GetInfo()
		{
			string[,] info = new string[m_info.Length, 6];

			for (int idx = 0; idx < m_info.Length; idx++)
			{
				info[idx, 0] = m_info[idx].hostname;
				info[idx, 1] = m_info[idx].service_class;
				info[idx, 2] = m_info[idx].identity;
				info[idx, 3] = m_info[idx].endpoint;
				info[idx, 4] = m_info[idx].description;
				info[idx, 5] = m_info[idx].status;
			}

			return info;
		}
	}
}
