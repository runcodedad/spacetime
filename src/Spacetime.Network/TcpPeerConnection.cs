using System.Net.Sockets;
using System.Net.Security;

namespace Spacetime.Network;

/// <summary>
/// Represents a TCP connection to a remote peer with optional TLS encryption.
/// </summary>
public sealed class TcpPeerConnection : IPeerConnection
{
    private readonly TcpClient _client;
    private readonly Stream _stream;
    private readonly IMessageCodec _codec;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public PeerInfo PeerInfo { get; }

    /// <inheritdoc/>
    public bool IsConnected => _client.Connected && !_disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpPeerConnection"/> class.
    /// </summary>
    /// <param name="client">The connected TCP client.</param>
    /// <param name="peerInfo">Information about the peer.</param>
    /// <param name="codec">The message codec to use.</param>
    /// <param name="useTls">Whether to use TLS encryption.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static async Task<TcpPeerConnection> CreateAsync(
        TcpClient client,
        PeerInfo peerInfo,
        IMessageCodec codec,
        bool useTls = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(peerInfo);
        ArgumentNullException.ThrowIfNull(codec);

        Stream stream = client.GetStream();

        if (useTls)
        {
            var sslStream = new SslStream(stream, false);
            try
            {
                // For now, accept any certificate (in production, implement proper certificate validation)
                await sslStream.AuthenticateAsClientAsync(
                    peerInfo.EndPoint.Address.ToString(),
                    null,
                    System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                    false).ConfigureAwait(false);
                stream = sslStream;
            }
            catch
            {
                await sslStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        return new TcpPeerConnection(client, stream, peerInfo, codec);
    }

    private TcpPeerConnection(TcpClient client, Stream stream, PeerInfo peerInfo, IMessageCodec codec)
    {
        _client = client;
        _stream = stream;
        PeerInfo = peerInfo;
        _codec = codec;
    }

    /// <inheritdoc/>
    public async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsConnected)
        {
            throw new InvalidOperationException("Connection is not active.");
        }

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = _codec.Encode(message);
            await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<NetworkMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Connection is not active.");
        }

        try
        {
            return await _codec.DecodeAsync(_stream, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Connection closed or network error
            return null;
        }
        catch (SocketException)
        {
            // Socket error
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _client.Close();
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors during close
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CloseAsync().ConfigureAwait(false);
        _sendLock.Dispose();
        _client.Dispose();
    }
}
