namespace TinyMarc;

public class MarcInvalidIndicatorException : MarcException
{

    public MarcInvalidIndicatorException(string message) : base(message) { }
    public MarcInvalidIndicatorException(string message, Exception innerException) : base(message, innerException) { }
}
