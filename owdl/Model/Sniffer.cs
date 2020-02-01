using ICSharpCode.SharpZipLib.BZip2;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
            AddToLog("Start capture on all devices");

            networkInterfaces = SharpPcap.CaptureDeviceList.Instance;

            /* If no network interfaces are available */
            if (networkInterfaces.Count < 1)
            {
                MessageBox.Show("There are no network devices available. Exiting...", "No network devices", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Parallel.ForEach(networkInterfaces, (networkInterface) =>
            {
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

            });

            AddToLog("Capturing on all devices...");
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
                if (packetData.ToLower().Contains("gzip") && (packetData.Contains(".valve.net") || packetData.Contains("replay")) && packetData.Contains("GET /730/") && packetData.Contains(".dem.bz2"))
                {
                    AddToLog("-----------------------------------------------------------------------");
                    /* Declare variables to store the domain and file URL */
                    string file = String.Empty;
                    string domain = String.Empty;

                    /* Extract the domain and the file name from the packet */
                    domain = packetData.Split(new[] { "Host: " }, StringSplitOptions.None)[1].Split(new[] { "Accept: " }, StringSplitOptions.None)[0].Replace(Environment.NewLine, "");

                    if (domain.Contains("Connection"))
                    {
                        string tmp;
                        tmp = domain.Substring(0, domain.IndexOf("Connection"));
                        domain = tmp;
                    }

                    file = packetData.Split(new[] { "GET " }, StringSplitOptions.None)[1].Split(new[] { " HTTP" }, StringSplitOptions.None)[0];

                    /* Log response to the console */
                    AddToLog("Sniffed HTTP request to a Valve replay server.");
                    AddToLog("http://" + domain + file);

                    /* Declare variables to store the filename to be used used on the local filesystem and it's full path */
                    string localFileName = file.Replace("/730/", "");
                    string localFile = Path.GetTempPath() + localFileName;

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
                        client.DownloadFile(String.Format("http://{0}{1}", domain, file), localFile);
                        AddToLog("Download completed.");
                    }


                    /* Declare variable to store the final demo file */
                    string finalPath = String.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), localFileName.Replace(".bz2", ""));

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
    }
}
