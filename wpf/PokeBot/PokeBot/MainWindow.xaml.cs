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
using System.Xml.Schema;

namespace PokeBot
{
    public partial class MainWindow : Window
    {
        private const int MAX_POKE = 5;

        private const string archivo_json = "poke.json";
        private const string archivo_guardado = "save.json";
        private const string archivo_config = "config.json";
        private const string archivo_bot = "bot.json";

        private int[] minimosIVS = new int[] { 31, 25, 11, 6, 1, 0 };
        private int[] minimosTotal = new int[] { 186, 145, 70, 29, 1, 0 };
        private int[] minimosSV = new int[] { 65536, 45001, 35001, 10001, 8, 0 };
        private Brush[] colores = new Brush[] { Colorette("#F5D236"), Brushes.YellowGreen, Colorette("#FFFFFF"), Colorette("#F75555"), Colorette("#8F1515"), Brushes.DarkOrchid};

        private Thread thread;

        private string antPID = "";
        private List<Pokemon> pokemons = new List<Pokemon>();

        private Guardado actual = new Guardado();

        private Config config = new Config();

        private Imagenes Imagenes = new Imagenes();
        private Pokedex Pokedex = new Pokedex();

        private Region region = new Region();

        private string horario = "Noche";
        private string zona = "Zonas Verdes";

        private Probabilidades Probabilidades = new Probabilidades();

        public MainWindow()
        {
            InitializeComponent();
            CargarImagenes();
            CargarConfig();
            Cargar();
            CargarInfoProbPokemon();
            thread = new Thread(ActualizarDesdeArchivo);
            thread.Start();
            File.WriteAllText(archivo_json,string.Empty);
        }

        private void CargarImagenes()
        {
            string[] imgs = { "play", "stop", "stop_big", "play_big" };
            foreach (string imagen in imgs)
            {
                Imagenes.Obtener("Imgs/"+imagen+".png");
            }

        }

        private void ActualizarDesdeArchivo()
        {
            using (Mutex mutex = new Mutex(false, "MutexParaArchivo"))
            {
                while (true)
                {
                    FileInfo file = new FileInfo(archivo_json);
                    try
                    {
                        mutex.WaitOne();
                        using (FileStream fs = new FileStream(archivo_json, FileMode.Open, FileAccess.Read))
                        { 
                            string data = File.ReadAllText(archivo_json);
                            if (data != "")
                            {
                                Pokemon pokemonData = JsonConvert.DeserializeObject<Pokemon>(data);

                                Dispatcher.Invoke(() => AgregarPokemon(pokemonData));

                            }
                            
                        }
                    }
                    catch (IOException ex)
                    {
                        //MessageBox.Show($"{ex.Message}");
                        Console.WriteLine(ex.Message);

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

        private void CambiarColor(int[] minimos, int val, Label label, bool reves = false)
        {
            if(reves) Array.Reverse(colores);

            if (val == minimos[0]) label.Foreground = colores[0];
            else
            {
                for (int i = 1; i < minimos.Length; i++)
                {
                    if (val >= minimos[i]) { label.Foreground = colores[i]; break; }
                }
            }

            if (reves) Array.Reverse(colores);

        }

        private void CambiarColorIV(int ivs, Label label){ CambiarColor(minimosIVS,ivs,label); }

        private void ActualizarUI(Pokemon pokemon, int i)
        {
            ((Label)Lista.FindName("Nombre" + i)).Content = pokemon.nombre;
            ((Label)Lista.FindName("Nivel" + i)).Content = pokemon.nivel;

            ((Label)Lista.FindName("HP" + i)).Content = pokemon.ivs.hp;
            CambiarColorIV(pokemon.ivs.hp, (Label)Lista.FindName("HP" + i));
            ((Label)Lista.FindName("ATTK" + i)).Content = pokemon.ivs.att;
            CambiarColorIV(pokemon.ivs.att, (Label)Lista.FindName("ATTK" + i));
            ((Label)Lista.FindName("DEF" + i)).Content = pokemon.ivs.def;
            CambiarColorIV(pokemon.ivs.def, (Label)Lista.FindName("DEF" + i));
            ((Label)Lista.FindName("SP_ATTK" + i)).Content = pokemon.ivs.spatt;
            CambiarColorIV(pokemon.ivs.spatt, (Label)Lista.FindName("SP_ATTK" + i));
            ((Label)Lista.FindName("SP_DEF" + i)).Content = pokemon.ivs.spdef;
            CambiarColorIV(pokemon.ivs.spdef, (Label)Lista.FindName("SP_DEF" + i));
            ((Label)Lista.FindName("SPEED" + i)).Content = pokemon.ivs.speed;
            CambiarColorIV(pokemon.ivs.speed, (Label)Lista.FindName("SPEED" + i));
            int total = pokemon.ivs.hp + pokemon.ivs.att + pokemon.ivs.def + pokemon.ivs.spatt + pokemon.ivs.spdef + pokemon.ivs.speed;
            Label l_total = (Label)Lista.FindName("TOTAL" + i);
            l_total.Content = total;
            CambiarColor(minimosTotal, total, l_total);

            Label l_SV = (Label)Lista.FindName("SV" + i);
            l_SV.Content = pokemon.sv;
            CambiarColor(minimosSV,pokemon.sv,l_SV,true);

            // Sprite
            if (pokemon.id != 0)
            {
                string genero = "";
                string shiny = "";
                if (pokemon.genero == 1) genero = "female\\";
                if (pokemon.shiny == 1) shiny = "shiny\\";
                string ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + genero + $"{pokemon.id}.png";

                if (pokemon.genero == 1 && !File.Exists(ruta)) ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + shiny + $"{pokemon.id}.png";
                
                ((Image)Lista.FindName("Sprite" + i)).Source = Imagenes.Obtener(ruta);
                ((Image)Lista.FindName("Sprite" + i)).Visibility = Visibility.Visible;
            }
            else ((Image)Lista.FindName("Sprite" + i)).Visibility = Visibility.Hidden;
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
        }

        private void CargarInfoProbPokemon()
        {
            ProbPokemon[] probPokemons = region.ObtenerPokemon(horario, zona);
            if(probPokemons != null)
            {
                string ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\shiny\\" + probPokemons[0].id + ".png";
                ((Image)Probs.FindName("SpriteProb")).Source = Imagenes.Obtener(ruta);
                ((Label)Probs.FindName("NombreProb")).Content = Pokedex.ObtenerPokemon(Int32.Parse(probPokemons[0].id)).name;
                ((Label)Probs.FindName("Probabilidad")).Content = probPokemons[0].porcentaje;

                //Probabilidades.AgregarPokemon(probPokemons[0].id);

                Canvas originalCanvas = ((Canvas)Probs.FindName("Canvas1")); // Your original Canvas
                for (int i = 1; i < probPokemons.Length; i++)
                {
                    
                    CanvasDuplicator duplicator = new CanvasDuplicator();
                    Canvas duplicatedCanvas = duplicator.DuplicateCanvas(originalCanvas, i.ToString(), 0, 45);
                    duplicatedCanvas.Name = "Canvas" + (i + 1);
                    Probs.Children.Add(duplicatedCanvas);

                    ruta = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\shiny\\" + probPokemons[i].id + ".png";
                    Image img = ((Image)duplicatedCanvas.Children[0]);
                    img.Source = Imagenes.Obtener(ruta);
                    ((Label)duplicatedCanvas.Children[1]).Content = Pokedex.ObtenerPokemon(Int32.Parse(probPokemons[i].id)).name;
                    ((Label)duplicatedCanvas.Children[2]).Content = probPokemons[i].porcentaje;

                    originalCanvas = duplicatedCanvas;
                }
            }
        }

        private void ImagenGrande(object sender, MouseButtonEventArgs e)
        {
            Button b = sender as Button;
            string nombre = (b.Tag as string);
            ((Image)b.FindName(nombre + "Imagen")).Source = Imagenes.Obtener("Imgs/" + nombre + "_big.png");
        }

        private void ImagenPeque(object sender, MouseButtonEventArgs e)
        {
            Button b = sender as Button;
            string nombre = (b.Tag as string);
            ((Image)b.FindName(nombre + "Imagen")).Source = Imagenes.Obtener("Imgs/" + nombre + ".png");
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
                    for (int i = guardado.pokemon.Count()-1; i >= 0; i--)
                    {
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
            //Reset_shiny.IsChecked = config.reset_shiny;
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

        private void ResetShinyC(object sender, RoutedEventArgs e)
        {
            config.reset_shiny = true;
            GuardarConfig();
        }
        private void ResetShinyU(object sender, RoutedEventArgs e)
        {
            config.reset_shiny = false;
            GuardarConfig();
        }

        public static SolidColorBrush Colorette(string hex)
        {
            // Convertir el código hexadecimal a un objeto Color
            Color color = (Color)ColorConverter.ConvertFromString(hex);

            // Crear un SolidColorBrush con el color obtenido
            SolidColorBrush brush = new SolidColorBrush(color);

            return brush;
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
            public bool reset_shiny { get; set; } = true;
        }

    }

}
