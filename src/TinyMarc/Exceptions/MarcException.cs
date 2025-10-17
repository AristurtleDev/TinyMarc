namespace TinyMarc;

public abstract class MarcException : Exception
{
    public MarcException(string message) : base(message) { }
    public MarcException(string message, Exception innerException) : base(message, innerException) { }
}
