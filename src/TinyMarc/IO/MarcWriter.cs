using System.Text;

namespace TinyMarc;

/// <summary>
/// Writes MARC-21 records to files, streams, or strings in binary format.
/// </summary>
public sealed class MarcWriter : IDisposable, IAsyncDisposable
{
    private readonly Stream? _stream;
    private readonly bool _leaveOpen;
    private readonly Encoding _utf8Encoding;
    private readonly Encoding _marc8Encoding;

    /// <summary>
    /// Gets a value that indicates whether this writer has been disposed of.
    /// </summary>
    public bool IsDisposed { get; private set; }

    private MarcWriter()
    {
        _utf8Encoding = Encoding.UTF8;
        _marc8Encoding = new Marc8Encoding();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarcWriter"/> class for writing to a file.
    /// </summary>
    /// <param name="filePath">Path to the output file.</param>
    public MarcWriter(string filePath) : this()
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        _stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        _leaveOpen = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarcWriter"/> class for writing to a stream.
    /// </summary>
    /// <param name="stream">Output stream.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after disposal.</param>
    public MarcWriter(Stream stream, bool leaveOpen = false) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Writes a single MARC record to the output.
    /// </summary>
    /// <param name="record">MARC record to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this writer has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying stream of this writer is <see langword="null"/>.</exception>
    public void WriteRecord(Record record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_stream == null)
        {
            throw new InvalidOperationException("Writer has been disposed");
        }

        Encoding encoding = DetectEncoding(record);
        string marcData = record.ToMarc();
        byte[] bytes = encoding.GetBytes(marcData);
        _stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Asynchronously writes a single MARC record to the output.
    /// </summary>
    /// <param name="record">MARC record to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this writer has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying stream of this writer is <see langword="null"/>.</exception>
    public async Task WriteRecordAsync(Record record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_stream == null)
        {
            throw new InvalidOperationException("Writer has been disposed");
        }

        Encoding encoding = DetectEncoding(record);
        string marcData = record.ToMarc();
        byte[] bytes = encoding.GetBytes(marcData);
        await _stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a MARC record to a string in binary format.
    /// </summary>
    /// <param name="record">MARC record to write.</param>
    /// <returns>Binary MARC string representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <see langword="null"/>.</exception>
    public static string WriteToString(Record record)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var stream = new MemoryStream();
        using var writer = new MarcWriter(stream, leaveOpen: true);
        writer.WriteRecord(record);
        writer.Flush();

        Encoding encoding = writer.DetectEncoding(record);
        return encoding.GetString(stream.ToArray());
    }

    private Encoding DetectEncoding(Record record)
    {
        string leader = record.CalculateLeader();
        if (leader.Length < MarcConstants.LeaderLength)
        {
            return _utf8Encoding;
        }

        // LEADER[9] contains the character encoding scheme
        // ' ' (space, 0x20) = MARC-8
        // 'a' (0x61) = UCS/Unicode (UTF-8)
        char encodingChar = leader[9];

        if (encodingChar == ' ')
        {
            return _marc8Encoding;
        }

        return _utf8Encoding;
    }

    /// <summary>
    /// Writes multiple MARC records to the output.
    /// </summary>
    /// <param name="records">Collection of MARC records to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is <see langword="null"/> or contains a <see langword="null"/> element.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this writer has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying stream of this writer is <see langword="null"/>.</exception>
    public void WriteRecords(IEnumerable<Record> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        foreach (Record record in records)
        {
            WriteRecord(record);
        }
    }

    /// <summary>
    /// Asynchronously writes multiple MARC records to the output.
    /// </summary>
    /// <param name="records">Collection of MARC records to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is <see langword="null"/> or contains a <see langword="null"/> element.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this writer has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying stream of this writer is <see langword="null"/>.</exception>
    public async Task WriteRecordsAsync(IEnumerable<Record> records, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);

        foreach (Record record in records)
        {
            await WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously writes multiple MARC records to the output from an async enumerable.
    /// </summary>
    /// <param name="records">Async enumerable of MARC records to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is <see langword="null"/> or contains a <see langword="null"/> element.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this writer has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying stream of this writer is <see langword="null"/>.</exception>
    public async Task WriteRecordsAsync(IAsyncEnumerable<Record> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            await WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Flushes any buffered data to the underlying stream.
    /// </summary>
    public void Flush()
    {
        _stream?.Flush();
    }

    /// <summary>
    /// Asynchronously flushes any buffered data to the underlying stream.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_stream != null)
        {
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes resources used by the writer.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_stream != null && !_leaveOpen)
        {
            _stream.Flush();
            _stream.Dispose();
        }

        IsDisposed = true;
    }

    /// <summary>
    /// Asynchronously disposes resources used by the writer.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_stream != null && !_leaveOpen)
        {
            await _stream.FlushAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        IsDisposed = true;
    }
}
