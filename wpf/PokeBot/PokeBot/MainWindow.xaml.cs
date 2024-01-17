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
        private const int MAX_POKE = 5;
        private const string archivo_json = "poke.json";
        private const string archivo_guardado = "save.json";
        private Thread thread;

        private string antPID = "";
        private List<Pokemon> pokemons = new List<Pokemon>();
        private int encuentros = 0;
        private int mode = 0; // 0: parado, 1: salvaje, 2: legendario

        public MainWindow()
        {
            InitializeComponent();
            thread = new Thread(ActualizarDesdeArchivo);
            thread.Start();
            File.WriteAllText(archivo_json,string.Empty);
            Cargar();
        }

        private void Cerrar(object sender, CancelEventArgs e)
        {
            Guardar(sender, null);
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
                        if(data != "")
                        {
                            Pokemon pokemonData = JsonConvert.DeserializeObject<Pokemon>(data);

                            Dispatcher.Invoke(() => AgregarPokemon(pokemonData));

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer el archivo: {ex.Message}");
                }

                Thread.Sleep(1000);
            }
        }

        private void AgregarPokemon(Pokemon pokemon)
        {
            if (pokemon.id == 0 || antPID==pokemon.pid) return;

            antPID = pokemon.pid;
            pokemons.Insert(0, pokemon);
            if (pokemons.Count > MAX_POKE) pokemons.RemoveAt(pokemons.Count - 1);
            encuentros++;
            Encuentros.Content = encuentros;

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
            if (pokemon.id != 0)
            {
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
        }

        public class Pokemon
        {
            public int[] movimientos { get; set; }
            public string nombre { get; set; } = "";
            public string pid { get; set; } = "";
            public int habilidad { get; set; } = 0;
            public int shiny { get; set; } = 0;
            public IVs ivs { get; set; } = new IVs();
            public int nivel { get; set; } = 0;
            public int id { get; set; } = 0;
            public int genero { get; set; } = 0;

            public string toJSON()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
        }

        public class IVs
        {
            public int spdef { get; set; } = 0;
            public int def { get; set; } = 0;
            public int speed { get; set; } = 0;
            public int spatt { get; set; } = 0;
            public int att { get; set; } = 0;
            public int hp { get; set; } = 0;
        }

        public class Guardado
        {
            public Pokemon[] pokemon{ get; set; }
            public int encuentros { get; set; } = 0;
            public int mode { get; set; } = 0;
        }

        private void Cargar()
        {
            if(File.Exists(archivo_guardado))
            {
                string data = File.ReadAllText(archivo_guardado);
                Guardado guardado = JsonConvert.DeserializeObject<Guardado>(data);
                if(guardado != null)
                {
                    mode = guardado.mode;
                    for (int i = 0; i < guardado.pokemon.Count(); i++)
                    {
                        //Pokemon pokemonData = JsonConvert.DeserializeObject<Pokemon>(guardado.pokemon[i]);
                        AgregarPokemon(guardado.pokemon[i]);
                    }
                    encuentros = guardado.encuentros;
                    Encuentros.Content = encuentros;
                }
            }
        }

        // Guarda el estado actual: el historial de pokes, los encuentros, el modo, el target...
        private void Guardar(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Guardando....");
            string pokemonsJSON = "{\"pokemon\":[";
            for (int i = 0; i < pokemons.Count; i++)
            {
                pokemonsJSON += pokemons[i].toJSON();
                if(i < pokemons.Count-1) pokemonsJSON += ",";
            }
            pokemonsJSON += "], \"encuentros\":"+encuentros+", \"mode\":"+mode+"}";
            File.WriteAllText(archivo_guardado, pokemonsJSON);
            Console.WriteLine("Guardado!");
        }

        private void GuardarYSalir(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Guardando....");
            string pokemonsJSON = "{\"pokemon\":[";
            for (int i = 0; i < pokemons.Count; i++)
            {
                pokemonsJSON += pokemons[i].toJSON();
                if (i < pokemons.Count - 1) pokemonsJSON += ",";
            }
            pokemonsJSON += "], \"encuentros\":" + encuentros + ", \"mode\":" + mode + "}";
            File.WriteAllText(archivo_guardado, pokemonsJSON);
            Console.WriteLine("Guardado!");
            return;
        }

        // Limpia el programa para empezar de 0
        private void Reset(object sender, RoutedEventArgs e)
        {
            pokemons.Clear();
            for (int i = 0; i < MAX_POKE; i++)
            {
                ActualizarUI(new Pokemon(), i);
            }
            File.WriteAllText(archivo_json, string.Empty);
        }

        private void Salir(object sender, RoutedEventArgs e)
        {
            Cerrar(sender, null);
            Close();

        }
    }
}
