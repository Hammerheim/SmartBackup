using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  public interface IRunner
  {
    Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, List<string> ignoredExtensions, IProgress<ProgressReport> progressCallback);
    Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, int fileSize, string filenamePattern, bool compressIfPossible, IProgress<ProgressReport> progressCallback);
    Task<bool> CalculateMissingHashes(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback);
    Task<bool> ReplaceDublicatesWithLinks(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback);
    Task<bool> DefragmentBinaries(DirectoryInfo targetRoot, int fileSize, string filenamePattern, IProgress<ProgressReport> progressCallback);
  }
}
