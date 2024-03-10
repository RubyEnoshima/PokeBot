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

namespace PokeBot
{
    internal class Region
    {
        public string juego = "Oro";

        public string region = "Kanto/";
        public string ruta = "ruta_15.json";

        string directorio = "json/";

        RegionData regionData;


        public Region() {
            directorio += region + ruta;

            if (File.Exists(directorio))
            {
                string data = File.ReadAllText(directorio);
                regionData = JsonConvert.DeserializeObject<RegionData>(data);
            }
        }

        public ProbPokemon[] ObtenerPokemon(string horario, string zona)
        {
            if (regionData != null)
            {
                foreach(Horario h in regionData.horarios)
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
    }

    public class RegionData
    {
        public string nombre;
        public Horario[] horarios;
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
}
