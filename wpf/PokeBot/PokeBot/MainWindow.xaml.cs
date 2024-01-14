using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace PokeBot
{
    public partial class MainWindow : Window
    {
        private string archivo_json = "poke.json";
        private Thread thread;
        private string antPID = "";
        private List<Pokemon> pokemons = new List<Pokemon>();
        public MainWindow()
        {
            InitializeComponent();
            thread = new Thread(ActualizarDesdeArchivo);
            thread.Start();
        }

        private void Cerrar(object sender, CancelEventArgs e)
        {
            thread.Abort();
        }

        private void ActualizarDesdeArchivo()
        {
            while (true)
            {
                try
                {
                    if (File.Exists(archivo_json))
                    {
                        string data = File.ReadAllText(archivo_json);
                        Pokemon pokemonData = JsonConvert.DeserializeObject<Pokemon>(data);

                        Dispatcher.Invoke(() => AgregarPokemon(pokemonData));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer el archivo: {ex.Message}");
                }

                Thread.Sleep(2000);
            }
        }

        private void AgregarPokemon(Pokemon pokemon)
        {
            if (pokemon.id == 0 || antPID==pokemon.pid) return;

            antPID = pokemon.pid;
            pokemons.Insert(0, pokemon);
            if (pokemons.Count > 5) pokemons.RemoveAt(pokemons.Count - 1);

            for(int i = 0; i < pokemons.Count; i++)
            {
                ActualizarUI(pokemons[i],i);
            }
        }

        private void ActualizarUI(Pokemon pokemon, int i)
        {
            ((Label)Lista.FindName("Nombre" + i)).Content = pokemon.nombre;
            ((Label)Lista.FindName("Nivel" + i)).Content = pokemon.nivel;

            ((Label)Lista.FindName("HP" + i)).Content = pokemon.ivs.hp;
            ((Label)Lista.FindName("ATTK" + i)).Content = pokemon.ivs.att;
            ((Label)Lista.FindName("DEF" + i)).Content = pokemon.ivs.def;
            ((Label)Lista.FindName("SP_ATTK" + i)).Content = pokemon.ivs.spatt;
            ((Label)Lista.FindName("SP_DEF" + i)).Content = pokemon.ivs.spdef;
            ((Label)Lista.FindName("SPEED" + i)).Content = pokemon.ivs.speed;
            ((Label)Lista.FindName("TOTAL" + i)).Content = pokemon.ivs.hp + pokemon.ivs.att + pokemon.ivs.def + pokemon.ivs.spatt + pokemon.ivs.spdef + pokemon.ivs.speed;

            // Sprite
            string genero = "";
            string shiny = "";
            if (pokemon.genero == 1) genero = "female\\";
            if (pokemon.shiny == 1) shiny = "shiny\\";
            string ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + genero + $"{pokemon.id}.png";

            if (pokemon.genero == 1 && !File.Exists(ruta)) ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + $"{pokemon.id}.png";
            BitmapImage nuevaImagen = new BitmapImage();
            nuevaImagen.BeginInit();
            nuevaImagen.UriSource = new Uri(ruta, UriKind.RelativeOrAbsolute);
            nuevaImagen.EndInit();
            ((Image)Lista.FindName("Sprite" + i)).Source = nuevaImagen;
        }

        public class Pokemon
        {
            public int[] movimientos { get; set; }
            public string nombre { get; set; }
            public string pid { get; set; }
            public int habilidad { get; set; }
            public int shiny { get; set; }
            public IVs ivs { get; set; }
            public int nivel { get; set; }
            public int id { get; set; }
            public int genero { get; set; }
        }

        public class IVs
        {
            public int spdef { get; set; }
            public int def { get; set; }
            public int speed { get; set; }
            public int spatt { get; set; }
            public int att { get; set; }
            public int hp { get; set; }
        }

    }
}
