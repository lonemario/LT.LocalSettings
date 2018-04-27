using LT.Cripto;
using LT.LocalSettings.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

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
            get { return SettingsList == null ? false : true; }
        }

        private SettingsManager() { }

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
        /// Inizialize the setting manager class
        /// </summary>
        /// <param name="initVector"></param>
        /// <param name="passPhrase"></param>
        /// <param name="user"></param>
        /// <param name="app"></param>
        public static void Init(string initVector, string passPhrase, string user, string app)
        {
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
        /// REturn the settings count
        /// </summary>
        public static int Count
        {
            get
            {
                if (!IsInizialized)
                    throw new Exception(_ErrorMessage);

                return SettingsList.Count;
            }

        }

        /// <summary>
        /// Delete a setting
        /// </summary>
        /// <param name="setting"></param>
        public static void Delete(Setting setting)
        {
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
                SettingsList.Add(new Setting { Name = setting.Name, Value = setting.Value });
            }
            SaveFile();
        }

        private static void SaveFile()
        {
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
