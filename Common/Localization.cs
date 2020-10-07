using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HhpLocalizationManager.Common
{
    public class Localization
    {
        private CultureInfo _cultureInfo;
        private string filePath;

        public CultureInfo CultureInfo { get => _cultureInfo; }
        public string FilePath { get => filePath; }

        public Localization(CultureInfo cultureInfo, string filePath)
        {
            _cultureInfo = cultureInfo;
            this.filePath = filePath;
        }

    }
}
