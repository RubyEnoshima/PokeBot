using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using static PokeBot.MainWindow;

namespace PokeBot
{
    internal class Probabilidades
    {
        string archivo = "";
        Dictionary<int,Encuentros> encuentros;
        Grid grid;
        bool cargado = false;
        Guardado actual;
        string _ruta, _zona;

        public Probabilidades(Grid _grid, string ruta, string zona)
        {
            encuentros = new Dictionary<int,Encuentros>();
            grid = _grid;
            _ruta = ruta.Replace(".json","");
            _zona = zona.Replace(" ","_");
            Cargar();
            if(!cargado) archivo = Environment.CurrentDirectory + "/Encuentros/prob_" + _ruta + "_" + _zona + ".json";

        }

        public void AgregarPokemon(int id)
        {
            if (cargado) return;
            Encuentros enc = new Encuentros();
            enc.id = id;
            //enc.canvas = canvas;
            encuentros.Add(id, enc);
        }

        public void AgregarEncuentro(int id, Pokemon pokemon)
        {
            if (encuentros.ContainsKey(id))
            {
                encuentros[id].encuentros_fase++;
                encuentros[id].encuentros_totales++;
                if (pokemon.shiny == 1) encuentros[id].shinies++;

                ActualizarUI(id);
                
            }
        }

        void ActualizarUI(int id)
        {
            Canvas canvas = EncontrarCanvas(id);
            (canvas.Children[3] as Label).Content = encuentros[id].encuentros_fase;
            (canvas.Children[4] as Label).Content = encuentros[id].encuentros_totales;
        }

        public void ActualizarUI() 
        {
            if (!cargado) return;
            foreach(var e in encuentros)
            {
                ActualizarUI(e.Value.id);
            }
        }

        public void SiguienteFase()
        {
            foreach(var e in encuentros)
            {
                e.Value.encuentros_fase = 0;
                (EncontrarCanvas(e.Value.id).Children[3] as Label).Content = 0;
            }
        }

        Canvas EncontrarCanvas(int id)
        {
            foreach(var c in grid.Children)
            {
                Canvas canvas = c as Canvas;
                if (canvas != null && canvas.Tag.ToString() == id.ToString()) return canvas;
            }
            return null;
        }

        void Cargar()
        {
            archivo = Environment.CurrentDirectory + "/Encuentros/prob_" + _ruta + "_" + _zona + ".json";
            if (File.Exists(archivo))
            {
                string data = File.ReadAllText(archivo);
                actual = JsonConvert.DeserializeObject<Guardado>(data);
                if(actual != null)
                {
                    cargado = true;
                    encuentros = actual.encuentros;
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
        public int encuentros_totales = 0;
        public Record iv_record;
        public Record sv_record;
    }

    public class Record
    {
        public int max = -1;
        public int min = -1;
    }

    
}
