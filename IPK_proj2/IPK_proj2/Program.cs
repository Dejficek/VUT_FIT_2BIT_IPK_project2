using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.NetworkInformation;
using SharpPcap;
using PacketDotNet;

namespace IPK_proj
{

    public static class Globals
    {
        public static String inter;
        public static String port = null;
        public static int numberOfPackets = 1;
        public static bool tcp = false;
        public static bool udp = false;

        public static CaptureDeviceList devices;

        public static int counter = 0;
    }
    class Program
    {
        static void Main(string[] args)
        {
            for(int i = 0; i < args.Length; i++)
            {
                // arguments parsing
                switch (args[i])
                {
                    case "-i":
                        Globals.inter = args[i + 1];
                        break;
                    case "-p":
                        Globals.port = args[i + 1];
                        break;
                    case "-t":
                    case "--tcp":
                        Globals.tcp = true;
                        break;
                    case "-u":
                    case "--udp":
                        Globals.udp = true;
                        break;
                    case "-n":
                        Globals.numberOfPackets = int.Parse(args[i + 1]);
                        break;
                    case "-h":
                    case "--help":
                        printHelp();
                        break;
                }
            }

            if(Globals.udp == false && Globals.tcp == false)
            {
                Globals.udp = true;
                Globals.tcp = true;
            }

            //getting computer's availible network interfaces and checking, if given interface match
            getInterfaces();
            var currentInterface = checkInterface();
            getPackets(currentInterface);

        }


        public static void getPackets(ICaptureDevice currentInterface)
        {
            currentInterface.OnPacketArrival += new PacketArrivalEventHandler(device_onPacketArrival);

            try
            {
                currentInterface.Open(DeviceMode.Promiscuous, 3000);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Please run this application as administrator or with sudo");
                System.Environment.Exit(1);
            }

            currentInterface.Capture();

            currentInterface.Close();
        }

        private static void device_onPacketArrival(Object sender, CaptureEventArgs e)
        {
            var date = e.Packet.Timeval.Date;
            var microSeconds = e.Packet.Timeval.MicroSeconds;
            var data = e.Packet.Data;

            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            var ip = packet.Extract<PacketDotNet.IPPacket>();
            PacketDotNet.TcpPacket tcp = packet.Extract<PacketDotNet.TcpPacket>();
            PacketDotNet.UdpPacket udp = packet.Extract<PacketDotNet.UdpPacket>();

            // ip addresses
            String SourceName;
            String DestinationName;
            try
            {
                SourceName = getFQDN(ip.SourceAddress);
            }
            catch (Exception ex)
            {
                SourceName = ip.SourceAddress.ToString();
            }
            try
            {
                DestinationName = getFQDN(ip.DestinationAddress);
            }
            catch (Exception ex)
            {
                DestinationName = ip.DestinationAddress.ToString();
            }

            if (Globals.counter == Globals.numberOfPackets) System.Environment.Exit(0);

            if (tcp != null && Globals.tcp == true)
            {
                if(Globals.port != null)
                {
                    if (tcp.DestinationPort == int.Parse(Globals.port) || tcp.SourcePort == int.Parse(Globals.port))
                    {

                    }
                    else return;
                }
                
                Console.WriteLine(date.Hour + ":" + date.Minute + ":" + date.Second + "." + microSeconds + " " +
                     Dns.GetHostEntry(ip.SourceAddress).HostName + " : " + tcp.SourcePort + " > " + 
                     Dns.GetHostEntry(ip.DestinationAddress).HostName + " : " + tcp.DestinationPort);
            }
            else if(udp != null && Globals.tcp == true)
            {
                if(Globals.port != null)
                {
                    if ((udp.DestinationPort != int.Parse(Globals.port) || udp.SourcePort != int.Parse(Globals.port)))
                    {

                    }
                    else return;
                }
                
                Console.WriteLine(date.Hour + ":" + date.Minute + ":" + date.Second + "." + microSeconds + " " +
                    SourceName + " : " + udp.SourcePort + " > " +
                    DestinationName + " : " + udp.DestinationPort);
            }

            //parsing data to array of strings with hex value and array of strings with ascii value
            String hex = BitConverter.ToString(e.Packet.Data);
            //String ascii = System.Text.Encoding.ASCII.GetString(packet.Data);
            String[] hexData = hex.Split("-");
            String hexOutputData;


            for (int n = 0; n < hexData.Length; n += 16)
            {
                hexOutputData = "";

                for (int m = 0; m < 16; m++)
                {
                    if (n + m == hexData.Length) break;
                    hexOutputData += hexData[n + m] + " ";
                }
                Console.Write("0x" + (hexOutputData.Length / 3 + n - 16).ToString("X4") + ": ");
                Console.Write(hexOutputData);
                Console.WriteLine(ConvertHex(String.Join("", hexOutputData.Split(" "))));
            }
            Console.WriteLine();
            Globals.counter++;

        }


        public static String getFQDN(IPAddress address)
        {
            String hostName = Dns.GetHostEntry(address).HostName;
            if (hostName == Dns.GetHostName())
            {
                // if the hostname is local computer AND has no domain name, only hostname will be returned, else domainname with hostname
                var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                return string.IsNullOrWhiteSpace(ipProperties.DomainName) ? ipProperties.HostName : string.Format("{0}.{1}", ipProperties.HostName, ipProperties.DomainName);
            }
            else return hostName;
        }
        public static void getInterfaces()
        {
            // stores all availible interfaces on current device
            Globals.devices = CaptureDeviceList.Instance;
        }

       
        //printing all availible interfaces on current device
        public static void printInterfaces()
        {
            Console.WriteLine("Here are availible interfaces:");
            foreach(ICaptureDevice inc in Globals.devices)
            {
                Console.WriteLine(getFriendlyName(inc));
            }
            System.Environment.Exit(1);
        }

        //checking if given interface name matches any availible interface on current device
        //returns choosen interface. if interface doesn't match, null is returned
        public static ICaptureDevice checkInterface()
        {
            foreach(ICaptureDevice inc in Globals.devices)
            {
                if (getFriendlyName(inc) == Globals.inter) return inc;
            }
            Console.WriteLine("Invalid network interface.\n");
            printInterfaces();
            return null;
        }


        //parses interface informations and return string containing interface name
        public static String getFriendlyName(ICaptureDevice device)
        {
            String[] splitStr = device.ToString().Split(':', '\n');
            for(int i = 0; i < splitStr.Length; i++)
            {
                if (splitStr[i].Equals("FriendlyName"))
                {
                    return splitStr[i + 1].Trim();
                }
            }
            return "";
        }


        // converting string of packet data
        // hexString contains one line, tahts printing out in compressed format.
        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = "";

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = "";

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);

                    char character;
                    // if the character is whitespace, the character will be replaced with dot.
                    if(decval > 32 && decval < 127)
                    {
                        character = System.Convert.ToChar(decval);   
                    }
                    else
                    {
                        character = '.';
                    }
                    ascii += character;


                }
                return ascii;
            }
            catch (Exception ex) { return ""; }
        }

        public static void printHelp()
        {
            Console.WriteLine("===== Help ====\n");
            Console.WriteLine("This application is packet sniffer. It displays specified packets according to given arguments.");
            Console.WriteLine("This is implementation of IPK - 2nd project at FIT VUT using C# by David Rubý (xrubyd00) 2020.\n");
            Console.WriteLine("Argumens options:\n");
            Console.WriteLine("-h OR --help\t\t\twill dispaly help");
            Console.WriteLine("-i <interface name>\t\tspecifies what network interface to use.");
            Console.WriteLine("-p <port number>\t\tspecifies what port to listen on. If not specified, will listen to all ports.");
            Console.WriteLine("-t OR --tcp\t\t\twill display only TCP packets.");
            Console.WriteLine("-u OR --udp\t\t\twill display only UDP packets.");
            Console.WriteLine("-n <number>\t\t\twill display <number> packets. If not specified, will dispaly 1 packet.\n");
            Console.WriteLine("if --tcp AND --udp not specified, both protocols will be used");

            System.Environment.Exit(0);
        }
    }
}