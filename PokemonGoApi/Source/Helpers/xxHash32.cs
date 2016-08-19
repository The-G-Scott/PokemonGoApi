using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PokemonGoApi.Source.Helpers
{
	public class xxHash32
	{
		public struct XXH32_State
		{
			public ulong total_len;
			public uint seed;
			public uint v1;
			public uint v2;
			public uint v3;
			public uint v4;
			public int memsize;
			public byte[] memory;
		};

		const uint PRIME32_1 = 2654435761U;
		const uint PRIME32_2 = 2246822519U;
		const uint PRIME32_3 = 3266489917U;
		const uint PRIME32_4 = 668265263U;
		const uint PRIME32_5 = 374761393U;

		protected XXH32_State _state;
		public xxHash32()
		{

		}

		public static uint CalculateHash(byte[] buf, uint seed = 0)
		{
			return CalculateHash(buf, -1, seed);
		}

		public static uint CalculateHash(byte[] buf, int len = -1, uint seed = 0)
		{
			uint h32;
			int index = 0;
			if (len == -1)
			{
				len = buf.Length;
			}


			if (len >= 16)
			{
				int limit = len - 16;
				uint v1 = seed + PRIME32_1 + PRIME32_2;
				uint v2 = seed + PRIME32_2;
				uint v3 = seed + 0;
				uint v4 = seed - PRIME32_1;

				do
				{
					v1 = CalcSubHash(v1, buf, index);
					index += 4;
					v2 = CalcSubHash(v2, buf, index);
					index += 4;
					v3 = CalcSubHash(v3, buf, index);
					index += 4;
					v4 = CalcSubHash(v4, buf, index);
					index += 4;
				} while (index <= limit);

				h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
			}
			else
			{
				h32 = seed + PRIME32_5;
			}

			h32 += (uint)len;

			while (index <= len - 4)
			{
				h32 += BitConverter.ToUInt32(buf, index) * PRIME32_3;
				h32 = RotateLeft(h32, 17) * PRIME32_4;
				index += 4;
			}

			while (index < len)
			{
				h32 += buf[index] * PRIME32_5;
				h32 = RotateLeft(h32, 11) * PRIME32_1;
				index++;
			}

			h32 ^= h32 >> 15;
			h32 *= PRIME32_2;
			h32 ^= h32 >> 13;
			h32 *= PRIME32_3;
			h32 ^= h32 >> 16;

			return h32;
		}

		public void Init(uint seed = 0)
		{
			_state.seed = seed;
			_state.v1 = seed + PRIME32_1 + PRIME32_2;
			_state.v2 = seed + PRIME32_2;
			_state.v3 = seed + 0;
			_state.v4 = seed - PRIME32_1;
			_state.total_len = 0;
			_state.memsize = 0;
			_state.memory = new byte[16];
		}

		public bool Update(byte[] input, int len)
		{
			int index = 0;

			_state.total_len += (uint)len;

			if (_state.memsize + len < 16) // 버퍼 + 입력길이가 16바이트 이하일경우 버퍼에 저장만 해둔다
			{
				Array.Copy(input, 0, _state.memory, _state.memsize, len);
				_state.memsize += len;

				return true;
			}

			if (_state.memsize > 0) // 이전데이터가 남아있을경우 먼저 처리한다.
			{
				Array.Copy(input, 0, _state.memory, _state.memsize, 16 - _state.memsize);

				_state.v1 = CalcSubHash(_state.v1, _state.memory, index);
				index += 4;
				_state.v2 = CalcSubHash(_state.v2, _state.memory, index);
				index += 4;
				_state.v3 = CalcSubHash(_state.v3, _state.memory, index);
				index += 4;
				_state.v4 = CalcSubHash(_state.v4, _state.memory, index);
				index += 4;

				index = 0;
				_state.memsize = 0;
			}

			if (index <= len - 16)
			{
				int limit = len - 16;
				uint v1 = _state.v1;
				uint v2 = _state.v2;
				uint v3 = _state.v3;
				uint v4 = _state.v4;

				do
				{
					v1 = CalcSubHash(v1, input, index);
					index += 4;
					v2 = CalcSubHash(v2, input, index);
					index += 4;
					v3 = CalcSubHash(v3, input, index);
					index += 4;
					v4 = CalcSubHash(v4, input, index);
					index += 4;
				} while (index <= limit);

				_state.v1 = v1;
				_state.v2 = v2;
				_state.v3 = v3;
				_state.v4 = v4;
			}

			if (index < len)
			{
				Array.Copy(input, index, _state.memory, 0, len - index);
				_state.memsize = len - index;
			}
			return true;
		}

		public uint Digest()
		{
			uint h32;
			int index = 0;
			if (_state.total_len >= 16)
			{
				h32 = RotateLeft(_state.v1, 1) + RotateLeft(_state.v2, 7) + RotateLeft(_state.v3, 12) + RotateLeft(_state.v4, 18);
			}
			else
			{
				h32 = _state.seed + PRIME32_5;
			}

			h32 += (UInt32)_state.total_len;

			while (index <= _state.memsize - 4)
			{
				h32 += BitConverter.ToUInt32(_state.memory, index) * PRIME32_3;
				h32 = RotateLeft(h32, 17) * PRIME32_4;
				index += 4;
			}

			while (index < _state.memsize)
			{
				h32 += _state.memory[index] * PRIME32_5;
				h32 = RotateLeft(h32, 11) * PRIME32_1;
				index++;
			}

			h32 ^= h32 >> 15;
			h32 *= PRIME32_2;
			h32 ^= h32 >> 13;
			h32 *= PRIME32_3;
			h32 ^= h32 >> 16;

			return h32;
		}
		private static uint CalcSubHash(uint value, byte[] buf, int index)
		{
			uint read_value = BitConverter.ToUInt32(buf, index);
			value += read_value * PRIME32_2;
			value = RotateLeft(value, 13);
			value *= PRIME32_1;
			return value;
		}

		private static uint RotateLeft(uint value, int count)
		{
			return (value << count) | (value >> (32 - count));
		}
	}
}