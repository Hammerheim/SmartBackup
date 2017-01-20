using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Compression
{
  public interface ICompressionHandler
  {
    Task<bool> CompressFile(string fullyQualifiedFilename);
    Task<bool> CompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result, long offset, long length);
    Task<FileInfo> DecompressFile(string fullyQualifiedFilename);

  }
}
