using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class InsertFileException : Exception
  {
    public InsertFileException()
    {
    }

    public InsertFileException(string message) : base(message)
    {
    }

    public InsertFileException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InsertFileException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}