using FileTransferClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        String[] fileList;

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
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = AvailableIPAddressListBox.SelectedItem.ToString();
            connectedIPAddress.Add(ipAddress);
            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
            LabelChecking.Content = peerConnection.ConnectToPeer(ipAddress);
        }
        bool dataSent;
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.FileSendingNotification += FileReached;
            for (int i = 0; i < fileList.Length; i++)
            {
                dataSent = true;
                int start = 0;
 
                while (dataSent)
                {

                    Thread metadatathread = new Thread(() =>
                    {
                        peerConnection.SendFileMetaData(fileList[i]);
                        foreach (String ip in connectedIPAddress)
                        {
                            peerConnection.ConnectToPeer(ip);
                        }
                    });
                   
                    if (!metadatathread.IsAlive)
                    {
                        metadatathread.Start();
                    }
                }
                MessageBox.Show("Metadata Sent");
                dataSent = true;
                while (dataSent)
                {
                    Thread metadatathread = new Thread(() =>
                    {
                        peerConnection.SendFile(fileList[i]);
                    });
                    if (!metadatathread.IsAlive)
                    {
                        metadatathread.Start();
                        foreach (String ip in connectedIPAddress)
                        {
                            peerConnection.ConnectToPeer(ip);
                        }
                    }
                }
                MessageBox.Show("file Sent");

            }
        }
        void FileReached(object sender, EventArgs e)
        {
            dataSent = false;
        }
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = (String)ConnectedIPAddressListBox.SelectedItem;
            connectedIPAddress.Remove(ipAddress);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.PingAddress();
            peerConnection.FileSendingNotification += EventReached;
        }

        void EventReached(object sender, EventArgs e)
        {
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            peerConnection.FileSendingNotification -= EventReached;
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
            //Ping all ip addressses on the network
            peerConnection.PingAddress();
            //Creates a socket listener for this client
            peerConnection.CreatePeerConnection();
            ///////Gets and display the host computer's ip address////////
            string hostName = Dns.GetHostName();
            IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            LabelChecking.Content = "Your IP Address is" + iPAddress[1].ToString();
            //////////////////////////////////////////////////////////////
            //Sets 
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            GetFileNames();
            DisconnectButton.IsEnabled = true;
            ConnectingB.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            SendButton.IsEnabled = true;
        }
        private void GetFileNames()
        {
            String[] tempList = Directory.GetFiles(peerConnection.GetSyncFolderName());
            fileList = new String[tempList.Length];
            int currentfile = 0;
            foreach (String file in tempList)
            {
                String fileName = file.Substring(file.LastIndexOf('\\') + 1);
                Console.WriteLine(fileName);
                fileList[currentfile] = fileName;
                currentfile++;
            }

        }

    }
}
