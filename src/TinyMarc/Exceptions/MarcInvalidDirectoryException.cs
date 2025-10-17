namespace TinyMarc;

public class MarcInvalidDirectoryException : MarcException
{
    public MarcInvalidDirectoryException(string message) : base(message) { }
    public MarcInvalidDirectoryException(string message, Exception innerException) : base(message, innerException) { }
}
