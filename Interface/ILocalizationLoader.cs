using HhpLocalizationManager.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HhpLocalizationManager.Interface
{
    public interface ILocalizationLoader
    {
        string LocalizationPatch { get ; }
        IEnumerable<Localization> Load();
    }
}
