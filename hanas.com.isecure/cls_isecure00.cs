using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Configuration;


namespace hanas.com.isecure
{
    public class cls_isecure00
    {
        //public static string m_app_path;
        private const string sSecurityKey = "gkskgksk##!!";
        public static string m_secure_key;

        public cls_isecure00()
        {
            InitializingVars();
        }

        public void InitializingVars()
        {
            string sConfigPath = this.GetType().Assembly.Location;
            Configuration cfConfigManager = null;

            cfConfigManager = ConfigurationManager.OpenExeConfiguration(sConfigPath);

            m_secure_key = String.IsNullOrEmpty(GetAppSetting(cfConfigManager, "iSecurityKey")) ? "" : GetAppSetting(cfConfigManager, "iSecurityKey");
            m_secure_key = m_secure_key == "" ? sSecurityKey : m_secure_key;
        }

        public string GetAppSetting(Configuration vConfManager, string vKeyString)
        {
            KeyValueConfigurationElement kvElement = vConfManager.AppSettings.Settings[vKeyString];

            if (kvElement != null)
            {
                string value = kvElement.Value;

                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return string.Empty;
        }

        public string tEncrypt(string vEncryptingString)
        {
            return fncEncrypt(vEncryptingString, true);
        }

        public string tDecrypt(string vEncryptedString)
        {
            return fncDecrypt(vEncryptedString, true);
        }

        public string fncGetMd5Sum(string str)
        {
            // First we need to convert the string into bytes, which means using a text encoder.
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();

            // Create a buffer large enough to hold the string
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            // Now that we have a byte array we can ask the CSP to hash it
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);

            // Build the final string by converting each byte into hex and appending it to a StringBuilder
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }

            // Return it
            return sb.ToString();
        }

        public string fncEncrypt(string vEncryptingString, bool vUseHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(vEncryptingString);

            System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();

            string key = m_secure_key; //Get your key from config file to open the lock!

            //System.Windows.Forms.MessageBox.Show(key);

            //If hashing use get hashcode regards to your key
            if (vUseHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

                //Always release the resources and flush data of the Cryptographic service provide. Best Practice
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;                                                        //Set the secret key for the tripleDES algorithm
            tdes.Mode = CipherMode.ECB;                                                 //Mode of operation. there are other 4 modes. We choose ECB(Electronic code Book)
            tdes.Padding = PaddingMode.PKCS7;                                           //Padding mode(if any extra byte added)

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);          //Transform the specified region of bytes array to resultArray

            tdes.Clear();                                                               //Release resources held by TripleDes Encryptor

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);          //Return the encrypted data into unreadable string format
        }

        public string fncDecrypt(string vEncryptedString, bool vUseHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(vEncryptedString);             //Get the byte code of the string

            System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();

            string key = m_secure_key; //Get your key from config file to open the lock!

            if (vUseHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();      //If hashing was used get the hash code with regards to your key

                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

                hashmd5.Clear();                                                        //Release any resource held by the MD5CryptoServiceProvider
            }
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);                             //if hashing was not implemented get the byte code of the key
            }

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;                                                        //Set the secret key for the tripleDES algorithm
            tdes.Mode = CipherMode.ECB;                                                 //Mode of operation. there are other 4 modes. We choose ECB(Electronic code Book)
            tdes.Padding = PaddingMode.PKCS7;                                           //Padding mode(if any extra byte added)

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();                                                               //Release resources held by TripleDes Encryptor                

            return UTF8Encoding.UTF8.GetString(resultArray);                            //Return the Clear decrypted TEXT
        }
    }
}
