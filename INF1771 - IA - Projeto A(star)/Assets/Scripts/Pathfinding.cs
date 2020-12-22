using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using TMPro;

public class Pathfinding : MonoBehaviour
{
    //Posição dos cavaleiros e do objetivo
    public Transform seeker, target;

    Vector3 startPos;

    //Ligar/desligar modo lento
    public bool pathFindingSlowMode;

    //Controlar quão lento será a simulação OBS: menor valor mais rápido, maior valor mais lento
    public float pathfindingSpeed;
    //Controla quão lento os cavaleiros seguirão o caminho OBS: menor valor mais rápido, maior valor mais lento
    public float followPathSpeed;

    //Texto para interface
    public TextMeshProUGUI totalCostText;
    public TextMeshProUGUI specialNodesText;
    public TextMeshProUGUI timerText;
    //Custo do caminho
    int totalCost;

    //Referencia da grid
    Grid grid;

    //Controla se simulação terminou
    bool firstCheckFinished = true;
    bool secondCheckReady = false;

    //Lista de nodes especiais no caminho
    public List<Node> specialNodesInPath;
    public int[] specialNodesDifficulty = { 50, 55, 60, 70, 75, 80, 85, 90, 95, 100, 110, 120 };

    private void Awake()
    {
        startPos = seeker.transform.position;
        specialNodesInPath = new List<Node>();
        grid = GetComponent<Grid>();
    }

    private void Update()
    {
        //Se apertar espaço inicia simulação
        if (Input.GetButtonDown("Jump"))
        {
            //Limpar caminho se ja existe um
            if(grid.path != null)
            {
                grid.path.Clear();
            }
            
            //Limpar nodes especiais no caminho se ja existe algum
            if (specialNodesInPath != null)
            {
                specialNodesInPath.Clear();
            }
            //Limpar nodes verificados se ja existe algum
            if (grid.closedSet != null)
                grid.closedSet.Clear();
            //Limpar nodes da fronteira se ja existe algum
            if (grid.openSet != null)
                grid.openSet.Clear();

            if (seeker.transform.position != startPos)
                seeker.transform.position = startPos;
            
            //Inicia primeira simulação
            else if (firstCheckFinished)
            {
                firstCheckFinished = false;
                StartCoroutine(FindPath(seeker.position, target.position, pathfindingSpeed));
                //Roda função combinatória da ordem de luta, recebe lista com os nodes especiais encontrados no caminho
                // e retorna lista de nodes especiais com a dificuldade traduzida em tempo preenchida
            }
            //Inicia segunda simulação
            else if (secondCheckReady)
            {
                StartCoroutine(FindPath(seeker.position, target.position, pathfindingSpeed));
                StartCoroutine(followPath());
            }
        }
    }

    //Procura caminho final
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, float pathfindingSpeed)
    {
        //Começa contador de tempo
        Stopwatch sw = new Stopwatch();
        sw.Start();

        //Pega nodes onde estão os cavaleiros e seu objetivo
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        //Inicializa listas
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        //Adiciona node inicial dos cavaleiros na lista de nodes de fronteira ainda não verificados
        openSet.Add(startNode);

        //Loop até tudo ser verificado ou acharem o objetivo
        while (openSet.Count > 0)
        {
            //Espera x tempo antes de iniciar o looping caso modo lento esteja ativado
            if(pathFindingSlowMode)
                yield return new WaitForSeconds(pathfindingSpeed);

            //Atualiza listas na grid para serem desenhadas na simulação
            grid.openSet = openSet;
            grid.closedSet = closedSet.ToList();

            //Verifica qual node da lista de fronteira vale mais a pena ser verificado através dos custos da distancia e dificuldade de cada um
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                //fcost é a soma do gcost(custo da distancia do caminho todo + distancia do node atual até seu vizinho + dificuldade do node)
                //com o hcost(distancia do node até o objetivo)
                //se o custo dessa soma (fcost) dos nodes na lista de fronteira for menor que do node atual, esse node da fronteira vira o atual
                //caso o fcost seja igual, desempata pelo mais perto do objetivo (hcost)
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            //Remove node verificado da lista de fronteira e adiciona na lista de nodes verificados
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            //Caso node atual seja o objetivo
            if (currentNode == targetNode)
            {
                //Preenche lista do caminho
                RetracePath(startNode, targetNode);
                
                //calcula custo do caminho
                calculateCost(grid.path);
                
                //Para de contar o tempo e mostra o tempo para achar o caminho
                sw.Stop();
                if (sw.ElapsedMilliseconds > 400)
                {
                    print("Path found in: " + sw.ElapsedMilliseconds / 1000f + " s");
                    timerText.text = "Tempo para achar caminho - " + sw.ElapsedMilliseconds / 1000f + " s";
                }
                else
                {
                    print("Path found in: " + sw.ElapsedMilliseconds + " ms");
                    timerText.text = "Tempo para achar caminho - " + sw.ElapsedMilliseconds + " ms";
                }
                
                //Mostra o custo e a quantidade de nodes especiais no caminho
                if (secondCheckReady)
                {
                    //Adicionando custo total da combinatória
                    totalCost += 373;
                    print("Path Cost: " + totalCost + " minutes");
                    totalCostText.text = "Custo caminho - " + totalCost + " minutos";
                }
                else
                    totalCostText.text = "Custo caminho - " + totalCost + " minutos";
                print("Special Nodes in path: " + specialNodesInPath.Count);
                specialNodesText.text = "Casas especiais no caminho - " + specialNodesInPath.Count + " casas";

                //Terminou segunda simulação
                if (secondCheckReady)
                {
                    firstCheckFinished = true;
                    secondCheckReady = false;
                    print("segunda checagem");
                }
                //Terminou primeira simulação
                if (!firstCheckFinished)
                {
                    secondCheckReady = true;
                    print("primeira checagem");
                }
                break;
            }

            //Para cada node vizinho do atual sendo verificado
            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {
                //Se não for caminhável ou ja tiver sido verificado pula
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                //Custo de movimento baseado na distancia total do caminho traçado até aqui mais a distancia do node atual a seu vizinho mais a dificuldade do node vizinho
                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;

                //Se o gCost ainda não foi atribuido, ou o vizinho ainda não estiver na lista de fronteira
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    //gcost do vizinho recebe valor do custo do movimento
                    neighbour.gCost = newMovementCostToNeighbour;

                    //hcost do vizinho recebe o valor da distancia dele para o objetivo 
                    neighbour.hCost = GetDistance(neighbour, targetNode);

                    //Adiciona o node atual como referencia para os vizinhos para poder traçar o caminho no futuro
                    neighbour.parent = currentNode;
                   
                    //Se ja não estiver na fronteira, adiciona
                    if (!openSet.Contains(neighbour)){
                        openSet.Add(neighbour);
                    }
                }

            }
        }
    }

    //Preenche a lista do caminho a ser seguido
    void RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
       
        //Começa do node final
        Node currentNode = endNode;
       
        //Enquanto não chegar no node inicial
        while (currentNode != startNode)
        {
            //Se for especial adiciona na lista de nodes especiais no caminho também
            if(currentNode.isSpecialNode)
                specialNodesInPath.Add(currentNode);
            
            //Adiciona node na lista do caminho
            path.Add(currentNode);
           
            //Verifica próximo node
            currentNode = currentNode.parent;
        }
       
        //Reverte a ordem das listas para ir do inicio ao final
        path.Reverse();
        specialNodesInPath.Reverse();

        //Passa o caminho e os nodes especiais para a grid desenhar
        grid.path = path;
        grid.specialNodesInPath = specialNodesInPath;
    }

    //Calcula custo do caminho
    void calculateCost(List <Node> path)
    {
        int cost=0;
        //Para cada node no caminho acumula os custos
        for (int i = 0; i < path.Count; i++)
        {
            cost += path[i].movementPenalty;
        }
        //Atribui o custo real
        totalCost = cost/1000;
    }

    //Cavaleiros seguem o caminho
    IEnumerator followPath()
    {
        //Posição dos cavaleiros muda para o node seguinte dentro da lista path
        if (grid.path != null)
        {
            for (int i = 0; i < grid.path.Count; i++)
            {
                seeker.transform.position = grid.path[i].worldPosition + Vector3.up;
                yield return new WaitForSeconds(followPathSpeed);
            }
        }
        

    }
    //calcula a distancia entre dois nodes
    int GetDistance(Node nodeA, Node nodeB)
    {
        //calcula distancia no eixo x e y
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        //Fórmula aproximada para o módulo da distancia entre os dois nodes OBS: geralmente 10 na horizontal e 14 na diagonal
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

}