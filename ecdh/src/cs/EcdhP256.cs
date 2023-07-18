// The MIT License (MIT)
//
// Copyright (c) Eclypses, Inc.
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Eclypses.EcdhP256 {
  /// <summary>
  /// Interface of an entropy input callback.
  /// </summary>
  public interface IEcdhP256EntropyCallback {
    int EntropyCallback(byte[] entropyInput);
  }

  /// <summary>
  /// Class EcdhP256
  ///
  /// This is ECDH interface class.
  ///
  /// </summary>
  public class EcdhP256 : IEcdhP256 {

    public const int Success = 0;
    public const int RandomFail = -1;
    public const int InvalidPubKey = -2;
    public const int InvalidPrivKey = -3;
    public const int MemoryFail = -4;

    public const int SzPublicKey = 64;
    public const int SzPrivateKey = 32;
    public const int SzSecretData = 32;



    /// <summary>
    /// Constructor.
    /// </summary>
    public EcdhP256() {
      myPrivateKey = new byte[SzPrivateKey];
      myPublicKey = new byte[SzPublicKey];

      myEntropyDelegate = new ENTROPY_CALLBACK(EcdhP256DefaultEntropyCallback);
      myEntropyCb = null;
      myEntropyInput = null;
    }

    /// <summary>
    /// Destructor.
    /// </summary>
    ~EcdhP256() {
      // Zeroize variables
      Array.Clear(myPrivateKey);
      Array.Clear(myPublicKey);
    }

    /// <summary>
    /// Calls ECDH to create a key pair.
    /// Returns the public key. The private key stays
    /// inside this class.
    /// </summary>
    unsafe public int CreateKeyPair(byte[] publicKey) {
      UInt32 szPrivateKey = (UInt32)myPrivateKey.Length;
      UInt32 szPublicKey = (UInt32)myPublicKey.Length;
      Int32 status;
      //---------------------------------------------
      // Check if the keys have already been created. 
      //---------------------------------------------
      if (!haveKeys)
      {
        // Check if publicKey is big enough
        // to receive a raw key. We know that
        // myPublicKey is big enough for that.
        if (publicKey.Length < SzPublicKey)
          return MemoryFail;
        // Create the private and public keys.
        if ((myEntropyCb == null) && (myEntropyInput == null))
          status = ecdh_p256_wrap_create_keypair(myPrivateKey, &szPrivateKey,
                                                 myPublicKey, &szPublicKey,
                                                 null, IntPtr.Zero);
        else
          // We do not need to pass a "context" to this
          // function because "myEntropyDelegate" already
          // is a pointer to "EcdhP256DefaultEntropyCallback"
          // for the calling instance of EcdhP256.
          status = ecdh_p256_wrap_create_keypair(myPrivateKey, &szPrivateKey,
                                                 myPublicKey, &szPublicKey,
                                                 myEntropyDelegate, IntPtr.Zero);
        if (status != Success)
          return status;
      }
      else
        status = Success;
      //---------------------------------------------
      // Copy the data from myPublicKey to publicKey.
      //---------------------------------------------
      Array.Copy(myPublicKey, 0, publicKey, 0, szPublicKey);
      haveKeys = true;
      return status;
    }



    /// <summary>
    /// Creates a shared secret using the supplied peer's public key.
    /// </summary>
    unsafe public int GetSharedSecret(byte[]? peerPublicKey, byte[] secret) {
      //------------------------------------------------------
      // If the private key has not been set, return an error.
      //------------------------------------------------------
      if (!haveKeys)
        return InvalidPrivKey;
      //-----------------------------------------------------
      // Check if the result buffer would hold a P256 secret.
      //-----------------------------------------------------
      if (secret.Length < SzSecretData)
        return MemoryFail;
      if (peerPublicKey.Length != SzPublicKey)
        return InvalidPubKey;
      //----------------------
      // Create shared secret.
      //----------------------
      UInt32 szSecret = (UInt32)secret.Length;
      Int32 status = ecdh_p256_wrap_create_secret
                     (myPrivateKey, (UInt32)myPrivateKey.Length,
                      peerPublicKey, (UInt32)peerPublicKey.Length,
                      secret, &szSecret);
      // Zeroize the private key and reset "haveKeys",
      // whether this just worked or not.
      Array.Clear(myPrivateKey);
      haveKeys = false;
      return status;
    }



    /// <summary>
    /// Set the entropy callback. If not null, it is called to get entropy. If
    /// null, the entropy set with SetEntropy() is used.
    /// </summary>
    public void SetEntropyCallback(IEcdhP256EntropyCallback cb) {
      myEntropyCb = cb;
    }



    /// <summary>
    /// Set the entropy input value. This must be done before calling an
    /// instantiation method that will trigger the entropy callback.
    ///
    /// The entropy is zeroized when used by an instantiation call.
    ///
    /// If the entropy callback is null, entropyInput is used as the entropy.
    /// </summary>
    public int SetEntropy(byte[] entropyInput) {
      if (entropyInput.Length != SzPrivateKey)
        return InvalidPrivKey;
      myEntropyInput = entropyInput;
      return Success;
    }



    /// <summary>
    /// Calls the internal random generator and fills the
    /// given "output" with random data. The function will fail
    /// if the implementation does not contain support for an
    /// OS supplied random number generator.
    /// </summary>
    public static int GetRandom(byte[] dest) {
      return ecdh_p256_wrap_random(dest, (uint)dest.Length);
    }



    /// <summary>
    /// Zeroize the given byte array.
    /// </summary>
    public static void Zeroize(byte[] dest) {
      ecdh_p256_wrap_zeroize(dest, (uint)dest.Length);
    }



    /// <summary>
    /// Internal entropy callback.
    /// </summary>
    protected Int32 EcdhP256DefaultEntropyCallback(IntPtr context,
                                                   IntPtr entropy,
                                                   UInt32 entropySize) {
      byte[] b = new byte[entropySize];
      Int32 status = EntropyCallback(b);
      if (status == Success)
        Marshal.Copy(b, 0, entropy, (int)entropySize);
      return status;
    }



    /// <summary>
    /// The entropy callback.
    /// </summary>
    protected int EntropyCallback(byte[] entropy) {
      // Call the callback if set.
      if (myEntropyCb != null) {
        return myEntropyCb.EntropyCallback(entropy);
      }
      // Check the length for entropyInput and given entropy param.
      int eiLen = myEntropyInput == null ? 0 : myEntropyInput.Length;
      if ((eiLen != SzPrivateKey) || (entropy.Length < SzPrivateKey)) {
        return InvalidPrivKey;
      }
      // Copy entropy input.
#pragma warning disable CS8604 // Possible null reference argument.
      Array.Copy(myEntropyInput, entropy, eiLen);
#pragma warning restore CS8604 // Possible null reference argument.
      Array.Clear(myEntropyInput);
      myEntropyInput = null;
      // Success.
      return Success;
    }



    /// <summary>
    /// Callback delegates.
    /// </summary>
    protected ENTROPY_CALLBACK myEntropyDelegate;



    /// <summary>
    /// Callback.
    /// </summary>
    private IEcdhP256EntropyCallback? myEntropyCb;



    /// <summary>
    /// entropy input.
    /// </summary>
    private byte[]? myEntropyInput;

    private bool haveKeys = false;
    private readonly byte[] myPrivateKey;
    private readonly byte[] myPublicKey;



    /// <summary>
    /// Library function declarations.
    /// </summary>
    [DllImport("mtesupport-ecdh", CallingConvention = CallingConvention.Cdecl)]
    unsafe private static extern Int32 ecdh_p256_wrap_create_keypair
           (byte[] privateKeyData, UInt32* privateKeySize,
            byte[] publicKeyData, UInt32* publicKeySize,
            ENTROPY_CALLBACK? entropyCb,
            IntPtr entropyContext);

    [DllImport("mtesupport-ecdh", CallingConvention = CallingConvention.Cdecl)]
    unsafe private static extern Int32 ecdh_p256_wrap_create_secret
           (byte[] privateKeyData, UInt32 privateKeySize,
            byte[]? peerPublicKeyData, UInt32 peerPublicKeySize,
            byte[] secret, UInt32* secretSize);

    [DllImport("mtesupport-ecdh", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ecdh_p256_wrap_zeroize(byte[] dest, UInt32 len);

    [DllImport("mtesupport-ecdh", CallingConvention = CallingConvention.Cdecl)]
    private static extern Int32 ecdh_p256_wrap_random(byte[] dest, UInt32 len);
  }



  // Method signatures for the internal callback.
  public delegate Int32 ENTROPY_CALLBACK(IntPtr context, IntPtr entropy, UInt32 entropySize);
}
