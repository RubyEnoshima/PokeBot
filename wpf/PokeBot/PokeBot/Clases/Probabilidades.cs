using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeBot
{
    internal class Probabilidades
    {
        Dictionary<int,Encuentros> encuentros;

        public Probabilidades()
        {
            encuentros = new Dictionary<int,Encuentros>();
        }

        public void AgregarPokemon(int id)
        {
            Encuentros enc = new Encuentros();
            enc.id = id;
            encuentros.Add(id, enc);
        }

        private void Cargar()
        {

        }

        private void Guardar() 
        {

        }
    }

    public class Encuentros
    {
        public int id;
        public int encuentros_fase = 0;
        public int shinies = 0;
        public int encuentros_totales = 0;
        public Record iv_record;
        public Record sv_record;
    }

    public class Record
    {
        public int max;
        public int min;
    }
}
