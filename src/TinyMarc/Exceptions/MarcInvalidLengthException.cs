namespace TinyMarc;

public class MarcInvalidLengthException : MarcException
{
    public MarcInvalidLengthException(string message) : base(message) { }
    public MarcInvalidLengthException(string message, Exception innerException) : base(message, innerException) { }
}
