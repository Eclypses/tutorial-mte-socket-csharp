

<img src="Eclypses.png" style="width:50%;margin-right:0;"/>

<div align="center" style="font-size:40pt; font-weight:900; font-family:arial; margin-top:300px; " >
C# Socket Tutorial</div>

<div align="center" style="font-size:28pt; font-family:arial; " >
MTE Implementation Tutorial </div>
<div align="center" style="font-size:15pt; font-family:arial; " >
Using MTE version 3.0.x</div>





[Introduction](#introduction)

[Socket Tutorial Server and Client](#socket-tutorial-server-and-client)


<div style="page-break-after: always; break-after: page;"></div>

# Introduction

This tutorial is sending messages via a socket connection. This is only a sample, the MTE does NOT require the usage of sockets, you can use whatever communication protocol that is needed.

This tutorial demonstrates how to use Mte Core, Mte MKE and Mte Fixed Length. Depending on what your needs are, these three different implementations can be used in the same application OR you can use any one of them. They are not dependent on each other and can run simultaneously in the same application if needed. 

The SDK that you received from Eclypses may not include the MKE or MTE FLEN add-ons. If your SDK contains either the MKE or the Fixed Length add-ons, the name of the SDK will contain "-MKE" or "-FLEN". If these add-ons are not there and you need them please work with your sales associate. If there is no need, please just ignore the MKE and FLEN options.

Here is a short explanation of when to use each, but it is encouraged to either speak to a sales associate or read the dev guide if you have additional concerns or questions.

***MTE Core:*** This is the recommended version of the MTE to use. Unless payloads are large or sequencing is needed this is the recommended version of the MTE and the most secure.

***MTE MKE:*** This version of the MTE is recommended when payloads are very large, the MTE Core would, depending on the token byte size, be multiple times larger than the original payload. Because this uses the MTE technology on encryption keys and encrypts the payload, the payload is only enlarged minimally.

***MTE Fixed Length:*** This version of the MTE is very secure and is used when the resulting payload is desired to be the same size for every transmission. The Fixed Length add-on is mainly used when using the sequencing verifier with MTE. In order to skip dropped packets or handle asynchronous packets the sequencing verifier requires that all packets be a predictable size. If you do not wish to handle this with your application then the Fixed Length add-on is a great choice. This is ONLY an encoder change - the decoder that is used is the MTE Core decoder.

In this tutorial we are creating an MTE Encoder and an MTE Decoder in the server as well as the client because we are sending secured messages in both directions. This is only needed when there are secured messages being sent from both sides, the server as well as the client. If only one side of your application is sending secured messages, then the side that sends the secured messages should have an Encoder and the side receiving the messages needs only a Decoder.

These steps should be followed on the server side as well as on the client side of the program.

**IMPORTANT**
>Please note the solution provided in this tutorial does NOT include the MTE library or supporting MTE library files. If you have NOT been provided an MTE library and supporting files, please contact Eclypses Inc. The solution will only work AFTER the MTE library and MTE library files have been incorporated.
  

# Socket Tutorial Server and Client

<ol>
<li>Create a directory named "include" in both the SocketClient and SocketServer projects. Add the contents of the  “src/cs” directory from the mte-Windows package or mte-Linux package to both projects into the newly created include directory. (SocketClient AND SocketServer projects)</li>
<br>
<li>Add the mte.dll (if using windows from the mte-Windows package) and/or libmte.so (if using Linux from the mte-Linux package) to the SocketClient AND SocketServer directory in the solution. </li>

**Make sure the library file will be copied to the output directory when the project is built.**

<br>
<li>Add a “using Eclypses.MTE;” to the top of the Program.cs file</li>
<br>
<li>Create the MTE Decoder and MTE Encoder as well as the accompanying MTE<sup>TM</sup> status for each as global variables. Also include fixed length parameter if using FLEN.</li>

***IMPORTANT NOTE***
> If using the fixed length MTE (FLEN), all messages that are sent that are longer than the set fixed length will be trimmed by the MTE. The other side of the MTE will NOT contain the trimmed portion. Also messages that are shorter than the fixed length will be padded by the MTE so each message that is sent will ALWAYS be the same length. When shorter message are "decoded" on the other side the MTE takes off the extra padding when using strings and hands back the original shorter message, BUT if you use the raw interface the padding will be present as all zeros. Please see official MTE Documentation for more information.

```csharp
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

```

<li>Create the instance of the MteBase class. This allows us to call on many functions within the MTE that we will need in order to run the library.</li>

```csharp
MteBase baseObj = new MteBase();
```
<br>

<li>We need to be able to set the entropy, nonce, and personalization/identification values.</li>
These values should be treated like encryption keys and never exposed. For demonstration purposes in the tutorial we are setting these values in the code. In a production environment these values should be protected and not available to outside sources. For the entropy, we have to determine the size of the allowed entropy value based on the drbg we have selected. A code sample below is included to demonstrate how to get these values.

To set the entropy in the tutorial we are simply getting the minimum bytes required and creating a byte array of that length that contains all zeros. We want to set the default first to be blank. 

```csharp
// If this is a trial version of the MTE, entropy must be blank - set this is global variables
private static string _entropy = "";

```

To set the nonce and the personalization/identifier string we are simply adding our default values as global variables to the top of the class.

```csharp
private static ulong _encoderNonce = 0;

//
// OPTIONAL!!! adding 1 to decoder nonce so return value changes (the nonce can be used for encoder and decoder)
// on client side values will be switched so they match up encoder to decoder and vise versa 
private static ulong _decoderNonce = 1;
private static string _identifier = "demo";
```

<li>To ensure the MTE library is licensed correctly, run the license check. The LicenseCompanyName, and LicenseKey below should be replaced with your company’s MTE license information provided by Eclypses. If a trial version of the MTE is being used any value can be passed into those fields and it will work.</li>

```csharp
//
// Check mte license
// Initialize MTE license
if (!baseObj.InitLicense(“LicenseCompanyName”, “LicenseKey”))
{
     _encoderStatus = MteStatus.mte_status_license_error;
     Console.Error.WriteLine("Instantiate error ({0}): {1}. Press ENTER to end.",
                    baseObj.GetStatusName(_encoderStatus),
                    baseObj.GetStatusDescription(_encoderStatus));
    Console.ReadLine();
    return;
 }

```

<li>Create MTE Decoder Instances and MTE Encoder Instances in a small number of functions.</li>

Here is a sample function that creates the MTE Decoder.

```csharp
public static void CreateDecoder(MteBase baseObj)
{
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
  }
  catch (Exception ex) {
    Console.WriteLine(ex);
    throw;
  }
}

```
*(For further information on Decoder constructor review the DevelopersGuide)*

Here is a sample function that creates the MTE Encoder.

```csharp
public static void CreateEncoder(MteBase baseObj)
{
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
  }
  catch (Exception ex) {
      Console.WriteLine(ex);
      throw;
  }
}

```
*(For further information on Encode constructor review the DevelopersGuide)*

Instantiate the MTE Decoder and MTE Encoder by calling that function at the start of your main function:

```csharp
CreateEncoder(baseObj);
CreateDecoder(baseObj);
```

<li>Finally, we need to add the MTE calls to encode and decode the messages that we are sending and receiving from the other side. (Ensure on the server side the Encoder is used to encode the outgoing text, then the Decoder is used to decode the incoming response.)</li>

<br>
Here is a sample of how to do this on the Server Side.

```csharp
// 
// encode text to send and send
byte[] encodedBytes = _encoder.Encode(textToSend, out _encoderStatus);
if (_encoderStatus != MteStatus.mte_status_success) {
    // Throw an error here
    Console.WriteLine($"Error decoding: Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
}
//
// Decode incoming message and check for non-error response
// When checking the status on decode use "StatusIsError"
// Only checking if status is success can be misleading, there may be a
// warning returned that the user can ignore 
// See MTE Documentation for more details
string returnedText = _decoder.DecodeStr(recData, out _decoderStatus);
if (_decoder.StatusIsError(_decoderStatus)) {
    // Throw an error here
    Console.WriteLine($"Error decoding: Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");
}

```
<br>
Here is a sample of how to do this on the Client Side.

```csharp
//
// Decode received bytes and check for non-error response
// When checking the status on decode use "StatusIsError"
// Only checking if status is success can be misleading, there may be a
// warning returned that the user can ignore 
// See MTE Documentation for more details
string decodedString = _decoder.DecodeStr(data.Value, out _decoderStatus);
if (_decoder.StatusIsError(_decoderStatus)) {
    // Throw an error here
    Console.WriteLine($"Error decoding: Status: {baseObj.GetStatusName(_decoderStatus)} / {baseObj.GetStatusDescription(_decoderStatus)}");
}
//
// Encode outgoing message
byte[] encodedReturn = _encoder.Encode(returnText, out _encoderStatus);
if (_encoderStatus != MteStatus.mte_status_success) {
    // Throw an error here
    Console.WriteLine($"Error encoding: Status: {baseObj.GetStatusName(_encoderStatus)} / {baseObj.GetStatusDescription(_encoderStatus)}");
}

```
</ol>

***The Server side and the Client side of the MTE Sockets Tutorial should now be ready for use on your device.***


<div style="page-break-after: always; break-after: page;"></div>

# Contact Eclypses

<img src="Eclypses.png" style="width:8in;"/>

<p align="center" style="font-weight: bold; font-size: 22pt;">For more information, please contact:</p>
<p align="center" style="font-weight: bold; font-size: 22pt;"><a href="mailto:info@eclypses.com">info@eclypses.com</a></p>
<p align="center" style="font-weight: bold; font-size: 22pt;"><a href="https://www.eclypses.com">www.eclypses.com</a></p>
<p align="center" style="font-weight: bold; font-size: 22pt;">+1.719.323.6680</p>

<p style="font-size: 8pt; margin-bottom: 0; margin: 300px 24px 30px 24px; " >
<b>All trademarks of Eclypses Inc.</b> may not be used without Eclypses Inc.'s prior written consent. No license for any use thereof has been granted without express written consent. Any unauthorized use thereof may violate copyright laws, trademark laws, privacy and publicity laws and communications regulations and statutes. The names, images and likeness of the Eclypses logo, along with all representations thereof, are valuable intellectual property assets of Eclypses, Inc. Accordingly, no party or parties, without the prior written consent of Eclypses, Inc., (which may be withheld in Eclypses' sole discretion), use or permit the use of any of the Eclypses trademarked names or logos of Eclypses, Inc. for any purpose other than as part of the address for the Premises, or use or permit the use of, for any purpose whatsoever, any image or rendering of, or any design based on, the exterior appearance or profile of the Eclypses trademarks and or logo(s).
</p>