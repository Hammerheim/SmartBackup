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
    bool ShouldCompress(FileInfo file);
    Task<FileInfo> CompressFile(FileInfo fullyQualifiedFilename);
    Task<bool> CompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result, long offset, long length);
    Task<FileInfo> DecompressFile(FileInfo fullyQualifiedFilename);

  }
}
