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
    public ContentCatalogueBinaryEntry()
    {

    }

    public ContentCatalogueBinaryEntry(ContentCatalogueBinaryEntry copyThis)
      : base(copyThis)
    {
      this.TargetOffset = copyThis.TargetOffset;
      this.TargetLength = copyThis.TargetLength;
      this.Compressed = copyThis.Compressed;
      this.PrimaryContentHash = copyThis.PrimaryContentHash;
      this.Verified = copyThis.Verified;
    }

    [XmlAttribute("to")]
    public long TargetOffset { get; set; }

    [XmlAttribute("tl")]
    public long TargetLength { get; set; }

    [XmlAttribute("c")]
    public bool Compressed { get; set; }

    [XmlAttribute("pch")]
    public string PrimaryContentHash { get; set; }

    [XmlAttribute("vf")]
    public bool Verified { get; set; }

    public override ContentCatalogueEntry Clone()
    {
      return new ContentCatalogueBinaryEntry(this);
    }
  }
}
