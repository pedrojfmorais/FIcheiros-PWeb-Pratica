﻿namespace Aula1.Models
{
    public class Agendamento
    {
        public int Id { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public double DuracaoHoras { get; set; }
        public double DuracaoMinutos { get; set; }
        public decimal Preco { get; set; }
        public DateTime DataHoraDoPedido { get; set; }

        public int TipoDeAulaId { get; set; }
        public TipoDeAula tipoDeAula { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
