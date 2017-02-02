﻿using System;
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
    Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback);
    Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback);
    Task<bool> CalculateMissingHashes(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback);
    Task<bool> ReplaceDublicatesWithLinks(DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback);
  }
}
