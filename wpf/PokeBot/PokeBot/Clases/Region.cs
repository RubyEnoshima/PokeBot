using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static PokeBot.MainWindow;
using System.Diagnostics;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace PokeBot
{
    internal class Region
    {
        public string juego = "Oro";

        public string region = "Kanto/";
        public string ruta;

        string directorio = "json/";

        Dictionary<string,RegionData> rutas;

        public Region(ComboBox comboBox, string _ruta)
        {
            ruta = FormatearRuta(_ruta);
            directorio += region;
            CargarRutas(comboBox);
        }

        public ProbPokemon[] ObtenerPokemon(string horario, string zona, bool radioActiva = false, DateTime? fechaActual = null)
        {
            ruta = FormatearRuta(ruta);
            DateTime fecha = fechaActual ?? DateTime.Now;

            if (!rutas.ContainsKey(ruta) || rutas[ruta] == null)
                return null;

            foreach (Horario h in rutas[ruta].horarios)
            {
                if (h.nombre != horario)
                    continue;

                foreach (Zona z in h.zonas)
                {
                    if (z.nombre != zona)
                        continue;

                    return ConstruirListaPokemon(z, radioActiva, fecha);
                }
            }

            return null;
        }

        private ProbPokemon[] ConstruirListaPokemon(Zona zona, bool radioActiva, DateTime fecha)
        {
            List<ProbPokemon> normales = new List<ProbPokemon>();
            List<ProbPokemon> hoenn = new List<ProbPokemon>();
            List<ProbPokemon> sinnoh = new List<ProbPokemon>();

            foreach (ProbPokemon p in zona.pokemon)
            {
                if (!(p.aparicion == "" || p.aparicion == juego))
                    continue;

                string porcentaje = (p.porcentaje ?? "").Trim();
                if (string.IsNullOrWhiteSpace(porcentaje))
                    continue;

                if (EmpiezaPorNumero(porcentaje))
                {
                    double valor = ExtraerPrimerNumero(porcentaje);
                    if (valor > 0)
                        normales.Add(ClonarPokemon(p));
                }
                else if (porcentaje.StartsWith("Hoenn", StringComparison.OrdinalIgnoreCase))
                {
                    hoenn.Add(ClonarPokemon(p));
                }
                else if (porcentaje.StartsWith("Sinnoh", StringComparison.OrdinalIgnoreCase))
                {
                    sinnoh.Add(ClonarPokemon(p));
                }
            }

            bool activarHoenn = radioActiva && fecha.DayOfWeek == DayOfWeek.Wednesday && hoenn.Count > 0;
            bool activarSinnoh = radioActiva && fecha.DayOfWeek == DayOfWeek.Thursday && sinnoh.Count > 0;

            List<ProbPokemon> resultado = new List<ProbPokemon>();

            if (activarHoenn || activarSinnoh)
            {
                foreach (ProbPokemon p in normales)
                {
                    double valor = ExtraerPrimerNumero(p.porcentaje);
                    double nuevo = valor * 0.6;
                    if (nuevo > 0)
                    {
                        p.porcentaje = FormatearPorcentaje(nuevo);
                        resultado.Add(p);
                    }
                }

                if (activarHoenn)
                    resultado.AddRange(hoenn);

                if (activarSinnoh)
                    resultado.AddRange(sinnoh);
            }
            else
            {
                resultado.AddRange(normales);
            }

            return resultado.ToArray();
        }

        private bool EmpiezaPorNumero(string texto)
        {
            texto = (texto ?? "").Trim();
            return texto.Length > 0 && char.IsDigit(texto[0]);
        }

        private double ExtraerPrimerNumero(string texto)
        {
            Match match = Regex.Match(texto ?? "", @"\d+(?:[\.,]\d+)?");
            if (!match.Success)
                return 0;

            string valor = match.Value.Replace(',', '.');
            return double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double resultado)
                ? resultado
                : 0;
        }

        private string FormatearPorcentaje(double valor)
        {
            if (Math.Abs(valor % 1) < 0.0001)
                return ((int)Math.Round(valor)).ToString(CultureInfo.InvariantCulture) + "%";

            return valor.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        private ProbPokemon ClonarPokemon(ProbPokemon p)
        {
            return new ProbPokemon
            {
                id = p.id,
                porcentaje = p.porcentaje,
                aparicion = p.aparicion
            };
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
            return Regex.Split(input, "([0-9]+)");
        }
    }
}
