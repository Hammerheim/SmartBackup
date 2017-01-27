using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class AddFileToArchiveException : Exception
  {
    public AddFileToArchiveException()
    {
    }

    public AddFileToArchiveException(string message) : base(message)
    {
    }

    public AddFileToArchiveException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected AddFileToArchiveException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}