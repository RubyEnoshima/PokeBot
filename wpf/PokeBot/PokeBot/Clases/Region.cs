using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static PokeBot.MainWindow;
using System.Diagnostics;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Windows.Controls;

namespace PokeBot
{
    internal class Region
    {
        public string juego = "Oro";

        public string region = "Kanto/";
        public string ruta;

        string directorio = "json/";

        Dictionary<string,RegionData> rutas;

        public Region(ComboBox comboBox, string _ruta) {
            ruta = FormatearRuta(_ruta);
            directorio += region;
            CargarRutas(comboBox);
        }

        public ProbPokemon[] ObtenerPokemon(string horario, string zona)
        {
            ruta = FormatearRuta(ruta);

            if (rutas[ruta] != null)
            {
                foreach(Horario h in rutas[ruta].horarios)
                {
                    if(h.nombre == horario)
                    {
                        foreach(Zona z in h.zonas)
                        {
                            if (z.nombre == zona)
                            {
                                List<ProbPokemon> res = new List<ProbPokemon>();
                                foreach(ProbPokemon p in z.pokemon)
                                {
                                    if(p.aparicion == "" || p.aparicion == juego) res.Add(p);
                                }
                                return res.ToArray();
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void CambiarRuta(string _ruta)
        {
            ruta = FormatearRuta(_ruta);
        }

        string FormatearRuta(string _ruta)
        {
            _ruta = _ruta.ToLower();
            _ruta = _ruta.Replace(" ", "_");
            return _ruta;
        }

        void CargarRutas(ComboBox comboBox)
        {
            string[] archivos = Directory.GetFiles(directorio);
            Array.Sort(archivos, new AlphanumericComparer());

            rutas = new Dictionary<string, RegionData>();
            int i = 0;
            foreach(string archivo in archivos)
            {
                string data = File.ReadAllText(archivo);
                RegionData regionData = JsonConvert.DeserializeObject<RegionData>(data);
                regionData.orden = i;
                rutas.Add(FormatearRuta(regionData.nombre), regionData);

                comboBox.Items.Add(regionData.nombre);
                i++;
            }

            comboBox.SelectedIndex = rutas[ruta].orden;
        }
    }

    public class RegionData
    {
        public string nombre;
        public Horario[] horarios;
        public int orden;
    }

    public class Horario
    {
        public string nombre;
        public Zona[] zonas;
    }

    public class Zona
    {
        public string nombre;
        public ProbPokemon[] pokemon;
    }

    public class ProbPokemon
    {
        public string id;
        public string porcentaje;
        public string aparicion;
    }

    public class AlphanumericComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string[] xParts = SplitString(x);
            string[] yParts = SplitString(y);

            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                int result;
                if (int.TryParse(xParts[i], out int xNum) && int.TryParse(yParts[i], out int yNum))
                {
                    result = xNum.CompareTo(yNum);
                    if (result != 0)
                        return result;
                }
                else
                {
                    result = string.Compare(xParts[i], yParts[i], StringComparison.Ordinal);
                    if (result != 0)
                        return result;
                }
            }

            return xParts.Length.CompareTo(yParts.Length);
        }

        private string[] SplitString(string input)
        {
            return System.Text.RegularExpressions.Regex.Split(input, "([0-9]+)");
        }
    }
}
