using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
/// <summary>
/// Jimmy Chao
/// Lazer Incorporate
/// 012677182
/// CECS 327
/// </summary>
namespace FileTransferClient.Models
{
    public class Connection
    {
        //The Static port the program is going to be listening on
        private const int PORT = 4450;
        //The File size limitations
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTELIMIT = 100;
        //Receiving information storage variables
        private bool metaData;
        private bool receivingSubdirectories;
        private DateTime metaDate;
        private String folderName; //The client's sync folder
        private String receivingFileName;

        //This Computer
        private Socket Listener;
        private IPEndPoint endpoint;
        private Socket Handler;
        public event EventHandler FileSendingNotification;
        //Connecting to Other Computer
        private Socket senderSocket;

        //Lock to lock the binary writer until the current writer finishes writing 
        private static System.Object lockBinaryWriter = new System.Object();
        private static System.Object lockMetaData = new System.Object();

        //This is for sending value information
        public bool isConnected { get; set; }
        public bool sendingfile { get; set; }
        public string currentSocket { get; set; }
        public bool GoodReceive { get; set; }
        public bool startSync { get; set; }
        /// <summary>
        /// Constructor for the Connection class
        /// Sets the inital value
        /// </summary>
        /// <param name="folderName">The sync folder name</param>
        public Connection(string folderName)
        {
            //set the folder name into this class instance
            this.folderName = folderName;
            currentSocket = null;
            sendingfile = false;
            metaData = true;
            receivingSubdirectories = true;
            startSync = true;
        }
        /// <summary>
        /// Getter for the sync folder name
        /// </summary>
        /// <returns>The sync folder name</returns>
        public String GetSyncFolderName()
        {
            return folderName;
        }
        /// <summary>
        /// This function creates the connection to the other client to start the sync process
        /// </summary>
        /// <returns>returns null if successful and an error message if </returns>
        public string CreatePeerConnection()
        {
            try
            {
                //Check if the socket has been created
                if (Listener == null)
                {
                    //Create a socket with TCP protocol
                    SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
                    permission.Demand();
                    //Get this client's ip address on the network
                    string hostName = Dns.GetHostName();
                    IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);
                    //Set the listening end point for the client
                    endpoint = new IPEndPoint(hostAddress[1], PORT);
                    Listener = new Socket(hostAddress[1].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Listener.Bind(endpoint);
                    //Give the client 10 tries to create the connection
                    Listener.Listen(10);
                    //Set the receive call back function 
                    AsyncCallback callBack = new AsyncCallback(CallBack);
                    //Set the callback function on the socket
                    Listener.BeginAccept(callBack, Listener);
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return null;
        }
        /// <summary>
        /// This function connects to the other client in the network
        /// </summary>
        /// <param name="Address">The other client's end point</param>
        /// <returns>A message if the connection was successful or not</returns>
        public string ConnectToPeer(string Address)
        {
            try
            {
                //Creates the permission for the socket and adds the TCP protocol
                SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
                permission.Demand();
                //Parses the ip address from a string to a IPAddress type
                IPAddress ipAddress = IPAddress.Parse(Address);
                //Uses the other client's ip address to set the endpoint of the messages this client is sending to
                IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, PORT);
                //Set's the message sendingto TCP protocol
                senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                senderSocket.Connect(ipEndpoint);
                //Flag that this client is connected to another socket
                isConnected = true;
            }
            catch (Exception ex) { return ex.ToString(); }
            return "Success";
        }
        /// <summary>
        /// Function call to ping all ip addresses in the network and find all active devices on the network
        /// </summary>
        /// <returns>List of ip addresses</returns>
        public List<String> GetIpAddress()
        {
            //Make a temp list to hold all information requested from the arp table
            List<String> ipAddressList = new List<String>();
            //Create start information to put into the ARP process
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = Environment.SystemDirectory + "\\ARP.EXE";
            //Gather all connectable ip addresses from the ARP table
            processInfo.Arguments = " -a";
            //Start the ARP process
            Process process = new Process();
            process.StartInfo = processInfo;
            //Prompt the process not to show the command prompt of the process
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            //Start Process
            process.Start();
            //Get the output of the process
            StreamReader x = process.StandardOutput;
            //filter out the dynamic ip addresses in the output and add them to the list
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
        /// <summary>
        /// This is the function to tell the other client they can start listening for messages in the network again
        /// </summary>
        public void SendResumeMessage()
        {
            //provide an endcode for the client to know that it has received the last file
            String endCode = "";
            for (int i = 0; i < 200; i++)
            {
                endCode += i;
            }
            //The endcode message is 490 bytes long
            byte[] haltMessage = Encoding.ASCII.GetBytes(endCode); //490bytes length
            try
            {
                //Send the byte stream
                senderSocket.Send(haltMessage);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());
            }
            //flag this client is the sending client
            sendingfile = true;
        }
        /// <summary>
        /// This is the function to send the actual file to the client
        /// </summary>
        /// <param name="file"> The File name in the sync folder</param>
        public void SendFile(String file)
        {
            //Add the directory of where that file is in the sync folder
            String fileDirectory = folderName + "\\" + file;
            //Create the byte stream to send the file
            byte[] metaData = File.ReadAllBytes(fileDirectory);
            try
            {
                //send the file
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            //flag this client is the sending client
            sendingfile = true;

        }
        /// <summary>
        /// This is the function to send the meta data of the file to the Client
        /// The meta data includes the file name and the date it was last modified
        /// </summary>
        /// <param name="file">The file name</param>
        public void SendFileMetaData(String file)
        {
            //Set the byte stream the first 400 bytes is for the file name then 
            //the 100 bytes are for the date last modified
            byte[] metaData = new byte[FILEDATEBYTELIMIT + FILENAMEBYTELIMIT];
            //add the directory to the file
            String fileDirectory = folderName + "\\" + file;
            int counter = 0;
            //Encode the file name into the byte stream
            foreach (byte x in Encoding.ASCII.GetBytes(file))
            {
                metaData[counter] = x;
                counter++;
            }
            counter = FILENAMEBYTELIMIT;
            //Encode the date last modified of the file into the byte stream
            foreach (byte x in Encoding.ASCII.GetBytes(File.GetLastWriteTime(fileDirectory).ToString()))
            {
                metaData[counter] = x;
                Console.WriteLine(metaData[counter]);
                counter++;
            }
            try
            {
                //send the byte stream
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            //flag this client is the sending client
            sendingfile = true;
        }
        /// <summary>
        /// This is the function that send all subdirectory folders to the client 
        /// </summary>
        /// <param name="subDirectories">The List of subdirectories to be sent</param>
        public void SendSubdirectories(List<String> subDirectories)
        {
            //Set the byte stream
            byte[] metaData = new byte[FILEBYTELIMIT];
            int counter = 0;
            int currentspot = 1;
            //Each 500 index will hold one name of the directory
            int DIRECTORYBYTESIZE = 500;
            //This part also sends the ip address of this client to the other client
            string hostName = Dns.GetHostName();
            IPAddress[] hostAddress = Dns.GetHostAddresses(hostName);
            //Reserve the first 4 bytes as the ip address
            byte[] clientAddress = hostAddress[1].GetAddressBytes();
            foreach (byte address in clientAddress)
            {
                metaData[counter] = address;
                counter++;
            }
            //Next save the directories into the byte stream
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
                //send the byte streeam
                senderSocket.Send(metaData);
            }
            catch (Exception ex) { }
            //flag this client is the sending client
            sendingfile = true;

        }
        /// <summary>
        /// This is the callback function that receives any data that has been sent directly to it
        /// </summary>
        public void CallBack(IAsyncResult ar)
        {
            try
            {
                //Set the maximum number of information that can be received to 2 MB
                byte[] buffer = new byte[FILEBYTELIMIT];
                //Set the current listener socket for this callback function here
                Socket currentListener = (Socket)ar.AsyncState;
                Socket currentHandler = currentListener.EndAccept(ar);
                currentHandler.NoDelay = false;
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = currentHandler;
                //Set the function to handle the information 
                currentHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveFile), obj);
                AsyncCallback aCallback = new AsyncCallback(CallBack);
                //Start listening
                currentListener.BeginAccept(aCallback, currentListener);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.ToString());

            }

        }
        /// <summary>
        /// This function handles the information that was received from the other client either 
        /// retrieving the files and parsing them back to files/directories
        /// or receiving messages if the files were sent or not
        /// </summary>
        public void ReceiveFile(IAsyncResult ar)
        {
            //set temp byte stream holder
            byte[] fileContents = null;
            try
            {
                //Get asynchronous status
                object[] obj = (object[])ar.AsyncState;
                //Get the byte stream information
                fileContents = (byte[])obj[0];
                Handler = (Socket)obj[1];
                int NumberOfBytes = fileContents.Length;
                //stop the automatic syncing for this client
                startSync = false;
                //Check if the message was the end code to resume the automatic syncing for this client
                byte[] resumeSync = new byte[490];
                //converts the first 4 bytes into an address to check if the containing message was a good ip address 
                byte[] checkGoodAddress = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    checkGoodAddress[i] = fileContents[i];
                }
                //Create the ip address from the ip address collected
                IPAddress isGoodAddress = new IPAddress(checkGoodAddress);
                for (int i = 0; i < 490; i++)
                {
                    resumeSync[i] = fileContents[i];
                }
                //recreate the endcode
                String endCode = "";
                for (int i = 0; i < 200; i++)
                {
                    endCode += i;
                }
                //Decodes the message to see if the byte strea is the endcode
                String haltMessage = Encoding.ASCII.GetString(resumeSync);
                //Check if decoded message is the endcode;
                if (haltMessage.Equals(endCode))
                {
                    startSync = true;
                    receivingSubdirectories = true;
                    metaData = true;
                    sendFileSendingNotification(EventArgs.Empty);
                }
                //This ignores the following Sync Message 
                if (startSync){}
                /////////////////////This is for the client that is sending///////////////////////////////////////
                /////////////////////This decodes the byte stream to check if the message received was good or not//////////////////////////////////////
                else if (sendingfile)
                {
                    sendFileSendingNotification(EventArgs.Empty);
                    if (fileContents[0] == 1) { GoodReceive = true; }
                    else { GoodReceive = false; }
                    sendingfile = false;
                }
                //Checks if an empty message was sent
                else if (isGoodAddress.ToString().Equals("0.0.0.0")&& currentSocket==null)
                {
                   //Ignore empty messages sent
                }
                ////////////////////////////////THIS IS FOR THE CLIENT RECEIVING THE MESESAGE////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////
                //This is to receive the subdirectories and makes directories in the synce file
                else if (NumberOfBytes >= FILEBYTELIMIT && receivingSubdirectories)
                {
                    List<String> receivedDirectories = new List<string>();
                    int index = 1;
                    //Go through all values in the byte stream to read the directories
                    for (int i = 0; i < FILEBYTELIMIT - 500; i++)
                    {
                        //Retreive the first 4 bytes to convert to and ip address to connect back to the client
                        if (i == 0)
                        {
                            byte[] retrieve = new byte[4];
                            for (i = 0; i < 4; i++)
                            {
                                retrieve[i] = fileContents[i];
                            }
                            IPAddress convert = new IPAddress(retrieve);
                            currentSocket = convert.ToString();
                        }
                        //Looks for the end of the name of the folder in the 500 bytes 
                        int spaces = 0;
                        for (int f = i; f < i + 500; f++)
                        {
                            if (fileContents[f] != 0)
                            {
                                spaces++;
                            }
                            else { break; }
                        }
                        //Gets the bytes and converts it into a string value
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
                    //Goes through the list of parsed names and make the directories for them
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

                    //Send a reply to the sending client that this client has received the files
                    sendFileSendingNotification(EventArgs.Empty);
                    receivingSubdirectories = false;
                    byte[] reply = { 1 };
                    senderSocket.Send(reply);
                    sendFileSendingNotification(EventArgs.Empty);
                }
                /////////////////////This receives the meta data of the file
                else if (NumberOfBytes >= FILENAMEBYTELIMIT && metaData)
                {
                    lock (lockMetaData)
                    {
                        //This gets the Name and parses it
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
                        //This gets the Date and parses it
                        byte[] date = new byte[FILEDATEBYTELIMIT];
                        int j = 0;
                        for (int i = FILENAMEBYTELIMIT; i < FILENAMEBYTELIMIT + FILEDATEBYTELIMIT; i++)
                        {
                            date[j] = fileContents[i];
                            j++;
                        }
                        //Trim the white spaces from the name
                        String fileDate = Encoding.ASCII.GetString(date).Trim();
                        receivingFileName = (Encoding.ASCII.GetString(fileNameBytes)).Trim();
                        metaDate = DateTime.Parse(fileDate);
                        //checks to see if the name received was good if it was reply with 1 else reply with 0
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
                //ACts when meta data is received then it creates the file
                else if (NumberOfBytes >= FILEBYTELIMIT && !metaData)
                {
                    //Lock the binary writer resource
                    lock (lockBinaryWriter)
                    {
                        //Uses the binary writer to conver the byte stream to file
                        BinaryWriter Writer;
                        bool fileExist = false;
                        //check date time of file if it exist
                        DateTime currentFile = new DateTime();
                        try
                        {
                            //checks if file exist
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
                            //if does not exist make a new file
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
                            //if does exist check if the date modified is older
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
                        //catch any bad byte
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
                            Writer.Write(fileContents);
                            Writer.Flush();
                            Writer.Close();
                            Writer.Dispose();
                            receivingFileName = "";
                            metaData = true;
                        }
                        //respond back with good respond
                        byte[] reply = { 1 };
                        senderSocket.Send(reply);
                        sendFileSendingNotification(EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!sendingfile) {
                    sendFileSendingNotification(EventArgs.Empty);
                    byte[] reply = { 0 };
                    senderSocket.Send(reply);
                    sendFileSendingNotification(EventArgs.Empty);
                }
                Debug.Assert(false, ex.Message);
            }

        }
        /// <summary>
        /// Disconnect the current socket
        /// </summary>
        public void Disconnect()
        {
            senderSocket.Dispose();
            senderSocket.Close();
            senderSocket = null;
        }
        /// <summary>
        /// Checks the socket if it is still connected
        /// </summary>
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
        /// <summary>
        /// This function pings every address in the network to see if the computer is connectable over the network
        /// </summary>
        public void PingAddress()
        {
            new Thread(() =>
            {
                //get this address
                string hostName = Dns.GetHostName();
                IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
                String ipAddressHost = iPAddress[1].ToString();
                //subsection it so that the it removes this client's ip address
                ipAddressHost = ipAddressHost.Substring(0, ipAddressHost.LastIndexOf('.') + 1);
                //goes through all 255 addresses in the network
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
                //this updates the UI
                sendFileSendingNotification(EventArgs.Empty);
            }).Start();

        }
        //event handling 
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
