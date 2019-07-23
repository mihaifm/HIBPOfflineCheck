using System;
using System.IO;

namespace HIBPOfflineCheck
{
    public class BloomFilter
    {
        private BitStorage hashBits;

        public long Capacity { get; private set; }
        public float ErrorRate { get; private set; }
        public ulong BitCount { get; private set; }
        public uint NumHashFuncs { get; private set; }
        public int Algorithm { get; private set; }

        public BloomFilter(long capacity, float errorRate)
            : this(capacity, errorRate, bestM(capacity, errorRate), bestK(capacity, errorRate)) { }

        public BloomFilter(long capacity, float errorRate, ulong bitCount, uint numHashFuncs)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must be positive");
            if (errorRate >= 1 || errorRate <= 0)
                throw new ArgumentOutOfRangeException("errorRate", errorRate, "errorRate must be between 0 and 1");

            Capacity = capacity;
            ErrorRate = errorRate;
            BitCount = bitCount;
            NumHashFuncs = numHashFuncs;

            // different algorithms could be added in the future
            Algorithm = 0;

            hashBits = new BitStorage(bitCount);
        }

        public BloomFilter(string filename)
        {
            Load(filename);
        }

        public void Save(string filename)
        {
            using (Stream stream = new FileStream(filename, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(Capacity);
                bw.Write(ErrorRate);
                bw.Write(BitCount);
                bw.Write(NumHashFuncs);
                bw.Write(Algorithm);

                hashBits.Save(bw);
            }
        }

        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException();
            }

            using (Stream stream = new FileStream(filename, FileMode.Open))
            using (BinaryReader br = new BinaryReader(stream))
            {
                Capacity = br.ReadInt64();
                ErrorRate = br.ReadSingle();
                BitCount = (ulong)br.ReadInt64();
                NumHashFuncs = (uint) br.ReadInt32();
                Algorithm = br.ReadInt32();

                hashBits = new BitStorage(BitCount, false);

                hashBits.Load(br);
            }
        }

        public void Add(string item)
        {
            for (uint i = 0; i < NumHashFuncs / 2; i++)
            {
                byte[] bitem = HexToByte(item);
                ulong[] hash = ComputeHash(bitem, i);

                hashBits.SetValue(hash[0] % hashBits.Length, true);
                hashBits.SetValue(hash[1] % hashBits.Length, true);
            }
        }

        public bool Contains(string item)
        {
            for (uint i = 0; i < NumHashFuncs / 2; i++)
            {
                byte[] bitem = HexToByte(item);
                ulong[] hash = ComputeHash(bitem, i);

                if (hashBits.GetValue(hash[0] % hashBits.Length) == false)
                    return false;

                if (hashBits.GetValue(hash[1] % hashBits.Length) == false)
                    return false;
            }

            return true;
        }

        private static uint bestK(long capacity, float errorRate)
        {
            return (uint)Math.Round(Math.Log(2.0) * bestM(capacity, errorRate) / capacity);
        }

        private static ulong bestM(long capacity, float errorRate)
        {
            return (ulong)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        // Hex string to byte array - efficient conversion
        // https://stackoverflow.com/a/6274772

        public static byte[] HexToByte(string input)
        {
            var outputLength = input.Length / 2;
            var output = new byte[outputLength];
            int k = 0;

            for (int i = 0; i < input.Length; i += 2)
            {
                byte b = (byte)(LookupTable[input[i]] << 4 | LookupTable[input[i + 1]]);
                output[k++] = b;
            }

            return output;
        }

        private static readonly byte[] LookupTable = new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        // Murmur3 implementation
        // http://blog.teamleadnet.com/2012/08/murmurhash3-ultra-fast-hash-algorithm.html

        public ulong[] ComputeHash(byte[] bb, uint seed)
        {
            ulong READ_SIZE = 16;

            ulong length = 0L;
            ulong h1 = 0L;
            ulong h2 = 0L;

            h1 = seed;

            int pos = 0;
            ulong remaining = (ulong)bb.Length;

            // read 128 bits, 16 bytes, 2 longs in eacy cycle
            while (remaining >= READ_SIZE)
            {
                ulong k1 = GetUInt64(bb, pos);
                pos += 8;

                ulong k2 = GetUInt64(bb, pos);
                pos += 8;

                length += READ_SIZE;
                remaining -= READ_SIZE;

                h1 ^= MixKey1(k1);

                h1 = RotateLeft(h1, 27);
                h1 += h2;
                h1 = h1 * 5 + 0x52dce729;

                h2 ^= MixKey2(k2);

                h2 = RotateLeft(h2, 31);
                h2 += h1;
                h2 = h2 * 5 + 0x38495ab5;
            }

            // if the input contains more than 16 bytes
            if (remaining > 0)
            {
                ulong k1 = 0;
                ulong k2 = 0;
                length += remaining;

                // little endian (x86) processing
                switch (remaining)
                {
                    case 15:
                        k2 ^= (ulong)bb[pos + 14] << 48;
                        goto case 14;
                    case 14:
                        k2 ^= (ulong)bb[pos + 13] << 40;
                        goto case 13;
                    case 13:
                        k2 ^= (ulong)bb[pos + 12] << 32;
                        goto case 12;
                    case 12:
                        k2 ^= (ulong)bb[pos + 11] << 24;
                        goto case 11;
                    case 11:
                        k2 ^= (ulong)bb[pos + 10] << 16;
                        goto case 10;
                    case 10:
                        k2 ^= (ulong)bb[pos + 9] << 8;
                        goto case 9;
                    case 9:
                        k2 ^= (ulong)bb[pos + 8];
                        goto case 8;
                    case 8:
                        k1 ^= GetUInt64(bb, pos);
                        break;
                    case 7:
                        k1 ^= (ulong)bb[pos + 6] << 48;
                        goto case 6;
                    case 6:
                        k1 ^= (ulong)bb[pos + 5] << 40;
                        goto case 5;
                    case 5:
                        k1 ^= (ulong)bb[pos + 4] << 32;
                        goto case 4;
                    case 4:
                        k1 ^= (ulong)bb[pos + 3] << 24;
                        goto case 3;
                    case 3:
                        k1 ^= (ulong)bb[pos + 2] << 16;
                        goto case 2;
                    case 2:
                        k1 ^= (ulong)bb[pos + 1] << 8;
                        goto case 1;
                    case 1:
                        k1 ^= (ulong)bb[pos];
                        break;
                    default:
                        throw new Exception("Something went wrong with remaining bytes calculation.");
                }

                h1 ^= MixKey1(k1);
                h2 ^= MixKey2(k2);
            }

            h1 ^= length;
            h2 ^= length;

            h1 += h2;
            h2 += h1;

            h1 = MixFinal(h1);
            h2 = MixFinal(h2);

            h1 += h2;
            h2 += h1;

            return new[] { h1, h2 };
        }

        private static ulong MixKey1(ulong k1)
        {
            k1 *= 0x87c37b91114253d5L;
            k1 = RotateLeft(k1, 31);
            k1 *= 0x4cf5ad432745937fL;
            return k1;
        }

        private static ulong MixKey2(ulong k2)
        {
            k2 *= 0x4cf5ad432745937fL;
            k2 = RotateLeft(k2, 33);
            k2 *= 0x87c37b91114253d5L;
            return k2;
        }

        private static ulong MixFinal(ulong k)
        {
            // avalanche bits
            k ^= k >> 33;
            k *= 0xff51afd7ed558ccdL;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53L;
            k ^= k >> 33;
            return k;
        }

        public static ulong RotateLeft(ulong original, int bits)
        {
            return (original << bits) | (original >> (64 - bits));
        }

        private static ulong GetUInt64(byte[] bb, int pos)
        {
            return BitConverter.ToUInt64(bb, pos);
        }
    }
}