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
    
    static void Main(string[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //

      Console.WriteLine("Starting C# Socket Client");

      try {     
            
        //
        // Set default server ip address - but also prompt for ip so user can change it at runtime.
        string ipAddress = "localhost";
        Console.WriteLine($"Please enter ip address of Server, press Enter to use default: {ipAddress}");
        string newIP = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newIP)) {
          ipAddress = newIP;
        }

        Console.WriteLine($"Server is at {ipAddress}");

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

        //
        // Create a TCP/IP  socket.  
        Socket sender = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try {
          sender.Connect(ipAddress, port);

          Console.WriteLine("Client connected to Server.");

          while (true) {
            //
            // Prompt user for input to send to other side
            Console.WriteLine("Please enter text to send: (To end please type 'quit')");
            string textToSend = Console.ReadLine();

            //
            // Check to see if we are quitting - if so set sendMessages to false so this is the last time it runs
            if (textToSend.Equals("quit", StringComparison.CurrentCultureIgnoreCase)) {
              break;
            }

            try {              

              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"The packet being sent: {textToSend}");

              //
              // Get the length of the text we are sending to send length-prefix
              int toSendLen = textToSend.Length;

              //
              // This puts the bytes of the send length
              byte[] toSendLenBytes = BitConverter.GetBytes(toSendLen);

              //
              // Check if little Endian and reverse if so - all sent in Big Endian
              if (BitConverter.IsLittleEndian) {
                Array.Reverse(toSendLenBytes);
              }

              //
              // Send the length-prefix
              sender.Send(toSendLenBytes);
              //
              // Send the actual message
              sender.Send(Encoding.ASCII.GetBytes(textToSend));

              // Receive the response from the remote device.  
              //
              // First get the length-prefix
              byte[] rcvLenBytes = new byte[4];
              sender.Receive(rcvLenBytes);
              if (BitConverter.IsLittleEndian) {
                Array.Reverse(rcvLenBytes);
              }

              //
              // Get message
              int amtReceived = 0;
              int rcvLen = System.BitConverter.ToInt32(rcvLenBytes, 0);
              byte[] rcvBytes = new byte[rcvLen];
              while (amtReceived < rcvLen) {
                amtReceived += sender.Receive(rcvBytes, amtReceived, rcvLen - amtReceived, SocketFlags.None);
              }              

              //
              // Convert byte array to string to view in console (this step is for display purposes)
              Console.WriteLine($"The received packet: {Encoding.UTF8.GetString(rcvBytes)}");
            } catch (Exception e) {
              Console.WriteLine(e);
              Environment.ExitCode = INNER_GENERAL_EXCEPTION;
            }

          }

          //
          // Close client and prompt to exit
          sender.Shutdown(SocketShutdown.Both);
          sender.Close();

          Console.WriteLine("Program stopped.");

        } catch (ArgumentNullException ane) {
          Console.WriteLine("ArgumentNullException : {0}", ane.ToString(), ane);
          Environment.ExitCode = ARGUMENT_NULL_EXCEPTION;
        } catch (SocketException se) {
          Console.WriteLine("SocketException : {0}", se.ToString(), se);
          Environment.ExitCode = SOCKET_EXCEPTION;
        } catch (Exception e) {
          Console.WriteLine("Unexpected exception : {0}", e.ToString(), e);
          Environment.ExitCode = GENERAL_EXCEPTION;
        }
      } catch (Exception e) {
        Console.WriteLine(e.ToString(), e);
        Environment.ExitCode = OUTER_GENERAL_EXCEPTION;
      }
    }   
  }
}