using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TestZ.Common
{

    /// <summary>
    ///      ------------------------------------------
    ///     │ 4 bytes: len  │ 1 byte: type | payload  |
    ///      ------------------------------------------
    ///       little endian 
    /// </summary>
    public static class Protocol
    {
        /// <summary>
        /// 64 mb
        /// </summary>
        public const int MaxPayloadBytes = 64 * 1024 * 1024;

        
        // ===========Write==========

        /// <summary>
        /// Writes one messages in stream. Should be called only in critical sections
        /// </summary>
        public static async Task WriteMessageAsync(NetworkStream stream, MessageType type,
            ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            var header = new byte[5];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
            header[4] = (byte)type;

            await stream.WriteAsync(header, ct);
            if (payload.Length > 0)
                await stream.WriteAsync(payload, ct);
        }

        public static Task WriteDtoAsync<T>(NetworkStream stream, MessageType type, T dto, CancellationToken ct = default)
            => WriteMessageAsync(stream, type, Serialize(dto), ct);

        public static byte[] Serialize<T>(T dto) => JsonSerializer.SerializeToUtf8Bytes(dto);

        // =================Read=================

        /// <summary>
        /// Read one message, blocks until get all bytes
        /// </summary>
        public static async Task<(MessageType Type, byte[] Payload)> ReadMessageAsync(
            NetworkStream stream, CancellationToken ct = default)
        {
            byte[] header = await ReadAsync(stream, 5, ct);
            int length = BinaryPrimitives.ReadInt32LittleEndian(header);
            var type = (MessageType)header[4];

            if (length < 0 || length > MaxPayloadBytes)
                throw new IOException($"Payload lenght is not correct ({length} bytes)");

            byte[] payload = length == 0
            ? Array.Empty<byte>()
            : await ReadAsync(stream, length, ct);

            return (type, payload);
        }

        public static T? ReadDto<T>(byte[] payload)
            => payload.Length == 0 ? default : JsonSerializer.Deserialize<T>(payload);

        /// <summary>
        /// Reading in cycle until get all message
        /// Read == 0 means remote side closed connections — throw IOException
        /// </summary>
        private static async Task<byte[]> ReadAsync(NetworkStream stream, int count, CancellationToken ct)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
                if (read == 0)
                    throw new IOException("Connection closed by remote");
                offset += read;
            }
            return buffer;
        }
    }

}
