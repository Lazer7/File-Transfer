﻿using FileTransferClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;

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
        String currentIpAddress;
        //(geply) check if file was sent correctly
        //(getNames) halts the refresh of the directory get list of files  
        bool reply,getNames;
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
            currentIpAddress = null;
        }
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = AvailableIPAddressListBox.SelectedItem.ToString();
            //Creates a socket for the selected ip 
            connectedIPAddress.Add(ipAddress);
            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
            LabelChecking.Content = peerConnection.ConnectToPeer(ipAddress);
            
        }
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //this event allows the program to continue to send data after receiving a message that the other client has recieved the previous data
            peerConnection.FileSendingNotification += fileReply;
            SendButton.IsEnabled = false;
            //End the sync folder refresh thread
            getNames = false;
            for(int index = 0; index < connectedIPAddress.Count; index++)
            {
                currentIpAddress = connectedIPAddress[index];
                MessageBox.Show(currentIpAddress);
                peerConnection.ConnectToPeer(currentIpAddress);
                reply = true;
                peerConnection.SendStartEnd();
                while (reply) ;
                ///////////////////////////////Start Of Single Peer Connection///////////////////////
                reply = true;
                peerConnection.SendSubdirectories(subdirectories);
                while (reply) ;

                for (int i = 0; i < fileList.Count; i++)
                {
                    reply = true;
                    peerConnection.SendFileMetaData(fileList[i]);
                    //Wait for Reply from other client
                    while (reply);

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
                ///////////////////////////////End Of Single Peer Connection///////////////////////
                MessageBox.Show("They Shouldn't be here yet");
                peerConnection.SendStartEnd();
            }
            currentIpAddress = null;
            //Remove event of receving file Responses
            peerConnection.FileSendingNotification -= fileReply;
            SendButton.IsEnabled = true;
            //Restart sync folder refresh directory
            GetFileNames();
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.PingAddress();
            peerConnection.FileSendingNotification += EventReached;
        }
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = (String)ConnectedIPAddressListBox.SelectedItem;
            connectedIPAddress.Remove(ipAddress);
        }
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
            ///////////////////////////////////////////////////////////////
            peerConnection.FileSendingNotification += Reconnect;
            //Ping all ip addressses on the network
            peerConnection.PingAddress();
            //Creates a socket listener for this client
            peerConnection.CreatePeerConnection();
            ///////Gets and display the host computer's ip address////////
            string hostName = Dns.GetHostName();
            IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            LabelChecking.Content = "Your IP Address is" + iPAddress[1].ToString();
            //////////////////////////////////////////////////////////////
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            GetFileNames();
            DisconnectButton.IsEnabled = true;
            ConnectingB.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            SendButton.IsEnabled = true;
        }
        ////////////////////Data Retrieval Functions//////////////////////////////////////
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
                        tempList = Directory.GetFiles(peerConnection.GetSyncFolderName()+directory);
                        foreach (String file in tempList)
                        {
                            String fileName = directory+"\\"+ file.Substring(file.LastIndexOf('\\') + 1);
                            fileList.Add(fileName);
                        }
                        this.Dispatcher.InvokeAsync(() =>
                        {
                            FileListBox.ItemsSource = fileList;
                            FileListBox.Items.Refresh();
                        });
                    }
                    Thread.Sleep(2000);
                }
            }).Start();
        }
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
        ///////////////////////// EVENT FUNCTIONS//////////////////////////////////////
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
            if (currentIpAddress != null)
            {
                peerConnection.ConnectToPeer(currentIpAddress);
            }
            else if(peerConnection.currentSocket!=null)
            {
                peerConnection.ConnectToPeer(peerConnection.currentSocket);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////
    }
}