using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal static class ByteConverter
  {
    public static string ByteArrayToString(byte[] ba)
    {
      string hex = BitConverter.ToString(ba);
      return hex.Replace("-", "");
    }
  }
}
