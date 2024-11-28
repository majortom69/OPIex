using System.Windows;

namespace ihatecs.Core
{
    internal class Extentions
    {

        public static readonly DependencyProperty Icon = 
            DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(Extentions), new PropertyMetadata(default(string)));

        public static void SetIcon(UIElement element, string value)
        {
            element.SetValue(Icon, value);
        }

        public static string GetIcon(UIElement element)
        {
            return (string)element.GetValue(Icon);
        }

    }
}
