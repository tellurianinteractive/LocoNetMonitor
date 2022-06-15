using Microsoft.Extensions.Options;
using System.IO.Ports;

namespace Tellurian.Trains.LocoNetMonitor;
internal sealed class LocoNetInterface : IDisposable
{
    private readonly IOptions<AppSettings> _options;
    readonly ILogger<LocoNetInterface> _logger;
    private readonly SerialPort _locoNetPort;

    public LocoNetInterface(IOptions<AppSettings> options, ILogger<LocoNetInterface> logger)
    {
        _options = options;
        _logger = logger;
        _locoNetPort = new(Settings.LocoNet.Port, Settings.LocoNet.BaudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = Settings.LocoNet.ReadTimeout
        };
    }

    AppSettings Settings => _options.Value;

    public async ValueTask Write(byte[] data, CancellationToken stoppingToken)
    {
        if (data == null || data.Length == 0 || stoppingToken.IsCancellationRequested) return;
#if DEBUG
        _logger.LogDebug("To LocoNet: {message}", data.ToHex());
#endif
        try
        {
            if (!_locoNetPort.IsOpen) _locoNetPort.Open();
            _locoNetPort.Write(data, 0, data.Length);
            await Task.Delay(100, stoppingToken);

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
    }

    public async Task<byte[]> WaitForData(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            if (!_locoNetPort.IsOpen) _locoNetPort.Open();
            if (_locoNetPort.BytesToRead > 0)
            {
                try
                {
                    var data = new byte[_locoNetPort.BytesToRead];
                    var count = _locoNetPort.Read(data, 0, _locoNetPort.BytesToRead);
                    if (count > 0)
                    {
#if DEBUG
                        _logger.LogDebug("From LocoNet: {message}", data.ToHex());
#endif
                        return data;
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("LocoNet read timeout.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("{message}", ex.Message);
                    _locoNetPort.Close();
                    throw;
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        return Array.Empty<byte>();
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
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}