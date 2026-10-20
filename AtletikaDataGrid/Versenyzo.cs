namespace AtletikaDataGrid
{
    public class Versenyzo
    {
        public int Rajtszam { get; set; }
        public string Nev { get; set; } = "";
        public string Versenyszam { get; set; } = "";
        public string Nem { get; set; } = ""; // F vagy N
        public int Eletkor { get; set; }
        public bool OR { get; set; }
        public bool VR { get; set; }
        public bool ER { get; set; }
    }
}
