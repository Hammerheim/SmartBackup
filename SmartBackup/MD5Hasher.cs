﻿//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace Vibe.Hammer.SmartBackup
//{
//  internal class MD5Hasher : HasherBase
//  {
//    protected override async Task<byte[]> GetHash(FileInfo file)
//    {
//      using (var md5 = MD5.Create())
//      {
//        using (var stream = File.OpenRead(file.FullName))
//        {
//          return await Task.Run(() => md5.ComputeHash(stream));
//        }
//      }
//    }
//  }
//}
