using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    //Vetor com objetos a serem instanciados
    public GameObject[] tilePrefabs; //OBS: Legenda do mapa texto: 0=Casa especial, 1=Rochoso, 2=Plano, 3=Montanhoso, 4=Origem, 5=Destino

    //Referencia a grid
    Grid grid;

    //Tamanho de cada tile baseado no tamanho de um node
    public float tileSize { get { return grid.nodeRadius * 2; } }

    private void Awake()
    {
        grid = GetComponent<Grid>();
    }
    void Start()
    {
        createLevel();
    }

    //Instancia um game object baseado no caractere do tileType recebido
    private void placeTile(string tileType, int x, int y, int mapX, int mapY, Vector3 worldStart)
    {
        //Converte caractere para int
        int tileIndex = int.Parse(tileType);

        //Usa como Index do vetor de objetos a serem instanciados
        GameObject newTile = Instantiate(tilePrefabs[tileIndex]);

        //Ajusta posição do objeto para alinhar com o texto
        newTile.transform.position = new Vector3((0.5f + worldStart.x - mapX / 2) + (tileSize * x), -0.4f, (worldStart.z - 0.5f + mapY / 2) - (tileSize * y));
    }

    //Gera o mapa
    private void createLevel()
    {
        //Vetor com cada elemento sendo uma linha do texto
        string[] mapData = readLevelText();

        //Pega o tamanho x do mapa baseado na quantidade de caracteres por linha
        int mapX = mapData[0].ToCharArray().Length;
        //Pega tamanho y do mapa baseado no tamanho do vetor
        int mapY = mapData.Length;

        //para cada caractere no texto
        for (int y = 0; y < mapY; y++)
        {
            //Cria uma instancia equivalente ao caractere
            char[] newTiles = mapData[y].ToCharArray();
            for (int x = 0; x < mapX; x++)
            {
                placeTile(newTiles[x].ToString(), x, y, mapX, mapY, transform.position);
            }
        }
    }

    //Le o mapa por um texto e retorna um vetor com cada elemento = uma linha do texto
    private string[] readLevelText()
    {
        //Pega o texto com informação do mapa
        TextAsset bindData = Resources.Load("Level") as TextAsset;

        //Transforma cada linha do texto separada por um "-" em um elemento do vetor e retorna
        string data = bindData.text.Replace(Environment.NewLine, string.Empty);
        return data.Split('-');
    }
}

