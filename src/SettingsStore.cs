using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace XieYiMiao
{
    public sealed class SettingsStore
    {
        private readonly string _folder;
        private readonly string _path;

        public SettingsStore()
            : this(null)
        {
        }

        internal SettingsStore(string overrideFolder)
        {
            _folder = string.IsNullOrWhiteSpace(overrideFolder)
                ? Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "歇一喵")
                : overrideFolder;
            _path = Path.Combine(_folder, "settings.json");
        }

        public string DataFolder
        {
            get { return _folder; }
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return AppSettings.CreateDefault();
                }

                using (FileStream stream = File.OpenRead(_path))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(AppSettings));
                    AppSettings settings = (AppSettings)serializer.ReadObject(stream);
                    settings.Normalize();
                    return settings;
                }
            }
            catch
            {
                TryBackupBrokenFile();
                return AppSettings.CreateDefault();
            }
        }

        public void Save(AppSettings settings)
        {
            settings.Normalize();
            Directory.CreateDirectory(_folder);
            string temp = _path + ".tmp";

            using (FileStream stream = new FileStream(
                temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(AppSettings));
                serializer.WriteObject(stream, settings);
                stream.Flush(true);
            }

            if (File.Exists(_path))
            {
                string backup = _path + ".bak";
                try
                {
                    File.Replace(temp, _path, backup, true);
                }
                catch
                {
                    File.Copy(temp, _path, true);
                    File.Delete(temp);
                }
            }
            else
            {
                File.Move(temp, _path);
            }
        }

        private void TryBackupBrokenFile()
        {
            try
            {
                if (File.Exists(_path))
                {
                    string backup = _path + ".broken-" +
                        DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    File.Copy(_path, backup, true);
                }
            }
            catch
            {
            }
        }
    }
}
