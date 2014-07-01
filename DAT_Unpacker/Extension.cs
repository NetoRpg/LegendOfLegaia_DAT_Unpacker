using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DAT_Unpacker
{
    static class Extension
    {


        public static int extractInt32(this byte[] bytes, int index = 0)
        {
            return (bytes[index + 3] << 24) + (bytes[index + 2] << 16) + (bytes[index + 1] << 8) + bytes[index + 0];
        }


        public static byte[] extractPiece(this FileStream ms, int offset, int length, int changeOffset = -1)
        {
            if (changeOffset > -1) ms.Position = changeOffset;

            byte[] data = new byte[length];
            ms.Read(data, 0, length);

            return data;
        }

        public static byte[] extractPiece(this MemoryStream ms, int offset, int length, int changeOffset = -1)
        {
            if (changeOffset > -1) ms.Position = changeOffset;

            byte[] data = new byte[length];
            ms.Read(data, 0, length);

            return data;
        }


        public static void Save(this byte[] data, string path, int offset = -1, int length = -1)
        {
            int _offset = (offset > -1) ? offset : 0;
            int _length = (length > -1) ? length : data.Length;

            using (FileStream fs = File.Create(path))
            {
                fs.Write(data, _offset, _length);
            }
        }


        public static byte[] int32ToByteArray(this int value)
        {
            byte[] result = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                result[i] = (byte)((value >> i * 8) & 0xFF);
            }
            return result;
        }




    }
}
