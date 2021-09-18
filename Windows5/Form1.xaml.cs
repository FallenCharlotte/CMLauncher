using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Windows5
{
    public class Form1 : Window
    {
        public Form1()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void button_Click(object sender, RoutedEventArgs e)
        {
            // Change button text when button is clicked.
            var button = (Button)sender;
            button.Content = "Hello, Avalonia!";
        }
    }
}