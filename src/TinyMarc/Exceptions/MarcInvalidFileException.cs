namespace TinyMarc;

public class MarcInvalidFileException : MarcException
{
    public MarcInvalidFileException(string message) : base(message) { }

    public MarcInvalidFileException(string message, Exception innerException) : base(message, innerException) { }
}
