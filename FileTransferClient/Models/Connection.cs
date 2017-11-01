using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileTransferClient.Models
{
    public class Connection
    {
        public string currentIP{ get; set; }

        private const int PORT = 4450;
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTELIMIT = 100;
        private bool metaData { get; set; }
        private List<String> fileReceivedList;
        private String folderName;
        private String receivingFileName;
        //This Computer
        private Socket Listener;
        private IPEndPoint endpoint;
        private Socket Handler;
        public event EventHandler FileSendingNotification;
        //Connecting to Other Computer
        private Socket senderSocket;
        private static System.Object lockBinaryWriter = new System.Object();
        private static System.Object lockMetaData = new System.Object();
        private bool sendingFile;

        public Connection(string folderName)
        {
            this.folderName = folderName;
            sendingFile = false;
            metaData = true;
        }
        public String GetSyncFolderName() { return folderName; }
        public string CreatePeerConnection()
        {
            try
            {
                SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
                permission.Demand();
                string hostName = Dns.GetHostName();
                IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);
                endpoint = new IPEndPoint(hostAddress[1], PORT);
                Listener = new Socket(hostAddress[1].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Listener.Bind(endpoint);
                Listener.Listen(10);
                AsyncCallback callBack = new AsyncCallback(CallBack);
                Listener.BeginAccept(callBack, Listener);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return null;
        }
        public string ConnectToPeer(string Address)
        {
            try
            {
                SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
                permission.Demand();
                IPAddress ipAddress = IPAddress.Parse(Address);
                IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, PORT);
                senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                senderSocket.Connect(ipEndpoint);
                sendingFile = false;
            }
            catch (Exception ex) { return ex.ToString(); }
            return "Success";
        }
        public List<String> GetIpAddress()
        {
            List<String> ipAddressList = new List<String>();
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = Environment.SystemDirectory + "\\ARP.EXE";
            processInfo.Arguments = " -a";
            Process process = new Process();
            process.StartInfo = processInfo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            StreamReader x = process.StandardOutput;
            while (!x.EndOfStream)
            {
                String ipAddress = x.ReadLine().Trim();
                if (ipAddress.Contains("dynamic"))
                {
                    String trueIP = ipAddress.Substring(0, ipAddress.IndexOf(" "));
                    ipAddressList.Add(trueIP);
                }
            }
            return ipAddressList;
        }
        public void SendFile(String file)
        {
            String fileDirectory = folderName + "\\" + file;
            byte[] metaData = File.ReadAllBytes(fileDirectory);

            try
            {
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            sendingFile = true;

        }
        public void SendFileMetaData(List<String> file)
        {
            byte[] metaData = new byte[FILEBYTELIMIT];
            int counter = 0;
            for (int i = 0; i < file.Count; )
            {
                String fileDirectory = folderName + "\\" + file[i];
                foreach (byte x in Encoding.ASCII.GetBytes(file[i]))
                {
                    metaData[counter] = x;
                    counter++;
                }
                counter +=FILENAMEBYTELIMIT;
                foreach (byte x in Encoding.ASCII.GetBytes(File.GetLastWriteTime(fileDirectory).ToString()))
                {
                    metaData[counter] = x;
                    Console.WriteLine(metaData[counter]);
                }
                Debug.Assert(false, file[i]);
                i++;
                counter = (i * FILENAMEBYTELIMIT);

            }
            try
            {
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            sendingFile = true;
        }
        public void CallBack(IAsyncResult ar)
        {
            try
            {
                byte[] buffer = new byte[FILEBYTELIMIT];
                Socket currentListener = (Socket)ar.AsyncState;
                Socket currentHandler = currentListener.EndAccept(ar);
                currentHandler.NoDelay = false;
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = currentHandler;
                currentHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveFile), obj);
                AsyncCallback aCallback = new AsyncCallback(CallBack);
                currentListener.BeginAccept(aCallback, currentListener);
            }
            catch (Exception ex) { Debug.Assert(false, ex.ToString()); }
        }
        public void ReceiveFile(IAsyncResult ar)
        {
            //GetEnvironmentString
            byte[] fileContents = null;
            try
            {

                object[] obj = (object[])ar.AsyncState;
                fileContents = (byte[])obj[0];
                Handler = (Socket)obj[1];
                int NumberOfBytes = fileContents.Length;
                if (NumberOfBytes >= FILENAMEBYTELIMIT && metaData)
                {
                    lock (lockMetaData)
                    {
                        fileReceivedList = new List<string>();
                        bool isLastString = false;
                        int counter = 0;
                        int index = 0;
                        while (!isLastString)
                        {
                            int spaces = 0;
                            byte[] fileNameBytes = new byte[FILENAMEBYTELIMIT];
                            for (int i = 0; i < FILENAMEBYTELIMIT; i++)
                            {
                                fileNameBytes[i] = fileContents[counter+i];
                                if (fileContents[counter + 1]==0)
                                {
                                    spaces++;
                                }
                                if (spaces == FILENAMEBYTELIMIT)
                                {
                                    isLastString = true;
                                }

                            }
                            if (!isLastString)
                            {
                                String found = Encoding.ASCII.GetString(fileNameBytes).Trim();
                                fileReceivedList.Add(found);
                                Debug.Assert(false, found);
                            }
                            index++;
                            counter += (index * FILENAMEBYTELIMIT) + FILEDATEBYTELIMIT;
                        }
                        metaData = false;
                    }
                }

                else if (NumberOfBytes >= FILEBYTELIMIT && !metaData)
                {
                    lock (lockBinaryWriter)
                    {
                        BinaryWriter Writer;
                        try
                        {
                            Writer = new BinaryWriter(File.OpenWrite(folderName + "\\" + receivingFileName));
                        }
                        catch
                        {
                            String[] tempList = Directory.GetFiles(folderName);
                            receivingFileName = "corruptedDownload.txt";
                            int counter = 1;
                            foreach (String x in tempList)
                            {
                                if (x.Contains(receivingFileName))
                                {
                                    receivingFileName = "corruptedDownload(" + counter + ").txt";
                                }

                            }
                            Writer = new BinaryWriter(File.OpenWrite(folderName + "\\" + receivingFileName));
                        }
                        byte[] fileContentsdecrypt = new byte[FILEBYTELIMIT];
                        for (int i = 0; i < FILEBYTELIMIT; i++)
                        {
                            fileContentsdecrypt[i] = fileContents[i];
                        }

                        Writer.Write(fileContents);
                        Writer.Flush();
                        Writer.Close();
                        receivingFileName = "";
                        metaData = true;
                    }
                }


        }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

}
        public void PingAddress()
        {
            new Thread(() =>
            {
                string hostName = Dns.GetHostName();
                IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
                String ipAddressHost = iPAddress[1].ToString();
                ipAddressHost = ipAddressHost.Substring(0, ipAddressHost.LastIndexOf('.') + 1);
                for (int i = 1; i < 255; i++)
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo();
                    processInfo.FileName = Environment.SystemDirectory + "\\PING.EXE";
                    processInfo.Arguments = ipAddressHost + i + " -n 1";
                    processInfo.UseShellExecute = false;
                    processInfo.CreateNoWindow = true;
                    Process process = new Process();
                    process.StartInfo = processInfo;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                }
                sendFileSendingNotification(EventArgs.Empty);
            }).Start();
        }
        protected virtual void sendFileSendingNotification(EventArgs e)
        {
            EventHandler handler = FileSendingNotification;
            if (handler != null)
            {
                handler(this, e);
            }
        }


    }




}
