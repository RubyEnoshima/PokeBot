using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PokeBot
{
    internal class Imagenes
    {
        private Dictionary<string, BitmapImage> imgs = new Dictionary<string, BitmapImage>();

        public BitmapImage Obtener(string ruta)
        {
            if (!imgs.ContainsKey(ruta))
            {
                AgregarImagen(ruta);
            }
            return imgs[ruta];
        }

        void AgregarImagen(string ruta)
        {
            BitmapImage nuevaImagen;
            nuevaImagen = new BitmapImage();
            nuevaImagen.BeginInit();
            nuevaImagen.UriSource = new Uri(ruta, UriKind.RelativeOrAbsolute);
            nuevaImagen.EndInit();
            imgs.Add(ruta, nuevaImagen);
        }
    }
}
