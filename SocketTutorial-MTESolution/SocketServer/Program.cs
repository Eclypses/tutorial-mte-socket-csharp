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

/* Step 3 */
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

    /* Step 4 */
    //---------------------------------------------------
    // MKE and Fixed length add-ons are NOT in all SDK
    // MTE versions. If the name of the SDK includes
    // "-MKE" then it will contain the MKE add-on. If the
    // name of the SDK includes "-FLEN" then it contains
    // the Fixed length add-on.
    //---------------------------------------------------

    // Create the MTE decoder, uncomment to use MTE core OR FLEN
    // Create the Mte Fixed length decoder (SAME as MTE Core)
    //---------------------------------------------------
    private static MteDec _decoder = new MteDec();
    //---------------------------------------------------
    // Create the Mte MKE decoder, uncomment to use MKE
    //---------------------------------------------------
    //private static MteMkeDec _decoder = new MteMkeDec();

    private static MteStatus _decoderStatus;
        
    // Create the Mte encoder, uncomment to use MTE core
    //---------------------------------------------------
    private static MteEnc _encoder = new MteEnc();
    private static string _mteType = "Core";
    //---------------------------------------------------
    // Create the Mte MKE encoder, uncomment to use MKE
    //---------------------------------------------------
    //static MteMkeEnc _encoder = new MteMkeEnc();
    //private static string _mteType = "MKE";
    //---------------------------------------------------
    // Create the Mte Fixed length encoder, uncomment to use MTE FLEN
    //---------------------------------------------------
    //private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
    //private static string _mteType = "FLEN";

    private static MteStatus _encoderStatus;

    /* Step 6 - Part 1 */
    // Set default entropy, nonce and identifier
    // Providing Entropy in this fashion is insecure. This is for demonstration purposes only and should never be done in practice. 
    // If this is a trial version of the MTE, entropy must be blank
    private static string _entropy = "";
    private static ulong _encoderNonce = 0;

    //
    // OPTIONAL!!! adding 1 to decoder nonce so return value changes -- same nonce can be used for encoder and decoder
    // on client side values will be switched so they match up encoder to decoder and vise versa 
    private static ulong _decoderNonce = 1;
    private static string _identifier = "demo";

    public static void Main(String[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //

      Console.WriteLine("Starting C# Socket Server");

      //
      // Step 5 
      // Create instance of MteBase object
      MteBase baseObj = new MteBase();

      //
      // Display what version of MTE we are using
      string mteVersion = baseObj.GetVersion();
      Console.WriteLine($"Using MTE Version {mteVersion}-{_mteType}");

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
      #region Check MTE License (Step 7)
#if USE_APP_SETTINGS
            //
            // Step 7
            // Check mte license
            // Initialize MTE license. If a license code is not required (e.g., trial mode), this can be skipped. 
            if (!baseObj.InitLicense(appSettings.LicenseCompanyName, appSettings.LicenseKey)) {
                Console.Error.WriteLine("There was an error attempting to initialize the MTE License.");
                return;
            }
#else
      //
      // Step 7
      // Check mte license
      // Initialize MTE license. If a license code is not required (e.g., trial
      // mode), this can be skipped. 
      if (!baseObj.InitLicense("LicenseCompanyName", "LicenseKey")) {
        Console.Error.WriteLine("There was an error attempting to initialize the MTE License.");
        return;
      }
#endif
      #endregion

      #region Create instances of MTE (step 8)

      //
      // Step 8
      // Create Instance of the Encoder
      CreateEncoder(baseObj);

      // 
      // Step 8
      // Create Instance of the Decoder
      CreateDecoder(baseObj);

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
              // convert byte data so we can view it in console (this step is for display purposes)
              string encodedText = Convert.ToBase64String(rcvBytes);

              //
              // Step 9
              //
              // Decode incoming message and check for non-error response
              // When checking the status on decode use "StatusIsError"
              // Only checking if status is success can be misleading, there may be a
              // warning returned that the user can ignore 
              // See MTE Documentation for more details
              string decodedText = _decoder.DecodeStr(rcvBytes, out _decoderStatus);
              if ((_decoder.StatusIsError(_decoderStatus))) {
                Console.WriteLine(
                  $"Error decoding: Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");

                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();

                Console.WriteLine(
                  "Socket server is closed due to decoding error, press ENTER to end this...");
                Console.ReadLine();
                return;
              }

              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"Base64 encoded representation of the received packet: {encodedText}");
              Console.WriteLine($"Decoded data: {decodedText}\n\n");

              //
              // Step 9
              // Encode returning text and ensure successful 
              byte[] encodedReturn = _encoder.Encode(decodedText, out _encoderStatus);
              if (_encoderStatus != MteStatus.mte_status_success) {
                Console.WriteLine(
                  $"Error encoding: Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
                //.... 
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();

                Console.WriteLine(
                  "Socket server is closed due to encoding error, press ENTER to end this...");
                Console.ReadLine();
                return;
              }

              //
              // This puts the bytes of the send length
              int toSendLen = encodedReturn.Length;
              byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);

              //
              // Check if little Endian and reverse if so - all sent in Big Endian
              if (BitConverter.IsLittleEndian) {
                Array.Reverse(toSendLenBytes);
              }

              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"Base64 encoded representation of the packet being sent: {Convert.ToBase64String(encodedReturn)}");

              //
              // Send the length of the message
              serverSocket.Send(toSendLenBytes);

              //
              // Send message encoded message
              serverSocket.Send(encodedReturn);
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

    #region CreateDecoder (Step 8)

    /// <summary>Creates the MTE Decoder.</summary>
    /// <exception cref="ApplicationException">Failed to initialize the MTE decode decoder. Status: {MteBase.GetStatusName(_decoderStatus)} / {MteBase.GetStatusDescription(_decoderStatus)}</exception>
    public static void CreateDecoder(MteBase baseObj) {
      try {
        int entropyMinBytes = baseObj.GetDrbgsEntropyMinBytes(_decoder.GetDrbg());
        _entropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _entropy;

        //
        // Set mte values for the decoder
        _decoder.SetEntropy(Encoding.UTF8.GetBytes(_entropy));
        _decoder.SetNonce(_decoderNonce);

        //
        // Initialize MTE decoder
        _decoderStatus = _decoder.Instantiate(_identifier);
        if (_decoderStatus != MteStatus.mte_status_success) {
          throw new ApplicationException(
            $"Failed to initialize the MTE decode decoder. Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");
        }
      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }
    }

    #endregion

    #region CreateEncoder (Step 8)

    /// <summary>Creates the MTE Encoder.</summary>
    /// <exception cref="ApplicationException">Failed to initialize the MTE encoder engine. Status: {MteBase.GetStatusName(_encoderStatus)} / {MteBase.GetStatusDescription(_encoderStatus)}</exception>
    public static void CreateEncoder(MteBase baseObj) {
      try {
        int entropyMinBytes = baseObj.GetDrbgsEntropyMinBytes(_encoder.GetDrbg());
        _entropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _entropy;

        //
        // Set mte values for the encoder
        _encoder.SetEntropy(Encoding.UTF8.GetBytes(_entropy));
        _encoder.SetNonce(_encoderNonce);

        //
        // Initialize MTE encoder
        _encoderStatus = _encoder.Instantiate(_identifier);
        if (_encoderStatus != MteStatus.mte_status_success) {
          throw new ApplicationException(
            $"Failed to initialize the MTE encoder engine. Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
        }
      } catch (Exception ex) {
        Console.WriteLine(ex);
        throw;
      }
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
