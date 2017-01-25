using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Progress
{
  public class ProgressReport
  {
    public ProgressReport(string additionalInfo)
    {
      AdditionalInfo = additionalInfo;
    }

    public ProgressReport(string additionalInfo, int currentActionNumber)
      : this(additionalInfo)
    {
      CurrentActionNumber = currentActionNumber;
    }

    public ProgressReport(string additionalInfo, int currentActionNumber, int expectedNumberOfActions)
      : this(additionalInfo, currentActionNumber)
    {
      ExpectedNumberOfActions = expectedNumberOfActions;
    }

    public string AdditionalInfo { get; set; }

    public int CurrentActionNumber { get; set; }

    public int ExpectedNumberOfActions { get; set; }
  }
}
