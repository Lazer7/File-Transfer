﻿using System;
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
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTELIMIT = 100;
        private bool metaData { get; set; }
        private DateTime metaDate { get; set; }
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
        public bool sendingfile { get; set; }
        public string currentSocket { get; set; }
        private bool receivingSubdirectories;
        public bool GoodReceive { get; set; }
        public bool startSync { get; set; }


        public Connection(string folderName)
        {
            this.folderName = folderName;
            sendingfile = false;
            metaData = true;
            receivingSubdirectories = true;
            startSync = true;
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
            return ipAddressList;
        }
        public void SendResumeMessage()
        {
            String endCode = "";
            for (int i = 0; i < 200; i++)
            {
                endCode += i;
            }
            byte[] haltMessage = Encoding.ASCII.GetBytes(endCode); //490bytes length
            try
            {
                senderSocket.Send(haltMessage);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());
            }
            sendingfile = true;
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
                counter++;
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
            string hostName = Dns.GetHostName();
            IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);
            byte[] clientAddress = hostAddress[1].GetAddressBytes();
            foreach (byte address in clientAddress)
            {
                metaData[counter] = address;
                counter++;
            }
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
                startSync = false;
                byte[] resumeSync = new byte[490];
                for (int i = 0; i < 490; i++)
                {
                    resumeSync[i] = fileContents[i];
                }
                String endCode = "";
                for (int i = 0; i < 200; i++)
                {
                    endCode += i;
                }
                String haltMessage = Encoding.ASCII.GetString(resumeSync);
                if (haltMessage.Equals(endCode))
                {
                    startSync = true;
                    receivingSubdirectories = true;
                    metaData = true;
                    sendFileSendingNotification(EventArgs.Empty);
                }
                if (startSync) { Debug.Assert(false,"Ignore the following"); }
                else if (sendingfile)
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
                        if (i == 0)
                        {
                            byte[] retrieve = new byte[4];
                            for (i = 0; i < 4; i++)
                            {
                                retrieve[i] = fileContents[i];
                            }
                            IPAddress convert = new IPAddress(retrieve);
                            currentSocket = convert.ToString();
                            Debug.Assert(false, currentSocket);
                        }
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
                    sendFileSendingNotification(EventArgs.Empty);
                    receivingSubdirectories = false;
                    byte[] reply = { 1 };
                    senderSocket.Send(reply);
                    sendFileSendingNotification(EventArgs.Empty);
                }

                else if (NumberOfBytes >= FILENAMEBYTELIMIT && metaData)
                {
                    lock (lockMetaData)
                    {
                        //Name
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
                        //Date
                        byte[] date = new byte[FILEDATEBYTELIMIT];
                        int j = 0;
                        for (int i = FILENAMEBYTELIMIT; i < FILENAMEBYTELIMIT + FILEDATEBYTELIMIT; i++)
                        {
                            date[j] = fileContents[i];
                            j++;
                        }
                        String fileDate = Encoding.ASCII.GetString(date).Trim();
                        receivingFileName = (Encoding.ASCII.GetString(fileNameBytes)).Trim();
                        metaDate = DateTime.Parse(fileDate);
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
                        bool fileExist = false;
                        DateTime currentFile = new DateTime();
                        try
                        {
                            String[] fileNames = Directory.GetFiles(folderName);
                            foreach (String file in fileNames)
                            {
                                String refiningFile = folderName + "\\" + receivingFileName;
                                if (refiningFile.Equals(file))
                                {
                                    fileExist = true;
                                    currentFile = File.GetLastWriteTime(folderName + "\\" + file);
                                }
                            }
                            if (!fileExist)
                            {
                                Writer = new BinaryWriter(File.OpenWrite(folderName + "\\" + receivingFileName));
                                Writer.Write(fileContents);
                                Writer.Flush();
                                Writer.Close();
                                Writer.Dispose();
                                receivingFileName = "";
                                metaData = true;
                            }
                            else if ((metaDate > currentFile))
                            {
                                Writer = new BinaryWriter(File.OpenWrite(folderName + "\\" + receivingFileName));
                                Writer.Write(fileContents);
                                Writer.Flush();
                                Writer.Close();
                                Writer.Dispose();
                                receivingFileName = "";
                                metaData = true;
                            }
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
        public void Disconnect()
        {
            senderSocket.Dispose();
            senderSocket.Close();
            senderSocket = null;
        }



        public void checkSocketConnection()
        {
            new Thread(() =>
            {
                if (senderSocket != null)
                {
                    if (!senderSocket.Connected)
                    {
                        Debug.Assert(false, "Socket is Disconnected");
                    }
                }
                Thread.Sleep(1000);
            }).Start();
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
