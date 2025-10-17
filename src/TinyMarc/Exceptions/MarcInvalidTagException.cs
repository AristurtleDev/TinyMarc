namespace TinyMarc;

public class MarcInvalidTagException : MarcException
{
    public MarcInvalidTagException(string message) : base(message) { }
    public MarcInvalidTagException(string message, Exception innerException) : base(message, innerException) { }
}
