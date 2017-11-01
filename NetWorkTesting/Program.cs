using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetWorkTesting
{
    class Program
    {
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 400;
        private const int FILEDATEBYTLELIMIT = 100;
        private static bool threadRunning;
        static void Main(string[] args)
        {

            //String fileDirectory = "C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg";
            //byte[] fileContents = File.ReadAllBytes(fileDirectory);
            //    for (int i = 0; i < fileContents.Length; i++) {
            //        Console.Write(fileContents[i].ToString());
            //            }


            /////////////////GET HOST MACHINE'S IP ADDRESS//////////////////
            //string hostName = Dns.GetHostName();
            //IPAddress[] iPAddress =  Dns.GetHostAddresses(hostName);
            //Console.WriteLine(iPAddress[1].ToString());
            ////////////////////////////////////////////////////////////////

            //////////////GET ALL IP ADDRESSES ON A NETWORK/////////////////
            //List<IPAddress> ipAddressList = new List<IPAddress>();
            //ProcessStartInfo processInfo = new ProcessStartInfo();
            //processInfo.FileName = Environment.SystemDirectory+"\\ARP.EXE";
            //processInfo.Arguments = " -a";
            //Process process = new Process();
            //process.StartInfo = processInfo;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardInput = true;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.Start();
            //StreamReader x = process.StandardOutput;
            //while (!x.EndOfStream)
            //{
            //    String ipAddress = x.ReadLine().Trim();
            //    if (ipAddress.Contains("dynamic"))
            //    {
            //        String trueIP = ipAddress.Substring(0, ipAddress.IndexOf(" "));
            //        ipAddressList.Add(IPAddress.Parse(trueIP));
            //    }
            //}
            //foreach (IPAddress hey in ipAddressList)
            //{
            //    Console.WriteLine(hey.ToString());
            //}
            /////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////

            ///////////////////////PINGING ALL ADDRESSES//////////////////////////////////////////
            //string hostName = Dns.GetHostName();
            //IPAddress[] iPAddress = Dns.GetHostAddresses(hostName);
            //String ipAddressHost = iPAddress[1].ToString();
            //ipAddressHost = ipAddressHost.Substring(0, ipAddressHost.LastIndexOf('.') + 1);
            //Console.WriteLine(ipAddressHost.Substring(0, ipAddressHost.LastIndexOf('.')+1));
            //for (int i = 1; i < 255; i++)
            //{
            //    ProcessStartInfo processInfo = new ProcessStartInfo();
            //    processInfo.FileName = Environment.SystemDirectory + "\\PING.EXE";
            //    processInfo.Arguments = ipAddressHost+i+" -n 1";
            //    Console.WriteLine(ipAddressHost + i + " -n 1");
            //    processInfo.UseShellExecute = false;
            //    processInfo.CreateNoWindow = true;
            //    Process process = new Process();
            //    process.StartInfo = processInfo;
            //    process.Start();   
            //}
            //////////////////////////////////////////////////////////////////////////////////////


            ////////////////////Getting all file names from a directory///////////////////////
            //String[] tempList = Directory.GetFiles("C:\\Users\\Jimmy\\Desktop\\CECS_327_Distributed_Network");
            //String[] fileList = new String[tempList.Length];
            //int currentfile = 0;
            //foreach (String file in tempList)
            //{
            //    String fileName = file.Substring(file.LastIndexOf('\\')+1);
            //    Console.WriteLine(fileName);
            //    fileList[currentfile] = file;
            //}

            //////////////////////////////////////////////////////////////////////////////


            ////////////////////////Byte Reading /////////////////////////////////////////////
            //String fileDirectory = "C:\\Users\\Jimmy\\Desktop\\Client1\\Helloworld.txt";
            //String file = "Helloworld.txt";
            //byte[] metaData = new byte[FILEBYTELIMIT + FILEDATEBYTLELIMIT + FILENAMEBYTELIMIT];
            //int counter = 0;
            //foreach (byte x in File.ReadAllBytes(fileDirectory))
            //{
            //    metaData[counter] = x;
            //    Console.Write(metaData[counter]);
            //    counter++;
            //}

            //counter = FILEBYTELIMIT;

            //foreach (byte x in Encoding.ASCII.GetBytes(file))
            //{

            //    metaData[counter] = x;
            //    counter++;

            //}
            //counter = FILEBYTELIMIT + FILENAMEBYTELIMIT;
            //foreach (byte x in Encoding.ASCII.GetBytes(File.GetLastWriteTime(fileDirectory).ToString()))
            //{
            //    metaData[counter] = x;
            //    Console.WriteLine(metaData[counter]);
            //}
            //// byte date = Convert.ToByte(File.GetLastWriteTime("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg"));
            ////Console.WriteLine(File.GetLastWriteTime("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg"));
            ////  DateTime x2 = DateTime.Parse("5/23/2017 1:28:02 PM");
            //int spaces = 0;
            //int stringSize = 0;
            //for (int i = FILEBYTELIMIT; i < FILEBYTELIMIT + FILENAMEBYTELIMIT; i++)
            //{
            //    if (metaData[i] == 0)
            //    {
            //        spaces++;
            //    }
            //    if (spaces == 5) { break; }
            //    stringSize++;
            //}
            //byte[] fileNameBytes = new byte[stringSize];
            //for (int i = FILEBYTELIMIT; i < FILEBYTELIMIT + stringSize; i++)
            //{
            //    fileNameBytes[i - FILEBYTELIMIT] = metaData[i];
            //}
            //String fileName = (Encoding.ASCII.GetString(fileNameBytes)).Trim();
            //Console.Write(fileName);
            ////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////Threading ////////////////////////////////
            DummyClass dummyClass = new DummyClass();
            dummyClass.ThreshholdReached += EventReached;
            dummyClass.startThread();
            threadRunning = true;
            while (threadRunning) ;
            ///////////////////////////////////////////////////////////////////////
        }
        static void EventReached(object sender, EventArgs e)
        {
            threadRunning = false;
        }

    }

    class DummyClass
    {
        public event EventHandler ThreshholdReached;

        protected virtual void OnThreshholdReached(EventArgs e)
        {
            EventHandler handler = ThreshholdReached;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public void startThread()
        {
            new Thread(() =>
            {

                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                for (int i = 1; i < 10000000; i++)
                {
                    Console.WriteLine("Hello, world");
                    if (i % 100==0) { Console.WriteLine("FLicker"); }
                }
                OnThreshholdReached(EventArgs.Empty); 
            }).Start();
        }
    }
}
