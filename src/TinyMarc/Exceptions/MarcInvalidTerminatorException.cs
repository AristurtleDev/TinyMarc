namespace TinyMarc;

public class MarcInvalidTerminatorException : MarcException
{
    public MarcInvalidTerminatorException(string message) : base(message) { }
    public MarcInvalidTerminatorException(string message, Exception innerException) : base(message, innerException) { }
}
