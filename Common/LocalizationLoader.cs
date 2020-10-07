using HhpLocalizationManager.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HhpLocalizationManager.Common
{
    public class LocalizationLoader : ILocalizationLoader
    {
        private string _localizationPatch;

        public string LocalizationPatch { get => _localizationPatch; }

        public LocalizationLoader(string localizationPatch)
        {
            if(!Directory.Exists(localizationPatch))
            {
                throw new ArgumentException("localizationPatch", $"Folder {localizationPatch} not exists!");
            }
            _localizationPatch = localizationPatch;
        }

        public IEnumerable<Localization> Load()
        {
            List<Localization> localizations = new List<Localization>();
            DirectoryInfo directoryInfo = new DirectoryInfo(LocalizationPatch);
            foreach (FileInfo file in directoryInfo.GetFiles("lang.*"))
            {
                localizations.Add(new Localization(CultureInfo.GetCultureInfo(file.Name.Replace("lang.", "").Replace(file.Extension, "")),_localizationPatch + "/" + file.Name));
            }
            if(!localizations.Any())
            {
                throw new ArgumentException("localizationPatch", $"Folder {_localizationPatch} does not contain localizations!");
            }
            return localizations;
        }
    }
}
