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
    Task<bool> CompressFile(FileInfo sourceFile, FileInfo targetFile, CompressionModes moode);
    Task<bool> CompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result);
    Task<bool> DecompressStream(Stream source, Stream result, long offset, long length);
    Task<bool> DecompressFile(FileInfo compressedFile, FileInfo targetFile, CompressionModes mode);

  }
}
