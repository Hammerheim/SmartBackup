using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface ICompressionHandler
  {
    Task<bool> CompressFile(string fullyQualifiedFilename);
    bool CompressStream(Stream source, Stream result);

  }
}
