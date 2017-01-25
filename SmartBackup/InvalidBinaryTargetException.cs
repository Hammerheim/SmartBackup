using System;
using System.Runtime.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [Serializable]
  internal class InvalidBinaryTargetException : Exception
  {
    private int iD;

    public InvalidBinaryTargetException()
    {
    }

    public InvalidBinaryTargetException(string message) : base(message)
    {
    }

    public InvalidBinaryTargetException(int iD)
    {
      this.iD = iD;
    }

    public InvalidBinaryTargetException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidBinaryTargetException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}