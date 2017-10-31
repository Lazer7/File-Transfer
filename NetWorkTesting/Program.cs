using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetWorkTesting
{
    class Program
    {
        private const int FILEBYTELIMIT = 2000000;
        private const int FILENAMEBYTELIMIT = 500;
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
            byte[] metaData = new byte[2000500];
            int counter = 0;
            foreach (byte x in File.ReadAllBytes("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg"))
            {
                metaData[counter] = x;
                counter++;
            }
            counter = 2000000;
            foreach (byte x in Encoding.ASCII.GetBytes("ahri.jpg"))
            {
                metaData[counter] = x;
                counter++;
            }
            counter = 2000400;
            foreach (byte x in Encoding.ASCII.GetBytes(File.GetLastWriteTime("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg").ToString()))
            {
                metaData[counter] = x;
                Console.WriteLine(metaData[counter]);
            }
            // byte date = Convert.ToByte(File.GetLastWriteTime("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg"));
            //Console.WriteLine(File.GetLastWriteTime("C:\\Users\\Jimmy\\Desktop\\Client1\\ahri.jpg"));
            DateTime x2 = DateTime.Parse("5/23/2017 1:28:02 PM");
            Console.WriteLine(x2.ToString());


            int spaces = 0;
            int stringSize = 0;
            for (int i = 2000000; i < 2000500; i++)
            {
                if (metaData[i] == 0)
                {
                    spaces++;
                    Console.WriteLine("Here");
                }
                if (spaces == 2) { break; }
                stringSize++;
            }
            byte[] decodeName = new byte[stringSize];
            for (int i = 2000000; i < 2000000 + stringSize; i++)
            {
                decodeName[i - 2000000] = metaData[i];
            }
            String filename = (Encoding.ASCII.GetString(decodeName)).Trim();
            Console.Write(filename);
            ////////////////////////////////////////////////////////////////////////////////

        }
    }
}
