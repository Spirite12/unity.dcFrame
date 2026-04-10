#nullable enable
using System;
using System.IO;
using System.Text;

namespace DCFrame.Foundation {
    /// <summary>
    /// XXTEA 对称加密工具。
    /// </summary>
    public static class XXTEA {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding();
        private const UInt32 Delta = 0x9E3779B9;

        /// <summary>
        /// 使用字节数组与字节密钥进行加密。
        /// </summary>
        public static Byte[] Encrypt(Byte[] data, Byte[] key) {
            if (data.Length == 0) {
                return data;
            }
            return ToByteArray(EncryptCore(ToUInt32Array(data, true), ToUInt32Array(FixKey(key), false)), false);
        }

        /// <summary>
        /// 使用字符串与字节密钥进行加密。
        /// </summary>
        public static Byte[] Encrypt(String data, Byte[] key) {
            return Encrypt(Utf8.GetBytes(data), key);
        }

        /// <summary>
        /// 使用字节数组与字符串密钥进行加密。
        /// </summary>
        public static Byte[] Encrypt(Byte[] data, String key) {
            return Encrypt(data, Utf8.GetBytes(key));
        }

        /// <summary>
        /// 使用字符串与字符串密钥进行加密。
        /// </summary>
        public static Byte[] Encrypt(String data, String key) {
            return Encrypt(Utf8.GetBytes(data), Utf8.GetBytes(key));
        }

        /// <summary>
        /// 加密后输出 Base64 字符串。
        /// </summary>
        public static String EncryptToBase64String(Byte[] data, Byte[] key) {
            return Convert.ToBase64String(Encrypt(data, key));
        }

        /// <summary>
        /// 加密后输出 Base64 字符串。
        /// </summary>
        public static String EncryptToBase64String(String data, Byte[] key) {
            return Convert.ToBase64String(Encrypt(data, key));
        }

        /// <summary>
        /// 加密后输出 Base64 字符串。
        /// </summary>
        public static String EncryptToBase64String(Byte[] data, String key) {
            return Convert.ToBase64String(Encrypt(data, key));
        }

        /// <summary>
        /// 加密后输出 Base64 字符串。
        /// </summary>
        public static String EncryptToBase64String(String data, String key) {
            return Convert.ToBase64String(Encrypt(data, key));
        }

        /// <summary>
        /// 使用字节数组与字节密钥进行解密。
        /// </summary>
        public static Byte[] Decrypt(Byte[] data, Byte[] key) {
            if (data.Length == 0) {
                return data;
            }
            return ToByteArray(DecryptCore(ToUInt32Array(data, false), ToUInt32Array(FixKey(key), false)), true);
        }

        /// <summary>
        /// 使用字节数组与字符串密钥进行解密。
        /// </summary>
        public static Byte[] Decrypt(Byte[] data, String key) {
            return Decrypt(data, Utf8.GetBytes(key));
        }

        /// <summary>
        /// 将普通字符串按 UTF8 字节进行解密。
        /// </summary>
        public static String Decrypt(String data, String key) {
            return Utf8.GetString(Decrypt(Utf8.GetBytes(data), key));
        }

        /// <summary>
        /// 解密 Base64 字符串为字节数组。
        /// </summary>
        public static Byte[] DecryptBase64String(String data, Byte[] key) {
            return Decrypt(Convert.FromBase64String(data), key);
        }

        /// <summary>
        /// 解密 Base64 字符串为字节数组。
        /// </summary>
        public static Byte[] DecryptBase64String(String data, String key) {
            return Decrypt(Convert.FromBase64String(data), key);
        }

        /// <summary>
        /// 解密字节数组并转为字符串。
        /// </summary>
        public static String DecryptToString(Byte[] data, Byte[] key) {
            return Utf8.GetString(Decrypt(data, key));
        }

        /// <summary>
        /// 解密字节数组并转为字符串。
        /// </summary>
        public static String DecryptToString(Byte[] data, String key) {
            return Utf8.GetString(Decrypt(data, key));
        }

        /// <summary>
        /// 解密 Base64 字符串并转为字符串。
        /// </summary>
        public static String DecryptBase64StringToString(String data, Byte[] key) {
            return Utf8.GetString(DecryptBase64String(data, key));
        }

        /// <summary>
        /// 解密 Base64 字符串并转为字符串。
        /// </summary>
        public static String DecryptBase64StringToString(String data, String key) {
            return Utf8.GetString(DecryptBase64String(data, key));
        }

        /// <summary>
        /// 读取文件后按 XXTEA 解密为字符串。
        /// </summary>
        public static String DecryptFile(String path, String key) {
            return DecryptToString(File.ReadAllBytes(path), key);
        }

        /// <summary>
        /// 对字符串执行简单异或混淆。
        /// </summary>
        public static String StringEncoding(String content, String secretKey) {
            Char[] data = content.ToCharArray();
            Char[] key = secretKey.ToCharArray();
            for (Int32 i = 0; i < data.Length; i++) {
                data[i] ^= key[i % key.Length];
            }
            return new String(data);
        }

        /// <summary>
        /// 对字符串执行简单异或反混淆。
        /// </summary>
        public static String StringDecoding(String content, String secretKey) {
            Char[] data = content.ToCharArray();
            Char[] key = secretKey.ToCharArray();
            for (Int32 i = 0; i < data.Length; i++) {
                data[i] ^= key[i % key.Length];
            }
            return new String(data);
        }

        /// <summary>
        /// XXTEA 核心混合函数。
        /// </summary>
        private static UInt32 MX(UInt32 sum, UInt32 y, UInt32 z, Int32 p, UInt32 e, UInt32[] k) {
            return (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
        }

        /// <summary>
        /// 处理 XXTEA 加密核心逻辑。
        /// </summary>
        private static UInt32[] EncryptCore(UInt32[] v, UInt32[] k) {
            Int32 n = v.Length - 1;
            if (n < 1) {
                return v;
            }
            UInt32 z = v[n];
            UInt32 y;
            UInt32 sum = 0;
            UInt32 e;
            Int32 p;
            Int32 q = 6 + 52 / (n + 1);
            unchecked {
                while (0 < q--) {
                    sum += Delta;
                    e = sum >> 2 & 3;
                    for (p = 0; p < n; p++) {
                        y = v[p + 1];
                        z = v[p] += MX(sum, y, z, p, e, k);
                    }
                    y = v[0];
                    z = v[n] += MX(sum, y, z, p, e, k);
                }
            }
            return v;
        }

        /// <summary>
        /// 处理 XXTEA 解密核心逻辑。
        /// </summary>
        private static UInt32[] DecryptCore(UInt32[] v, UInt32[] k) {
            Int32 n = v.Length - 1;
            if (n < 1) {
                return v;
            }
            UInt32 z;
            UInt32 y = v[0];
            UInt32 sum;
            UInt32 e;
            Int32 p;
            Int32 q = 6 + 52 / (n + 1);
            unchecked {
                sum = (UInt32)(q * Delta);
                while (sum != 0) {
                    e = sum >> 2 & 3;
                    for (p = n; p > 0; p--) {
                        z = v[p - 1];
                        y = v[p] -= MX(sum, y, z, p, e, k);
                    }
                    z = v[n];
                    y = v[0] -= MX(sum, y, z, p, e, k);
                    sum -= Delta;
                }
            }
            return v;
        }

        /// <summary>
        /// 将密钥规整到 16 字节。
        /// </summary>
        private static Byte[] FixKey(Byte[] key) {
            if (key.Length == 16) {
                return key;
            }
            Byte[] fixedKey = new Byte[16];
            if (key.Length < 16) {
                key.CopyTo(fixedKey, 0);
            } else {
                Array.Copy(key, 0, fixedKey, 0, 16);
            }
            return fixedKey;
        }

        /// <summary>
        /// 将字节数组转换为 UInt32 数组。
        /// </summary>
        private static UInt32[] ToUInt32Array(Byte[] data, Boolean includeLength) {
            Int32 length = data.Length;
            Int32 n = ((length & 3) == 0) ? (length >> 2) : ((length >> 2) + 1);
            UInt32[] result;
            if (includeLength) {
                result = new UInt32[n + 1];
                result[n] = (UInt32)length;
            } else {
                result = new UInt32[n];
            }
            for (Int32 i = 0; i < length; i++) {
                result[i >> 2] |= (UInt32)data[i] << ((i & 3) << 3);
            }
            return result;
        }

        /// <summary>
        /// 将 UInt32 数组转换为字节数组。
        /// </summary>
        private static Byte[] ToByteArray(UInt32[] data, Boolean includeLength) {
            Int32 n = data.Length << 2;
            if (includeLength) {
                Int32 m = (Int32)data[data.Length - 1];
                n -= 4;
                if ((m < n - 3) || (m > n)) {
                    throw new InvalidDataException("XXTEA 解密后的数据长度无效。");
                }
                n = m;
            }
            Byte[] result = new Byte[n];
            for (Int32 i = 0; i < n; i++) {
                result[i] = (Byte)(data[i >> 2] >> ((i & 3) << 3));
            }
            return result;
        }
    }
}
