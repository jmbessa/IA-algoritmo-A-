using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Grid : MonoBehaviour
{
    //Para mostrar apenas nodes utilizados
    public bool onlyDisplayPathGizmos;

    //Posição dos cavaleiros
    public Transform cavs;

    //Verificar Mask não caminhável
    public LayerMask unwalkableMask;

    //Tamanho da grid em metros
    public Vector2 gridWorldSize;
    //Raio do node
    public float nodeRadius;
    //Diametro do node
    float nodeDiameter;
    Node[,] grid;

    //Tamanho da grid em nodes
    int gridSizeX, gridSizeY;
    
    //Declarando cores
    private Color red, green, white, black;

    private void Start()
    {
        //Cores para simular o caminho na sena usando OnDrawGizmos
        red = new Color(1f, 0f, 0f, 0.3f);
        green = new Color(0f, 1f, 0f, 0.3f);
        white = new Color(1f, 1f, 1f, 0.3f);
        black = new Color(0f, 0f, 0f, 0.4f);
       
        //Define o diametro do node baseado em seu raio
        nodeDiameter = nodeRadius * 2;
      
        //Pega o tamanho da grid dependendo do tamanho do node
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
       
        //Chama a função para criar a grid com 0.3 segundos de atraso para esperar o mapa estar gerado
        Invoke( "CreateGrid",0.3f);
    }
    
    //Cria grid
    private void CreateGrid()
    {
        //Inicializa uma grid do tamanho do mapa em Nodes.
        grid = new Node[gridSizeX, gridSizeY];
        
        //Pega a posição do canto inferior esquerdo da grid para ser usada como referencia
        Vector3 worldBottonLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
       
        //Para cada Node dentro da grid
        for (int x = 0; x < gridSizeX; x++){
            for (int y = 0; y < gridSizeY; y++){
                
                //Guarda a posição no mundo desse Node específico
                Vector3 worldPoint = worldBottonLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                
                //Verifica se o node esta em uma superfície não caminhável.
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius,unwalkableMask));

                int movementPenalty = 0;
                bool isSpecial=false;

                //Se o node é caminhável
                if (walkable)
                {
                    //Lança um raycast para verificar o custo do caminho e atribuir ao node
                    Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;
                    if(Physics.Raycast(ray,out hit, 100))
                    {
                        GameObject gObject = hit.collider.gameObject;
                        //Se for uma casa normal atribui a dificuldade dela ao node
                        if (gObject.transform.tag == "mapa")
                        {
                            isSpecial = false;
                            movementPenalty = gObject.GetComponent<Difficulty>().difficulty;
                        }
                        //Se for uma casa especial atribui a dificuldade 0 e avisa que é um node especial
                        else if (gObject.transform.tag == "mapaespecial")
                        {
                            isSpecial = true;
                            movementPenalty = 0;
                        }
                        else
                            isSpecial = false;
                    }
                }
                //Cria uma instancia do node na grid com o custo verificado anteriormente e se é especial ou nao
                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty*1000);
                grid[x, y].isSpecialNode = isSpecial;
            }
        }
    }

    //Pega os vizinhos laterais do node de referencia
    public List<Node> GetNeighbours(Node node)
    {
        //Lista para ser retornada com os vizinhos válidos do node passado na função
        List<Node> neighbours = new List<Node>();
        
        //Pega posição do vizinho direito
        int checkX = node.gridX + 1;
        int checkY = node.gridY;
        //Checa se node vizinho esta fora da grid
        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            //se não estiver fora, adiciona na lista
            neighbours.Add(grid[checkX, checkY]);
        }
       
        //Pega posição do vizinho esquerdo
        checkX = node.gridX - 1;
        checkY = node.gridY;
        //Checa se node vizinho esta fora da grid
        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            //se não estiver fora, adiciona na lista
            neighbours.Add(grid[checkX, checkY]);
        }

        //Pega posição do vizinho de cima
        checkX = node.gridX;
        checkY = node.gridY + 1;
        //Checa se node vizinho esta fora da grid
        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            //se não estiver fora, adiciona na lista
            neighbours.Add(grid[checkX, checkY]);
        }

        //Pega posição do vizinho de baixo
        checkX = node.gridX;
        checkY = node.gridY - 1;
        //Checa se node vizinho esta fora da grid
        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        {
            //se não estiver fora, adiciona na lista
            neighbours.Add(grid[checkX, checkY]);
        }
        //retorna a lista com os vizinhos laterais do node (para não andar na diagonal)
        return neighbours;
    }

    //Pega node na posição de mundo passada
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        //Pega uma porcentagem no eixo x e y da posição do objeto dentro da grid
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

        //Garante valor entre 0 e 1
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        //Tamanho total da grid nos eixos x e y, multiplicados pela porcentagem, retornando o node nessa posição
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    //Nodes no caminho calculado
    public List<Node> path;
    //Fronteira de nodes vizinhos ainda não verificados
    public List<Node> openSet;
    //Nodes ja verificados
    public List<Node> closedSet;
    //Casas especiais presentes no caminho
    public List<Node> specialNodesInPath;

    //Desenha cubos representando os nodes
    private void OnDrawGizmos(){
        //Desenha um cubo do tamanho da grid
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        
        //Desenhar apenas o caminho, nodes verificados e fronteira não verificada
        if (onlyDisplayPathGizmos)
        {
            //Desenhar cubo verde nos nodes da fronteira
            if (openSet != null)
                foreach (Node n in openSet)
                {
                    Gizmos.color = green;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            //Desenhar cubo vermelho nos nodes da verificados
            if (closedSet != null)
                foreach (Node n in closedSet)
                {
                    Gizmos.color = red;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            //Desenhar cubo preto nos nodes do caminho achado
            if (path != null)
            {
                foreach (Node n in path)
                {
                    Gizmos.color = black;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
            //Desenhar cubo azul nos nodes especiais pelo caminho
            if (specialNodesInPath != null)
            {
                foreach (Node n in specialNodesInPath)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }
        //Desenhar todos os nodes da grid sem exceção OBS: impacto na performance
        else{
            if (grid != null)
            {
                //Pega node em que os cavaleiros estão
                Node cavsNode = NodeFromWorldPoint(cavs.position);

                //Para cada node na grid verifica
                foreach (Node n in grid)
                {
                    //Se for uma superfícies caminhável node branco, se não magenta.
                    Gizmos.color = (n.walkable) ? white : Color.magenta;
                    
                    //Cor verde para nodes na fronteira
                    if (openSet != null)
                        if (openSet.Contains(n))
                            Gizmos.color = green;
                    
                    //Cor vermelha para ja nodes verificados
                    if (closedSet != null)
                        if (closedSet.Contains(n))
                            Gizmos.color = red;

                    //Cor preta para nodes no caminho
                    if (path != null)
                        if (path.Contains(n))
                            Gizmos.color = black;
                    
                    //Cor azul para nodes especiais
                    if (specialNodesInPath != null)
                        if(specialNodesInPath.Contains(n))
                            Gizmos.color = Color.blue;
                    
                    //Cor cyan para a node dos cavaleiros
                    if (cavsNode == n)
                    {
                        Gizmos.color = Color.cyan;
                    }

                    //Desenha cubo na posição e do tamanho dos nodes, da cor anteriormente atribuída
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }
    }
}
