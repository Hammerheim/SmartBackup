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
    Task<bool> CompressFile(FileInfo sourceFile, FileInfo targetFile);
    Task<bool> DecompressFile(FileInfo compressedFile, FileInfo targetFile);
  }
}
