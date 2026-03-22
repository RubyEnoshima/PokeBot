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
using System.Windows.Threading;

namespace PokeBot
{
    public partial class MainWindow : Window
    {
        private const int MAX_POKE = 5;

        private const string archivo_json = "poke.json";
        private const string archivo_guardado = "save.json";
        private const string archivo_config = "config.json";
        private const string archivo_bot = "bot.json";

        private bool cargado = false;

        private int[] minimosIVS = new int[] { 31, 25, 11, 6, 1, 0 };
        private int[] minimosTotal = new int[] { 186, 145, 70, 29, 1, 0 };
        private int[] minimosSV = new int[] { 65536, 45001, 35001, 10001, 8, 0 };
        private Brush[] colores = new Brush[] { Colorette("#F5D236"), Brushes.YellowGreen, Colorette("#FFFFFF"), Colorette("#F75555"), Colorette("#8F1515"), Brushes.DarkOrchid };

        private Thread thread;

        private string antPID = "";
        private List<Pokemon> pokemons = new List<Pokemon>();
        private Pokemon antPoke;

        private Guardado actual = new Guardado();

        private Config config = new Config();

        private Imagenes Imagenes = new Imagenes();
        private Pokedex Pokedex = new Pokedex();

        private Region region;

        private string horario = "";
        private string zona = "Zonas Verdes";
        private bool radioActiva = false;

        private Probabilidades Probabilidades;

        private DispatcherTimer timer;
        private bool cargandoSave = false;
        private DateTime ultimaFechaEvaluada = DateTime.MinValue;
        private Pokemon shinyPendiente = null;
        private bool cambioFasePendientePorShiny = false;

        private string horarioForzado = null; // null = automático
        private DayOfWeek? diaForzado = null; // null = automático

        public MainWindow()
        {
            InitializeComponent();
            CargarImagenes();

            Loaded += WindowLoaded;
        }
        void WindowLoaded(object sender, RoutedEventArgs e)
        {
            cargado = true;
            Cargar();
            CargarConfig();

            region = new Region(ComboRutas, actual.ruta);

            MirarHora();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerTick;

            timer.Start();

            RadioToggle.IsChecked = config.radio_activa;
            radioActiva = config.radio_activa;
            ActualizarUIRadio();

            thread = new Thread(ActualizarDesdeArchivo);
            thread.Start();
            File.WriteAllText(archivo_json, string.Empty);
        }

        private void CargarImagenes()
        {
            string[] imgs = { "play", "stop", "stop_big", "play_big" };
            foreach (string imagen in imgs)
            {
                Imagenes.Obtener("Imgs/" + imagen + ".png");
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

        private void TimerTick(object sender, EventArgs e)
        {
            bool haCambiadoHorario = MirarHora();
            bool haCambiadoDia = ultimaFechaEvaluada != DateTime.MinValue && ultimaFechaEvaluada.Date != DateTime.Now.Date;
            ultimaFechaEvaluada = DateTime.Now.Date;
            ActualizarUITiempoFase();

            if (haCambiadoHorario || haCambiadoDia)
            {
                IniciarNuevaFase(true, true);
            }
        }

        private bool MirarHora()
        {
            DateTime currentTime = DateTime.Now;
            string antHorario = horario;

            if (!string.IsNullOrEmpty(horarioForzado))
            {
                horario = horarioForzado;
            }
            else
            {
                if (currentTime.Hour >= 4 && currentTime.Hour <= 9) { horario = "Mañana"; }
                else if (currentTime.Hour >= 10 && currentTime.Hour <= 19) { horario = "Tarde"; }
                else { horario = "Noche"; }
            }

            ActualizarUIHorario();
            if (ultimaFechaEvaluada == DateTime.MinValue) ultimaFechaEvaluada = currentTime.Date;
            return horario != antHorario;
        }

        private DateTime ObtenerFechaActualEfectiva()
        {
            DateTime ahora = DateTime.Now;

            if (!diaForzado.HasValue)
                return ahora;

            int diferencia = ((int)diaForzado.Value - (int)ahora.DayOfWeek + 7) % 7;
            return ahora.Date.AddDays(diferencia).Add(ahora.TimeOfDay);
        }

        private void ActualizarTabla()
        {
            LimpiarProbUI();
            CargarInfoProbPokemon();
        }

        private void AgregarPokemon(Pokemon pokemon)
        {
            if (pokemon.id == 0 || antPID == pokemon.pid) return;

            ResolverShinyPendienteSiCorresponde(pokemon);

            antPID = pokemon.pid;
            antPoke = pokemon;
            pokemons.Insert(0, pokemon);
            if (pokemons.Count > MAX_POKE) pokemons.RemoveAt(pokemons.Count - 1);
            actual.encuentros++;
            actual.phase_encounters++;
            Encuentros.Content = actual.phase_encounters;

            if (pokemon.sv < actual.sv_minimo || actual.sv_minimo == -1)
            {
                actual.sv_minimo = pokemon.sv;
                SV_MIN.Content = actual.sv_minimo;
            }

            if (Probabilidades != null) Probabilidades.AgregarEncuentro(pokemon.id, pokemon);

            for (int i = 0; i < pokemons.Count; i++)
            {
                ActualizarUI(pokemons[i], i);
            }

            if (!cargandoSave && pokemon.shiny == 1)
            {
                shinyPendiente = pokemon;
                cambioFasePendientePorShiny = true;
            }
        }

        private void ResolverShinyPendienteSiCorresponde(Pokemon nuevoPokemon)
        {
            if (cargandoSave || !cambioFasePendientePorShiny || shinyPendiente == null) return;
            if (nuevoPokemon == null) return;
            if (nuevoPokemon.pid == shinyPendiente.pid) return;

            bool capturado = PreguntarResultadoShiny(shinyPendiente);

            if (Probabilidades != null)
            {
                if (capturado) Probabilidades.MarcarShinyObtenido(shinyPendiente.id);
                else Probabilidades.MarcarShinyEscapado(shinyPendiente.id);
            }

            shinyPendiente = null;
            cambioFasePendientePorShiny = false;

            if (config.reset_shiny)
            {
                IniciarNuevaFase(false, true);
            }
            else
            {
                Guardar();
            }
        }

        private bool PreguntarResultadoShiny(Pokemon pokemon)
        {
            string nombre = pokemon != null ? pokemon.nombre : "el shiny";
            MessageBoxResult resultado = MessageBox.Show(
                "El combate shiny ha terminado.\n\n¿Se ha capturado " + nombre + "?",
                "Resultado del shiny",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return resultado == MessageBoxResult.Yes;
        }

        private void CambiarColor(int[] minimos, int val, Label label, bool reves = false)
        {
            if (reves) Array.Reverse(colores);

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

        private void CambiarColorIV(int ivs, Label label) { CambiarColor(minimosIVS, ivs, label); }

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
            CambiarColor(minimosSV, pokemon.sv, l_SV, true);

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

        private void CambiarRuta(object sender, SelectionChangedEventArgs e)
        {
            if (region != null)
            {
                Console.WriteLine(region.ruta);
                ComboBox comboBox = sender as ComboBox;
                actual.ruta = comboBox.SelectedValue.ToString();
                region.CambiarRuta(actual.ruta);
                IniciarNuevaFase(true, true);

            }
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
            Probabilidades = new Probabilidades(Probs, region.ruta, zona);

            DateTime fechaActual = ObtenerFechaActualEfectiva();
            ProbPokemon[] probPokemons = region.ObtenerPokemon(horario, zona, radioActiva, fechaActual);

            Probs.Children.Clear();

            if (probPokemons != null && probPokemons.Length > 0)
            {
                Canvas canvasBase = (Canvas)Probs.FindName("Canvas1");
                if (canvasBase == null) return;

                canvasBase.Margin = new Thickness(0, 0, 0, 0);
                canvasBase.Tag = probPokemons[0].id;

                string sprite = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + probPokemons[0].id + ".png";
                ((Image)canvasBase.Children[0]).Source = Imagenes.Obtener(sprite);
                ((Label)canvasBase.Children[1]).Content = Pokedex.ObtenerPokemon(Int32.Parse(probPokemons[0].id)).name;
                ((Label)canvasBase.Children[2]).Content = probPokemons[0].porcentaje;

                Probabilidades.AgregarPokemon(Int32.Parse(probPokemons[0].id));
                Probs.Children.Add(canvasBase);

                for (int i = 1; i < probPokemons.Length; i++)
                {
                    CanvasDuplicator duplicator = new CanvasDuplicator();

                    Canvas duplicatedCanvas = duplicator.DuplicateCanvas(canvasBase, i.ToString(), 0, 0);
                    duplicatedCanvas.Name = "Canvas" + (i + 1);
                    duplicatedCanvas.Tag = probPokemons[i].id;

                    duplicatedCanvas.HorizontalAlignment = HorizontalAlignment.Left;
                    duplicatedCanvas.VerticalAlignment = VerticalAlignment.Top;
                    duplicatedCanvas.Margin = new Thickness(0, i * 40, 0, 0);

                    sprite = Environment.CurrentDirectory + "\\Resources\\Pokemon\\hgss\\" + probPokemons[i].id + ".png";
                    ((Image)duplicatedCanvas.Children[0]).Source = Imagenes.Obtener(sprite);
                    ((Label)duplicatedCanvas.Children[1]).Content = Pokedex.ObtenerPokemon(Int32.Parse(probPokemons[i].id)).name;
                    ((Label)duplicatedCanvas.Children[2]).Content = probPokemons[i].porcentaje;

                    Probabilidades.AgregarPokemon(Int32.Parse(probPokemons[i].id));
                    Probs.Children.Add(duplicatedCanvas);
                }

                Probabilidades.ActualizarUI();
            }
        }

        private void LimpiarProbUI()
        {
            if (Probs.Children.Count > 1) Probs.Children.RemoveRange(1, Probs.Children.Count - 1);
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
            Encuentros.Content = actual.phase_encounters;
            SV_MIN.Content = actual.sv_minimo;
            ActualizarUITiempoFase();
            // falta mode
        }

        private void Cargar()
        {
            if (File.Exists(archivo_guardado))
            {
                string data = File.ReadAllText(archivo_guardado);
                Guardado guardado = JsonConvert.DeserializeObject<Guardado>(data);
                if (guardado != null)
                {
                    actual = guardado;
                    if (actual.phase_start == DateTime.MinValue) actual.phase_start = DateTime.Now;

                    cargandoSave = true;
                    for (int i = guardado.pokemon.Count() - 1; i >= 0; i--)
                    {
                        AgregarPokemon(guardado.pokemon[i]);
                    }
                    cargandoSave = false;

                    actual = guardado;
                    ActualizarUISave();
                }
            }
        }

        void Guardar()
        {
            Console.WriteLine("Guardando....");
            string pokemonsJSON = "{\"pokemon\":[";
            for (int i = 0; i < pokemons.Count; i++)
            {
                pokemonsJSON += pokemons[i].toJSON();
                if (i < pokemons.Count - 1) pokemonsJSON += ",";
            }
            pokemonsJSON += "], " + actual.toString() + "}";
            File.WriteAllText(archivo_guardado, pokemonsJSON);
            if (Probabilidades != null) Probabilidades.Guardar();
            Console.WriteLine("Guardado!");
        }

        void Reset()
        {
            pokemons.Clear();
            for (int i = 0; i < MAX_POKE; i++)
            {
                ActualizarUI(new Pokemon(), i);
            }
            actual.encuentros = 0;
            actual.phase_encounters = 0;
            Encuentros.Content = actual.phase_encounters;
            actual.sv_minimo = -1;
            actual.phase_start = DateTime.Now;
            SV_MIN.Content = "-";
            ActualizarUITiempoFase();
            //File.WriteAllText(archivo_json, string.Empty);
            if (Probabilidades != null) Probabilidades.SiguienteFase();
        }

        // Guarda el estado actual: el historial de pokes, los encuentros, el modo, el target...
        private void Guardar(object sender, RoutedEventArgs e)
        {
            if (config.guardar_al_salir)
            {
                Guardar();
            }
        }

        // Limpia el programa para empezar de 0
        private void Reset(object sender, RoutedEventArgs e)
        {
            Reset();
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
            Reset_shiny.IsChecked = config.reset_shiny;
            RadioToggle.IsChecked = config.radio_activa;
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
            if (cargado) File.WriteAllText(archivo_config, JsonConvert.SerializeObject(config, Formatting.Indented));
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

        private void RadioToggle_Click(object sender, RoutedEventArgs e)
        {
            radioActiva = RadioToggle.IsChecked == true;
            config.radio_activa = radioActiva;
            GuardarConfig();
            ActualizarUIRadio();
            ActualizarTabla();
        }

private void IniciarNuevaFase(bool recargarProbabilidades, bool guardarProbabilidadesAnteriores)
{
    if (guardarProbabilidadesAnteriores && Probabilidades != null)
    {
        Probabilidades.Guardar();
    }

    actual.phase_encounters = 0;
    actual.sv_minimo = -1;
    actual.phase_start = DateTime.Now;

    Encuentros.Content = actual.phase_encounters;
    SV_MIN.Content = "-";
    ActualizarUITiempoFase();

    if (Probabilidades != null)
    {
        if (Probabilidades != null) Probabilidades.SiguienteFase();
    }

    Guardar();

    if (recargarProbabilidades)
    {
        ActualizarTabla();
    }
}

        private void ActualizarUITiempoFase()
        {
            DateTime inicio = actual.phase_start == DateTime.MinValue ? DateTime.Now : actual.phase_start;
            TimeSpan tiempo = DateTime.Now - inicio;
            if (tiempo < TimeSpan.Zero) tiempo = TimeSpan.Zero;

            PhaseTimer.Content = string.Format("{0} h {1:00} min", (int)tiempo.TotalHours, tiempo.Minutes);
        }

        private void ActualizarUIHorario()
        {
            string tooltip = "Horari: " + horario;
            TimeIcon.Source = Imagenes.Obtener("Imgs/play.png");
            TimeIcon.ToolTip = tooltip;
            TimeLabel.Content = horario;
        }

        private void ActualizarUIRadio()
        {
            RadioStatus.Content = radioActiva ? "ON" : "OFF";
        }

        private void BtnForzarFase_Click(object sender, RoutedEventArgs e)
        {
            IniciarNuevaFase(true, true);
        }

        private void CbHorario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cargado) return;

            ComboBoxItem item = CbHorario.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string nuevo = item.Tag != null ? item.Tag.ToString() : null;

            if (string.IsNullOrWhiteSpace(nuevo) || nuevo.ToLower() == "auto")
                horarioForzado = null;
            else
                horarioForzado = nuevo;

            string horarioAnterior = horario;

            MirarHora();

            if (horario != horarioAnterior)
            {
                IniciarNuevaFase(true, true);
            }
            else
            {
                ActualizarTabla();
            }
        }

        private void CbDia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cargado) return;

            ComboBoxItem item = CbDia.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string tag = item.Tag != null ? item.Tag.ToString() : null;

            if (string.IsNullOrWhiteSpace(tag) || tag.ToLower() == "auto")
            {
                diaForzado = null;
            }
            else if (tag == "Wednesday")
            {
                diaForzado = DayOfWeek.Wednesday;
            }
            else if (tag == "Thursday")
            {
                diaForzado = DayOfWeek.Thursday;
            }
            else
            {
                diaForzado = null;
            }

            ActualizarTabla();
        }

        public static SolidColorBrush Colorette(string hex)
        {
            // Convertir el código hexadecimal a un objeto Color
            Color color = (Color)ColorConverter.ConvertFromString(hex);

            // Crear un SolidColorBrush con el color obtenido
            SolidColorBrush brush = new SolidColorBrush(color);

            return brush;
        }


        public class Guardado
        {
            public Pokemon[] pokemon { get; set; }
            public int encuentros { get; set; } = 0;
            public int phase_encounters { get; set; } = 0;
            public int mode { get; set; } = 0;
            public string ruta { get; set; } = "";
            public int sv_minimo { get; set; } = -1;
            public DateTime phase_start { get; set; } = DateTime.Now;

            public string toString()
            {
                return "\"encuentros\":" + encuentros + ", \"phase_encounters\":" + phase_encounters + ", \"mode\":" + mode + ", \"sv_minimo\":" + sv_minimo + ", \"phase_start\":\"" + phase_start.ToString("o") + "\", \"ruta\":\"" + ruta + "\"";
            }
        }

        public class Config
        {
            public bool guardar_al_salir { get; set; } = true;
            public bool reset_shiny { get; set; } = true;
            public bool radio_activa { get; set; } = false;
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


}