namespace Spacetime.Network;

/// <summary>
/// Defines methods for encoding and decoding network messages.
/// </summary>
public interface IMessageCodec
{
    /// <summary>
    /// Encodes a network message into a byte array suitable for transmission.
    /// </summary>
    /// <param name="message">The message to encode.</param>
    /// <returns>The encoded message bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    byte[] Encode(NetworkMessage message);

    /// <summary>
    /// Attempts to decode a network message from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The decoded message, or null if not enough data is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the message format is invalid.</exception>
    Task<NetworkMessage?> DecodeAsync(Stream stream, CancellationToken cancellationToken = default);
}
