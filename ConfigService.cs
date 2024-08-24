using System.Text.Json;

namespace ComiServ
{
    public class Configuration
    {
        public string LibraryRoot { get; set; }
        public string DatabaseFile { get; set; }
        public Configuration Copy()
            => MemberwiseClone() as Configuration
                //this really shouldn't be possible
                ?? throw new Exception("Failed to clone configuration");
    }
    public interface IConfigService
    {
        public Configuration Config { get; }
    }
    public class ConfigService : IConfigService
    {
        public Configuration _Config;
        //protect original
        public Configuration Config => _Config.Copy();
        public ConfigService(string filepath)
        {
            using var fileStream = File.OpenRead(filepath);
            _Config = JsonSerializer.Deserialize<Configuration>(fileStream)
                ?? throw new ArgumentException("Failed to parse config file");
        }
    }
}
