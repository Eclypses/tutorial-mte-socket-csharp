

<img src="Eclypses.png" style="width:50%;margin-right:0;"/>

<div align="center" style="font-size:40pt; font-weight:900; font-family:arial; margin-top:300px; " >
C# Socket Tutorial</div>
<br>
<div align="center" style="font-size:28pt; font-family:arial; " >
MTE Implementation Tutorial (MTE Core, MKE, MTE Fixed Length)</div>
<br>
<div align="center" style="font-size:15pt; font-family:arial; " >
Using MTE version 3.1.x</div>





[Introduction](#introduction)

[Socket Tutorial Server and Client](#socket-tutorial-server-and-client)


<div style="page-break-after: always; break-after: page;"></div>

# Introduction

This tutorial is sending messages via a socket connection. This is only a sample, the MTE does NOT require the usage of sockets, you can use whatever communication protocol that is needed.

This tutorial demonstrates how to use Mte Core, Mte MKE and Mte Fixed Length. For this application, only one type can be used at a time; however, it is possible to implement any and all at the same time depending on needs.

This tutorial contains two main programs, a client and a server, and also for Windows and Linux. Note that any of the available languages can be used for any available platform as long as communication is possible. It is just recommended that a server program is started first and then a client program can be started.

The MTE Encoder and Decoder need several pieces of information to be the same in order to function properly. This includes entropy, nonce, and personalization. If this information must be shared, the entropy MUST be passed securely. One way to do this is with a Diffie-Hellman approach. Each side will then be able to create two shared secrets to use as entropy for each pair of Encoder/Decoder. The two personalization values will be created by the client and shared to the other side. The two nonce values will be created by the server and shared.

The SDK that you received from Eclypses may not include the MKE or MTE FLEN add-ons. If your SDK contains either the MKE or the Fixed Length add-ons, the name of the SDK will contain "-MKE" or "-FLEN". If these add-ons are not there and you need them please work with your sales associate. If there is no need, please just ignore the MKE and FLEN options.

Here is a short explanation of when to use each, but it is encouraged to either speak to a sales associate or read the dev guide if you have additional concerns or questions.

***MTE Core:*** This is the recommended version of the MTE to use. Unless payloads are large or sequencing is needed this is the recommended version of the MTE and the most secure.

***MTE MKE:*** This version of the MTE is recommended when payloads are very large, the MTE Core would, depending on the token byte size, be multiple times larger than the original payload. Because this uses the MTE technology on encryption keys and encrypts the payload, the payload is only enlarged minimally.

***MTE Fixed Length:*** This version of the MTE is very secure and is used when the resulting payload is desired to be the same size for every transmission. The Fixed Length add-on is mainly used when using the sequencing verifier with MTE. In order to skip dropped packets or handle asynchronous packets the sequencing verifier requires that all packets be a predictable size. If you do not wish to handle this with your application then the Fixed Length add-on is a great choice. This is ONLY an encoder change - the decoder that is used is the MTE Core decoder.

***IMPORTANT NOTE***
>If using the fixed length MTE (FLEN), all messages that are sent that are longer than the set fixed length will be trimmed by the MTE. The other side of the MTE will NOT contain the trimmed portion. Also messages that are shorter than the fixed length will be padded by the MTE so each message that is sent will ALWAYS be the same length. When shorter message are "decoded" on the other side the MTE takes off the extra padding when using strings and hands back the original shorter message, BUT if you use the raw interface the padding will be present as all zeros. Please see official MTE Documentation for more information.

In this tutorial, there is an MTE Encoder on the client that is paired with an MTE Decoder on the server. Likewise, there is an MTE Encoder on the server that is paired with an MTE Decoder on the client. Secured messages wil be sent to and from both sides. If a system only needs to secure messages one way, only one pair could be used.

**IMPORTANT**
>Please note the solution provided in this tutorial does NOT include the MTE library or supporting MTE library files. If you have NOT been provided an MTE library and supporting files, please contact Eclypses Inc. The solution will only work AFTER the MTE library and MTE library files have been incorporated.
  

# Socket Tutorial Server and Client

## MTE Directory and File Setup
<ol>
<li>
Navigate to the "tutorial-mte-socket-csharp" directory.
</li>
<li>
Create a directory named "MTE". This will contain all needed MTE files.
</li>
<li>
Copy the "lib" directory and contents from the MTE SDK into the "MTE" directory.
</li>
<li>
Copy the "src/cs" directory and contents from the MTE SDK into the "MTE" directory.
</li>
</ol>


The common source code between the client and server will be found in the "common" directory. The client and server specific source code will be found in their respective directories.

## Project Settings
<ol>
<li>
Ensure that the library directory path contains the path to the "MTE/lib" and "ecdh/lib" directories.
</li>
<li>
The project will require either the dynamic MTE library or the static libraries depending on add-ons; for MTE Core: mte_mtee, mte_mted, and mte_mteb in that order; for MKE add-on: mte_mkee, mte_mked, mte_mtee, mte_mted, and mte_mteb in that order; or for Fixed length add-on: mte_flen, mte_mtee, mte_mted, and mte_mteb in that order.
</li>
</ol>

## Source Code Key Points

### MTE Setup

<ol>
<li>
Utilize preprocessor directives to more easily handle the function calls for the MTE Core or the add-on configurations. In the file "globals.h", uncomment 'USE_MTE_CORE' to utilize the main MTE Core functions; uncomment 'USE_MKE_ADDON' to use the MTE MKE add-on functions; or uncomment 'USE_FLEN_ADDON' to use the Fixed length add-on functions. In this application, only one can be used at a time. This file is shared between the two projects, so both projects will have the changes reflected accordingly.

```csharp
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
```

</li>

<li>
In this application, the Eclypses Elliptic Curve Diffie-Hellman (ECDH) support package is used to create entropy public and private keys. The public keys are then shared between the client and server, and then shared secrets are created to use as matching entropy for the creation of the Encoders and Decoders. The personalization strings and nonces are also created using the randomization feature of the support package.

```csharp
// Create the private and public keys.
const int res = ecdhManager.createKeyPair(publicKey);
if (res < 0)
{
  throw res;
}
```
The c++ ECDHP256 class will keep the private key to itself and not provide access to the calling application.
</li>
<li>
The public keys created by the client will be sent to the server, and vice versa, and will be received as <i>peer public keys</i>. Then the shared secret can be created on each side. These should match as long as the information has been created and shared correctly.

```csharp
// Create shared secret.
byte[] secret = new byte[EcdhP256.SzSecretData];
int res = myEcdhManager.GetSharedSecret(PeerKey, secret);
if (res < 0) {
  throw new Exception("Unable to create shared secret: " + EcdhP256.MemoryFail);
}
```
These secrets will then be used to fufill the entropy needed for the Encoders and Decoders.
</li>
<li>
The client will create the personalization strings, in this case using the built in guid creation system.

```csharp
// Create personalization strings.
_clientEncoderInfo.Personalization = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
_clientDecoderInfo.Personalization = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());
```
</li>
<li>
The two public keys and the two personalization strings will then be sent to the server. The client will wait for an acknowledgement.

```csharp
// Send out information to the server.
// 1 - client Encoder public key (to server Decoder)
// 2 - client Encoder personalization string (to server Decoder)
// 3 - client Decoder public key (to server Encoder)
// 4 - client Decoder personalization string (to server Encoder)
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
```
</li>
<li>
The server will wait for the two public keys and the two personalization strings from the client. Once all four pieces of information have been received, it will send an acknowledgement.

```csharp
// Processing incoming message all 4 will be needed.
UInt16 recvCount = 0;
ServerSocketManager.RecvMsg recvData = new ServerSocketManager.RecvMsg();

// Loop until all 4 data are received from client, can be in any order.
while (recvCount < 4) {
  // Receive the next message from the server.
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

// Now all values from client have been received, send an 'A' for acknowledge to client.
_socketManager.SendMessage('A', Encoding.ASCII.GetBytes("ACK"));
```
</li>
<li>
The server will create the private and public keypairs, one for the server Encoder and client Decoder, and one for the server Decoder and client Encoder. 

```csharp
// Create the private and public keys.
const int res = ecdhManager.createKeyPair(publicKey);
if (res < 0)
{
  throw res;
}
```

</li>
<li>
The server will create the nonces, using the platform supplied secure RNG via the ECDH support library.

```csharp
// Create nonces.
int minNonceBytes = _encoder.GetDrbgsNonceMinBytes(_encoder.GetDrbg());
if (minNonceBytes <= 0) {
  minNonceBytes = 1;
}

EcdhP256 myEcdhManager = new EcdhP256();
byte[] serverEncoderNonce = new byte[minNonceBytes];
int res = myEcdhManager.GetRandom(serverEncoderNonce);
if (res < 0) {
  return false;
}
_serverEncoderInfo.Nonce = serverEncoderNonce;

byte[] serverDecoderNonce = new byte[minNonceBytes];
res = myEcdhManager.GetRandom(serverDecoderNonce);
if (res < 0) {
  return false;
}
_serverDecoderInfo.Nonce = serverDecoderNonce;
```
</li>
<li>
The two public keys and the two nonces will then be sent to the client. The server will wait for an acknowledgement. 
```csharp
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
if (recvData.header != 'A') {
  return false;
}
```
</li>

<li>
The client will now wait for information from the server. This includes the two server public keys, and the two nonces. Once all pieces of information have been obtained, the client will send an acknowledgement back to the server.

```csharp
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
```

</li>
<li>
After the client and server have exchanged their information, the client and server can each create their respective Encoder and Decoder. This is where the personalization string and nonce will be added. Additionally, the entropy will be set by getting the shared secret from ECDH. This sample code showcases the client Encoder. There will be four of each of these that will be very similar. Ensure carefully that each function uses the appropriate client/server, and Encoder/Decoder variables and functions.

```csharp
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
```

</li>
</ol>

### Diagnostic Test
<ol>
<li>
The application will run a diagnostic test, where the client will encode the word "ping", then send the encoded message to the server. The server will decode the received message to confirm that the original message is "ping". Then the server will encode the word "ack" and send the encoded message to the client. The client then decodes the received message, and confirms that it decodes it to the word "ack". 
</li>
</ol>

### User Interaction
<ol>
<li>
The application will continously prompt the user for an input (until the user types "quit"). That input will be encoded with the client Encoder and sent to the server.

```csharp
static bool EncodeAndSendMessage(byte[] message) {

  // Display original message.
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
  long res = _socketManager.SendMessage('m', encoded);
  if (res <= 0) {
    return false;
  }

  // Display encoded message.
  Console.WriteLine("Encoded message being sent\n: {0}", BitConverter.ToString(encoded).Replace("-", ""));
  return true;
}
```
</li>
<li>
The server will use its Decoder to decode the message.

```c
static bool ReceiveAndDecodeMessage(out byte[] message) {
  message = null;

  // Wait for return message.
  ServerSocketManager.RecvMsg msgStruct = _socketManager.ReceiveMessage();

  if (msgStruct.success == false || msgStruct.message.Length == 0 || msgStruct.header != 'm') {
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

  // Display decoded message.
  Console.WriteLine("Decoded message: {0}\n", Encoding.ASCII.GetString(message));

  return true;
}
```

</li>
<li>
Then that message will be re-encoded with the server Encoder and sent to the client.The client Decoder will then decode that message, which then will be compared with the original user input.
</li>
</ol>

### MTE Finialize

<ol>
<li>
Once the user has stopped the user input, the program should securely clear out MTE Encoder and Decoder information.

```c

// Uninstantiate Encoder and Decoder.
_encoder.Uninstantiate();
_decoder.Uninstantiate();
```
</li>
</ol>

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