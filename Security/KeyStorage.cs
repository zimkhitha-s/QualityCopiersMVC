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
            if (File.Exists(KeyFilePath))
            {
                var storedBytes = File.ReadAllBytes(KeyFilePath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // windows DPAPI secure decrypt
                    return ProtectedData.Unprotect(storedBytes, null, DataProtectionScope.LocalMachine);
                }
                else
                {
                    return storedBytes;
                }
            }
            else
            {
                // generate a new 256-bit AES key
                using var aes = Aes.Create();
                aes.KeySize = 256;
                var newKey = aes.Key;

                byte[] toStore;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    toStore = ProtectedData.Protect(newKey, null, DataProtectionScope.LocalMachine);
                }
                else
                {
                    toStore = newKey;  
                }

                File.WriteAllBytes(KeyFilePath, toStore);
                return newKey;
            }
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//