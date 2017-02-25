using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Compression;

namespace Vibe.Hammer.SmartBackup.Target
{
  public static class BackupTargetFactory
  {
    private static Dictionary<int, IBackupTarget> targets = new Dictionary<int, IBackupTarget>();
    private static int filesize;
    private static DirectoryInfo targetDirectory;
    private static string filenamePattern;

    public static IBackupTarget GetTarget(TargetContentCatalogue targetCatalogue, int fileSizeInMB, DirectoryInfo backupDirectory, string filenamePattern)
    {

      if (IsCached(targetCatalogue.BackupTargetIndex))
        return targets[targetCatalogue.BackupTargetIndex];

      var target = new BackupTarget(new MD5Hasher(), new Sha256Hasher(), new CompressionHandler());
      target.Initialize(fileSizeInMB, backupDirectory, targetCatalogue.BackupTargetIndex, targetCatalogue.CalculateTail(), filenamePattern);
      targets.Add(targetCatalogue.BackupTargetIndex, target);
      return target;
    }

    private static bool IsCached(int backupTargetIndex)
    {
      return targets.ContainsKey(backupTargetIndex);
    }
  }
}
