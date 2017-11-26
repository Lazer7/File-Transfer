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
        private Socket Listener;
        private IPEndPoint endpoint;
        private Socket Handler;
        //This Computer Meta Listener
        private Socket MetaListener;
        private IPEndPoint MetaEndPoint;
        private Socket MetaHandler;
        public event EventHandler FileSendingNotification;
        //Connecting to Other Computer
        private Socket senderSocket;
        private Socket MetaSenderSocket;
        private static System.Object lockBinaryWriter = new System.Object();
        private static System.Object lockMetaData = new System.Object();
        private bool sendingfile;
        private bool MetaSending;
        private bool receivingSubdirectories;
        public string currentSocket { get; set; }
        public bool GoodReceive { get; set; }


        public Connection(string folderName)
        {
            this.folderName = folderName;
            sendingfile = false;
            metaData = true;
            receivingSubdirectories = true;
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

                MetaEndPoint = new IPEndPoint(hostAddress[1], METAPORT);
                MetaListener = new Socket(hostAddress[1].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                MetaListener.Bind(MetaEndPoint);
                MetaListener.Listen(5);
                AsyncCallback metaCallBack = new AsyncCallback(MetaCallBack);
                MetaListener.BeginAccept(metaCallBack, MetaListener);
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
                IPEndPoint MetaIPEndpoint = new IPEndPoint(ipAddress, METAPORT);
                MetaSenderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                MetaSenderSocket.Connect(MetaIPEndpoint);
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
                senderSocket.Send(metaData);
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
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            sendingfile = true;

        }
        public void SendStartEnd()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);
            byte[] metaData = Encoding.ASCII.GetBytes(hostName[1].ToString());
            try
            {
                MetaSenderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            MetaSending= true;
        }



        public void MetaCallBack(IAsyncResult ar)
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
                currentHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(MetaReceiveData), obj);
                AsyncCallback aCallback = new AsyncCallback(MetaCallBack);
                currentListener.BeginAccept(aCallback, currentListener);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());

            }
        }
        public void MetaReceiveData(IAsyncResult ar)
        {
            //GetEnvironmentString
            byte[] fileContents = null;
            try
            {
                object[] obj = (object[])ar.AsyncState;
                fileContents = (byte[])obj[0];
                Handler = (Socket)obj[1];
                if (MetaSending)
                {
                    sendFileSendingNotification(EventArgs.Empty);
                    if (fileContents[0] == 1) { GoodReceive = true; }
                    else { GoodReceive = false; }
                    MetaSending = false;
                }
                else if (receivingSubdirectories)
                {
                    currentSocket = (Encoding.ASCII.GetString(fileContents)).Trim();
                    Debug.Assert(false, "||" + currentSocket + "||");
                    sendFileSendingNotification(EventArgs.Empty);
                    byte[] reply = { 1 };
                    MetaSenderSocket.Send(reply);
                }
                else
                {
                    receivingSubdirectories = true;
                    currentSocket = null;
                }
                sendFileSendingNotification(EventArgs.Empty);
            }
            catch (Exception ex) { Debug.Assert(false, "META SENT ERROR" + ex.Message); }
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
                    senderSocket.Send(reply);
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
                            senderSocket.Send(reply);
                            sendFileSendingNotification(EventArgs.Empty);
                        }
                        else
                        {

                            byte[] reply = { 0 };
                            senderSocket.Send(reply);
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
                        senderSocket.Send(reply);
                        sendFileSendingNotification(EventArgs.Empty);

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
