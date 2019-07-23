using System.Collections;
using System.IO;

namespace HIBPOfflineCheck
{
    class BitStorage
    {
        private BitArray[] master;
        private const int subArraySize = 64 * 1024 * 1024;

        public ulong Length { get; private set;  }

        public BitStorage(ulong length) : this(length, true) { }

        public BitStorage(ulong length, bool allocateMem)
        {
            Length = length;
            int remainder = (int)(length % subArraySize);

            int numberSubArrays;
            int lastArraySize;

            if (remainder == 0)
            {
                numberSubArrays = (int)(length / subArraySize);
                master = new BitArray[numberSubArrays];

                if (allocateMem)
                {
                    for (int i = 0; i < master.Length; ++i)
                        master[i] = new BitArray(subArraySize);
                }
            }
            else
            {
                numberSubArrays = (int)(length / subArraySize) + 1;

                //lastArraySize = remainder;
                lastArraySize = subArraySize;

                master = new BitArray[numberSubArrays];

                if (allocateMem)
                {
                    for (int i = 0; i < master.Length - 1; ++i)
                        master[i] = new BitArray(subArraySize);

                    master[master.Length - 1] = new BitArray(lastArraySize);
                }
            }
        }

        public bool GetValue(ulong index)
        {
            ulong row = index / subArraySize;
            int col = (int) (index % subArraySize);
            return master[row][col];
        }

        public void SetValue(ulong index, bool value)
        {
            ulong row = index / subArraySize;
            int col = (int) (index % subArraySize);
            master[row][col] = value;
        }

        public void Save(BinaryWriter binaryWriter)
        {
            for (int i = 0; i < master.Length; i++)
            {
                byte[] bytes = new byte[subArraySize / 8];
                master[i].CopyTo(bytes, 0);
                binaryWriter.Write(bytes);
            }
        }

        public void Load(BinaryReader binaryReader)
        {
            int chunkSize = subArraySize / 8;
            byte[] bytes = new byte[chunkSize];

            for (int i = 0; i < master.Length; i++)
            {
                binaryReader.Read(bytes, 0, chunkSize);
                master[i] = new BitArray(bytes);
            }
        }
    }
}
