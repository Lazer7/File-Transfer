using FileTransferClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
/// <summary>
/// Jimmy Chao
/// Lazer Incorporate
/// 012677182
/// CECS 327
/// </summary>
namespace FileTransferClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Creates the sockets to connect to peers
        Connection peerConnection;
        //Contains list of connected IPAddresses
        List<String> connectedIPAddress;
        //Contains list of all files in the specified directory
        List<String> fileList;
        List<String> subdirectories;
        //(reply) check if file was sent correctly
        //(getNames) halts the refresh of the directory get list of files  
        bool reply, getNames, isConnected;
        //The current Socket the client is connected to
        string currentSocket;
        ////////////////////Window Form Functions ////////////////
        public MainWindow()
        {
            InitializeComponent();
            //Creates an empty list of connected ip
            connectedIPAddress = new List<string>();
            //Set all buttons to inenabled until user specifies a sync directory
            DisconnectButton.IsEnabled = false;
            ConnectingB.IsEnabled = false;
            RefreshButton.IsEnabled = false;
            SendButton.IsEnabled = false;
            //Brings Window into focus
            BringIntoView();
            Focus();
            //End's the Thread to refresh the sync folder directory
            this.Closed += CloseActiveThreads;

        }
        /// <summary>
        /// The function to connect to a computer once the
        /// Connect button has been pressed
        /// </summary>
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = AvailableIPAddressListBox.SelectedItem.ToString();
            //Adds the address name to the list of connected devices
            connectedIPAddress.Add(ipAddress);
            //Update the list onto the view
            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
            //Connect to the IPAddress
            peerConnection.ConnectToPeer(ipAddress);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //this event allows the program to continue to send data after receiving a message that the other client has recieved the previous data
            peerConnection.FileSendingNotification += fileReply;
            peerConnection.sendingfile = true;
            SendButton.IsEnabled = false;
            //End the sync folder refresh thread
            getNames = false;
            foreach (string ipAddress in connectedIPAddress)
            {
                currentSocket = ipAddress;
                //peerConnection.ConnectToPeer(ipAddress);
                ////////////////////////Start if Single Peer Transfer/////////////////////////////////////////////////////
                reply = true;
                peerConnection.SendSubdirectories(subdirectories);
                while (reply) ;
                for (int i = 0; i < fileList.Count; i++)
                {
                    reply = true;
                    peerConnection.SendFileMetaData(fileList[i]);
                    //Wait for Reply from other client
                    while (reply) ;
                    //Check if the data sent was received correctly
                    if (!peerConnection.GoodReceive)
                    {
                        //roll back and resend the file again
                        i--;
                        continue;
                    }
                    reply = true;
                    peerConnection.SendFile(fileList[i]);
                    //Wait for Reply from other client
                    while (reply) ;
                    if (!peerConnection.GoodReceive)
                    {
                        //roll back and resend the file again
                        i--;
                        continue;
                    }
                }
                //////////////////////////////////End Single Peer Connection///////////////////////////////////
            }
            currentSocket = null;
            peerConnection.sendingfile = false;
            //Remove event of receving file Responses
            peerConnection.FileSendingNotification -= fileReply;
            SendButton.IsEnabled = true;
            //Restart sync folder refresh directory
            GetFileNames();
        }
        /// <summary>
        /// Thread to automatically send files to the peer client
        /// </summary>
        private void SendFile()
        {
            //Create a Thread to run the syncing of folders every 10-15 seconds
            new Thread(() =>
            {
                while (true)
                {
                    if (peerConnection.startSync && peerConnection.isConnected)
                    {
                        //Updates UI components to notify the current client is starting to sync
                        this.Dispatcher.InvokeAsync(() =>
                        {
                            StatusLabel.Content = "Sending";
                            SendButton.IsEnabled = false;
                        });
                        //this event allows the program to continue to send data after receiving a message that the other client has recieved the previous data
                        peerConnection.FileSendingNotification += fileReply;
                        //Notify the sockets that the receving information is about good or bad receives
                        peerConnection.sendingfile = true;
                        //End the sync folder refresh thread
                        getNames = false;
                        //Gets the current address to send to
                        currentSocket = connectedIPAddress[0];
                        ////////////////////////Start if Single Peer Transfer/////////////////////////////////////////////////////
                        reply = true;
                        //Begin the sending of directories
                        peerConnection.SendSubdirectories(subdirectories);
                        //Wait for a reply back from the receiving client to see if they got the subdirectories
                        while (reply) ;
                        //Loop through all the files in the directory
                        for (int i = 0; i < fileList.Count; i++)
                        {
                            reply = true;
                            peerConnection.SendFileMetaData(fileList[i]);
                            //Wait for Reply from other client
                            while (reply) ;
                            //Check if the data sent was received correctly
                            if (!peerConnection.GoodReceive)
                            {
                                //roll back and resend the file again
                                i--;
                                continue;
                            }
                            reply = true;
                            peerConnection.SendFile(fileList[i]);
                            //Wait for Reply from other client
                            while (reply) ;
                            if (!peerConnection.GoodReceive)
                            {
                                //roll back and resend the file again
                                i--;
                                continue;
                            }
                        }
                        //////////////////////////////////End Single Peer Connection///////////////////////////////////
                        peerConnection.SendResumeMessage();
                        //Clear sending node after sync is complete
                        currentSocket = null;
                        peerConnection.sendingfile = false;
                        peerConnection.startSync = true;
                        //Remove event of receving file Responses
                        peerConnection.FileSendingNotification -= fileReply;
                        //Restart sync folder refresh directory
                        GetFileNames();
                        //Update UI components
                        this.Dispatcher.InvokeAsync(() =>
                        {
                            StatusLabel.Content = "Standby";
                            SendButton.IsEnabled = true;
                        });
                        //Wait 10 seconds to check for syncing again
                        Thread.Sleep(10000);
                    }
                    Thread.Sleep(5000);
                }
            }).Start();
        }
        /// <summary>
        /// The function to refresh the arp table and checking any new 
        /// devices or remove any devices that disconnected
        /// </summary>
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.PingAddress();
            peerConnection.FileSendingNotification += EventReached;
        }
        /// <summary>
        /// Disconnect the connected ip address that was selected from the listbox
        /// </summary>
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.Disconnect();
            String ipAddress = (String)ConnectedIPAddressListBox.SelectedItem;
            connectedIPAddress.Remove(ipAddress);
            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
        }
        /// <summary>
        /// This is the button that selects the folder to sync between the clients
        /// </summary>
        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            ///////////////////////GET FOLDER FOR SYNCING//////////////////
            using (var open = new System.Windows.Forms.FolderBrowserDialog())
            {
                open.Description = "Locate your Folder to Sync";
                open.ShowDialog();
                peerConnection = new Connection(open.SelectedPath);
                open.Dispose();
            }
            ////////////////////END GETTING FOLDER FOR SYNCING/////////////
            //Add the reconnect event handler to reconnect to the socket after receiving a message
            peerConnection.FileSendingNotification += Reconnect;
            //Ping all ip addressses on the network
            peerConnection.PingAddress();
            //Creates a socket listener for this client
            peerConnection.CreatePeerConnection();
            ///////Gets and display the host computer's ip address////////
            string hostName = Dns.GetHostName();
            IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            LabelChecking.Content = "Your IP Address is " + iPAddress[1].ToString();
            //////////////////////////////////////////////////////////////
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            //Start Thread to refresh any new files that has been inputted into the sync folder
            GetFileNames();
            //Start the Thread to sync the folder to the other client
            SendFile();
            //This updates the receiving ip address
            updateConnections();
            //Enabling client functionalities
            DisconnectButton.IsEnabled = true;
            ConnectingB.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            SendButton.IsEnabled = true;
            currentSocket = null;
            StatusLabel.Content = "Standby";
            isConnected = false;
        }
        ////////////////////Data Retrieval Functions//////////////////////////////////////
        /// <summary>
        /// This gets all the file names in the sync folder and displays them in the listbox
        /// </summary>
        private void GetFileNames()
        {
            getNames = true;
            new Thread(() =>
            {

                while (getNames)
                {
                    subdirectories = subdirectoryEntries(peerConnection.GetSyncFolderName());
                    fileList = new List<string>();
                    String[] tempList = Directory.GetFiles(peerConnection.GetSyncFolderName());
                    foreach (String file in tempList)
                    {
                        String fileName = file.Substring(file.LastIndexOf('\\') + 1);
                        fileList.Add(fileName);
                    }
                    foreach (string directory in subdirectories)
                    {
                        tempList = Directory.GetFiles(peerConnection.GetSyncFolderName() + directory);
                        foreach (String file in tempList)
                        {
                            String fileName = directory + "\\" + file.Substring(file.LastIndexOf('\\') + 1);
                            fileList.Add(fileName);
                        }
                    }
                    this.Dispatcher.InvokeAsync(() =>
                    {
                        FileListBox.ItemsSource = fileList;
                        FileListBox.Items.Refresh();
                    });
                    Thread.Sleep(2000);
                }
            }).Start();
        }
        /// <summary>
        /// This gets the sub directories in the sync folder recursively
        /// </summary>
        /// <param name="homeDirectory"></param> The primary home directory of the sync folder
        /// <param name="previousDirectory"></param> The previous directory in the sync folder 
        /// <returns>List of </returns>
        List<String> subdirectoryEntries(string homeDirectory, string previousDirectory = "")
        {
            string[] subdirectory = Directory.GetDirectories(homeDirectory);
            List<String> internalsubdirectory = new List<string>(); ;
            foreach (String x in subdirectory)
            {
                String currentDirectory = x.Substring(x.LastIndexOf('\\'));
                if (previousDirectory.Equals(""))
                {
                    internalsubdirectory.Add(currentDirectory);
                }
                else
                {
                    internalsubdirectory.Add(previousDirectory + x.Substring(x.LastIndexOf('\\')));
                }
                if (Directory.GetDirectories(x) != null)
                {
                    List<String> temp = subdirectoryEntries(x, previousDirectory + currentDirectory);
                    foreach (String y in temp)
                    {
                        internalsubdirectory.Add(y);
                    }
                }

            }

            return internalsubdirectory;
        }
        /// <summary>
        /// Updates the end client peer's connection
        /// Once it receives a message from a sending client and connect to it
        /// </summary>
        private void updateConnections()
        {
            new Thread(() =>
            {
                while (true)
                {
                    //Check if the receiving client is not connected to anything and if the current client does not have that 
                    //client on their connected list
                    if (peerConnection.currentSocket != null && !connectedIPAddress.Contains(peerConnection.currentSocket))
                    {
                        connectedIPAddress.Add(peerConnection.currentSocket);
                        this.Dispatcher.InvokeAsync(() =>
                        {
                            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
                        });
                        isConnected = true;
                    }
                }
            }).Start();
        }
        ///////////////////////// EVENT FUNCTIONS//////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void EventReached(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            });
            peerConnection.FileSendingNotification -= EventReached;
        }
        private void CloseActiveThreads(object sender, EventArgs e)
        {
            getNames = false;
        }
        private void fileReply(object sender, EventArgs e)
        {
            reply = false;
        }
        private void Reconnect(object sender, EventArgs e)
        {
            if (currentSocket != null)
            {
                peerConnection.ConnectToPeer(currentSocket);
            }
            else if (peerConnection.currentSocket != null)
            {
                peerConnection.ConnectToPeer(peerConnection.currentSocket);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////
    }
}