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
  internal class BackupTargetFactory : IBackupTargetFactory
  {
    private static Dictionary<int, IBackupTarget> targets = new Dictionary<int, IBackupTarget>();
    private static int filesize;
    private static DirectoryInfo targetDirectory;
    private static string pattern;

    public BackupTargetFactory(int fileSizeInMB, DirectoryInfo backupDirectory, string filenamePattern)
    {
      filesize = fileSizeInMB;
      targetDirectory = backupDirectory;
      pattern = filenamePattern;
    }
    public void InitializeTarget(int id, long tail)
    {
      GetOrCreate(id, tail);
    }

    public IBackupTarget GetTarget(int id)
    {
      return GetTarget(id, false);
    }

    public IBackupTarget GetTarget(int id, bool createNewIfMissing)
    {
      if (createNewIfMissing)
        return GetOrCreate(id, BackupTargetConstants.DataOffset);

      if (IsCached(id))
        return targets[id];

      return null;
    }

    private static IBackupTarget GetOrCreate(int id, long tail)
    {
      if (IsCached(id))
        return targets[id];

      var target = new BackupTarget(new Sha256Hasher(), new CompressionHandler());
      target.Initialize(filesize, targetDirectory, id, tail, pattern);
      targets.Add(id, target);
      return target;
    }

    private static bool IsCached(int backupTargetIndex) => targets.ContainsKey(backupTargetIndex);

    private static void ClearCache() => targets.Clear();
  }
}
