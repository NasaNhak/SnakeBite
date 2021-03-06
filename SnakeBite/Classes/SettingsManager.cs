﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SnakeBite
{
    
    public class SettingsManager
    { // originally static, now modified to support the needs of PreinstallManager.RefreshXml
        public string vanillaDatHash = "78219F6C88B062D5ADCBE6ACADC16E8DB6905D4A97F8A9750B6CC2B97B6BE218"; //expected original hash for 1.0.13.0

        public string directory;
        public string filename;
        public SettingsManager(string dir, string file = "snakebite.xml")
        {
            directory = dir;
            filename = file;
        }
        public bool DisableConflictCheck
        {
            get
            {
                return Properties.Settings.Default.DisableConflictCheck;
            }
            set
            {
                Properties.Settings.Default.DisableConflictCheck = value;
                Properties.Settings.Default.Save();
            }
        }

        public List<string> GetModFpkFiles()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            List<string> fpkList = new List<string>();
            foreach (ModEntry mod in settings.ModEntries)
            {
                foreach (ModFpkEntry fpkFile in mod.ModFpkEntries)
                {
                    fpkList.Add(Tools.ToQarPath(fpkFile.FilePath));
                }
            }
            return fpkList;
        }

        public List<string> GetModQarFiles(bool HideExtension = false)
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            List<string> qarList = new List<string>();
            foreach (ModEntry mod in settings.ModEntries)
            {
                foreach (ModQarEntry qarFile in mod.ModQarEntries)
                {
                    string fileName;
                    if (HideExtension)
                    {
                        fileName = Tools.ToQarPath(qarFile.FilePath.Substring(0, qarFile.FilePath.IndexOf(".")));
                    }
                    else
                    {
                        fileName = Tools.ToQarPath(qarFile.FilePath);
                    }
                    qarList.Add(fileName);
                }
            }
            return qarList;
        }

        public List<string> GetModExternalFiles() {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            List<string> fileList = new List<string>();
            foreach (ModEntry mod in settings.ModEntries) {
                foreach (ModFileEntry fpkFile in mod.ModFileEntries) {
                    fileList.Add(Tools.ToQarPath(fpkFile.FilePath));
                }
            }
            return fileList;
        }

        public bool SettingsExist()
        {
            return File.Exists(directory + "\\" + filename);
        }

        public void DeleteSettings()
        {
            File.Delete(directory + "\\" + filename);
        }

        public void AddMod(ModEntry Mod)
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);

            foreach (ModFpkEntry f in Mod.ModFpkEntries)
            {
                f.SourceType = FileSource.Mod;
                f.FpkFile = Tools.ToQarPath(f.FpkFile);
                f.FilePath = Tools.ToQarPath(f.FilePath);
            }

            foreach (ModQarEntry q in Mod.ModQarEntries)
            {
                q.SourceType = FileSource.Mod;
                q.FilePath = Tools.ToQarPath(q.FilePath);
            }

            settings.ModEntries.Add(Mod);
            settings.SaveTo(directory, filename);
        }

        public void RemoveMod(ModEntry Mod)
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            ModEntry remMod = settings.ModEntries.Find(entry => entry.Name == Mod.Name);
            settings.ModEntries.Remove(remMod);
            settings.SaveTo(directory, filename);
        }

        public List<ModEntry> GetInstalledMods()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            return settings.ModEntries;
        }

        public GameData GetGameData()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            return settings.GameData;
        }

        public void SetGameData(GameData NewGameData)
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            settings.GameData = NewGameData;
            settings.SaveTo(directory, filename);
        }

        public Version GetSettingsVersion()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            return settings.SbVersion.AsVersion();
        }

        public void UpdateDatHash()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
             
            // Hash 01.dat and update settings file
            string datHash = Tools.GetMd5Hash(ModManager.ZeroPath) + Tools.GetMd5Hash(ModManager.OnePath);
            settings.GameData.DatHash = datHash;
            Debug.LogLine(String.Format("[UpdateDatHash] Updated 00/01 dat hash to: {0}", datHash), Debug.LogLevel.All);
            settings.SaveTo(directory, filename);
        }
        public bool IsVanilla0001DatHash()
        {
            return vanillaDatHash.Equals(Tools.GetMd5Hash(ModManager.ZeroPath) + Tools.GetMd5Hash(ModManager.OnePath));
        }

        public void ClearAllMods()
        {
            Settings settings = new Settings();
            settings.LoadFrom(directory, filename);
            settings.ModEntries = new List<ModEntry>();
            settings.SaveTo(directory, filename);
        }

        internal bool ValidateDatHash()
        {
            string datHash = Tools.GetMd5Hash(ModManager.ZeroPath) + Tools.GetMd5Hash(ModManager.OnePath);
            string hashOld = this.GetGameData().DatHash;
            if (datHash != hashOld)
            {
                Debug.LogLine(String.Format("[ValidateDatHash] 00/01 dat hash mismatch:\n{0} (Found Hash) != {1} (Expected Hash)", datHash, hashOld), Debug.LogLevel.All);
                return false;
            }
            return true;
        }

        // Checks the saved InstallPath variable for the existence of MGSVTPP.exe
        public bool ValidInstallPath
        {
            get
            {
                string installPath = Properties.Settings.Default.InstallPath;
                if (Directory.Exists(installPath))
                {
                    if (File.Exists(String.Format("{0}\\MGSVTPP.exe", installPath)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    [XmlType("Settings")]
    public class Settings
    {
        [XmlElement("SbVersion")]
        public SerialVersion SbVersion { get; set; } = new SerialVersion();

        [XmlElement("MGSVersion")]
        public SerialVersion MGSVersion { get; set; } = new SerialVersion();

        [XmlElement("GameData")]
        public GameData GameData { get; set; } = new GameData();

        [XmlArray("Mods")]
        public List<ModEntry> ModEntries { get; set; } = new List<ModEntry>();

        public void SaveTo(string directory, string filename)
        {
            // Write settings to XML
            Directory.CreateDirectory(directory);
            using (FileStream s = new FileStream(Path.Combine(directory, filename), FileMode.Create))
            {
                XmlSerializer x = new XmlSerializer(typeof(Settings), new[] { typeof(Settings) });
                foreach (ModEntry mod in ModEntries)
                {
                    mod.Description = mod.Description.Replace("\r\n", "\n");
                }
                SbVersion.Version = ModManager.GetSBVersion().ToString();
                MGSVersion.Version = ModManager.GetMGSVersion().ToString();
                x.Serialize(s, this);
            }
        }

        public void LoadFrom(string directory, string filename)
        {
            // Load settings from XML

            if (!File.Exists(directory + "\\" + filename))
            {
                return;
            }

            using (FileStream s = new FileStream(Path.Combine(directory, filename), FileMode.Open))
            {
                XmlSerializer x = new XmlSerializer(typeof(Settings));
                Settings loaded = (Settings)x.Deserialize(s);
                GameData = loaded.GameData;
                ModEntries = loaded.ModEntries;
                SbVersion = loaded.SbVersion;
                foreach (ModEntry mod in ModEntries)
                {
                    mod.Description = mod.Description.Replace("\n", "\r\n");
                }
            }
            return;
        }
    }

    [XmlType("GameData")]
    public class GameData
    {
        public GameData()
        {
            GameQarEntries = new List<ModQarEntry>();
            GameFpkEntries = new List<ModFpkEntry>();
            GameFileEntries = new List<ModFileEntry>();
        }

        [XmlAttribute("DatHash")]
        public string DatHash { get; set; }

        [XmlArray("QarEntries")]
        public List<ModQarEntry> GameQarEntries { get; set; } = new List<ModQarEntry>();

        [XmlArray("FpkEntries")]
        public List<ModFpkEntry> GameFpkEntries { get; set; } = new List<ModFpkEntry>();

        [XmlArray("FileEntries")]
        public List<ModFileEntry> GameFileEntries { get; set; } = new List<ModFileEntry>();
    }

    [XmlType("ModEntry")]
    public class ModEntry
    {
        public ModEntry()
        {
        }

        public ModEntry(string SourceFile)
        {
            ReadFromFile(SourceFile);
        }

        [XmlAttribute("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute("Version")]
        public string Version { get; set; } = string.Empty;

        [XmlElement("MGSVersion")]
        public SerialVersion MGSVersion { get; set; } = new SerialVersion();

        [XmlElement("SBVersion")]
        public SerialVersion SBVersion { get; set; } = new SerialVersion();

        [XmlAttribute("Author")]
        public string Author { get; set; } = string.Empty;

        [XmlAttribute("Website")]
        public string Website { get; set; } = string.Empty;

        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;

        [XmlArray("QarEntries")]
        public List<ModQarEntry> ModQarEntries { get; set; } = new List<ModQarEntry>();

        [XmlArray("FpkEntries")]
        public List<ModFpkEntry> ModFpkEntries { get; set; } = new List<ModFpkEntry>();

        [XmlArray("FileEntries")]
        public List<ModFileEntry> ModFileEntries { get; set; } = new List<ModFileEntry>();

        public void ReadFromFile(string Filename)
        {
            // Read mod metadata from xml

            if (!File.Exists(Filename)) return;

            XmlSerializer x = new XmlSerializer(typeof(ModEntry));
            StreamReader s = new StreamReader(Filename);
            System.Xml.XmlReader xr = System.Xml.XmlReader.Create(s);

            ModEntry loaded = (ModEntry)x.Deserialize(xr);

            Name = loaded.Name;
            Version = loaded.Version;
            MGSVersion = loaded.MGSVersion;
            SBVersion = loaded.SBVersion;
            Author = loaded.Author;
            Website = loaded.Website;
            Description = loaded.Description;

            ModQarEntries = loaded.ModQarEntries;
            ModFpkEntries = loaded.ModFpkEntries;
            ModFileEntries = loaded.ModFileEntries;

            s.Close();
        }

        public void SaveToFile(string Filename)
        {
            // Write mod metadata to XML

            if (File.Exists(Filename)) File.Delete(Filename);

            XmlSerializer x = new XmlSerializer(typeof(ModEntry), new[] { typeof(ModEntry) });
            StreamWriter s = new StreamWriter(Filename);
            x.Serialize(s, this);
            s.Close();
        }
    }

    public enum FileSource
    {
        System,
        Merged,
        Mod
    }

    [XmlType("QarEntry")]
    public class ModQarEntry
    {
        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("Compressed")]
        public bool Compressed { get; set; }

        [XmlAttribute("ContentHash")]
        public string ContentHash { get; set; }

        [XmlAttribute("SourceType")]
        public FileSource SourceType { get; set; }

        [XmlAttribute("SourceName")]
        public string SourceName { get; set; }
    }

    [XmlType("FpkEntry")]
    public class ModFpkEntry
    {
        [XmlAttribute("FpkFile")]
        public string FpkFile { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("ContentHash")]
        public string ContentHash { get; set; }

        [XmlAttribute("SourceType")]
        public FileSource SourceType { get; set; }

        [XmlAttribute("SourceName")]
        public string SourceName { get; set; }
    }

    [XmlType("FileEntry")]
    public class ModFileEntry {
        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("ContentHash")]
        public string ContentHash { get; set; }
    }
}