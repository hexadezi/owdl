using ICSharpCode.SharpZipLib.BZip2;
using SharpPcap;
using SharpPcap.WinPcap;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;


namespace owdl.Model
{
	public class Sniffer
	{
		public event EventHandler<string> OnLineAddition;

		CaptureDeviceList networkInterfaces;

		public void InitializeAndStart()
		{
			try
			{
				networkInterfaces = SharpPcap.CaptureDeviceList.Instance;
				if (networkInterfaces.Count < 1)
				{
					MessageBox.Show("There are no network devices available. This can also happen if the application does not have enough rights. Try and run as administrator. Exiting...", "No network devices", MessageBoxButton.OK, MessageBoxImage.Information);
					Environment.Exit(0);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
				Environment.Exit(1);
			}

			/* If no network interfaces are available */

			Parallel.ForEach(networkInterfaces, (networkInterface) =>
			{
				string settingId = GetSettingId(networkInterface.Name);

				if (settingId == String.Empty)
				{
					return;
				}

				networkInterface.OnPacketArrival += NetworkInterface_OnPacketArrival;

				networkInterface.Open(DeviceMode.Normal, 500);

				/* What this does is get the first 4 bytes after the TCP header, 
                i.e.the first 4 data bytes of the payload.In the filter you pulled this from,.
                We are looking for HTTP GET requests using this filter:
                The 0x47455420 constant is actually a numeric encoding of the ASCII bytes
                for GET(that last character is a space), where the ASCII values
                of those characters are 0x47, 0x45, 0x54, 0x20.
                So, how does this work in full ? It extracts the 4 - bit Data Offset field
                from the TCP header, multiplies it by 4 to compute the size of the header 
                in bytes(which is also the offset to the data), then extracts 4 bytes at
                this offset to get the first 4 bytes of the data, which it then
                compares to "GET " to check it's a HTTP GET. */
				networkInterface.Filter = "port 80 and tcp[((tcp[12:1] & 0xf0) >> 2):4] = 0x47455420";

				/* Start capturing network traffic */
				networkInterface.StartCapture();

				AddToLog($"Capturing on {GetDescription(settingId)}");
			});

		}

		private void AddToLog(string str)
		{
			OnLineAddition?.Invoke(this, str);
		}

		private void NetworkInterface_OnPacketArrival(object sender, CaptureEventArgs packet)
		{
			/* Store the packet in a string */
			string packetData = Encoding.UTF8.GetString(packet.Packet.Data);

			/* If the packet contains a compressed demo file */
			try
			{
				//if (packetData.ToLower().Contains("gzip") && (packetData.Contains(".valve.net") || packetData.Contains("replay")) && packetData.Contains("GET /730/") && packetData.Contains(".dem.bz2"))
				if (packetData.ToLower().Contains(".dem.bz2") && packetData.Contains("user-agent: Valve/Steam HTTP Client"))
				{
					AddToLog("-----------------------------------------------------------------------");
					/* Declare variables to store the domain and file URL */
					Regex pattern = new Regex("GET /(?<id>.*)/(?<file>.*bz2).*Host: (?<server>.*).*\r\nAccept", RegexOptions.Singleline);
					Match match = pattern.Match(packetData);

					string id = match.Groups["id"].Value;
					string file = match.Groups["file"].Value;
					string host = match.Groups["server"].Value;
					string domain = host;

					// Check if host in china
					if (host.Contains("com.cn"))
					{
						domain = $"replay{id}.valve.net";
					}

					string fileAddress = $"http://{domain}/730/{file}";

					/* Log response to the console */
					AddToLog("Sniffed HTTP request to a Valve replay server.");
					AddToLog(fileAddress);
					Debug.WriteLine(fileAddress);

					/* Declare variables to store the filename to be used used on the local filesystem and it's full path */
					string localFile = Path.Combine(Path.GetTempPath(), file);

					/* Check if the file already exists in temp */
					if (File.Exists(localFile))
					{
						File.Delete(localFile);
					}

					using (var client = new WebClient())
					{
						/* Download the compressed demo file */
						AddToLog("Downloading compressed demo file...");

						/* Build the download URL */
						/* Path to store the downloaded file */
						client.DownloadFile(fileAddress, localFile);
						AddToLog("Download completed.");
					}


					/* Declare variable to store the final demo file */
					string finalPath = String.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), file.Replace(".bz2", ""));

					// Decompress the downloaded demo file
					FileInfo compressedDemo = new FileInfo(localFile);
					using (FileStream decompressStream = compressedDemo.OpenRead())
					{
						using (FileStream writeStream = File.Create(finalPath))
						{
							AddToLog("Decompressing demo file...");
							BZip2.Decompress(decompressStream, writeStream, true);
							AddToLog("Successfully decompressed demo file.");
						}
					}

					/* Delete the compressed demo */
					if (File.Exists(localFile))
					{
						File.Delete(localFile);
					}

					AddToLog("Saved to: " + finalPath);
				}
			}
			catch (Exception ex)
			{
				AddToLog(ex.Message);
			}
		}

		public void Start()
		{
			AddToLog("Start capture on all devices");
			Parallel.ForEach(networkInterfaces, (networkInterface) =>
			{
				/* Start capturing network traffic */
				networkInterface.StartCapture();
			});
			AddToLog("Started capture on all devices");
		}

		public void Stop()
		{
			Parallel.ForEach(networkInterfaces, (networkInterface) =>
			{
				networkInterface.StopCapture();
				networkInterface.Close();
			});
		}

		private string GetSettingId(string s)
		{
			Regex regex = new Regex(".*?{(?<settingId>.*)}");
			Match match = regex.Match(s);
			if (match.Success)
			{
				return match.Groups["settingId"].Value;
			}
			else
			{
				return String.Empty;
			}
		}

		private string GetDescription(string s)
		{
			ManagementObjectSearcher mos = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE SettingID = '{{{s}}}'");
			
			foreach (ManagementObject managementObject in mos.Get())
			{
				return managementObject.Properties["Description"].Value.ToString();
			}

			return String.Empty;
		}
	}
}
