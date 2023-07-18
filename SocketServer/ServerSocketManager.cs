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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SocketsTutorialCSharp {

  internal class ServerSocketManager {
    public struct RecvMsg {
      public bool success;
      public char header;
      public byte[]? message;
    }

    private static Socket? _listener;
    private static Socket? _serverSocket;

    /// <summary>
    /// ServerSocketManager constructor.
    /// </summary>
    /// <param name="port">The port of the socket connection.</param>
    public ServerSocketManager(int port) {

      Console.WriteLine("Listening for a new Client connection...");

      // Creation TCP/IP Socket using  
      // Socket Class Constructor 
      _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);

      // Using Bind() method we associate a 
      // network address to the Server Socket 
      // All client that will connect to this  
      // Server Socket must know this network 
      // Address 
      IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

      _listener.Bind(localEndPoint);

      // Using Listen() method we create  
      // the Client list that will want 
      // to connect to Server 
      _listener.Listen(10);

      // Suspend while waiting for 
      // incoming connection Using  
      // Accept() method the server  
      // will accept connection of client 
      _serverSocket = _listener.Accept();

      Console.WriteLine($"Socket Server is listening on localhost : port {port}");


      Console.WriteLine("Connected with Client.");

      if (!_serverSocket.IsConnected()) {
        throw new Exception("Could not connect to socket.");
      }

      Console.WriteLine("Listening for messages from Client...");
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
        if (_serverSocket != null) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();
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
      if (_serverSocket != null) {
        int res = _serverSocket.Send(toSendLenBytes);
        if (res <= 0) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return 0;
        }

        res = _serverSocket.Send(Encoding.ASCII.GetBytes(header.ToString()));
        if (res <= 0) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return 0;
        }

        // Send the actual message.
        res = _serverSocket.Send(message);
        if (res <= 0) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();

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
      if (_serverSocket != null) {
        int res = _serverSocket.Receive(rcvLenBytes);
        if (res <= 0) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return msgStruct;
        }

        // Check if little Endian and reverse if no - all received in Big Endian.
        if (BitConverter.IsLittleEndian) {
          Array.Reverse(rcvLenBytes);
        }

        // Get the header.
        byte[] headerByte = new byte[1];
        res = _serverSocket.Receive(headerByte);
        if (res <= 0) {
          _serverSocket.Shutdown(SocketShutdown.Both);
          _serverSocket.Close();

          Console.WriteLine("Socket client is closed due to socket error, press ENTER to end this...");
          return msgStruct;
        }
        msgStruct.header = Convert.ToChar(headerByte[0]);

        // Receive the message from the server.
        int rcvLen = BitConverter.ToInt32(rcvLenBytes);
        msgStruct.message = new byte[rcvLen];
        res = 0;
        while (res < rcvLen) {
          res += _serverSocket.Receive(msgStruct.message, res, rcvLen - res, SocketFlags.None);
        }
      }

      // Set status to true.
      msgStruct.success = true;

      return msgStruct;
    }

    public void Shutdown() {
      if (_listener is { Connected: true }) {
        _listener.Shutdown(SocketShutdown.Both);
        _listener.Close();
      }

    }
  }
}
