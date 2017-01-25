using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [XmlType("BTI")]
  [XmlInclude(typeof(FileInformation))]
  public class BackupTargetItem
  {
    public BackupTargetItem()
    {

    }

    [XmlElement("F")]
    public FileInformation File { get; set; }
    [XmlAttribute("to")]
    public long TargetOffset { get; set; }
    [XmlAttribute("tl")]
    public long TargetLength { get; set; }
    [XmlAttribute("c")]
    public bool Compressed { get; set; }
  }
}
