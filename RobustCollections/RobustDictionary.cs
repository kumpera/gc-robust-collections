using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RobustCollections
{
    
    class ArrayBuilder {

        MemoryStream stream = new MemoryStream();

        public int Add(ReadOnlySpan<byte> data) {
            if(data.Length > 255)
                throw new ArgumentException("data item must be smaller than 256 bytes");
            int res = (int)stream.Position;
            stream.WriteByte((byte)data.Length);
            stream.Write(data);
            return res;
        }

        public ReadOnlyMemory<byte> Finish() {
            ArraySegment<byte> res;
            if(stream.TryGetBuffer(out res))
                return res;
            return stream.ToArray();
        }
    }

    public class RobustDictionary
    {
        //FIXME make sure we use prime values for the table size
        // int searchStart = (int)(hash * 2654435761L | 0x7FFFFFFF);

        const float FILL_FACTOR = 0.5f;
        const int MIN_SIZE = 37;
        readonly int[] hashtable; //1 based     

        readonly ReadOnlyMemory<byte> data;   

        static int Hash(string str) {
            int hash = str.GetHashCode();
            return hash & 0x7FFFFFFF;
        }

        public RobustDictionary(Dictionary<string, string> dictionary)
        {
            int table_size = Math.Max((int)(dictionary.Count / FILL_FACTOR), MIN_SIZE);

            hashtable = new int[table_size];
            var ab = new ArrayBuilder();
            Span<byte> tempBuffer = stackalloc byte[257];

            foreach(var kv in dictionary) {
                //XXX use a temporary buffer
                    
                int bytes = Encoding.UTF8.GetBytes(kv.Key, tempBuffer);
                int idx = 1 + ab.Add(tempBuffer.Slice(0, bytes));

                bytes = Encoding.UTF8.GetBytes(kv.Value, tempBuffer);
                ab.Add(tempBuffer.Slice(0, bytes));

                //now add to the hashtable
                int searchStart = Hash(kv.Key) % table_size;
                int i = searchStart;
                // Console.WriteLine($"k:{kv.Key} i:{i} size:{table_size}");
                do {
                    //found a hole
                    if(hashtable[i] == 0) {
                        // Console.WriteLine($"adding at {i} idx:{idx}");
                        hashtable[i] = idx;
                        ++i; //signal we advanced
                        break;
                    }
                    i = (i + 1) % table_size;
                } while(i != searchStart);

                if (i == searchStart)
                    throw new ArgumentException("For some reason we exausted the hashtable memory");
            }
            data = ab.Finish();
        }

        public bool TryGetValue(string key, out string value)
        {
            Span<byte> tempBuffer = stackalloc byte[257];
            int bytes = Encoding.UTF8.GetBytes(key, tempBuffer);
            if (bytes > 255)
                throw new ArgumentException("key must be smaller than 256 bytes");
            var keyBytes = tempBuffer.Slice(0, bytes);

            int table_size = hashtable.Length;
            int searchStart = Hash(key) % table_size;
            int i = searchStart;
            value = null;

            var d  = data.Span;
            do {
                //not found
                int idx = hashtable[i];

                if(idx == 0)
                    return false;

                int key_size = d[idx - 1];
                var slotKey = d.Slice(idx, key_size);
                //Found it
                if(keyBytes.SequenceEqual(slotKey)) {
                    var val = d.Slice(idx + key_size);
                    int size = val[0];
                    value = Encoding.UTF8.GetString(val.Slice(1, size));
                    return true;
                }
                i = (i + 1) % table_size;
            } while(i != searchStart);

            return false;
        }
    }
}
