using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("CCBE")]
  public class ContentCatalogueBinaryEntry : ContentCatalogueEntry
  {
    [XmlAttribute("to")]
    public long TargetOffset { get; set; }

    [XmlAttribute("tl")]
    public long TargetLength { get; set; }

    [XmlAttribute("c")]
    public bool Compressed { get; set; }

    [XmlAttribute("pch")]
    public string PrimaryContentHash { get; set; }

    [XmlAttribute("pch")]
    public string SecondaryContentHash { get; set; }

  }
}
