using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class SecureStringToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SecureString secureString)
            {
                return GetStringFromSecureString(secureString);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return CreateSecureStringFromString(str);
            }
            return null;
        }

        private static string GetStringFromSecureString(SecureString secureString)
        {
            if (secureString == null) return string.Empty;

            nint ptr = Marshal.SecureStringToBSTR(secureString);
            try
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.FreeBSTR(ptr);
            }
        }

        private static SecureString CreateSecureStringFromString(string str)
        {
            var secure = new SecureString();
            foreach (char c in str)
            {
                secure.AppendChar(c);
            }
            return secure;
        }



    }


    


}
