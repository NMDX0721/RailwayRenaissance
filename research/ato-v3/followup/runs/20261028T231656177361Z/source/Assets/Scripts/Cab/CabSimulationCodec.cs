using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RailwayRenaissance.Cab.Simulation
{
    // Exact scalar encoding avoids JSON rounding changing a resumed integration.
    public static class SimulationCodec
    {
        static FieldInfo[] Fields(Type type) => type.GetFields(BindingFlags.Public|BindingFlags.Instance).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();
        public static string Encode<T>(T data)
        {
            using(var stream=new MemoryStream())
            using(var writer=new BinaryWriter(stream))
            {
                writer.Write("NF5K1");writer.Write(typeof(T).FullName);var fields=Fields(typeof(T));writer.Write(fields.Length);
                foreach(var f in fields)
                {
                    writer.Write(f.Name);var type=f.FieldType;var value=f.GetValue(data);
                    if(type==typeof(double))writer.Write((double)value);
                    else if(type==typeof(bool))writer.Write((bool)value);
                    else if(type==typeof(int))writer.Write((int)value);
                    else if(type==typeof(long))writer.Write((long)value);
                    else throw new ArgumentException("Unsupported checkpoint scalar");
                }
                writer.Flush();return Convert.ToBase64String(stream.ToArray());
            }
        }
        public static T Decode<T>(string encoded) where T:new()
        {
            if(string.IsNullOrEmpty(encoded)||encoded.Length>1000000)throw new ArgumentException("Invalid checkpoint size");
            using(var stream=new MemoryStream(Convert.FromBase64String(encoded)))
            using(var reader=new BinaryReader(stream))
            {
                var fields=Fields(typeof(T));
                if(reader.ReadString()!="NF5K1"||reader.ReadString()!=typeof(T).FullName||reader.ReadInt32()!=fields.Length)throw new ArgumentException("Checkpoint schema mismatch");
                var result=new T();
                foreach(var f in fields)
                {
                    if(reader.ReadString()!=f.Name)throw new ArgumentException("Checkpoint field mismatch");
                    var type=f.FieldType;
                    if(type==typeof(double))f.SetValue(result,reader.ReadDouble());
                    else if(type==typeof(bool))f.SetValue(result,reader.ReadBoolean());
                    else if(type==typeof(int))f.SetValue(result,reader.ReadInt32());
                    else if(type==typeof(long))f.SetValue(result,reader.ReadInt64());
                    else throw new ArgumentException("Unsupported checkpoint scalar");
                }
                if(stream.Position!=stream.Length)throw new ArgumentException("Checkpoint trailing data");return result;
            }
        }
    }
}
