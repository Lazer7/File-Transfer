Lazer's Peer to Peer Transfer File
Jimmy Chao
CECS 327

##File-Transfer
This is a peer to peer networking project that allows a user to select a file to sync over a network to other peers over 
the same network. This program finds all connectable devices on the network by pinging each ip address throughout the router, then
creates a listener on the host computer to start listening for any message (files) sent to the computer on a specific port. Then the 
client decodes the message into the file so that the user can interact with.

##FileTransferClient Project
This project holds the actual peer to peer networking file syncing program with a user interface. The code has business logic, views
and models all integrated together without a design pattern due to my current philosophy on this project which is decomposition 
(to get a working program done as soon as possible). There are 2 main important c# files that holds all my logic for peer to peer networking
which are **MainWindow.xaml.cs** view model under the View Folder and the **Connection.cs** in the Model Folder. In the **MainWindow.xaml.cs**
it contains the thread of the order of sending subdirectories -> file meta data -> file. It also contains the logic of finding the sync
folder directory and constantly update the folder to check if there were new file/subdirectory additions during the execution of the program.
In the **Connection.cs** file it holds all the logic of creating a listener on the host computer to listen for messages and how to deal with the 
message that was received by the host computer, in addition the logic to send to another client(endpoint/socket).
Most Important Functions of each file
In the **MainWindow.xaml.cs** the below functions are the important code for the criteria of this project
**SendFile** - This is the logic of order of sending subdirectories -> file meta data -> file, and tell the receiving clent when the end of the sync is
**Folder_Click** - This is to locate the sync and starts all the thread for locating files and syncing files
**GetFileNames** - This is the Thread to update any file changes within the sync folder
**subdirectoryEntries** - This is the Thread to locate all the subdirectories in the sync folder
In the **Connection.cs** the below functions are the important code for the criteria of this project
**CreatePeerConnection** - This creates the socket listener for the host computer to listen for any messages sent on the network
**ConnectToPeer** - This create a socket to provide an end point connection to the receiving client 
**GetIpAddress** - Gets all connectable IP Addresses from the ARP table
**SendFile** - This sends the byte stream of the file to the receiving client
**SendFileMetaData** - This send the byte stream of the file meta data which includes their name and their date last modified
**SendSubdirectories** - This sends the subdirectories in the sync folder to the client
**ReceiveFile** - This is the function that handles the receiving of byte streams from the host computer
**PingAddress** - This pings all 255 IP address accross the network

##NetWorkTesting Project
This project holds all test data and logic. This project was created to provide quick and simple business logic building and testing before
adding it to the FileTransfer Project for final use. This Project contains logic to ping all ip address on a network, call the ARP table,
and converting strings and files to byte streams and parsing them back to files and strings.

##Stage 1 of the Project
The first stage of the project was to send a static file over the network over hardcoded ip address (provide the program with the ip address of the program by putting it in as a string)
It was a a struggle at first trying as being new to networking and sockets I had to do various research on how to create a socket. I have decided on using the C# socket class found here
https://msdn.microsoft.com/en-us/library/system.net.sockets.socket(v=vs.110).aspx 
and the .NET framework to help make my business logic while using WPF as my User Interface client. For the inital stages of this project, I have research more into a peer to peer client
and uncovered how to make my program peer to peer which was by making the application both the server and the client to handle both sending and receiving files over the network. So in my
discovery I have first tested my program to have one computer be the client and another computer to be the server to test if the sockets will connect and send a simple string using the (send()) 
method in the Socket class. It took many tries but I have successfully created the connection between 2 computers over the same network. The next step was to make both computers a server and a client
in order to both send and receive a file. After creating the sockets to make each computer a host and a receiver, I started research on how to send a file over the network. I have found 
that we could convert a file into a byte stream and can send it over the network. After multiple testing I successfully got the file to send over the network and tested it with multiple files such as
lecture pdf notes for CECS 327, ahri pictures, and other files. This concludes the end of stage 1 of creating a peer to peer connection statically and sending files over the network

##Stage 2 of the Project
The second stage of the project was to dynamically get and allow the user to choose an ip address over the network rather than just hardcoding the ip address into the code. (The reason why 
we can't keep the hardcoded ip address is due to the fact that ip addresses changes on a computer when switching different routers and networks (which happens alot during development))
So the first attempt was to ping the broadcast address to try to get the ip addresses of the connected devices on the network, however it did not populate the arp table with those connectable
addresses. So instead, I have decided to ping every single ip address in the network to populate the arp table. This method was successful so in my program I called the PING.EXE process to ping all addresses 
in the network as the first 3 bytes of the address is the address location of the network and the last byte is the device dynamic ip address. So I have incorporated my function into the File Transfer Client and allowed
the user to select the ip the user wished to connect to

##Stage 3 of the Project
The third stage of the project was to send multiple files and directories to the receiving client and ensure that the file does/does not get overwritten based on timestamps.
The first step was to figure out what to send first. I have decided to send the directories first in order to have those in the sync folder on the other client so that the files
in the subdirectories can be saved in their apporpriate subdirectory. The next step was to send the file, however when I send the file it does not receive the name or the date modify
from the original sync folder. So I have decided to send something called a MetaFile byte stream holding that metadata of the file. Then I actually send the file to the other client.
On the receiving end of the client, it receives the byte stream and what it does is that it parses it back to a file or a subdirectory then send a message of 0 or 1 back to the client that
was sending in order for it to proceed to the next send. The reason behind this was that the receiving client was receiving the request faster than it was parsing it causing multiple
errors in the system. After finalizing that component I put this functionality in a thread in order to make it an automatic syncing.

By the way Anthony Giacalone is an awesome professor :D 