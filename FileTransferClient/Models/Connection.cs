using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FileTransferClient;
using System.Windows;

namespace FileTransferClient.Models
{
    public class Connection
    {
        public string currentIP { get; set; }

        private const int PORT = 4450;
        private const int METAPORT = 4500;
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTELIMIT = 100;
        private bool metaData { get; set; }
        private String folderName;
        private String receivingFileName;
        //This Computer
        private Socket MetaListener;
        private Socket Listener;
        private IPEndPoint MetaEndPoint;
        private IPEndPoint endpoint;
        private Socket MetaHandler;
        private Socket Handler;
        public event EventHandler FileSendingNotification;
        //Connecting to Other Computer
        private List<String> addressNames;
        private List<Socket> senderSocket;
        private List<Socket> MetaSenderSocket;
        private static System.Object lockBinaryWriter = new System.Object();
        private static System.Object lockMetaData = new System.Object();
        private bool sendingfile;
        private int indexSendingIPAddress;


        private bool receivingSubdirectories;
        public bool GoodReceive { get; set; }


        public Connection(string folderName)
        {
            this.folderName = folderName;
            sendingfile = false;
            metaData = true;
            receivingSubdirectories = true;
            addressNames = new List<string>();
            senderSocket = new List<Socket>();
            MetaSenderSocket = new List<Socket>();
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

                MetaEndPoint = new IPEndPoint(hostAddress[1], METAPORT);
                MetaListener = new Socket(hostAddress[1].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                MetaListener.Bind(MetaEndPoint);
                MetaListener.Listen(10);
                AsyncCallback callBackreceiver = new AsyncCallback(CallBackReceiver);
                MetaListener.BeginAccept(callBackreceiver, MetaListener);

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
                int index = 0;
                int sameSocket = -1;
                Socket currentSocket;
                SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
                permission.Demand();
                IPAddress ipAddress = IPAddress.Parse(Address);
                IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, PORT);
                IPEndPoint ipEndpointmeta = new IPEndPoint(ipAddress, METAPORT);
                currentSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                foreach (Socket x in senderSocket)
                {
                    if (x.AddressFamily.Equals(currentSocket.AddressFamily))
                    {
                        sameSocket = index;
                    }
                    index++;
                }
                currentSocket.Connect(ipEndpoint);
                currentSocket.Connect(ipEndpointmeta);
                if (sameSocket != -1)
                {
                    senderSocket[sameSocket] = (currentSocket);
                    MetaSenderSocket[sameSocket] = (currentSocket);
                    addressNames[sameSocket] = Address;
                }
                else
                {
                    senderSocket.Add(currentSocket);
                    MetaSenderSocket.Add(currentSocket);
                    addressNames.Add(Address);
                }
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
                senderSocket[MainWindow.currentSocket].Send(metaData);
            }
            catch (Exception ex) { }
            sendingfile = true;

        }
        public void SendFileMetaData(String file)
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
                senderSocket[MainWindow.currentSocket].Send(metaData);
            }
            catch (Exception ex) { }
            sendingfile = true;
        }
        public void SendSubdirectories(List<String> subDirectories)
        {
            byte[] metaData = new byte[FILEBYTELIMIT];
            int counter = 0;
            int currentspot = 1;
            int DIRECTORYBYTESIZE = 500;
            foreach (String directory in subDirectories)
            {
                foreach (byte x in Encoding.ASCII.GetBytes(directory))
                {
                    metaData[counter] = x;
                    counter++;
                }
                counter = currentspot * DIRECTORYBYTESIZE;
                currentspot++;
            }
            try
            {
                senderSocket[MainWindow.currentSocket].Send(metaData);
            }
            catch (Exception ex) { }
            sendingfile = true;

        }
        public void SendConnectorSocket()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);

            String ipAddress = hostAddress[1].ToString();
            byte[] metaData = Encoding.ASCII.GetBytes(ipAddress);
            try
            {
                senderSocket[MainWindow.currentSocket].Send(metaData);
            }
            catch (Exception ex) { }
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
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());

            }
        }
        public void CallBackReceiver(IAsyncResult ar)
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
                currentHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(RelocateIp), obj);
                AsyncCallback aCallback = new AsyncCallback(CallBackReceiver);
                currentListener.BeginAccept(aCallback, currentListener);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());

            }
        }
        public void RelocateIp(IAsyncResult ar)
        {
            try
            {
                byte[] fileContents = null;
                object[] obj = (object[])ar.AsyncState;
                fileContents = (byte[])obj[0];
                Handler = (Socket)obj[1];
                int NumberOfBytes = fileContents.Length;

                if (receivingSubdirectories)
                {
                    string parseDirectory = (Encoding.ASCII.GetString(fileContents)).Trim();
                    indexSendingIPAddress = addressNames.IndexOf(parseDirectory);
                }
                else
                {
                    receivingSubdirectories = true;
                }
            }
            catch { }
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

                if (sendingfile)
                {
                    sendFileSendingNotification(EventArgs.Empty);
                    if (fileContents[0] == 1) { GoodReceive = true; }
                    else { GoodReceive = false; }
                    sendingfile = false;
                }
                else if (NumberOfBytes >= FILEBYTELIMIT && receivingSubdirectories)
                {
                    Debug.Assert(false, "Doust is here");
                    List<String> receivedDirectories = new List<string>();
                    int index = 1;
                    for (int i = 0; i < FILEBYTELIMIT - 500; i++)
                    {
                        int spaces = 0;
                        for (int f = i; f < i + 500; f++)
                        {
                            if (fileContents[f] != 0)
                            {
                                spaces++;
                            }
                            else { break; }
                        }
                        byte[] currentName = new byte[spaces];
                        for (int j = 0; j < spaces; j++)
                        {
                            if (fileContents[i + j] != 0)
                            {
                                currentName[j] = fileContents[j + i];
                            }
                            else { break; }
                        }
                        string parseDirectory = (Encoding.ASCII.GetString(currentName)).Trim();
                        if (parseDirectory.Equals(""))
                        {
                            break;
                        }
                        receivedDirectories.Add(parseDirectory);
                        i = index * 500;
                        index++;
                    }
                    foreach (String directory in receivedDirectories)
                    {

                        String refinedDirectory = directory;
                        if (!refinedDirectory[0].Equals("\\"))
                        {
                            refinedDirectory = "\\" + directory;
                        }
                        if (!System.IO.Directory.Exists(folderName + refinedDirectory))
                        {
                            System.IO.Directory.CreateDirectory(folderName + refinedDirectory);
                        }
                    }
                    receivingSubdirectories = false;
                    byte[] reply = { 1 };
                    senderSocket[indexSendingIPAddress].Send(reply);
                    sendFileSendingNotification(EventArgs.Empty);
                }

                else if (NumberOfBytes >= FILENAMEBYTELIMIT && metaData)
                {
                    lock (lockMetaData)
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
                        if (!receivingFileName.Equals("") && receivingFileName.Contains("."))
                        {
                            metaData = false;

                            byte[] reply = { 1 };
                            senderSocket[indexSendingIPAddress].Send(reply);
                            sendFileSendingNotification(EventArgs.Empty);
                        }
                        else
                        {

                            byte[] reply = { 0 };
                            senderSocket[indexSendingIPAddress].Send(reply);
                            sendFileSendingNotification(EventArgs.Empty);
                        }

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
                        Writer.Dispose();
                        receivingFileName = "";
                        metaData = true;

                        byte[] reply = { 1 };
                        senderSocket[indexSendingIPAddress].Send(reply);
                        sendFileSendingNotification(EventArgs.Empty);

                    }
                }


            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                Debug.Assert(false, "THE INDEXSENDINGIPADDRESS IS" + indexSendingIPAddress);
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
