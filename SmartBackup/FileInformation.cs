using System;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [XmlType("FI")]
  public class FileInformation
  {
    public FileInformation()
    {

    }

    public FileInformation(string content)
      : this()
    {
      var contentSections = content.Split(',');
      Directory = contentSections[0];
      FileName = contentSections[1];
      Size = long.Parse(contentSections[2]);
      LastModified = DateTime.Parse(contentSections[3]);
      FullyQualifiedFilename = contentSections[4];
      ContentHash = contentSections[5];
      Version = int.Parse(contentSections[6]);
      RelativePath = contentSections[7];
    }

    [XmlElement("FQF")]
    public string FullyQualifiedFilename { get; set; }
    [XmlAttribute("ch")]
    public string ContentHash { get; set; }
    [XmlElement("D")]
    public string Directory { get; set; }
    [XmlElement("FN")]
    public string FileName { get; set; }
    [XmlIgnore]
    public DateTime LastModified { get; set; }
    [XmlAttribute("s")]
    public long Size { get; set; }
    [XmlAttribute("lm")]
    public string LastModifiedText
    {
      get { return LastModified.ToString("o"); }
      set { LastModified = DateTime.Parse(value); }
    }
    [XmlAttribute("v")]
    public int Version { get; set; }

    [XmlElement("rp")]
    public string RelativePath { get; set; }
    public override string ToString()
    {
      return $"{Directory},{FileName},{Size},{LastModified.ToString("o")},{FullyQualifiedFilename},{ContentHash},{Version},{RelativePath}";
    }
  }
}