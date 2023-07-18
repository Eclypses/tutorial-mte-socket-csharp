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

namespace Eclypses.EcdhP256 {
  /// <summary>
  /// Interface IEcdhP256
  ///
  /// This is the class for accessing ECDH.
  /// </summary>
  public interface IEcdhP256 {

    /// <summary>
    /// Creates a key pair and returns the public key.
    /// </summary>
    int CreateKeyPair(byte[] publicKey);

    /// <summary>
    /// Creates a shared secret using the supplied peer's public key.
    /// </summary>
    int GetSharedSecret(byte[]? peerPublicKey, byte[] secret);

    /// <summary>
    /// Set the entropy callback. If not null, it is called to get entropy. If
    /// null, the entropy set with SetEntropy() is used.
    /// </summary>
    void SetEntropyCallback(IEcdhP256EntropyCallback cb);

    /// <summary>
    /// Set the entropy input value. This must be done before calling an
    /// instantiation method that will trigger the entropy callback.
    ///
    /// The entropy is zeroized when used by an instantiation call.
    ///
    /// If the entropy callback is null, entropyInput is used as the entropy.
    /// </summary>
    int SetEntropy(byte[] entropyInput);
  }
}
