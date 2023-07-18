using System;
using System.Collections.Generic;
using System.Text;
using Eclypses.EcdhP256;

namespace SocketsTutorialCSharp {
  internal class MteSetupInfo {
    private readonly EcdhP256 myEcdhManager;
    private readonly byte[] myPublicKey;

    public MteSetupInfo() {
      // Set public key size.
      myPublicKey = new byte[EcdhP256.SzPublicKey];

      myEcdhManager = new EcdhP256();

      // Create the private and public keys.
      int res = myEcdhManager.CreateKeyPair(myPublicKey);
      if (res < 0) {
        throw new Exception("Unable to create key pair: " + res);
      }
    }

     ~MteSetupInfo() {
       EcdhP256.Zeroize(myPublicKey);
     }

     public byte[] GetPublicKey() {
       return myPublicKey;
     }

     public byte[] GetSharedSecret() {
       if (PeerKey is { Length: 0 }) {
         throw new Exception("Unable to create shared secret: " + EcdhP256.MemoryFail);
       }

       // Create shared secret.
       byte[] secret = new byte[EcdhP256.SzSecretData];
       int res = myEcdhManager.GetSharedSecret(PeerKey, secret);
       if (res < 0) {
         throw new Exception("Unable to create shared secret: " + EcdhP256.MemoryFail);
       }

       return secret;
     }

     public byte[]? Personalization { get; set; }

     public byte[]? Nonce { get; set; }

     public byte[]? PeerKey { get; set; }
  }
}
