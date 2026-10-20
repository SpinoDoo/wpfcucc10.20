using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace AtletikaDataGrid
{
    public partial class MainWindow : Window
    {
        private const string CsvFejlec = "Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER";
        // a korábbi feladat fejléce: betöltéskor ezt is elfogadjuk
        private const string RegiCsvFejlec = "Rajtszam;Nev;Versenyszam;Nem;Eletkor;OlimpiaiCsucs;VBCsucs;EBCsucs";

        public ObservableCollection<Versenyzo> Versenyzok { get; } = new();

        private readonly ICollectionView nezet;
        private bool feluletKesz = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            nezet = CollectionViewSource.GetDefaultView(Versenyzok);
            nezet.Filter = SzuroFeltetel;

            feluletKesz = true;
            FrissitNezet();
        }

        // ---------- Életkor ----------
        private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtEletkor == null) return;
            txtEletkor.Text = $"{(int)e.NewValue} év";
        }

        // ---------- Szűrés ----------
        private bool SzuroFeltetel(object elem)
        {
            if (elem is not Versenyzo v) return false;
            switch (cmbRekordSzuro.SelectedIndex)
            {
                case 1: return v.OR;
                case 2: return v.VR;
                case 3: return v.ER;
                default: return true; // Összes
            }
        }

        private void FrissitNezet()
        {
            nezet.Refresh();
            int lathato = nezet.Cast<object>().Count();
            txtLetszam.Text = $"Látható: {lathato} / Összesen: {Versenyzok.Count}";
        }

        private void Szuro_Valtozott(object sender, SelectionChangedEventArgs e)
        {
            if (!feluletKesz) return;
            FrissitNezet();
        }

        // ---------- Regisztráció ----------
        private void Regisztral_Click(object sender, RoutedEventArgs e)
        {
            string nev = txtNev.Text.Trim();
            if (nev == "")
            {
                Figyelmeztet("A versenyző nevének megadása kötelező.");
                return;
            }
            if (nev.Contains(';') || nev.Contains('|'))
            {
                Figyelmeztet("A névben pontosvessző és függőleges vonal nem használható.");
                return;
            }

            if (cmbVersenyszam.SelectedItem is not ComboBoxItem kivalasztott)
            {
                Figyelmeztet("Válasszon versenyszámot.");
                return;
            }
            string versenyszam = kivalasztott.Content.ToString();

            if (rbFerfi.IsChecked != true && rbNo.IsChecked != true)
            {
                Figyelmeztet("Adja meg a versenyző nemét.");
                return;
            }
            string nem = rbFerfi.IsChecked == true ? "F" : "N";

            string kertSzoveg = txtRajtszam.Text.Trim();
            int rajtszam = 0;
            bool kert = kertSzoveg != "";
            if (kert)
            {
                if (!int.TryParse(kertSzoveg, NumberStyles.None, CultureInfo.InvariantCulture, out rajtszam)
                    || rajtszam < 1 || rajtszam > 9999)
                {
                    Figyelmeztet("A rajtszám 1 és 9999 közötti egész szám lehet.");
                    return;
                }
                // foglaltság: mindig a teljes gyűjteményben (a rejtett sorok is számítanak)
                if (Versenyzok.Any(v => v.Rajtszam == rajtszam))
                {
                    Figyelmeztet("A kért rajtszám már foglalt. Adjon meg másikat, vagy hagyja üresen a mezőt.");
                    return;
                }
            }
            else
            {
                var foglalt = new HashSet<int>(Versenyzok.Select(v => v.Rajtszam));
                for (int i = 1; i <= 9999; i++)
                {
                    if (!foglalt.Contains(i)) { rajtszam = i; break; }
                }
                if (rajtszam == 0)
                {
                    Figyelmeztet("Nincs több szabad rajtszám.");
                    return;
                }
            }

            Versenyzok.Add(new Versenyzo
            {
                Rajtszam = rajtszam,
                Nev = nev,
                Versenyszam = versenyszam,
                Nem = nem,
                Eletkor = (int)sldEletkor.Value,
                OR = chkOlimpiai.IsChecked == true,
                VR = chkVB.IsChecked == true,
                ER = chkEB.IsChecked == true
            });
            FrissitNezet(); // az aktuális szűrő érvényben marad

            MessageBox.Show($"A regisztráció sikeres. Kiosztott rajtszám: {rajtszam}",
                "Regisztráció", MessageBoxButton.OK, MessageBoxImage.Information);

            txtNev.Clear();
            txtRajtszam.Clear();
        }

        private static void Figyelmeztet(string uzenet)
        {
            MessageBox.Show(uzenet, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ---------- Mentés ----------
        private void Mentes_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog ablak = new()
            {
                Filter = "CSV-fájl (*.csv)|*.csv|Szövegfájl (*.txt)|*.txt",
                FilterIndex = 1,
                FileName = "atletika",
                AddExtension = false
            };
            if (ablak.ShowDialog() != true) return;

            string elvart = ablak.FilterIndex == 1 ? ".csv" : ".txt";
            string kapott = Path.GetExtension(ablak.FileName);
            if (kapott != "" && kapott.ToLower() != elvart)
            {
                Figyelmeztet("A fájltípus és a kiterjesztés nem egyezik.");
                return;
            }
            string fajl = kapott == "" ? ablak.FileName + elvart : ablak.FileName;

            // Ha mi fűztük hozzá a kiterjesztést, a dialógus nem kérdezett rá a felülírásra.
            if (kapott == "" && File.Exists(fajl))
            {
                var valasz = MessageBox.Show($"A(z) {Path.GetFileName(fajl)} már létezik. Felülírja?",
                    "Felülírás", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (valasz != MessageBoxResult.Yes) return;
            }

            try
            {
                if (ablak.FilterIndex == 1)
                    MentesCsv(fajl);
                else
                    MentesTxt(fajl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a fájl mentésekor: " + ex.Message,
                    "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // A mentés mindig a teljes Versenyzok gyűjteményből történik, a szűrőtől függetlenül.
        private void MentesCsv(string fajl)
        {
            var sorok = new List<string> { CsvFejlec };
            foreach (Versenyzo v in Versenyzok)
            {
                sorok.Add($"{v.Rajtszam};{v.Nev};{v.Versenyszam};{v.Nem};{v.Eletkor};" +
                          $"{(v.OR ? 1 : 0)};{(v.VR ? 1 : 0)};{(v.ER ? 1 : 0)}");
            }
            File.WriteAllLines(fajl, sorok, new UTF8Encoding(true));
        }

        private void MentesTxt(string fajl)
        {
            string nl = Environment.NewLine;
            var blokkok = Versenyzok.Select(v => v.Nev + nl + Bekezdes(v));
            File.WriteAllText(fajl, string.Join(nl + nl, blokkok), new UTF8Encoding(true));
        }

        private static string Bekezdes(Versenyzo v)
        {
            string nem = v.Nem == "F" ? "férfi" : "női";
            string orSzoveg = v.OR ? "rendelkezik" : "nem rendelkezik";
            string vrSzoveg = v.VR ? "rendelkezik" : "nem rendelkezik";
            string erSzoveg = v.ER ? "rendelkezik" : "nem rendelkezik";
            return $"A {v.Eletkor} éves {nem} versenyző a(z) " +
                   $"{v.Versenyszam} versenyszámban indul, rajtszáma {v.Rajtszam}. " +
                   $"Olimpiai csúccsal {orSzoveg}, világcsúccsal {vrSzoveg}, " +
                   $"Európa-csúccsal {erSzoveg}.";
        }

        // ---------- Betöltés ----------
        private void Betoltes_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ablak = new()
            {
                Filter = "CSV-fájl (*.csv)|*.csv"
            };
            if (ablak.ShowDialog() != true) return;

            try
            {
                string[] sorok = File.ReadAllLines(ablak.FileName, Encoding.UTF8);
                if (sorok.Length == 0)
                    throw new InvalidDataException("A fájl üres.");

                string fejlec = sorok[0].Trim().TrimStart('\uFEFF');
                if (fejlec != CsvFejlec && fejlec != RegiCsvFejlec)
                    throw new InvalidDataException("A fájl fejléce nem a várt CSV-szerkezetet mutatja.");

                // először ideiglenes listába dolgozzuk fel
                var ideiglenes = new List<Versenyzo>();
                var latottSzamok = new HashSet<int>();
                for (int i = 1; i < sorok.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(sorok[i])) continue;
                    string[] mezok = sorok[i].Split(';');
                    if (mezok.Length != 8)
                        throw new InvalidDataException($"A(z) {i + 1}. sor nem nyolc mezőből áll.");

                    var v = new Versenyzo
                    {
                        Rajtszam = int.Parse(mezok[0].Trim(), CultureInfo.InvariantCulture),
                        Nev = mezok[1].Trim(),
                        Versenyszam = mezok[2].Trim(),
                        Nem = mezok[3].Trim(),
                        Eletkor = int.Parse(mezok[4].Trim(), CultureInfo.InvariantCulture),
                        OR = mezok[5].Trim() == "1",
                        VR = mezok[6].Trim() == "1",
                        ER = mezok[7].Trim() == "1"
                    };
                    if (!latottSzamok.Add(v.Rajtszam))
                    {
                        MessageBox.Show($"A fájlban a(z) {v.Rajtszam} rajtszám többször szerepel. A betöltés megszakadt, a korábbi adatok megmaradtak.",
                            "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    ideiglenes.Add(v);
                }

                // siker után: ugyanazt a gyűjteményt ürítjük és töltjük fel
                Versenyzok.Clear();
                foreach (Versenyzo v in ideiglenes)
                    Versenyzok.Add(v);
                FrissitNezet();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a fájl betöltésekor: " + ex.Message + "\nA korábbi adatok megmaradtak.",
                    "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
