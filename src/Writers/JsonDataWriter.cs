using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Sharphound.Client;
using SharpHoundCommonLib.OutputTypes;

namespace Sharphound.Writers
{

    public class JsonDataWriter<T> : BaseWriter<T>
    {
        private JsonTextWriter _jsonWriter;
        private readonly IContext _context;
        private string _fileName;
        private JsonSerializerSettings _serializerSettings;

        private const int DataVersion = 6;


        public JsonDataWriter(IContext context, string dataType) : base(dataType)
        {
            _context = context;
            if (_context.Flags.NoOutput)
                NoOp = true;

            _serializerSettings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                },
                Formatting = PrettyPrint
            };
        }

        private Formatting PrettyPrint => _context.Flags.PrettyPrint ? Formatting.Indented : Formatting.None;


        protected override void CreateFile()
        {
            var filename = _context.ResolveFileName(DataType, "json", true);
            if (File.Exists(filename))
                throw new FileExistsException($"File {filename} already exists. This should never happen!");

            _fileName = filename;

            _jsonWriter = new JsonTextWriter(new StreamWriter(filename, false, new UTF8Encoding(false)));
            _jsonWriter.Formatting = PrettyPrint;
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName("data");
            _jsonWriter.WriteStartArray();
        }

        protected override async Task WriteData()
        {
            foreach (var item in Queue)
            {
                await _jsonWriter.WriteRawValueAsync(JsonConvert.SerializeObject(item, _serializerSettings));
            }
        }

        internal override async Task FlushWriter()
        {
            if (!FileCreated)
                return;
            
            if (Queue.Count > 0)
            {
                await WriteData();
            }
            
            var meta = new MetaTag
            {
                Count = Count,
                CollectionMethods = (long)_context.ResolvedCollectionMethods,
                DataType = DataType,
                Version = DataVersion,
                CollectorVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            };
            
            await _jsonWriter.FlushAsync();
            await _jsonWriter.WriteEndArrayAsync();
            await _jsonWriter.WritePropertyNameAsync("meta");
            await _jsonWriter.WriteRawValueAsync(JsonConvert.SerializeObject(meta, PrettyPrint));
            await _jsonWriter.FlushAsync();
            await _jsonWriter.CloseAsync();
        }

        internal string GetFilename()
        {
            return FileCreated ? _fileName : null;
        }
    }
}