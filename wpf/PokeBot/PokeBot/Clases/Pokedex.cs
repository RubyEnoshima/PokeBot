using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PokeBot.MainWindow;


namespace PokeBot
{
    internal class Pokedex
    {
        private const string archivo_pokedex = "json/pokedex.json";
        private Entrada[] pokedex;

        public Pokedex() 
        {
            if (File.Exists(archivo_pokedex))
            {
                string data = File.ReadAllText(archivo_pokedex);
                pokedex = JsonConvert.DeserializeObject<Entrada[]>(data);
            }
        }

        public Entrada ObtenerPokemon(int id)
        {
            return pokedex[id];
        }
    }

    public class Entrada
    {
        public int id;
        public string name;
        public string male;
        public string female;
    }
}
