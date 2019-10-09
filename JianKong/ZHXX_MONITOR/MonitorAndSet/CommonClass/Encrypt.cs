using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace MonitorAndSet.CommonClass
{
	/// <summary>
	/// 一个通用的加密、解密类
	/// </summary>
	public class Encrypt
	{
		/// <summary>
		/// 加密
		/// </summary>
		/// <param name="str">待加密的明文字符串</param>
		/// <param name="key">密钥</param>
		/// <returns>加密后的字符串</returns>
		public static string EncryptString(string str,string key)
		{
			byte[] bStr=(new UnicodeEncoding()).GetBytes(str);
			byte[] bKey=(new UnicodeEncoding()).GetBytes(key);

			for(int i=0; i<bStr.Length; i+=2) 
			{ 
				for(int j=0; j<bKey.Length; j+=2) 
				{ 
					bStr[i] = Convert.ToByte(bStr[i]^bKey[j]); 
				} 
			}

			return (new UnicodeEncoding()).GetString(bStr).TrimEnd('\0');
		}

		/// <summary>
		/// 解密
		/// </summary>
		/// <param name="str">待解密的密文字符串</param>
		/// <param name="key">密钥</param>
		/// <returns>解密后的明文</returns>
		public static string DecryptString(string str,string key)
		{
			return EncryptString(str,key);
		}
	}
    public class DESEncrypt
    {
        [DllImport(@"desdll.dll")]
        
        public static extern int gen_pinblock(StringBuilder key, StringBuilder pwd, StringBuilder pinblock);

        public static string encrypt(string Pwd)
        {
            StringBuilder Key = new StringBuilder("12345678");
            StringBuilder PinBlock = new StringBuilder();
            StringBuilder PwdBuilder = new StringBuilder(Pwd);
            gen_pinblock(Key, PwdBuilder, PinBlock);
            return PinBlock.ToString();
        }
    }
}
