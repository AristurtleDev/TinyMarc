using System.Runtime.CompilerServices;
using System.Text;

namespace TinyMarc;

/// <summary>
/// Reads MARC-21 records from files, streams, or strings.
/// </summary>
public sealed class MarcReader : IDisposable, IAsyncDisposable
{
    private readonly Stream? _stream;
    private readonly bool _leaveOpen;
    private readonly Encoding _utf8Encoding;
    private readonly Encoding _marc8Encoding;

    /// <summary>
    /// Gets a value that indicates whether this reader has been disposed of.
    /// </summary>
    public bool IsDisposed { get; private set; }

    private MarcReader()
    {
        _utf8Encoding = Encoding.UTF8;
        _marc8Encoding = new Marc8Encoding();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarcReader"/> class from a file path.
    /// </summary>
    /// <param name="filepath">Path to the MARC file.</param>
    /// <exception cref="MarcInvalidFileException">Thrown when <paramref name="filepath"/> cannot be opened.</exception>
    public MarcReader(string filepath) : this()
    {
        ArgumentException.ThrowIfNullOrEmpty(filepath);
        try
        {
            _stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _leaveOpen = false;
        }
        catch (Exception ex)
        {
            throw new MarcInvalidFileException($"Invalid input file: {filepath}", ex);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarcReader"/> class from a stream
    /// </summary>
    /// <param name="stream">Stream containing MARC records.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after disposal.</param>
    public MarcReader(Stream stream, bool leaveOpen = false) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Reads the next MARC record from the source.
    /// </summary>
    /// <returns>Next MARC record, or <see langword="null"/> if no more records.</returns>
    public Record? ReadRecord()
    {
        byte[]? rawRecord = ReadNextRaw();

        if (rawRecord == null)
        {
            return null;
        }

        Encoding encoding = DetectEncoding(rawRecord);
        return DecodeRecord(rawRecord, encoding);
    }

    /// <summary>
    /// Asynchronously reads the next MARC record from the source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Next MARC record, or <see langword="null"/> if no more records.</returns>
    public async Task<Record?> ReadRecordAsync(CancellationToken cancellationToken = default)
    {
        byte[]? rawRecord = await ReadNextRawAsync(cancellationToken).ConfigureAwait(false);

        if (rawRecord == null)
        {
            return null;
        }

        Encoding encoding = DetectEncoding(rawRecord);
        return DecodeRecord(rawRecord, encoding);
    }

    /// <summary>
    /// Reads all MARC records from the source.
    /// </summary>
    /// <returns>Enumerable collection of MARC records.</returns>
    public IEnumerable<Record> ReadRecords()
    {
        Record? record;
        while ((record = ReadRecord()) != null)
        {
            yield return record;
        }
    }

    /// <summary>
    /// Asynchronously reads all MARC records from the source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable collection of MARC records.</returns>
    public async IAsyncEnumerable<Record> ReadRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Record? record;
        while ((record = await ReadRecordAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            yield return record;
        }
    }

    private Encoding DetectEncoding(ReadOnlySpan<byte> rawRecord)
    {
        // Check if record has a valid leader
        if (rawRecord.Length < MarcConstants.LeaderLength)
        {
            return _utf8Encoding;
        }

        // LEADER[9] contains the character encoding scheme
        // ' ' (space, 0x20) = MARC-8
        // 'a' (0x61) = UCS/Unicode (UTF-8)
        byte encodingByte = rawRecord[9];

        if (encodingByte == 0x20)
        {
            return _marc8Encoding;
        }

        return _utf8Encoding;
    }

    private byte[]? ReadNextRaw()
    {
        if (_stream == null)
        {
            return null;
        }

        List<byte> buffer = [];
        int b;

        while ((b = _stream.ReadByte()) != -1)
        {
            buffer.Add((byte)b);

            if (b == MarcConstants.EndOfRecord)
            {
                // Remove illegal characters that sometimes occur between records
                while (buffer.Count > 0 && (buffer[0] == 0x0A || buffer[0] == 0x0D || buffer[0] == 0x00))
                {
                    buffer.RemoveAt(0);
                }

                return buffer.ToArray();
            }

            if (buffer.Count > MarcConstants.MaxRecordLength)
            {
                throw new MarcInvalidLengthException($"Record exceeds maximum length of {MarcConstants.MaxRecordLength} bytes");
            }
        }

        return buffer.Count > 0 ? buffer.ToArray() : null;
    }

    private async Task<byte[]?> ReadNextRawAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            return null;
        }

        List<byte> buffer = [];
        byte[] singleByte = new byte[1];

        while (await _stream.ReadAsync(singleByte.AsMemory(0, 1), cancellationToken).ConfigureAwait(false) > 0)
        {
            buffer.Add(singleByte[0]);

            if (singleByte[0] == MarcConstants.EndOfRecord)
            {
                // Remove illegal characters that sometimes occur between records.
                while (buffer.Count > 0 && (buffer[0] == 0x0A || buffer[0] == 0x0D || buffer[0] == 0x00))
                {
                    buffer.RemoveAt(0);
                }

                return buffer.ToArray();
            }

            if (buffer.Count > MarcConstants.MaxRecordLength)
            {
                throw new MarcInvalidLengthException($"Record exceeds maximum length of {MarcConstants.MaxRecordLength} bytes");
            }
        }

        return buffer.Count > 0 ? buffer.ToArray() : null;
    }

    private static Record DecodeRecord(ReadOnlySpan<byte> rawData, Encoding encoding)
    {
        Record record = new();
        int recordLength = rawData.Length;

        // Parse record length from leader
        if (rawData.Length >= 5)
        {
            string lengthStr = Encoding.ASCII.GetString(rawData[0..5]);
            if (int.TryParse(lengthStr, out int declaredLength))
            {
                if (declaredLength != recordLength)
                {
                    record.AddWarning($"Invalid record length: Leader says {declaredLength} bytes; actual record length is {recordLength}");
                }
            }
            else
            {
                record.AddWarning($"Record length '{lengthStr}' is not numeric");
            }
        }

        // Validate terminator
        if (rawData[^1] != MarcConstants.EndOfRecord)
        {
            throw new MarcInvalidTerminatorException("Invalid record terminator");
        }

        // Extract leader
        if (rawData.Length < MarcConstants.LeaderLength)
        {
            throw new MarcInvalidLengthException($"Record too short to contain a valid leader");
        }

        string leader = Encoding.ASCII.GetString(rawData[0..MarcConstants.LeaderLength]);
        record.SetLeaderFromString(leader);

        // Extract base address from leader
        string baseAddressStr = Encoding.ASCII.GetString(rawData[12..17]);
        if (!int.TryParse(baseAddressStr, out int baseAddress) || baseAddress < MarcConstants.LeaderLength)
        {
            throw new MarcInvalidDirectoryException("Invalid base address in leader");
        }

        // Extract directory
        int directoryLength = baseAddress - MarcConstants.LeaderLength - 1;
        if (directoryLength < 0 || MarcConstants.LeaderLength + directoryLength >= rawData.Length)
        {
            throw new MarcInvalidDirectoryException("Invalid directory length");
        }

        if (directoryLength % MarcConstants.DirectoryEntryLength != 0)
        {
            throw new MarcInvalidDirectoryException("Invalid directory length");
        }

        // Parse directory entries and extract fields
        for (int i = 0; i < directoryLength; i += MarcConstants.DirectoryEntryLength)
        {
            int directoryStart = MarcConstants.LeaderLength + i;

            // Directory entries are ASCII
            string tag = Encoding.ASCII.GetString(rawData.Slice(directoryStart, 3));
            string lengthStr = Encoding.ASCII.GetString(rawData.Slice(directoryStart + 3, 4));
            string offsetStr = Encoding.ASCII.GetString(rawData.Slice(directoryStart + 7, 5));

            if (!int.TryParse(lengthStr, out int fieldLength))
            {
                throw new MarcInvalidDirectoryException($"Invalid length in directory for tag {tag}");
            }

            if (!int.TryParse(offsetStr, out int fieldOffset))
            {
                throw new MarcInvalidDirectoryException($"Invalid offset in directory for tag {tag}");
            }

            int fieldStart = baseAddress + fieldOffset;
            int fieldEnd = fieldStart + fieldLength;

            if (fieldEnd > rawData.Length)
            {
                throw new MarcInvalidDirectoryException($"Directory entry for tag {tag} runs past the end of the record");
            }

            // Extract field data bytes (excluding end-of-field marker)
            ReadOnlySpan<byte> fieldBytes = rawData.Slice(fieldStart, fieldLength - 1);

            try
            {
                Field field = MarcHelper.IsControlField(tag)
                                  ? new ControlField(tag, encoding.GetString(fieldBytes))
                                  : ParseDataField(tag, fieldBytes, encoding);

                record.AppendField(field);
            }
            catch (Exception ex)
            {
                record.AddWarning($"Error parsing field {tag}: {ex.Message}");
            }
        }

        return record;
    }

    private static DataField ParseDataField(string tag, ReadOnlySpan<byte> fieldData, Encoding encoding)
    {
        if (fieldData.Length < 2)
        {
            throw new MarcInvalidLengthException($"Data field {tag} is too short to contain indicators");
        }

        char indicator1 = (char)fieldData[0];
        char indicator2 = (char)fieldData[1];

        DataField dataField = new(tag, indicator1, indicator2);

        // Parse subfields
        int start = 2;
        for (int i = start; i < fieldData.Length; i++)
        {
            if (fieldData[i] == (byte)MarcConstants.SubfieldIndicator)
            {
                // Process previous subfield, if any
                if (i > start && start > 2)
                {
                    char code = (char)fieldData[start];
                    ReadOnlySpan<byte> subfieldBytes = fieldData.Slice(start + 1, i - start - 1);
                    string data = encoding.GetString(subfieldBytes);
                    dataField.AppendSubfield(new Subfield(code, data));
                }
                start = i + 1;
            }
        }

        // Handle last subfield
        if (start < fieldData.Length && start > 2)
        {
            char code = (char)fieldData[start];
            ReadOnlySpan<byte> subfieldBytes = fieldData.Slice(start + 1, fieldData.Length - start - 1);
            string data = encoding.GetString(subfieldBytes);
            dataField.AppendSubfield(new Subfield(code, data));
        }

        return dataField;
    }

    /// <summary>
    /// Disposes resources used by the reader.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_stream != null && !_leaveOpen)
        {
            _stream.Dispose();
        }

        IsDisposed = true;
    }

    /// <summary>
    /// Asynchronously disposes resources used by the reader.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_stream != null && !_leaveOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        IsDisposed = true;
    }

    /// <summary>
    /// Reads a single MARC record from a string containing binary MARC data.
    /// </summary>
    /// <param name="marcData">String containing binary MARC data.</param>
    /// <returns>Parsed MARC record.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="marcData"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="MarcException">Thrown when no valid record is found in the string.</exception>
    public static Record FromString(string marcData)
    {
        ArgumentException.ThrowIfNullOrEmpty(marcData);

        byte[] bytes = Encoding.UTF8.GetBytes(marcData);
        using var stream = new MemoryStream(bytes);
        using var reader = new MarcReader(stream);

        return reader.ReadRecord() ?? throw new MarcInvalidRecordException("No valid record found in string");
    }

    /// <summary>
    /// Reads a single MARC record from a string containing binary MARC data with the specified encoding.
    /// </summary>
    /// <param name="marcData">String containing binary MARC data.</param>
    /// <param name="encoding">Encoding to use for interpreting the string.</param>
    /// <returns>Parsed MARC record.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="marcData"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is <see langword="null"/>.</exception>
    /// <exception cref="MarcException">Thrown when no valid record is found in the string.</exception>
    public static Record FromString(string marcData, Encoding encoding)
    {
        ArgumentException.ThrowIfNullOrEmpty(marcData);
        ArgumentNullException.ThrowIfNull(encoding);

        byte[] bytes = encoding.GetBytes(marcData);
        using var stream = new MemoryStream(bytes);
        using var reader = new MarcReader(stream);

        return reader.ReadRecord() ?? throw new MarcInvalidRecordException("No valid record found in string");
    }

    /// <summary>
    /// Reads a single MARC record from a byte array.
    /// </summary>
    /// <param name="bytes">Byte array containing binary MARC data.</param>
    /// <returns>Parsed MARC record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
    /// <exception cref="MarcException">Thrown when no valid record is found in the byte array.</exception>
    public static Record FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        using var stream = new MemoryStream(bytes);
        using var reader = new MarcReader(stream);

        return reader.ReadRecord() ?? throw new MarcInvalidRecordException("No valid record found in byte array");
    }
}
