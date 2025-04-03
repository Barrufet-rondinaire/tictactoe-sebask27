using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TicTacToe
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static readonly string baseUrl = "http://localhost:8080";

        static async Task Main(string[] args)
        {
            try
            {
                var participantes = await ObtenerParticipantes();
                Console.WriteLine("=== LISTA DE PARTICIPANTES ===");
                foreach (var p in participantes)
                {
                    Console.WriteLine($"{p.Nombre} ({p.Pais}) - {(p.Descalificado ? "DESCALIFICADO" : "Activo")}");
                }

                var participantesActivos = participantes.Where(p => !p.Descalificado).ToList();

                var contadorVictorias = await ContarVictorias(participantesActivos);

                var maxVictorias = contadorVictorias.Values.Max();
                var ganadores = contadorVictorias.Where(kv => kv.Value == maxVictorias).ToList();

                Console.WriteLine("\n=== RESULTADOS FINALES ===");
                if (ganadores.Count == 0)
                {
                    Console.WriteLine("No hay ganadores (todas las partidas fueron inválidas o no hubo participantes activos)");
                }
                else if (ganadores.Count == 1)
                {
                    var ganador = ganadores[0];
                    var participante = participantes.First(p => p.Nombre == ganador.Key);
                    Console.WriteLine($"¡GANADOR: {ganador.Key} ({participante.Pais}) con {ganador.Value} victorias!");
                }
                else
                {
                    Console.WriteLine($"¡EMPATE entre {ganadores.Count} jugadores con {maxVictorias} victorias cada uno:");
                    foreach (var g in ganadores)
                    {
                        var participante = participantes.First(p => p.Nombre == g.Key);
                        Console.WriteLine($"- {g.Key} ({participante.Pais})");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"\nError al conectar con la API: {e.Message}");
                Console.WriteLine("Asegúrate de que el servidor está ejecutándose en http://localhost:8080");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError inesperado: {e.Message}");
            }
        }

        static async Task<List<Participante>> ObtenerParticipantes()
        {
            var participantes = new List<Participante>();
            var response = await client.GetAsync($"{baseUrl}/jugadors");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var regexParticipante = new Regex(@"participant\s+""([^""]+)""\s+""([^""]+)""");
            var regexDescalificado = new Regex(@"desqualificada\s+""([^""]+)""");

            var matchesParticipantes = regexParticipante.Matches(responseBody);
            var descalificados = regexDescalificado.Matches(responseBody)
                .Select(m => m.Groups[1].Value)
                .ToList();

            foreach (Match match in matchesParticipantes)
            {
                var nombre = match.Groups[1].Value;
                var pais = match.Groups[2].Value;
                participantes.Add(new Participante
                {
                    Nombre = nombre,
                    Pais = pais,
                    Descalificado = descalificados.Contains(nombre)
                });
            }

            return participantes;
        }

        static async Task<Dictionary<string, int>> ContarVictorias(List<Participante> participantesActivos)
        {
            var contador = new Dictionary<string, int>();
            var nombresActivos = participantesActivos.Select(p => p.Nombre).ToList();

            foreach (var nombre in nombresActivos)
            {
                contador[nombre] = 0;
            }

            for (int i = 1; i <= 10000; i++)
            {
                var response = await client.GetAsync($"{baseUrl}/partida/{i}");
                response.EnsureSuccessStatusCode();
                var tablero = await response.Content.ReadAsStringAsync();

                var ganador = DeterminarGanador(tablero);
                if (ganador != null && nombresActivos.Contains(ganador))
                {
                    contador[ganador]++;
                }

                if (i % 1000 == 0)
                {
                    Console.WriteLine($"Analizadas {i} partidas...");
                }
            }

            return contador;
        }

        static string DeterminarGanador(string tablero)
        {
            var filas = tablero.Split('\n').Take(3).ToArray();
            char[,] matriz = new char[3, 3];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matriz[i, j] = filas[i].Length > j ? filas[i][j] : ' ';
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (matriz[i, 0] != '.' && matriz[i, 0] == matriz[i, 1] && matriz[i, 1] == matriz[i, 2])
                {
                    return matriz[i, 0] == '0' ? "jugador1" : "jugador2";
                }
            }

            for (int j = 0; j < 3; j++)
            {
                if (matriz[0, j] != '.' && matriz[0, j] == matriz[1, j] && matriz[1, j] == matriz[2, j])
                {
                    return matriz[0, j] == '0' ? "jugador1" : "jugador2";
                }
            }

            if (matriz[0, 0] != '.' && matriz[0, 0] == matriz[1, 1] && matriz[1, 1] == matriz[2, 2])
            {
                return matriz[0, 0] == '0' ? "jugador1" : "jugador2";
            }
            if (matriz[0, 2] != '.' && matriz[0, 2] == matriz[1, 1] && matriz[1, 1] == matriz[2, 0])
            {
                return matriz[0, 2] == '0' ? "jugador1" : "jugador2";
            }

            return null; 
        }
    }

    class Participante
    {
        public string Nombre { get; set; }
        public string Pais { get; set; }
        public bool Descalificado { get; set; }
    }
}