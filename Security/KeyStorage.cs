using System.Runtime.InteropServices;
using System.Security.Cryptography;


namespace INSY7315_ElevateDigitalStudios_POE.Security
{
    public class KeyStorage
    {
        private static readonly string KeyFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "secure_aes_key.dat");

        public static byte[] GetOrCreateKey()
        {
            // If a key file already exists, load it
            if (File.Exists(KeyFilePath))
            {
                var storedBytes = File.ReadAllBytes(KeyFilePath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows DPAPI secure decrypt
                    return ProtectedData.Unprotect(storedBytes, null, DataProtectionScope.LocalMachine);
                }
                else
                {
                    // Non-Windows: just return the raw key (unencrypted)
                    return storedBytes;
                }
            }
            else
            {
                // Generate a new 256-bit AES key
                using var aes = Aes.Create();
                aes.KeySize = 256;
                var newKey = aes.Key;

                byte[] toStore;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Protect using DPAPI on Windows
                    toStore = ProtectedData.Protect(newKey, null, DataProtectionScope.LocalMachine);
                }
                else
                {
                    // Non-Windows: fallback, store raw key (can later replace with a stronger option)
                    toStore = newKey;  
                }

                File.WriteAllBytes(KeyFilePath, toStore);
                return newKey;
            }
        }
    }
}
