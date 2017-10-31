using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileTransferClient.Models
{
    public class Connection
    {
        private const int PORT = 4450;
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTELIMIT = 100;
        private bool metaData { get; set; }
        private String folderName;
        private String receivingFileName;
        private SocketPermission permission;
        //This Computer
        private Socket Listener;
        private IPEndPoint endpoint;
        private Socket Handler;
        //Connecting to Other Computer
        private Socket senderSocket;
        public Connection(string folderName)
        {
            this.folderName = folderName;
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
            /*string hostName = Dns.GetHostName();
            IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            ipAddressList.Add(iPAddress[1].ToString());*/
            return ipAddressList;
        }
        public string SendFile(String file)
        {
            String fileDirectory = folderName+"\\"+ file;
            byte[] metaData = File.ReadAllBytes(fileDirectory);
            try
            {
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { return ex.ToString(); }
            
            return null;
        }
        public string SendFileMetaData(String file)
        {
            byte[] metaData = new byte[FILEDATEBYTELIMIT + FILENAMEBYTELIMIT];
            String fileDirectory = folderName + "\\" + file;
            int counter = 0;
            foreach (byte x in Encoding.ASCII.GetBytes(file))
            {
                metaData[counter] = x;
                counter++;
            }
            counter = FILENAMEBYTELIMIT;
            foreach (byte x in Encoding.ASCII.GetBytes(File.GetLastWriteTime(fileDirectory).ToString()))
            {
                metaData[counter] = x;
                Console.WriteLine(metaData[counter]);
            }
            try
            {
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { return ex.ToString(); }
            return null;
        }




        public void CallBack(IAsyncResult ar)
        {
            //Debug.Assert(false, "Receiving");
            try
            {
                byte[] buffer = new byte[FILEBYTELIMIT+FILENAMEBYTELIMIT+FILEDATEBYTELIMIT];
                Socket currentListener = (Socket)ar.AsyncState;
                Socket currentHandler = currentListener.EndAccept(ar);
                currentHandler.NoDelay = false;
                object[] obj = new object[2];
                obj[0]= buffer;
                obj[1]=currentHandler ;
                currentHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveFile), obj);
                AsyncCallback aCallback = new AsyncCallback(CallBack);
                currentListener.BeginAccept(aCallback, currentListener);
            }
            catch (Exception ex) { Debug.Assert(false,ex.ToString()); }

        }
        public void ReceiveFile(IAsyncResult ar)
        { 
            //GetEnvironmentString
            byte[] fileContents=null; 
            try
            {
                object[] obj = (object[])ar.AsyncState;
                fileContents = (byte[])obj[0];
                Handler = (Socket)obj[1];
                int NumberOfBytes = fileContents.Length;
                if (NumberOfBytes >= 0 && metaData)
                {
                    int spaces = 0;
                    int stringSize = 0;
                    for (int i = 0; i < FILENAMEBYTELIMIT; i++)
                    {
                        if (fileContents[i] == 0)
                        {
                            spaces++;
                        }
                        if (spaces == 1) { break; }
                        stringSize++;
                    }
                    byte[] fileNameBytes = new byte[stringSize];
                    for (int i = 0; i < stringSize; i++)
                    {
                        fileNameBytes[i] = fileContents[i];
                    }
                    receivingFileName = (Encoding.ASCII.GetString(fileNameBytes)).Trim();
                    metaData = false; 

                }
                else if (NumberOfBytes >= 0 && !metaData)
                {
                    BinaryWriter Writer = new BinaryWriter(File.OpenWrite(folderName + "\\" + receivingFileName));
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
            catch(Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
           
        }
        public void PingAddress()
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
                process.Start();
            }
        }



    }




}
