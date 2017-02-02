using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("CCULE")]
  public class ContentCatalogueUnclaimedLinkEntry : ContentCatalogueLinkEntry
  {
    public ContentCatalogueUnclaimedLinkEntry(ContentCatalogueBinaryEntry linkFrom, ContentCatalogueBinaryEntry linkTo)
      : base(linkFrom, linkTo)
    {
      TargetLength = linkFrom.TargetLength;
      TargetOffset = linkFrom.TargetOffset;
    }

    [XmlAttribute("to")]
    public long TargetOffset { get; set; }
    [XmlAttribute("tl")]
    public long TargetLength { get; set; }
  }
}
