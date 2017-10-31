using FileTransferClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
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
        Connection peerConnection;
        List<String> connectedIPAddress;
        
        public MainWindow()
        {
            InitializeComponent();
            DisconnectButton.IsEnabled = false;
            ConnectingB.IsEnabled = false;
            RefreshButton.IsEnabled = false;
            SendButton.IsEnabled = false;
            BringIntoView();
            Focus();
        }

        private void ConnectingB_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = AvailableIPAddressListBox.SelectedItem.ToString();
            connectedIPAddress.Add(ipAddress);
            ConnectedIPAddressListBox.ItemsSource = connectedIPAddress;
            LabelChecking.Content = peerConnection.ConnectToPeer(ipAddress);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string errorCheck = peerConnection.SendFile("ahri.jpg");
            if (errorCheck!= null)
            {
                LabelChecking.Content = errorCheck;
            }
            peerConnection.ConnectToPeer("192.168.1.7");
            LabelChecking.Content = "ReConnected";
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            String ipAddress = (String) ConnectedIPAddressListBox.SelectedItem;
            connectedIPAddress.Remove(ipAddress);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            peerConnection.PingAddress();
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
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
            peerConnection.PingAddress();
            connectedIPAddress = new List<string>();
            peerConnection.CreatePeerConnection();
            string hostName = Dns.GetHostName();
            IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            LabelChecking.Content = "Your IP Address is" + iPAddress[1].ToString();
            AvailableIPAddressListBox.ItemsSource = peerConnection.GetIpAddress();
            DisconnectButton.IsEnabled = true;
            ConnectingB.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            SendButton.IsEnabled = true;
        }
    }
}
