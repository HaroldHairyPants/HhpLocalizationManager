using HhpLocalizationManager.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace HhpLocalizationManager.Common
{
    public static class LocalizationManager
    {
        private static ILocalizationLoader _localizationLoader;
        private static IEnumerable<Localization> _localizations;
        private static Localization _language;
        private static CultureInfo _culture;
        private static bool _unitLangWithCulture;
        private static bool _needReboot;


        /// <summary>
        /// Событие изменения локализации
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Список языков из папки с языками
        /// </summary>
        public static IEnumerable<CultureInfo> Localizations
        {
            get
            {
                return _localizations.Select(p => p.CultureInfo);
            }
        }

        /// <summary>
        /// Язык интерфейса приложения 
        /// (Не включает информацию о разделителях, форматах дат и числовых значениях)
        /// </summary>
        public static CultureInfo Language
        {
            get
            {
                return _language?.CultureInfo;
            }
            set
            {
                Localization currentLocal = GetLocalizationByCultureInfo(value);

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Name == _language?.CultureInfo?.Name)
                {
                    return;
                }

                if (!File.Exists(currentLocal?.FilePath))
                {
                    throw new FileNotFoundException(currentLocal?.FilePath);
                }

                //Создаём ResourceDictionary для новой культуры
                ResourceDictionary dict = new ResourceDictionary();
                dict.Source = new Uri(currentLocal?.FilePath, UriKind.Relative);

                //Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.StartsWith(_localizationLoader?.LocalizationPatch)
                                              select d).First();

                if (oldDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }
                _language = currentLocal;

                //Если указанр объединение языка интерфейса с культурой
                //Устанавливаем культуру
                if (_unitLangWithCulture)
                {
                    Culture = Language;
                }

                //Вызываем ивент для оповещения всех окон.
                LanguageChanged?.Invoke(Application.Current, new EventArgs());
            }
        }

        /// <summary>
        /// Культура приложения 
        /// (информация о разделителях, форматах дат и числовых значений).
        /// При установке культуры более одного раза, необходим перезапуск приложения
        /// </summary>
        public static CultureInfo Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Name == _culture?.Name)
                {
                    return;
                }

                //1. Меняем язык binding'ов потоков и элементов интерфейса
                XmlLanguage lang = XmlLanguage.GetLanguage(value.Name);
                lang.GetEquivalentCulture();
                lang.GetSpecificCulture();

                Type langType = typeof(XmlLanguage);
                BindingFlags accessFlags =
                    BindingFlags.ExactBinding | BindingFlags.SetField |
                    BindingFlags.Instance | BindingFlags.NonPublic;

                FieldInfo field;
                field = langType.GetField("_equivalentCulture", accessFlags);
                field.SetValue(lang, value);
                field = langType.GetField("_specificCulture", accessFlags);
                field.SetValue(lang, value);
                field = langType.GetField("_compatibleCulture", accessFlags);
                field.SetValue(lang, value);
                System.Threading.Thread.CurrentThread.CurrentCulture = value;
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;
                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;

                try
                {
                    FrameworkContentElement.LanguageProperty.OverrideMetadata(
                      typeof(System.Windows.Documents.TextElement),
                      new FrameworkPropertyMetadata(lang)
                    );

                    FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                        new FrameworkPropertyMetadata(lang));
                }
                catch (Exception ex)
                {
                    _needReboot = true;
                }

                _culture = value;
            }
        }

        /// <summary>
        /// Объединить язык интерфейса с культурой приложения
        /// </summary>
        public static bool UnitLangWithCulture
        {
            get
            {
                return _unitLangWithCulture;
            }
            set
            {
                if (value == _unitLangWithCulture) return;

                _unitLangWithCulture = value;
                if (Language != null && Language?.Name != Culture?.Name)
                {
                    Culture = Language;
                }
            }
        }

        /// <summary>
        /// Необходима для перезагрузки.
        /// При установке культуры более одного раза, необходим перезапуск приложения
        /// </summary>
        public static bool NeedReboot { get => _needReboot; }

        /// <summary>
        /// Получение локализации по культуре
        /// </summary>
        /// <param name="cultureInfo">Язык локализации</param>
        /// <returns>Возвращает локализацию с информацией о файле</returns>
        private static Localization GetLocalizationByCultureInfo(CultureInfo cultureInfo)
        {
            Localization currentLocal = _localizations.FirstOrDefault(p => p.CultureInfo == cultureInfo);
            if (currentLocal == null)
            {
                throw new CultureNotFoundException("Language", $"Localization {cultureInfo?.Name} is not included in the list of available languages.");
            }
            return currentLocal;
        }

        /// <summary>
        /// Инициализация локализации.
        /// Получает список языков используя загрузчик.
        /// </summary>
        /// <param name="localizationLoader">Загрузчик локализаций</param>
        public static void Startup(ILocalizationLoader localizationLoader)
        {
            _localizationLoader = localizationLoader;
            _localizations = localizationLoader.Load();

        }

        /// <summary>
        /// Инициализация локализации.
        /// Получает список языков используя загрузчик и устанавливает указанный язык.
        /// </summary>
        /// <param name="localizationLoader">Загрузчик локализаций</param>
        /// <param name="language">Язык интерфейса приложения
        /// (Не включает информацию о разделителях, форматах дат и числовых значениях)</param>
        /// <param name="culture">Культура приложения 
        /// (информация о разделителях, форматах дат и числовых значений)</param>
        public static void Startup(ILocalizationLoader localizationLoader, CultureInfo language, CultureInfo culture = null)
        {
            Startup(localizationLoader);
            Language = language;
            if (culture != null)
            {
                Culture = culture;
            }
        }

        /// <summary>
        /// Инициализация локализации.
        /// Получает список языков используя загрузчик и устанавливает указанный язык.
        /// </summary>
        /// <param name="localizationLoader">Загрузчик локализаций</param>
        /// <param name="language">Язык интерфейса приложения
        /// (Не включает информацию о разделителях, форматах дат и числовых значениях)</param>
        /// <param name="unitLangWithCulture">Объединить язык интерфейса с культурой приложения</param>
        public static void Startup(ILocalizationLoader localizationLoader, CultureInfo language, bool unitLangWithCulture)
        {
            _unitLangWithCulture = unitLangWithCulture;
            if (unitLangWithCulture)
            {
                Startup(localizationLoader, language, language);
            }
            else
            {
                Startup(localizationLoader, language);
            }
        }

        /// <summary>
        /// Инициализация локализации.
        /// Получает список языков используя загрузчик и устанавливает указанный язык.
        /// </summary>
        /// <param name="language">Язык интерфейса приложения
        /// (Не включает информацию о разделителях, форматах дат и числовых значениях)</param>
        /// <param name="unitLangWithCulture">Объединить язык интерфейса с культурой приложения</param>
        public static void Startup(CultureInfo language, bool unitLangWithCulture)
        {
            Startup(_localizationLoader, language, unitLangWithCulture);
        }

        /// <summary>
        /// Инициализация локализации.
        /// Получает список языков используя загрузчик и устанавливает указанный язык.
        /// </summary>
        /// <param name="language">Язык интерфейса приложения
        /// (Не включает информацию о разделителях, форматах дат и числовых значениях)</param>
        /// <param name="culture">Культура приложения 
        /// (информация о разделителях, форматах дат и числовых значений)</param>
        public static void Startup(CultureInfo language, CultureInfo culture = null)
        {
            Startup(_localizationLoader, language, culture);
        }
    }
}
