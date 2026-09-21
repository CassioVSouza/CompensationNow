namespace CompenseAgora.Entities
{
    public class FatorEmissaoFrota
    {
        public int Codigo { get; set; }
        public int CodigoFrota { get; set; }
        public Frota Frota { get; set; } = null!;
        public decimal CH4 { get; set; }
        public decimal N2O { get; set; }
        public int Ano { get; set; }
        public bool ParaTodos { get; set; }
    }
}
