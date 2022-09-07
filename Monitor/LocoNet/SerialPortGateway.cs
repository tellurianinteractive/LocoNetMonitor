using System.IO.Ports;

namespace Tellurian.Trains.LocoNetMonitor.LocoNet;
internal sealed class SerialPortGateway : IDisposable, ISerialPortGateway
{
    private readonly IOptions<AppSettings> _options;
    readonly ILogger<SerialPortGateway> _logger;
    private readonly SerialPort _locoNetPort;
    private readonly object _writeLock = new();
    private readonly TextWriter _writer;

    public SerialPortGateway(IOptions<AppSettings> options, ILogger<SerialPortGateway> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetPort = new(Settings.LocoNet.Port, Settings.LocoNet.BaudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = Settings.LocoNet.ReadTimeout
        };
        _writer = new StreamWriter(new FileStream(Filename(Settings.LoggingPath), FileMode.CreateNew, FileAccess.Write));
    }

    AppSettings Settings => _options.Value;

    public async ValueTask Write(byte[] data, CancellationToken stoppingToken)
    {
        if (data == null || data.Length == 0 || stoppingToken.IsCancellationRequested) return;
        _logger.LogDebug("To LocoNet: {message}", data.ToHex());
        try
        {
            lock (_writeLock)
            {
                if (!_locoNetPort.IsOpen) _locoNetPort.Open();
                _locoNetPort.Write(data, 0, data.Length);

            }
            await Task.Delay(Settings.LocoNet.MinWriteInterval, stoppingToken);

        }
        catch (InvalidOperationException)
        {
            _logger.LogError("Serial port {port} is not opened.", Settings.LocoNet.Port);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Write operation to serial port {port} timed out.", Settings.LocoNet.Port);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            _logger.LogError("Write operation to serial port {port} failed. Reason {ex}", Settings.LocoNet.Port, ex.Message);
        }
    }

    public async Task<byte[]> WaitForData(CancellationToken cancellationToken)
    {
    Restart:
        bool isFirstRead = true;
        if (!cancellationToken.IsCancellationRequested)
        {
            if (!_locoNetPort.IsOpen) _locoNetPort.Open();
            if (_locoNetPort.BytesToRead > 0)
            {
                try
                {
                    var data = new byte[_locoNetPort.BytesToRead];
                    var count = _locoNetPort.Read(data, 0, _locoNetPort.BytesToRead);
                    if (count > 0) await WriteReadBytesToFile(_writer, data);
                    var opcodeIndex = 0;
                    if (isFirstRead)
                    {
                        opcodeIndex = data.IndexOfFirstOperationCode();
                        if (opcodeIndex >= 0) isFirstRead = false;
                        else return Array.Empty<byte>();
                    }
                    if (count > 0)
                    {
                        _logger.LogDebug("From LocoNet: {message}", data.ToHex());
                        return data.Skip(opcodeIndex).ToArray();
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("LocoNet read timeout.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("{message}", ex.Message);
                    goto Restart;
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        return Array.Empty<byte>();
    }

    private static string Filename(string directoryPath)
    {
        var dateTime = DateTime.Now.ToString("G").Replace(':', '_');
        return $"{directoryPath}{nameof(SerialPortGateway)} {dateTime}.txt";
    }

    private static async Task WriteReadBytesToFile(TextWriter writer, byte[] data)
    {
        if (data.Length > 0 )
        {
            await writer.WriteLineAsync(data.SelectMany(b => b.ToString("X2")).ToArray());
        }
    }

    #region Dispose
    private bool disposedValue;
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _locoNetPort.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
    #endregion
}