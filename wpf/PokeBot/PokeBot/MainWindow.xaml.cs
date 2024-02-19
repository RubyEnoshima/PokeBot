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
using static PokeBot.MainWindow;
using System.Xml.Linq;
using System.Security.RightsManagement;

namespace PokeBot
{
    public partial class MainWindow : Window
    {
        private const int MAX_POKE = 5;
        private const string archivo_json = "poke.json";
        private const string archivo_guardado = "save.json";
        private const string archivo_config = "config.json";
        private const string archivo_bot = "bot.json";
        private Thread thread;

        private string antPID = "";
        private List<Pokemon> pokemons = new List<Pokemon>();

        private Guardado actual = new Guardado();

        private Config config = new Config();

        private Dictionary<string,BitmapImage> imgs = new Dictionary<string, BitmapImage>();

        public MainWindow()
        {
            InitializeComponent();
            CargarImagenes();
            CargarConfig();
            Cargar();
            thread = new Thread(ActualizarDesdeArchivo);
            thread.Start();
            File.WriteAllText(archivo_json,string.Empty);
        }

        private void CargarImagenes()
        {
            string[] imagenes = { "play", "stop", "stop_big", "play_big" };
            foreach (string imagen in imagenes)
            {
                BitmapImage nuevaImagen = new BitmapImage();
                nuevaImagen.BeginInit();
                nuevaImagen.UriSource = new Uri("Imgs/"+imagen+".png", UriKind.RelativeOrAbsolute);
                nuevaImagen.EndInit();
                imgs.Add(imagen, nuevaImagen);
            }

        }

        private void ActualizarDesdeArchivo()
        {
            using (Mutex mutex = new Mutex(false, "MutexParaArchivo"))
            {
                while (true)
                {
                    try
                    {
                        mutex.WaitOne();
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
                    finally
                    {
                        // Liberar el bloqueo después de la lectura
                        mutex.ReleaseMutex();
                    }

                    Thread.Sleep(500);
                }

            }
        }

        private void AgregarPokemon(Pokemon pokemon)
        {
            if (pokemon.id == 0 || antPID==pokemon.pid) return;

            antPID = pokemon.pid;
            pokemons.Insert(0, pokemon);
            if (pokemons.Count > MAX_POKE) pokemons.RemoveAt(pokemons.Count - 1);
            actual.encuentros++;
            Encuentros.Content = actual.encuentros;
            if (pokemon.sv < actual.sv_minimo || actual.sv_minimo == -1)
            {
                actual.sv_minimo = pokemon.sv;
                SV_MIN.Content = actual.sv_minimo;
            }

            //File.AppendAllText("sv.txt",pokemon.sv.ToString()+"\n");
            //File.AppendAllText("sv_coma.txt",pokemon.sv.ToString()+",");
            //File.AppendAllText("pid.txt",pokemon.pid+" - "+ pokemon.sv.ToString() + "\n");

            for (int i = 0; i < pokemons.Count; i++)
            {
                ActualizarUI(pokemons[i],i);
            }
        }

        private void CambiarColorIV(int ivs, Label label)
        {
            if (ivs == 31) label.Foreground = Brushes.Gold;
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

            ((Label)Lista.FindName("SV" + i)).Content = pokemon.sv;

            // Sprite
            if (pokemon.id != 0)
            {
                string genero = "";
                string shiny = "";
                if (pokemon.genero == 1) genero = "female\\";
                if (pokemon.shiny == 1) shiny = "shiny\\";
                string ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + genero + $"{pokemon.id}.png";

                if (pokemon.genero == 1 && !File.Exists(ruta)) ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + $"{pokemon.id}.png";
                BitmapImage nuevaImagen;
                if (imgs.ContainsKey(ruta))
                {
                    nuevaImagen = imgs[ruta];
                }
                else
                {
                    nuevaImagen = new BitmapImage();
                    nuevaImagen.BeginInit();
                    nuevaImagen.UriSource = new Uri(ruta, UriKind.RelativeOrAbsolute);
                    nuevaImagen.EndInit();
                    imgs.Add(ruta,nuevaImagen);

                }
                ((Image)Lista.FindName("Sprite" + i)).Source = nuevaImagen;
            }
        }

        private void CambiarModo(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            actual.mode = comboBox.SelectedIndex + 1;
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Play");
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Stop");
            //StopImagen.Source = "stop_big.png";
        }

        private void ImagenGrande(object sender, MouseButtonEventArgs e)
        {
            Button b = sender as Button;
            string nombre = (b.Tag as string) + "_big";
            ((Image)b.FindName((b.Tag as string) + "Imagen")).Source = imgs[nombre];
        }

        private void ImagenPeque(object sender, MouseButtonEventArgs e)
        {
            Button b = sender as Button;
            string nombre = (b.Tag as string);
            ((Image)b.FindName(nombre + "Imagen")).Source = imgs[nombre];
        }

        private void ActualizarUISave()
        {
            Encuentros.Content = actual.encuentros;
            SV_MIN.Content = actual.sv_minimo;
            // falta mode
        }

        private void Cargar()
        {
            if(File.Exists(archivo_guardado))
            {
                string data = File.ReadAllText(archivo_guardado);
                Guardado guardado = JsonConvert.DeserializeObject<Guardado>(data);
                if(guardado != null)
                {
                    for (int i = 0; i < guardado.pokemon.Count(); i++)
                    {
                        //Pokemon pokemonData = JsonConvert.DeserializeObject<Pokemon>(guardado.pokemon[i]);
                        AgregarPokemon(guardado.pokemon[i]);
                    }
                    actual = guardado;

                    ActualizarUISave();
                }
            }
        }

        // Guarda el estado actual: el historial de pokes, los encuentros, el modo, el target...
        private void Guardar(object sender, RoutedEventArgs e)
        {
            if(config.guardar_al_salir)
            {
                Console.WriteLine("Guardando....");
                string pokemonsJSON = "{\"pokemon\":[";
                for (int i = 0; i < pokemons.Count; i++)
                {
                    pokemonsJSON += pokemons[i].toJSON();
                    if(i < pokemons.Count-1) pokemonsJSON += ",";
                }
                pokemonsJSON += "], "+actual.toString()+"}";
                File.WriteAllText(archivo_guardado, pokemonsJSON);
                Console.WriteLine("Guardado!");

            }
        }

        // Limpia el programa para empezar de 0
        private void Reset(object sender, RoutedEventArgs e)
        {
            pokemons.Clear();
            for (int i = 0; i < MAX_POKE; i++)
            {
                ActualizarUI(new Pokemon(), i);
            }
            actual.encuentros = 0;
            Encuentros.Content = actual.encuentros;
            actual.sv_minimo = -1;
            SV_MIN.Content = "-";
            File.WriteAllText(archivo_json, string.Empty);
        }

        private void Cerrar(object sender, CancelEventArgs e)
        {
            Guardar(sender, null);
            thread.Abort();
        }

        private void Salir(object sender, RoutedEventArgs e)
        {
            Cerrar(sender, null);
            Close();

        }

        private void SalirSinGuardar(object sender, RoutedEventArgs e)
        {
            config.guardar_al_salir = false;
            Close();
        }

        private void ActualizarUIConfig()
        {
            Guardar_salir.IsChecked = config.guardar_al_salir;
        }

        private void CargarConfig()
        {
            if (File.Exists(archivo_config))
            {
                string data = File.ReadAllText(archivo_config);
                Config config_nuevo = JsonConvert.DeserializeObject<Config>(data);
                if (config_nuevo != null)
                {
                    config = config_nuevo;
                    ActualizarUIConfig();
                }
            }
        }

        private void GuardarConfig()
        {
            File.WriteAllText(archivo_config,JsonConvert.SerializeObject(config, Formatting.Indented));
            ActualizarUIConfig();
        }

        private void GuardarSalirC(object sender, RoutedEventArgs e)
        {
            config.guardar_al_salir = true;
            GuardarConfig();
        }

        private void GuardarSalirU(object sender, RoutedEventArgs e)
        {
            config.guardar_al_salir = false;
            GuardarConfig();
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
            public int sv { get; set; } = 0;

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
            public Pokemon[] pokemon { get; set; }
            public int encuentros { get; set; } = 0;
            public int mode { get; set; } = 0;
            public int sv_minimo { get; set; } = -1;

            public string toString()
            {
                return "\"encuentros\":" + encuentros + ", \"mode\":" + mode + ", \"sv_minimo\":" + sv_minimo;
            }
        }

        public class Config
        {
            public bool guardar_al_salir { get; set; } = true;
        }

    }
}
