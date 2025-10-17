namespace TinyMarc;

public class MarcInvalidLeaderLengthException : MarcException
{
    public MarcInvalidLeaderLengthException(string message) : base(message) { }
    public MarcInvalidLeaderLengthException(string message, Exception innerException) : base(message, innerException) { }
}
