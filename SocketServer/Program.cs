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
using Eclypses.EcdhP256;

#if USE_APP_SETTINGS
//using Microsoft.Extensions.ConfigurationBuilder;
using Microsoft.Extensions.Configuration;
#endif

/* Add "using Eclypses.MTE" */
using Eclypses.MTE;

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
    //static MteMkeEnc _encoder = new MteMkeEnc();
    //private static string _mteType = "MKE";
    //---------------------------------------------------
    // Create the MTE Fixed length Encoder, uncomment to use MTE FLEN
    //---------------------------------------------------
    //private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
    //private static string _mteType = "FLEN";

    // The server socket manager.
    private static ServerSocketManager? _socketManager;

    private static MteSetupInfo? _serverEncoderInfo;
    private static MteSetupInfo? _serverDecoderInfo;

    public static void Main(String[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //

      Console.WriteLine("Starting C# Socket Server");

      // Create instance of MteBase object
      MteBase baseObj = new MteBase();

      //
      // Display what version of MTE we are using
      string mteVersion = baseObj.GetVersion();
      Console.WriteLine($"Using MTE Version {mteVersion}-{_mteType}");

      #region Set/Prompt for needed parameters

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

      try {
        _serverEncoderInfo = new MteSetupInfo();
        _serverDecoderInfo = new MteSetupInfo();

        _socketManager = new ServerSocketManager(port);

        // Exchange entropy, nonce, and personalization string between the client and server.
        if (!ExchangeMteInfo()) {
          Console.Error.WriteLine("There was an error attempting to exchange infromation between this and the client.");
          return;
        }

        #region Create instances of MTE

        // Create Instance of the Encoder
        CreateEncoder();

        // Create Instance of the Decoder
        CreateDecoder();

        #endregion

        // Run the diagnostic test.
        if (!RunDiagnosticTest()) {
          Console.Error.WriteLine("There was a problem running the diagnostic test.");
          return;
        }

        while (true) {
          try {

            Console.WriteLine("Listening for messages from client...");

            // Receive and decode the message from the client.
            byte[]? decoded;
            if (ReceiveAndDecodeMessage(out decoded) == false) {
              break;
            }

            // Encode and send the input.
            if (EncodeAndSendMessage(decoded) == false) {
              break;
            }

            // Free the decoded message.
            decoded = null;

          } catch (Exception e) {
            Console.WriteLine(e);
            Environment.ExitCode = INNER_GENERAL_EXCEPTION;
          }
        }


        // Close server socket and prompt to end.
        _socketManager.Shutdown();

        // Uninstantiate Encoder and Decoder.
        _encoder.Uninstantiate();
        _decoder.Uninstantiate();

        Console.WriteLine("Program stopped.");
      } catch (Exception e) {
        Console.WriteLine(e);
        throw;
      }

    }

    #region CreateDecoder

    /// <summary>Creates the MTE Decoder.</summary>
    /// <exception cref="ApplicationException">Failed to initialize the MTE decoder. Status: {MteBase.GetStatusName(_decoderStatus)} / {MteBase.GetStatusDescription(_decoderStatus)}</exception>
    public static bool CreateDecoder() {
      if (_serverDecoderInfo == null || _serverDecoderInfo.Personalization == null ||
          _serverDecoderInfo.Nonce == null || _serverDecoderInfo.PeerKey == null) {
        return false;
      }

      // Display all info related to the client Decoder.
      Console.WriteLine("Server Decoder public key:");
      Console.WriteLine(BitConverter.ToString(_serverDecoderInfo.GetPublicKey()).Replace("-", ""));
      Console.WriteLine("Server Decoder peer's key:");
      Console.WriteLine(BitConverter.ToString(_serverDecoderInfo.PeerKey).Replace("-", ""));
      Console.WriteLine("Server Decoder nonce:");
      Console.WriteLine(BitConverter.ToString(_serverDecoderInfo.Nonce).Replace("-", ""));
      Console.WriteLine("Server Decoder personalization:");
      Console.WriteLine(Encoding.ASCII.GetString(_serverDecoderInfo.Personalization));

      try {
        // Create shared secret.
        byte[] secret = _serverDecoderInfo.GetSharedSecret();

        // Set Decoder entropy using this shared secret.
        _decoder.SetEntropy(secret);

        // Set Decoder nonce.
        _decoder.SetNonce(_serverDecoderInfo.Nonce);

        // Instantiate Decoder.
        MteStatus status = _decoder.Instantiate(_serverDecoderInfo.Personalization);
        if (status != MteStatus.mte_status_success) {
          Console.Error.WriteLine("Decoder instantiate error {0}: {1}", _decoder.GetStatusName(status),
            _decoder.GetStatusDescription(status));
        }

        // Delete server Decoder info.
        _serverDecoderInfo = null;

      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }

      return true;
    }

    #endregion

    #region CreateEncoder

    /// <summary>Creates the MTE Encoder.</summary>
    /// <exception cref="ApplicationException">Failed to initialize the MTE encoder engine. Status: {MteBase.GetStatusName(_encoderStatus)} / {MteBase.GetStatusDescription(_encoderStatus)}</exception>
    public static bool CreateEncoder() {
      if (_serverEncoderInfo == null || _serverEncoderInfo.Personalization == null ||
          _serverEncoderInfo.Nonce == null || _serverEncoderInfo.PeerKey == null) {
        return false;
      }

      // Display all info related to the server Encoder.
      Console.WriteLine("Server Encoder public key:");
      Console.WriteLine(BitConverter.ToString(_serverEncoderInfo.GetPublicKey()).Replace("-", ""));
      Console.WriteLine("Server Encoder peer's key:");
      Console.WriteLine(BitConverter.ToString(_serverEncoderInfo.PeerKey).Replace("-", ""));
      Console.WriteLine("Server Encoder nonce:");
      Console.WriteLine(BitConverter.ToString(_serverEncoderInfo.Nonce).Replace("-", ""));
      Console.WriteLine("Server Encoder personalization:");
      Console.WriteLine(Encoding.ASCII.GetString(_serverEncoderInfo.Personalization));

      try {
        // Create shared secret.
        byte[] secret = _serverEncoderInfo.GetSharedSecret();

        // Set Encoder entropy using this shared secret.
        _encoder.SetEntropy(secret);

        // Set Encoder nonce.
        _encoder.SetNonce(_serverEncoderInfo.Nonce);

        // Instantiate Encoder.
        MteStatus status = _encoder.Instantiate(_serverEncoderInfo.Personalization);
        if (status != MteStatus.mte_status_success) {
          Console.Error.WriteLine("Encoder instantiate error {0}: {1}", _encoder.GetStatusName(status),
            _encoder.GetStatusDescription(status));
        }

        // Delete server Encoder info.
        _serverEncoderInfo = null;
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
    static bool EncodeAndSendMessage(byte[]? message) {

      // Display original message.
      if (message != null) {
        Console.WriteLine("\nMessage to be encoded: {0}", Encoding.ASCII.GetString(message));

        MteStatus status;
        // Encode the message.
        byte[] encoded = _encoder.Encode(message, out status);
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
      }

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
        ServerSocketManager.RecvMsg msgStruct = _socketManager.ReceiveMessage();

        if (msgStruct.message == null || msgStruct.message.Length == 0 || msgStruct.success == false || msgStruct.header != 'm') {
          return false;
        }

        // Display encoded message.
        Console.WriteLine("Encoded message received\n: {0}",
          BitConverter.ToString(msgStruct.message).Replace("-", ""));

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
      if (_serverEncoderInfo == null || _serverDecoderInfo == null) {
        return false;
      }
      // The client Encoder and the server Decoder will be paired.
      // The client Decoder and the server Encoder will be paired.

      // Processing incoming message all 4 will be needed.
      UInt16 recvCount = 0;
      ServerSocketManager.RecvMsg recvData = new ServerSocketManager.RecvMsg();

      // Loop until all 4 data are received from client, can be in any order.
      while (recvCount < 4) {
        // Receive the next message from the client.
        if (_socketManager != null) {
          recvData = _socketManager.ReceiveMessage();

          // Evaluate the header.
          // 1 - server Decoder public key (from client Encoder)
          // 2 - server Decoder personalization string (from client Encoder)
          // 3 - server Encoder public key (from client Decoder)
          // 4 - server Encoder personalization string (from client Decoder)
          switch (recvData.header) {
            case '1':
              if (_serverDecoderInfo.PeerKey?.Length != 0) {
                recvCount++;
              }

              _serverDecoderInfo.PeerKey = recvData.message;
              break;
            case '2':
              if (_serverDecoderInfo.Personalization?.Length != 0) {
                recvCount++;
              }

              _serverDecoderInfo.Personalization = recvData.message;
              break;
            case '3':
              if (_serverEncoderInfo.PeerKey?.Length != 0) {
                recvCount++;
              }

              _serverEncoderInfo.PeerKey = recvData.message;
              break;
            case '4':
              if (_serverEncoderInfo.Personalization?.Length != 0) {
                recvCount++;
              }

              _serverEncoderInfo.Personalization = recvData.message;
              break;
            default:
              // Unknown message, abort here, send an 'E' for error.
              _socketManager.SendMessage('E', Encoding.ASCII.GetBytes("ERR"));
              return false;
          }
        }
      }

      // Now all values from client have been received, send an 'A' for acknowledge to client.
      if (_socketManager != null) {
        _socketManager.SendMessage('A', Encoding.ASCII.GetBytes("ACK"));

        // Prepare to send server information now.

        // Create nonces.
        int minNonceBytes = _encoder.GetDrbgsNonceMinBytes(_encoder.GetDrbg());
        if (minNonceBytes <= 0) {
          minNonceBytes = 1;
        }

        byte[] serverEncoderNonce = new byte[minNonceBytes];
        int res = EcdhP256.GetRandom(serverEncoderNonce);
        if (res < 0) {
          return false;
        }

        _serverEncoderInfo.Nonce = serverEncoderNonce;

        byte[] serverDecoderNonce = new byte[minNonceBytes];
        res = EcdhP256.GetRandom(serverEncoderNonce);
        if (res < 0) {
          return false;
        }

        _serverDecoderInfo.Nonce = serverDecoderNonce;

        // Send out information to the client.
        // 1 - server Encoder public key (to client Decoder)
        // 2 - server Encoder nonce (to client Decoder)
        // 3 - server Decoder public key (to client Encoder)
        // 4 - server Decoder nonce (to client Encoder)
        _socketManager.SendMessage('1', _serverEncoderInfo.GetPublicKey());
        _socketManager.SendMessage('2', _serverEncoderInfo.Nonce);
        _socketManager.SendMessage('3', _serverDecoderInfo.GetPublicKey());
        _socketManager.SendMessage('4', _serverDecoderInfo.Nonce);

        // Wait for ack from client.
        recvData = _socketManager.ReceiveMessage();
      }

      if (recvData.header != 'A') {
        return false;
      }

      recvData.message = null;

      return true;
    }

    #endregion

    #region RunDiagnosticTest
    static bool RunDiagnosticTest() {
      // Receive and decode the message.
      byte[]? decoded;
      if (ReceiveAndDecodeMessage(out decoded) == false) {
        return false;
      }

      // Check that it successfully decoded as "ping".
      if (decoded != null && "ping" == Encoding.ASCII.GetString(decoded)) {
        Console.WriteLine("Server Decoder decoded the message from the client Encoder successfully.");
      } else {
        Console.WriteLine("Server Decoder DID NOT decode the message from the client Encoder successfully.");
        return false;
      }

      // Create "ack" message.
      string message = "ack";

      // Encode and send message.
      if (EncodeAndSendMessage(Encoding.ASCII.GetBytes(message)) == false) {
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

  #region SocketExtensions

  /// <summary>
  /// Class SocketExtensions to check if socket is still connected
  /// </summary>
  static class SocketExtensions {
    public static bool IsConnected(this Socket? socket) {
      try {
        return socket != null && !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
      } catch (SocketException) {
        return false;
      }
    }
  }

  #endregion
}
