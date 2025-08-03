// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace ZeroInstall.DesktopIntegration.Windows;

partial class FileType
{
    private static string CalculateHash(string extension, string progID, DateTime lastWriteTime)
    {
        if (WindowsIdentity.GetCurrent().User is not { Value: var sid }) return "";
        string lastWriteString = new DateTime(lastWriteTime.Year, lastWriteTime.Month, lastWriteTime.Day, lastWriteTime.Hour, lastWriteTime.Minute, second: 0).ToFileTime().ToString("x16");
        const string experience = @"user choice set via windows user experience {d18b6dd5-6124-4341-9318-804003bafa0b}";
        byte[] data = Encoding.Unicode.GetBytes((extension + sid + progID).ToLower() + lastWriteString + experience + "\0");

        using var md5 = MD5.Create();
        return HashInner(data, md5.ComputeHash(data));
    }

    private static string HashInner(byte[] data, byte[] hashedData)
        => Convert.ToBase64String(BitwiseUtils.XOr(
            HashInnerPart1(data, hashedData),
            HashInnerPart2(data, hashedData)));

    private static byte[] HashInnerPart1(byte[] data, byte[] hashedData)
    {
        byte[] result = new byte[8];

        uint length = (uint)((((data.Length >> 2) & 1) < 1 ? 1 : 0) + (data.Length >> 2) - 1);
        uint[] dword_data = new uint[length];
        uint[] dword_md5 = new uint[4];
        for (int i = 0; i < dword_data.Length; i++)
            dword_data[i] = BitConverter.ToUInt32(data, i * 4);
        dword_md5[0] = BitConverter.ToUInt32(hashedData, 0);
        dword_md5[1] = BitConverter.ToUInt32(hashedData, 4);
        dword_md5[2] = BitConverter.ToUInt32(hashedData, 8);
        dword_md5[3] = BitConverter.ToUInt32(hashedData, 12);

        if (length <= 1 || (length & 1) == 1)
            return result;

        uint v5 = 0, v6 = 0;
        uint v7 = (length - 2) >> 1;
        uint v18 = v7++;
        uint v8 = v7;
        uint v19 = v7;
        uint res = 0;
        uint v9 = (dword_md5[1] | 1) + 0x13DB0000u;
        uint v10 = (dword_md5[0] | 1) + 0x69FB0000u;

        do
        {
            uint v11 = dword_data[v6] + res;
            v6 += 2;
            uint v12 = 0x79F8A395u * (v10 * v11 - 0x10FA9605u * (v11 >> 16)) + 0x689B6B9Fu * ((v10 * v11 - 0x10FA9605u * (v11 >> 16)) >> 16);
            uint v13 = 0xEA970001u * v12 - 0x3C101569u * (v12 >> 16);
            uint v14 = v13 + v5;
            uint v15 = v9 * (dword_data[v6 - 1] + v13) - 0x3CE8EC25u * ((dword_data[v6 - 1] + v13) >> 16);
            res = 0x1EC90001u * (0x59C3AF2Du * v15 - 0x2232E0F1u * (v15 >> 16)) + 0x35BD1EC9u * ((0x59C3AF2Du * v15 - 0x2232E0F1u * (v15 >> 16)) >> 16);
            v5 = res + v14;
            --v8;
        } while (v8 != 0);
        if (length - 2 - 2 * v18 == 1)
        {
            uint v16 = (dword_data[2 * v19] + res) * v10 - 0x10FA9605u * ((dword_data[2 * v19] + res) >> 16);
            uint v17 = 0x39646B9Fu * (v16 >> 16) + 0x28DBA395u * v16 - 0x3C101569u * ((0x689B6B9Fu * (v16 >> 16) + 0x79F8A395u * v16) >> 16);
            res = 0x35BD1EC9u * ((0x59C3AF2Du * (v17 * v9 - 0x3CE8EC25u * (v17 >> 16)) - 0x2232E0F1u * ((v17 * v9 - 0x3CE8EC25u * (v17 >> 16)) >> 16)) >> 16) + 0x2A18AF2Du * (v17 * v9 - 0x3CE8EC25u * (v17 >> 16)) - 0xFD6BE0F1u * ((v17 * v9 - 0x3CE8EC25u * (v17 >> 16)) >> 16);
            v5 += res + v17;
        }

        BitConverter.GetBytes(res).CopyTo(result, 0);
        BitConverter.GetBytes(v5).CopyTo(result, 4);
        return result;
    }

    private static byte[] HashInnerPart2(byte[] data, byte[] hashedData)
    {
        byte[] result = new byte[8];

        uint length = (uint)((((data.Length >> 2) & 1) < 1 ? 1 : 0) + (data.Length >> 2) - 1);
        uint[] dword_data = new uint[length];
        uint[] dword_md5 = new uint[4];
        for (int i = 0; i < dword_data.Length; i++)
            dword_data[i] = BitConverter.ToUInt32(data, i * 4);
        dword_md5[0] = BitConverter.ToUInt32(hashedData, 0);
        dword_md5[1] = BitConverter.ToUInt32(hashedData, 4);
        dword_md5[2] = BitConverter.ToUInt32(hashedData, 8);
        dword_md5[3] = BitConverter.ToUInt32(hashedData, 12);

        if (length <= 1 || (length & 1) == 1)
            return result;

        uint v5 = 0, v6 = 0, v7 = 0;
        uint v25 = (length - 2) >> 1;
        uint v21 = dword_md5[0] | 1;
        uint v22 = dword_md5[1] | 1;
        uint v23 = 0xB1110000u * v21;
        uint v24 = 0x16F50000u * v22;
        uint v8 = v25 + 1;

        do
        {
            v6 += 2;
            uint v9 = (dword_data[v6 - 2] + v5) * v23 - 0x30674EEFu * (v21 * (dword_data[v6 - 2] + v5) >> 16);
            uint v10 = v9 >> 16;
            uint v11 = 0xE9B30000u * v10 + 0x12CEB96Du * ((0x5B9F0000u * v9 - 0x78F7A461u * v10) >> 16);
            uint v12 = 0x1D830000u * v11 + 0x257E1D83u * (v11 >> 16);
            uint v13 = ((v12 + dword_data[v6 - 1]) * v24 - 0x5D8BE90Bu * ((v22 * (v12 + dword_data[v6 - 1])) >> 16)) >> 16;
            uint v14 = 0x96FF0000u * ((v12 + dword_data[v6 - 1]) * v24 - 0x5D8BE90Bu * ((v22 * (v12 + dword_data[v6 - 1])) >> 16)) - 0x2C7C6901u * v13 >> 16;
            v5 = 0xF2310000u * v14 - 0x405B6097u * ((0x7C932B89u * v14 - 0x5C890000u * v13) >> 16);
            v7 += v5 + v12;
            --v8;
        } while (v8 != 0);
        if (length - 2 - 2 * v25 == 1)
        {
            uint v15 = 0xB1110000u * v21 * (dword_data[2 * (v25 + 1)] + v5) - 0x30674EEFu * (v21 * (dword_data[2 * (v25 + 1)] + v5) >> 16);
            uint v16 = v15 >> 16;
            uint v17 = (0x5B9F0000u * v15 - 0x78F7A461u * (v15 >> 16)) >> 16;
            uint v18 = 0x257E1D83u * ((0xE9B30000u * v16 + 0x12CEB96Du * v17) >> 16) + 0x3BC70000u * v17;
            uint v19 = (0x16F50000u * v18 * v22 - 0x5D8BE90Bu * (v18 * v22 >> 16)) >> 16;
            uint v20 = (0x96FF0000u * (0x16F50000u * v18 * v22 - 0x5D8BE90Bu * (v18 * v22 >> 16)) - 0x2C7C6901u * v19) >> 16;
            v5 = 0xF2310000u * v20 - 0x405B6097u * ((0x7C932B89u * v20 - 0x5C890000u * v19) >> 16);
            v7 += v5 + v18;
        }

        BitConverter.GetBytes(v5).CopyTo(result, 0);
        BitConverter.GetBytes(v7).CopyTo(result, 4);

        return result;
    }
}
