using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace PokeBot
{
    public class CanvasDuplicator
    {
        public Canvas DuplicateCanvas(Canvas originalCanvas, string i, double offsetX = 0, double offsetY = 0)
        {
            // Create a new Canvas
            Canvas duplicatedCanvas = new Canvas();

            // Copy properties
            duplicatedCanvas.Background = originalCanvas.Background;
            duplicatedCanvas.Width = originalCanvas.Width;
            duplicatedCanvas.Height = originalCanvas.Height;

            // Copy children with offset
            foreach (UIElement child in originalCanvas.Children)
            {
                // Clone the child element
                UIElement clonedChild = CloneUIElement(child);

                if (clonedChild is FrameworkElement frameworkElement)
                {
                    // Modify the name if needed
                    frameworkElement.Name += i;
                }

                duplicatedCanvas.Children.Add(clonedChild);
            }

            duplicatedCanvas.Margin = new Thickness(originalCanvas.Margin.Left + offsetX, originalCanvas.Margin.Top + offsetY, originalCanvas.Margin.Right, originalCanvas.Margin.Bottom);


            return duplicatedCanvas;
        }


        private UIElement CloneUIElement(UIElement originalElement)
        {
            // Serialize the original element to XAML
            string xaml = System.Windows.Markup.XamlWriter.Save(originalElement);

            // Deserialize the XAML to create a deep copy
            UIElement clonedElement = System.Windows.Markup.XamlReader.Parse(xaml) as UIElement;

            return clonedElement;
        }
    }

}
