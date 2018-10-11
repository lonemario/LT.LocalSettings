using LT.Cripto;
using LT.LocalSettings.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LT.LocalSettings
{
    public sealed class SettingsManager
    {
        private static string homePath;
        private static string fileName;
        private static volatile SettingsManager instance;
        private static object syncRoot = new Object();
        private static CriptoHelper criptoHelper;
        private static string _passPhrase;
        private static string fullNameFile;
        private static List<Setting> _SettingsList;
        const string _ErrorMessage = "The SettingsManager Singleton class must be initialized before use. " + 
            "Use Init(string initVector, string passPhrase, string user, string app) method. It is sufficient to initialize the class only once in the application";

        private static bool _readOnly;
        private static List<Setting> _ExtraSettingsList;

        /// <summary>
        /// The list of all Settings
        /// </summary>
        public static List<Setting> SettingsList { get {
                if (_SettingsList==null)
                    throw new Exception(_ErrorMessage);
                return _SettingsList;
            }
        }

        public static bool IsInizialized {
            get { return _SettingsList == null ? false : true; }
        }

        public static string ConfigFilePath
        {
            get { return fullNameFile; }
        }

        private SettingsManager() {
            _readOnly = false;
        }
        /// <summary>
        /// REturn the settings count
        /// </summary>
        public static int Count
        {
            get
            {
                if (!IsInizialized)
                    throw new Exception(_ErrorMessage);
                if (_ExtraSettingsList != null)
                    return SettingsList.Count + _ExtraSettingsList.Count;

                return SettingsList.Count;
            }

        }

        /// <summary>
        /// [Deprecated] Use Init
        /// </summary>
        /// <param name="initVector"></param>
        /// <param name="passPhrase"></param>
        /// <param name="user"></param>
        /// <param name="app"></param>
        public static void Initialize(string initVector, string passPhrase, string user, string app)
        {
            Init(initVector, passPhrase, user, app);
        }

        /// <summary>
        /// Reinitialize the SettingsManager in read only mode and store the old settings in extra settings
        /// </summary>
        /// <param name="initVector"></param>
        /// <param name="passPhrase"></param>
        /// <param name="user"></param>
        /// <param name="app"></param>
        public static void Reinitialize(string initVector, string passPhrase, string user, string app)
        {
            _readOnly = true;
            if (_ExtraSettingsList == null)
                _ExtraSettingsList = new List<Setting>();
            if (_SettingsList != null)
                if (_SettingsList.Count > 0)
                    _ExtraSettingsList.AddRange(_SettingsList);

            Init(initVector, passPhrase, user, app, true);
        }

        /// <summary>
        /// Inizialize the setting manager class
        /// </summary>
        /// <param name="initVector"></param>
        /// <param name="passPhrase"></param>
        /// <param name="user"></param>
        /// <param name="app"></param>
        public static void Init(string initVector, string passPhrase, string user, string app, bool readOnly = false)
        {
            _readOnly = readOnly;
            //Initialize component
            homePath = String.Empty;
            fileName = String.Empty;
            fullNameFile = String.Empty;

            //Check arguments
            if (String.IsNullOrEmpty(initVector))
                throw new ArgumentNullException(nameof(initVector));
            if (String.IsNullOrEmpty(passPhrase))
                throw new ArgumentNullException(nameof(passPhrase));
            _passPhrase = passPhrase;
            if (String.IsNullOrEmpty(user))
                throw new ArgumentNullException(nameof(user));
            if (String.IsNullOrEmpty(app))
                throw new ArgumentNullException(nameof(app));

            fileName = $"{app}-{user}.lt".ToLower();
            //Retrive Home Path
            homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            if (String.IsNullOrEmpty(homePath))
                throw new Exception("Enviroment variabile HOME not found!");

            if (homePath == "%HOMEDRIVE%%HOMEPATH%")
                homePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //If it's possible create a directory, I create it, also save the file in the home folder
            var tmpPath = Path.Combine(homePath, ".ltsettings");

            if (!Directory.Exists(tmpPath))
            {
                try
                {
                    Directory.CreateDirectory(tmpPath);
                    homePath = tmpPath;
                }
                catch
                {
                    //User don't have grants to create a directory
                }
            }
            else
            {
                homePath = tmpPath;
            }
            fullNameFile = Path.Combine(homePath, fileName);

            criptoHelper = new CriptoHelper(initVector);

            //if exists read it, else create new empty list
            _SettingsList = File.Exists(fullNameFile)
                ? JsonConvert.DeserializeObject<List<Setting>>(criptoHelper.DecryptString(
                    File.ReadAllText(fullNameFile), _passPhrase))
                : new List<Setting>();
        }

        /// <summary>
        /// Delete a setting
        /// </summary>
        /// <param name="setting"></param>
        public static void Delete(Setting setting)
        {
            if (_readOnly)
                throw new Exception("Using reinitialize switch the SettingManager in read only mode");
            if (!IsInizialized)
                throw new Exception(_ErrorMessage);
            //Check arguments
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            var validationResults = Validate(setting);
            if (validationResults.Count > 0)
            {
                string Errors = String.Empty;
                foreach (var item in validationResults)
                {
                    Errors = Errors + item.ErrorMessage + Environment.NewLine;
                }
                throw new ArgumentException(Errors);
            }

            //Inserisci logica inserimento/modifica
            var res = Get(setting.Name);

            if (res == null)
                throw new Exception("Settig not found!");

            SettingsList.Remove(res);

            SaveFile();

        }

        /// <summary>
        /// Get A Setting
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        public static Setting Get(string settingName)
        {
            if (!IsInizialized)
                throw new Exception(_ErrorMessage);
            //Check arguments
            if (String.IsNullOrWhiteSpace(settingName))
                throw new ArgumentNullException(nameof(settingName));

            //If the setting is in the Main Settings Collection return it, else find in Extra Settings Collection
            var res = SettingsList.Where(s => s.Name == settingName).FirstOrDefault();
            if (res != null)
                return res;

            return SettingsList.Where(s => s.Name == settingName).FirstOrDefault();

        }

        /// <summary>
        /// Validate a setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static bool IsValid(Setting setting)
        {
            var context = new ValidationContext(setting, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            return Validator.TryValidateObject(setting, context, results);
        }

        /// <summary>
        /// Save a setting
        /// </summary>
        /// <param name="setting"></param>
        public static void Save(Setting setting)
        {
            if (_readOnly)
                throw new Exception("Using reinitialize switch the SettingManager in read only mode");
            if (!IsInizialized)
                throw new Exception(_ErrorMessage);
            //Check arguments
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            var validationResults = Validate(setting);
            if (validationResults.Count > 0)
            {
                string Errors = String.Empty;
                foreach (var item in validationResults)
                {
                    Errors = Errors + item.ErrorMessage + Environment.NewLine;
                }
                throw new ArgumentException(Errors);
            }

            //Inserisci logica inserimento/modifica
            var res = Get(setting.Name);

            if (res != null)
            {
                //EDIT
                res.Value = setting.Value;
            }
            else
            {
                //INSERT
                //res = new Setting { Name = setting.Name, Value = setting.Value };
                _SettingsList.Add(new Setting { Name = setting.Name, Value = setting.Value });
            }
            SaveFile();
        }

        private static void SaveFile()
        {
            if (_readOnly)
                throw new Exception("Using reinitialize switch the SettingManager in read only mode");
            if (!IsInizialized)
                throw new Exception(_ErrorMessage);

            File.WriteAllText(fullNameFile, criptoHelper.EncryptString(JsonConvert.SerializeObject(SettingsList), _passPhrase));
        }

        /// <summary>
        /// Validate the setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static IList<ValidationResult> Validate(Setting setting)
        {
            var context = new ValidationContext(setting, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(setting, context, results);

            return results;
        }
    }
}
