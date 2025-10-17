namespace TinyMarc;

public class MarcInvalidRecordException : MarcException
{
    public MarcInvalidRecordException(string message) : base(message) { }
    public MarcInvalidRecordException(string message, Exception innerException) : base(message, innerException) { }
}
