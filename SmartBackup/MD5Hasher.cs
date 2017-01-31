﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class MD5Hasher : IHasher
  {
    public async Task<byte[]> GetHash(string source)
    {
      using (var md5 = MD5.Create())
      {
        var bytes = Encoding.ASCII.GetBytes(source);
        return await Task.Run(() => md5.ComputeHash(bytes));
      }
    }

    public async Task<byte[]> GetHash(FileInfo file)
    {
      using (var md5 = MD5.Create())
      {
        using (var stream = File.OpenRead(file.FullName))
        {
          return await Task.Run(() => md5.ComputeHash(stream));
        }
      }
    }

    public async Task<string> GetHashString(FileInfo file)
    {
        var array = await GetHash(file);
        return array != null ? ByteConverter.ByteArrayToString(array) : string.Empty;
    }

    public async Task<string> GetHashString(string source)
    {
      var array = await GetHash(source);
      return array != null ? ByteConverter.ByteArrayToString(array) : string.Empty;
    }

    public async Task<string> ToHashString(string source)
    {
      return await GetHashString(source);
    }
  }
}