using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokeBot
{
    internal class Probabilidades
    {
        private string archivo = "";
        private Dictionary<int, Encuentros> encuentros;
        private Grid grid;
        private bool cargado = false;
        private Guardado actual;
        private string _ruta, _zona;

        private readonly int[] minimosTotal = new int[] { 186, 145, 70, 29, 1, 0 };
        private readonly int[] minimosSV = new int[] { 65536, 45001, 35001, 10001, 8, 0 };
        private readonly Brush[] colores = new Brush[]
        {
            Colorette("#F5D236"),
            Brushes.YellowGreen,
            Colorette("#FFFFFF"),
            Colorette("#F75555"),
            Colorette("#8F1515"),
            Brushes.DarkOrchid
        };

        public Probabilidades(Grid _grid, string ruta, string zona)
        {
            encuentros = new Dictionary<int, Encuentros>();
            grid = _grid;
            _ruta = ruta.Replace(".json", "");
            _zona = zona.Replace(" ", "_");
            Cargar();

            if (!cargado)
                archivo = Environment.CurrentDirectory + "/Encuentros/prob_" + _ruta + "_" + _zona + ".json";
        }

        public void AgregarPokemon(int id)
        {
            if (!cargado) return;

            if (!encuentros.ContainsKey(id))
            {
                Encuentros enc = new Encuentros();
                enc.id = id;
                encuentros.Add(id, enc);
            }
        }

        public void AgregarEncuentro(int id, Pokemon pokemon)
        {
            if (!encuentros.ContainsKey(id)) return;

            Encuentros enc = encuentros[id];

            enc.encuentros_fase++;
            enc.encuentros_totales++;

            int ivTotal = pokemon.ivs.hp + pokemon.ivs.att + pokemon.ivs.def + pokemon.ivs.spatt + pokemon.ivs.spdef + pokemon.ivs.speed;

            if (enc.iv_record == null) enc.iv_record = new Record();
            if (enc.iv_record.max == -1 || ivTotal > enc.iv_record.max) enc.iv_record.max = ivTotal;
            if (enc.iv_record.min == -1 || ivTotal < enc.iv_record.min) enc.iv_record.min = ivTotal;

            if (enc.sv_record == null) enc.sv_record = new Record();
            if (enc.sv_record.min == -1 || pokemon.sv < enc.sv_record.min) enc.sv_record.min = pokemon.sv;
            if (enc.sv_record.max == -1 || pokemon.sv > enc.sv_record.max) enc.sv_record.max = pokemon.sv;

            ActualizarUI();
        }

        private void ActualizarUI(int id)
        {
            List<Canvas> canvases = EncontrarCanvas(id);
            if (!encuentros.ContainsKey(id)) return;

            Encuentros enc = encuentros[id];
            int totalFaseGlobal = ObtenerTotalFaseVisible();

            foreach (Canvas c in canvases)
            {
                Label lPhaseEncounters = FindLabel(c, "PhaseEncounters");
                Label lTotalEncounters = FindLabel(c, "TotalEncounters");
                Label lPhaseSVBest = FindLabel(c, "PhaseSVBest");
                Label lPhaseSVWorst = FindLabel(c, "PhaseSVWorst");
                Label lPhaseIVBest = FindLabel(c, "PhaseIVBest");
                Label lPhaseIVWorst = FindLabel(c, "PhaseIVWorst");
                Label lPhaseEncountersPercent = FindLabel(c, "PhaseEncountersPercent");
                Label lTotalShinyRatio = FindLabel(c, "TotalShinyRatio");
                Label lShiniesObtained = FindLabel(c, "ShiniesObtained");
                Label lShiniesMissed = FindLabel(c, "ShiniesMissed");

                if (lPhaseEncounters != null)
                    lPhaseEncounters.Content = enc.encuentros_fase;

                if (lTotalEncounters != null)
                    lTotalEncounters.Content = enc.encuentros_totales;

                if (lPhaseSVBest != null)
                    lPhaseSVBest.Content = (enc.sv_record != null && enc.sv_record.min != -1)
                        ? enc.sv_record.min.ToString()
                        : "-";

                if (lPhaseSVWorst != null)
                    lPhaseSVWorst.Content = (enc.sv_record != null && enc.sv_record.max != -1)
                        ? enc.sv_record.max.ToString()
                        : "-";

                if (lPhaseIVBest != null)
                    lPhaseIVBest.Content = (enc.iv_record != null && enc.iv_record.max != -1)
                        ? enc.iv_record.max.ToString()
                        : "-";

                if (lPhaseIVWorst != null)
                    lPhaseIVWorst.Content = (enc.iv_record != null && enc.iv_record.min != -1)
                        ? enc.iv_record.min.ToString()
                        : "-";

                if (lPhaseEncountersPercent != null)
                {
                    double porcentaje = totalFaseGlobal > 0
                        ? (enc.encuentros_fase * 100.0) / totalFaseGlobal
                        : 0.0;

                    lPhaseEncountersPercent.Content = porcentaje.ToString("0.0") + "%";
                }

                if (lTotalShinyRatio != null)
                {
                    int totalShinies = enc.shinies + enc.huidos;
                    int totalEncuentros = enc.encuentros_totales;

                    if (totalEncuentros > 0)
                    {
                        if (totalShinies > 0)
                        {
                            int mcd = CalcularMCD(totalShinies, totalEncuentros);
                            int num = totalShinies / mcd;
                            int den = totalEncuentros / mcd;

                            lTotalShinyRatio.Content = "✨ " + num + "/" + den;
                        }
                        else
                        {
                            lTotalShinyRatio.Content = "✨ 0/" + totalEncuentros;
                        }
                    }
                    else
                    {
                        lTotalShinyRatio.Content = "✨ -";
                    }
                }

                if (lShiniesObtained != null)
                {
                    lShiniesObtained.Content = enc.shinies + "/" + (enc.shinies + enc.huidos);
                    Canvas.SetTop(lShiniesObtained, enc.huidos > 0 ? 14 : 20);
                }

                if (lShiniesMissed != null)
                {
                    lShiniesMissed.Content = "(" + enc.huidos + " huídos)";
                    lShiniesMissed.Visibility = enc.huidos > 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                // Reset
                if (lPhaseSVBest != null) lPhaseSVBest.Foreground = Brushes.White;
                if (lPhaseSVWorst != null) lPhaseSVWorst.Foreground = Brushes.White;
                if (lPhaseIVBest != null) lPhaseIVBest.Foreground = Brushes.White;
                if (lPhaseIVWorst != null) lPhaseIVWorst.Foreground = Brushes.White;

                // Colors correctes
                if (lPhaseSVBest != null && enc.sv_record != null && enc.sv_record.min != -1)
                    PintarColorSV(lPhaseSVBest, enc.sv_record.min);

                if (lPhaseSVWorst != null && enc.sv_record != null && enc.sv_record.max != -1)
                    PintarColorSV(lPhaseSVWorst, enc.sv_record.max);

                if (lPhaseIVBest != null && enc.iv_record != null && enc.iv_record.max != -1)
                    PintarColorIVTotal(lPhaseIVBest, enc.iv_record.max);

                if (lPhaseIVWorst != null && enc.iv_record != null && enc.iv_record.min != -1)
                    PintarColorIVTotal(lPhaseIVWorst, enc.iv_record.min);
            }
        }

        public void ActualizarUI()
        {
            foreach (var e in encuentros)
            {
                ActualizarUI(e.Value.id);
            }
        }

        public void MarcarShinyObtenido(int id)
        {
            if (!encuentros.ContainsKey(id)) return;

            encuentros[id].shinies++;
            ActualizarUI();
            Guardar();
        }

        public void MarcarShinyEscapado(int id)
        {
            if (!encuentros.ContainsKey(id)) return;

            encuentros[id].huidos++;
            ActualizarUI();
            Guardar();
        }

        public void SiguienteFase()
        {
            foreach (var e in encuentros.Values)
            {
                e.encuentros_fase = 0;
                e.iv_record = new Record();
                e.sv_record = new Record();
            }

            ActualizarUI();
            Guardar();
        }

        private int CalcularMCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        private int ObtenerTotalFaseVisible()
        {
            int total = 0;
            HashSet<int> idsVisibles = new HashSet<int>();

            foreach (var c in grid.Children)
            {
                Canvas canvas = c as Canvas;
                if (canvas == null)
                    continue;

                if (canvas.Visibility != Visibility.Visible)
                    continue;

                if (canvas.Tag == null)
                    continue;

                int id;
                if (!int.TryParse(canvas.Tag.ToString(), out id))
                    continue;

                if (idsVisibles.Contains(id))
                    continue;

                idsVisibles.Add(id);

                if (encuentros.ContainsKey(id))
                    total += encuentros[id].encuentros_fase;
            }

            return total;
        }

        private List<Canvas> EncontrarCanvas(int id)
        {
            List<Canvas> res = new List<Canvas>();

            foreach (var c in grid.Children)
            {
                Canvas canvas = c as Canvas;
                if (canvas != null && canvas.Tag != null && canvas.Tag.ToString() == id.ToString())
                    res.Add(canvas);
            }

            return res;
        }

        private Label FindLabel(Canvas canvas, string name)
        {
            foreach (var child in canvas.Children)
            {
                if (child is Label label && !string.IsNullOrEmpty(label.Name))
                {
                    if (label.Name == name)
                        return label;
                    if (label.Name.StartsWith(name))
                    {
                        string sufix = label.Name.Substring(name.Length);
                        if (sufix.All(char.IsDigit))
                            return label;
                    }
                }
            }

            return null;
        }

        private void PintarColorIVTotal(Label label, int valor)
        {
            if (label == null) return;

            if (valor >= 186) label.Foreground = Colorette("#F5D236");
            else if (valor >= 145) label.Foreground = Brushes.YellowGreen;
            else if (valor >= 70) label.Foreground = Colorette("#FFFFFF");
            else if (valor >= 29) label.Foreground = Colorette("#F75555");
            else if (valor >= 1) label.Foreground = Colorette("#8F1515");
            else label.Foreground = Brushes.DarkOrchid;
        }

        private void PintarColorSV(Label label, int valor)
        {
            if (label == null) return;

            if (valor <= 8) label.Foreground = Colorette("#F5D236");
            else if (valor <= 10000) label.Foreground = Brushes.YellowGreen;
            else if (valor <= 35000) label.Foreground = Colorette("#FFFFFF");
            else if (valor <= 45000) label.Foreground = Colorette("#F75555");
            else if (valor <= 65535) label.Foreground = Colorette("#8F1515");
            else label.Foreground = Brushes.DarkOrchid;
        }

        private static SolidColorBrush Colorette(string hex)
        {
            Color color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }

        private void Cargar()
        {
            archivo = Environment.CurrentDirectory + "/Encuentros/prob_" + _ruta + "_" + _zona + ".json";

            if (File.Exists(archivo))
            {
                string data = File.ReadAllText(archivo);
                actual = JsonConvert.DeserializeObject<Guardado>(data);

                if (actual != null)
                {
                    cargado = true;
                    encuentros = actual.encuentros ?? new Dictionary<int, Encuentros>();

                    foreach (var e in encuentros.Values)
                    {
                        if (e.iv_record == null) e.iv_record = new Record();
                        if (e.sv_record == null) e.sv_record = new Record();
                    }
                }
            }
        }

        public void Guardar()
        {
            Guardado guardado = new Guardado();
            guardado.ruta = _ruta;
            guardado.zona = _zona;
            guardado.encuentros = encuentros;

            File.WriteAllText(archivo, JsonConvert.SerializeObject(guardado, Formatting.Indented));
        }

        class Guardado
        {
            public string ruta = "";
            public string zona = "";
            public Dictionary<int, Encuentros> encuentros;
        }
    }

    public class Encuentros
    {
        public int id = -1;
        public int encuentros_fase = 0;
        public int shinies = 0;
        public int huidos = 0;
        public int encuentros_totales = 0;
        public Record iv_record = new Record();
        public Record sv_record = new Record();
    }

    public class Record
    {
        public int max = -1;
        public int min = -1;
    }
}