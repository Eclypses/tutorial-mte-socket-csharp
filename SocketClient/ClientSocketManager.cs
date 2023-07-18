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
using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SocketsTutorialCSharp {

  internal class ClientSocketManager {
    public struct RecvMsg {
      public bool success;
      public char header;
      public byte[]? message;
    }

    private static Socket? _sender;

    /// <summary>
    /// ClientSocketManager constructor.
    /// </summary>
    /// <param name="ipAddress">The IP address of the server.</param>
    /// <param name="port">The port of the socket connection.</param>
    public ClientSocketManager(string? ipAddress, int port) {
      _sender = new Socket(SocketType.Stream, ProtocolType.Tcp);

      if (ipAddress != null) {
        _sender.Connect(ipAddress, port);
      }

      Console.WriteLine("Client connected to Server.");
    }

    /// <summary>
    /// Sends the message through the socket.
    /// </summary>
    /// <param name="header">The header to go with the message.</param>
    /// <param name="message">The message.</param>
    /// <returns>The number of bytes sent.</returns>
    public long SendMessage(char header, byte[] message) {

      // Get the length of the message.
      int toSendLen = message.Length;

      if (toSendLen == 0 || header == '\0') {
        if (_sender != null) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();
        }

        Console.WriteLine("Socket client is closed due to message length error, press ENTER to end this...");
        return 0;
      }

      // Put the length into byte array.
      byte[] toSendLenBytes = BitConverter.GetBytes(toSendLen);

      // Check if little Endian and reverse if no - all sent in Big Endian.
      if (BitConverter.IsLittleEndian) {
        Array.Reverse(toSendLenBytes);
      }

      // Send the message size as big-endian.
      if (_sender != null) {
        int res = _sender.Send(toSendLenBytes);
        if (res <= 0) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return 0;
        }

        res = _sender.Send(Encoding.ASCII.GetBytes(header.ToString()));
        if (res <= 0) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return 0;
        }

        // Send the actual message.
        res = _sender.Send(message);
        if (res <= 0) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return 0;
        }

        return res;
      } else {
        return 0;
      }
    }

    /// <summary>
    /// Receives the message through the socket.
    /// </summary>
    /// <returns>Struct that contains the message, header, and success result.</returns>
    public RecvMsg ReceiveMessage() {

      // Create RcvMsg struct.
      RecvMsg msgStruct = new RecvMsg() {
        success = false,
        header = '\0',
        message = null
      };

      // Create array to hold the message size coming in.
      byte[] rcvLenBytes = new byte[4];
      if (_sender != null) {
        int res = _sender.Receive(rcvLenBytes);
        if (res <= 0) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return msgStruct;
        }

        // Check if little Endian and reverse if no - all received in Big Endian.
        if (BitConverter.IsLittleEndian) {
          Array.Reverse(rcvLenBytes);
        }

        // Get the header.
        byte[] headerByte = new byte[1];
        res = _sender.Receive(headerByte);
        if (res <= 0) {
          _sender.Shutdown(SocketShutdown.Both);
          _sender.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return msgStruct;
        }
        msgStruct.header = Convert.ToChar(headerByte[0]); 

        // Receive the message from the server.
        int rcvLen = BitConverter.ToInt32(rcvLenBytes);
        msgStruct.message = new byte[rcvLen];
        res = 0;
        while (res < rcvLen) {
          res += _sender.Receive(msgStruct.message, res, rcvLen - res, SocketFlags.None);
        }
      }

      // Set status to true.
      msgStruct.success = true;

      return msgStruct;
    }

    public void Shutdown() {
      if (_sender != null) {
        _sender.Shutdown(SocketShutdown.Both);
        _sender.Close();
      }
    }
  }
}
