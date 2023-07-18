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

/* Add "using Eclypses.MTE;" */
using Eclypses.MTE;

using Eclypses.EcdhP256;
using System.Reflection;

namespace SocketsTutorialCSharp {
  class Program {
    // define exit codes
    private const int SOCKET_EXCEPTION = 1;
    private const int ARGUMENT_NULL_EXCEPTION = 2;
    private const int INNER_GENERAL_EXCEPTION = 3;
    private const int GENERAL_EXCEPTION = 4;
    private const int OUTER_GENERAL_EXCEPTION = 5;

    //--------------------------------------------
    // The fixed length, only needed for MTE FLEN
    //--------------------------------------------
    private const int _fixedLength = 8;

    //---------------------------------------------------
    // MKE and Fixed length add-ons are NOT in all SDK
    // MTE versions. If the name of the SDK includes
    // "-MKE" then it will contain the MKE add-on. If the
    // name of the SDK includes "-FLEN" then it contains
    // the Fixed length add-on.
    //---------------------------------------------------

    // Create the MTE Decoder, uncomment to use MTE core OR FLEN
    // Create the MTE Fixed length Decoder (SAME as MTE Core)
    //---------------------------------------------------
    private static MteDec _decoder = new MteDec();
    //---------------------------------------------------
    // Create the MTE MKE Decoder, uncomment to use MKE
    //---------------------------------------------------
    //private static MteMkeDec _decoder = new MteMkeDec();

    // Create the MTE Encoder, uncomment to use MTE core
    //---------------------------------------------------
    private static MteEnc _encoder = new MteEnc();
    private static string _mteType = "Core";
    //---------------------------------------------------
    // Create the MTE MKE Encoder, uncomment to use MKE
    //---------------------------------------------------
    //private static MteMkeEnc _encoder = new MteMkeEnc();
    //private static string _mteType = "MKE";
    //---------------------------------------------------
    // Create the MTE Fixed length encoder, uncomment to use MTE FLEN
    //---------------------------------------------------
    //private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
    //private static string _mteType = "FLEN";

    // The client socket manager.
    private static ClientSocketManager? _socketManager;

    private static MteSetupInfo? _clientEncoderInfo;
    private static MteSetupInfo? _clientDecoderInfo;

    static void Main(string[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //


      // todo: Break this out into more classes and functions.
      // todo add program arguments and remove prompts

      Console.WriteLine("Starting C# Socket Client");

      try {
        // Create instance of MteBase object
        MteBase baseObj = new MteBase();
        //
        // Display what version of MTE we are using
        string mteVersion = baseObj.GetVersion();
        Console.WriteLine($"Using MTE Version {mteVersion}-{_mteType}");

        #region Set for MTE settings

#if USE_APP_SETTINGS
        //
        // optional way to pull in settings
        // Above settings can also be pulled in with this if needed.
        AppSettings appSettings = GetAppSettings();
        if (string.IsNullOrWhiteSpace(appSettings.LicenseKey) ||
            string.IsNullOrWhiteSpace(appSettings.LicenseCompanyName)) {
          Console.Error.WriteLine("License Key and License Company name must not be empty, press ENTER to end this...");
          Console.ReadLine();
          return;
        }
#endif

        //
        // Set default server ip address - but also prompt for ip so user can change it at runtime.
        string? ipAddress = "localhost";
        Console.WriteLine($"Please enter ip address of Server, press Enter to use default: {ipAddress}");
        string? newIP = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newIP)) {
          ipAddress = newIP;
        }

        Console.WriteLine($"Server is at {ipAddress}");

        //
        // Set default port - but also prompt for port so user can change it at runtime.
        int port = 27015;

        Console.WriteLine($"Please enter port to use, press Enter to use default: {port}");
        string? newPort = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newPort)) {
          while (!int.TryParse(newPort, out port)) {
            Console.WriteLine($"{newPort} is not a valid integer, please try again.");
            newPort = Console.ReadLine();
          }
        }

        #endregion

        #region Check MTE License

#if USE_APP_SETTINGS
        // Check MTE license
        // Initialize MTE license. If a license code is not required (e.g., trial mode), this can be skipped. 
        if (!baseObj.InitLicense(appSettings.LicenseCompanyName, appSettings.LicenseKey)) {
          Console.Error.WriteLine("There was an error attempting to initialize the MTE License.");
          return;
        }
#else
       
        // Check MTE license
        // Initialize MTE license. If a license code is not required (e.g., trial
        // mode), this can be skipped. 
        if (!baseObj.InitLicense("LicenseCompanyName", "LicenseKey")) {
          Console.Error.WriteLine("There was an error attempting to initialize the MTE License.");
          return;
        }
#endif
        #endregion

        _clientEncoderInfo = new MteSetupInfo();
        _clientDecoderInfo = new MteSetupInfo();

        //
        // Create a TCP/IP  socket.  
        _socketManager = new ClientSocketManager(ipAddress, port);
        try {

          // Exchange entropy, nonce, and personalization string between the client and server.
          if (!ExchangeMteInfo()) {
            Console.Error.WriteLine(
              "There was an error attempting to exchange information between this and the server.");
            return;
          }

          // Create the Encoder.
          if (!CreateEncoder()) {
            Console.Error.WriteLine("There was a problem creating the Encoder.");
            return;
          }

          // Create the Decoder.
          if (!CreateDecoder()) {
            Console.Error.WriteLine("There was a problem creating the Decoder.");
            return;
          }

          // Run the diagnostic test.
          if (!RunDiagnosticTest()) {
            Console.Error.WriteLine("There was a problem running the diagnostic test.");
            return;
          }

          while (true) {
            //
            // Prompt user for input to send to other side
            Console.WriteLine("Please enter text to send: (To end please type 'quit')");
            string? input = Console.ReadLine();

            //
            // Check to see if we are quitting - if so set sendMessages to false so this is the last time it runs
            if (input != null && input.Equals("quit", StringComparison.CurrentCultureIgnoreCase)) {
              break;
            }

            try {
              // Encode and send the input.
              if (input != null && !EncodeAndSendMessage(Encoding.ASCII.GetBytes(input))) {
                break;
              }

              // Receive and decode the returned data.
              if (!ReceiveAndDecodeMessage(out byte[]? decoded)) {
                break;
              }

              // Compare the decoded message to the original.
              if (decoded != null && Encoding.ASCII.GetString(decoded) == input) {
                Console.WriteLine("The original input and decoded return match.");
              } else {
                Console.Error.WriteLine("The original input and decoded return DO NOT match.");
                break;
              }

            } catch (Exception e) {
              Console.WriteLine(e);
              Environment.ExitCode = INNER_GENERAL_EXCEPTION;
            }

          }

          // Close socket.
          _socketManager.Shutdown();

          // Uninstantiate Encoder and Decoder.
          _encoder.Uninstantiate();
          _decoder.Uninstantiate();

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

    #region CreateDecoder
    /// <summary>Creates the MTE Decoder.</summary>
    /// <exception cref="ApplicationException">"Encoder instantiate error {0}: {1}", _encoder.GetStatusName(status),
    /// _encoder.GetStatusDescription(status)}</exception>
    public static bool CreateDecoder() {

      if (_clientDecoderInfo == null || _clientDecoderInfo.Personalization == null ||
          _clientDecoderInfo.Nonce == null || _clientDecoderInfo.PeerKey == null) {
        return false;
      }

      // Display all info related to the client Decoder.
      Console.WriteLine("Client Decoder public key:");
      Console.WriteLine(BitConverter.ToString(_clientDecoderInfo.GetPublicKey()).Replace("-", ""));
      Console.WriteLine("Client Decoder peer's key:");
      Console.WriteLine(BitConverter.ToString(_clientDecoderInfo.PeerKey).Replace("-", ""));
      Console.WriteLine("Client Decoder nonce:");
      Console.WriteLine(BitConverter.ToString(_clientDecoderInfo.Nonce).Replace("-", ""));
      Console.WriteLine("Client Decoder personalization:");
      Console.WriteLine(Encoding.ASCII.GetString(_clientDecoderInfo.Personalization));

      try {
        // Create shared secret.
        byte[] secret = _clientDecoderInfo.GetSharedSecret();

        // Set Decoder entropy using this shared secret.
        _decoder.SetEntropy(secret);

        // Set Decoder nonce.
        _decoder.SetNonce(_clientDecoderInfo.Nonce);

        // Instantiate Decoder.
        MteStatus status = _decoder.Instantiate(_clientDecoderInfo.Personalization);
        if (status != MteStatus.mte_status_success) {
          Console.Error.WriteLine("Decoder instantiate error {0}: {1}", _decoder.GetStatusName(status),
            _decoder.GetStatusDescription(status));
        }

        // Delete client Decoder info.
        _clientDecoderInfo = null;

      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }

      return true;
    }
    #endregion

    #region CreateEncoder
    /// <summary>Creates the MTE Encoder.</summary>
    /// <exception cref="ApplicationException">"Encoder instantiate error {0}: {1}", _encoder.GetStatusName(status),
    /// _encoder.GetStatusDescription(status)}</exception>
    public static bool CreateEncoder() {
      if (_clientEncoderInfo == null || _clientEncoderInfo.Personalization == null ||
          _clientEncoderInfo.Nonce == null || _clientEncoderInfo.PeerKey == null) {
        return false;
      }

      // Display all info related to the client Encoder.
      Console.WriteLine("Client Encoder public key:");
      Console.WriteLine(BitConverter.ToString(_clientEncoderInfo.GetPublicKey()).Replace("-", ""));
      Console.WriteLine("Client Encoder peer's key:");
      Console.WriteLine(BitConverter.ToString(_clientEncoderInfo.PeerKey).Replace("-", ""));
      Console.WriteLine("Client Encoder nonce:");
      Console.WriteLine(BitConverter.ToString(_clientEncoderInfo.Nonce).Replace("-", ""));
      Console.WriteLine("Client Encoder personalization:");
      Console.WriteLine(Encoding.ASCII.GetString(_clientEncoderInfo.Personalization));

      try {
        // Create shared secret.
        byte[] secret = _clientEncoderInfo.GetSharedSecret();

        // Set Encoder entropy using this shared secret.
        _encoder.SetEntropy(secret);

        // Set Encoder nonce.
        _encoder.SetNonce(_clientEncoderInfo.Nonce);

        // Instantiate Encoder.
        MteStatus status = _encoder.Instantiate(_clientEncoderInfo.Personalization);
        if (status != MteStatus.mte_status_success) {
          Console.Error.WriteLine("Encoder instantiate error {0}: {1}", _encoder.GetStatusName(status),
            _encoder.GetStatusDescription(status));
        }

        // Delete client Encoder info.
        _clientEncoderInfo = null;
      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }

      return true;
    }
    #endregion

    #region EncodeAndSendMessage
    /// <summary>
    /// Encodes the message with the MTE and then sends it.
    /// </summary>
    /// <param name="message">The message to be encoded and sent.</param>
    /// <returns>True if encoded and sent successfully, false otherwise.</returns>
    static bool EncodeAndSendMessage(byte[] message) {

      // Display original message.
      Console.WriteLine("\nMessage to be encoded: {0}", Encoding.ASCII.GetString(message));

      MteStatus status;
      // Encode the message.
      byte[]? encoded = _encoder.Encode(message, out status);
      if (status != MteStatus.mte_status_success) {
        Console.Error.WriteLine("Error encoding (" + _encoder.GetStatusName(status) + "): " +
                                _encoder.GetStatusDescription(status));
        return false;
      }

      // Send the encoded message.
      if (_socketManager != null) {
        long res = _socketManager.SendMessage('m', encoded);
        if (res <= 0) {
          return false;
        }
      }

      // Display encoded message.
      Console.WriteLine("Encoded message being sent\n: {0}", BitConverter.ToString(encoded).Replace("-", ""));
      return true;
    }
    #endregion

    #region ReceiveAndDecodeMessage
    /// <summary>
    /// Receives the incoming message and then decodes it with the MTE.
    /// </summary>
    /// <param name="message">The decoded message.</param>
    /// <returns>True if received and decoded successfully, false otherwise.</returns>
    static bool ReceiveAndDecodeMessage(out byte[]? message) {
      message = null;

      // Wait for return message.
      if (_socketManager != null) {
        ClientSocketManager.RecvMsg msgStruct = _socketManager.ReceiveMessage();

        if (msgStruct.message == null || msgStruct.message.Length == 0 || msgStruct.success == false || msgStruct.header != 'm') {
          return false;
        }

        // Display encoded message.
        Console.WriteLine("Encoded message received\n: {0}", BitConverter.ToString(msgStruct.message).Replace("-", ""));

        // Decode the message.
        MteStatus status;
        message = _decoder.Decode(msgStruct.message, out status);
        if (_decoder.StatusIsError(status)) {
          Console.Error.WriteLine("Error decoding (" + _decoder.GetStatusName(status) + "): " +
                                  _decoder.GetStatusDescription(status));
          return false;
        }
      }

      // Display decoded message.
      if (message != null) {
        Console.WriteLine("Decoded message: {0}\n", Encoding.ASCII.GetString(message));
      }

      return true;
    }
    #endregion 

    #region ExchangeMteInfo

    static bool ExchangeMteInfo() {
      if (_clientEncoderInfo == null || _clientDecoderInfo == null) {
        return false;
      }

      // The client Encoder and the server Decoder will be paired.
      // The client Decoder and the server Encoder will be paired.

      // Prepare to send client information.

      // Create personalization strings.
      _clientEncoderInfo.Personalization = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
      _clientDecoderInfo.Personalization = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());

      // Send out information to the server.
      // 1 - client Encoder public key (to server Decoder)
      // 2 - client Encoder personalization string (to server Decoder)
      // 3 - client Decoder public key (to server Encoder)
      // 4 - client Decoder personalization string (to server Encoder)
      if (_socketManager != null) {
        _socketManager.SendMessage('1', _clientEncoderInfo.GetPublicKey());
        _socketManager.SendMessage('2', _clientEncoderInfo.Personalization);
        _socketManager.SendMessage('3', _clientDecoderInfo.GetPublicKey());
        _socketManager.SendMessage('4', _clientDecoderInfo.Personalization);

        // Wait for ack from server.
        ClientSocketManager.RecvMsg recvData = _socketManager.ReceiveMessage();
        if (recvData.header != 'A') {
          return false;
        }

        recvData.message = null;

        // Processing incoming message all 4 will be needed.
        UInt16 recvCount = 0;

        while (recvCount < 4) {
          // Receive the next message from the server.
          recvData = _socketManager.ReceiveMessage();

          // Evaluate the header.
          // 1 - client Decoder public key (from server Encoder)
          // 2 - client Decoder nonce (from server Encoder)
          // 3 - client Encoder public key (from server Decoder)
          // 4 - client Encoder nonce (from server Decoder)
          switch (recvData.header) {
            case '1':
              if (_clientDecoderInfo.PeerKey?.Length != 0) {
                recvCount++;
              }

              _clientDecoderInfo.PeerKey = recvData.message;
              break;
            case '2':
              if (_clientDecoderInfo.Nonce?.Length != 0) {
                recvCount++;
              }

              _clientDecoderInfo.Nonce = recvData.message;
              break;
            case '3':
              if (_clientEncoderInfo.PeerKey?.Length != 0) {
                recvCount++;
              }

              _clientEncoderInfo.PeerKey = recvData.message;
              break;
            case '4':
              if (_clientEncoderInfo.Nonce?.Length != 0) {
                recvCount++;
              }

              _clientEncoderInfo.Nonce = recvData.message;
              break;
            default:
              // Unknown message, abort here, send an 'E' for error.
              _socketManager.SendMessage('E', Encoding.ASCII.GetBytes("ERR"));
              return false;
          }
        }

        // Now all values from server have been received, send an 'A' for acknowledge to server.
        _socketManager.SendMessage('A', Encoding.ASCII.GetBytes("ACK"));
      }

      return true;
    }

    #endregion

    #region RunDiagnosticTest
    static bool RunDiagnosticTest() {
      // Create ping message.
      string message = "ping";

      // Encode and send message.
      if (EncodeAndSendMessage(Encoding.ASCII.GetBytes(message)) == false) {
        return false;
      }

      // Receive and decode the message.
      byte[]? decoded;
      if (ReceiveAndDecodeMessage(out decoded) == false) {
        return false;
      }

      // Check that it successfully decoded as "ack".
      if (decoded != null && "ack" == Encoding.ASCII.GetString(decoded)) {
        Console.WriteLine("Client Decoder decoded the message from the server Encoder successfully.");
      } else {
        Console.Error.WriteLine("Client Decoder DID NOT decode the message from the server Encoder successfully.");
        return false;
      }

      return true;
    }
    #endregion

#if USE_APP_SETTINGS
    #region GetAppSettings
    /// <summary>Gets the application settings.</summary>
    /// <returns>AppSettings.</returns>
    private static AppSettings GetAppSettings() {
      var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

      AppSettings returnSettings = new AppSettings {
        LicenseCompanyName = builder.Build().GetSection("AppSettings").GetSection("LicenseCompanyName").Value,
        LicenseKey = builder.Build().GetSection("AppSettings").GetSection("LicenseKey").Value
      };

      return returnSettings;
    }
    #endregion
#endif
  }
}