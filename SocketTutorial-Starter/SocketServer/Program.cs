/*
THIS SOFTWARE MAY NOT BE USED FOR PRODUCTION. Otherwise,
The MIT License (MIT)

Copyright (c) Eclypses, Inc.

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// Uncomment the following to use the appsettings file.
#define USE_APP_SETTINGS

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

#if USE_APP_SETTINGS
//using Microsoft.Extensions.ConfigurationBuilder;
using Microsoft.Extensions.Configuration;
#endif

namespace SocketsTutorialCSharp {
  class Program {
    // define exit codes
    private const int SOCKET_EXCEPTION = 1;
    private const int ARGUMENT_NULL_EXCEPTION = 2;
    private const int INNER_GENERAL_EXCEPTION = 3;
    private const int GENERAL_EXCEPTION = 4;
    private const int OUTER_GENERAL_EXCEPTION = 5;  
    
    public static void Main(String[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //

      Console.WriteLine("Starting C# Socket Server");

      #region Set/Prompt for needed parameters

      //
      // Set/prompt for all parameters needed
      string ipAddress = "localhost";

      //
      // Set default port - but also prompt for port so user can change it at runtime.
      int port = 27015;

      Console.WriteLine($"Please enter port to use, press Enter to use default: {port}");
      string newPort = Console.ReadLine();
      if (!string.IsNullOrWhiteSpace(newPort)) {
        while (!int.TryParse(newPort, out port)) {
          Console.WriteLine($"{newPort} is not a valid integer, please try again.");
          newPort = Console.ReadLine();
        }
      }

      #endregion         

      Console.WriteLine("Listening for a new Client connection...");

      // Creation TCP/IP Socket using  
      // Socket Class Constructor 
      Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
      try {

        // Using Bind() method we associate a 
        // network address to the Server Socket 
        // All client that will connect to this  
        // Server Socket must know this network 
        // Address 
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

        listener.Bind(localEndPoint);

        // Using Listen() method we create  
        // the Client list that will want 
        // to connect to Server 
        listener.Listen(10);

        // Suspend while waiting for 
        // incoming connection Using  
        // Accept() method the server  
        // will accept connection of client 
        Socket serverSocket = listener.Accept();
        Console.WriteLine($"Socket Server is listening on {ipAddress} : port {port}");

        Console.WriteLine("Connected with Client.");

        while (true) {
          try {
            if (!serverSocket.IsConnected()) {
              break;
            }

            Console.WriteLine("Listening for messages from Client...");

            //
            // Get the length of bytes coming in
            byte[] rcvLenBytes = new byte[4];
            serverSocket.Receive(rcvLenBytes);

            //
            // Check if littleEndian - if so reverse so we always send in BigEndian
            if (BitConverter.IsLittleEndian) {
              Array.Reverse(rcvLenBytes);
            }

            //
            // Get the full message based on length of bytes coming in
            int amtReceived = 0;
            int rcvLen = System.BitConverter.ToInt32(rcvLenBytes, 0);
            byte[] rcvBytes = new byte[rcvLen];
            while (amtReceived < rcvLen) {
              amtReceived += serverSocket.Receive(rcvBytes, amtReceived, rcvLen - amtReceived, SocketFlags.None);
            }

            //
            // If there is a message to receive we want to grab it
            if (rcvBytes.Length > 0) {
              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"The received packet: {Encoding.UTF8.GetString(rcvBytes)}");            

              //
              // This puts the bytes of the send length
              int toSendLen = rcvBytes.Length;
              byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);

              //
              // Check if little Endian and reverse if so - all sent in Big Endian
              if (BitConverter.IsLittleEndian) {
                Array.Reverse(toSendLenBytes);
              }

              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"The packet being sent: {Encoding.UTF8.GetString(rcvBytes)}");

              //
              // Send the length of the message
              serverSocket.Send(toSendLenBytes);

              //
              // Send message encoded message
              serverSocket.Send(rcvBytes);
            }
          } catch (Exception e) {
            Console.WriteLine(e);
            Environment.ExitCode = INNER_GENERAL_EXCEPTION;
          }
        }

        //
        // Close server socket and prompt to end
        serverSocket.Shutdown(SocketShutdown.Both);
        serverSocket.Close();

        Console.WriteLine("Program stopped.");
      } catch (Exception e) {
        Console.WriteLine(e);
        throw;
      }
    }  
  }

  #region SocketExtensions

  /// <summary>
  /// Class SocketExtensions to check if socket is still connected
  /// </summary>
  static class SocketExtensions {
    public static bool IsConnected(this Socket socket) {
      try {
        return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
      } catch (SocketException) {
        return false;
      }
    }
  }

  #endregion
}
