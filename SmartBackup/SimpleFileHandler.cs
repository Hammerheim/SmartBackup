using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class SimpleFileHandler : IFileHandler
  {
    private IFileInformationGatherer gatherer;
    private IFileLog logger;

    public SimpleFileHandler(IFileInformationGatherer gatherer, IFileLog logger)
    {
      this.gatherer = gatherer;
      this.logger = logger;
    }

    public async Task<bool> Handle(FileInfo info, bool deepScan)
    {
      var fileData = await gatherer.Gather(info, deepScan);
      if (fileData != null)
        logger.Log(fileData);
      return true;
    }
  }
}
