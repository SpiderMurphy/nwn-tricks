using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace NWNMasterServer.libs
{
    public static class Hashing
    {
        /**
         * Metodo hash password + salt
         */
        public static String Md5PwdHashStr(String password, byte[] salt)
        {
            // Hash
            String hash = "";

            try
            {
                byte[] md5buffer = new byte[64];

                Buffer.BlockCopy(Encoding.UTF8.GetBytes(password), 0, md5buffer, 0, 32);
                Buffer.BlockCopy(salt, 0, md5buffer, 32, 32);

                // Md5 digest
                MD5 md5 = MD5.Create();

                byte[] md5hash = md5.ComputeHash(md5buffer);

                // Imposta  stringa esadecimale
                for (int i = 0; i < md5hash.Length; i++)
                {
                    hash += md5hash[i].ToString("x2");
                }
            }
            catch (Exception e)
            {
            }

            // Exit
            return hash;
        }
    }
}
