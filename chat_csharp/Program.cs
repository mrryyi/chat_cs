using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;

public class SynchronousSocketClient
{ 

    static IPAddress IPAddr_;
    static Socket sender;
    static bool programGoing_ = true;
    static String currentSetup;
    static String currentPunchline;

    public static void ThreadReceive()
    {

        byte[] recvBuffer = new byte[1024];

        int bytesRecv;

        while (programGoing_)
        {
            try
            {
                bytesRecv = sender.Receive(recvBuffer);
                Console.WriteLine(Encoding.UTF8.GetString(recvBuffer, 0, bytesRecv));
            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("omg 2 much");
            }
        }
    }

    public static String getWebData()
    {
        string webData = null;
        string urlAddress = "https://us-central1-dadsofunny.cloudfunctions.net/DadJokes/random/jokes";

        //try the URI, fail out if not successful 

        HttpWebRequest request;
        HttpWebResponse response;


        try
        {
            request = (HttpWebRequest)WebRequest.Create(urlAddress);
            //request.Headers["Accept"] = "text/plain";
            response = (HttpWebResponse)request.GetResponse();
        }

        catch
        {
            //this could be modified for specific responses if needed
            return null;
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = null;
            if (response.CharacterSet == null)
            {
                readStream = new StreamReader(receiveStream);
            }
            else
            {
                readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
            }

            webData = readStream.ReadToEnd();

            response.Close();
            readStream.Close();


            Console.WriteLine(webData);
        }
        return webData;
    }

    public static bool getJoke()
    {
        String data = getWebData();

        bool valid = false;

        if (data != null) { 
            JObject o = JObject.Parse(data);
            currentSetup = o["setup"].Value<String>();
            currentPunchline = o["punchline"].Value<String>();
            valid = true;
        }
        else
        {
            data = "Det var en gång och den var sandad.";
        }
        return valid;
    }

    public static void SendJoke()
    {
        byte[] msg = new byte[1024];
        int bytesSent;

        // Encode the data string into a byte array.  
        msg = Encoding.UTF8.GetBytes(currentSetup);
        Console.WriteLine("Sent " + currentSetup);

        // Send the data through the socket.  
        bytesSent = sender.Send(msg);


        Thread.Sleep(4000);

        // Encode the data string into a byte array.  
        msg = Encoding.UTF8.GetBytes(currentPunchline);
        Console.WriteLine("Sent " + currentPunchline);

        // Send the data through the socket.  
        bytesSent = sender.Send(msg);

    }

    public static bool CommandHandle( ref string cmdStr )
    {

        bool eligible = true;

        if (cmdStr.Contains("dad"))
        {
            if (getJoke())
            {
                Thread jokeThread = new Thread(new ThreadStart(SendJoke));
                jokeThread.Start();
                eligible = false;
            }
        }
        else
        {
            Console.WriteLine("Command not recognized.");
            eligible = false;
        }

        return eligible;
    }

    public static void ThreadSend()
    {

        string input;
        int bytesSent;

        byte[] msg = new byte[1024];

        bool sendEligible;

        while (programGoing_)
        {
            input = Console.ReadLine();

            if (input.Length > 0)
            {
                sendEligible = true;

                if (input[0] == 'q')
                {
                    programGoing_ = false;
                    sendEligible = false;
                }
                if (input[0] == '!')
                {
                    sendEligible = CommandHandle(ref input);
                }

                if (sendEligible)
                {
                    // Encode the data string into a byte array.  
                    msg = Encoding.UTF8.GetBytes(input);

                    sending = true;
                    // Send the data through the socket.  
                    bytesSent = sender.Send(msg);
                    sending = false;
                }
                else
                {
                    Console.WriteLine("MEssagE iNelIgIblEIe");
                }
            }
            
        }
    }

    public static void StartClient()
    {
        // Data buffer for incoming data.  
        byte[] bytes = new byte[1024];

        /*
        Console.WriteLine("Enter a host: ");
        String host = Console.ReadLine();
        Console.WriteLine("Enter a port: ");
        var port = Console.ReadLine();*/

        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.  
            // This example uses port 1234 on the local computer.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            string ipAddress = "127.0.0.1";
            //string ipAddress = "172.16.117.80";
            //string ipAddress = "172.16.118.44";

            IPAddr_ = IPAddress.Parse(ipAddress);

            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(IPAddr_, 1234);

            // Create a TCP/IP  socket.  
            sender = new Socket(IPAddr_.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // https://docs.microsoft.com/en-us/dotnet/framework/network-programming/synchronous-client-socket-example

            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {

                sender.Connect(remoteEP);

                Thread recvThread = new Thread(new ThreadStart(ThreadReceive));
                Thread sendThread = new Thread(new ThreadStart(ThreadSend));

                recvThread.Start();
                sendThread.Start();

                sendThread.Join();
                recvThread.Join();

                // Release the socket.  
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {
        StartClient();
        return 0;
    }
}