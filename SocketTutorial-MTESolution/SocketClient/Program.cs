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

/* Step 3 */
/* Add "using Eclypses.MTE;" */
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
    //private static MteMkeEnc _encoder = new MteMkeEnc();
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
    private static string _entropy = "";
    private static ulong _decoderNonce = 0;

    // OPTIONAL - adding 1 to decoder nonce so return value changes
    // on client side values will be switched so they match up encoder to decoder and vise versa 
    private static ulong _encoderNonce = 1;
    private static string _identifier = "demo";
    static void Main(string[] args) {
      //
      // This Tutorial uses Sockets for communication.
      // It should be noted that the MTE can be used with any type of communication. (SOCKETS are not required!)
      //

      Console.WriteLine("Starting C# Socket Client");

      try {
        //
        // Step 5
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

        #region Create Instance of Encoder and Decoder (Step 8)

        //
        // Step 8
        // Create Instance of Encoder
        CreateEncoder(baseObj);

        //
        // Step 8
        // Create Instance of Decoder
        CreateDecoder(baseObj);
        #endregion

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
              // Step 9
              // encode text to send and check for successful result
              byte[] encodedBytes = _encoder.Encode(textToSend, out _encoderStatus);
              if (_encoderStatus != MteStatus.mte_status_success) {
                Console.WriteLine($"Error decoding: Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
                //.... 
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

                Console.WriteLine("Socket client is closed due to encoding error, press ENTER to end this...");
                Console.ReadLine();
                return;
              }

              //
              // For demonstration purposes only to show packets
              Console.WriteLine($"Base64 encoded representation of the packet being sent: {Convert.ToBase64String(encodedBytes)}");

              //
              // Get the length of the text we are sending to send length-prefix
              int toSendLen = encodedBytes.Length;

              //
              // This puts the bytes of the send length
              byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);

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
              sender.Send(encodedBytes);

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
              // Step 9
              // Decode incoming message and check for non-error response
              // When checking the status on decode use "StatusIsError"
              // Only checking if status is success can be misleading, there may be a
              // warning returned that the user can ignore 
              // See MTE Documentation for more details
              string returnedText = _decoder.DecodeStr(rcvBytes, out _decoderStatus);
              if (_decoder.StatusIsError(_decoderStatus)) {
                Console.WriteLine($"Error decoding: Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");
                //.... 
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

                Console.WriteLine("Socket client is closed due to decoding error, press ENTER to end this...");
                return;
              }

              //
              // Convert byte array to string to view in console (this step is for display purposes)
              Console.WriteLine($"Base64 encoded representation of the received packet: {Convert.ToBase64String(rcvBytes)}");
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

    #region CreateDecoder (Step 8)
    /// <summary>Creates the MTE Decoder.</summary>
    /// <exception cref="ApplicationException">Failed to initialize the MTE decode decoder. Status: {MteBase.GetStatusName(_decoderStatus)} / {MteBase.GetStatusDescription(_decoderStatus)}</exception>
    public static void CreateDecoder(MteBase baseObj) {
      try {
        // Check how long entropy we need, set default and prompt if we need it
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
          throw new ApplicationException($"Failed to initialize the MTE decode decoder. Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");
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
        // Check how long entropy we need, set default and prompt if we need it
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
          throw new ApplicationException($"Failed to initialize the MTE encoder engine. Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
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
}