using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.IO;

namespace GLTF
{
	public enum ChunkFormat : uint
	{
		JSON = 0x4e4f534a,
		BIN = 0x004e4942
	}

	/// <summary>
	/// Information containing parsed GLB Header
	/// </summary>
	public struct GLBHeader
	{
		public uint Version { get; set; }
		public uint FileLength { get; set; }
	}

	/// <summary>
	/// Infomration that contains parsed chunk
	/// </summary>
	public struct ChunkInfo
	{
		public long StartPosition;
		public uint Length;
		public ChunkFormat Type;
	}
	
	public class GLTFParser
	{
		public const uint HEADER_SIZE = 12;
		public const uint CHUNK_HEADER_SIZE = 8;
		public const uint MAGIC_NUMBER = 0x46546c67;

		public static GLTFRoot ParseJson(byte[] gltfBinary)
		{
			string gltfContent;

			if (gltfBinary.Length == 0) {
				throw new GLTFHeaderInvalidException("glTF file cannot be empty.");
			}

			// Check for binary format magic bytes
			if (IsGLB(gltfBinary))
			{
				gltfContent = ParseJsonChunk(gltfBinary);
			}
			else
			{
				gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary);
			}

			return ParseString(gltfContent);
		}

		// todo optimize: this can have a caching system where GLTFRoot stores data about JSON offset of a GLB
		public static void ExtractBinaryChunk(byte[] gltfBinary, int binaryChunkIndex, out byte[] glbBuffer)
		{
			GLBHeader header = ParseGLBHeader(gltfBinary);
			int chunkOffset = 12;   // sizeof(GLBHeader) + magic number
			int chunkLength = 0;
			for (int i = 0; i < binaryChunkIndex + 2; ++i)
			{
				chunkOffset += chunkLength;
				chunkLength = (int)GetChunkLength(gltfBinary, chunkOffset);
				chunkOffset += 8;   // to account for chunk length (4 bytes) and type (4 bytes)
			}

			// Load Binary Chunk
			if (chunkOffset + chunkLength <= header.FileLength)
			{
				uint chunkType = BitConverter.ToUInt32(gltfBinary, chunkOffset - 4);
				if (chunkType != (uint)ChunkFormat.BIN)
				{
					throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				}

				glbBuffer = new byte[chunkLength];
				System.Buffer.BlockCopy(gltfBinary, chunkOffset, glbBuffer, 0, chunkLength);
			}
			else
			{
				throw new GLTFHeaderInvalidException("File length does not match chunk header.");
			}
		}

		private static GLBHeader ParseGLBHeader(byte[] gltfBinary)
		{
			uint version = BitConverter.ToUInt32(gltfBinary, 4);
			uint length = BitConverter.ToUInt32(gltfBinary, 8);

			return new GLBHeader
			{
				Version = version,
				FileLength = length
			};
		}

		private static bool IsGLB(byte[] gltfBinary)
		{
			return BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67;
		}

		private static uint GetChunkLength(byte[] gltfBinary, int chunkOffset)
		{
			return BitConverter.ToUInt32(gltfBinary, chunkOffset);
		}

		private static string ParseJsonChunk(byte[] gltfBinary)
		{
			GLBHeader header = ParseGLBHeader(gltfBinary);
			if (header.Version != 2)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (header.FileLength != gltfBinary.Length)
			{
				throw new GLTFHeaderInvalidException("File length does not match header.");
			}

			uint chunkLength = GetChunkLength(gltfBinary, 12);
			var chunkType = BitConverter.ToUInt32(gltfBinary, 16);
			if (chunkType != (uint)ChunkFormat.JSON)
			{
				throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			}

			// Load JSON chunk
			return System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);
		}

		private static GLTFRoot ParseString(string gltfContent)
		{
			var stringReader = new StringReader(gltfContent);
			return GLTFRoot.Deserialize(stringReader);
		}

		public static void ParseJson(Stream stream, out GLTFRoot gltfRoot, long startPosition = 0)
		{
			stream.Position = startPosition;
			bool isGLB = IsGLB(stream);
			
			// Check for binary format magic bytes
			if (isGLB)
			{
				ParseJsonChunk(stream, startPosition);
			}
			else
			{
				stream.Position = startPosition;
			}

			gltfRoot = GLTFRoot.Deserialize(new StreamReader(stream));
			gltfRoot.IsGLB = isGLB;
		}
		
		// Moves stream position to binary chunk location
		public static ChunkInfo SeekToBinaryChunk(Stream stream, int binaryChunkIndex, long startPosition = 0)
		{
			stream.Position = startPosition + 4;	 // start after magic number chunk
			GLBHeader header = ParseGLBHeader(stream);
			uint chunkOffset = (uint)startPosition + 12;   // sizeof(GLBHeader) + magic number
			uint chunkLength = 0;
			for (int i = 0; i < binaryChunkIndex + 2; ++i)
			{
				chunkOffset += chunkLength;
				stream.Position = chunkOffset;
				chunkLength = GetUInt32(stream);
				chunkOffset += 8;   // to account for chunk length (4 bytes) and type (4 bytes)
			}

			// Load Binary Chunk
			if (chunkOffset + chunkLength - (uint)startPosition <= header.FileLength)
			{
				ChunkFormat chunkType = (ChunkFormat)GetUInt32(stream);
				if (chunkType != ChunkFormat.BIN)
				{
					throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
				}

				return new ChunkInfo
				{
					StartPosition = stream.Position - CHUNK_HEADER_SIZE,
					Length = chunkLength,
					Type = chunkType
				};
			}

			throw new GLTFHeaderInvalidException("File length does not match chunk header.");
		}

		public static GLBHeader ParseGLBHeader(Stream stream)
		{
			uint version = GetUInt32(stream);   // 4
			uint length = GetUInt32(stream); // 8

			return new GLBHeader
			{
				Version = version,
				FileLength = length
			};
		}

		public static bool IsGLB(Stream stream)
		{
			return GetUInt32(stream) == 0x46546c67;  // 0
		}

		public static ChunkInfo ParseChunkInfo(Stream stream)
		{
			ChunkInfo chunkInfo = new ChunkInfo
			{
				StartPosition = stream.Position
			};

			chunkInfo.Length = GetUInt32(stream);					// 12
			chunkInfo.Type = (ChunkFormat)GetUInt32(stream);		// 16
			return chunkInfo;
		}

		public static List<ChunkInfo> FindChunks(Stream stream, long startPosition = 0)
		{
			stream.Position = startPosition + 4;     // start after magic number bytes (4 bytes past)
			ParseGLBHeader(stream);
			List<ChunkInfo> allChunks = new List<ChunkInfo>();

			// we only need to search for top two chunks (the JSON and binary chunks are guarenteed to be the top two chunks)
			// other chunks can be in the file but we do not care about them
			for (int i = 0; i < 2; ++i)
			{
				if (stream.Position == stream.Length)
				{
					break;
				}

				ChunkInfo chunkInfo = ParseChunkInfo(stream);
				allChunks.Add(chunkInfo);
				stream.Position += chunkInfo.Length;
			}

			return allChunks;
		}

		private static void ParseJsonChunk(Stream stream, long startPosition)
		{
			GLBHeader header = ParseGLBHeader(stream);  // 4, 8
			if (header.Version != 2)
			{
				throw new GLTFHeaderInvalidException("Unsupported glTF version");
			};

			if (header.FileLength != (stream.Length - startPosition))
			{
				throw new GLTFHeaderInvalidException("File length does not match header.");
			}

			ChunkInfo chunkInfo = ParseChunkInfo(stream);
			if (chunkInfo.Type != ChunkFormat.JSON)
			{
				throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
			}
		}

		private static uint GetUInt32(Stream stream)
		{
			var uintSize = sizeof(uint);
			byte[] headerBuffer = new byte[uintSize];
			stream.Read(headerBuffer, 0, uintSize);
			return BitConverter.ToUInt32(headerBuffer, 0);
		}
	}
}
