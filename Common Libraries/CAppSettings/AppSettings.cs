using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cybertron.CAppSettings
{
    public class AppSettings
    {
        internal readonly Dictionary<string, Profile> Profiles;
        internal readonly Settings Settings;
        private readonly string SettingsXMLPath = Path.Combine(AppContext.BaseDirectory, "settings.xml");
        private readonly string ProfilesXMLPath = Path.Combine(AppContext.BaseDirectory, "profiles.xml");
        private readonly bool IsLinux;
        public AppSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                IsLinux = true;
            }
            if (File.Exists(SettingsXMLPath))
            {
                Settings = new Settings();
                ImportSettingsXML();
                if (Settings.FFmpegPath is null)
                {
                    FindFFPath();
                }
            }
            else
            {
                Settings = new Settings();
                FindFFPath();
            }
            if (File.Exists(ProfilesXMLPath))
            {
                Profiles = new Dictionary<string, Profile>();
                ImportProfilesXML();
            }
            else
            {
                Profiles = new Dictionary<string, Profile>
                {
                    { "Test Profile", new Profile() { Name = "Test Profile", Arguments = "-map 0:v:0 -map 0:a:2", OutputExtension = ".mkv" } }
                };
            }
        }
        public void ImportProfilesXML(string text)
        {
            XDocument doc = XDocument.Parse(text);
            PropertyInfo[] properties = typeof(Profile).GetProperties();
            foreach (XElement element in doc.Root.Descendants("Profile"))
            {
                Profile profile = new() { Name = element.Attribute("Name").Value };
                foreach (PropertyInfo property in properties.Where(x => x.Name != "Name"))
                {
                    property.SetValue(profile, element.Element(property.Name).Value);
                }
                Profiles.Add(profile.Name, profile);
            }
        }
        public void ImportProfilesXML()
        {
            var text = File.ReadAllText(ProfilesXMLPath);
            ImportProfilesXML(text);
        }
        public void ImportSettingsXML(string text)
        {
            XDocument doc = XDocument.Parse(text);
            PropertyInfo[] properties = typeof(Settings).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object value;
                bool? boolean = doc.Root.Element(property.Name).Value.TryParseToBool(out string str);
                if (boolean is null)
                {
                    value = str;
                }
                else
                {
                    value = boolean;
                }
                property.SetValue(Settings, value);
            }
        }
        public void ImportSettingsXML()
        {
            var text = File.ReadAllText(SettingsXMLPath);
            ImportSettingsXML(text);
        }
        public string GetXElementString<T>()
        {
            if (typeof(T) == typeof(Profile))
            {
                return GetProfilesXElement().ToString();
            }
            else if (typeof(T) == typeof(Settings))
            {
                return GetSettingsXElement().ToString();
            }
            else throw new ArgumentException();
        }
        private XElement GetProfilesXElement()
        {
            XElement result = new("Profiles", new XAttribute("AppVersion", Assembly.GetExecutingAssembly().GetName().Version!.ToString()));
            PropertyInfo[] properties = typeof(Profile).GetProperties();
            foreach (var profileName in Profiles.Keys)
            {
                XElement profileElement = new("Profile", new XAttribute("Name", profileName));
                foreach (PropertyInfo property in properties.Where(x => x.Name != "Name"))
                {
                    profileElement.Add(new XElement(property.Name, Profiles[profileName].GetPropVal(property.Name)));
                }
                result.Add(profileElement);
            }
            return result;
        }
        private XElement GetSettingsXElement()
        {
            XElement result = new("Settings", new XAttribute("AppVersion", Assembly.GetExecutingAssembly().GetName().Version!.ToString()));
            PropertyInfo[] properties = typeof(Settings).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                result.Add(new XElement(property.Name, Settings.GetPropVal(property.Name)));
            }
            return result;
        }
        public void ExportProfilesXML()
        {
            GetProfilesXElement().Save(ProfilesXMLPath);
        }
        public void ExportSettingsXML()
        {
            GetSettingsXElement().Save(SettingsXMLPath);
        }
        public void Save()
        {
            ExportSettingsXML();
            ExportProfilesXML();
        }
        public void ReadProfiles(string dir)
        {
            DirectoryInfo dirInfo = new(dir);
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                string profileName = file.GetNameWithoutExtension();
                string args = "";
                string ext = "";
                string mo = "";
                string dr = "";
                //extract extension, datarate, arguments, and name of profile
                var lines = File.ReadAllLines(file.FullName);
                Trace.TraceInformation("name: " + profileName);
                foreach (string line in lines)
                {
                    if (line.StartsWith("mo="))
                    {
                        mo = line.Split('=')[1].Trim();
                    }
                    if (line.StartsWith("dr="))
                    {
                        if (line.Contains("$mo"))
                        {
                            if (mo == "")
                            {
                                Trace.TraceInformation("ERROR: " + line);
                                Trace.TraceInformation("ERROR: " + file.FullName);
                                return;
                            }
                            else
                            {
                                dr += mo;
                            }
                        }
                        else
                        {
                            dr = line.Split("=")[1].Trim();
                        }
                    }
                    if (line.StartsWith("ffmpeg"))
                    {
                        //
                        if (!line.Contains("$etn"))
                        {
                            Trace.TraceInformation("name: " + profileName);
                            Trace.TraceInformation("args: " + args);
                            Trace.TraceInformation("ext: " + ext);
                            Trace.TraceInformation("mo: " + mo);
                            Trace.TraceInformation("dr: " + dr);
                            Trace.TraceInformation("ERROR: LINE DOES NOT CONTAIN $etn");
                        }
                        Trace.TraceInformation("ffmpeg line: " + line);
                        //
                        if (line.Contains(@"$dr"))
                        {
                            if (dr == "")
                            {
                                Trace.TraceInformation("ERROR: " + line);
                                Trace.TraceInformation("ERROR: " + file.FullName);
                                return;
                            }
                            else
                            {
                                Regex reg = new("(?:ffmpeg.*\\$etn\\s+)(?<args1>(?:(?!\\s+\\$).)*).*(?<dr>\\$dr)(?<args2>(?:(?!\\s+\\$).)*).*(?<ext>\\..*)");
                                Match match = reg.Match(line);
                                args = $"{match.Groups["args1"].Value.Trim()} {dr} {match.Groups["args2"].Value.Trim()}";
                                ext = match.Groups["ext"].Value;
                            }
                        }
                        else
                        {
                            Regex reg = new("(?:ffmpeg.*\\$etn\\s+)(?<args>(?:(?!\\s+\\$).)*).*(?<ext>\\..*)");
                            Match match = reg.Match(line);
                            args = match.Groups["args"].Value.Trim();
                            ext = match.Groups["ext"].Value;
                        }
                    }
                }
                Trace.TraceInformation("args: " + args);
                Trace.TraceInformation("ext: " + ext);
                Trace.TraceInformation("mo: " + mo);
                Trace.TraceInformation("dr: " + dr);
                if (!string.IsNullOrEmpty(args) && !string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(profileName))
                {
                    Profiles.Add(profileName, new Profile() { Name = profileName, Arguments = args, OutputExtension = ext });
                }
                else
                {
                    Trace.TraceInformation("ERROR: Failed to parse file, " + file.FullName);
                }
            }
        }
        private void FindFFPath()
        {
            if (IsLinux)
            {
                string pathOne = @"/usr/bin/ffmpeg";
                if (File.Exists(pathOne))
                {
                    Settings.FFmpegPath = Path.GetDirectoryName(pathOne)!;
                }
            }
            else
            {
                string pathOne = @"C:\FFmpeg\ffmpeg.exe";
                if (File.Exists(pathOne))
                {
                    Settings.FFmpegPath = Path.GetDirectoryName(pathOne)!;
                }
            }
        }
    }
}